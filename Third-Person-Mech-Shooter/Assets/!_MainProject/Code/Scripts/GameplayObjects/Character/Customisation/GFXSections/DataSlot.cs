using UnityEngine;

namespace Gameplay.GameplayObjects.Character.Customisation.Sections
{
    public abstract class DataSlot<TData, TGFXSection> : MonoBehaviour where TGFXSection : GFXSection<TData>
    {
        [Header("GFX")]
        [SerializeField] private TGFXSection[] _gfxs;
        private int _activeGFXIndex = -1;


        /// <summary>
        ///     Toggles all SlotGFXSections under this AttachmentSlot and returns the value of the active element (If one exists).
        /// </summary>
        /// <returns> False if no elements were enabled.</returns>
        public bool Toggle(TData activeData)
        {
            _activeGFXIndex = -1;
            for (int i = 0; i < _gfxs.Length; ++i)
            {
                if (_gfxs[i].Toggle(activeData))
                    _activeGFXIndex = i;
            }

            return _activeGFXIndex != -1;
        }


        public bool HasActiveGFXSlot() => _activeGFXIndex >= 0 && _activeGFXIndex < _gfxs.Length;

        /// <summary>
        ///     Returns the associated data of the active GFX (Or default if none are active).
        /// </summary>
        public TData GetAssociatedData() => HasActiveGFXSlot() ? _gfxs[_activeGFXIndex].AssociatedData : default(TData);
        public ulong GetActionSourceObjectId() => HasActiveGFXSlot() ? _gfxs[_activeGFXIndex].GetAbilitySourceObjectId() : 0;

        public TGFXSection GetActiveGFXSection() => HasActiveGFXSlot() ? _gfxs[_activeGFXIndex] : null;
    }
}