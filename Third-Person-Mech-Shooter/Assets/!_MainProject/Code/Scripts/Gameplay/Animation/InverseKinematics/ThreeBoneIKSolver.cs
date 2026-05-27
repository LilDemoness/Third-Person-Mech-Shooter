using UnityEngine;

namespace Gameplay.Animations
{
    /// <summary>
    ///     A solver for a three-bone IK.
    /// </summary>
    /// <remarks>
    ///     Adapted from: 'https://www.youtube.com/watch?v=ZYu5r4tlfhA'
    ///     View 'https://www.youtube.com/watch?v=A0XGAasLyaE&t=100s' for a potentially better source.
    /// </remarks>
    public class ThreeBoneIKSolver : MonoBehaviour
    {
        /*
        -  
        - 
        - 
         */

        [SerializeField] private Transform[] _chainBones = new Transform[4];
        private IKChain _ikChain;

        [SerializeField] private Transform _target;
        [SerializeField] private Transform _hint;
        [SerializeField] private float _poleAngle;


        private void Start()
        {
            Bone[] bones = new Bone[3]
            {
                new Bone(_chainBones[0], _chainBones[1]),
                new Bone(_chainBones[1], _chainBones[2]),
                new Bone(_chainBones[2], _chainBones[3])
            };
            _ikChain = new IKChain(bones);
        }


        private void Update()
        {
            TestSolve();
        }
        private void UpdateIK()
        {
            
        }


        [ContextMenu("Test Solve")]
        private void TestSolve() => Solve(_ikChain, _target, _hint);

        void Solve(IKChain chain, Transform target, Transform hint)
        {
            Vector3 targetVector = target.position - chain.Start;
            if (targetVector.sqrMagnitude >= chain.SqrMaxLength)
                // Check if we should be fully extended.
            {
                throw new System.NotImplementedException("Implement fully extended solver.");
                return;
            }


            /* Get the length of the bones, then calculate the ratio length based on the chain length.
             * When placed in a zig-zag pattern, the three bones create a trapezoid, which can break down into two triangles.
             * We use the ratio of the target length divided between the two triangles, then using the first bone + half
             * of the second bound to solve for the top 2 joints, while half the second bone + the third bone solves the bottom joint.
             
             * If all bones are of equal length, then we only need to use half of the target length and only test one triangle, using that for both.
             * If bones are uneven, then we need to solve an angle for each triangle.
             */


            // How much of the target length to use for bones 1+2.
            // Bones 2+3 use '1 - lengthRatio'.
            float lengthRatio = (chain.Bones[0].Length + chain.Bones[1].Length * 0.5f) / chain.MaxLength;
            float targetLength = targetVector.magnitude;

            // Calculate the angles for our bone pairs (a = 1-2; b = 2-3).
            float angleA = CalculateSolverAngle(chain.Bones[0].Length, chain.Bones[1].Length * 0.5f, targetLength * lengthRatio);
            float angleB = CalculateSolverAngle(chain.Bones[2].Length, chain.Bones[1].Length * 0.5f, targetLength * (1.0f - lengthRatio));

            // Apply the angle.
            //Vector3 hintNormal = ;
            //Vector3 cross = Vector3.Cross((target.position - chain.Start).normalized, hintNormal);
            Vector3 cross = Vector3.left;

            IKSolver.AimDirection(chain, target);   // Align the root bone.
            chain.Bones[1].SetAngleAxis(angleA * Mathf.Rad2Deg, cross);
            chain.Bones[2].SetAngleAxis(-angleB * Mathf.Rad2Deg, cross);
        }
        void Solve2(IKChain chain, Transform target, Transform hint)
        {
            Vector3 targetVector = target.position - chain.Start;
            if (targetVector.sqrMagnitude >= chain.SqrMaxLength)
            // Check if we should be fully extended.
            {
                throw new System.NotImplementedException("Implement fully extended solver.");
                return;
            }


            float az = Mathf.PI - Mathf.Atan2(targetVector.x, targetVector.y);
            Vector3 dt = Quaternion.AngleAxis(-az * Mathf.Rad2Deg, Vector3.back) * targetVector;
            float l = Mathf.Min(targetVector.magnitude, chain.MaxLength * 0.99f);

            float ax = Mathf.Atan2(dt.y, dt.z);

            float e = Remap(l, 0.0f, chain.MaxLength, chain.Bones[2].Length, chain.Bones[0].Length + chain.Bones[1].Length);

            float angle1 = CosineRule(e, chain.Bones[0].Length, chain.Bones[1].Length);
            float angle2 = CosineRule(chain.Bones[0].Length, chain.Bones[1].Length, e);
            float angle3 = CosineRule(chain.Bones[1].Length, e, chain.Bones[0].Length);
            float angle4 = CosineRule(e, chain.Bones[2].Length, l);
            float angle5 = CosineRule(chain.Bones[2].Length, l, e);
            float angle6 = 2.0f * Mathf.PI - (angle1 + angle2 + angle3 + angle4 + angle5);

            Vector3 poleAxis = targetVector.normalized;

            float bone1Angle = (angle1 + angle6 + ax);
            Quaternion bone1Dir = chain.Bones[0].InitialRotation
                * Quaternion.AngleAxis((Mathf.PI - bone1Angle) * Mathf.Rad2Deg, Vector3.right)
                * Quaternion.AngleAxis(az * Mathf.Rad2Deg, Vector3.back)
                * Quaternion.AngleAxis((_poleAngle - az) * Mathf.Rad2Deg, poleAxis);
            chain.Bones[0].SetRotation(bone1Dir);

            float bone2Angle = bone1Angle + angle2;
            Quaternion bone2Dir = chain.Bones[1].InitialRotation
                * Quaternion.AngleAxis((2.0f * Mathf.PI - bone2Angle) * Mathf.Rad2Deg, Vector3.right)
                * Quaternion.AngleAxis(az * Mathf.Rad2Deg, Vector3.back)
                * Quaternion.AngleAxis((_poleAngle - az) * Mathf.Rad2Deg, poleAxis);
            chain.Bones[1].SetRotation(bone2Dir);

            float bone3Angle = bone2Angle + angle3;
            Quaternion bone3Dir = chain.Bones[2].InitialRotation
                * Quaternion.AngleAxis((Mathf.PI - bone3Angle) * Mathf.Rad2Deg, Vector3.right)
                * Quaternion.AngleAxis(az * Mathf.Rad2Deg, Vector3.back)
                * Quaternion.AngleAxis((_poleAngle - az) * Mathf.Rad2Deg, poleAxis);
            chain.Bones[2].SetRotation(bone3Dir);
        }


