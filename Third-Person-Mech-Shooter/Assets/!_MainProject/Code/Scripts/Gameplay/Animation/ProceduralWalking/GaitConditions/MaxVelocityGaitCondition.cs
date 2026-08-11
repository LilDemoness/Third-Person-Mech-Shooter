using UnityEngine;

namespace Gameplay.Animations.ProceduralAnimations
{
    public class MaxVelocityGaitCondition : GaitCondition
    {
        [SerializeField] private float _maxVelocity;

        public override bool TestCondition(MechBody body) => body.Velocity.sqrMagnitude <= (_maxVelocity * _maxVelocity);

#if UNITY_EDITOR
        public float Editor_MaxVelocityAccessor => _maxVelocity;

        public override bool Editor_HasConflict(GaitCondition conditionToCheck, out string errorMessage)
        {
            errorMessage = string.Empty;

            switch (conditionToCheck)
            {
                case MaxVelocityGaitCondition: return true; // Cannot have two Max Velocity Gait Conditions.
                case MinVelocityGaitCondition:
                    if (_maxVelocity >= (conditionToCheck as MaxVelocityGaitCondition).Editor_MaxVelocityAccessor)
                    {
                        errorMessage = $"Max Velocity Condition: {_maxVelocity}; cannot be below Min Velocity Condition: {(conditionToCheck as MinVelocityGaitCondition).Editor_MinVelocityAccessor}";
                        return true;
                    }
                    else
                        return false;
                default: return false;  // No conflicts.
            };
        }

#endif
    }
}