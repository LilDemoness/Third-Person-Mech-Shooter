using Gameplay.UI.Popups;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Gameplay.UI.Menus.Session
{
    public class LobbyCreationUI : Menu
    {
        private const int MIN_LOBBY_NAME_LENGTH = 3;
        private const int MAX_LOBBY_NAME_LENGTH = 16;

        private const int MIN_PASSWORD_LENGTH = 1;
        private const int MAX_PASSWORD_LENGTH = 16;


        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private GameObject _loadingIndicatorObject;

        [Space(10)]
        [SerializeField] private TMP_InputField _lobbyNameInputField;
        [SerializeField] private Toggle _isPrivate;
        [SerializeField] private Toggle _usePasswordToggle;
        [SerializeField] private TMP_InputField _lobbyPasswordInputField;


        [Inject]
        private LobbyUIMediator _lobbyUIMediator;


        private void Awake()
        {
            InitialiseUI();
            EnableUnityRelayUI();
        }
        private void OnDestroy()
        {
            _usePasswordToggle.onValueChanged.RemoveListener(OnUsePasswordToggleChanged);
            _lobbyPasswordInputField.onValueChanged.RemoveListener(OnLobbyPasswordChanged);
        }

        public override void Show()
        {
            base.Show();
            ResetUIValues();
        }


        #region UI Initialisation

        private void InitialiseUI()
        {
            InitialisePasswordUI();
            ResetUIValues();
        }
        private void InitialisePasswordUI()
        {
            _usePasswordToggle.onValueChanged.AddListener(OnUsePasswordToggleChanged);
            _lobbyPasswordInputField.onValueChanged.AddListener(OnLobbyPasswordChanged);
        }

        #endregion

        private void ResetUIValues()
        {
            _usePasswordToggle.SetIsOnWithoutNotify(false);
            _lobbyPasswordInputField.interactable = false;
            _lobbyPasswordInputField.SetTextWithoutNotify(string.Empty);
        }



        private void EnableUnityRelayUI()
        {
            _loadingIndicatorObject.SetActive(false);
        }

        public void OnCreateButtonPressed()
        {
            if (!IsLobbyNameValid())
            {
                PopupManager.ShowPopupPanel("Lobby Creation Error", $"Invalid Lobby Name\nLength must be between {MIN_LOBBY_NAME_LENGTH} & {MAX_LOBBY_NAME_LENGTH}");
                return;
            }
            if (_usePasswordToggle.isOn && !IsLobbyPasswordValid())
            {
                PopupManager.ShowPopupPanel("Lobby Creation Error", $"Invalid Lobby Password\nLength must be between {MIN_PASSWORD_LENGTH} & {MAX_PASSWORD_LENGTH}");
                return;
            }


            // Valid creation attempt.
            _lobbyUIMediator.CreateSessionRequest(_lobbyNameInputField.text, _isPrivate.isOn, (_usePasswordToggle.isOn ? _lobbyPasswordInputField.text : null));
        }



        private bool IsLobbyNameValid() => _lobbyNameInputField.text.Length >= MIN_LOBBY_NAME_LENGTH &&  _lobbyNameInputField.text.Length <= MAX_LOBBY_NAME_LENGTH;


        private void OnUsePasswordToggleChanged(bool usePassword) => _lobbyPasswordInputField.interactable = usePassword;
        private void OnLobbyPasswordChanged(string newValue)
        {
            _lobbyPasswordInputField.text = SanitisePasswordString(newValue);
        }
        private static string SanitisePasswordString(string dirtyString)
        {
            string output = Regex.Replace(dirtyString, "[^a-zA-z0-9]", "");
            return output[..Mathf.Min(output.Length, MAX_PASSWORD_LENGTH)];
        }


        private bool IsLobbyPasswordValid() => _lobbyPasswordInputField.text.Length >= MIN_PASSWORD_LENGTH && _lobbyPasswordInputField.text.Length <= MAX_PASSWORD_LENGTH;
    }
}