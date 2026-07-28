using UnityEngine;

namespace Gameplay.Animations.InverseKinematics
{
    /// <summary>
    ///     An implementation of <see cref="IKIterateBase<>"/> using the FABRIK algorithm.
    /// </summary>
    public class FABRICIK : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private Transform[] _bones; // Including Tip Bone.
        private Vector3[] _restPositions;
        private float[] _boneLengths;
        private Vector3[] _chain;

        [SerializeField] private int _iterations = 4;


        private void Start()
        {
            _restPositions = new Vector3[_bones.Length];
            _boneLengths = new float[_bones.Length - 1];
            _chain = new Vector3[_bones.Length];

            for(int i = 0; i < _restPositions.Length; ++i)
                _restPositions[i] = _bones[i].position;
            for(int i = 0; i < _boneLengths.Length; ++i)
                _boneLengths[i] = _bones[i + 1].localPosition.magnitude;
            for(int i = 0; i < _chain.Length; ++i)
                _chain[i] = _bones[i].position;
        }


        private void Update() => ProcessIK();

        [ContextMenu("Solve")]
        private void ProcessIK()
        {
            for(int i = 0; i < _restPositions.Length; ++i)
                _chain[i] = _restPositions[i];

            for (int i = 0; i < _iterations; ++i)
                SolveIK(Time.deltaTime, _target.position);
        }

        protected void SolveIK(float deltaTime, Vector3 targetPos)
        {
            // Iterates by:
            // - Moves bones towards the effector, pointing to their previous position.
            // - Moves bones towards the root, pointing to their previous position.

            int jointSize = _chain.Length - 1; // Joint Count.

            // Forwards: To Effector.
            for(int i = jointSize - 1; i >= 0; --i)
            {
                int head = i;
                int tail = i + 1;

                Vector3 currentHead = _chain[head];
                Vector3 headToTarget = targetPos - currentHead;

                _chain[head] = targetPos + -headToTarget.normalized * _boneLengths[i];
                _chain[tail] = targetPos;

                targetPos = _chain[head];
            }


            // Backwards: To Root.
            targetPos = _restPositions[0];
            for (int i = 0; i < jointSize; ++i)
            {
                int head = i;
                int tail = i + 1;

                Vector3 currentHead = _chain[tail];
                Vector3 headToTarget = targetPos - currentHead;

                _chain[head] = targetPos;
                _chain[tail] = targetPos + -headToTarget.normalized * _boneLengths[i];

                targetPos = _chain[tail];
            }


            for (int i = 0; i < _chain.Length; ++i)
                _bones[i].position = _chain[i];
        }

        private void OnDrawGizmos()
        {
            if (_chain == null)
                return;

            for(int i = 0; i < _chain.Length - 1; ++i)
                Gizmos.DrawLine(_chain[i], _chain[i + 1]);
        }
    }
}