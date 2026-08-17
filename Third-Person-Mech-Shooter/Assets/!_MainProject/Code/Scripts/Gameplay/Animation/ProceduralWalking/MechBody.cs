using UnityEngine;

namespace Gameplay.Animations.ProceduralAnimations
{
    public class MechBody : MonoBehaviour
    {
        [field: SerializeField] public MechLeg[] Legs { get; private set; }
        private Vector3 _previousPosition;
        private Quaternion _previousRotation;

        [field: SerializeField] public LayerMask GroundLayers { get; private set; }
        public bool IsGrounded { get; private set; }

        [field: SerializeField, ReadOnly] public Vector3 Velocity { get; private set; }           // Velocity in units/sec.
        [field: SerializeField, ReadOnly] public Vector3 RotationalVelocity { get; private set; } // Rotational Velocity in Rads/sec.
        public bool IsMoving { get; private set; }
        [field: SerializeField] public float MaxSpeed { get; private set; }


        [Header("Gait Settings")]
        [SerializeField] private Gait _defaultGait;                 // Fallback if no ConditionalGaits are valid.
        [SerializeField] private ConditionalGait[] _conditionalGaits;// Priority from Index 0 (Highest) to Index n (Lowest).
        public Gait Gait { get; private set; }  // Active Gait.


        [Header("Lerped Gait Settings")]
        [SerializeField] private float _velocityLerpGaitDelay;
        [SerializeField] private float _velocityLerpGaitTime;
        private float _velocityReachedZeroTime;

        [SerializeField] private float _yawLerpGaitDelay;
        [SerializeField] private float _yawLerpGaitTime;
        private float _yawDeltaReachedZeroTime; // Yaw Delta = RotationalVelocity.y
        private bool _isRotatingYaw;


        public LerpGait StationaryLerpGait => new LerpGait(bodyHeight: 1.1f, triggerZoneRadius: 0.25f);
        public LerpGait MovingLerpGait => new LerpGait(bodyHeight: 1.1f, triggerZoneRadius: 0.8f);


        [Header("Gizmos")]
        [SerializeField] private bool _drawGizmos = true;


        public LerpGait GetLerpedGait()
        {
            float velocityLerpFactor = IsMoving ? 1.0f : GetLerpFactor(_velocityReachedZeroTime, _velocityLerpGaitDelay, _velocityLerpGaitTime);
            float yawLerpFactor = _isRotatingYaw ? 1.0f : GetLerpFactor(_yawDeltaReachedZeroTime, _yawLerpGaitDelay, _yawLerpGaitTime);

            float lerpFactor = Mathf.Max(velocityLerpFactor, yawLerpFactor);
            return LerpGait.Lerp(StationaryLerpGait, MovingLerpGait, lerpFactor);


            // Returns the Lerp Factor for the given parameters,
            //  accounting for the delay not being reached & instant transition times.
            float GetLerpFactor(float reachedZeroTime, float lerpDelay, float lerpTime)
            {
                float timeSinceLerpStarted = Time.time - (reachedZeroTime + lerpDelay);
                return lerpTime > 0.0f
                    ? 1.0f - Mathf.Clamp01(timeSinceLerpStarted / lerpTime)
                    : (timeSinceLerpStarted > 0.0f ? 0.0f : 1.0f);
            }
        }
        /// <summary>
        ///     Returns true and sets <paramref name="index"/> to the index of the
        ///     passed <paramref name="leg"/> if it exists within this body's Legs array.<br/>
        ///     
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
            _defaultGait.Init(this);
            foreach(ConditionalGait conditionalGait in _conditionalGaits)
                conditionalGait.Gait.Init(this);
            Gait = _defaultGait;

            // Initialise legs.
            foreach(MechLeg leg in Legs) leg.InitMemory(this);
        }
        private void Update()
        {
            IsGrounded = Physics.Raycast(transform.position + transform.up * 0.1f, -transform.up, 0.2f, GroundLayers);

            // Update our cached gait based on our current state.
            UpdateGait();


            // Update the legs in order.
            foreach(MechLeg leg in Legs) leg.UpdateMemory();
            foreach(MechLeg leg in Legs) leg.UpdateMovement(Time.deltaTime);
        }
        private void FixedUpdate() => UpdateCachedVelocities();



        private void UpdateCachedVelocities()
        {
            // Update velocity values.
            Velocity = (transform.position - _previousPosition) / Time.fixedDeltaTime;

            Quaternion deltaRotation = _previousRotation * Quaternion.Inverse(transform.rotation);
            Vector3 eulerRotation = new Vector3(
                Mathf.DeltaAngle(0.0f, Mathf.Round(deltaRotation.eulerAngles.x)),
                Mathf.DeltaAngle(0.0f, Mathf.Round(deltaRotation.eulerAngles.y)),
                Mathf.DeltaAngle(0.0f, Mathf.Round(deltaRotation.eulerAngles.z)));
            RotationalVelocity = (eulerRotation / Time.fixedDeltaTime) * Mathf.Deg2Rad;


            // Cache values for lerped gait.
            if (MathUtils.IsApproximatelyZero(Velocity.sqrMagnitude))
            {
                if (IsMoving)
                    // We have just stopped moving.
                {
                    _velocityReachedZeroTime = Time.time;
                    IsMoving = false;
                }
            }
            else
                IsMoving = true;
            if (MathUtils.IsApproximatelyZero(RotationalVelocity.y))
            {
                if (_isRotatingYaw)
                    // We have just stopped rotating along the yaw.
                {
                    _yawDeltaReachedZeroTime = Time.time;
                    _isRotatingYaw = false;
                }
            }
            else
                _isRotatingYaw = true;

            // Cache values for next frame.
            _previousPosition = transform.position;
            _previousRotation = transform.rotation;
        }


        /// <summary>
        ///     Checks ConditionalGaits to determine which should be the active Gait.<br/>
        ///     Uses DefaultGait if no ConditionalGaits are valid.
        /// </summary>
        private void UpdateGait()
        {
            for(int i = 0; i < _conditionalGaits.Length; ++i)
            {
                if (_conditionalGaits[i].TestCondition(this))
                {
                    Gait = _conditionalGaits[i].Gait;
                    return;
                }
            }

            Gait = _defaultGait;
        }


        private void OnDrawGizmos()
        {
            if (_drawGizmos)
            {
                foreach (MechLeg leg in Legs)
                    leg.DrawGizmos(this);
            }
        }
        #if UNITY_EDITOR
        /// <summary>
        ///     Returns 'MechBody.Gait' if in play mode, or 'MechBody._defaultGait' if not.
        /// </summary>
        /// <returns></returns>
        public Gait Editor_GetGaitWithApplicationFallback() => Application.isPlaying ? Gait : _defaultGait;
        #endif
    }
}