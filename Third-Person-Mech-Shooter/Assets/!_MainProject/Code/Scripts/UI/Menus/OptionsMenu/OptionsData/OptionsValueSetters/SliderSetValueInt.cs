using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI.Menus.Options
{
    /// <summary>
    ///     Allows a <see cref="Slider"/> to set the value of an <see cref="IntOptionValue"/>, rounding its float value to the nearest int.
    /// </summary>
    public class SliderSetValueInt : SliderSetValue
    {
        [SerializeField] private Slider _slider;

        [SerializeField] private IntOptionValue _intOptionValue;
        protected override BaseOptionsValue OptionsValue => _intOptionValue;

        public override string GetDisplayValue() => _intOptionValue.Value.ToString();
        public override void SetValue(float newValue) => _slider.value = Mathf.RoundToInt(newValue);


        private void Awake() => _slider.onValueChanged.AddListener(OnSliderValueChanged);
        private void OnDestroy() => _slider.onValueChanged.RemoveListener(OnSliderValueChanged);


        public override void Initialise()
        {
            if (_intOptionValue.HasLimits)
            {
                _slider.minValue = _intOptionValue.MinValue;
                _slider.maxValue = _intOptionValue.MaxValue;
            }
            else
                Debug.LogWarning("Int Option without limits attached to a slider. How should we handle this?", this);

            base.Initialise();
        }


        public void OnSliderValueChanged(float newValue) => _intOptionValue.SetValue(Mathf.RoundToInt(newValue));
        protected override void OnOptionsValueChanged()
        {
            base.OnOptionsValueChanged();
            _slider.SetValueWithoutNotify(_intOptionValue.Value);
        }


#if UNITY_EDITOR

        private void OnValidate()
        {
            if (_intOptionValue == null)
                return;

            if (!_intOptionValue.HasLimits)
                Debug.LogWarning($"Int Option without limits attached to a slider.\n{this.name}", this);
        }

#endif
    }
}