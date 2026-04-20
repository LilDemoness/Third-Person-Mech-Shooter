using UnityEngine;
using Unity.Netcode;
using Gameplay.Actions;
using Gameplay.GameplayObjects.Character.Customisation.Data;
using Gameplay.StatusEffects;
using Utils;
using System.Collections.Generic;
using Gameplay.GameplayObjects.Character.Statistics;
using Gameplay.Passives;
using Gameplay.GameplayObjects.Character.Customisation.Sections;

namespace Gameplay.GameplayObjects.Character
{
    /// <summary>
    ///     Contains all NetworkVariables, RPCs, and Server-Side Logic of a Character.
    ///     Separated from the Client Logic so that it is always known whether a section of code is running on the server or the client.
    /// </summary>
    public class ServerCharacter : NetworkBehaviour, IActionSource
    {


        [SerializeField] private ClientCharacter m_clientCharacter;
        public ClientCharacter ClientCharacter => m_clientCharacter;


        [SerializeField] private NetworkNameState m_networkNameState;
        [SerializeField] private string _characterName;
        public string CharacterName => m_networkNameState ? m_networkNameState.Name.Value : _characterName;
        public FixedPlayerName FixedCharacterName => m_networkNameState ? m_networkNameState.Name.Value : new FixedPlayerName(_characterName);


        [SerializeField, ReadOnly] private BuildData _buildDataReference;
        /// <summary>
        ///     Property for this ServerCharacter's BuildData.<br/>
        ///     Must be populated through both Clients and Servers.
        /// </summary>
        public BuildData BuildDataReference
        {
            get => _buildDataReference;
            set
            {
                _buildDataReference = value;

                if (IsServer)
                    ServerCharacter_OnBuildDataChanged_Server(_buildDataReference);
                ServerCharacter_OnBuildDataChanged(_buildDataReference);
            }
        }
        public void SetBuildDataReference(BuildData buildData)
        {

        }


        /// <summary>
        ///     Indicates how the character's movement should be depicted.
        /// </summary>
        public NetworkVariable<MovementStatus> MovementStatus { get; } = new NetworkVariable<MovementStatus>();

        /// <summary>
        ///     Indicates whether this character is in "stealth" (Invisible to AI agents and other players).
        /// </summary>
        public NetworkVariable<bool> IsInStealth { get; } = new NetworkVariable<bool>();
        /// <summary>
        ///     Indicates whether this character is "intangible" (Cannot be affected by AI agents and other players that are not also intangible).
        /// </summary>
        public NetworkVariable<bool> IsIntangible { get; } = new NetworkVariable<bool>();


        [SerializeField] private NetworkHealthComponent _networkHealthComponent;
        public NetworkHealthComponent NetworkHealthComponent => _networkHealthComponent;


        // Heat.

        public NetworkVariable<float> CurrentHeat { get; private set; } = new NetworkVariable<float>();
        public float MaxHeat => _characterStats.GetStatisticValue(Statistic.MaxHeat);
        private float _lastHeatIncreaseTime = 0.0f;

        public event System.Action<float, float> OnHeatChanged; // Current, Max.

        [SerializeField] private Gameplay.StatusEffects.Definitions.Overheating _overheatingEffectDefinition;


        // Movement.
        public float MovementSpeed => Movement.MovementSpeed;


        // Core System Power.
        public float MaxCoreSystemCharge => BuildDataReference.GetCoreSystemData().CoreSystemCost;
        [SerializeField, ReadOnly] private float _coreSystemCharge;
        private bool _coreSystemInUse = false;

        public float CoreSystemCharge => _coreSystemCharge;
        public float CoreSystemChargePercentage => _coreSystemCharge / MaxCoreSystemCharge;


        // References.

        /// <summary>
        ///     This Character's ActionPlayer, exposed for use by ActionEffects.
        /// </summary>
        public ServerActionPlayer ActionPlayer => m_serverActionPlayer;
        private ServerActionPlayer m_serverActionPlayer;
        public bool CanPerformActionInstantly = true;

        public bool CanPerformActions => !_networkHealthComponent.IsDead;


        /// <summary>
        ///     The character's StatusEffectPlayer, exposed for use by ActionEffects.
        /// </summary>
        public ServerStatusEffectPlayer StatusEffectPlayer => m_statusEffectPlayer;
        private ServerStatusEffectPlayer m_statusEffectPlayer;

        public ServerPassivePlayer ServerPassivePlayer => m_serverPassiveManager;
        private ServerPassivePlayer m_serverPassiveManager;


