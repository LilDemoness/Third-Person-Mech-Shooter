using Gameplay.Animations.ProceduralAnimations;
using UnityEngine;
using UserInput;

public class TempCharacterController : MonoBehaviour
{
    /* Requirements:
     * - WASD Movement based on RotationPivot direction.
     * - Rotation Pivot based rotation.
     * - Individual rotation settings for body, head, etc, to catch up to the Rotation Pivot.
     */


    private Vector3 _desiredVelocity;
    [SerializeField] private MechBody _bodyRef;
    [SerializeField] private Transform _cameraParent;


    [Header("Movement")]
    [SerializeField] private float _moveSpeed;
    [SerializeField] private float _moveAcceleration;
    [SerializeField] private CharacterController _characterController;


    [Header("Rotation")]
    [SerializeField] private Transform _rotationPivot;
    [SerializeField] private FloatOptionValue _mnkHorizontalSensitivity;
    [SerializeField] private FloatOptionValue _mnkVerticalSensitivity;

    private Vector2 _rotation;
    private const float MIN_VERTICAL_ROTATION = -45.0f;
    private const float MAX_VERTICAL_ROTATION = 70.0f;


    [Header("In-Air Settings")]
    [SerializeField] private float _gravityMultiplier = 1.0f;
    private const float GRAVITY = -9.81f;
    private float _verticalVelocity;

    [SerializeField] private float _airSpeedDecreaseRate = 1.0f;


    [Header("Ground Check")]
    [SerializeField] private float _groundCheckRadius = 0.5f;
    [SerializeField] private LayerMask _groundLayers;
    private bool _isGrounded;


    [Header("GFX Rotation")]
    [SerializeField] private Transform _rootTransform;
    private Quaternion _rootRotationOffset; // Offset rotation for the 'rootTransform' to rotate properly relative to the rotation pivot;
    [SerializeField] private float _rootRotationRate = 90.0f;

    [Space(5)]
    [SerializeField] private Transform _neckRootTransform;
    private Quaternion _neckRotationOffset; // Offset rotation for the 'defaultNeckRotation' to rotate properly relative to the rotation pivot;
    [SerializeField] private float _neckRotationRate = 270.0f;
    [SerializeField] private float _neckRotationLimit = 90.0f;


    private void Awake()
    {
        _rootRotationOffset = Quaternion.Inverse(Quaternion.Inverse(_rotationPivot.rotation) * _rootTransform.rotation);
        _neckRotationOffset = Quaternion.Inverse(Quaternion.Inverse(_rotationPivot.rotation) * _neckRootTransform.rotation);

        _mnkHorizontalSensitivity.Init();
        _mnkVerticalSensitivity.Init();

        Cursor.lockState = CursorLockMode.Locked;
    }
    private void Update()
    {
        PerformRotation();
    }
    private void FixedUpdate()
    {
        CheckIsGrounded();
        PerformMovement();
    }

    private void CheckIsGrounded() => _isGrounded = Physics.CheckSphere(transform.position, _groundCheckRadius, _groundLayers, QueryTriggerInteraction.Ignore);

    private Vector3 CalculateDesiredMovement() => ClientInput.MovementInput != Vector2.zero ? GetProjectedMovementVector() * _moveSpeed : Vector3.zero;
    private Vector3 GetProjectedMovementVector() => GetProjectedVector(_rotationPivot.right * ClientInput.MovementInput.x + _rotationPivot.forward * ClientInput.MovementInput.y);
    private Vector3 GetProjectedVector(Vector3 vector) => Vector3.ProjectOnPlane(vector, Vector3.up).normalized * vector.magnitude;

    private void PerformMovement()
    {
        // Update desired velocity.
        if (_isGrounded)
            // Allow movement while grounded.
            _desiredVelocity = Vector3.MoveTowards(_desiredVelocity, CalculateDesiredMovement(), _moveAcceleration * Time.fixedDeltaTime);
        else
            // Apply drag while in the air.
            _desiredVelocity = Vector3.Lerp(_desiredVelocity, Vector3.zero, _airSpeedDecreaseRate * Time.fixedDeltaTime);

        
        // We use a separate Vector3 so we can modify it without affecting our desiredVelocity.
        Vector3 movementVector = _desiredVelocity;

        // Apply gravity.
        if (_isGrounded)
            _verticalVelocity = -2.0f;
        else
            _verticalVelocity += GRAVITY * _gravityMultiplier * Time.fixedDeltaTime;
        movementVector += Vector3.up * _verticalVelocity;

        // Perform movement.
        _characterController.Move(movementVector * Time.fixedDeltaTime);
    }


    private void PerformRotation()
    {
        Vector3 cameraInput = ClientInput.LookInput;
        _rotation.x += cameraInput.y * _mnkVerticalSensitivity.Value * Time.deltaTime;
        _rotation.y += cameraInput.x * _mnkHorizontalSensitivity.Value * Time.deltaTime;

        _rotation.x = Mathf.Clamp(_rotation.x, MIN_VERTICAL_ROTATION, MAX_VERTICAL_ROTATION);

        // Apply our rotation, accounting for the angle of the current slope & our rotation relative to that.
        // 1 - Account for the grounded normal of the mech (For slopes/inclines).
        _rotationPivot.parent.up = _bodyRef.GroundedNormal;
        // 2 - Rotate locally so that our rotation input makes sense in the given context.
        _rotationPivot.localRotation = Quaternion.Euler(0.0f, _rotation.y, 0.0f);
        _rotationPivot.GetChild(0).localRotation = Quaternion.Euler(_rotation.x, 0.0f, 0.0f);


        // Rotate root GFX.
        Quaternion rootRot = _rootTransform.rotation;
        Quaternion neckRot = _neckRootTransform.rotation;


        // Rotate GFX.
        _rootTransform.rotation = Quaternion.RotateTowards(rootRot, Quaternion.Euler(rootRot.eulerAngles.x, _rotation.y, rootRot.eulerAngles.z) * _rootRotationOffset, _rootRotationRate * Time.deltaTime);
        _neckRootTransform.rotation = Quaternion.RotateTowards(neckRot, _rotationPivot.rotation * _neckRotationOffset, _neckRotationRate * Time.deltaTime);

        // Constrain the neck's rotation.
        _neckRootTransform.localRotation = Quaternion.RotateTowards(Quaternion.identity, _neckRootTransform.localRotation, _neckRotationLimit);
    }
}