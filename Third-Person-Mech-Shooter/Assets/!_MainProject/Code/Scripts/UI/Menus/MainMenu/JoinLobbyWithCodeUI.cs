using UnityEngine;
using VContainer;
using System.Text.RegularExpressions;
using TMPro;

namespace Gameplay.UI.Menus
{
    public class JoinLobbyWithCodeUI : Menu
    {
        [SerializeField] private TMP_InputField _joinCodeField;

        private LobbyUIMediator _lobbyUIMediator;
        private LobbyBrowserUI _lobbyBrowserUI;

        [Inject]
        private void InjectDependenciesAndInitialize(
            LobbyUIMediator sessionUIMediator)
        {
            this._lobbyUIMediator = sessionUIMediator;
        }


        public override void Show()
        {
            base.Show();
            ResetJoinCodeInput();
        }

        public void ShowJoinLobbyWithCodePressed() => MenuManager.SetActivePopup(this);
        public void HidePressed() => MenuManager.ReturnToPreviousMenu();


        /// <summary>
        ///     Added to the Join Code InputField component's OnValueChanged callback.
        /// </summary>
        public void OnJoinCodeInputTextChanged()
        {
            _joinCodeField.text = SanitizeJoinCode(_joinCodeField.text);
        }

        private string SanitizeJoinCode(string dirtyString) => Regex.Replace(dirtyString.ToUpper(), "[^A-Z0-9]", "");
        private bool IsJoinCodeValid() => !string.IsNullOrEmpty(_joinCodeField.text);


        // Called via UI Button.
        public void OnJoinWithCodePressed()
        {
            if (IsJoinCodeValid())
                _lobbyUIMediator.JoinSessionWithCodeRequest(SanitizeJoinCode(_joinCodeField.text));
            else
                OnInvalidJoinAttemptPerformed();
        }
        public void ResetJoinCodeInput() => _joinCodeField.text = "";


        private void OnInvalidJoinAttemptPerformed()
        {
            Debug.Log("Invalid Join Code");
        }
    }
}