        [SerializeField] private ServerCharacterMovement _movement; 
        public ServerCharacterMovement Movement => _movement;


        [SerializeField] private CharacterStats _characterStats; 
        public CharacterStats CharacterStats => _characterStats;


        public NetworkVariable<int> TeamID { get; set; } = new NetworkVariable<int>(-1);

        [SerializeField] private GameObject _gfxRoot;
        [SerializeField] private VisualEffects.SpecialFXGraphic _deathExplosionEffectPrefab;


        [Header("GFX References")]
        [SerializeField] private FrameGFX[] _characterFrames;
        private FrameGFXWrapper[] _frameGFXWrappers;
        private FrameGFX _activeFrame;


        private void Awake()
        {
            m_serverActionPlayer = new ServerActionPlayer(this);
            m_statusEffectPlayer = new ServerStatusEffectPlayer(this);
            m_serverPassiveManager = new ServerPassivePlayer(this);


            // Setup our FrameGFX Wrappers for simpler retrieving later.
            _frameGFXWrappers = new FrameGFXWrapper[_characterFrames.Length];
            for (int i = 0; i < _characterFrames.Length; ++i)
            {
                _frameGFXWrappers[i] = new FrameGFXWrapper(_characterFrames[i]);
            }


            _movement.OnMovementStatusChanged += MovementScript_OnMovementStatusChanged;
            _characterStats.OnStatisticChanged += CharacterStats_OnStatisticChanged;


            IDamageable.OnAnyHealthChange += IDamageable_OnAnyHealthChange;
        }
        public override void OnDestroy()
        {
            base.OnDestroy();

            _movement.OnMovementStatusChanged -= MovementScript_OnMovementStatusChanged;
            _characterStats.OnStatisticChanged -= CharacterStats_OnStatisticChanged;


            IDamageable.OnAnyHealthChange -= IDamageable_OnAnyHealthChange;
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                this.enabled = false;
                return;
            }
            
            CurrentHeat.OnValueChanged += OnCurrentHeatChanged;
            NetworkHealthComponent.OnDied += OnCharacterDied;
            NetworkHealthComponent.CollisionEntered += CollisionEntered;
            ActionPlayer.OnActionQueueFilled += ServerActionPlayer_OnActionQueueFilled;
            ActionPlayer.OnActionQueueEmptied += ServerActionPlayer_OnActionQueueEmptied;
        }
        public override void OnNetworkDespawn()
        {
            // Unsubscribe from NetworkVariable Events.
            CurrentHeat.OnValueChanged -= OnCurrentHeatChanged;
            NetworkHealthComponent.OnDied -= OnCharacterDied;
            NetworkHealthComponent.CollisionEntered -= CollisionEntered;
            ActionPlayer.OnActionQueueFilled -= ServerActionPlayer_OnActionQueueFilled;
            ActionPlayer.OnActionQueueEmptied -= ServerActionPlayer_OnActionQueueEmptied;
        }


        private void IDamageable_OnAnyHealthChange(ServerCharacter inflicter, float healthChange)
        {
            if (inflicter != this)
                return; // Not this character.

            // Increase our core system charge proportional to the health change.
            if (healthChange > 0.0f)
                _coreSystemCharge = Mathf.Min(_coreSystemCharge + healthChange * CoreSystemData.HEALING_TO_CHARGE_MULTIPLIER, MaxCoreSystemCharge);
            else
                _coreSystemCharge = Mathf.Min(_coreSystemCharge + (-healthChange) * CoreSystemData.DAMAGE_TO_CHARGE_MULTIPLIER, MaxCoreSystemCharge);
        }


        private void MovementScript_OnMovementStatusChanged(MovementStatus newState) => MovementStatus.Value = newState;
        private void CharacterStats_OnStatisticChanged(Statistic statistic)
        {
            if (IsServer && statistic == Statistic.MaxHealth)
                NetworkHealthComponent.SetMaxHealth_Server(null, Mathf.CeilToInt(_characterStats.GetStatisticValue(Statistic.MaxHealth)), true);

            if (statistic == Statistic.MaxHeat)
                NotifyOfHeatChange();
        }


