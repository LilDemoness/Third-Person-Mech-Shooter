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

        [SerializeField] private BoolOptionValue _boolOptionValue;
        protected override BaseOptionsValue OptionsValue => _boolOptionValue;


        private void Awake() => _toggle.onValueChanged.AddListener(OnValueChanged);
        private void OnDestroy() => _toggle.onValueChanged.RemoveListener(OnValueChanged);


        public void OnValueChanged(bool newValue) => _boolOptionValue.SetValue(newValue);
        protected override void OnOptionsValueChanged() => _toggle.SetIsOnWithoutNotify(_boolOptionValue.Value);
    }
}