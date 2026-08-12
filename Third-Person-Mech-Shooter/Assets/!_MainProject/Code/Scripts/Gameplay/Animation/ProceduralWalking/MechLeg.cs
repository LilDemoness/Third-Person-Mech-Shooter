using UnityEngine;

namespace Gameplay.Animations.ProceduralAnimations
{
    [System.Serializable]
    public class MechLeg
    {
        #region Settings

        private MechBody _body;


        [SerializeField] private Transform _ikTargetTransform;
        private Vector3 _ikTargetPosition;          // World-space position of the IK Target (Unaffected by transform hierarchies vs the actual transform).
        private Vector3 _previousIKTargetPosition;  // Previous world-space position of the IK Target.


        private Capsule _triggerZoneLocal;          // In Local space. Outside of this capsule, the leg should step to reach the _target position.
        private Capsule _comfortZoneLocal;          // In Local space. Outside of this capsule, ...

        private Vector3 _restLocalPosition;         // Rest position of the IK Target, in Local-Space.
        private Vector3 _lookAheadLocalPosition;    // Current Look-Ahead position for the IK Target, in Local-Space. Used for determining our actual target position.

        private LineSegment _scanLineLocal;         // In Local Space. Contains the values used when searching for our grounded LegTarget (Where we wish to step).


        private LegTarget _groundTarget;
        private LegTarget _target;


        private bool _isTouchingGround = true;      // Whether this leg is currently touching the ground.
        private bool _isMoving = false;             // Whether this leg is currently stepping.
        private float _timeSinceLastMoveStarted = 0.0f;     // Time (In Seconds) since the last Leg Move started.   (Change to be a static cached Time.time value instead?)
        private float _timeSinceLastMoveCompleted = 0.0f;   // Time (In Seconds) since the last Leg Move completed. (Change to be a static cached Time.time value instead?)


        private Vector3 _stepStartPosition;         // World-space position where the current step started.
        private float _stepProgress;                // Percentage progress of the current step.


        private bool _isDisabled = false;           // If true, this Leg has been disabled and should remain in its disabled position.
        private bool _isPrimary = false;            // If true, this leg is ...
        private bool _canStep = false;              // Cached value of whether this leg can move or not.


        private bool _isOutsideTriggerZone => !_triggerZoneLocal.Contains(_body.transform.InverseTransformPoint(_ikTargetPosition));    // If true, the IKTarget is outwith our trigger zone (We're wanting to start a step to get back inside).
        private bool _isOutsideComfortZone => !_comfortZoneLocal.Contains(_body.transform.InverseTransformPoint(_ikTargetPosition));    // If true...

        #endregion

        #region Accessors

        public Transform IKTargetTransform => _ikTargetTransform;
        public Vector3 IKTargetPosition => _ikTargetPosition;

        public LegTarget Target => _target;

        public bool IsTouchingGround => _isTouchingGround;

        public float TimeSinceLastMoveStarted => _timeSinceLastMoveStarted;
        public float TimeSinceLastMoveCompleted => _timeSinceLastMoveCompleted;

        public bool IsDisabled => _isDisabled;
        public bool IsPrimary { get => _isPrimary; set => _isPrimary = value; }

        public bool IsOutsideTriggerZone => _isOutsideTriggerZone;
        public bool IsOutsideComfortZone => _isOutsideComfortZone;

        #endregion


        public bool IsGrounded() => _isTouchingGround && !_isMoving && !_isDisabled;


