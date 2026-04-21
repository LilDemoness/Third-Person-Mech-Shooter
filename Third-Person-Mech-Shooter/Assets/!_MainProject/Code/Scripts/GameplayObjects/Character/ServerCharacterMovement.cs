using UnityEngine;
using Unity.Netcode;
using Gameplay.GameplayObjects.Character.Statistics;
using Gameplay.Actions.Definitions;
using Gameplay.Actions;
using System.Collections.Generic;
using Unity.Netcode.Components;

namespace Gameplay.GameplayObjects.Character
{
    /// <summary>
    ///     A component responsible for moving a character on the server side based on inputs (Both User and Pathing).
    /// </summary>
    public class ServerCharacterMovement : NetworkBehaviour
    {
        private Vector2 _movementInput;
        private Vector3 _desiredVelocity;


        private MovementType _movementState;
        private MovementStatus _previousMovementStatus;

        public event System.Action<MovementStatus> OnMovementStatusChanged;


        [Header("General References")]
        [SerializeField] private ServerCharacter _serverCharacter;
        [SerializeField] private CharacterStats _characterStats;
        [SerializeField] private Transform _rotationPivot;
        [SerializeField] private CharacterController _characterController;
        [SerializeField] private NetworkTransform _networkTransform;


        public Transform RotationPivot => _rotationPivot;
        public float MovementSpeed => _characterStats.GetStatisticValue(Statistic.MovementSpeed);


        [Header("Boost Settings")]
        [SerializeField] private ActionDefinition _defaultBoostAction;
        [SerializeField, ReadOnly] private ActionDefinition _boostActionOverride;
        private ActionDefinition _boostAction => _boostActionOverride ?? _defaultBoostAction;

        private int _boostCount => Mathf.CeilToInt(_characterStats.GetStatisticValue(Statistic.BoostCount));


        const float BOOST_RECHARGE_DURATION = 2.0f; // Time in Seconds.
        const float BOOST_RECHARGE_RATE = 1.0f / BOOST_RECHARGE_DURATION;  // Percentage / Second.
        
        private float _boostRechargeMultiplier => _characterStats.GetStatisticValue(Statistic.BoostRechargeMultiplier);
        private int _boostCountRemaining;
        private float _boostRechargeProgress;


        public void OnBoostStatsChanged_SubscribeAndCallback(System.Action<int> callback) { _onBoostStatsChanged += callback; callback?.Invoke(_boostCount); }
        public void OnBoostStatsChanged_Unsubscribe(System.Action<int> callback) => _onBoostStatsChanged -= callback;
        private event System.Action<int> _onBoostStatsChanged;

        public event System.EventHandler<OnBoostChargeValuesChangedEventArgs> OnBoostRechargeValuesChanged;


        [Header("In-Air Settings")]
        [SerializeField] private float _gravityMultiplier = 1.0f;
        private const float GRAVITY = -9.81f;
        private float _verticalVelocity;

        [SerializeField] private float _airSpeedDecreaseRate = 1.0f;


        [Header("Ground Check")]
        [SerializeField] private float _groundCheckRadius = 0.5f;
        [SerializeField] private LayerMask _groundLayers;
        private bool _isGrounded;


        [Header("Uncontrolled Movement")]
        private Dictionary<object, ForcedMovementData> _forcedMovementValues;
        private Vector3 _currentForces;
        private float _allowInputTime;
        private float _forceDecreaseRate = 2.0f;


