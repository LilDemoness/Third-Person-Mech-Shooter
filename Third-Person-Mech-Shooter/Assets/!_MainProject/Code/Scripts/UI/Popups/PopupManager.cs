using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.UI.Popups
{
    /// <summary>
    ///     Handles the display of Popup Messages.<br/>
    ///     Uses Object Pooling to allow displaying multiple messages in succession.
    /// </summary>
    public class PopupManager : MonoBehaviour
    {
        private static PopupManager s_instance;
        public static GameObject Root => s_instance.gameObject;

        
        [SerializeField] private ModalPopup _popupPanelPrefab;
        [SerializeField] private GameObject _canvas;

        private List<ModalPopup> _popupPanels = new List<ModalPopup>();

        private const float OFFSET = 30.0f;
        private const float MAX_OFFSET = 200.0f;


        private void Awake()
        {
            if (s_instance != null)
                throw new System.Exception("Invalid State. Another PopupManager instance already exists");

            s_instance = this;
            DontDestroyOnLoad(_canvas);
        }
        private void OnDestroy()
        {
            s_instance = null;
        }



        /// <summary>
        ///     Shows a default popup with the button 'Close'.
        /// </summary>
        public static ModalPopup ShowDefaultPopup(string titleText, string contentText, Sprite contentSprite = null, LayoutOption layout = LayoutOption.Horizontal) => ShowPopup(titleText, contentText, contentSprite, layout, new PopupButtonParameters("Close", null));

        /// <summary>
        ///     Shows a default popup with no buttons.
        /// </summary>
        public static ModalPopup ShowAutoClosingPopup(string titleText, string contentText, Sprite contentSprite = null, LayoutOption layout = LayoutOption.Horizontal) => ShowPopup(titleText, contentText, contentSprite, layout, null);

        public static ModalPopup ShowPopup(string titleText, string bodyText, params PopupButtonParameters[] popupButtons) => ShowPopup(titleText, bodyText, null, LayoutOption.Horizontal, popupButtons);

        /// <summary>
        ///     Shows a fully configurable popup.
        /// </summary>
        /// <param name="titleText"> Title text of the Popup.<br/>Set to 'null' for no title.</param>
        /// <param name="bodyText"> Body text of the Popup.<br/>Set to 'null' for no body text.</param>
        /// <param name="contentSprite"> Sprite for the body image of the Popup.<br/>Set to 'null' to hide the body image.</param>
        /// <param name="layout"> Layout Method for the Popup's Body.</param>
        /// <param name="popupButtons"> Information for the popup's buttons.<br/>All buttons automatically close the popup once performed.</param>
        public static ModalPopup ShowPopup(string titleText, string bodyText, Sprite contentSprite = null, LayoutOption layout = LayoutOption.Horizontal, params PopupButtonParameters[] popupButtons)
        {
            if (s_instance == null)
            {
                Debug.LogError($"No PopupManager instance found. Cannot display message: {titleText}: {bodyText}");
                return null;
            }

            // Create a popup menu with buttons corresponding to the values of 'popupOptions'.
            return s_instance.DisplayPopupPanel(titleText, bodyText, contentSprite, layout, popupButtons);
        }


        private ModalPopup DisplayPopupPanel(string titleText, string mainText, Sprite contentSprite, LayoutOption layout, params PopupButtonParameters[] popupButtons)
        {
            ModalPopup popup = GetNextAvailablePopupPanel();
            if (popup != null)
            {
                popup.SetupModalWindow(mainText, contentSprite, layout, titleText, popupButtons);
            }

            return popup;
        }

        /// <summary>
        ///     Retrieve the next available PopupPanel instance, creating one if there are none available.
        /// </summary>
        private ModalPopup GetNextAvailablePopupPanel()
        {
            // Find the index of the first PopupPanel that is not displaying and has no popups after it that are currently displaying.
            int nextAvailablePopupIndex = 0;
            for(int i = 0; i < _popupPanels.Count; ++i)
            {
                if (_popupPanels[i].IsDisplaying)
                {
                    nextAvailablePopupIndex = i + 1;
                }
            }

            if (nextAvailablePopupIndex < _popupPanels.Count)
            {
                return _popupPanels[nextAvailablePopupIndex];
            }

            // None of our current PopupPanels are available, so instantiate a new one.
            // Create and position the panel.
            ModalPopup popupPanel = Instantiate(_popupPanelPrefab, this.transform);
            popupPanel.transform.position += new Vector3(1.0f, -1.0f) * (OFFSET * _popupPanels.Count % MAX_OFFSET);

            // Track the panel instance for future retrieval.
            if (popupPanel != null)
            {
                _popupPanels.Add(popupPanel);
            }
            else
            {
                Debug.LogError("PopupPanel prefab does not have a PopupPanel component!");
            }

            return popupPanel;
        }
    }
}