using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI.Menus.Options
{
    /// <summary>
    ///     Allows a <see cref="Toggle"/> to set the value of a <see cref="BoolOptionValue"/>.
    /// </summary>
    public class ToggleSetValue : BaseSetOption
    {
        [SerializeField] private Toggle _toggle;
        public override Selectable PrimaryNavigationElement => _toggle;


        [SerializeField] private BoolOptionValue _boolOptionValue;
        protected override BaseOptionsValue OptionsValue => _boolOptionValue;


        protected override void Awake()
        {
            base.Awake();
            _toggle.onValueChanged.AddListener(OnValueChanged);
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            _toggle.onValueChanged.RemoveListener(OnValueChanged);
        }


        public void OnValueChanged(bool newValue) => _boolOptionValue.SetValue(newValue);
        protected override void UpdateDisplayedValue()
        {
            _toggle.SetIsOnWithoutNotify(_boolOptionValue.Value);
        }


        public override void SetupNavigation(BaseSetOption onUp, BaseSetOption onDown)
        {
            _toggle.SetNavigation(null, null, onUp.PrimaryNavigationElement, onDown.PrimaryNavigationElement);
        }
    }
}