        private void Awake()
        {
            // Disable ourselves until we have been spawned
            this.enabled = false;
            this._characterController.enabled = false;
        }
        public override void OnNetworkSpawn()
        {
            if (!IsServer)
                return;
            
            // Only enable this component on servers.
            this.enabled = true;

            // On the server, enable our other components and initialise ourself.
            this._characterController.enabled = true;


            // Initialise Values.
            _forcedMovementValues = new Dictionary<object, ForcedMovementData>();
            _allowInputTime = 0.0f;


            // Subscribe to events.
            _characterStats.OnStatisticChanged += CharacterStats_OnStatisticChanged;
            CharacterStats_OnStatisticChanged(Statistic.BoostCount);
        }
        private void FixedUpdate()
        {
            CheckIsGrounded();
            PerformMovement();
            RechargeBoostFramewise();

            var currentState = GetMovementStatus(_movementState);
            if (_previousMovementStatus != currentState)
            {
                OnMovementStatusChanged?.Invoke(currentState);
                _previousMovementStatus = currentState;
            }
        }
        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                // Disable server components when despawning.
                this.enabled = false;

                // Unsubscribe from events.
                _characterStats.OnStatisticChanged -= CharacterStats_OnStatisticChanged;
            }
        }

        private void CharacterStats_OnStatisticChanged(Statistic statistic)
        {
            if (statistic != Statistic.BoostCount)
                return;

            _boostCountRemaining = _boostCount;
            NotifyListenersOfBoostCountChanged();
        }



        public void SetPositionAndRotation(Vector3 newPosition, Quaternion newRotation)
        {
            this._characterController.enabled = false;  // Disable the CharacterController to facilitate teleportation.

            transform.position = newPosition;
            if (IsSpawned) // Check facilitates initial position setting for spawning players.
            {
                _networkTransform.Teleport(newPosition, transform.rotation, this.transform.lossyScale);   // Teleport for instant movement in clients.
            
                if (this.TryGetComponent<CameraControllerTest>(out CameraControllerTest cameraController))  // Temporary fix to ensure that player rotation isn't messed up by the forced change in rotation.
                    cameraController.SetRotationOwnerRpc(newRotation.eulerAngles.y, newRotation.eulerAngles.x);
            }

            this._characterController.enabled = true;
        }


        private void CheckIsGrounded() => _isGrounded = Physics.CheckSphere(transform.position, _groundCheckRadius, _groundLayers, QueryTriggerInteraction.Ignore);
        private Vector3 CalculateDesiredMovement()
        {
            if (_movementState == MovementType.Idle)
                return Vector3.zero;


            // Calculate Movement.
            if (_movementState == MovementType.ForcedMovement)
            {
                // Calculate from Forced Movement.
            }
            else if (_movementState == MovementType.FollowingPath)
            {
                // Pathfinding-based Movement.
                Vector3 movementVector = Vector3.zero;

                // If we didn't move, then stop moving (We reached the end of the path).
                if (movementVector == Vector3.zero)
                {
                    _movementState = MovementType.Idle;
                    return Vector3.zero;
                }

                // To-do: Calculate desired movementVector to reach next point in path.
            }
            else if (_movementState == MovementType.DirectInput)
            {
                // Input-based movement.
                return GetProjectedMovementVector() * MovementSpeed;
            }


            return Vector3.zero;
        }
        private Vector3 GetProjectedMovementVector() => GetProjectedVector(_rotationPivot.right * _movementInput.x + _rotationPivot.forward * _movementInput.y);
        private Vector3 GetProjectedVector(Vector3 vector) => Vector3.ProjectOnPlane(vector, Vector3.up).normalized * vector.magnitude;
        private void PerformMovement()
        {
            if (_movementState == MovementType.ForcedMovement)
            {
                AdjustForcedMovement();
                _desiredVelocity = CalculateForcedMovement();
            }
            else if (_isGrounded)
                _desiredVelocity = Vector3.MoveTowards(_desiredVelocity, CalculateDesiredMovement(), MovementSpeed * MovementSpeed * Time.deltaTime);  // We are grounded, so handle movement normally.

            _desiredVelocity += _currentForces;
            _currentForces = Vector3.Lerp(_currentForces, Vector3.zero, _forceDecreaseRate * Time.fixedDeltaTime);

            if (!_isGrounded)
                _desiredVelocity = Vector3.Lerp(_desiredVelocity, Vector3.zero, _airSpeedDecreaseRate * Time.fixedDeltaTime);   // Prevent change in movement direction while in-air, but also decrease towards 0 to simulate resistance.

            Vector3 movementVector = _desiredVelocity;  // Use a separate Vector3 so that we can modify it without affecting our desired velocity.

            // Apply Gravity.
            if (_isGrounded)
                _verticalVelocity = -2.0f;  // Small value to prevent floating above the ground.
            else
                _verticalVelocity += GRAVITY * _gravityMultiplier * Time.fixedDeltaTime;    // Apply gravity acceleration.
            movementVector += Vector3.up * _verticalVelocity;   // Apply current gravity.

            // Perform Movement.
            _characterController.Move(movementVector * Time.fixedDeltaTime);
        }

        private void AdjustForcedMovement()
        {
            if (_movementInput == Vector2.zero)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(GetProjectedMovementVector(), Vector3.up);

            foreach (var kvp in _forcedMovementValues)
            {
                if (kvp.Value.InputRotationRate <= 0.0f)
                    continue;

                Quaternion rotation = Quaternion.RotateTowards(Quaternion.LookRotation(kvp.Value.Direction, Vector3.up), targetRotation, kvp.Value.InputRotationRate * Time.deltaTime);
                _forcedMovementValues[kvp.Key].Direction = rotation * Vector3.forward;
            }
        }
        private Vector3 CalculateForcedMovement()
        {
            Vector3 movement = Vector3.zero;
            foreach(var forcedMovementData in _forcedMovementValues.Values)
            {
                Debug.DrawRay(transform.position, forcedMovementData.Direction * 5.0f, Color.red, 0.25f);

                if (forcedMovementData.TransformToGround)
                    movement += TransformToGround(forcedMovementData.Direction) * forcedMovementData.Speed;
                else
                    movement += forcedMovementData.Direction * forcedMovementData.Speed;
            }

            return movement;
        }
        private Vector3 TransformToGround(Vector3 direction) => throw new System.NotImplementedException();


        private void RechargeBoostFramewise()
        {
            if(_boostCountRemaining == _boostCount)
            {
                // Already at our max boost, so don't continue recharging.
                _boostRechargeProgress = 0.0f;
                return;
            }

            _boostRechargeProgress += BOOST_RECHARGE_RATE * _boostRechargeMultiplier * Time.deltaTime;
            while (_boostRechargeProgress >= 1.0f && _boostCountRemaining < _boostCount)
            {
                ++_boostCountRemaining;
                _boostRechargeProgress -= 1.0f;
            }
        }



        /// <summary>
        ///     Sets our movement input for direct control (E.g. Players controlling with movement keys).
        /// </summary>
        /// <param name="movementInput"></param>
        public void SetMovementInput(Vector2 movementInput)
        {
            this._movementInput = movementInput;

            if (_movementState == MovementType.ForcedMovement)
                return;
            _movementState = movementInput == Vector2.zero ? MovementType.Idle : MovementType.DirectInput;
        }
        /// <summary>
        ///     Sets a movement target for the character to pathfind towards, avoiding static obstacles.
        /// </summary>
        /// <param name="position"> Position in world space to pathfind towards.</param>
        public void SetMovementTarget(Vector3 position)
        {
            _movementState = MovementType.FollowingPath;
            throw new System.NotImplementedException("Moving to Target Logic not implemented");
        }


        public void SetPosition(Vector3 newPosition, bool performObstructionChecks) => AddPosition(newPosition - transform.position, performObstructionChecks);
        public void AddPosition(Vector3 positionOffset, bool performObstructionChecks)
        {
            if (!performObstructionChecks)
            {
                transform.position += positionOffset;
            }
            else
            {
                _characterController.Move(positionOffset);
            }

            _networkTransform.Teleport(transform.position, transform.rotation, this.transform.lossyScale);   // Teleport for instant movement on clients.
        }

        /// <summary>
        ///     Adds a instant knockback force to the character.
        /// </summary>
        public void AddKnockbackForce(Vector3 force, float preventMovementTime = 0.0f)
        {
            _currentForces += force;
            UpdatePreventMovementTime(preventMovementTime);
        }

        /// <summary>
        ///     Adds an instance of forced movement from a given <paramref name="source"/>.
        /// </summary>
        /// <param name="direction"> The direction of forced movement.</param>
        /// <param name="speed"> The speed of the forced movement.</param>
        /// <param name="preventMovementTime"> The time (In Seconds) that the player is unable to perform input.</param>
        /// <param name="inputRotationRate"> The rate of rotation (In Degrees/Second) that the player can rotate the forced movement. Leave at 0 for input to not affect the direction.</param>
        /// <param name="transformToGround"> If true, adjusts the direction to allow for an easier time going up/down slopes when the character is already on them.</param>
        public void AddContinuousForcedMovement(object source, Vector3 direction, float speed, float preventMovementTime = 0.0f, float inputRotationRate = 0.0f, bool transformToGround = false)
        {
            _movementState = MovementType.ForcedMovement;

            // Input Prevention.
            UpdatePreventMovementTime(preventMovementTime);

            // Create and store our forced movement.
            ForcedMovementData forcedMovementData = new ForcedMovementData(direction, speed, inputRotationRate, transformToGround);
            if (!_forcedMovementValues.TryAdd(source, forcedMovementData))
                _forcedMovementValues[source] = forcedMovementData;
        }
        /// <summary>
        ///     Removes all instances of forced movement from a given <paramref name="source"/>.
        /// </summary>
        public void RemoveContinuousForcedMovement(object source)
        {
            _forcedMovementValues.Remove(source);

            if (_forcedMovementValues.Count == 0)
                _movementState = _movementInput == Vector2.zero ? MovementType.Idle : MovementType.DirectInput;
        }

        /// <summary>
        ///     Sets the value of '_allowInputTime' if ServerTime + <paramref name="inputPreventionDuration"/> is greater than its current value.
        /// </summary>
        private void UpdatePreventMovementTime(float inputPreventionDuration)
        {
            if (inputPreventionDuration > 0.0f)
                _allowInputTime = Mathf.Max(_allowInputTime, NetworkManager.Singleton.ServerTime.TimeAsFloat + inputPreventionDuration);
        }


        public void PerformBoost()
        {
            if (_boostCountRemaining <= 0)
            {
                Debug.Log("Out of boosts");
                return;
            }
            --_boostCountRemaining;

            NotifyListenersOfBoostRechargeValuesChanged();
            
            ActionRequestData actionRequestData = new ActionRequestData()
            {
                ActionID = _boostAction.ActionID,
                IActionSourceObjectID = _serverCharacter.NetworkObjectId,
                Direction = _movementInput != Vector2.zero ? GetProjectedMovementVector() : GetProjectedVector(_rotationPivot.forward),
            };
            _serverCharacter.PlayAction_Server(ref actionRequestData);
        }

        private void NotifyListenersOfBoostCountChanged()
        {
            NotifyListenersOfBoostCountChangedOwnerRpc(_boostCount);
            NotifyListenersOfBoostRechargeValuesChanged();
        }
        private void NotifyListenersOfBoostRechargeValuesChanged()
        {
            // Calculate Values.
            float rechargeStartTime = NetworkManager.Singleton.ServerTime.TimeAsFloat;

            float currentTotalBoostPercentage = (_boostCountRemaining + _boostRechargeProgress) / (float)_boostCount;   // Our current percent of boosts in total (E.g. 1.5 boosts / 2 max = 0.75).

            float timeFromZeroToMaxBoosts = BOOST_RECHARGE_DURATION * _boostCount; // How long it would take to recharge all boosts from 0% (In seconds).
            float percentageTillFullBoosts = 1.0f - currentTotalBoostPercentage;
            float rechargeEndTime = rechargeStartTime + (timeFromZeroToMaxBoosts * percentageTillFullBoosts);


            // Notify owner so they can update their UI and such.
            NotifyListenersOfBoostRechargeValuesChangedOwnerRpc(rechargeStartTime, currentTotalBoostPercentage, rechargeEndTime);
        }
        [Rpc(SendTo.Owner)]
        private void NotifyListenersOfBoostRechargeValuesChangedOwnerRpc(float rechargeStartTime, float currentTotalBoostPercentage, float rechargeEndTime) => OnBoostRechargeValuesChanged?.Invoke(this, new OnBoostChargeValuesChangedEventArgs(rechargeStartTime, currentTotalBoostPercentage, rechargeEndTime));
        [Rpc(SendTo.Owner)]
        private void NotifyListenersOfBoostCountChangedOwnerRpc(int newBoostCount) => _onBoostStatsChanged?.Invoke(newBoostCount);


        private void SetBoostAction(ActionDefinition newBoostAction) => _boostActionOverride = newBoostAction;
        [Rpc(SendTo.Server)]
        public void SetBoostActionServerRpc(ActionID actionID) => SetBoostAction(GameDataSource.Instance.GetActionDefinitionByID(actionID));

        private void ClearBoostActionOverride() => _boostActionOverride = null;
        [Rpc(SendTo.Server)]
        public void ClearBoostActionOverrideServerRpc() => ClearBoostActionOverride();



        /// <summary>
        ///     Returns true if the current mvoement mode is unabortable (E.g. A knockback effect).
        /// </summary>
        public bool IsPerformingForcedMovement() => _movementState == MovementType.ForcedMovement;

        /// <summary>
        ///     Returns true if the character is actively moving, false otherwise.
        /// </summary>
        public bool IsMoving() => _movementState != MovementType.Idle;

        /// <summary>
        ///     Cancels any moves that are currently in progress.
        /// </summary>
        public void CancelMove()
        {
            _movementState = MovementType.Idle;
        }

        /// <summary>
        ///     Determines the appropriate MovementStatus for the character. <br></br>
        ///     MovementStatus is used by the client code when animating the character.
        /// </summary>
        private MovementStatus GetMovementStatus(MovementType movementState)
        {
            return movementState switch
            {
                MovementType.Idle => MovementStatus.Idle,
                _ => MovementStatus.Normal,
            };
        }



        public class OnBoostChargeValuesChangedEventArgs : System.EventArgs
        {
            public readonly float ChargeStartTime;
            public readonly float ChargeStartPercentage;
            public readonly float ChargeEndTime;

            public OnBoostChargeValuesChangedEventArgs(float chargeStartTime, float chargeStartPercentage, float chargeEndTime)
            {
                ChargeStartTime = chargeStartTime;
                ChargeStartPercentage = chargeStartPercentage;
                ChargeEndTime = chargeEndTime;
            }
        }
        private class ForcedMovementData    // Class so it's passed ByRef.
        {
            public Vector3 Direction;
            public float Speed;             // Units/Sec

            public float InputRotationRate; // Degrees/Sec
            public bool TransformToGround;


            public ForcedMovementData(Vector3 direction, float speed, float inputRotationRate, bool transformToGround)
            {
                this.Direction = direction;
                this.Speed = speed;
                this.InputRotationRate = inputRotationRate;
                this.TransformToGround = transformToGround;
            }
        }
    }


    /// <summary>
    ///     The current movement state of a ServerCharacter.
    /// </summary>
    public enum MovementType
    {
        Idle = 0,
        DirectInput = 1,
        FollowingPath = 2,
        ForcedMovement = 3,
    }

    /// <summary>
    ///     Describes how a character's movement should be animated
    /// </summary>
    [System.Serializable]
    public enum MovementStatus
    {
        Idle,
        Normal,
    }
}