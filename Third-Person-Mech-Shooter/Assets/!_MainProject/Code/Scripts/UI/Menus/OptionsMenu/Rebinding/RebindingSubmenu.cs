using UnityEngine;

namespace Gameplay.UI.Menus.Options
{
    public class RebindingSubmenu : OptionsSubmenu
    {
        protected override void OnDestroy()
        {
            base.OnDestroy();
            ControlsRebindingValue.Instance.UnsubscribeFromOnValueChanged(OnAnyOptionChanged);
        }
        public override void Open(bool selectFirstElement = true)
        {
            ControlsRebindingValue.Instance.SubscribeToOnValueChanged(OnAnyOptionChanged);
            base.Open(selectFirstElement);
        }
        protected override void FinishClose()
        {
            ControlsRebindingValue.Instance.UnsubscribeFromOnValueChanged(OnAnyOptionChanged);
        }


        public override void SaveAllOptionsToPrefs()
        {
            ControlsRebindingValue.Instance.SaveToPrefs();
            base.SaveAllOptionsToPrefs();
        }
        public override void LoadAllOptionsFromPrefs()
        {
            ControlsRebindingValue.Instance.LoadFromPrefs();
            base.LoadAllOptionsFromPrefs();
        }

        public override void ResetAllOptions()
        {
            ControlsRebindingValue.Instance.ResetValue();
            base.ResetAllOptions();
        }
    }
}