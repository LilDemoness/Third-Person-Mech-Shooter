using TMPro;
using UnityEngine;

namespace Gameplay.UI.Menus.Options
{
    public class OptionsDataDisplay : MonoBehaviour
    {
        [SerializeField] private TMP_Text _optionTitleText;
        [SerializeField] private TMP_Text _optionDescriptionText;


        private void Awake()
        {
            ClearText();
            BaseSetOption.OnAnySelected += DisplayForOption;
            BaseSetOption.OnAnyDeselected += ClearText;
        }
        private void OnDestroy()
        {
            BaseSetOption.OnAnySelected -= DisplayForOption;
            BaseSetOption.OnAnyDeselected -= ClearText;
        }


        public void DisplayForOption(BaseOptionsValue optionsValue)
        {
            _optionTitleText.text = optionsValue.Title;
            _optionDescriptionText.text = optionsValue.Description;
        }
        public void ClearText()
        {
            _optionTitleText.text = "";
            _optionDescriptionText.text = "";
        }
    }
}