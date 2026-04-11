using UnityEngine;
using Unity.Netcode;
using Gameplay.GameplayObjects.Character;
using Gameplay.Actions.Effects;
using Gameplay.Actions.Visuals;
using UI.Crosshairs;

namespace Gameplay.Actions.Definitions
{
    /// <summary>
    ///     The shared (Non-instanced) definition for an Action, including all its data.<br/>
    ///     Shouldn't contain any instance information.
    /// </summary>
    /// <remarks>
    ///     Children of this class specify the targeting information of the action.
    /// </remarks>
    public abstract class ActionDefinition : ScriptableObject
    {
        /// <summary>
        ///     An index into the GameDataSource array of action prototypes.
        ///     Set at runtime by GameDataSource class.
        ///     If the action is not itself a prototype, it will contain the ActionID of the prototype reference.
        ///     <br/>This field is used to identify actions in a way that can be sent over the network.
        /// </summary>
        /// <remarks> Non-serialized, so it doesn't get saved between editor sessions.</remarks>
        [field: System.NonSerialized] public ActionID ActionID { get; set; }


        #region Action Definition Data

        [field: Header("General Settings")]
        /// <summary>
        ///     Does this count as a hostile Action?
        ///     (E.g. Should it: Break Stealth, Drop Shields, etc?).
        /// </summary>
        [field: SerializeField, Tooltip("Does this count as a hostile Action? (Should it: Break Stealth, Dropp Shields, etc?)")]
            public bool IsHostileAction { get; private set; } = true;

        /// <summary>
        ///     A.
        /// </summary>
        // Change to be based on the Action Type?
        [field: SerializeField, Tooltip("")]
        public bool ShouldNotifyClient { get; private set; } = true;

        /// <summary>
        ///     A.
        /// </summary>
        [field: SerializeField, Tooltip("")]
        public ActionActivationStyle ActivationStyle { get; private set; } = ActionActivationStyle.Held;

        /// <summary>
        ///     What stages this Action can be cancelled within.
        /// </summary>
        [field: SerializeField, Tooltip("What stages this Action can be cancelled within.")]
            public ActionInterruptionStages InterruptableStages { get; private set; } = ActionInterruptionStages.EntireDuration;


        [field: Header("Heat")]
        /// <summary>
        ///     How much heat this Action generates when initially activated.
        /// </summary>
        [field: SerializeField, Tooltip("How much heat this Action generates when activated.")]
            public float ImmediateHeat { get; private set; } = 0.0f;

        /// <summary>
        ///     How much heat this Action generates each second while charging.
        /// </summary>
        [field: SerializeField, Tooltip("How much heat this Action generates each second while charging.")]
            public float ChargingContinuousHeat { get; private set; } = 0.0f;

        /// <summary>
        ///     How much heat this Action generates each second while in use.
        /// </summary>
        [field: SerializeField, Tooltip("How much heat this Action generates each second while in use.")]
            public float ContinuousHeat { get; private set; } = 0.0f;

        /// <summary>
        ///     How much heat this Action generates each time that it successfully triggers.
        /// </summary>
        [field: SerializeField, Tooltip("How much heat this Action generates each time that it successfully updates.")]
            public float RetriggerHeat { get; private set; } = 0.0f;



        [field: Header("Timings")]
        /// <summary>
        ///     The time (In seconds) between starting this action and its effects triggering.
        /// </summary>
        [field: SerializeField, Min(0.0f), Tooltip("The time (In seconds) between starting this action and its effects triggering.")]
            public float WindupTime { get; private set; } = 0.0f;

        /// <summary>
        ///     The time (In seconds) between the final activation of this action and it being considered as over.
        /// </summary>
        [field: SerializeField, Min(0.0f), Tooltip("The time (In seconds) between the final activation of this action and it being considered as over.")]
            public float RecoveryTime { get; private set; } = 0.0f;

        /// <summary>
        ///     The time (In seconds) between this action ending and being able to activate it again..
        /// </summary>
        [field: SerializeField, Min(0.0f), Tooltip("The time (In seconds) between this action ending and being able to activate it again.")]
            public float CooldownTime { get; private set; } = 0.0f;

        /// <summary>
        ///     The periods of time in which the action prevents others from being activated.
        /// </summary>
        [field: SerializeField, Tooltip("The periods of time in which the action prevents others from being activated.")]
            public BlockingModeType BlockingMode { get; private set; } = BlockingModeType.OnlyDuringWindup;