        /// <summary>
        ///     ServerRPC to send movement input for this character.
        /// </summary>
        /// <param name="movementInput"> The character's movement input</param>
        [ServerRpc]
        public void SendCharacterMovementInputServerRpc(Vector2 movementInput)
        {
            // Check that we're not dead or currently experiencing forced movement (E.g. Knockback/Charge).
            if (!CanPerformActions)// || _movement.IsPerformingForcedMovement())
                return;

            // Check if our current action prevents movement.
            if (ActionPlayer.GetActiveActionInfo(out ActionRequestData data))
            {
                if (data.PreventMovement)
                    return;
            }

            // We can move.

            _movement.SetMovementInput(movementInput);
        }
        [Rpc(SendTo.Server)]
        public void SendCharacterBoostRequestServerRpc()
        {
            // Check that we're not dead or currently experiencing forced movement (E.g. Knockback/Charge).
            if (!CanPerformActions)// || _movement.IsPerformingForcedMovement())
                return;

            // Check if our current action prevents movement.
            if (ActionPlayer.GetActiveActionInfo(out ActionRequestData data))
            {
                if (data.PreventMovement)
                    return;
            }

            // We can boost.
            _movement.PerformBoost();
        }

        /// <summary>
        ///     ServerRPC to cancel the actions with the given ActionID.
        /// </summary>
        /// <param name="actionID"> The ActionID of the actions we wish to cancel.</param>
        /// <param name="slotIndex"> The AttachmentSlot that the cancelled actions should be in.</param>
        [Rpc(SendTo.Server)]
        public void CancelActionByIDServerRpc(ActionID actionID) => CancelAction_Server(actionID);

        /// <summary>
        ///     ServerRPC to cancel all actions in the Attachment Slot <paramref name="slotIndex"/>.
        /// </summary>
        [Rpc(SendTo.Server)]
        public void CancelActionBySlotServerRpc(AttachmentSlotIndex slotIndex) => CancelAction_Server(slotIndex);
        /// <summary>
        ///     ServerRPC to cancel all actions with the given <paramref name="actionID"/> in the Attachment Slot <paramref name="slotIndex"/>.
        /// </summary>
        [Rpc(SendTo.Server)]
        public void CancelActionBySlotServerRpc(AttachmentSlotIndex slotIndex, ActionID actionID) => CancelAction_Server(slotIndex, actionID);


        /// <summary>
        ///     Play a desired action from the given <see cref="ActionRequestData"/> '<paramref name="data"/>'.
        /// </summary>
        public void PlayAction_Server(ref ActionRequestData data, System.Action<Action> onActionCompleteCallback = null)
        {
            if (!CanPerformActions)
                return;

            if (GameDataSource.Instance.GetActionDefinitionByID(data.ActionID).IsHostileAction)
            {
                // Notify our running actions that we're using a new hostile action.
                // Called so that things like Stealth can end themselves.
                ActionPlayer.OnGameplayActivity(Action.GameplayActivity.UsingHostileAction);
            }

            // Perform the action.

            if (data.PreventMovement)
                _movement.CancelMove();

            ActionPlayer.PlayAction(ref data, onActionCompleteCallback);
        }
        /// <summary>
        ///     Cancel the actions with the given ActionID.
        /// </summary>
        /// <remarks> Called on the Server.</remarks>
        public bool CancelAction_Server(ActionID actionID, bool cancelNonBlocking = true, Action exceptThis = null, bool forceCancel = false)
        {
            if (GameDataSource.Instance.GetActionDefinitionByID(actionID).ShouldNotifyClient)
                m_clientCharacter.CancelRunningActionsByIDClientRpc(actionID);

            return ActionPlayer.CancelRunningActionsByID(actionID, cancelNonBlocking, exceptThis, forceCancel);
        }
        /// <summary>
        ///     Cancel all actions in a given Attachment Slot.
        /// </summary>
        /// <remarks> Called on the Server.</remarks>
        public bool CancelAction_Server(AttachmentSlotIndex slotIndex, bool cancelNonBlocking = true, bool forceCancel = false)
        {
            m_clientCharacter.CancelRunningActionsBySlotIDClientRpc(slotIndex);
            return ActionPlayer.CancelRunningActionsBySlotID(slotIndex, cancelNonBlocking, forceCancel);
        }
        /// <summary>
        ///     Cancel all actions in a given Attachment Slot.
        /// </summary>
        /// <remarks> Called on the Server.</remarks>
        public bool CancelAction_Server(AttachmentSlotIndex slotIndex, ActionID actionID, bool cancelNonBlocking = true, bool forceCancel = false)
        {
            if (GameDataSource.Instance.GetActionDefinitionByID(actionID).ShouldNotifyClient)
                m_clientCharacter.CancelRunningActionsBySlotIDClientRpc(slotIndex, actionID);

            return ActionPlayer.CancelRunningActionsBySlotID(slotIndex, actionID, cancelNonBlocking, forceCancel);
        }

