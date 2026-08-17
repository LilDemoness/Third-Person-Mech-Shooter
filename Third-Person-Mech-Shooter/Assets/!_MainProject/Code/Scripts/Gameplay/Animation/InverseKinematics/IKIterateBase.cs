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

        [SerializeField] private float m_angularDeltaLimit = 2.0f; // If the delta is too large, the results before and after iterating can change significantly, and divergence of calculations can easily occur.
        protected float _angularDeltaLimit => m_angularDeltaLimit * Mathf.Deg2Rad;

        [SerializeField] protected bool _deterministic = false;
        [SerializeField, ShowIf(nameof(_deterministic))] protected RotationAxis _straightenToTarget = RotationAxis.None;
        [SerializeField, ShowIf(nameof(_straightenToTarget), RotationAxis.Custom)] protected Vector3 _customStraightenDirection;
        [SerializeField, HideIf(nameof(_straightenToTarget), RotationAxis.None)] protected Vector3 _straightenRotationOffset = Vector3.zero;


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
            
            setting.SolverInfoList.Clear();
            setting.SolverInfoList = new IKBaseSolverInfo[setting.Joints.Length];
            for(int i = 0; i < setting.SolverInfoList.Length; ++i)
                setting.SolverInfoList[i] = new();
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
            for (int i = 0; i < Settings.Length; ++i)
                Settings[i].ResetJoints();

            for (int i = 0; i < Settings.Length; ++i)
            {
                Transform target = Settings[i].Target;
                if (target == null)
                    continue; // No target. Abort.

                if (_deterministic && _straightenToTarget != RotationAxis.None)
                    Settings[i].StraightenDirection(target.position, _straightenToTarget, _straightenRotationOffset);
                InitJoints(i);
                

                Settings[i].CacheCurrentJointRotations(); // Iterate over first to detect parent (Outside of the chain) bone pose changes.

                // Convert the target position from world space to relative to the root bone (Account for Position & Scale, but not Rotation).
                Vector3 destination = (target.position - Settings[i].RootBone.Bone.position).DivideElementwise(Settings[i].RootBone.Bone.lossyScale);
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

        public override void DrawGizmos()
        {
            base.DrawGizmos();

            if (_straightenToTarget != RotationAxis.None)
                for(int i = 0; i < Settings.Length; ++i)
                    Settings[i].DrawStraightenGizmos(Settings[i].Target.position, _straightenToTarget, _straightenRotationOffset);
        }
    }


    [System.Serializable]
    public class IKIterateBaseJoint : BoneJoint
    {
        public RotationAxis RotationAxis = RotationAxis.Unrestricted;
        [ShowIf(nameof(RotationAxis), RotationAxis.Custom)] public Vector3 RotationAxisVector = Vector3.right;
 
        [SerializeReference, SubclassSelector] public JointLimitation Limitation;
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
                RotationAxis.Unrestricted => Vector3.zero,
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

            Vector3 localVector = offset * vector;
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
        [HideInInspector] public bool HasSimulated = false;

        [SerializeField] public Transform Target;


        public void InitJoints(bool mutableBoneAxes)
        {
            Chain.Clear();

            bool extendsEnd = ExtendEndBone && EndBoneLength > 0.0f;
            for (int i = 0; i < Joints.Length; ++i)
            {
                Vector3 globalPos = Joints[i].Bone.position - RootBone.Bone.position; // Position relative to the root bone.
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
            Quaternion parentGPose = RootBone.Bone.parent != null ? RootBone.Bone.parent.rotation : Quaternion.identity;

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

                // Apply angular delta limit.
                float diff = Quaternion.Angle(prev, solverInfo.CurrentLPose) * Mathf.Deg2Rad;
                if (!MathUtils.IsApproximatelyZero(diff))
                    solverInfo.CurrentLPose = Quaternion.Slerp(prev, solverInfo.CurrentLPose, Mathf.Min(1.0f, angularDeltaLimit / diff));

                solverInfo.CurrentGPose = (parentGPose * solverInfo.CurrentLPose).normalized;
                parentGPose = solverInfo.CurrentGPose;
            }
        }


        /// <summary>
        ///     Straighten the chain from the base joint to point towards the target via the given axis.
        /// </summary>
        /// <param name="targetPos"> World position of the target.</param>
        /// <param name="straightenAxis"> Axis to straighten through.</param>
        /// <param name="straightenRotationOffset"> Euler angle local-space offset applied to the rotation.</param>
        public void StraightenDirection(Vector3 targetPos, Vector3 straightenAxis, Vector3 straightenRotationOffset)
        {
            Quaternion planeNormalOffset = Quaternion.FromToRotation(Joints[0].Bone.up, Vector3.up);
            Vector3 planeNormal = planeNormalOffset * straightenAxis; // Transform the straightenAxis to local space.

            Vector3 horizontalToTarget = Vector3.ProjectOnPlane(targetPos - Joints[0].Bone.position, planeNormal).normalized;
            Vector3 defaultDirection = Vector3.ProjectOnPlane(Joints[0].RestPosition - Joints[1].RestPosition, planeNormal).normalized;
            Debug.DrawRay(Joints[0].Bone.position, horizontalToTarget);
            DebugUtils.DrawPlane(Joints[0].Bone.position, planeNormal, 0.5f);

            Joints[0].Bone.rotation = GetStraightenRotation(horizontalToTarget, defaultDirection, straightenRotationOffset);
        }
        /// <inheritdoc cref="StraightenDirection(Vector3, Vector3, Vector3)"/>
        /// <param name="pivotMode"> The axis to rotate around.</param>
        public void StraightenDirection(Vector3 targetPos, RotationAxis pivotMode, Vector3 straightenRotationOffset)
        {
            if (pivotMode == RotationAxis.Unrestricted)
            {
                Vector3 toTarget = Joints[0].Bone.InverseTransformDirection(targetPos - Joints[0].Bone.position).normalized;
                Vector3 defaultDirection = (Joints[0].RestPosition - Joints[1].RestPosition).normalized;
                Debug.DrawRay(Joints[0].Bone.position, toTarget, Color.green);
                Debug.DrawRay(Joints[0].Bone.position, defaultDirection, Color.yellow);

                Joints[0].Bone.rotation = GetStraightenRotation(toTarget, defaultDirection, straightenRotationOffset);
            }
            else
                StraightenDirection(targetPos, pivotMode.GetAxisFromTransform(Joints[0].Bone), straightenRotationOffset);
        }
        /// <summary>
        ///     Calculate the world-space rotation of our base joint so that it points towards the target direction.
        /// </summary>
        /// <returns> The world-space rotation for the root joint.</returns>
        /// <remarks>
        ///     Calculates the rotation to go from <paramref name="defaultDirection"/> to <paramref name="toTarget"/>,
        ///     then applies the supplied <paramref name="straightenRotationOffset"/> & the root joint's rest offset.<br/>
        ///     Also accounts for the invalid rotation returned if the two directions are perfectly or oppositely aligned.
        /// </remarks>
        private Quaternion GetStraightenRotation(Vector3 toTarget, Vector3 defaultDirection, Vector3 straightenRotationOffset)
        {
            const float PERFECT_ALIGNMENT_THRESHOLD = 0.999995f;
            bool perfectlyAligned = Mathf.Abs(Vector3.Dot(defaultDirection, toTarget)) >= PERFECT_ALIGNMENT_THRESHOLD;

            // If the two vectors are perfectly aligned, we wish to use the default rotation as our fromTo.
            //  As we are using this rotation as a world-space base, this default must be in world-space,
            //  so we cannot use Joints[0].RestPosition, instead retrieving the world-space version of
            //  the rest position (Parent rotation if one exists, otherwise the identity).
            Quaternion fromTo = perfectlyAligned
                ? (Joints[0].Bone.parent?.rotation ?? Quaternion.identity)
                : Quaternion.FromToRotation(defaultDirection, toTarget);

            return (fromTo * Quaternion.Euler(straightenRotationOffset)) * Joints[0].RestRotation;
        }


        public override void DrawGizmos()
        {
            base.DrawGizmos();

            // Dirty implementation: Draw limitations.
            {
                for (int i = 0; i < Joints.Length; ++i)
                {
                    if (Joints[i].Limitation != null)
                    {
                        if (i == 0)
                            Joints[i].Limitation.DrawLimitationGizmos(Joints[i].Bone, Joints[i].RestRotation, Joints[i].RestRotation * Vector3.forward, Joints[i].RestRotation * Vector3.right, Joints[i].RestRotation * Vector3.up);
                        else
                            Joints[i].Limitation.DrawLimitationGizmos(Joints[i].Bone, Joints[i - 1].Bone.rotation, Joints[i - 1].Bone.forward, Joints[i - 1].Bone.right, Joints[i - 1].Bone.up);
                    }
                }
            }
        }
        public void DrawStraightenGizmos(Vector3 targetPos, Vector3 straightenAxis, Vector3 straightenRotationOffset)
        {
            Quaternion planeNormalOffset = Quaternion.FromToRotation(Joints[0].Bone.up, Vector3.up);
            Vector3 planeNormal = planeNormalOffset * straightenAxis;

            Vector3 horizontalToTarget = Vector3.ProjectOnPlane((targetPos - Joints[0].Bone.position), planeNormal).normalized;

            Gizmos.DrawRay(Joints[0].Bone.position, horizontalToTarget);
            GizmosUtils.DrawPlane(Joints[0].Bone.position, planeNormal, 0.5f);
        }
        public void DrawStraightenGizmos(Vector3 targetPos, RotationAxis pivotMode, Vector3 straightenRotationOffset)
        {
            if (pivotMode == RotationAxis.Unrestricted)
            {
                Vector3 toTarget = (targetPos - Joints[0].Bone.position).normalized;
                Gizmos.DrawRay(Joints[0].Bone.position, toTarget);
            }
            else
                DrawStraightenGizmos(targetPos, pivotMode.GetAxisFromTransform(Joints[0].Bone), straightenRotationOffset);
        }
    }


    public static class DebugUtils
    {
        public static void DrawPlane(Vector3 planePosition, Vector3 planeNormal, float size) => DrawPlane(planePosition, planeNormal, size, Color.white, 0.0f);
        public static void DrawPlane(Vector3 planePosition, Vector3 planeNormal, float size, Color color) => DrawPlane(planePosition, planeNormal, size, color, 0.0f);
        public static void DrawPlane(Vector3 planePosition, Vector3 planeNormal, float size, Color color, float duration)
        {
            // Set our matrix to represent the plane transformation.
            Matrix4x4 matrix = Matrix4x4.TRS(planePosition, Quaternion.LookRotation(planeNormal), Vector3.one * size);

            // Calculate the corners of our display plane.
            Vector3 topLeft = matrix.MultiplyPoint(new Vector2(-1, 1));
            Vector3 topRight = matrix.MultiplyPoint(new Vector2(1, 1));
            Vector3 bottomLeft = matrix.MultiplyPoint(new Vector2(-1, -1));
            Vector3 bottomRight = matrix.MultiplyPoint(new Vector2(1, -1));

            // Draw our plane.
            Debug.DrawLine(topLeft, topRight, color, duration);
            Debug.DrawLine(topRight, bottomRight, color, duration);
            Debug.DrawLine(bottomRight, bottomLeft, color, duration);
            Debug.DrawLine(bottomLeft, topLeft, color, duration);
        }
    }
    public static class GizmosUtils
    {
        public static void DrawPlane(Vector3 planePosition, Vector3 planeNormal, float size)
        {
            Matrix4x4 oldMatrix = Gizmos.matrix;

            // Set our matrix to represent the plane transformation.
            Gizmos.matrix = Matrix4x4.TRS(planePosition, Quaternion.LookRotation(planeNormal), Vector3.one * size);

            // Draw a unit plane (The matrix makes it our desired size & shape).
            Vector3 topLeft = new Vector2(-1, 1);
            Vector3 topRight = new Vector2(1, 1);
            Vector3 bottomLeft = new Vector2(-1, -1);
            Vector3 bottomRight = new Vector2(1, -1);

            Gizmos.DrawLine(topLeft, topRight);
            Gizmos.DrawLine(topRight, bottomRight);
            Gizmos.DrawLine(bottomRight, bottomLeft);
            Gizmos.DrawLine(bottomLeft, topLeft);

            // Reset our matrix.
            Gizmos.matrix = oldMatrix;
        }
    }
}