using UnityEngine;

public class ProcMovementTester : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform _target;

    [Space(5)]
    [SerializeField] private bool _moveToTarget = true;
    [SerializeField] private float _moveToTargetSpeed = 5.0f;

    [SerializeField] private bool _rotateToTarget = true;
    [SerializeField] private float _rotationRate = 90.0f;


    [Header("Free Movement Settings")]
    [SerializeField] private bool _moveForwards = false;
    [SerializeField] private float _moveForwardSpeed = 5.0f;

    [Space(5)]
    [SerializeField] private bool _rotateCounterClockwise = false;
    [SerializeField] private float _counterClockwiseRotationRate = 90.0f;


    [Header("Head Rotation Test")]
    [SerializeField] private bool _rotateHeadToTarget = true;
    [SerializeField] private Transform _headRoot;
    [SerializeField] private float _headRotationRate = 270.0f;


    private void Update()
    {
        Quaternion rotationToTarget = Quaternion.LookRotation((_target.position - transform.position).normalized);

        // Target.
        if (_moveToTarget)
            transform.position = Vector3.MoveTowards(transform.position, new Vector3(_target.position.x, transform.position.y, _target.position.z), _moveToTargetSpeed * Time.deltaTime);
        if (_rotateToTarget)
            transform.rotation = Quaternion.RotateTowards(transform.rotation, rotationToTarget, _rotationRate * Time.deltaTime);


        // Free Movement.
        if (_moveForwards)
            transform.position += transform.forward * _moveForwardSpeed * Time.deltaTime;
        if (_rotateCounterClockwise)
            transform.rotation = transform.rotation * Quaternion.AngleAxis(_counterClockwiseRotationRate * Time.deltaTime, Vector3.up);


        // Head rotation test.
        if (_rotateHeadToTarget && _headRoot != null)
            _headRoot.rotation = Quaternion.RotateTowards(_headRoot.rotation, rotationToTarget, _headRotationRate * Time.deltaTime);
    }
}
