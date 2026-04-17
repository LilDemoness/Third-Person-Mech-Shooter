using UnityEngine;

namespace Gameplay.Passives
{
    [System.Serializable]
    public class DirectionalCondition : System.IEquatable<DirectionalCondition>
    {
        [SerializeField] private Vector3 _localDirection;
        [SerializeField] private float _angle;

        public DirectionalCondition(Vector3 localDirection, float angle)
        {
            this._localDirection = localDirection;
            this._angle = angle;
        }


        public bool Evaluate(Vector3 localDirection) => Vector3.Angle(_localDirection, localDirection) <= _angle;
        public bool Equals(DirectionalCondition other) => (_localDirection, _angle) == (other._localDirection, other._angle);
    }
}