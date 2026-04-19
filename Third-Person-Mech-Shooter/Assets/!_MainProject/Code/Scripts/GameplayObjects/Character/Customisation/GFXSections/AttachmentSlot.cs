using System.Collections.Generic;
using UnityEngine;
using Gameplay.GameplayObjects.Character.Customisation.Data;

namespace Gameplay.GameplayObjects.Character.Customisation.Sections
{
    /// <summary>
    ///     An attachment slot on a frame.
    /// </summary>
    public class AttachmentSlot : DataSlot<ModuleData, SlotGFXSection>
    {
        [SerializeField] private AttachmentSlotIndex _slotIndex = AttachmentSlotIndex.Primary;
        public AttachmentSlotIndex AttachmentSlotIndex => _slotIndex;
    }
}