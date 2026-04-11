using Gameplay.GameplayObjects;
using Gameplay.GameplayObjects.Character;
using UnityEngine;

namespace Gameplay.Actions.Definitions
{
    /// <summary>
    ///     An action that moves a <see cref="ServerCharacter"/> over time, selecting targets within a spherical Area of Effect around themselves as it does.
    /// </summary>
    [CreateAssetMenu(menuName = "Actions/Dash Action", order = 5)]
    public class DashAction : ActionDefinition
    {
        [Header("Dash - Movement")]
        /// <summary>
        ///     The distance (In units) that the dash can go before automatically ending.<br/>
        ///     0 = Unlimited.
        /// </summary>
        [SerializeField, Min(0.0f), Tooltip("How far the dash can go before automatically ending. 0 = Unlimited")]
        private float _dashMaxDistance = 0.0f;

        /// <summary>
        ///     The speed (In units/s) that the user travels while in the dash.<br/>
        ///     0 = Instantly reaches max distance.
        /// </summary>
        [SerializeField, Min(0.0f), Tooltip("The speed (In units/s) that the user travels while in the dash. 0 = Instantly reaches max distance")]
        private float _dashSpeed = 0.0f;

        [Space(5)]
        [SerializeField] private float _movementPreventionDuration = 0.0f;

        [Space(5)]
        [SerializeField] private ChargeScalingOptions _chargeScalingOptions = ChargeScalingOptions.None;


        [Space(10)]
        [SerializeField, Min(0.0f)] private float _movementRotationRate = 0.0f;
        [SerializeField] private bool _transformMovementToGround = true;


        [Header("Dash - Speed over Time")]
        [SerializeField] private bool _useSpeedOverTime = false;
        [SerializeField] private AnimationCurve _speedOverTimeCurve = AnimationCurve.Constant(0.0f, 1.0f, 1.0f);


        [Header("Dash - Collisions")]
        [SerializeField] private bool _cancelDashOnTargetCollision = false;
        [SerializeField] private bool _cancelDashOnEnvironmentCollision = false;
        [SerializeField] private LayerMask _environmentLayers;


        public override bool OnStart(Action action, ServerCharacter owner, ref ActionRequestData data)
        {
            if (_dashSpeed == 0.0f)
            {
                owner.Movement.AddPosition(data.Direction * _dashMaxDistance, true);
                return ActionConclusion.Stop;
            }

            owner.Movement.AddContinuousForcedMovement(action, data.Direction, _dashSpeed, _movementPreventionDuration, _movementRotationRate, _transformMovementToGround);
            return ActionConclusion.Continue;
        }
        protected override bool HandleTrigger(Action action, ServerCharacter owner, Vector3 direction, ref ActionRequestData data, float chargePercentage)
        {
            if (_useSpeedOverTime)
            {
                // Speed over time calc.
                //owner.Movement.AdjustContinuousForcedMovement(action, newSpeed);
            }

            // View for inspiration: https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop/blob/main/Assets/Scripts/Gameplay/Action/ConcreteActions/TrampleAction.cs
            return ActionConclusion.Continue;
            //return CheckForCancellableCollision(action, owner) ? ActionConclusion.Stop : ActionConclusion.Continue;
        }
        protected override void Cleanup(Action action, ServerCharacter owner)
        {
            base.Cleanup(action, owner);

            owner.Movement.RemoveContinuousForcedMovement(action);
        }

        private bool CheckForCancellableCollision(Action action, ServerCharacter owner)
        {
            if (!_cancelDashOnEnvironmentCollision)
                return false;   // We're not cancelling on environment collisions.
            if (!Physics.Raycast(action.Data.Position, action.Data.Direction, out RaycastHit hitInfo, 1.0f, _environmentLayers, QueryTriggerInteraction.Ignore))
                return false;   // No collision.

            float dot = Vector3.Dot(-hitInfo.normal, GetActionDirection(ref action.Data));


            const float MIN_COLLISION_DOT_VALUE = 0.6f;
            return dot >= MIN_COLLISION_DOT_VALUE;   // True if collision should cancel action.
        }

        public override void OnCollisionEntered(Action action, ServerCharacter owner, Collision collision)
        {
            if (!collision.transform.TryGetComponentThroughParents<IDamageable>(out IDamageable damageScript, checkSelf: true))
                return; // Not an object we can target.

            // Apply hit effects.
            Debug.Log("Apply Hit Effects");



            // Check if we should cancel.
            if (_cancelDashOnTargetCollision)
            {
                Debug.Log("We should cancel");
            }
        }


        [System.Serializable, System.Flags]
        private enum ChargeScalingOptions
        {
            None = 0,

            Duration = 1 << 0,
            Speed = 1 << 1,

            Everything = ~0
        }
    }
}