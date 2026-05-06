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
        private List<ModalPopup> _popupPanels = new List<ModalPopup>();

        [SerializeField] private ModalInputPopup _popupInputPanelPrefab;
        private List<ModalInputPopup> _popupInputPanels = new List<ModalInputPopup>();


        private const float OFFSET = 30.0f;
        private const float MAX_OFFSET = 200.0f;


        [SerializeField] private GameObject _canvas;


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

            // Retrieve/Create a popup panel and set it up with our desired values.
            ModalPopup popup = GetNextAvailablePanel<ModalPopup>(s_instance._popupPanels, s_instance._popupPanelPrefab);
            if (popup != null)
                popup.SetupModalWindow(bodyText, contentSprite, layout, titleText, popupButtons);

            return popup;
        }


        public static ModalInputPopup ShowInputPopup(string titleText, string bodyText, string inputPlaceholderText, System.Action onCancelCallback, System.Action<string> onSubmitCallback, System.Func<string, string> sanitiseTextFunc, System.Func<string, bool> isValidFunc)
        {
            if (s_instance == null)
            {
                Debug.LogError($"No PopupManager instance found. Cannot display input popup with title: {titleText}");
                return null;
            }

            // Retrieve/Create a popup input panel and set it up with our desired values.
            ModalInputPopup popup = GetNextAvailablePanel<ModalInputPopup>(s_instance._popupInputPanels, s_instance._popupInputPanelPrefab);
            if (popup != null)
                popup.SetupModalInputWindow(titleText, bodyText, inputPlaceholderText, onCancelCallback, onSubmitCallback, sanitiseTextFunc, isValidFunc);

            return popup;
        }


        /// <summary>
        ///     Retrieve the next available <typeparamref name="T"/> instance from <paramref name="existingPanels"/>,
        ///     creating a new one from <paramref name="panelPrefab"/> if there are none available.
        /// </summary>
        private static T GetNextAvailablePanel<T>(List<T> existingPanels, T panelPrefab) where T : MonoBehaviour, IModalPopup
        {
            // Find the index of the first PopupPanel that is not displaying and has no popups after it that are currently displaying.
            int nextAvailablePopupIndex = 0;
            for (int i = 0; i < existingPanels.Count; ++i)
            {
                if (existingPanels[i].IsDisplaying)
                {
                    nextAvailablePopupIndex = i + 1;
                }
            }

            if (nextAvailablePopupIndex < existingPanels.Count)
            {
                return existingPanels[nextAvailablePopupIndex];
            }

            // None of our current PopupPanels are available, so instantiate a new one.
            // Create and position the panel.
            T popupPanel = Instantiate(panelPrefab, s_instance.transform);
            popupPanel.transform.position += new Vector3(1.0f, -1.0f) * (OFFSET * existingPanels.Count % MAX_OFFSET);

            // Track the panel instance for future retrieval.
            if (popupPanel != null)
            {
                existingPanels.Add(popupPanel);
            }
            else
            {
                Debug.LogError("PopupPanel prefab does not have a PopupPanel component!");
            }

            return popupPanel;
        }
    }


    public interface IModalPopup
    {
        public bool IsDisplaying { get; }

        public event System.Action<IModalPopup> OnClose;


        public void Open();
        public void Close();
    }
}