        // A Jacobian method for solving a n-length IK target, based on Blender's source code.
        void SolveJacobian()
        {
            /* Reference Links:
             * - https://github.com/dfelinto/blender/blob/master/intern/iksolver/intern/IK_Solver.cpp#L386
             * - https://github.com/dfelinto/blender/blob/master/intern/iksolver/intern/IK_QJacobianSolver.cpp
             */

            /* ---- Summaries ----
             * Setup:
             * - Clears previous data
             * - ...
             
             * Solve:
             * - ...
             */
        }



        // Calculate the angle for a pair of bones using the Law of Cosines.
        // Law of Cosines = cos(C) = (a^2 + b^2 - c^2) / (2 * a * b)
        float CalculateSolverAngle(float lengthA, float lengthB, float lengthC)
        {
            float n = ((lengthA * lengthA) + (lengthB * lengthB) - (lengthC * lengthC)) / (2.0f * lengthA * lengthB);
            // Prevent a NAN error by clamping.
            return Mathf.PI - Mathf.Acos(Mathf.Clamp(n, -1.0f, 1.0f));
        }
    


        private float CosineRule(float a, float b, float c) => Mathf.Acos(((a * a) + (b * b) - (c * c)) / (2 * a * b));
        private float Remap(float input, float oldMin, float oldMax, float newMin, float newMax) => ((input - oldMin) / ((oldMax - oldMin) * (newMax - newMin))) + newMin;

    }
    public class Bone
    {
        public readonly Transform Head;
        public readonly Transform Tail;

        // If true, then 'Tail' is the immediate child of 'Head' in the transform hierarchy.
        // This simplifies some calculations as position, rotation, and scale changes are automatically propogated by Unity.
        private readonly bool _tailIsChild;

