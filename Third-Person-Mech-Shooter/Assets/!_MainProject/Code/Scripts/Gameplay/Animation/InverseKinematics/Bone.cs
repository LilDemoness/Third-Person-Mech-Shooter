using UnityEngine;

namespace Gameplay.Animations
{
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
}