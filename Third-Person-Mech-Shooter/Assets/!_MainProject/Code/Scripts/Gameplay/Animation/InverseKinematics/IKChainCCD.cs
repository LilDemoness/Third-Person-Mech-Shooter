using UnityEngine;

namespace Gameplay.Animations.InverseKinematics
{
    /// <summary>
    ///     An implementation of <see cref="IKIterateBase<>"/> using the CCD algorithm.
    /// </summary>
    public class IKChainCCD : IKIterateBase<IKIterateBaseSetting>
    {
        protected override void SolveIK(float deltaTime, IKIterateBaseSetting setting, Vector3 target)
        {
            int jointSize = setting.Joints.Length; // Joint Count.
            int chainSize = setting.Chain.Count; // Chain Bone Count.

            // Backward Pass.
            for (int ancestor = jointSize - 1; ancestor >= 0; --ancestor)
            {
                // Forward Pass.
                for (int i = ancestor; i < jointSize; ++i)
                {
                    IKBaseSolverInfo solverInfo = setting.SolverInfoList[i];
                    if (solverInfo == null || MathUtils.IsApproximatelyZero(solverInfo.Length))
                        continue; // There is no bone, or its length is invalid (Approx. 0).

                    int head = i;
                    int tail = i + 1;

                    Vector3 currentHead = setting.Chain[head];
                    Vector3 currentEffector = setting.Chain[chainSize - 1];
                    Vector3 headToEffector = currentEffector - currentHead;
                    Vector3 headToTarget = target - currentHead;

                    if (headToTarget.IsApproximatelyZero() || headToEffector.IsApproximatelyZero())
                        continue; // One of our distances is approximately zero, and therefore we shouldn't iterate (We're either at the target pos, or are currently working on the tip of the final bone).

                    // Calculate our desired rotation.
                    Quaternion toRot = Quaternion.FromToRotation(headToEffector.normalized, headToTarget.normalized); // Might be Quaternion.LookRotation()
                    Vector3 toTail = setting.Chain[tail] - currentHead;

                    // Rotate the joint.
                    Vector3 targetPos = currentHead + (toRot * toTail.normalized * setting.SolverInfoList[i].Length);
                    setting.UpdateChainCoordinateForward(tail, targetPos);

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
}