        /// <summary>
        ///     Initialise this leg's cached values and assign it to the given <paramref name="body"/>.
        /// </summary>
        public void InitMemory(MechBody body)
        {
            _body = body;
            _restLocalPosition = body.transform.InverseTransformPoint(_ikTargetTransform.position);

            UpdateMemory();

            _groundTarget = LocateGroundTarget();
            _target = _groundTarget ?? GetStrandedTarget();
            _ikTargetPosition = _ikTargetTransform.position;
            _previousIKTargetPosition = _ikTargetPosition;
        }
        /// <summary>
        ///     Update the current cached values for this leg.
        /// </summary>
        public void UpdateMemory()
        {
            LerpGait lerpedGait = _body.GetLerpedGait();
            Vector3 scanOrientation = _body.transform.up;//_body.Gait.ScanPivotMode.Get(_body);

            Vector3 upVector = Quaternion.LookRotation(Vector3.forward, scanOrientation) * Vector3.up;


            // Update look-ahead position for the current motion.
            _lookAheadLocalPosition = GetLookAheadPosition(_restLocalPosition, lerpedGait.TriggerZoneRadius);

            // Update our desired scan values (From the look-ahead position).
            const float SCAN_HEIGHT_MULTIPLIER = 1.6f;
            const float SCAN_LENGTH_MULTIPLIER = 2.5f;
            Vector3 scanStartOffset = upVector * lerpedGait.BodyHeight * SCAN_HEIGHT_MULTIPLIER;
            Vector3 scanVector = upVector * -lerpedGait.BodyHeight * SCAN_LENGTH_MULTIPLIER;
            _scanLineLocal = LineSegment.FromOffset(_lookAheadLocalPosition + scanStartOffset, scanVector);

            // Update our Trigger/Comfort Zone Capsules (From rest position; Same axis as the scan line).
            Vector3 zoneStart = _restLocalPosition + scanStartOffset;
            Vector3 zoneEnd = zoneStart + scanVector;
            _triggerZoneLocal = new Capsule(zoneStart, zoneEnd, lerpedGait.TriggerZoneRadius);
            _comfortZoneLocal = new Capsule(zoneStart, zoneEnd, _body.Gait.Settings.ComfortZoneRadius);
        }


        /// <summary>
        ///     Updates the Leg.
        /// </summary>
        public void UpdateMovement(float deltaTime)
        {
            _previousIKTargetPosition = _ikTargetPosition;

            _timeSinceLastMoveStarted += deltaTime;
            _timeSinceLastMoveCompleted += deltaTime;

            // Update the target position.
            bool targetChanged = UpdateTarget();
            if (targetChanged)
                SoftResetStep();


            // Inherit parent motion while falling.
            if (!IsGrounded())
            {
                ApplyBodyMotion(ref _ikTargetPosition);
                if (_isMoving)
                    ApplyBodyMotion(ref _stepStartPosition);
            }


            // Handle Step.
            bool stepCompleted = HandleStep(deltaTime);

            // Resolve ground collisions to avoid the leg phasing through objects.
            //var collision = ;
            //if (collision != null)
            //{
                // Ignore the collision if it would push the IKTarget further from the target.
            //}


            // Update the target transform.
            _ikTargetTransform.position = _ikTargetPosition;

            // Play "On Step End" effects (C# Event? UnityEvent? Hard-coded elements?).
            if (stepCompleted)
            {
                Debug.Log("Step");
            }
        }
        /// <summary>
        ///     Updates the current value of _target.
        /// </summary>
        /// <returns> True if the target position was changed, otherwise false.</returns>
        private bool UpdateTarget()
        {
            Vector3 oldTargetPosition = _target.Position;
            _groundTarget = LocateGroundTarget();

            if (_isDisabled)
                _target = GetDisabledTarget(_groundTarget != null ? _groundTarget.Position : null);
            else
            {
                if (_groundTarget != null)
                    _target = _groundTarget;

                if (!_target.IsGrounded || !_comfortZoneLocal.Contains(_body.transform.InverseTransformPoint(_target.Position)))
                    _target = GetStrandedTarget();
            }

            const float TARGET_MOVED_SQR_DISTANCE_THRESHOLD = 0.01f;
            return (oldTargetPosition - _target.Position).sqrMagnitude >= TARGET_MOVED_SQR_DISTANCE_THRESHOLD;
        }
        /// <summary>
        ///     Handles the 'update' call of the Step.
        /// </summary>
        /// <returns> True if the step was completed, otherwise false.</returns>
        private bool HandleStep(float deltaTime)
        {
            if (_isMoving)
                return UpdateStep(deltaTime);
            else
            {
                _canStep = _body.Gait.CanMoveLeg(this);
                if (_canStep)
                    BeginStep();

                return false;
            }
        }

        /// <summary>
        ///     Applies the Velocity and Rotational Velocity of this leg's body to the vector <paramref name="pos"/>.
        /// </summary>
        private void ApplyBodyMotion(ref Vector3 pos)
        {
            pos += _body.Velocity * Time.deltaTime;
            pos = RotateAroundY(pos, _body.RotationalVelocity.y * Time.deltaTime, _body.transform.position);

            // Separated for readability.
            Vector3 RotateAroundY(Vector3 point, float angle, Vector3 origin) => (Quaternion.AngleAxis(angle, Vector3.up) * (point - origin)) + origin;
        }


        #region Step Functions

