using UnityEngine;

namespace Gameplay.Animations
{
    public class IKTest : MonoBehaviour
    {
        private IKSolver _ikSolver;
        private IKSegment[] _ikSegments;


        [SerializeField] private Transform _root;
        [SerializeField] private Transform _tip;

        [Space(10)]
        [SerializeField] private Transform _target;


        private void Start()
        {
            InitialiseSegments();
            InitialiseSolver();
        }

        private void InitialiseSegments()
        {
            _ikSegments = new IKSegment[1];

            // Create segment.
            IKSolver.CreateSegmentFlags flags = IKSolver.CreateSegmentFlags.X_AXIS_DOF | IKSolver.CreateSegmentFlags.Y_AXIS_DOF | IKSolver.CreateSegmentFlags.Z_AXIS_DOF;
            _ikSegments[0] = IKSolver.CreateSegment(flags, false);

            float length = Vector3.Distance(_root.position, _tip.position);
            IKSolver.SetTransform(_ikSegments[0], Vector3.zero, _root.rotation, Quaternion.identity, length);

            // Limits.

            // Stiffness.
        }
        private void InitialiseSolver()
        {
            _ikSolver = IKSolver.CreateIKSolver(_ikSegments[0]);

        }


        [ContextMenu("Test")]
        private void Solve()
        {
            // Set Solver Goals.
            _ikSolver.AddGoal(_ikSegments[0], _target.position, 1.0f);
            _ikSolver.Solve(10);
            _ikSolver.FreeGoals();


            IKSolver.GetBasisChange(_ikSegments[0], out Quaternion basisChange);
            _root.rotation = basisChange;
        }
    }

}
namespace EigenPort
{
    public static class Matrix3x3Unity
    {
        public static EigenPort.Matrix3x3 FromUnityQuaternion(UnityEngine.Quaternion q)
        {
            return new EigenPort.Matrix3x3(
                1.0f - (2.0f * q.y * q.y) - (2.0f * q.z * q.z), (2.0f * q.x * q.y) - (2.0f * q.z * q.w),        (2.0f * q.x * q.z) + (2.0f * q.y * q.w),
                (2.0f * q.x * q.y) + (2.0f * q.z * q.w),        1.0f - (2.0f * q.x * q.x) - (2.0f * q.z * q.z), (2.0f * q.y * q.z) - (2.0f * q.x * q.w),
                (2.0f * q.x * q.z) - (2.0f * q.y * q.w),        (2.0f * q.y * q.z) + (2.0f * q.x * q.w),        1.0f - (2.0f * q.x * q.x) - (2.0f * q.y * q.y));
        }
    }
}