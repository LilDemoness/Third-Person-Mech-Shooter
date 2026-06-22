using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Animations
{
    /// <summary>
    /// </summary>
    /// <remarks>
    ///     Blender Source Code Link: https://github.com/dfelinto/blender/blob/master/intern/iksolver/extern/IK_solver.h
    ///     
    ///     Allows you to create segments and form them into a tree.
    ///     You can then define goal points that the end of a given segment should attempt to reach (Aka: An Inverse Kinematics problem).
    ///     This class will then modify the segments in the tree in order to get them as near as possible to the goal.
    ///     This solver uses an inverse jacobian method to find a solution.
    ///     
    ///     -----
    ///     
    ///     Typical calls for solving an IK problem (Blender):
    ///     - Create a number of <see cref="IKSegment"/> instances and set their parents and transforms
    ///     - Create an <see cref="IKSolver"/>
    ///     - Set a number of goals for the <see cref="IKSolver"/> to solve.
    ///     - Call IK_Solve
    ///     - Free the IKSolver
    ///     - Get basis and translation changes from segments
    ///     - Free all segments
    ///     
    ///     This will differ in our implementation as we can't free elements the same as in C++.
    ///     Additionally, repeatedly declaring and freeing elements would cause us to use more memory than we really need to (AFAIK)
    ///     Instead, we'll likely alter this to have an IKSolver (Or a separate class like IKController) that is persistent for an IK task, with the elements within it maintained between frames.
    /// </remarks>
    public class IKSolver
    {
        private const float IK_STRETCH_STIFF_EPS = 0.01f;
        private const float IK_STRETCH_STIFF_MIN = 0.001f;
        private const float IK_STRETCH_STIFF_MAX = 1e10f;

        private IKJacobianSolver _solver;
        private IKSegment _root;
        private List<IKTask> _tasks;


        private IKSolver(IKSegment root)
        {
            _root = root;
            _solver = new IKJacobianSolver();
            _tasks = new List<IKTask>();
        }
        public static IKSolver CreateIKSolver(IKSegment root)
        {
            if (root == null)
                return null;

            return new IKSolver(root);
        }


        [System.Serializable, System.Flags]
        public enum CreateSegmentFlags
        {
            X_AXIS_DOF = 1 << 0,
            Y_AXIS_DOF = 1 << 1,
            Z_AXIS_DOF = 1 << 2,

            X_TRANS_DOF = 1 << 3,
            Y_TRANS_DOF = 1 << 4,
            Z_TRANS_DOF = 1 << 5,
        }
        [System.Serializable]
        public enum IKSegmentAxis
        {
            X = 0,
            Y = 1,
            Z = 2,

            TRANS_X = 3,
            TRANS_Y = 4,
            IK_TRANS_Z = 5
        }

        public static IKSegment CreateSegment(CreateSegmentFlags flags, bool translate)
        {
            int degreesOfDreedomCount = 0;
            degreesOfDreedomCount += flags.HasFlag(CreateSegmentFlags.X_AXIS_DOF) ? 1 : 0;
            degreesOfDreedomCount += flags.HasFlag(CreateSegmentFlags.Y_AXIS_DOF) ? 1 : 0;
            degreesOfDreedomCount += flags.HasFlag(CreateSegmentFlags.Z_AXIS_DOF) ? 1 : 0;

            IKSegment segment;
            switch (degreesOfDreedomCount)
            {
                case 0: return null;

                case 1:
                    {
                        int axis = flags.HasFlag(CreateSegmentFlags.X_AXIS_DOF) ? 0 : (flags.HasFlag(CreateSegmentFlags.Y_AXIS_DOF) ? 1 : 2);
                        segment = translate ? new IKTranslateSegment(axis) : new IKRevoluteSegment(axis);
                        break;
                    }
                case 2:
                    {
                        int axis1, axis2;
                        if (flags.HasFlag(CreateSegmentFlags.X_AXIS_DOF))
                        {
                            axis1 = 0;
                            axis2 = flags.HasFlag(CreateSegmentFlags.Y_AXIS_DOF) ? 1 : 2;
                        }
                        else
                        {
                            axis1 = 1;
                            axis2 = 2;
                        }

                        segment = translate
                            ? new IKTranslateSegment(axis1, axis2)
                            : segment = (axis1 + axis2 == 2) ? new IKSwingSegment() : new IKElbowSegment((axis1 == 0) ? 0 : 2);
                        break;
                    }
                default:
                    {
                        segment = translate ? new IKTranslateSegment() : new IKSphericalSegment();
                        break;
                    }
            }

            return segment;
        }


        public static void SetParent(IKSegment segment, IKSegment parent)
        {
            if (parent != null && parent.GetComposite() != null)
                segment.SetParent(parent.GetComposite());
            else
                segment.SetParent(parent);
        }
        public static void SetTransform(IKSegment segment, Vector3 start, Quaternion rest, Quaternion basis, float length)
        {
            EigenPort.Vector3 mStart = new EigenPort.Vector3(start.x, start.y, start.z);

            // Note: We may need to adjust to account for how blender's implementation changes from column major to row major here.
            EigenPort.Matrix3x3 mRest = EigenPort.Matrix3x3Unity.FromUnityQuaternion(rest);
            EigenPort.Matrix3x3 mBasis = EigenPort.Matrix3x3Unity.FromUnityQuaternion(basis);


            if (segment.GetComposite() != null)
            {
                EigenPort.Vector3 cStart = EigenPort.Vector3.zero;
                EigenPort.Matrix3x3 cBasis = EigenPort.Matrix3x3.Identity;

                segment.SetTransform(mStart, mRest, mBasis, 0.0f);
                segment.GetComposite().SetTransform(cStart, cBasis, cBasis, length);
            }
            else
                segment.SetTransform(mStart, mRest, mBasis, length);
        }

        public static void SetLimit(IKSegment segment, IKSegmentAxis axis, float limitMin, float limitMax)
        {
            if (axis >= IKSegmentAxis.TRANS_X)
            {
                if (!segment.IsTranslationalSegment())
                {
                    if (segment.GetComposite() != null && segment.GetComposite().IsTranslationalSegment())
                        segment = segment.GetComposite();
                    else
                        return;
                }

                if (axis == IKSegmentAxis.TRANS_X)
                    axis = IKSegmentAxis.X;
                else if (axis == IKSegmentAxis.TRANS_Y)
                    axis = IKSegmentAxis.Y;
                else
                    axis = IKSegmentAxis.Z;
            }

            segment.SetLimit((int)axis, limitMin, limitMax);
        }
        public static void SetStiffness(IKSegment segment, IKSegmentAxis axis, float stiffness)
        {
            if (stiffness < 0.0f)
                return;

            if (stiffness > (1.0f - IK_STRETCH_STIFF_EPS))
                stiffness = (1.0f - IK_STRETCH_STIFF_EPS);

            float weight = 1.0f - stiffness;

            if (axis >= IKSegmentAxis.TRANS_X)
            {
                if (!segment.IsTranslationalSegment())
                {
                    if (segment.GetComposite() != null && segment.GetComposite().IsTranslationalSegment())
                        segment = segment.GetComposite();
                    else
                        return;
                }

                if (axis == IKSegmentAxis.TRANS_X)
                    axis = IKSegmentAxis.X;
                else if (axis == IKSegmentAxis.TRANS_Y)
                    axis = IKSegmentAxis.Y;
                else
                    axis = IKSegmentAxis.Z;
            }

            segment.SetWeight((int)axis, weight);
        }
        public static void GetBasisChange(IKSegment segment, out Quaternion basisChange)
        {
            if (segment.IsTranslationalSegment() && segment.GetComposite() != null)
                segment = segment.GetComposite();

            EigenPort.Matrix3x3 change = segment.GetBasisChange();

            // Convert the change to a quaternion.
            if (change[2,2] < 0.0f)
            {
                if (change[0, 0] > change[1, 1])
                {
                    float trace = 1.0f + change[0,0] - change[1,1] - change[2,2];
                    float scale = 2.0f * Mathf.Sqrt(trace);
                    if (change[1,2] < change[2,1])
                        // Ensure w is non-negative.
                        scale = -scale;
                    float qx = 0.25f * scale;
                    scale = 1.0f / scale;

                    basisChange = new Quaternion(
                        qx,
                        (change[0,1] + change[1,0]) * scale,
                        (change[2,0] + change[0,2]) * scale,
                        (change[1,2] - change[2,1]) * scale
                        );
                }
                else
                {
                    float trace = 1.0f - change[0, 0] + change[1, 1] - change[2, 2];
                    float scale = 2.0f * Mathf.Sqrt(trace);
                    if (change[2, 0] < change[0, 2])
                        // Ensure w is non-negative.
                        scale = -scale;
                    float qy = 0.25f * scale;
                    scale = 1.0f / scale;

                    basisChange = new Quaternion(
                        (change[0, 1] + change[1, 0]) * scale,
                        qy,
                        (change[1, 2] + change[2, 1]) * scale,
                        (change[0, 2] - change[0, 2]) * scale
                        );
                }
            }
            else
            {
                if (change[0, 0] < -change[1, 1])
                {
                    float trace = 1.0f - change[0, 0] - change[1, 1] + change[2, 2];
                    float scale = 2.0f * Mathf.Sqrt(trace);
                    if (change[0, 1] < change[1, 0])
                        // Ensure w is non-negative.
                        scale = -scale;
                    float qz = 0.25f * scale;
                    scale = 1.0f / scale;

                    basisChange = new Quaternion(
                        (change[2, 0] + change[0, 2]) * scale,
                        (change[1, 2] + change[2, 1]) * scale,
                        qz,
                        (change[0, 1] - change[1, 0]) * scale
                        );
                }
                else
                {
                    float trace = 1.0f + change[0, 0] + change[1, 1] + change[2, 2];
                    float scale = 2.0f * Mathf.Sqrt(trace);
                    float qw = 0.25f * scale;
                    scale = 1.0f / scale;

                    basisChange = new Quaternion(
                        (change[1, 2] - change[2, 1]) * scale,
                        (change[2, 0] - change[0, 2]) * scale,
                        (change[0, 1] - change[1, 0]) * scale,
                        qw
                        );
                }
            }


            // Note: Currently is converting to column major as per Blender's implementations.
            //  Research what Unity uses.
            /*basisChange[0][0] = change[0, 0];
            basisChange[1][0] = change[0, 1];
            basisChange[2][0] = change[0, 2];
            basisChange[0][1] = change[1, 0];
            basisChange[1][1] = change[1, 1];
            basisChange[2][1] = change[1, 2];
            basisChange[0][2] = change[2, 0];
            basisChange[1][2] = change[2, 1];
            basisChange[2][2] = change[2, 2];*/
        }
        public static void GetTranslationChange(IKSegment segment, Vector3 translationChange)
        {
            if (!segment.IsTranslationalSegment() && segment.GetComposite() != null)
                segment = segment.GetComposite();

            EigenPort.Vector3 change = segment.GetTranslationChange();
            translationChange[0] = change[0];
            translationChange[1] = change[1];
            translationChange[2] = change[2];
        }


        public void AddGoal(IKSegment tip, Vector3 goal, float weight)
        {
            // In case of a composite segment, the second segment is the real tip.
            if (tip.GetComposite() != null)
                tip = tip.GetComposite();

            EigenPort.Vector3 goalPos = new EigenPort.Vector3(goal.x, goal.y, goal.z);
            IKTask task = new IKPositionTask(true, tip, goalPos);
            task.SetWeight(weight);
            _tasks.Add(task);
        }
        public void AddGoalOrientation(IKSegment tip, Vector3 goalA, Vector3 goalB, Vector3 goalC, float weight)
        {
            // In case of a composite segment, the second segment is the real tip.
            if (tip.GetComposite() != null)
                tip = tip.GetComposite();

            // Note: Order may be different.
            EigenPort.Matrix3x3 rot = new EigenPort.Matrix3x3(
                goalA.x, goalB.x, goalC.x,
                goalA.y, goalB.y, goalC.y,
                goalA.z, goalB.z, goalC.z
                );

            IKTask orient = new IKOrientationTask(true, tip, rot);
            orient.SetWeight(weight);
            _tasks.Add(orient);
        }
        public void SetPoleVectorConstraint(IKSegment tip, Vector3 goal, Vector3 poleGoal, float poleAngle, bool getAngle)
        {
            // In case of a composite segment, the second segment is the real tip.
            if (tip.GetComposite() != null)
                tip = tip.GetComposite();

            EigenPort.Vector3 qGoal = new EigenPort.Vector3(goal.x, goal.y, goal.z);
            EigenPort.Vector3 qPoleGoal = new EigenPort.Vector3(poleGoal.x, poleGoal.y, poleGoal.z);
            _solver.SetPoleVectorConstraint(tip, qGoal, qPoleGoal, poleAngle, getAngle);
        }
        public float GetPoleAngle() => _solver != null ? _solver.GetPoleAngle() : 0.0f;


        /// <summary>
        /// 
        /// </summary>
        /// <returns> True if successfully solved, false if it failed.</returns>
        public bool Solve(int maxIterations)
        {
            if (_solver == null)
                return false;
            if (!_solver.Setup(_root, _tasks))
                return false;

            return _solver.Solve(_root, _tasks, maxIterations);
        }


        public void FreeGoals()
        {
            _tasks.Clear();
        }
    }
}