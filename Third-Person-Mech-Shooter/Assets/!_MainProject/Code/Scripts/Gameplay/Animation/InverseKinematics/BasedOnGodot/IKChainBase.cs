using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Gameplay.Animations.InverseKinematics
{

    /// <summary>
    ///     A base class for Chain IK Solvers.
    /// </summary>
    public abstract class IKChainBase<TSetting, TBoneJoint> : IKBase<TSetting> where TSetting : IKChainBaseSetting<TBoneJoint> where TBoneJoint : BoneJoint
    {
        protected override void UpdateJoints(int index)
        {
            // Mark ourselves as dirty so we can update ourselves.
            MakeSimulationDirty(index);

            foreach(TBoneJoint boneJoint in Settings[index].Joints)
            {
                boneJoint.UpdateRest();
            }
        }
        protected override void MakeAllJointsDirty()
        {
            for (int i = 0; i < Settings.Count; ++i)
                UpdateJoints(i);
        }
    }


    [System.Serializable]
    public class IKChainBaseSetting<TBoneJoint> : IKBaseSetting where TBoneJoint : BoneJoint
    {
#if UNITY_EDITOR
        [SerializeField, HideInInspector] private Transform _rootBone; // Editor Only: Only to be accessed by Inspector scripts.
        [SerializeField, HideInInspector] private Transform _endBone; // Editor Only: Only to be accessed by Inspector scripts.
#endif

        public TBoneJoint RootBone => Joints[0];
        public TBoneJoint EndBone => Joints[_jointCount - 1];

        // For making a virtual end joint.
        [SerializeField] public bool ExtendEndBone = false;
        [SerializeField] public BoneDirection EndBoneDirection = BoneDirection.FromParent;
        [SerializeField] public float EndBoneLength = 0.0f;


        [SerializeField] public TBoneJoint[] Joints = new TBoneJoint[0];
        private int _jointCount;
        [HideInInspector] public IKBaseSolverInfo[] SolverInfoList = new IKBaseSolverInfo[0];
        [HideInInspector] public Vector3[] Chain = new Vector3[0];

        public void UpdateChainCoordinate(int index, Vector3 position)
        {
            // Don't update if the position is the same as the current position.
            // We're not using sqrMagnitude as we need more precision that it provides.
            if (MathUtils.IsApproximatelyZero((Chain[index] - position).sqrMagnitude))
                return;

            // Allow flipping.
            Chain[index] = position;
            CacheCurrentVector(index);
        }
        public void UpdateChainCoordinateBackward(int index, Vector3 position)
        {
            // Don't update if the position is the same as the current position.
            // We're not using sqrMagnitude as we need more precision that it provides.
            if (MathUtils.IsApproximatelyZero((Chain[index] - position).sqrMagnitude))
                return;

            // Prevent flipping from backwards.
            Vector3 result = position;
            int head = index - 1;
            int tail = index;
            if (head >= 0 && head < SolverInfoList.Length)
            {
                IKBaseSolverInfo solverInfo = SolverInfoList[head];
                if (solverInfo != null)
                {
                    Vector3 oldHeadToTail = solverInfo.CurrentVector;
                    Vector3 newHeadToTail = (result - Chain[head]).normalized;

                    if (MathUtils.IsApproximatelyEqual(Vector3.Dot(oldHeadToTail, newHeadToTail), -1.0f))
                        // We've flipped.
                    {
                        Chain[tail] = Chain[head] + oldHeadToTail * solverInfo.Length; // Revert the flip.
                        return; // Since we reverted, there is no change since last frame & we don't need to update the cached values.
                    }
                }
            }

            Chain[index] = result;
            CacheCurrentVector(index);
        }
        public void UpdateChainCoordinateForward(int index, Vector3 targetPosition)
        {
            // Don't update if the target position is the same as the current position.
            // We're using magnitude over sqrMagnitude as we require more precision.
            if (MathUtils.IsApproximatelyZero((Chain[index] - targetPosition).magnitude))
                return;

            // Prevent flipping from forwards.
            int head = index;
            int tail = index + 1;
            if (tail >= 0 && tail < SolverInfoList.Length)
            {
                IKBaseSolverInfo solverInfo = SolverInfoList[head];
                if (solverInfo != null)
                {
                    Vector3 oldHeadToTail = solverInfo.CurrentVector;
                    Vector3 newHeadToTail = (Chain[tail] - targetPosition).normalized;

                    if (MathUtils.IsApproximatelyEqual(Vector3.Dot(oldHeadToTail, newHeadToTail), -1.0f))
                        // We've flipped.
                    {
                        Chain[head] = Chain[tail] - oldHeadToTail * solverInfo.Length; // Revert.
                        return; // We aren't saving our changes, so we don't need to re-cache.
                    }
                }
            }

            Chain[index] = targetPosition;
            CacheCurrentVector(index);
        }


        public void CacheCurrentVector(int index)
        {
            int currentHead = index - 1;
            int currentTail = index;

            if (currentHead >= 0)
            {
                IKBaseSolverInfo solverInfo = SolverInfoList[currentHead];
                if (solverInfo != null)
                    solverInfo.CurrentVector = (Chain[currentTail] - Chain[currentHead]).normalized;
            }

            currentHead = index;
            currentTail = index - 1;
            if (currentTail < Chain.Length)
            {
                IKBaseSolverInfo solverInfo = SolverInfoList[currentHead];
                if (solverInfo != null)
                    solverInfo.CurrentVector = (Chain[currentTail] - Chain[currentHead]).normalized;
            }
        }
        public void CacheCurrentVectors()
        {
            for (int i = 0; i < Joints.Length; ++i)
            {
                IKBaseSolverInfo solverInfo = SolverInfoList[i];
                if (solverInfo == null)
                    continue;

                solverInfo.CurrentVector = (Chain[i + 1] - Chain[i]).normalized;
            }
        }

        public void InitCurrentJointRotations()
        {
            if (RootBone == null || RootBone.Bone == null)
                return; // Unset values.

            Quaternion parentGPose = RootBone.Bone.rotation;

            for (int i = 0; i < Joints.Length; ++i)
            {
                IKBaseSolverInfo solverInfo = SolverInfoList[i];
                if (solverInfo == null)
                    continue;

                solverInfo.CurrentLRest = Joints[i].Bone.localRotation;
                solverInfo.CurrentGRest = (parentGPose * solverInfo.CurrentLRest).normalized;

                solverInfo.CurrentLPose = Joints[i].Bone.localRotation;
                solverInfo.CurrentGPose = (parentGPose * solverInfo.CurrentLPose).normalized;

                parentGPose = solverInfo.CurrentGPose;
            }

            CacheCurrentVectors();
        }
    }
    public class IKChainBaseSetting : IKChainBaseSetting<BoneJoint>
    { }
}