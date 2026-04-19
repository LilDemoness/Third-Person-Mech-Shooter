using Gameplay.GameplayObjects.Character;
using Gameplay.GameplayObjects.Character.Statistics;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.GameplayObjects
{
    /// <summary>
    ///     Handles the Health & Life of an object within the game, synced between the Server and Clients via NetworkVariables.<br/>
    ///     Also calculates adjustments to damage/healing based on StatusEffects (Eventually).
    /// </summary>
    /// <remarks>
    ///     All functions marked with '_Server' should only be called on the Server.
    ///     All events are send to the Server and Clients (SendTo.Everyone)
    /// </remarks>
    public class NetworkHealthComponent : NetworkBehaviour, IDamageable
    {
        private ServerCharacter _serverCharacter;


        private NetworkVariable<float> _currentHealth { get; } = new NetworkVariable<float>();
        public float MaxHealth { get; set; } = -1.0f;   // Initial value for notifying us that we haven't set the value for some reason.

        private NetworkVariable<float> _currentShields { get; } = new NetworkVariable<float>();
        public float MaxShields { get; set; } = 0.0f;

        private float _lastDamageTime;  // Server Only.
        private float _shieldRechargeDelay => _serverCharacter.CharacterStats.GetStatisticValue(Statistic.ShieldRechargeDelay);
        private float _shieldRechargeRate => _serverCharacter.CharacterStats.GetStatisticValue(Statistic.ShieldRechargeRate);

        private NetworkVariable<LifeState> _lifeState { get; } = new NetworkVariable<LifeState>(LifeState.Alive);


        public bool IsDead => _lifeState.Value != Character.LifeState.Alive;


        #region Events

        public event System.Action OnInitialised;
        public bool IsInitialised = false;

        public static event System.Action<AnyHealthChangeEventArgs> OnAnyHealthChange;
        public static event System.Action<AnyShieldsChangeEventArgs> OnAnyShieldsChange;
        public event System.Action<float, float> OnHealthChanged;

        public event System.Action<BaseDamageReceiverEventArgs> OnDied;
        public event System.Action<BaseDamageReceiverEventArgs> OnRevived;
        public event System.Action<ulong?, LifeState> OnLifeStateChanged;


        public event System.Action<Collision> CollisionEntered;

        #endregion

        #region Event RPCs

        [Rpc(SendTo.Everyone)]
        private void NotifyOfInitialisationRpc(float maxHealth, float maxShields)
        {
            if (!IsServer)
                this.MaxHealth = maxHealth;

            IsInitialised = true;
            OnInitialised?.Invoke();

            Debug.Log($"Notify Health Change ({_currentHealth.Value}|{maxHealth}) for {this.OwnerClientId}");
            OnAnyHealthChange?.Invoke(new AnyHealthChangeEventArgs(null, _serverCharacter, _currentHealth.Value, maxHealth, 0.0f));
            OnAnyShieldsChange?.Invoke(new AnyShieldsChangeEventArgs(null, _serverCharacter, _currentShields.Value, maxShields, 0.0f));
        }


        private void NotifyOfHealthChange(ServerCharacter inflicter, float healthChange)
        {
            if (inflicter == null)
                NotifyOfHealthChangeRpc(_currentHealth.Value, MaxHealth, healthChange);
            else
                NotifyOfHealthChangeRpc(inflicter.NetworkObjectId, _currentHealth.Value, MaxHealth, healthChange);
        }
        [Rpc(SendTo.Everyone)]
        private void NotifyOfHealthChangeRpc(float newHealth, float newMaxHealth, float healthChange) => OnAnyHealthChange?.Invoke(new AnyHealthChangeEventArgs(null, _serverCharacter, newHealth, newMaxHealth, healthChange));
        [Rpc(SendTo.Everyone)]
        private void NotifyOfHealthChangeRpc(ulong inflicterObjectId, float newHealth, float newMaxShields, float healthChange) => OnAnyHealthChange?.Invoke(new AnyHealthChangeEventArgs(GetServerCharacterForObjectId(inflicterObjectId), _serverCharacter, newHealth, newMaxShields, healthChange));


        private void NotifyOfShieldsChange(ServerCharacter inflicter, float shieldsChange)
        {
            if (inflicter == null)
                NotifyOfShieldsChangeRpc(_currentShields.Value, MaxShields, shieldsChange);
            else
                NotifyOfShieldsChangeRpc(inflicter.NetworkObjectId, _currentShields.Value, MaxShields, shieldsChange);
        }
        [Rpc(SendTo.Everyone)]
        private void NotifyOfShieldsChangeRpc(float newShields, float newMaxShields, float healthChange) => OnAnyShieldsChange?.Invoke(new AnyShieldsChangeEventArgs(null, _serverCharacter, newShields, newMaxShields, healthChange));
        [Rpc(SendTo.Everyone)]
        private void NotifyOfShieldsChangeRpc(ulong inflicterObjectId, float newShields, float newMaxShields, float healthChange) => OnAnyShieldsChange?.Invoke(new AnyShieldsChangeEventArgs(GetServerCharacterForObjectId(inflicterObjectId), _serverCharacter, newShields, newMaxShields, healthChange));



        private void NotifyOfDeath(ServerCharacter inflicter)
        {
            if (inflicter == null)
                NotifyOfDeathRpc();
            else
                NotifyOfDeathRpc(inflicter.NetworkObjectId);
        }
        [Rpc(SendTo.Everyone)]
        private void NotifyOfDeathRpc(ulong inflicterObjectId)
        {
            OnLifeStateChanged?.Invoke(inflicterObjectId, _lifeState.Value);
            OnDied?.Invoke(new BaseDamageReceiverEventArgs(GetServerCharacterForObjectId(inflicterObjectId)));
        }
        [Rpc(SendTo.Everyone)]
        private void NotifyOfDeathRpc()
        {
            OnLifeStateChanged?.Invoke(null, _lifeState.Value);
            OnDied?.Invoke(new BaseDamageReceiverEventArgs(null));
        }


        private void NotifyOfRevive(ServerCharacter inflicter)
        {
            if (inflicter == null)
                NotifyOfReviveRpc();
            else
                NotifyOfReviveRpc(inflicter.NetworkObjectId);
        }
        [Rpc(SendTo.Everyone)]
        private void NotifyOfReviveRpc(ulong inflicterObjectId)
        {
            OnLifeStateChanged?.Invoke(inflicterObjectId, _lifeState.Value);
            OnRevived?.Invoke(new BaseDamageReceiverEventArgs(GetServerCharacterForObjectId(inflicterObjectId)));
        }
        [Rpc(SendTo.Everyone)]
        private void NotifyOfReviveRpc()
        {
            OnLifeStateChanged?.Invoke(null, _lifeState.Value);
            OnRevived?.Invoke(new BaseDamageReceiverEventArgs(null));
        }

        #endregion


        private void Awake()
        {
            _serverCharacter = GetComponent<ServerCharacter>();
        }
        public override void OnNetworkSpawn()
        {
            _currentHealth.OnValueChanged += InvokeOnHealthChanged;
        }
        public override void OnNetworkDespawn()
        {
            _currentHealth.OnValueChanged -= InvokeOnHealthChanged;
        }
        private void InvokeOnHealthChanged(float prevVal, float newVal)
        {
            OnHealthChanged?.Invoke(prevVal, newVal);
            OnAnyHealthChange?.Invoke(new AnyHealthChangeEventArgs(null, _serverCharacter, newVal, MaxHealth, newVal - prevVal));
        }


        public void InitialiseDamageReceiver_Server(float maxHealth, float maxShields)
        {
            this.MaxHealth = maxHealth;
            this.MaxShields = maxShields;
            if (this._lifeState.Value == LifeState.Alive)
            {
                this._currentHealth.Value = maxHealth;
                this._currentShields.Value = maxShields;
            }
            //this._lifeState.Value = LifeState.Alive;

            NotifyOfInitialisationRpc(maxHealth, maxShields);
        }


        private void Update()
        {
            // Regenerate Shields.
            if (Time.time >= _lastDamageTime + _shieldRechargeDelay)
            {
                RegenerateShields(_shieldRechargeRate * Time.deltaTime);
            }
        }


        public void SetMaxHealth_Server(ServerCharacter inflicter, float newMaxHealth, bool increaseHealth = false)
        {
            float delta = newMaxHealth - Mathf.Max(MaxHealth, 0.0f);

            // Set our MaxHealth.
            MaxHealth = newMaxHealth;
            Debug.Log("Set Max Health");

            // Handle required changes to current health.
            if (delta > 0.0f)
            {
                // Max Health is being increased.
                if (increaseHealth)
                    SetCurrentHealth_Server(inflicter, _currentHealth.Value + delta);
                
                NotifyOfHealthChange(inflicter, 0.0f);
            }
            else
            {
                // Max Health is being decreased.
                if (MaxHealth < _currentHealth.Value)
                    SetCurrentHealth_Server(inflicter, _currentHealth.Value + delta);
                
                NotifyOfHealthChange(inflicter, 0.0f);
            }
        }
        public void SetCurrentHealth_Server(ServerCharacter inflicter, float newValue)
        {
            float clampedValue = Mathf.Clamp(newValue, 0, MaxHealth);
            float healthChange = clampedValue - _currentHealth.Value;
            _currentHealth.Value = clampedValue;


            // Notify whether we received healing or damage
            NotifyOfHealthChange(inflicter, healthChange);

            if (_currentHealth.Value <= 0.0f)
            {
                // We've died.
                SetLifeState_Server(inflicter, LifeState.Dead);
            }
        }


        public void SetMaxShields_Server(ServerCharacter inflicter, float newMaxShields, bool automaticallyRefill = false)
        {
            float delta = newMaxShields - Mathf.Max(MaxShields, 0.0f);

            MaxShields = newMaxShields;

            if (delta > 0.0f)
            {
                if (automaticallyRefill)
                    SetCurrentShields_Server(inflicter, _currentShields.Value + delta);
                else
                    NotifyOfShieldsChange(inflicter, 0.0f);
            }
            else
            {
                if (MaxShields < _currentShields.Value)
                    SetCurrentShields_Server(inflicter, MaxShields);
                else
                    NotifyOfShieldsChange(inflicter, 0.0f);
            }
        }
        public void SetCurrentShields_Server(ServerCharacter inflicter, float newValue)
        {
            float clampedValue = Mathf.Clamp(newValue, 0, MaxShields);
            float healthChange = clampedValue - _currentShields.Value;
            _currentShields.Value = clampedValue;

            NotifyOfShieldsChange(inflicter, 0.0f);
        }



        public void ReceiveDamage_Server(ServerCharacter inflicter, float damage, DamageTypes damageType, Vector3 damageSourceDirection)
        {
            if (!CanHaveHealthChanged())
                return; // This object cannot be damaged.
            if (damage == 0.0f)
                return; // The health change was invalid.
            if (!CanTakeDamage())
                return;

            _lastDamageTime = Time.time;
            IDamageable.InvokeOnAnyHealthChange(inflicter, -damage);
            
            Vector3 localDamageDirection = _serverCharacter.transform.InverseTransformDirection(damageSourceDirection);
            if (_currentShields.Value > 0.0f)
            {
                float shieldDamageMultiplier = _serverCharacter.CharacterStats.GetDamageTakenMultiplier(DamageTakenStatistic.RegeneratingShieldResistances, damageType, localDamageDirection);
                float effectiveShieldsHealth = _currentShields.Value / shieldDamageMultiplier;

                Debug.Log($"Shields Took '{Mathf.Min(damage * shieldDamageMultiplier, _currentShields.Value)}' damage (Actual: {Mathf.Min(damage, effectiveShieldsHealth)})");
                SetCurrentShields_Server(inflicter, _currentShields.Value - damage * shieldDamageMultiplier);
                
                damage -= effectiveShieldsHealth;
                if (damage <= 0.0f)
                    return; // All the damage has been dealt to shields.
            }

            damage *= _serverCharacter.CharacterStats.GetDamageTakenMultiplier(DamageTakenStatistic.DamageResistances, damageType, localDamageDirection);
            Debug.Log($"Health Took '{damage}' damage");

            SetCurrentHealth_Server(inflicter, _currentHealth.Value - damage);
        }
        public void ReceiveHealing_Server(ServerCharacter inflicter, float healing)
        {
            if (!CanHaveHealthChanged())
                return; // This object cannot be damaged.
            if (healing == 0.0f)
                return; // The health change was invalid.

            // Apply modifications to the healing/damage as appropriate.
            healing = CanReceiveHealing() ? healing : 0.0f;

            IDamageable.InvokeOnAnyHealthChange(inflicter, healing);
            SetCurrentHealth_Server(inflicter, _currentHealth.Value + healing);
        }
        private void RegenerateShields(float shieldIncreaseValue)
        {
            if (_currentShields.Value >= MaxShields)
                return;

            SetCurrentShields_Server(null, _currentShields.Value + shieldIncreaseValue);
        }


        public void Revive_Server(ServerCharacter inflicter)
        {
            _currentHealth.Value = MaxHealth;
            SetLifeState_Server(inflicter, LifeState.Alive);
        }
        public void SetLifeState_Server(ServerCharacter inflicter, LifeState newLifeState)
        {
            LifeState oldLifeState = _lifeState.Value;
            _lifeState.Value = newLifeState;
            Debug.Log($"Old: {oldLifeState} | New: {_lifeState.Value}");

            if (oldLifeState == LifeState.Alive && newLifeState == LifeState.Dead)
            {
                NotifyOfDeath(inflicter);
            }
            else if (oldLifeState == LifeState.Dead && newLifeState == LifeState.Alive)
            {
                NotifyOfRevive(inflicter);
            }
        }


        public float GetCurrentHealth() => Mathf.Min(_currentHealth.Value, MaxHealth);
        public float GetMissingHealth() => MaxHealth - GetCurrentHealth();

        public float GetCurrentShields() => Mathf.Min(_currentShields.Value, 0.0f);

        public float GetHealthPercentage() => GetCurrentHealth() / MaxHealth;
        public float GetShieldsPercentage() => MaxShields != 0.0f ? GetCurrentShields() / MaxShields : 0.0f;



        public bool CanHaveHealthChanged() => !IsDead;
        public bool CanTakeDamage() => !IsDead;
        public bool CanReceiveHealing() => !IsDead;



        [ContextMenu("Take Fifteen Damage")]
        private void TakeFifteenDamage() => ReceiveDamage_Server(null, 15.0f, DamageTypes.Ballistic, Vector3.zero);


        private ServerCharacter GetServerCharacterForObjectId(ulong objectId)
        {
            if (NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(objectId, out NetworkObject networkObject))
            {
                if (networkObject.TryGetComponent<ServerCharacter>(out ServerCharacter serverCharacter))
                    return serverCharacter;
                else
                    throw new System.Exception($"Invalid Object ID ({objectId}): No corresponding ServerCharacter on NetworkObject");
            }
            else
                throw new System.Exception($"Invalid Object ID ({objectId}): No corresponding NetworkObject");
        }


        private void OnCollisionEnter(Collision collision)
        {
            CollisionEntered?.Invoke(collision);
        }


        #region Event Args Definitions

        public class BaseDamageReceiverEventArgs : System.EventArgs
        {
            public ServerCharacter Inflicter;


            private BaseDamageReceiverEventArgs() { }
            public BaseDamageReceiverEventArgs(ServerCharacter inflicter)
            {
                this.Inflicter = inflicter;
            }
        }
        public class AnyHealthChangeEventArgs : BaseDamageReceiverEventArgs
        {
            public ServerCharacter ThisCharacter { get; }

            public float NewCurrentHealth { get; }
            public float NewMaxHealth { get; }
            public float HealthChange { get; }

            public AnyHealthChangeEventArgs(ServerCharacter inflicter, ServerCharacter thisCharacter, float newCurrent, float newMaxHealth, float healthChange) : base(inflicter)
            {
                this.ThisCharacter = thisCharacter;
                this.NewCurrentHealth = newCurrent;
                this.NewMaxHealth = newMaxHealth;
                this.HealthChange = healthChange;
            }
        }
        public class AnyShieldsChangeEventArgs : BaseDamageReceiverEventArgs
        {
            public ServerCharacter ThisCharacter { get; }

            public float NewCurrentShields { get; }
            public float NewMaxShields { get; }
            public float ShieldsChange { get; }

            public AnyShieldsChangeEventArgs(ServerCharacter inflicter, ServerCharacter thisCharacter, float newCurrent, float newMaxShields, float shieldsChange) : base(inflicter)
            {
                this.ThisCharacter = thisCharacter;
                this.NewCurrentShields = newCurrent;
                this.NewMaxShields = newMaxShields;
                this.ShieldsChange = shieldsChange;
            }
        }
        public class HealthChangeEventArgs : BaseDamageReceiverEventArgs
        {
            public float HealthChange;

            public HealthChangeEventArgs(ServerCharacter inflicter, float healthChange) : base(inflicter)
            {
                this.HealthChange = healthChange;
            }
        }

        #endregion
    }
}