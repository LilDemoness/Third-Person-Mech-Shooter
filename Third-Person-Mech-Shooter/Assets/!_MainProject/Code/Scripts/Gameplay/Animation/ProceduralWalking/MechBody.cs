using UnityEngine;

namespace Gameplay.Animations.ProceduralAnimations
{
    public class MechBody : MonoBehaviour
    {
        public MechLeg[] Legs;
        private Vector3 _previousPosition;
        private Quaternion _previousRotation;

        public LayerMask GroundLayers;

        public Gait Gait { get; private set; }      // Current Gait.
        public Gait DefaultGait;                    // Fallback if no ConditionalGaits are valid.
        public ConditionalGait[] ConditionalGaits;  // Priority from Index 0 (Highest) to Index n (Lowest).

        public LerpGait StationaryLerpGait  => new LerpGait(bodyHeight: 1.1f, triggerZoneRadius: 0.25f);
        public LerpGait MovingLerpGait      => new LerpGait(bodyHeight: 1.1f, triggerZoneRadius: 0.8f);

        public Vector3 Velocity { get; private set; }           // Velocity in units/sec.
        public Vector3 RotationalVelocity { get; private set; } // Rotational Velocity in Deg/sec.

        public bool IsGrounded { get; private set; }
        public bool IsMoving { get; private set; }

        [field: SerializeField] public float MaxSpeed { get; private set; }

        public LerpGait GetLerpedGait()
        {
            if (!MathUtils.IsApproximatelyZero(RotationalVelocity.y))
                return new LerpGait(MovingLerpGait);

            float speedFraction = Velocity.magnitude / MaxSpeed;
            return LerpGait.Lerp(StationaryLerpGait, MovingLerpGait, speedFraction);
        }


        [Header("Gizmos")]
        [SerializeField] private bool _drawGizmos = true;


        /// <summary>
        ///     Returns true and sets <paramref name="index"/> to the index of the passed <paramref name="leg"/> if it exists within this body's Legs array.<br/>
        ///     Otherwise, returns false and sets <paramref name="index"/> to -1.
        /// </summary>
        public bool TryGetLegIndex(MechLeg leg, out int index)
        {
            for(int i = 0; i < Legs.Length; ++i)
            {
                if (Legs[i] == leg)
                {
                    index = i;
                    return true;
                }
            }

            index = -1;
            return false;
        }


        private void Awake()
        {
            // Initialise Gaits.
            DefaultGait.Init(this);
            foreach(ConditionalGait conditionalGait in ConditionalGaits)
                conditionalGait.Gait.Init(this);
            Gait = DefaultGait;

            // Initialise legs.
            foreach(MechLeg leg in Legs) leg.InitMemory(this);
        }
        private void Update()
        {
            // Update cached values.
            Velocity = (transform.position - _previousPosition) / Time.deltaTime;
            RotationalVelocity = (Quaternion.Inverse(_previousRotation) * transform.rotation).eulerAngles / Time.deltaTime;

            const float MIN_SQR_VELOCITY_FOR_MOVEMENT = 0.1f * 0.1f;
            IsMoving = Velocity.sqrMagnitude >= MIN_SQR_VELOCITY_FOR_MOVEMENT;
            IsGrounded = Physics.Raycast(transform.position + transform.up * 0.1f, -transform.up, 0.2f, GroundLayers);

            // Update our cached gait based on our current state.
            UpdateGait();


            // Update the legs in order.
            foreach(MechLeg leg in Legs) leg.UpdateMemory();
            foreach(MechLeg leg in Legs) leg.UpdateMovement(Time.deltaTime);


            _previousPosition = transform.position;
            _previousRotation = transform.rotation;
        }
        /// <summary>
        ///     Checks ConditionalGaits to determine which should be the active Gait.<br/>
        ///     Uses DefaultGait if no ConditionalGaits are valid.
        /// </summary>
        private void UpdateGait()
        {
            for(int i = 0; i < ConditionalGaits.Length; ++i)
            {
                if (ConditionalGaits[i].TestCondition(this))
                {
                    Gait = ConditionalGaits[i].Gait;
                    return;
                }
            }

            Gait = DefaultGait;
        }


        private void OnDrawGizmos()
        {
            if (_drawGizmos)
            {
                foreach (MechLeg leg in Legs)
                    leg.DrawGizmos(this);
            }
        }
    }
}