using UnityEngine;

namespace Gameplay.UI.Minimap
{
    /// <summary>
    ///     Abstract base for the icon of a <see cref="BaseLocatable"/> to be displayed on the <see cref="Radar"/>.
    /// </summary>
    public abstract class BaseLocatableIcon : MonoBehaviour
    {
        public abstract void SetVisible(bool isVisible);
        public abstract void SetAlpha(float alpha);
    }
}