        private void ServerActionPlayer_OnActionQueueFilled() => UpdateInstantActionAvailabilityOwnerRpc(false);
        private void ServerActionPlayer_OnActionQueueEmptied() => UpdateInstantActionAvailabilityOwnerRpc(true);
        [Rpc(SendTo.Owner)]
        private void UpdateInstantActionAvailabilityOwnerRpc(bool canPerformInstantAction) => CanPerformActionInstantly = canPerformInstantAction;



        private void Update()
        {
            ActionPlayer.OnUpdate();
            StatusEffectPlayer.OnUpdate();
            ServerPassivePlayer.OnUpdate(Time.deltaTime);

            float heatDecreaseDelay = 1.0f;
            float heatDecreaseRate = 1.0f;
            if (NetworkManager.ServerTime.TimeAsFloat >= (_lastHeatIncreaseTime + heatDecreaseDelay))
            {
                ReceiveHeatChange(this, -heatDecreaseRate * Time.deltaTime);
            }

            HandleCoreSystemCharge();
        }


        #region Heat

        /// <summary>
        ///     Initialise our Heat.
        /// </summary>
        private void InitialiseHeat()
        {
            CurrentHeat.Value = 0.0f;
        }


        /// <summary>
        ///     Apply a heat change to the ServerCharacter.
        /// </summary>
        /// <param name="heatChange"> The heat to be applied (Negative values reduce heat).</param>
        public void ReceiveHeatChange(ServerCharacter inflicter, float heatChange)
        {
            if (heatChange > 0.0f)
                heatChange *= CharacterStats.GetStatisticValue(Statistic.PersonalHeatGainMultiplier);

            SetCurrentHeat(CurrentHeat.Value + heatChange);
        }
        
        /// <summary>
        ///     Set the value of CurrentHeat, clamping if below 0 or above <see cref="MaxHeat"/>.</br>
        ///     Heat Cap Exceeding is handled separately via the OnValueChanged event on the server.
        /// </summary>
        private void SetCurrentHeat(float newValue)
        {
            if (CurrentHeat.Value < newValue)
            {
                // Our heat is increasing. Cache this value so we know when we can next start cooling down.
                _lastHeatIncreaseTime = NetworkManager.ServerTime.TimeAsFloat;
            }

            CurrentHeat.Value = Mathf.Clamp(newValue, 0, MaxHeat);
        }


        private void OnCurrentHeatChanged(float previousHeat, float newHeat)
        {
            CheckIfExceededHeatCap(previousHeat, newHeat);
            NotifyOfHeatChange();
        }
        private void NotifyOfHeatChange() => OnHeatChanged?.Invoke(CurrentHeat.Value, MaxHeat);


        private void CheckIfExceededHeatCap(float previousHeat, float newHeat)
        {
            if (newHeat >= MaxHeat && previousHeat < MaxHeat)
            {
                // Just Exceeded Heat Cap.
                StatusEffectPlayer.AddStatusEffect(_overheatingEffectDefinition);
            }
            else if (newHeat < MaxHeat && previousHeat >= MaxHeat)
            {
                // Just went under our Heat Cap.
                StatusEffectPlayer.ClearAllEffectsOfType(_overheatingEffectDefinition);
            }
        }

        #endregion


        #region Build

        //private void NetworkedBuildDataChanged(BuildDataState oldValue, BuildDataState newValue)
        //{
        //    OnBuildDataChanged?.Invoke(new BuildDataReference(newValue));
        //}
        [Rpc(SendTo.ClientsAndHost)]
        public void UpdateBuildStateClientRpc(int activeFrame, int[] activeSlottables)
        {
            this.BuildDataReference = new BuildData(activeFrame, activeSlottables);
        }

