using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Gameplay.UI.Menus;

namespace Gameplay.UI.Popups
{
    public class ModalInputPopup : MonoBehaviour, IModalPopup
    {
        private bool _isDisplaying;
        public bool IsDisplaying
        {
            get => _isDisplaying;
            private set => _isDisplaying = value;
        }


        public event System.Action<IModalPopup> OnClose;


        [Header("Header References")]
        [SerializeField] private GameObject _headerRoot;
        [SerializeField] private TMP_Text _titleText;


        [Header("Content References")]
        [SerializeField] private VerticalLayoutGroup _contentLayoutGroup;

        [Space(5)]
        [SerializeField] private GameObject _bodyTextRoot;
        [SerializeField] private TMP_Text _bodyText;

        [Space(5)]
        [SerializeField] private TMP_InputField _inputField;
        [SerializeField] private TMP_Text _inputPlaceholderText;

        private System.Func<string, string> _sanitiseTextFunc;
        private System.Func<string, bool> _isValidFunc;


        [Header("Footer References")]
        [SerializeField] private Button _cancelButton;
        [SerializeField] private Button _submitButton;


        private void Awake()
        {
            _inputField.onValueChanged.AddListener(OnInputFieldValueChanged);
        }
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
            Cleanup();
        }


        public void SetupModalInputWindow(string titleText, string bodyText, LayoutOption layoutOption, string inputPlaceholderText, System.Action onCancelCallback, System.Action<string> onSubmitCallback, System.Func<string, string> sanitiseTextFunc, System.Func<string, bool> isValidFunc)
        {
            // Setup the Modal Window Components.
            SetupTitle(titleText);
            SetupBodyText(bodyText);
            _contentLayoutGroup.reverseArrangement = layoutOption == LayoutOption.ContentsBelow;
            SetupInputField(inputPlaceholderText, sanitiseTextFunc, isValidFunc);
            SetupButtons(onCancelCallback, onSubmitCallback);

            // Initial Navigation Selection.
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(_inputField.gameObject);

            // Ensure that we account for the initial value with our sanitisation and validation callbacks.
            OnInputFieldValueChanged(string.Empty);

            // Finish Linking the Popup and show it.
            MenuManager.LinkPopup(this);
            Open();
        }



        private void SetupTitle(string titleText)
        {
            if (string.IsNullOrWhiteSpace(titleText))
                _headerRoot.SetActive(false);
            else
            {
                _headerRoot.SetActive(true);
                _titleText.text = titleText;
            }
        }
        private void SetupBodyText(string bodyText)
        {
            if (string.IsNullOrWhiteSpace(bodyText))
                _bodyTextRoot.SetActive(false);
            else
            {
                _bodyTextRoot.SetActive(true);
                _bodyText.text = bodyText;
            }
        }
        private void SetupInputField(string inputPlaceholderText, System.Func<string, string> sanitiseTextFunc, System.Func<string, bool> isValidFunc)
        {
            _inputPlaceholderText.text = inputPlaceholderText;

            _sanitiseTextFunc = sanitiseTextFunc;
            _isValidFunc = isValidFunc;
        }
        private void SetupButtons(System.Action onCancelCallback, System.Action<string> onSubmitCallback)
        {
            _cancelButton.onClick.AddListener(CancelCallback);
            _submitButton.onClick.AddListener(SubmitCallback);


            void CancelCallback()
            {
                Close();
                onCancelCallback?.Invoke();
            }
            void SubmitCallback()
            {
                Close();
                onSubmitCallback?.Invoke(_inputField.text);
            }
        }


        private void Cleanup()
        {
            // Cleanup Buttons.
            _cancelButton.onClick.RemoveAllListeners();
            _submitButton.onClick.RemoveAllListeners();
        }


        private void OnInputFieldValueChanged(string _)
        {
            if (_sanitiseTextFunc != null)
                _inputField.text = _sanitiseTextFunc(_inputField.text);

            _submitButton.interactable = _isValidFunc == null ? true : _isValidFunc(_inputField.text);
        }


        [System.Serializable]
        public enum LayoutOption
        {
            // Display the body text below the input field.
            ContentsBelow,
            // Display the body text above the input field.
            ContentsAbove
        }
    }
}