        /// <summary>
        ///     Starts a step with the leg.
        /// </summary>
        private void BeginStep()
        {
            _isMoving = true;
            _timeSinceLastMoveStarted = 0.0f;
            _stepStartPosition = _ikTargetPosition;
            _stepProgress = 0.0f;
            _isTouchingGround = false;

            Debug.Log($"Step Started (Target Name: {_ikTargetTransform.name})");
        }
        /// <summary>
        ///     Completes the current step.
        /// </summary>
        /// <returns> True if the step completed by touching the ground, otherwise false.</returns>
        private bool CompleteStep()
        {
            _isMoving = false;
            _timeSinceLastMoveCompleted = 0.0f;
            _ikTargetPosition = _target.Position; // Ensure we're at the target.
            _isTouchingGround = GetIsTouchingGround();
            Debug.Log($"Step Complete (Target Name: {_ikTargetTransform.name})");
            return _isTouchingGround;
        }
        /// <summary>
        ///     Restarts a step from the current position.<br/>
        ///     Has no meaningful effect if a step isn't currently in progress.
        /// </summary>
        private void SoftResetStep()
        {
            _stepStartPosition = _ikTargetPosition;
            _stepProgress = 0.0f;
        }

        /// <summary>
        ///     Updates a step with the given <paramref name="deltaTime"/>.
        /// </summary>
        /// <returns> True if the step is complete and the leg is touching the ground, otherwise false.</returns>
        private bool UpdateStep(float deltaTime)
        {
            float distance = Vector3.ProjectOnPlane(_stepStartPosition - _target.Position, Vector3.up).magnitude;
            float legMoveSpeed = _body.MaxSpeed * _body.Gait.Settings.LegSpeedMultiplier;
            _stepProgress = MathUtils.IsApproximatelyZero(distance) ? 1.0f : Mathf.MoveTowards(_stepProgress, 1.0f, (legMoveSpeed / distance) * deltaTime);

            _ikTargetPosition = SampleStepPosition(_stepProgress);

            if (_stepProgress >= 1.0f)
                return CompleteStep();

            return false;
        }

        #endregion


        /// <summary>
        ///     Returns the target position for a step at the percentage <paramref name="t"/>.
        /// </summary>
        private Vector3 SampleStepPosition(float t)
        {
            Vector3 position = Vector3.Lerp(_stepStartPosition, _target.Position, t);
            position.y += _body.Gait.Settings.LegLiftHeight * GetStepLiftFactor(t);
            return position;
        }
        // Uses a parabolic forumla to get the fractional height of a step (t: 0f/1f = x0; t: 0.5f = x1).
        private float GetStepLiftFactor(float t) => 4.0f * t * (1.0f - t);


        /// <summary>
        ///     Sends a short raycast to determine if the body is on the ground or not.
        /// </summary>
        private bool GetIsTouchingGround() => Physics.Raycast(_body.transform.position + _body.transform.up * 0.01f, -_body.transform.up, 0.02f);
        private Vector3 GetLookAheadPosition(Vector3 restPosition, float triggerZoneRadius)
        {
            if (_body.IsMoving)
                return restPosition; // Check notes as to why we're doing this.

            // Get the direction we are moving in.
            Vector3 direction = MathUtils.IsApproximatelyZero(_body.Velocity.sqrMagnitude) ? Vector3.forward : _body.transform.InverseTransformDirection(_body.Velocity.normalized);

            Vector3 lookAheadOffset = direction * triggerZoneRadius * _body.Gait.Settings.LegLookAheadFraction;
            //lookAhead = Quaternion.AngleAxis(-_body.RotationalVelocity.y, Vector3.up) * lookAhead; // We are already casting from local space to world space, so this would mess up our position. (Double Check).
            return restPosition + lookAheadOffset;
        }


        #region Leg Targets

        private LegTarget LocateGroundTarget()
        {
            Vector3 lookAhead = _body.transform.TransformPoint(_lookAheadLocalPosition);
            Vector3 rayStart = _body.transform.TransformPoint(_scanLineLocal.Point1);
            Vector3 rayDir = _scanLineLocal.GetVector().normalized;
            float rayLength = _scanLineLocal.GetVector().magnitude;

            // A.
            LegTarget Raycast(float testX, float testZ)
            {
                Vector3 start = new Vector3(testX, rayStart.y, testZ);
                Debug.DrawRay(start, rayDir * rayLength, Color.red, 0.01f);

                if (Physics.Raycast(start, rayDir, out RaycastHit hitInfo, rayLength, _body.GroundLayers))
                    return new LegTarget(position: hitInfo.point, isGrounded: true);
                return null;
            }
            
            LegTarget mainTargetCandidate = Raycast(rayStart.x, rayStart.z);

            if (!_body.Gait.Settings.LegScanAlternativeGround)
                return mainTargetCandidate;

            // To-do: Test fallback positions around the main candidate when it isn't valid.
            return mainTargetCandidate;
        }