        /// <summary>
        ///     Server-only Logic to be performed when a ServerCharacter's build changes.
        /// </summary>
        /// <param name="buildData"></param>
        private void ServerCharacter_OnBuildDataChanged_Server(BuildData buildData)
        {
            ServerPassivePlayer.ClearAllPassives();
            ServerPassivePlayer.AddPassive(_buildDataReference.GetFrameData().CoreSystem.PassiveFeatureDefinition);

            //_networkHealthComponent.InitialiseDamageReceiver_Server(buildData.GetFrameData().MaxHealth);
            _networkHealthComponent.InitialiseDamageReceiver_Server(_characterStats.GetStatisticValue(Statistic.MaxHealth), _characterStats.GetStatisticValue(Statistic.MaxShields));
            InitialiseHeat();
        }
        /// <summary>
        ///     Logic to be performed on all instances when a ServerCharacter's build changes.
        /// </summary>
        /// <param name="buildData"></param>
        private void ServerCharacter_OnBuildDataChanged(BuildData buildData)
        {
            // Toggle GFX.
            bool hasFoundActiveFrame = false;
            for (int i = 0; i < _frameGFXWrappers.Length; ++i)
            {
                if (!hasFoundActiveFrame)
                {
                    // We haven't yet found our active frame. Perform a full check toggle (Also sets our ActiveGFX references if we find our active frame).
                    if (_frameGFXWrappers[i].Toggle(buildData, ref _activeFrame))
                    {
                        // This is our active frame.
                        hasFoundActiveFrame = true; // All other frames should be disabled without a toggle check.
                    }
                }
                else
                {
                    // We will only ever have 1 active frame, and we have already found it. Disable all other frames.
                    _frameGFXWrappers[i].Disable();
                }
            }
        }



        public ModuleData GetModuleDataForSlotIndex(AttachmentSlotIndex slotIndex) => BuildDataReference.GetSlottableData(slotIndex);
        public ulong GetSourceObjectIDForSlotIndex(AttachmentSlotIndex slotIndex)
        {
            ulong id = _activeFrame.GetObjectIDForSlotIndex(slotIndex);
            return id != 0 ? id : this.NetworkObjectId;
        }

        public CoreSystemData GetCoreSystemData() => BuildDataReference.GetCoreSystemData();
        public ulong GetSourceObjectIDForCoreSystem()
        {
            CoreSystemGFXSection coreSystemGFX = _activeFrame.GetCoreSystemSlot().GetActiveGFXSection();

            return coreSystemGFX != null ? coreSystemGFX.GetAbilitySourceObjectId() : this.NetworkObjectId;
        }

        #endregion


        #region GFX

        /// <summary>
        ///     Returns the currently active FrameGFX instance.
        /// </summary>
        public FrameGFX GetActiveFrame() => _activeFrame;


        // Note: May return null.
        public SlotGFXSection GetSlotGFXForIndex(AttachmentSlotIndex index) => _activeFrame.GetSlotGFXForIndex(index);

        public Transform GetOriginTransform(TransformRelation transformRelation)
        {
            return transformRelation switch
            {
                TransformRelation.PrimaryModuleSlot     => _activeFrame.GetSlotGFXForIndex(AttachmentSlotIndex.Primary)?.GetAbilityOriginTransform(),
                TransformRelation.SecondaryModuleSlot   => _activeFrame.GetSlotGFXForIndex(AttachmentSlotIndex.Secondary)?.GetAbilityOriginTransform(),
                TransformRelation.TertiaryModuleSlot    => _activeFrame.GetSlotGFXForIndex(AttachmentSlotIndex.Tertiary)?.GetAbilityOriginTransform(),
                TransformRelation.QuaternaryModuleSlot  => _activeFrame.GetSlotGFXForIndex(AttachmentSlotIndex.Quaternary)?.GetAbilityOriginTransform(),

                TransformRelation.CoreSystem => _activeFrame.GetCoreSystemSlot().GetActiveGFXSection()?.GetAbilityOriginTransform(),

                _ => null
            } ?? Movement.RotationPivot;    // Default to the rotation pivot.
        }


        struct FrameGFXWrapper
        {
            FrameGFX _frameGFX;

            public FrameGFXWrapper(FrameGFX frameGFX)
            {
                this._frameGFX = frameGFX;
            }


            public bool Toggle(BuildData buildData, ref FrameGFX activeFrameGFX)
            {
                Debug.Log("Test: " + _frameGFX.name);
                if (_frameGFX.Toggle(buildData.GetFrameData()) == false)
                {
                    // This wrapper's frame isn't the correct frame for this build.
                    return false;
                }

                activeFrameGFX = this._frameGFX;

                // This wrapper's frame is the desired one.
                // Update slottables gfx.
                for (int i = 0; i < buildData.ActiveSlottableIndicies.Length; ++i)
                {
                    if (_frameGFX.TryGetAttachmentSlotForIndex(i.ToSlotIndex(), out AttachmentSlot attachmentSlot) == false)
                        break;   // No AttachmentSlot for this index, and we'll subsequently not have any of a higher index.

                    if (!attachmentSlot.Toggle(buildData.GetSlottableData(i.ToSlotIndex())))
                        throw new System.Exception($"No valid Slottable GFX Instances within '{attachmentSlot.name}' for '{buildData.GetSlottableData(i.ToSlotIndex()).name}'");
                }

                _frameGFX.GetCoreSystemSlot().Toggle(buildData.GetCoreSystemData());

                return true;
            }
            public void Disable() => _frameGFX.Toggle(null);
        }

