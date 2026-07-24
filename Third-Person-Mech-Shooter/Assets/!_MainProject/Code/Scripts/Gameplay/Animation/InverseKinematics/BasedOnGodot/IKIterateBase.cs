using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Animations.InverseKinematics
{
    /// <summary>
    ///     An base type for iteratively updating <see cref="IKChainBase<>"/> instances.
    /// </summary>
    public abstract class IKIterateBase<TSetting> : IKChainBase<TSetting, IKIterateBaseJoint> where TSetting : IKIterateBaseSetting
    {
        [SerializeField] protected int _maxIterations = 4;
        [SerializeField] protected float _minDistance = 0.001f; // If the distance between the end joint and the target falls beneath this value, then finish the iteration.
        protected float _sqrMinDistance; // Cached.
        [SerializeField] protected float _angularDeltaLimit = 2.0f * Mathf.Deg2Rad; // If the delta is too large, the results before and after iterating can change significantly, and divergence of calculations can easily occur.
        
        [SerializeField] protected bool _deterministic = false;


        protected override void InitJoints(int index)
        {
            IKIterateBaseSetting setting = Settings[index];
            if (setting == null)
                return;

            if (setting.SimulationDirty)
            {
                ClearJoints(index);
                setting.InitJoints(_mutableBoneAxes);
                setting.SimulationDirty = false;
            }
            else if (_deterministic)
                // To make our calculation deterministic, re-initialise our Chain so that we're operating from the default again (Bone Transform Position is reset at the start of each Update, so we don't need to do this here).
                setting.InitJoints(_mutableBoneAxes);
        }
        public void ClearJoints(int index)
        {
            IKIterateBaseSetting setting = Settings[index];
            if (setting == null)
                return;

            UnsubscribeFromJointLimitationEvents(index);
            for (int i = 0; i < setting.SolverInfoList.Length; ++i)
                setting.SolverInfoList[i] = null;
            
            setting.SolverInfoList.Clear();
            setting.SolverInfoList = new IKBaseSolverInfo[setting.Joints.Length];
            for(int i = 0; i < setting.SolverInfoList.Length; ++i)
                setting.SolverInfoList[i] = new();

            SubscribeToJointLimitationEvents(index);
        }

        protected void SubscribeToJointLimitationEvents(int index)
        {
            // When the joint limination is changed in the inspector, we want to trigger our Update Limitation function.
            /*IKIterateBaseSetting setting = _iterateSettings[index];
            for (int i = 0; i < setting.SolverInfoList.Length; ++i)
                if (setting.JointSettings[i].Limitation != null)
                    setting.JointSettings[i].Limitation.OnChanged += () => UpdateJointLimitation(index, i);*/
        }
        protected void UnsubscribeFromJointLimitationEvents(int index)
        {
            /*IKIterateBaseSetting setting = _iterateSettings[index];
            for (int i = 0; i < setting.SolverInfoList.Length; ++i)
                if (setting.JointSettings[i].Limitation != null)
                    setting.JointSettings[i].Limitation.OnChanged -= () => UpdateJointLimitation(index, i);*/
        }
        protected void UpdateJointLimitation(int index, int joint) => Settings[index].HasSimulated = false;


        protected override void UpdateBoneAxis(int index)
        {
            IKIterateBaseSetting setting = Settings[index];
            int len = setting.SolverInfoList.Length - 1;

            for (int i = 0; i < len; ++i)
            {
                IKIterateBaseJoint jointSetting = setting.Joints[i];
                if (jointSetting == null || setting.SolverInfoList[i] == null)
                    continue; // No data to update.
                
                Vector3 axis = setting.Joints[i + 1].Bone.localPosition;
                if (axis.IsApproximatelyZero())
                    continue;

                setting.SolverInfoList[i].ForwardVector = axis.normalized.SnapToPlane(jointSetting.GetRotationAxisVector());
                setting.SolverInfoList[i].Length = axis.magnitude;
            }

            // Add our virtual end bone (If desired & valid).
            if (setting.ExtendEndBone && len >= 0)
                // We're wishing to extend the end bone, and we have at least 1 bone.
            {
                IKIterateBaseJoint jointSetting = setting.Joints[len];
                if (jointSetting != null && setting.SolverInfoList[len] != null)
                    // Passed validity check.
                {
                    Vector3 axis = setting.EndBone.GetBoneAxis(setting.EndBoneDirection, _mutableBoneAxes);
                    if (!axis.IsApproximatelyZero())
                        // Axis is valid (Bone has a length).
                    {
                        setting.SolverInfoList[len].ForwardVector = axis.normalized.SnapToPlane(jointSetting.GetRotationAxisVector());
                        setting.SolverInfoList[len].Length = setting.EndBoneLength;
                    }
                }
            }
        }


        protected override void MakeSimulationDirty(int index)
        {
            IKIterateBaseSetting setting = Settings[index];
            if (setting == null)
                return;

            setting.SimulationDirty = true;
        }


        public override void ProcessIK(float deltaTime)
        {
            _sqrMinDistance = _minDistance * _minDistance;
            for (int i = 0; i < Settings.Count; ++i)
                Settings[i].ResetJoints();

            for (int i = 0; i < Settings.Count; ++i)
            {
                InitJoints(i);

                Transform target = Settings[i].Target;
                if (target == null)
                    continue; // No target. Abort.

                Settings[i].CacheCurrentJointRotations(); // Iterate over first to detect parent (Outside of the chain) bone pose changes.

                Vector3 destination = /*_cachedSpace * */target.position;
                ProcessJoints(deltaTime, Settings[i], destination);
            }
        }
        protected void ProcessJoints(float delta, IKIterateBaseSetting setting, Vector3 targetPos)
        {
            float sqrDstToTarget = float.PositiveInfinity;
            int iterationCount = 0;

            // To prevent rapid oscillation, if we have processed at least once and the target was reached, don't iterate.
            if (setting.HasSimulated)
                sqrDstToTarget = (setting.Chain[setting.Chain.Count - 1] - targetPos).sqrMagnitude;

            while(sqrDstToTarget > _sqrMinDistance && iterationCount < _maxIterations)
            {
                // Solve the IK for this iteration.
                SolveIK(delta, setting, targetPos);

                // Update the virtual bone rest/poses.
                setting.CacheCurrentJointRotations(_angularDeltaLimit);
                sqrDstToTarget = (setting.Chain[setting.Chain.Count - 1] - targetPos).sqrMagnitude;
                ++iterationCount;
            }

            // Apply the virtual bone rest/poses to the actual bones.
            setting.ApplyJointRotations();

            setting.HasSimulated = true;
        }

        protected abstract void SolveIK(float deltaTime, IKIterateBaseSetting setting, Vector3 targetPos);
    }


    [System.Serializable]
    public class IKIterateBaseJoint : BoneJoint
    {
        public RotationAxis RotationAxis = RotationAxis.All;
        [ShowIf(nameof(RotationAxis), RotationAxis.Custom)] public Vector3 RotationAxisVector = Vector3.right;
 
        public JointLimitation Limitation;
        public SecondaryDirection LimitationRightAxis = SecondaryDirection.None;
        [ShowIf(nameof(LimitationRightAxis), SecondaryDirection.Custom)] public Vector3 LimitationRightAxisVector = Vector3.right;
        public Quaternion LimitationRotationOffset;

        public IKIterateBaseJoint() : base()
        { }


        #region Rotation Axis

        public Vector3 GetRotationAxisVector()
            => RotationAxis switch
            {
                RotationAxis.X => Vector3.right,
                RotationAxis.Y => Vector3.up,
                RotationAxis.Z => Vector3.forward,
                RotationAxis.All => Vector3.zero,
                _ => throw new System.NotImplementedException(),
            };

        public Vector3 GetLimitationRightAxisVector()
            => LimitationRightAxis switch
            {
                SecondaryDirection.None => Vector3.zero,

                SecondaryDirection.PosX => Vector3.right,
                SecondaryDirection.NegX => Vector3.left,

                SecondaryDirection.PosY => Vector3.up,
                SecondaryDirection.NegY => Vector3.down,

                SecondaryDirection.PosZ => Vector3.forward,
                SecondaryDirection.NegZ => Vector3.back,

                _ => throw new System.NotImplementedException(),
            };

        #endregion

        public Quaternion GetLimitationSpace(Vector3 localForward) => Limitation != null ? Limitation.MakeSpace(localForward, GetLimitationRightAxisVector(), LimitationRotationOffset) : new Quaternion();

        /// <summary>
        ///     Get the rotation around the normal vector (<see cref="RotationAxis"/>)
        /// </summary>
        public Vector3 GetProjectedRotation(Quaternion offset, Vector3 vector)
        {
            const float ALMOST_ONE = 1.0f - float.Epsilon;

            Vector3 axis = GetRotationAxisVector().normalized;
            Vector3 localVector = Quaternion.Inverse(offset) * vector;
            float length = localVector.magnitude;
            Vector3 projected = localVector.normalized.SnapToPlane(axis);

            if (length >= float.Epsilon)
                projected = projected.normalized * length;

            return Mathf.Abs(Vector3.Dot(localVector.normalized, axis)) > ALMOST_ONE ? vector : offset * projected;
                    
        }

        /// <summary>
        ///     Get limited rotation from forward axis in local rest space.
        /// </summary>
        public Vector3 GetLimitedRotation(Quaternion offset, Vector3 vector, Vector3 forward)
        {
            if (Limitation == null)
                throw new System.Exception("You need a value for the limitation to retrieve a limited value");

            UnityEngine.Debug.LogWarning("May be the opposite way round.");
            Vector3 localVector = Quaternion.Inverse(offset) * vector;
            float length = localVector.magnitude;

            if (length < float.Epsilon)
                return vector;

            Vector3 limited = Limitation.Solve(forward, GetLimitationRightAxisVector(), LimitationRotationOffset, localVector.normalized) * length;
            return offset * limited;
        }
    }

    [System.Serializable]
    public class IKIterateBaseSetting : IKChainBaseSetting<IKIterateBaseJoint>
    {
        [SerializeField] public Transform Target;

        [HideInInspector] public bool HasSimulated = false;


        public void InitJoints(bool mutableBoneAxes)
        {
            Chain.Clear();

            bool extendsEnd = ExtendEndBone && EndBoneLength > 0.0f;
            for (int i = 0; i < Joints.Length; ++i)
            {
                Vector3 globalPos = Joints[i].Bone.position; // Skeleton3D.GetBoneGlobalPose();
                Chain.Add(globalPos);

                bool isLast = i == Joints.Length - 1;
                if (isLast && extendsEnd)
                    // The last real bone. We are wishing to add a virtual bone to represent the tip.
                {
                    Vector3 axis = EndBone.GetBoneAxis(EndBoneDirection, mutableBoneAxes);
                    if (axis.IsApproximatelyZero())
                        continue;

                    SolverInfoList[i] ??= new();
                    SolverInfoList[i].ForwardVector = axis.normalized.SnapToPlane(Joints[i].GetRotationAxisVector());
                    SolverInfoList[i].Length = EndBoneLength;
                    Chain.Add(globalPos + Joints[i].Bone.rotation * (axis * EndBoneLength));
                }
                else if (isLast)
                {
                    Vector3 axis = _endBone.localPosition;
                    if (axis.IsApproximatelyZero())
                        continue;

                    SolverInfoList[i] ??= new();
                    SolverInfoList[i].ForwardVector = axis.normalized.SnapToPlane(Joints[i].GetRotationAxisVector());
                    SolverInfoList[i].Length = axis.magnitude;
                    //Chain.Add(_endBone.position);
                    Chain.Add(_endBone.position);
                }
                else if (!isLast)
                    // Not the last bone.
                {
                    // Rest Origin.
                    Vector3 axis = Joints[i + 1].RestPosition;
                    if (axis.IsApproximatelyZero())
                        continue;

                    SolverInfoList[i] ??= new();
                    SolverInfoList[i].ForwardVector = axis.normalized.SnapToPlane(Joints[i].GetRotationAxisVector());
                    SolverInfoList[i].Length = axis.magnitude;
                }
            }

            InitCurrentJointRotations();
        }


        /// <summary>
        ///     A
        /// </summary>
        public void CacheCurrentJointRotations(float angularDeltaLimit = Mathf.PI)
        {
            Quaternion parentGPose = Quaternion.identity;
            if (RootBone.Bone.parent != null)
                parentGPose = RootBone.Bone.parent.localRotation;

            for (int i = 0; i < Joints.Length; ++i)
            {
                int head = i;
                IKBaseSolverInfo solverInfo = SolverInfoList[head];
                if (solverInfo == null)
                    continue;

                solverInfo.CurrentLRest = Joints[head].Bone.localRotation;
                solverInfo.CurrentGRest = parentGPose * solverInfo.CurrentLRest;

                Vector3 from = solverInfo.ForwardVector;
                Vector3 to = (Quaternion.Inverse(solverInfo.CurrentGRest) * solverInfo.CurrentVector).normalized;
                Quaternion prev = solverInfo.CurrentLPose;

                //if (Joints[head].RotationAxis == RotationAxis.All)
                    // No rotation axis restrictions.
                    solverInfo.CurrentLPose = solverInfo.CurrentLRest * Quaternion.FromToRotation(from, to).GetSwing(from);
                //else
                    // Stabilize the rotation path (Especially near 180 degrees).
                //    solverInfo.CurrentLPose = solverInfo.CurrentLRest * QuaternionExtensions.GetFromToRotationByAxis(from, to, Joints[head].GetRotationAxisVector().normalized);

                //Debug.DrawRay(Joints[head].Bone.position, QuaternionExtensions.GetFromToRotationByAxis(from, to, Joints[head].GetRotationAxisVector()) * Vector3.up, Color.red, 0.1f);

                // Apply angular delta limit.
                float diff = Quaternion.Angle(prev, solverInfo.CurrentLPose) * Mathf.Deg2Rad;
                if (!MathUtils.IsApproximatelyZero(diff))
                    solverInfo.CurrentLPose = Quaternion.Slerp(prev, solverInfo.CurrentLPose, Mathf.Min(1.0f, angularDeltaLimit / diff));

                solverInfo.CurrentGPose = (parentGPose * solverInfo.CurrentLPose).normalized;
                parentGPose = solverInfo.CurrentGPose;
            }
        }
    }
}