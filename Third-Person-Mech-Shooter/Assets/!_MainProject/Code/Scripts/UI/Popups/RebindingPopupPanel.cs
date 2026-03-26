using UnityEngine.InputSystem;

namespace Gameplay.UI.Popups
{
    /// <summary>
    ///     Static utility functions for displaying a popup binding from an action rebinding request.
    /// </summary>
    public static class RebindingPopupPanel
    {
        private static ModalPopup s_activeInstance;

        private const string TITLE_FORMATTING_TEXT = "Rebinding: '{0}'";
        private const string BODY_TEXT = "...Press Any Key to Rebind...";

        public static void ShowForRebinding(RebindActionUI rebindUI, InputActionRebindingExtensions.RebindingOperation rebindOperation)
        {
            s_activeInstance = PopupManager.ShowPopup(string.Format(TITLE_FORMATTING_TEXT, rebindUI.ActionNameLabel.text), BODY_TEXT);
        }
        public static void Hide()
        {
            if (s_activeInstance)
                s_activeInstance.Close();
        }
    }
}