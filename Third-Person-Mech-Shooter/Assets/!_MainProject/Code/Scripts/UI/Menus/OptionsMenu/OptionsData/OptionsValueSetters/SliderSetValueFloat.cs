using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI.Menus.Options
{
    /// <summary>
    ///     Allows a <see cref="Slider"/> to set the value of <see cref="FloatOptionValue"/>.
    /// </summary>
    public class SliderSetValueFloat : SliderSetValue
    {
        [SerializeField] private FloatOptionValue _floatOptionValue;
        protected override BaseOptionsValue OptionsValue => _floatOptionValue;

        public override string GetDisplayValue() => _floatOptionValue.Value.ToString();
        public override void SetValue(float newValue) => Slider.value = newValue;


        protected override void Awake()
        {
            base.Awake();
            Slider.onValueChanged.AddListener(OnSliderValueChanged);
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            Slider.onValueChanged.RemoveListener(OnSliderValueChanged);
        }


        public override void Initialise()
        {
            if (_floatOptionValue.HasLimits)
            {
                Slider.minValue = _floatOptionValue.MinValue;
                Slider.maxValue = _floatOptionValue.MaxValue;
            }
            else
                Debug.LogWarning("Float Option without limits attached to a slider. How should we handle this?");

            base.Initialise();
        }


        public void OnSliderValueChanged(float newValue) => _floatOptionValue.SetValue(newValue);
        protected override void OnOptionsValueChanged()
        {
            Slider.SetValueWithoutNotify(_floatOptionValue.Value);
            base.OnOptionsValueChanged();
        }


#if UNITY_EDITOR

        private void OnValidate()
        {
            if (_floatOptionValue == null)
                return;

            if (!_floatOptionValue.HasLimits)
                Debug.LogWarning($"Float Option without limits attached to a slider.\n{this.name}", this);
        }

#endif
    }


    public abstract class SliderSetValue : BaseSetOption
    {
        [SerializeField] protected Slider Slider;
        [SerializeField] protected SliderInputConnection SliderInputConnection;
        public override Selectable PrimaryNavigationElement => Slider;


        public event System.Action OnUpdateDisplayValue;
        public abstract string GetDisplayValue();

        public abstract void SetValue(float value);

        protected override void OnOptionsValueChanged()
        {
            OnUpdateDisplayValue?.Invoke();
            base.OnOptionsValueChanged();
        }


        public override void SetupNavigation(BaseSetOption onUp, BaseSetOption onDown)
        {
            Slider.SetNavigation(null, null, onUp.PrimaryNavigationElement, onDown.PrimaryNavigationElement);
            SliderInputConnection.Selectable.SetNavigation(Slider, null, onUp.PrimaryNavigationElement, onDown.PrimaryNavigationElement);
        }
    }
}