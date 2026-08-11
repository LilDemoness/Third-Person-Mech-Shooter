using UnityEngine;

namespace Gameplay.Animations.ProceduralAnimations
{
    [System.Serializable]
    public class MinVelocityGaitCondition : GaitCondition
    {
        [SerializeField] private float _minVelocity;

        public override bool TestCondition(MechBody body) => body.Velocity.sqrMagnitude >= (_minVelocity * _minVelocity);

#if UNITY_EDITOR
        public float Editor_MinVelocityAccessor => _minVelocity;

        public override bool Editor_HasConflict(GaitCondition conditionToCheck, out string errorMessage)
        {
            errorMessage = string.Empty;

            switch (conditionToCheck)
            {
                case MinVelocityGaitCondition: return true; // Cannot have two Min Velocity Gait Conditions.
                case MaxVelocityGaitCondition:
                    if (_minVelocity >= (conditionToCheck as MaxVelocityGaitCondition).Editor_MaxVelocityAccessor)
                    {
                        errorMessage = $"Min Velocity Condition: {_minVelocity}; cannot exceed Max Velocity Condition: {(conditionToCheck as MaxVelocityGaitCondition).Editor_MaxVelocityAccessor}";
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