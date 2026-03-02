using UnityEngine;

namespace Gameplay.UI.Menus.Options
{
    /// <summary>
    ///     Base class for components that change the value of a <see cref="OptionsValue{T}"/>,
    ///     such as option menu components.
    /// </summary>
    public abstract class BaseSetOption : MonoBehaviour
    {
        protected abstract BaseOptionsValue OptionsValue { get; }
        public static event System.Action OnAnyChanged;

        private void OnDestroy() => OptionsValue?.UnsubscribeFromOnValueChanged(OnOptionsValueChanged);

        public virtual void Initialise()
        {
            OptionsValue.Init();
            OptionsValue.SubscribeToOnValueChangedAndTryTrigger(OnOptionsValueChanged);
        }
        public virtual void SaveToPrefs() => OptionsValue.SaveToPrefs();
        public virtual void LoadFromPrefs() => OptionsValue.LoadFromPrefs();
        public virtual void ResetValue() => OptionsValue.ResetValue();


        protected virtual void OnOptionsValueChanged()
        {
            OnAnyChanged?.Invoke();
        }
    }
}