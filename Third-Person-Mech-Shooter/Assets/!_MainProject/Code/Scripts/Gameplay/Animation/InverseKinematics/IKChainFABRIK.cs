using UnityEngine;

namespace Gameplay.Animations.InverseKinematics
{
    public class IKChainFABRIK : IKIterateBase<IKIterateBaseSetting>
    {
        protected override void SolveIK(float deltaTime, IKIterateBaseSetting setting, Vector3 targetPos)
        {
            int jointCount = setting.Joints.Length;
            if (jointCount == 0)
                return; // If we have no joints, we can't solve anyway, so exit out to avoid an IndexOutOfRange exception too.

            // ----- Backwards iteration (To Effector) -----

            // Move the head of the first bone to the target to allow for our FABRIK repositioning.
            IKBaseSolverInfo solverInfo = setting.SolverInfoList[jointCount - 1];
            if (solverInfo != null && !MathUtils.IsApproximatelyZero(solverInfo.Length))
                setting.UpdateChainCoordinateBackward(jointCount, targetPos);

            // Iterate through all our bones, tail to root.
            for (int i = jointCount - 1; i >= 0; --i)
            {
                solverInfo = setting.SolverInfoList[i];
                if (solverInfo == null || MathUtils.IsApproximatelyZero(solverInfo.Length))
                    continue; // There is no bone, or its length is invalid (Approx. 0).

                int head = i;
                int tail = i + 1;

                // Update our bones by moving the head of the current bone towards their tail, pointing back to their current position.
                setting.UpdateChainCoordinateBackward(head, setting.Chain[tail] + (setting.Chain[head] - setting.Chain[tail]).normalized * solverInfo.Length);

                // Apply rotation axis locks.
                if (setting.Joints[head].RotationAxis != RotationAxis.Unrestricted)
                    setting.UpdateChainCoordinateBackward(head, setting.Chain[tail] + setting.Joints[head].GetProjectedRotation(solverInfo.CurrentGRest, setting.Chain[head] - setting.Chain[tail]));

                // Apply rotation axis degree limitations.
                if (setting.Joints[head].Limitation != null)
                    setting.UpdateChainCoordinateBackward(head, setting.Chain[tail] + setting.Joints[head].GetLimitedRotation(solverInfo.CurrentGRest, setting.Chain[head] - setting.Chain[tail], solverInfo.ForwardVector));
            }


            // ----- Forwards iteration (To Root) -----

            // Move the head of the first bone to (0,0,0) to allow for our FABRIK repositioning.
            // This works since the values in Chain are all relative to the root bone's position.
            solverInfo = setting.SolverInfoList[0];
            if (solverInfo != null && !MathUtils.IsApproximatelyZero(solverInfo.Length))
                setting.UpdateChainCoordinateForward(0, Vector3.zero);

            // Iterate through all our bones, root to tail.
            for (int i = 0; i < jointCount; ++i)
            {
                solverInfo = setting.SolverInfoList[i];
                if (solverInfo == null || MathUtils.IsApproximatelyZero(solverInfo.Length))
                    continue; // There is no bone, or its length is invalid (Approx. 0).

                int head = i;
                int tail = i + 1;

                // Update our bones by moving the tail of the current bone towards their head, pointing back to their current position.
                setting.UpdateChainCoordinateForward(tail, setting.Chain[head] + (setting.Chain[tail] - setting.Chain[head]).normalized * solverInfo.Length);

                // Apply rotation axis locks.
                if (setting.Joints[head].RotationAxis != RotationAxis.Unrestricted)
                    setting.UpdateChainCoordinateForward(tail, setting.Chain[head] + setting.Joints[head].GetProjectedRotation(solverInfo.CurrentGRest, setting.Chain[tail] - setting.Chain[head]));

                // Apply rotation axis degree limitations.
                if (setting.Joints[head].Limitation != null)
                    setting.UpdateChainCoordinateForward(tail, setting.Chain[head] + setting.Joints[head].GetLimitedRotation(solverInfo.CurrentGRest, setting.Chain[tail] - setting.Chain[head], solverInfo.ForwardVector));
            }
        }
    }
}