        /// <summary>
        ///     The maximum time (In Seconds) that this action can be active for. 0 = Infinite.
        /// </summary>
        [field: SerializeField, Min(0.0f), Tooltip("The maximum time (In Seconds) that this action can be active for. 0 = Infinite..")]
            public float MaxActiveDuration { get; private set; } = 0.0f;



        [field: Header("Charging")]
        /// <summary>
        ///     Whether this action can be charged or not.
        /// </summary>
        [field: SerializeField, Tooltip("Whether this action can be charged or not.")]
            public bool CanCharge { get; private set; } = false;

        /// <summary>
        ///     The minimum time (In Seconds) that the action must be charged to to reach 100% effectiveness.
        /// </summary>
        [field: SerializeField, Min(0.0f), Tooltip("The minimum time (In Seconds) that the action must be charged to to reach 100% effectiveness.")]
            public float ChargeTime { get; private set; } = 0.0f;

        /// <summary>
        ///     The minimum charge percentage for the action to be triggered.
        /// </summary>
        [field: SerializeField, Range(0.0f, 1.0f), Tooltip("The minimum charge percentage for the action to be triggered.")]
            public float MinChargeActivationPercentage { get; private set; } = 1.0f;


        [field: Space(10)]
        /// <summary>
        ///     If true, the action should trigger immediately once reaching 100%. Otherwise, the action can be held at 100% until activated.
        /// </summary>
        [field: SerializeField, Tooltip("Should the action immediately execute once fully charged, or can it be held at full charge?")]
            public bool ExecuteImmediatelyOnceAtFullCharge { get; private set; } = true;


        [field: Space(10)]
        /// <summary>
        ///     If true, the action should treat itself as having had 0% charge if it ends while at or above full charge.
        ///     If false, always handle charge as normal.
        /// </summary>
        [field: SerializeField, Tooltip("Should this action discard its charge if it ends while at full charge?")]
            public bool RetainChargeOnceFull { get; private set; } = false;

        /// <summary>
        ///     The time (In seconds) that it takes for this action to go from 100% to 0% charge after ending.
        ///     0 = Immediate.
        /// </summary>
        [field: SerializeField, Min(0.0f), Tooltip("The time (In seconds) that it takes for this action to go from 100% to 0% charge after ending. 0 = Immediate.")]
            public float ChargeDepletionTime { get; private set; } = 0.0f;

        /// <summary>
        ///     The time (In seconds) that it takes for the UI of this action to reset to 0 after successfully firing.
        ///     0 = Immediate.
        /// </summary>
        [field: SerializeField, Min(0.0f), Tooltip("The time (In seconds) that it takes for the UI of this action to reset to 0 after successfully firing. 0 = Immediate.")]
            public float UIChargeDepletionTime { get; private set; } = 0.0f;


        [field: Header("Retrigger Timings")]
        [field: SerializeField]
        public ActionTriggerType TriggerType { get; private set; }
        [field: SerializeField]
        public bool CancelOnLastTrigger { get; private set; } = true;


        [field: Space(5)]
        [field: SerializeField]
        public float RetriggerDelay { get; private set; }


        [field: Space(5)]
        [field: SerializeField]
        public int Bursts { get; private set; }
        [field: SerializeField]
        public float BurstDelay { get; private set; }



        // Client Only Settings.
        [field: Header("Client Settings")]
        [field: SerializeReference, SubclassSelector]
            public ActionVisual[] TriggeringVisuals { get; private set; }
        // Animation Triggers.


        [field: Space(5)]
        [field: SerializeReference, SubclassSelector]
            public ActionVisual[] HitVisuals { get; private set; }


        [field: Space(5)]
        [field: SerializeField]
            public Crosshair ActionCrosshairPrefab { get; private set; }



        [field: Header("Triggering Settings")]
        /// <summary>
        ///     The <see cref="ActionEffect"/>s applied to hit targets.
        /// </summary>
        [field: SerializeReference, SubclassSelector, Tooltip("The ActionEffects applied to hit targets.")]
            public ActionEffect[] HitEffects { get; private set; }


        [field: Header("Activation Pattern Settings")]
        /// <summary>
        ///     How many times this action is activated when it is triggered.
        /// </summary>
        [field: SerializeField, Min(1), Tooltip("How many times this action is activated when it is triggered.")]
            public int ActivationsPerTrigger { get; private set; } = 1;

