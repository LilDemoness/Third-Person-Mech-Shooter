using UnityEngine;

namespace Gameplay.Animations.InverseKinematics
{
    /// <summary>
    ///     Base class for Joint Limitations which can influence <see cref="IKChainBase<>"/> instances.
    /// </summary>
    /// <remarks>
    ///     For now, these are made with default class instances.
    ///     In the future this may change (E.g. To MonoBehaviours or ScriptableObjects).
    /// </remarks>
    [System.Serializable]
    public abstract class JointLimitation
    {
        public Vector3 Solve(Vector3 localForward, Vector3 localRight, Quaternion rotationOffset, Vector3 localCurrent)
        {
            Quaternion space = MakeSpace(localForward, localRight, rotationOffset);
            Vector3 dir = localCurrent.normalized;
            return space * Solve(Quaternion.Inverse(space) * dir);
        }
        protected abstract Vector3 Solve(Vector3 direction);


        public virtual Quaternion MakeSpace(Vector3 localForward, Vector3 localRight, Quaternion rotationOffset)
        {
            const float ALMOST_ONE = 1.0f - float.Epsilon;

            // Default to interpreting the forward vector as the +Y axis.
            Vector3 axisY = localForward.normalized;
            Vector3 axisX = localRight.normalized;

            if (axisX.IsApproximatelyZero() || Mathf.Abs(Vector3.Dot(axisX, axisY)) > ALMOST_ONE)
                return Quaternion.FromToRotation(axisY, Vector3.up) * rotationOffset;

            Vector3 axisZ = Vector3.Cross(axisX, axisY).normalized;
            axisX = Vector3.Cross(axisY, axisZ).normalized;
            return Quaternion.LookRotation(axisZ, axisY) * rotationOffset; // We may need to change the first quaternion creation.
        }


        public abstract void DrawLimitationGizmos(Transform joint, Quaternion jointRotation, Vector3 forward, Vector3 right, Vector3 up);
    }
}