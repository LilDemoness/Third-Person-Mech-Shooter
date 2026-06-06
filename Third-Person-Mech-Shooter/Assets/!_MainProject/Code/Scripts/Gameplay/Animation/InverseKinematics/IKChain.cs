using EigenPort;

namespace Gameplay.Animations
{
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


        public Vector3 Start => new Vector3(Bones[0].Head.position.x, Bones[0].Head.position.y, Bones[0].Head.position.z);
        public float SqrMaxLength => MaxLength * MaxLength;
    }
}