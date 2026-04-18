using UnityEngine;

namespace Gameplay.UI.Minimap
{
    /// <summary>
    ///     Default implementation for a <see cref="BaseLocatableIcon"/>.
    /// </summary>
    public class LocatableIcon : BaseLocatableIcon
    {
        [SerializeField] private CanvasGroup _canvasGroup;

        public override void SetVisible(bool isVisible) => _canvasGroup.alpha = isVisible ? 1.0f : 0.0f;
        public override void SetAlpha(float alpha) => _canvasGroup.alpha = alpha;
    }
}