        #endregion


        #region Death & Respawning

        public void OnCharacterDied(NetworkHealthComponent.BaseDamageReceiverEventArgs _) => OnCharacterDied();
        public void OnCharacterDied()
        {
            // Spawn Death Model.

            // Play Death Effects.
            PlayDeathEffectsClientRpc();
        }

        // Server-only.
        public void RespawnCharacter(Vector3 spawnPosition, Quaternion spawnRotation)
        {
            // Remove the Death Model (Or have it handle that itself).

            // Show GFX.
            ShowCharacterGFXClientRpc();

            // Perpetuate Required Revive Changes.
            NetworkHealthComponent.Revive_Server(null); // Revive the character from the server.
            Movement.SetPositionAndRotation(spawnPosition, spawnRotation);
        }


        [Rpc(SendTo.ClientsAndHost)]
        private void ShowCharacterGFXClientRpc() => _gfxRoot.SetActive(true);
        [Rpc(SendTo.ClientsAndHost)]
        private void PlayDeathEffectsClientRpc()
        {
            // Death Explosion.
            VisualEffects.SpecialFXGraphic deathExplosion = VisualEffects.SpecialFXPoolManager.GetFromPrefab(_deathExplosionEffectPrefab);
            deathExplosion.OnShutdownComplete += EffectShutdownComplete;
            deathExplosion.transform.SetPositionAndRotation(transform.position, transform.rotation);
            deathExplosion.Play();

            // Hide GFX.
            _gfxRoot.SetActive(false);

            void EffectShutdownComplete(VisualEffects.SpecialFXGraphic instance)
            {
                instance.OnShutdownComplete -= EffectShutdownComplete;
                VisualEffects.SpecialFXPoolManager.ReturnFromPrefab(_deathExplosionEffectPrefab, instance);
            }
        }

        #endregion


        private void CollisionEntered(Collision collision)
        {
            if (ActionPlayer != null)
                ActionPlayer.CollisionEntered(collision);
        }


        #region Core System

        public void StartCoreSystemUse(ref ActionRequestData data)
        {
            _coreSystemInUse = true;
            PlayAction_Server(ref data, OnCoreSystemEnded);
        }
        public void CancelCoreSystemUse()
        {
            if (!_coreSystemInUse)
                return;

            CancelAction_Server(BuildDataReference.GetCoreSystemData().ActiveActionDefinition.ActionID);
            OnCoreSystemEnded();
        }
        private void OnCoreSystemEnded(Action _) => OnCoreSystemEnded();
        private void OnCoreSystemEnded()
        {
            _coreSystemInUse = false;

            if (BuildDataReference.GetCoreSystemData().FullyDrainOnEnd)
                _coreSystemCharge = 0.0f;
        }

        private void HandleCoreSystemCharge()
        {
            if (!_coreSystemInUse)
                _coreSystemCharge = Mathf.Min(_coreSystemCharge + CoreSystemData.TIME_TO_CHARGE_MULTIPLIER * Time.deltaTime, MaxCoreSystemCharge);
            else
            {
                _coreSystemCharge -= BuildDataReference.GetCoreSystemData().PowerPercentageDrainRate * Time.deltaTime;

                if (_coreSystemCharge <= 0.0f)
                {
                    _coreSystemCharge = 0.0f;
                    CancelCoreSystemUse();
                }
            }
        }

#endregion


        #region Editor Testing Functions
#if UNITY_EDITOR

        [ContextMenu("Kill Character")]
        private void Editor_KillCharacter()
        {
            _networkHealthComponent.SetLifeState_Server(this, LifeState.Dead);
        }

#endif
        #endregion
    }


    public enum LifeState
    {
        Alive = 0,
        Dead = 1,
    }
    public class CharacterDeadEventArgs : System.EventArgs
    {
        public ServerCharacter Character;
        public ServerCharacter Inflicter;

        public CharacterDeadEventArgs(ServerCharacter character, ServerCharacter inflicter)
        {
            this.Character = character;
            this.Inflicter = inflicter;
        }
    }
}