        /// <summary>
        ///     A fixed pattern to be applied to the direction of each individual activation.
        /// </summary>
        [field: SerializeField, Tooltip("The fixed pattern to be applied to the direction of each individual activation.")]
            public bool ActivationDeviationPattern { get; private set; }    // Bool as Placeholder.


        [field: Header("Deviation")]
        /// <summary>
        ///     A random deviation applied to the initial direction of targeting before the <see cref="ActivationDeviationPattern"/> is calculated.
        /// </summary>
        [field: SerializeField, Tooltip("")]
            public float TargetingStartDeviation { get; private set; }

        /// <summary>
        ///     A random deviation applied to the initial direction of targeting after the <see cref="ActivationDeviationPattern"/> is calculated.
        /// </summary>
        [field: SerializeField, Tooltip("Random deviation within the ActivationDeviationPattern")]
            public float TargetingInnerDeviation { get; private set; }


        #endregion


        #region Validation

#if UNITY_EDITOR

        public void OnValidate()
        {
            if (TriggerType != ActionTriggerType.Single)
            {
                if (TriggerType == ActionTriggerType.Repeated || TriggerType == ActionTriggerType.RepeatedBurst)
                {
                    // Check for issues with Repeated settings.
                    if (RetriggerDelay <= 0.0f)
                        Debug.LogError($"Error: You have a Repeating RetriggerType, but '{nameof(RetriggerDelay)}' is non-positive.");
                }

                if (TriggerType == ActionTriggerType.Burst || TriggerType == ActionTriggerType.RepeatedBurst)
                {
                    // Check for issues with Burst settings.
                    if (Bursts <= 0)
                        Debug.LogError($"Error: You have a Burst RetriggerType, but '{nameof(Bursts)}' is non-positive");
                    if (BurstDelay <= 0.0f)
                        Debug.LogError($"Error: You have a Burst RetriggerType, but '{nameof(BurstDelay)}' is non-positive");
                }
            }
        }

#endif

        #endregion



        protected Vector3 GetActionOrigin(ref ActionRequestData data) => data.OriginTransform != null ? data.OriginTransform.position : data.Position;
        protected Vector3 GetActionDirection(ref ActionRequestData data) => (data.OriginTransform != null ? data.OriginTransform.forward : data.Direction).normalized;


        #region Base Methods - Server

        /// <summary>
        ///     Called when the Action starts actually playing (Which may be after it is created, due to queueing).
        /// </summary>
        /// <returns> False if the Action decided it doesn't want to run. True otherwise.</returns>
        public abstract bool OnStart(Action action, ServerCharacter owner, ref ActionRequestData data);

        /// <summary>
        ///     Called when the Action wishes to Update itself.
        /// </summary>
        /// <returns> True to keep running, false to stop. The Action will stop by default when its duration expires, if it has one set.</returns>
        public bool OnUpdate(Action action, ServerCharacter owner, ref ActionRequestData data, float chargePercentage = 1.0f)
        {
            Vector3 baseDirection = GetActionDirection(ref data);

            for(int i = 0; i < ActivationsPerTrigger; ++i)
            {
                Vector3 triggerDirection = baseDirection;
                HandleTrigger(action, owner, triggerDirection, ref data, chargePercentage);
            }

            return ShouldContinue(action, owner);
        }
        protected virtual bool ShouldContinue(Action action, ServerCharacter owner) => ActionConclusion.Continue;
        protected abstract bool HandleTrigger(Action action, ServerCharacter owner, Vector3 direction, ref ActionRequestData data, float chargePercentage);


        /// <summary>
        ///     Called when the Action ends naturally.
        /// </summary>
        public virtual void OnEnd(Action action, ServerCharacter owner, ref ActionRequestData data) => Cleanup(action, owner);

        /// <summary>
        ///     Called when the Action gets cancelled.
        /// </summary>
        public virtual void OnCancel(Action action, ServerCharacter owner, ref ActionRequestData data) => Cleanup(action, owner);


        /// <summary>
        ///     Cleans up any ongoing effects on the server.
        /// </summary>
        protected virtual void Cleanup(Action action, ServerCharacter owner)
        {
            for (int i = 0; i < HitEffects.Length; ++i)
            {
                HitEffects[i].Cleanup(owner);
            }
        }



