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


        public static IKSolver CreateIKSolver(IKSegment root)
        {

        }
        public static IKSegment CreateSegment(int flag, bool translate)
        {
            
        }
        public void FreeIKSolver()
        {

        }


        public void AddGoal(IKSegment tip, Vector3 goal, float weight)
        {

        }
        public void AddGoalOrientation(IKSegment tip, Vector3 goal, float weight)
        {

        }
        public void SetPoleVectorConstraint(IKSegment tip, Vector3 goal, Vector3 poleGoal, float poleAngle, int getAngle)
        {

        }
        public float GetPoleAngle()
        {

        }


        public int Solve(float tolerance, int maxIterations)
        {

        }
    }
}