        public readonly float Length;

        public Vector3 InitialPosition { get; private set; }
        public Quaternion InitialRotation { get; private set; }


        public Bone(Transform head, Transform tail)
        {
            Head = head;
            Tail = tail;
            _tailIsChild = tail.IsChildOf(head, checkDepth: 1);

            Length = CalculateHeadToTailVector().magnitude;

            UpdateInitialValues();
        }
        public void UpdateInitialValues() => UpdateInitialValues(Head.position, _tailIsChild ? Head.rotation : Quaternion.LookRotation(CalculateHeadToTailVector().normalized, Head.up));
        public void UpdateInitialValues(Vector3 newInitialPosition, Quaternion newInitialRotation)
        {
            InitialPosition = newInitialPosition;
            InitialRotation = newInitialRotation;
        }

        private Vector3 CalculateHeadToTailVector() => Tail.position - Head.position;


        public void SetPosition(Vector3 newPosition)
        {
            /*if (!_tailIsChild)
            {
                Vector3 cachedHeadToTail = CalculateHeadToTailVector();
                Head.position = newPosition;
                Tail.position = Head.position + cachedHeadToTail;
            }
            else*/
                Head.position = newPosition;
        }

        public void SetRotation(Quaternion newRotation)
        {
            /*if (!_tailIsChild)
            {
                Quaternion cachedRotationDifference = Quaternion.Inverse(Head.rotation) * Tail.rotation;    // Rotation applied to the head to get to the tail.
                Head.rotation = newRotation;
                Tail.rotation = Head.rotation * cachedRotationDifference;
            }
            else*/
                Head.rotation = newRotation;
        }

        public void AddRotation(Quaternion quaternion) => SetRotation(Head.rotation * quaternion);
        public void SubtractRotation(Quaternion quaternion) => SetRotation(Quaternion.Inverse(Head.rotation) * quaternion);

        public void SetAngleAxis(float angleDeg, Vector3 axis) => SetRotation(Quaternion.AngleAxis(angleDeg, axis));
    }
    public class IKChain
    {
        public int BoneCount { get; private set; }
        public Bone[] Bones { get; private set; }
        public float MaxLength { get; private set; }


        public IKChain(params Bone[] bones)
        {
            BoneCount = bones.Length;
            Bones = bones;

            // Calculate and set the max length.
            MaxLength = 0.0f;
            for(int i = 0; i < bones.Length; i++)
                MaxLength += bones[i].Length;
        }


        public Vector3 Start => Bones[0].Head.position;
        public float SqrMaxLength => MaxLength * MaxLength;
    }

    public static class IKSolver
    {
        /// <summary>
        ///     Aligns the first bone to the target axis, then resets rotation (And optionally position) of all sub-links from the root.
        /// </summary>
        /// <param name="chain"> The <see cref="IKChain"/> to affect.</param>
        /// <param name="target"> The Target Transform we're aiming towards.</param>
        /// <param name="resetPosition"> Should we reset the chain elements positions too?</param>
        public static void FullDirection(IKChain chain, Transform target, bool resetPosition = false)
        {
            // Point the chain in the target's direction.
            IKSolver.AimDirection(chain, target, true);

            // Reset rotation (And optionally position) on all bones except the root.
            for(int i = 1; i < chain.BoneCount; ++i)
            {
                chain.Bones[i].SetRotation(chain.Bones[i].InitialRotation);

                if (resetPosition)
                {
                    chain.Bones[i].SetPosition(chain.Bones[i].InitialPosition);
                }
            }
        }

        /// <summary>
        ///     Aligns the first link of the chain towards the target axis.
        /// </summary>
        /// <param name="chain"></param>
        /// <param name="target"></param>
        /// <param name="doOffset"></param>
        public static void AimDirection(IKChain chain, Transform target, bool doOffset = false)
        {
            chain.Bones[0].SetRotation(target.rotation);

            if (doOffset)
            {
                Quaternion forwardToUp = Quaternion.AngleAxis(90.0f * Mathf.Rad2Deg, Vector3.left);
                chain.Bones[0].AddRotation(forwardToUp);
            }
        }
    }
}