using UnityEngine;

namespace Gameplay.Animations.ProceduralAnimations
{
    public class MechBody : MonoBehaviour
    {
        [SerializeField] private Transform _root;

        [field: Space(5)]
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


        [Header("Body Settings")]
        [SerializeField] private Transform _body;
        [SerializeField] private float _bodyHeight;
        [SerializeField] private int _frontLeftLegIndex, _frontRightLegIndex, _backLeftLegIndex, _backRightLegIndex;

        [Space(5)]
        [SerializeField] private Transform _rotationPivot;
        public Vector3 GroundedNormal;


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

            GroundedNormal = Vector3.up;
            _defaultRot = _body.rotation;
        }
        private void Update()
        {
            IsGrounded = Physics.Raycast(_root.position + _root.up * 0.1f, -_root.up, 0.2f, GroundLayers);

            // Update our cached gait based on our current state.
            UpdateGait();

            // Update the legs in order.
            foreach(MechLeg leg in Legs) leg.UpdateMemory();
            foreach(MechLeg leg in Legs) leg.UpdateMovement(Time.deltaTime);

            UpdateBody();
        }
        private void FixedUpdate() => UpdateCachedVelocities();


        private void UpdateBody()
        {
            #region Local Function Definitions

            // Returns the position of the given leg.
            Vector3 GetLegPos(MechLeg leg) => leg.IKTargetPosition;

            // Returns the desired LegGroundedPos, taking into account both the rotation of the rotationPivot (To allow for responsive camera rotation on slopes)
            //  and the leg positions (To account for cases where target is on a small obstacle and is positioned away from the rest).
            // Probably quite expensive, so if the Procedural Walking starts causing issues, this could be something to make more performant.
            //  Note: We only need to get the grounded pos for the local player (Maybe the server too), as the rotation pivot's roll and pitch aren't considered elsewhere (Confirm).
            Vector3 GetLegGroundedPos(MechLeg leg)
            {
                // World rest pos, calculated from the rotation pivot.
                // Note: Change to prevent accounting the yaw of the rotation pivot.
                Vector3 restWorldPos = _rotationPivot.TransformPoint(leg.RestLocalPosition);
                Debug.DrawRay(restWorldPos, Vector3.up * 0.1f, Color.red);

                // Start the linecase slightly before our last grounded position so that we will actually hit obstacles.
                const float LINECAST_START_BUFFER = 0.2f;
                Vector3 lineStart = leg.LastGroundedPosition + (leg.LastGroundedPosition - restWorldPos).normalized * LINECAST_START_BUFFER;
                Debug.DrawLine(lineStart, restWorldPos, Color.black);


                // If there is an obstruction and the hit is valid (E.g. Not a wall/ceiling collision), then use the hit point for our grounded pos.
                const float VALID_ANGLE_MIN_DOT = 0.1f;
                if (Physics.Linecast(lineStart, restWorldPos, out RaycastHit hitInfo, GroundLayers) && Vector3.Dot(hitInfo.normal, GroundedNormal) >= VALID_ANGLE_MIN_DOT)
                    return hitInfo.point;

                // Use the rest position if there is no obstruction.
                return restWorldPos;
            }

            bool RaycastGround(Vector3 origin, Vector3 direction, out RaycastHit hitInfo)
            {
                const float RAY_HALF_LENGTH = 1.0f;
                return Physics.Raycast(origin - direction * RAY_HALF_LENGTH, direction, out hitInfo, RAY_HALF_LENGTH * 2.0f, GroundLayers, QueryTriggerInteraction.Ignore);
            }

            #endregion


            // Note: Mark legs as 'Left' or 'Right', then use that data instead of index manipulation for ascribing our forward & right vectors.


            Vector3 frontLeft = GetLegPos(Legs[_frontLeftLegIndex]);
            Vector3 frontRight = GetLegPos(Legs[_frontRightLegIndex]);
            Vector3 backLeft = GetLegPos(Legs[_backLeftLegIndex]);
            Vector3 backRight = GetLegPos(Legs[_backRightLegIndex]);

            Vector3 leftSideForward = (frontLeft - backLeft).normalized;
            Vector3 rightSideForward = (frontRight - backRight).normalized;
            Vector3 forward = (leftSideForward + rightSideForward) / 2.0f;
            //Vector3 rightTest = ((backLeft - frontRight).normalized - (backRight - frontLeft).normalized) / 2.0f;

            Vector3 right = Vector3.zero;
            Vector3 groundedRight = Vector3.zero;
            Vector3 groundedForward = Vector3.zero;
            Vector3 rayDirection = -Vector3.up;
            for(int i = 0; i < Legs.Length; i += 2)
            {
                right += GetLegPos(Legs[i]);
                right -= GetLegPos(Legs[i + 1]);

                // Get the grounded right & forward vectors for calculating the rotation pivot's pitch & roll.
                if (RaycastGround(GetLegGroundedPos(Legs[i]), rayDirection, out RaycastHit hitInfo))
                {
                    groundedRight += hitInfo.point;

                    if ((i + 2) < Legs.Length && RaycastGround(GetLegGroundedPos(Legs[i + 2]), rayDirection, out RaycastHit hitInfo2))
                        groundedForward += (hitInfo.point - hitInfo2.point);
                }
                if (RaycastGround(GetLegGroundedPos(Legs[i + 1]), rayDirection, out hitInfo))
                {
                    groundedRight -= hitInfo.point;

                    if ((i + 3) < Legs.Length && RaycastGround(GetLegGroundedPos(Legs[i + 3]), rayDirection, out RaycastHit hitInfo2))
                        groundedForward += (hitInfo.point - hitInfo2.point);
                }
            }
            right = right.normalized;
            groundedRight = groundedRight.normalized;
            groundedForward = groundedForward.normalized;


            float preferredPitch = forward.GetPitch();
            float preferredRoll = right.GetPitch();

            // Body height.
            Vector3 bodyPos = (frontLeft + frontRight + backLeft + backRight) / 4.0f;
            //bodyPos.y += _bodyHeight;
            //bodyPos.x = _body.position.x;
            //bodyPos.z = _body.position.z;

            const float BODY_LOCAL_ADJUSTMENT_SPEED = 1.0f;
            _body.position = Vector3.MoveTowards(_body.position, bodyPos, BODY_LOCAL_ADJUSTMENT_SPEED * Time.deltaTime);



            // Body rotation.
            Quaternion orientation = _body.rotation * Quaternion.Inverse(_defaultRot);
            Quaternion preferredRotation = orientation.GetHorizontal() * Quaternion.AngleAxis(preferredPitch * Mathf.Rad2Deg, Vector3.right) * Quaternion.AngleAxis(preferredRoll * Mathf.Rad2Deg, Vector3.forward);
            const float BODY_ROTATION_RATE = 180.0f;
            if (_enableRot)
                _body.rotation = Quaternion.RotateTowards(_body.rotation, preferredRotation * _defaultRot, BODY_ROTATION_RATE * Time.deltaTime);


            // Pitch & roll for rotation pivot.
            Quaternion groundedRot = Quaternion.AngleAxis(groundedForward.GetPitch() * Mathf.Rad2Deg, _rotationPivot.right) * Quaternion.AngleAxis(groundedRight.GetPitch() * Mathf.Rad2Deg, _rotationPivot.forward);
            GroundedNormal = groundedRot * Vector3.up; // To-do: Add lerping.


            Debug.DrawRay(_root.position, groundedRight, Color.green);
            Debug.DrawRay(_root.position, groundedForward, Color.red);

            //Debug.DrawRay(_body.position, forward, Color.red);
            //Debug.DrawRay(_body.position, right, Color.green);
            //Debug.DrawRay(_body.position, preferredRotation * Vector3.up, Color.blue);
        }
        [SerializeField] private bool _enableRot;
        private Quaternion _defaultRot;

