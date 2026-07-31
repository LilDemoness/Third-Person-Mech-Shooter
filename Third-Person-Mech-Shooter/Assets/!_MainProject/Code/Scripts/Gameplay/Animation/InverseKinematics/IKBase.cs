using System.Collections.Generic;
using UnityEngine;
using Utils;

namespace Gameplay.Animations.InverseKinematics
{
    /* Godot Source: https://github.com/godotengine/godot/blob/master/scene/3d/ik_modifier_3d.h
     
     - Godot uses its IKModifier (And all subsequent children) as nodes, and therefore the settings represent individual instances of the solver.
        - We'll have each instance be an instance of a solver, and if later desired we can transition to using an "IKBrain" or similar to act as the controller/triggering element.
     
    - For Pole Targets, Godot instead has you have multiple solvers run in order, with the first targeting the pole target, and the second targeting the final target.
        - Then, the second ik solver uses the result from the first as its rest, giving you a flexible targeting system.
        - This approach requires the IK to be deterministic (No data from previous frames considered) so that the rest pose is properly accounted for.

     */


    /// <summary>
    ///     A base class for IK Solvers.
    ///     Do not inherit from this class. Instead, inherit from <see cref="IKBase{TSetting}"/> or one of its children.
    /// </summary>
    /// <remarks>
    ///     A base, non-generic class for IK Solvers so that we can reference them without needing to know their specific type.
    /// </remarks>
    public abstract class IKBase : MonoBehaviour
    {
        protected bool _mutableBoneAxes = true;
        protected bool _jointsDirty = false;


        public abstract void ProcessIK(float deltaTime);
        public void RestUpdated() => MakeAllJointsDirty();

        protected abstract void MakeAllJointsDirty();
        protected abstract void InitJoints(int index);
        protected abstract void UpdateJoints(int index);
        protected abstract void MakeSimulationDirty(int index);
        protected abstract void UpdateBoneAxis(int index);


        public void SetMutableBoneAxes(bool newIsMutable) => _mutableBoneAxes = newIsMutable;
        public bool AreBoneAxesMutable() => _mutableBoneAxes;


        private void OnDrawGizmos() => DrawGizmos();
        public abstract void DrawGizmos();
    }

    /// <summary>
    ///     A base class for IK Solvers.
    /// </summary>
    /// <typeparam name="TSetting"> The container class used for this IK solver's settings.</typeparam>
    public abstract class IKBase<TSetting> : IKBase where TSetting : IKBaseSetting
    {
        [SerializeField] protected ClassArray<TSetting> Settings = new ClassArray<TSetting>();

        public override void DrawGizmos()
        {
            for (int i = 0; i < Settings.Length; ++i)
            {
                if (Settings[i] != null)
                    Settings[i].DrawGizmos();
            }
        }
    }



    [System.Serializable]
    public class BoneJoint
    {
        #if UNITY_EDITOR
        [HideInInspector] public bool Editor_Show = false;
        #endif

        [field: SerializeField, ReadOnly] public Transform Bone { get; private set; }

        /// <summary>
        ///     Rest position in local space.
        /// </summary>
        [field:SerializeField, ReadOnly] public Vector3 RestPosition { get; private set; }
        /// <summary>
        ///     Rest rotation in local space.
        /// </summary>
        [field: SerializeField, ReadOnly] public Quaternion RestRotation { get; private set; }


        public BoneJoint(){ }
        public virtual void Initialise(Transform jointTransform)
        {
            Bone = jointTransform;
            UpdateRest();
        }

        public void UpdateRest()
        {
            RestPosition = Bone.localPosition;
            RestRotation = Bone.localRotation;
        }
    }
    public static class BoneJointExtensions
    {
        public static Vector3 GetBoneAxis(this BoneJoint endBone, BoneDirection direction, bool mutableBoneAxes)
        {
            return direction == BoneDirection.FromParent
                ? (endBone.RestPosition + Quaternion.Inverse(endBone.RestRotation) * (mutableBoneAxes ? endBone.Bone.localPosition : endBone.RestPosition)).normalized
                : direction.ToBoneAxis().ToVector();
        }
    }


