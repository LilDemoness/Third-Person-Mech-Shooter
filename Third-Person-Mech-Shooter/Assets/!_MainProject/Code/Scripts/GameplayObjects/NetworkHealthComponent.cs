using Gameplay.GameplayObjects.Character;
using Unity.Collections.LowLevel.Unsafe;
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

        private NetworkVariable<LifeState> _lifeState { get; } = new NetworkVariable<LifeState>(LifeState.Alive);


        public bool IsDead => _lifeState.Value != Character.LifeState.Alive;


        #region Events

        public event System.Action OnInitialised;
        public bool IsInitialised = false;

        public static event System.Action<AnyHealthChangeEventArgs> OnAnyHealthChange;
        public event System.Action<HealthChangeEventArgs> OnDamageReceived;
        public event System.Action<HealthChangeEventArgs> OnHealingReceived;
        public event System.Action<float, float> OnHealthChanged;

        public event System.Action<BaseDamageReceiverEventArgs> OnDied;
        public event System.Action<BaseDamageReceiverEventArgs> OnRevived;
        public event System.Action<ulong?, LifeState> OnLifeStateChanged;

        #endregion

        #region Event RPCs

        [Rpc(SendTo.Everyone)]
        private void NotifyOfInitialisationRpc(float maxHealth)
        {
            if (!IsServer)
                this.MaxHealth = maxHealth;

            IsInitialised = true;
            OnInitialised?.Invoke();
            Debug.Log($"Notify Health Change ({_currentHealth.Value}|{maxHealth}) for {this.OwnerClientId}");
            OnAnyHealthChange?.Invoke(new AnyHealthChangeEventArgs(null, _serverCharacter, _currentHealth.Value, maxHealth, 0.0f));
        }


        private void NotifyOfHealthChange(ServerCharacter inflicter, float newHealth, float newMaxHealth, float healthChange)
        {
            if (inflicter == null)
                NotifyOfHealthChangeRpc(newHealth, newMaxHealth, healthChange);
            else
                NotifyOfHealthChangeRpc(inflicter.NetworkObjectId, newHealth, newMaxHealth, healthChange);
        }
        [Rpc(SendTo.Everyone)]
        private void NotifyOfHealthChangeRpc(float newHealth, float newMaxHealth, float healthChange) => OnAnyHealthChange?.Invoke(new AnyHealthChangeEventArgs(null, _serverCharacter, newHealth, newMaxHealth, healthChange));
        [Rpc(SendTo.Everyone)]
        private void NotifyOfHealthChangeRpc(ulong inflicterObjectId, float newHealth, float newMaxHealth, float healthChange) => OnAnyHealthChange?.Invoke(new AnyHealthChangeEventArgs(GetServerCharacterForObjectId(inflicterObjectId), _serverCharacter, newHealth, newMaxHealth, healthChange));


        private void NotifyOfDamage(ServerCharacter inflicter, float healthChange)
        {
            if (inflicter == null)
                NotifyOfDamageRpc(healthChange);
            else
                NotifyOfDamageRpc(inflicter.NetworkObjectId, healthChange);
        }
        [Rpc(SendTo.Everyone)]
        private void NotifyOfDamageRpc(float healthChange) => OnDamageReceived?.Invoke(new HealthChangeEventArgs(null, healthChange));
        [Rpc(SendTo.Everyone)]
        private void NotifyOfDamageRpc(ulong inflicterObjectId, float healthChange) => OnDamageReceived?.Invoke(new HealthChangeEventArgs(GetServerCharacterForObjectId(inflicterObjectId), healthChange));


        private void NotifyOfHealing(ServerCharacter inflicter, float healthChange)
        {
            if (inflicter == null)
                NotifyOfHealingRpc(healthChange);
            else
                NotifyOfHealingRpc(inflicter.NetworkObjectId, healthChange);
        }
        [Rpc(SendTo.Everyone)]
        private void NotifyOfHealingRpc(float healthChange) => OnHealingReceived?.Invoke(new HealthChangeEventArgs(null, healthChange));
        [Rpc(SendTo.Everyone)]
        private void NotifyOfHealingRpc(ulong inflicterObjectId, float healthChange) => OnHealingReceived?.Invoke(new HealthChangeEventArgs(GetServerCharacterForObjectId(inflicterObjectId), healthChange));


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


        public void InitialiseDamageReceiver_Server(float maxHealth)
        {
            this.MaxHealth = maxHealth;
            if (this._lifeState.Value == LifeState.Alive)
                this._currentHealth.Value = maxHealth;
            //this._lifeState.Value = LifeState.Alive;

            NotifyOfInitialisationRpc(maxHealth);
        }

        public void SetMaxHealth_Server(ServerCharacter inflicter, int newMaxHealth, bool increaseHealth = false, bool excessHealthBecomesOverhealth = false)
        {
            float delta = newMaxHealth - Mathf.Max(MaxHealth, 0);

            // Set our MaxHealth.
            MaxHealth = newMaxHealth;
            Debug.Log("Set Max Health");

            // Handle required changes to current health.
            if (delta > 0.0f)
            {
                // Max Health is being increased.
                if (increaseHealth)
                    SetCurrentHealth_Server(inflicter, _currentHealth.Value + delta);
                else
                    NotifyOfHealthChange(inflicter, _currentHealth.Value, MaxHealth, 0.0f);
            }
            else
            {
                // Max Health is being decreased.
                if (MaxHealth < _currentHealth.Value)
                {
                    if (excessHealthBecomesOverhealth)
                        throw new System.NotImplementedException("Overhealth");
                    else
                        SetCurrentHealth_Server(inflicter, _currentHealth.Value + delta);
                }
                else
                    NotifyOfHealthChange(inflicter, _currentHealth.Value, MaxHealth, 0.0f);
            }
        }


        public void ReceiveHealthChange_Server(ServerCharacter inflicter, float healthChange)
        {
            if (!CanHaveHealthChanged())
                return; // This object cannot be damaged.
            if (healthChange == 0.0f)
                return; // The health change was invalid.

            // Apply modifications to the healing/damage as appropriate.
            bool isHeal = healthChange > 0.0f;
            if (isHeal)
            {
                healthChange = CanReceiveHealing() ? ApplyHealingModifications(healthChange) : 0.0f;
            }
            else
            {
                healthChange = CanTakeDamage() ? ApplyDamageModifications(healthChange) : 0.0f;
            }


            SetCurrentHealth_Server(inflicter, _currentHealth.Value + healthChange);
        }
        public void SetCurrentHealth_Server(ServerCharacter inflicter, float newValue, bool excessBecomesOverhealth = false)
        {
            if (excessBecomesOverhealth)
                throw new System.NotImplementedException();

            float healthChange = newValue - _currentHealth.Value;
            _currentHealth.Value = Mathf.Clamp(newValue, 0, MaxHealth);


            // Notify whether we received healing or damage
            NotifyOfHealthChange(inflicter, newValue, MaxHealth, healthChange);
            if (healthChange > 0.0f)
                NotifyOfHealing(inflicter, healthChange);
            else
                NotifyOfDamage(inflicter, healthChange);


            if (_currentHealth.Value <= 0.0f)
            {
                // We've died.
                SetLifeState_Server(inflicter, LifeState.Dead);
            }
        }


        private float ApplyHealingModifications(float unmodifiedValue)
        {
            return unmodifiedValue;
        }
        private float ApplyDamageModifications(float unmodifiedValue)
        {
            return unmodifiedValue;
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


        public float GetMissingHealth() => MaxHealth - _currentHealth.Value;
        public float GetCurrentHealth() => _currentHealth.Value;


        public bool CanHaveHealthChanged() => !IsDead;
        public bool CanTakeDamage() => !IsDead;
        public bool CanReceiveHealing() => !IsDead;



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