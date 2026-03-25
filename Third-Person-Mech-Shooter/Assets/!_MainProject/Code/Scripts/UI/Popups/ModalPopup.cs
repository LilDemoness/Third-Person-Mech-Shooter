using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UI;
using Gameplay.UI.Menus;
using System.Collections.Generic;

namespace Gameplay.UI.Popups
{
    public class ModalPopup : MonoBehaviour
    {
        public bool IsDisplaying { get; private set; }

        public System.Action<ModalPopup> OnClose;


        [Header("Header References")]
        [SerializeField] private GameObject _headerRoot;
        [SerializeField] private TMP_Text _titleText;


        [Header("Content References")]
        [SerializeField] private GameObject _contentRoot;

        [Space(5)]
        [SerializeField] private GameObject _horizontalContentRoot;
        [SerializeField] private Image _horizontalContentImage;
        [SerializeField] private TMP_Text _horizontalContentText;

        [Space(5)]
        [SerializeField] private GameObject _verticalContentRoot;
        [SerializeField] private Image _verticalContentImage;
        [SerializeField] private TMP_Text _verticalContentText;


        [Header("Footer References")]
        [SerializeField] private GameObject _footerRoot;
        [SerializeField] private GameObject _loadingSpinner;

        [Space(5)]
        [SerializeField] private Button _navigableButtonPrefab;
        [SerializeField] private NonNavigableButton _nonNavigableButtonPrefab;
        private GameObject[] _buttonInstances;



        private void OnDestroy() => Close();

        public virtual void Open()
        {
            IsDisplaying = true;
            this.gameObject.SetActive(true);
        }
        public virtual void Close()
        {
            IsDisplaying = false;
            this.gameObject.SetActive(false);

            OnClose?.Invoke(this);
            CleanupButtons();
        }


        /// <summary>
        ///     Setup and Show the Modal Window.
        /// </summary>
        public void SetupModalWindow(string bodyText, Sprite bodyImage = null, LayoutOption layoutOption = LayoutOption.Horizontal, string titleText = null, params PopupButtonParameters[] popupButtons)
        {
            SetupTitle(titleText);
            SetupContent(bodyText, bodyImage, layoutOption);
            SetupFooter(popupButtons);

            MenuManager.LinkPopup(this);
            Open();
        }

        private void SetupTitle(string titleText)
        {
            if (string.IsNullOrWhiteSpace(titleText))
            {
                _headerRoot.SetActive(false);
            }
            else
            {
                _headerRoot.SetActive(true);
                _titleText.text = titleText;
            }
        }
        private void SetupContent(string bodyText, Sprite bodyImage, LayoutOption layoutOption)
        {
            if (string.IsNullOrWhiteSpace(bodyText) && bodyImage == null)
            {
                _contentRoot.SetActive(false);
                return;
            }
            _contentRoot.SetActive(true);


            if (layoutOption == LayoutOption.Horizontal)
            {
                // Using Horizontal Layout.
                // Show the correct Layout.
                _verticalContentRoot.SetActive(false);
                _horizontalContentRoot.SetActive(true);

                // Body Text.
                _horizontalContentText.gameObject.SetActive(!string.IsNullOrWhiteSpace(bodyText));
                _horizontalContentText.text = bodyText;

                // Body Image.
                _horizontalContentImage.gameObject.SetActive(bodyImage != null);
                _horizontalContentImage.sprite = bodyImage;
            }
            else
            {
                // Using Vertical Layout.
                // Show the correct Layout.
                _horizontalContentRoot.SetActive(false);
                _verticalContentRoot.SetActive(true);

                // Body Text.
                _verticalContentText.gameObject.SetActive(!string.IsNullOrWhiteSpace(bodyText));
                _verticalContentText.text = bodyText;

                // Body Image.
                _verticalContentImage.gameObject.SetActive(bodyImage != null);
                _verticalContentImage.sprite = bodyImage;
            }
        }
        private void SetupFooter(params PopupButtonParameters[] popupButtons)
        {
            // The footer always displays.
            _footerRoot.SetActive(true);


            if (popupButtons == null || popupButtons.Length == 0)
                _loadingSpinner.SetActive(true); // No Buttons - Display loading spinner.
            else
            {
                _loadingSpinner.SetActive(false);   // There are buttons, so we don't need our loading spinner.

                // Setup Buttons.

                _buttonInstances = new GameObject[popupButtons.Length];
                for (int i = 0; i < popupButtons.Length; ++i)
                {
                    _buttonInstances[i] = CreateButton(popupButtons[i]);
                }
                SetupButtonNavigation();
            }
        }



        private GameObject CreateButton(PopupButtonParameters setupParameters) => setupParameters.TriggerButtonInput != null ? CreateNonNavigableButton(setupParameters).gameObject : CreateNavigableButton(setupParameters).gameObject;
        private Button CreateNavigableButton(PopupButtonParameters setupParameters)
        {
            // Create and setup the button.
            Button button = Instantiate<Button>(_navigableButtonPrefab, _footerRoot.transform);
            button.GetComponentInChildren<TMP_Text>().text = setupParameters.ButtonText;
            button.onClick.AddListener(CompleteCallback);

            // Return the newly created button.
            return button;


            void CompleteCallback()
            {
                Close();
                setupParameters.OnPressedCallback?.Invoke();
            }
        }
        private NonNavigableButton CreateNonNavigableButton(PopupButtonParameters setupParameters)
        {
            // Create and setup the button.
            NonNavigableButton button = Instantiate<NonNavigableButton>(_nonNavigableButtonPrefab, _footerRoot.transform);
            button.GetComponentInChildren<TMP_Text>().text = setupParameters.ButtonText;
            button.OnButtonTriggered += CompleteCallback;

            // Return the newly created button.
            return button;


            void CompleteCallback()
            {
                Close();
                setupParameters.OnPressedCallback?.Invoke();
            }
        }
        private void SetupButtonNavigation()
        {
            // Get all our selectable children.
            List<Selectable> selectableChildren = new List<Selectable>(_buttonInstances.Length);
            foreach(GameObject buttonInstance in _buttonInstances)
                if (buttonInstance.TryGetComponent<Selectable>(out Selectable selectable))
                    selectableChildren.Add(selectable);
            int selectableCount = selectableChildren.Count;

            // No selectables under our button container.
            if (selectableCount == 0)
                return;
            // Only one selectable, so no navigation is needed.
            else if (selectableCount == 1)
            {
                selectableChildren[0].RemoveNavigation();
                return;
            }

            // Multiple selectables. Setup their horizontal navigation.
            for (int i = 0; i < selectableCount; ++i)
            {
                if (i == 0)
                    selectableChildren[i].SetNavigation(onLeft: selectableChildren[selectableCount - 1], onRight: selectableChildren[i + 1]);
                else if (i == selectableCount - 1)
                    selectableChildren[i].SetNavigation(onLeft: selectableChildren[i - 1], onRight: selectableChildren[0]);
                else
                    selectableChildren[i].SetNavigation(onLeft: selectableChildren[i - 1], onRight: selectableChildren[i + 1]);
            }

            // Select the first button.
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(selectableChildren[0].gameObject);
        }
        private void CleanupButtons()
        {
            if (!_footerRoot || _buttonInstances == null)
                return;

            foreach(GameObject button in _buttonInstances)
                Destroy(button);
            _buttonInstances = null;
        }
    }

    [System.Serializable]
    public enum LayoutOption { Horizontal, Vertical }
}