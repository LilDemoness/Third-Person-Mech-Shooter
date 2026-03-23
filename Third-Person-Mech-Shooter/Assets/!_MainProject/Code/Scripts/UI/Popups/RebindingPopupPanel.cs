using UnityEngine;
using UnityEngine.InputSystem;

namespace Gameplay.UI.Popups
{
    public class RebindingPopupPanel : PopupPanel
    {
        private const string PREFAB_RESOURCES_PATH = "UI/RebindingPopupPrefab";
        private static RebindingPopupPanel m_instance;
        private static RebindingPopupPanel s_instance
        {
            get
            {
                if (m_instance == null)
                {
                    RebindingPopupPanel prefab = Resources.Load<RebindingPopupPanel>(PREFAB_RESOURCES_PATH);
                    m_instance = Instantiate<RebindingPopupPanel>(prefab);

                    s_instance.transform.SetParent(PopupManager.Root.transform);
                    s_instance.transform.localPosition = Vector2.zero;
                    (s_instance.transform as RectTransform).offsetMin = Vector2.zero;
                    (s_instance.transform as RectTransform).offsetMax = Vector2.zero;
                }

                return m_instance;
            }
        }

        private const string TITLE_FORMATTING_TEXT = "Rebinding: '{0}'";
        private const string BODY_TEXT = "...Press Any Key to Rebind...";

        public static void ShowForRebinding(RebindActionUI rebindUI, InputActionRebindingExtensions.RebindingOperation rebindOperation)
        {
            s_instance.SetupPopupPanel(string.Format(TITLE_FORMATTING_TEXT, rebindUI.ActionNameLabel.text), BODY_TEXT, false, false, null);
        }
        public static void Hide()
        {
            s_instance.Close();
        }
    }
}