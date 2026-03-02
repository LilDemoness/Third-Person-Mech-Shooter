using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI.Menus.Options
{
    /// <summary>
    ///     Allows a <see cref="Slider"/> to set the value of <see cref="FloatOptionValue"/>.
    /// </summary>
    public class SliderSetValueFloat : SliderSetValue
    {
        [SerializeField] private Slider _slider;

        [SerializeField] private FloatOptionValue _floatOptionValue;
        protected override BaseOptionsValue OptionsValue => _floatOptionValue;

        public override string GetDisplayValue() => _floatOptionValue.Value.ToString();
        public override void SetValue(float newValue) => _slider.value = newValue;


        private void Awake() => _slider.onValueChanged.AddListener(OnSliderValueChanged);
        private void OnDestroy() => _slider.onValueChanged.RemoveListener(OnSliderValueChanged);


        public override void Initialise()
        {
            if (_floatOptionValue.HasLimits)
            {
                _slider.minValue = _floatOptionValue.MinValue;
                _slider.maxValue = _floatOptionValue.MaxValue;
            }
            else
                Debug.LogWarning("Float Option without limits attached to a slider. How should we handle this?");

            base.Initialise();
        }


        public void OnSliderValueChanged(float newValue) => _floatOptionValue.SetValue(newValue);
        protected override void OnOptionsValueChanged()
        {
            _slider.SetValueWithoutNotify(_floatOptionValue.Value);
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
        public event System.Action OnUpdateDisplayValue;
        public abstract string GetDisplayValue();

        public abstract void SetValue(float value);

        protected override void OnOptionsValueChanged()
        {
            OnUpdateDisplayValue?.Invoke();
            base.OnOptionsValueChanged();
        }
    }
}