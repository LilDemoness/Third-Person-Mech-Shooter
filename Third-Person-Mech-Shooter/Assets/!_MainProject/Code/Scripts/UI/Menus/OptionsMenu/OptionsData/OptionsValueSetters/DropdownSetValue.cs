using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace Gameplay.UI.Menus.Options
{
    /// <summary>
    ///     Allows a <see cref="TMP_Dropdown"/> to set the value of any <see cref="IDropdownSupportingOptionValue"/>.
    /// </summary>
    public class DropdownSetValue : BaseSetOption
    {
        [SerializeField] private TMP_Dropdown _dropdown;
        public override Selectable PrimaryNavigationElement => _dropdown;



        [SerializeField] private BaseOptionsValue m_optionsValue;
        private IDropdownSupportingOptionValue _optionsValue => (m_optionsValue as IDropdownSupportingOptionValue);
        protected override BaseOptionsValue OptionsValue => (_optionsValue as BaseOptionsValue);


        protected override void Awake()
        {
            base.Awake();
            _dropdown.onValueChanged.AddListener(OnValueChanged);
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            _dropdown.onValueChanged.RemoveListener(OnValueChanged);
        }

        public override void Initialise()
        {
            base.Initialise();
            InitialiseDropdownWithResolutions();
        }

        public void OnValueChanged(int resolutionValue) => _optionsValue.SetValue(resolutionValue);
        protected override void UpdateDisplayedValue()
        {
            _dropdown.SetValueWithoutNotify(_optionsValue.GetSelectedOptionIndex());
        }

        private void InitialiseDropdownWithResolutions()
        {
            _dropdown.ClearOptions();
            _dropdown.AddOptions(_optionsValue.GetDropdownOptions());
            _dropdown.SetValueWithoutNotify(_optionsValue.GetSelectedOptionIndex());
        }


        public override void SetupNavigation(BaseSetOption onUp, BaseSetOption onDown) => _dropdown.SetNavigation(null, null, onUp.PrimaryNavigationElement, onDown.PrimaryNavigationElement);
        


#if UNITY_EDITOR

        private void OnValidate()
        {
            if (m_optionsValue == null)
                return;

            if (m_optionsValue is not IDropdownSupportingOptionValue)
            {
                Debug.LogError("Error: Value assigned to DropdownSetValue is not an IDropdownSupportingOptionValue, and is therefore invalid.", this);
                m_optionsValue = null;
            }
        }

#endif
    }
}