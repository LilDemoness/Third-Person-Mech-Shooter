using UnityEngine;

namespace Gameplay.UI.Minimap
{
    /// <summary>
    ///     Abstract base for an object that should be locatable by the <see cref="Radar"/>.
    /// </summary>
    public abstract class BaseLocatable : MonoBehaviour
    {
        public abstract bool ClampToRadarBorder { get; }
        public abstract LocatableType LocatableType { get; }

        // <this, Old Value>
        public event System.Action<BaseLocatable, LocatableType> OnLocatableTypeChanged;


        protected virtual void OnEnable() => LocatableManager.Register(this);
        protected virtual void OnDisable() => LocatableManager.Deregister(this);

        public abstract BaseLocatableIcon CreateIcon();

        protected void InvokeOnLocatableTypeChanged(LocatableType oldLocatableType) => OnLocatableTypeChanged?.Invoke(this, oldLocatableType);
    }


    public enum LocatableType
    {
        Friendly,
        Enemy,
        Objective
    }
}