        public virtual void OnCollisionEntered(Action action, ServerCharacter owner, Collision collision) { }


        #endregion


        #region Base Methods - Client

        /// <summary>
        ///     Called on the client when the Action starts actually playing (Which may be after it is created, due to queueing).
        /// </summary>
        /// <returns> False if the Action decided it doesn't want to run. True otherwise.</returns>
        public virtual bool OnStartClient(Action action, ClientCharacter clientCharacter, ref ActionRequestData data)
        {
            foreach (ActionVisual visual in TriggeringVisuals)
                visual.OnClientStart(clientCharacter, GetActionOrigin(ref data), GetActionDirection(ref data));

            return ActionConclusion.Continue;
        }
        /// <summary>
        ///     Called on the client when this action should start charging.
        /// </summary>
        public virtual void OnStartChargingClient(Action action, ClientCharacter clientCharacter, ref ActionRequestData data)
        {
            foreach (ActionVisual visual in TriggeringVisuals)
                visual.OnClientStartCharging(clientCharacter, GetActionOrigin(ref data), GetActionDirection(ref data));
        }
        /// <summary>
        ///     Called on the client when the Action wishes to Update itself.
        /// </summary>
        /// <returns> True to keep running, false to stop. The Action will stop by default when its duration expires, if it has one set.</returns>
        public bool OnUpdateClient(Action action, ClientCharacter clientCharacter, ref ActionRequestData data, float chargePercentage = 1.0f)
        {
            Vector3 origin = GetActionOrigin(ref data);
            Vector3 baseDirection = GetActionDirection(ref data);

            for (int i = 0; i < ActivationsPerTrigger; ++i)
            {
                Vector3 triggerDirection = baseDirection;
                HandleClientTrigger(action, clientCharacter, origin, triggerDirection, ref data, chargePercentage);
            }

            return ActionConclusion.Continue;
        }
        protected virtual void HandleClientTrigger(Action action, ClientCharacter clientCharacter, Vector3 origin, Vector3 direction, ref ActionRequestData data, float chargePercentage)
        {
            foreach (ActionVisual visual in TriggeringVisuals)
                visual.OnClientUpdate(clientCharacter, origin, direction);
        }
        /// <summary>
        ///     Called on the client when the Action ends naturally.
        /// </summary>
        public virtual void OnEndClient(Action action, ClientCharacter clientCharacter, ref ActionRequestData data)
        {
            foreach (ActionVisual visual in TriggeringVisuals)
                visual.OnClientEnd(clientCharacter, GetActionOrigin(ref data), GetActionDirection(ref data));

            CleanupClient(action, clientCharacter);
        }
        /// <summary>
        ///     Called on the client when the Action gets cancelled.
        /// </summary>
        public virtual void OnCancelClient(Action action, ClientCharacter clientCharacter, ref ActionRequestData data)
        {
            foreach (ActionVisual visual in TriggeringVisuals)
                visual.OnClientCancel(clientCharacter, GetActionOrigin(ref data), GetActionDirection(ref data));

            CleanupClient(action, clientCharacter);
        }


        /// <summary>
        ///     Cleans up any ongoing effects on the client.
        /// </summary>
        protected virtual void CleanupClient(Action action, ClientCharacter clientCharacter) { }


        public virtual void AnticipateClient(Action action, ClientCharacter clientCharacter, ref ActionRequestData data) => Debug.Log("Anticipate");

        #endregion
    }
}

namespace Gameplay.Actions
{
    [System.Serializable]
    public enum ActionActivationStyle
    {
        /// <summary> Activate the action upon press. Cancel it upon release. </summary>
        Held,

        /// <summary> Activate the action upon press. Cancel it upon the next press.</summary>
        Toggle,

        /// <summary> Activate the action upon press. Don't cancel it ourselves.</summary>
        Pressed,
    }
    [System.Serializable, System.Flags]
    public enum ActionInterruptionStages
    {
        /// <summary> The action can never be interrupted.</summary>
        Never = 0,

        /// <summary> The action can be interrupted while in Windup.</summary>
        DuringWindup = 1 << 0,
        /// <summary> The action can be interrupted while executing.</summary>
        DuringExecution = 1 << 1,
        /// <summary> The action can be interrupted during recovery.</summary>
        DuringRecovery = 1 << 2,

        /// <summary> The action can be interrupted at any point.</summary>
        EntireDuration = ~0,
    }
}