    [System.Serializable]
    public class IKBaseSolverInfo
    {
        public Quaternion CurrentLPose { get; set; } // Local Space.
        public Quaternion CurrentLRest { get; set; } // Local Space.
        public Quaternion CurrentGPose { get; set; } // Root-Bone's Parent Local Space
        public Quaternion CurrentGRest { get; set; } // Root-Bone's Parent Local Space


        public Vector3 CurrentVector { get; set; } // Root-Bone's Parent Local Space
        public Vector3 ForwardVector { get; set; } // Local Direction. Points to the next bone in the IK Chain.


        public float Length { get; set; } = 0.0f; // Length of this bone (Distance to the next bone in the IK Chain).
    }
    [System.Serializable]
    public class IKBaseSetting
    {
        #if UNITY_EDITOR
        [SerializeField, HideInInspector] private bool _editorFoldout = true;
        #endif

        [field: SerializeField] public bool SimulationDirty {get; set; } = true; // If true, our simulation parameters have changed.
        [field: SerializeField] public bool JointsDirty { get; set; } = false;

        public virtual void DrawGizmos() { }
    }


    /// <summary>
    ///     Indicates the bone axis on a basis without a custom vector.
    /// </summary>
    public enum BoneAxis
    {
        PosX,
        NegX,

        PosY,
        NegY,

        PosZ,
        NegZ,
    }
    /// <summary>
    ///     Indicates the Head-Tail of the bone.
    /// </summary>
    public enum BoneDirection
    {
        PosX,
        NegX,

        PosY,
        NegY,

        PosZ,
        NegZ,

        FromParent,
    }

    public enum RotationAxis
    {
        X,
        Y,
        Z,

        All,

        Custom,
    }
    public enum SecondaryDirection
    {
        None,

        PosX,
        NegX,

        PosY,
        NegY,

        PosZ,
        NegZ,

        Custom,
    }
}


namespace Gameplay.Animations.InverseKinematics
{
    public static class BoneAxisExtensions
    {
        public static Vector3 ToVector(this BoneAxis boneAxis)
            => boneAxis switch
            {
                BoneAxis.PosX => Vector3.right,
                BoneAxis.NegX => Vector3.left,

                BoneAxis.PosY => Vector3.up,
                BoneAxis.NegY => Vector3.down,

                BoneAxis.PosZ => Vector3.forward,
                BoneAxis.NegZ => Vector3.back,

                _ => throw new System.NotImplementedException($"No implementation for {boneAxis.ToString()}")
            };
    }
    public static class BoneDirectionExtensions
    {
        public static BoneAxis ToBoneAxis(this BoneDirection boneDirection)
            => boneDirection switch
            {
                BoneDirection.PosX => BoneAxis.PosX,
                BoneDirection.NegX => BoneAxis.NegX,

                BoneDirection.PosY => BoneAxis.PosY,
                BoneDirection.NegY => BoneAxis.NegY,

                BoneDirection.PosZ => BoneAxis.PosZ,
                BoneDirection.NegZ => BoneAxis.NegZ,

                _ => throw new System.NotImplementedException($"There is no corresponding BoneAxis for the BoneDirection {boneDirection.ToString()}")
            };
    }
    public static class ArrayExtensions
    {
        /// <summary>
        ///     Sets all values within <paramref name="array"/> to default.
        /// </summary>
        public static void Clear<TType>(this TType[] array)
        {
            for (int i = 0; i < array.Length; ++i)
                array[i] = default;
        }
        public static void NullValues<TType>(this TType[] array) where TType : class
        {
            for (int i = 0; i < array.Length; ++i)
                array[i] = null;
        }
    }
    public static class Vector3Extensions
    {
        public static bool IsApproximatelyZero(this Vector3 v) => MathUtils.IsApproximatelyZero(v.x) && MathUtils.IsApproximatelyZero(v.y) && MathUtils.IsApproximatelyZero(v.z);

