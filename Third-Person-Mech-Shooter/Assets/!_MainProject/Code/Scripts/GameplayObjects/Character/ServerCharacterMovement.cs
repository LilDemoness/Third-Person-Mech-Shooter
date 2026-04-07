using UnityEngine;
using Unity.Netcode;
using Gameplay.GameplayObjects.Character.Statistics;
using Gameplay.Actions.Definitions;
using Gameplay.Actions;

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


        private float _movementSpeed => _characterStats.GetStatisticValue(Statistic.MovementSpeed);


        [Header("Boost Settings")]
        [SerializeField] private ActionDefinition _boostAction;

        private int _boostCount { get; set; }


        const float BOOST_RECHARGE_DURATION = 2.0f; // Time in Seconds.
        const float BOOST_RECHARGE_RATE = 1.0f / BOOST_RECHARGE_DURATION;  // Percentage / Second.
        
        private float _boostRechargeMultiplier => _characterStats.GetStatisticValue(Statistic.BoostRechargeMultiplier);
        private int _boostCountRemaining;
        private float _boostRechargeProgress;


        public event System.Action<int> OnBoostStatsChanged;
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

            // Subscribe to events.
            _characterStats.OnAnyStatisticChanged += CharacterStats_OnAnyStatisticChanged;
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
                _characterStats.OnAnyStatisticChanged += CharacterStats_OnAnyStatisticChanged;
            }
        }

        private void CharacterStats_OnAnyStatisticChanged()
        {
            int boostCount = Mathf.CeilToInt(_characterStats.GetStatisticValue(Statistic.BoostCount));
            if (boostCount != _boostCount)
            {
                _boostCount = boostCount;
                _boostCountRemaining = _boostCount;
                OnBoostStatsChanged?.Invoke(boostCount);
            }
        }



        public void SetPositionAndRotation(Vector3 newPosition, Quaternion newRotation)
        {
            this._characterController.enabled = false;  // Disable the CharacterController to facilitate teleportation.

            transform.position = newPosition;
            if (IsSpawned) // Check facilitates initial position setting for spawning players.
            {
                this.GetComponent<Unity.Netcode.Components.NetworkTransform>().Teleport(newPosition, transform.rotation, this.transform.lossyScale);   // Teleport for instant movement in clients.
            
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
                return GetProjectedMovementVector() * _movementSpeed;
            }


            return Vector3.zero;
        }
        private Vector3 GetProjectedMovementVector() => GetProjectedVector(_rotationPivot.right * _movementInput.x + _rotationPivot.forward * _movementInput.y);
        private Vector3 GetProjectedVector(Vector3 vector) => Vector3.ProjectOnPlane(vector, Vector3.up).normalized * vector.magnitude;
        private void PerformMovement()
        {
            if (_isGrounded)
                _desiredVelocity = CalculateDesiredMovement();  // We are grounded, so handle movement normally.
            else
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
            if (movementInput == Vector2.zero)
            {
                _movementState = MovementType.Idle;
                return;
            }

            _movementState = MovementType.DirectInput;
            this._movementInput = movementInput;
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


        public void PerformBoost()
        {
            if (_boostCountRemaining <= 0)
                return;
            --_boostCountRemaining;

            NotifyListenersOfBoostRechargeValuesChanged();
            
            ActionRequestData actionRequestData = new ActionRequestData()
            {
                ActionID = _boostAction.ActionID,
                IActionSourceObjectID = _serverCharacter.NetworkObjectId,
                Direction = _movementInput != Vector2.zero ? GetProjectedMovementVector() : GetProjectedVector(_characterController.transform.forward),
            };
            _serverCharacter.ActionPlayer.PlayAction(ref actionRequestData);
        }
        private void NotifyListenersOfBoostRechargeValuesChanged()
        {
            float rechargeStartTime = NetworkManager.Singleton.ServerTime.TimeAsFloat;

            float currentTotalBoostsPercentage = (_boostCountRemaining + _boostRechargeProgress) / (float)_boostCount;   // Our current percent of boosts in total (E.g. 1.5 boosts / 2 max = 0.75).

            float timeFromZeroToMaxBoosts = BOOST_RECHARGE_DURATION * _boostCount; // How long it would take to recharge all boosts from 0% (In seconds).
            float percentageTillFullBoosts = 1.0f - currentTotalBoostsPercentage;
            float rechargeEndTime = rechargeStartTime + (timeFromZeroToMaxBoosts * percentageTillFullBoosts);

            OnBoostRechargeValuesChanged?.Invoke(this, new OnBoostChargeValuesChangedEventArgs(rechargeStartTime, currentTotalBoostsPercentage, rechargeEndTime));
        }


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