        private void UpdateCachedVelocities()
        {
            // Update velocity values.
            Velocity = (transform.position - _previousPosition) / Time.fixedDeltaTime;

            Quaternion deltaRotation = _previousRotation * Quaternion.Inverse(_body.rotation);
            deltaRotation = deltaRotation.GetHorizontal();
            Vector3 eulerRotation = new Vector3(
                Mathf.DeltaAngle(0.0f, Mathf.Round(deltaRotation.eulerAngles.x)),
                Mathf.DeltaAngle(0.0f, Mathf.Round(deltaRotation.eulerAngles.y)),
                Mathf.DeltaAngle(0.0f, Mathf.Round(deltaRotation.eulerAngles.z)));
            RotationalVelocity = (eulerRotation * Mathf.Deg2Rad) / Time.fixedDeltaTime;


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
            _previousRotation = _body.rotation;
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
            if (!_drawGizmos)
                return;

            foreach (MechLeg leg in Legs)
                leg.DrawGizmos(this);


            // Centre of Mass.
            Vector3 centreOfMass = transform.position + Vector3.up * 0.4f;
            Vector3[] polygon = new Vector3[]
            {
                Legs[_frontLeftLegIndex].IKTargetPosition,
                Legs[_frontRightLegIndex].IKTargetPosition,
                Legs[_backRightLegIndex].IKTargetPosition,
                Legs[_backLeftLegIndex].IKTargetPosition,
            };

            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(centreOfMass, 0.1f);

            Gizmos.color = IsWithinPolygon(centreOfMass, polygon) ? Color.green : Color.red;
            Gizmos.DrawLine(polygon[0], polygon[1]);
            Gizmos.DrawLine(polygon[1], polygon[2]);
            Gizmos.DrawLine(polygon[2], polygon[3]);
            Gizmos.DrawLine(polygon[3], polygon[0]);
        }
        private bool IsWithinPolygon(Vector3 testPoint, Vector3[] polygon)
        {
            // Cast a ray along Vector3.right.
            // If there are an odd number of intersections with the polygon, then we are inside it.
            return true;
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