        /// <summary>
        ///     Returns a perpendicular vector by the cross product with Vector3.right or Vector3.up
        ///     (Whichever has the greatest angle to the absolute of the current vector).
        /// </summary>
        /// <remarks>
        ///     https://github.com/godotengine/godot/blob/master/core/math/vector3.h#L375
        /// </remarks>
        public static Vector3 GetAnyPerpendicular(this Vector3 v) => Vector3.Cross(v, (Mathf.Abs(v.x) <= Mathf.Abs(v.y) && Mathf.Abs(v.x) <= Mathf.Abs(v.z)) ? Vector3.right : Vector3.up).normalized;


        /// <summary>
        ///     Returns <param name="vector"/> snapped to a plane with the normal <paramref name="planeNormal"/>.<br/>
        ///     Maintains the magnitude of <paramref name="vector"/>.
        /// </summary>
        /// <param name="vector"> The vector to clamp.</param>
        /// <param name="planeNormal"> The normal of the plane to clamp <paramref name="vector"/> two.</param>
        public static Vector3 SnapToPlane(this Vector3 vector, Vector3 planeNormal)
        {
            if (planeNormal.sqrMagnitude <= float.Epsilon)
                return vector;

            float length = vector.magnitude;
            Vector3 normalizedVec = vector.normalized;
            Vector3 normal = planeNormal.normalized;
            return (normalizedVec - (normal * Vector3.Dot(planeNormal, vector))) * length;
        }


        public static Vector3 MultiplyElementwise(this Vector3 a, Vector3 b)
        => new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
        public static Vector3 DivideElementwise(this Vector3 a, Vector3 b)
        => new Vector3(a.x / b.x, a.y / b.y, a.z / b.z);
    }
    public static class QuaternionExtensions
    {
        public static Quaternion GetSwing(this Quaternion rotation, Vector3 axis)
        {
            if (axis.IsApproximatelyZero())
                return rotation;
            if (axis.sqrMagnitude != 1.0f)
                axis.Normalize();

            // Calculate the twist rotation.
            Vector3 v = new Vector3(rotation.x, rotation.y, rotation.z);
            float projectionLength = Vector3.Dot(v, axis);
            Vector3 twistVec = axis * projectionLength;
            Quaternion twist = new Quaternion(twistVec.x, twistVec.y, twistVec.z, rotation.w);

            // Return the swing rotation.
            return rotation * Quaternion.Inverse(twist);
        }


        /// <summary>
        ///     Returns the rotation that when applied to <paramref name="from"/> turns it into <paramref name="to"/> along the specified <paramref name="axis"/>.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="axis"> The axis to calculate the rotation around. Must be normalized.</param>
        public static Quaternion GetFromToRotationByAxis(Vector3 from, Vector3 to, Vector3 axis)
        {
            const float ALMOST_ONE = 1.0f - float.Epsilon;
            float dot = Vector3.Dot(from, to);

            if (dot > ALMOST_ONE)
                return Quaternion.identity; // No rotation required.
            if (dot < -ALMOST_ONE)
                return Quaternion.AngleAxis(360.0f, axis); // Return a rotation to flip the vector.

            float angle = Vector3.Angle(from, to);
            Vector3 cross = Vector3.Cross(from, to);
            if (Mathf.Sign(Vector3.Dot(cross, axis)) == 1)
                angle = -angle;

            return Quaternion.AngleAxis(angle, axis);
        }
    }
    public static class MathUtils
    {
        /// <summary>
        ///     Returns true if the passed value is within the Epsilon of zero.
        /// </summary>
        public static bool IsApproximatelyZero(float v) => Mathf.Abs(v) <= float.Epsilon;

        /// <summary>
        ///     Returns true if the two passed values are within the Epsilon value of each other.
        /// </summary>
        public static bool IsApproximatelyEqual(float a, float b) => Mathf.Abs(a - b) < float.Epsilon;
    }
}