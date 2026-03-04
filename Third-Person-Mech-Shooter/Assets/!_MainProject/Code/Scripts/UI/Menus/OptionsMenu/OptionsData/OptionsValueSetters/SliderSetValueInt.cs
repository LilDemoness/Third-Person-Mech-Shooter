using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI.Menus.Options
{
    /// <summary>
    ///     Allows a <see cref="UnityEngine.UI.Slider"/> to set the value of an <see cref="IntOptionValue"/>, rounding its float value to the nearest int.
    /// </summary>
    public class SliderSetValueInt : SliderSetValue
    {
        [SerializeField] private IntOptionValue _intOptionValue;
        protected override BaseOptionsValue OptionsValue => _intOptionValue;

        public override string GetDisplayValue() => _intOptionValue.Value.ToString();
        public override void SetValue(float newValue) => Slider.value = Mathf.RoundToInt(newValue);


        private void Awake() => Slider.onValueChanged.AddListener(OnSliderValueChanged);
        private void OnDestroy() => Slider.onValueChanged.RemoveListener(OnSliderValueChanged);


        public override void Initialise()
        {
            if (_intOptionValue.HasLimits)
            {
                Slider.minValue = _intOptionValue.MinValue;
                Slider.maxValue = _intOptionValue.MaxValue;
            }
            else
                Debug.LogWarning("Int Option without limits attached to a slider. How should we handle this?", this);

            base.Initialise();
        }


        public void OnSliderValueChanged(float newValue) => _intOptionValue.SetValue(Mathf.RoundToInt(newValue));
        protected override void OnOptionsValueChanged()
        {
            base.OnOptionsValueChanged();
            Slider.SetValueWithoutNotify(_intOptionValue.Value);
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