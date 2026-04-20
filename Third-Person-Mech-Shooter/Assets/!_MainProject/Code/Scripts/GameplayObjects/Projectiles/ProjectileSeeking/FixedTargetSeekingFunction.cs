using UnityEngine;

namespace Gameplay.GameplayObjects.Projectiles.Seeking
{
    [System.Serializable]
    public class FixedTargetSeekingFunction : SeekingFunction
    {
        private Transform m_target;

        private Character.ServerCharacter m_targetServerCharacter; // If set, intangibility checks are performed.
        private bool m_seekingTargetIntangibilityState; // The intantibility that 'm_targetServerCharacter' must be to seek towards the target.


        public FixedTargetSeekingFunction(FixedTargetSeekingFunction other) { }
        public FixedTargetSeekingFunction Setup(Transform owner, Transform target)
        {
            this.m_target = target;

            if (owner.TryGetComponent<Character.ServerCharacter>(out Character.ServerCharacter ownerCharacter) && target.TryGetComponent<Character.ServerCharacter>(out this.m_targetServerCharacter))
                m_seekingTargetIntangibilityState = ownerCharacter.IsIntangible.Value;

            return this;
        }

        public override bool TryGetTargetDirection(Vector3 currentPosition, out Vector3 seekingDirection)
        {
            if (m_target != null && (m_targetServerCharacter == null || m_targetServerCharacter.IsIntangible.Value == m_seekingTargetIntangibilityState))
            {
                seekingDirection = (m_target.position - currentPosition).normalized;
                return true;
            }

            seekingDirection = Vector3.zero;
            return false;
        }
    }
}