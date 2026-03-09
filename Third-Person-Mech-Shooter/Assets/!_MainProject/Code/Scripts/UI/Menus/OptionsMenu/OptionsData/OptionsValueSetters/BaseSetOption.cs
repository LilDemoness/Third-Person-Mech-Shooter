using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI.Menus.Options
{
    /// <summary>
    ///     Base class for components that change the value of a <see cref="OptionsValue{T}"/>,
    ///     such as option menu components.
    /// </summary>
    public abstract class BaseSetOption : MonoBehaviour
    {
        private SelectionNotifier[] _selectionNotifiers;


        public abstract Selectable PrimaryNavigationElement { get; }

        protected abstract BaseOptionsValue OptionsValue { get; }
        public static event System.Action OnAnyChanged;

        public static event System.Action<BaseOptionsValue> OnAnySelected;
        public static event System.Action OnAnyDeselected;


        protected virtual void Awake()
        {
            _selectionNotifiers = GetComponentsInChildren<SelectionNotifier>();
            for(int i = 0; i < _selectionNotifiers.Length; ++i)
            {
                _selectionNotifiers[i].OnSelected += NotifyOnSelected;
                _selectionNotifiers[i].OnDeselected += NotifyOnDeselected;
            }
        }
        protected virtual void OnDestroy()
        {
            if (_selectionNotifiers != null)
            {
                for(int i = 0; i < _selectionNotifiers.Length; ++i)
                {
                    _selectionNotifiers[i].OnSelected -= NotifyOnSelected;
                    _selectionNotifiers[i].OnDeselected -= NotifyOnDeselected;
                }
            }

            OptionsValue?.UnsubscribeFromOnValueChanged(OnOptionsValueChanged);
        }


        private void NotifyOnSelected(UnityEngine.EventSystems.BaseEventData _) => OnAnySelected?.Invoke(this.OptionsValue);
        private void NotifyOnDeselected(UnityEngine.EventSystems.BaseEventData _) => OnAnyDeselected?.Invoke();


        public virtual void Initialise()
        {
            OptionsValue.Init();
            OptionsValue.SubscribeToOnValueChanged(OnOptionsValueChanged);
            UpdateDisplayedValue();
        }
        public virtual void SaveToPrefs() => OptionsValue.SaveToPrefs();
        public virtual void LoadFromPrefs() => OptionsValue.LoadFromPrefs();
        public virtual void ResetValue() => OptionsValue.ResetValue();


        protected virtual void OnOptionsValueChanged()
        {
            UpdateDisplayedValue();
            OnAnyChanged?.Invoke();
        }
        protected abstract void UpdateDisplayedValue();


        public abstract void SetupNavigation(BaseSetOption onUp, BaseSetOption onDown);
    }
}