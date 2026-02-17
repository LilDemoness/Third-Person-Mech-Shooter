using Gameplay.Configuration;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.UI;
using UnityServices.Auth;
using UnityServices.Sessions;
using Utils;
using VContainer;
using VContainer.Unity;
using Gameplay.UI.Menus;
using Gameplay.UI.Menus.Session;
using Gameplay.UI.Tooltips;
using UI;

namespace Gameplay.GameState
{
    /// <summary>
    ///     Game Logic that runs while sitting in the MainMenu. <br/>
    ///     This is likly to be "nothing" ad no game has started, but is is nonetheless
    ///     important to have a GameState, as the GameStateBehaviour system requires that all scenes have states
    /// </summary>
    /// <remarks>
    ///     OnNetworkSpawn() won't ever run because there is no network connection at the main menu screen.<br/>
    ///     Fortunately we know that you are a client, as all players are clients when sitting at the main menu screen.
    /// </remarks>
    public class ClientMainMenuState : GameStateBehaviour
    {
        public override GameState ActiveState => GameState.MainMenu;


        [SerializeField] private NameGenerationData _nameGenerationData;
        [SerializeField] private Button _sessionButton;
        [SerializeField] private GameObject _signInSpinner;
        [SerializeField] private UIProfileSelector _uiProfileSelector;
        [SerializeField] private UITooltipDetector _ugsSetupTooltipDetector;


        [Header("Menu References")]
        [SerializeField] private Menu _playGameMenu;
        [SerializeField] private Menu _profileMenu;
        [SerializeField] private Menu _optionsMenu;


        [Header("Component References")]
        [SerializeField] private LobbyUIMediator _lobbyUIMediator;
        [SerializeField] private DirectIPUI _directIPUI;


        [Inject]
        private AuthenticationServiceFacade _authServiceFacade;
        [Inject]
        private LocalSessionUser _localUser;
        [Inject]
        private LocalSession _localSession;
        [Inject]
        private ProfileManager _profileManager;


        protected override void Awake()
        {
            base.Awake();

            _sessionButton.interactable = false;
            _playGameMenu.Hide();

            if (string.IsNullOrEmpty(Application.cloudProjectId))
            {
                OnSignInFailed();
                return;
            }

            TrySignIn();

            Cursor.lockState = CursorLockMode.None;
        }
        protected override void OnDestroy()
        {
            if (_profileManager != null)
                _profileManager.OnProfileChanged -= OnProfileChanged;
            base.OnDestroy();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);
            builder.RegisterComponent(_nameGenerationData);
            builder.RegisterComponent(_lobbyUIMediator);
            builder.RegisterComponent(_directIPUI);
        }


        private async void TrySignIn()
        {
            try{
                InitializationOptions unityAuthenticationInitOptions = _authServiceFacade.GenerateAuthenticationOptions(_profileManager.Profile);

                await _authServiceFacade.InitialiseAndSignInAsync(unityAuthenticationInitOptions);
                OnAuthSignIn();
                _profileManager.OnProfileChanged += OnProfileChanged;
            }
            catch (System.Exception)
            {
                OnSignInFailed();
            }
        }


        private void OnAuthSignIn()
        {
            _sessionButton.interactable = true;
            _ugsSetupTooltipDetector.enabled = false;
            _signInSpinner.SetActive(false);

            Debug.Log($"Signed in. Unity Player ID {AuthenticationService.Instance.PlayerId}");

            _localUser.ID = AuthenticationService.Instance.PlayerId;

            // The local SessionUser object will be hooked into the UI before the LocalSession is population
            //  during session join, so the LocalSession must know about it already when that happens.
            _localSession.AddUser(_localUser);
        }
        private void OnSignInFailed()
        {
            if (_sessionButton != null)
            {
                _sessionButton.interactable = false;
                _ugsSetupTooltipDetector.enabled = true;
            }

            if (_signInSpinner)
            {
                _signInSpinner.SetActive(false);
            }
        }


        private async void OnProfileChanged()
        {
            _sessionButton.interactable = false;
            _signInSpinner.SetActive(true);
            await _authServiceFacade.SwitchProfileAndReSignInAsync(_profileManager.Profile);

            _sessionButton.interactable = true;
            _signInSpinner.SetActive(false);

            Debug.Log($"Signed in. Unity Player ID {AuthenticationService.Instance.PlayerId}");

            // Update the LocalUser and LocalSession.
            _localSession.RemoveUser(_localUser);
            _localUser.ID = AuthenticationService.Instance.PlayerId;
            _localSession.AddUser(_localUser);
        }



        #region UI Button Functions

        public void OnQuickJoinPressed()
        {
            _lobbyUIMediator.QuickJoinRequest(ignoreFilters: true);
        }
        public void OnPlayGamePressed()
        {
            MenuManager.SetActiveMenu(_playGameMenu);
        }
        public void OnArmouryPressed()
        {

        }
        public void OnProfilePressed()
        {
            MenuManager.SetActiveMenu(_profileMenu);
        }
        public void OnOptionsPressed()
        {
            MenuManager.SetActiveMenu(_optionsMenu);
        }
        public void OnQuitGamePressed()
        {
            Application.Quit();
        }

        #endregion

        public void OnChangeProfileClicked()
        {
            _uiProfileSelector.Show();
        }
        public void OnOpenSettingsClicked()
        {
        }
    }
}