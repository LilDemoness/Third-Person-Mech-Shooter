using Gameplay.Animations.InverseKinematics;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Gameplay.Animations.ProceduralAnimations
{
    public class MechBody : MonoBehaviour
    {
        public MechLeg[] Legs;
        private Vector3 _previousPosition;
        private Quaternion _previousRotation;

        public LayerMask GroundLayers;

        public Gait Gait { get; private set; }
        public Gait DefaultGait;
        public ConditionalGait[] ConditionalGaits;  // Priority from Index 0 (Highest) to Index n (Lowest).

        public LerpGait StationaryLerpGait => new LerpGait(1.1f, 0.25f);
        public LerpGait MovingLerpGait => new LerpGait(1.1f, 0.8f);

        public Vector3 Velocity { get; private set; }
        public Vector3 RotationalVelocity { get; private set; }

        public int LegCount { get; }
        public bool IsGrounded { get; }
        public bool IsMoving { get; }

        public float MaxSpeed => _moveSpeed;
        public float CurrentSpeedFraction => Velocity.magnitude / MaxSpeed;

        public LerpGait GetLerpedGait() => LerpGait.Lerp(StationaryLerpGait, MovingLerpGait, CurrentSpeedFraction);


        [Header("Testing")]
        [SerializeField] private Transform _target;
        [SerializeField] private bool _moveToTarget = true;
        [SerializeField] private float _moveSpeed = 5.0f;
        [SerializeField] private bool _rotateToTarget = true;
        [SerializeField] private float _rotationRate = 270.0f;


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
            if (_moveToTarget)
                transform.position = Vector3.MoveTowards(transform.position, new Vector3(_target.position.x, transform.position.y, _target.position.z), MaxSpeed * Time.deltaTime);
            if (_rotateToTarget)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation((_target.position - transform.position).normalized), _rotationRate * Time.deltaTime);


            Velocity = (transform.position - _previousPosition) / Time.deltaTime;
            RotationalVelocity = (Quaternion.Inverse(_previousRotation) * transform.rotation).eulerAngles / Time.deltaTime;

            UpdateGait();

            foreach(MechLeg leg in Legs) leg.UpdateMemory();
            foreach(MechLeg leg in Legs) leg.UpdateMovement(Time.deltaTime);

            _previousPosition = transform.position;
            _previousRotation = transform.rotation;
        }
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
            foreach(MechLeg leg in Legs)
                leg.DrawGizmos(this);
        }
    }
}