        private LegTarget GetStrandedTarget() => new LegTarget(position: _body.transform.TransformPoint(_lookAheadLocalPosition), isGrounded: false);

        private LegTarget GetDisabledTarget(Vector3? groundPosition)
        {
            LerpGait lerpedGait = _body.GetLerpedGait();
            Vector3 upVector = _body.transform.up;

            LegTarget target = GetStrandedTarget();
            target.Position += upVector * lerpedGait.BodyHeight * 0.5f;

            float minY = (groundPosition.HasValue ? groundPosition.Value.y : float.MinValue) + lerpedGait.BodyHeight * 0.1f;
            target.Position = new Vector3(
                target.Position.x,
                Mathf.Min(target.Position.y, minY),
                target.Position.z);

            return target;
        }

        #endregion


        /// <summary>
        ///     Returns true if <paramref name="other"/> shares the same IKTargetTransform as this.
        /// </summary>
        public bool ShareTargets(MechLeg other) => this.IKTargetTransform == other.IKTargetTransform;


        /// <summary>
        ///     Draws gizmos for this leg using the passed <paramref name="body"/>. 
        /// </summary>
        public void DrawGizmos(MechBody body)
        {
            // Account for the fact that not everything is set before play mode.
            Vector3 restLocalPos = Application.isPlaying ? _restLocalPosition : _ikTargetTransform.position;
            Vector3 targetWorldPos = Application.isPlaying ? _ikTargetPosition : _ikTargetTransform.position;

            // Translate necessary local-space values to world-space.
            Vector3 restWorldPos = body.transform.TransformPoint(restLocalPos);

            // Draw Gizmos.

            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(restWorldPos, 0.1f);
            Gizmos.color = Color.black;
            Gizmos.DrawSphere(targetWorldPos, 0.1f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(restWorldPos, body.DefaultGait.Settings.ComfortZoneRadius);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(restWorldPos, body.MovingLerpGait.TriggerZoneRadius);
        }
    }

    /// <summary>
    ///     Container for a potential target position of a <seealso cref="MechLeg"/>
    /// </summary>
    [System.Serializable]
    public class LegTarget
    {
        public Vector3 Position { get; set; } = Vector3.zero;
        public bool IsGrounded { get; set; } = false;


        public LegTarget(Vector3 position, bool isGrounded)
        {
            Position = position;
            IsGrounded = isGrounded;
        }
    }


    [System.Serializable]
    public class Capsule
    {
        public Vector3 Point1 { get; set; }
        public Vector3 Point2 { get; set; }
        public float Radius { get; set; }


        public Capsule(Vector3 point1, Vector3 point2, float radius)
        {
            Point1 = point1;
            Point2 = point2;
            Radius = radius;
        }


        public LineSegment GetLine() => new LineSegment(Point1, Point2);
        public bool Contains(Vector3 point) => GetLine().SqrDistance(point) <= (Radius * Radius);
    }
    [System.Serializable]
    public class LineSegment
    {
        public Vector3 Point1 { get; set; }
        public Vector3 Point2 { get; set; }

        public LineSegment(Vector3 point1, Vector3 point2)
        {
            Point1 = point1;
            Point2 = point2;
        }
        public static LineSegment FromOffset(Vector3 start, Vector3 offset) => new LineSegment(start, start + offset);


        public Vector3 GetVector() => Point2 - Point1;

        public float Distance(Vector3 point) => Mathf.Sqrt(SqrDistance(point));
        /// <summary>
        ///     Returns the square distance from <paramref name="point"/> to the closest point on this line.
        /// </summary>
        public float SqrDistance(Vector3 point)
        {
            float abX = Point2.x - Point1.x;
            float abY = Point2.y - Point1.y;
            float abZ = Point2.z - Point1.z;
            float sqrLength = (abX * abX) + (abY * abY) + (abZ * abZ);

            float apX = point.x - Point1.x;
            float apY = point.y - Point1.y;
            float apZ = point.z - Point1.z;
            float t = MathUtils.IsApproximatelyZero(sqrLength) ? 0.0f : ((apX * abX) + (apY * abY) + (apZ * abZ)) / sqrLength;

            float dX = point.x - (Point1.x + abX * t);
            float dY = point.y - (Point1.y + abY * t);
            float dZ = point.z - (Point1.z + abZ * t);
            return (dX * dX) + (dY * dY) + (dZ * dZ);
        }
    }
}