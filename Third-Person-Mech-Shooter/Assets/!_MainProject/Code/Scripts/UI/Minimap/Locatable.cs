using UnityEngine;

namespace Gameplay.UI.Minimap
{
    /// <summary>
    ///     Default implementation for a <see cref="BaseLocatable"/>.
    /// </summary>
    public class Locatable : BaseLocatable
    {
        [SerializeField] private BaseLocatableIcon _iconPrefab;

        [SerializeField]
        [Tooltip("Should the locatable be clamped to the edge of the minimap while out of the radar radius. If false, it is instead hidden.")]
        private bool _clampToRadarBorder = false;

        [SerializeField]
        [Tooltip("The type of the locatable (E.g. Enemy, Friendly, Objective).")]
        private LocatableType _locatableType;

        public override bool ClampToRadarBorder { get => _clampToRadarBorder; }
        public override LocatableType LocatableType { get => _locatableType; }
        public override BaseLocatableIcon CreateIcon() => Instantiate(_iconPrefab);
    }
}