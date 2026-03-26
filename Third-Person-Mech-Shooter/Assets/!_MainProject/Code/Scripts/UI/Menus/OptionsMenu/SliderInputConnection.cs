using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI.Menus.Options
{
    [RequireComponent(typeof(TMP_InputField))]
    public class SliderInputConnection : MonoBehaviour
    {
        [SerializeField] private SliderSetValue _slider;
        [SerializeField] private TMP_InputField _thisInputField;
        public Selectable Selectable => _thisInputField;


        private void Awake()
        {
            _thisInputField.onSubmit.AddListener(SetSliderValue);
            _slider.OnUpdateDisplayValue += UpdateTextToSliderValueNoNotify;
        }
        private void OnDestroy()
        {
            _thisInputField.onSubmit.RemoveListener(SetSliderValue);
            _slider.OnUpdateDisplayValue -= UpdateTextToSliderValueNoNotify;
        }


        private void SetSliderValue(string input)
        {
            if (!float.TryParse(input, out float floatInput))
            {
                // Invalid input. Reset to current value.
                UpdateTextToSliderValueNoNotify();
                return;
            }

            // Valid input. Update slider.
            _slider.SetValue(floatInput);
        }
        private void UpdateTextToSliderValueNoNotify() => _thisInputField.SetTextWithoutNotify(_slider.GetDisplayValue());
    }
}