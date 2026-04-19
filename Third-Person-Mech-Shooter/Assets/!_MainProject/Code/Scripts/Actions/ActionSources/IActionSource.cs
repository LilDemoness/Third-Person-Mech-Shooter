using UnityEngine;

namespace Gameplay.Actions
{
    /// <summary>
    ///     An interface for passing Action origin references using a NetworkObjectID. Requires a NetworkObject on the same GameObject.
    /// </summary>
    public interface IActionSource
    {
        /// <summary>
        ///     Get the origin transform for the passed transform relation, if one exists.<br/>
        ///     Transform relations link to a transform child within an object (E.g. Active Attachment Slot or Core System origin), allowing for the passing of child references between the Server and Clients effectively.
        /// </summary>
        public Transform GetOriginTransform(TransformRelation transformRelation);


    }
    public enum TransformRelation
    {
        Unset = 0,

        PrimaryModuleSlot,
        SecondaryModuleSlot,
        TertiaryModuleSlot,
        QuaternaryModuleSlot,

        CoreSystem,
    }

    public static class TransformRelationsExtensions
    {
        public static TransformRelation ToTransformRelation(this AttachmentSlotIndex attachmentSlotIndex)
            => attachmentSlotIndex switch
            {
                AttachmentSlotIndex.Primary => TransformRelation.PrimaryModuleSlot,
                AttachmentSlotIndex.Secondary => TransformRelation.SecondaryModuleSlot,
                AttachmentSlotIndex.Tertiary => TransformRelation.TertiaryModuleSlot,
                AttachmentSlotIndex.Quaternary => TransformRelation.QuaternaryModuleSlot,

                _ => throw new System.NotImplementedException()
            };
    }
}