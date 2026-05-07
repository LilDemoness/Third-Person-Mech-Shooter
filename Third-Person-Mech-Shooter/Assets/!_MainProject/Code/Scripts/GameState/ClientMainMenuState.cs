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
using Gameplay.UI.Tooltips;
using ApplicationLifecycle.Messages;
using Infrastructure;
using Cysharp.Threading.Tasks;

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
        [SerializeField] private GameObject _loadingSpinner;
        [SerializeField] private UIProfileSelector _uiProfileSelector;
        [SerializeField] private UITooltipDetector _ugsSetupTooltipDetector;

        [Space(5)]
        [SerializeField] private CanvasGroup _rootCanvasGroup;

        [Space(5)]
        [SerializeField] private Button _firstSelectedElement;


        [Header("Menu References")]
        [SerializeField] private Menu _playGameMenu;
        [SerializeField] private Menu _profileMenu;
        [SerializeField] private Menu _optionsMenu;



        [Header("Component References")]
        [SerializeField] private LobbyUIMediator _lobbyUIMediator;
        [SerializeField] private DirectIPUI _directIPUI;

        private ButtonSelectionIndicator _selectedMenuRootButton;


        [Inject]
        private AuthenticationServiceFacade _authServiceFacade;
        [Inject]
        private LocalSessionUser _localUser;
        [Inject]
        private LocalSession _localSession;
        [Inject]
        IPublisher<QuitApplicationMessage> _quitApplicationPub;


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
        protected override void Start()
        {
            base.Start();
            LoadActiveProfile();
        }
        protected override void OnDestroy()
        {
            ProfileManager.OnProfileChanged -= OnProfileChanged;
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
                InitializationOptions unityAuthenticationInitOptions = _authServiceFacade.GenerateAuthenticationOptions(ProfileManager.GetActiveProfile());

                await _authServiceFacade.InitialiseAndSignInAsync(unityAuthenticationInitOptions);
                OnAuthSignIn();
                ProfileManager.OnProfileChanged += OnProfileChanged;
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
            _loadingSpinner.SetActive(false);

            Debug.Log($"Signed in. ID: {AuthenticationService.Instance.PlayerId}\nName: {ProfileManager.GetActiveProfile()}");

            _localUser.ID = AuthenticationService.Instance.PlayerId;
            _localUser.DisplayName = ProfileManager.GetActiveProfile();

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

            if (_loadingSpinner)
                _loadingSpinner.SetActive(false);
        }


        [ContextMenu("Test/Load Active Profile")]
        public void TestLoadActiveProfile() => LoadActiveProfile();
        //private async UniTask LoadActiveProfile()
        private void LoadActiveProfile()
        {
            // Try to load the active profile.
            if (ProfileManager.TryLoadActiveProfile())
                return; // Successfully loaded active profile.

            // Failed to load the active profile.
            // Force the user to create a new profile via the profile select menu.
            OnProfilePressed(_firstSelectedElement);
        }
        private async void OnProfileChanged()
        {
            _sessionButton.interactable = false;
            _loadingSpinner.SetActive(true);
            await _authServiceFacade.SwitchProfileAndReSignInAsync(ProfileManager.GetActiveProfile());

            _sessionButton.interactable = true;
            _loadingSpinner.SetActive(false);

            Debug.Log($"Signed in. ID: {AuthenticationService.Instance.PlayerId}\nName: {ProfileManager.GetActiveProfile()}");

            // Update the LocalUser and LocalSession.
            _localSession.RemoveUser(_localUser);
            _localUser.ID = AuthenticationService.Instance.PlayerId;
            _localUser.DisplayName = ProfileManager.GetActiveProfile();
            _localSession.AddUser(_localUser);
        }



        #region UI Button Functions

        public void OnQuickJoinPressed() => TryQuickJoin().Forget();
        public void OnPlayGamePressed(Button sender)
        {
            ChangeSelectedMenuIndicator(sender.GetComponent<ButtonSelectionIndicator>());
            MenuManager.SetActiveMenu(_playGameMenu, sender);
        }
        public void OnArmouryPressed(Button sender)
        {
            ChangeSelectedMenuIndicator(sender.GetComponent<ButtonSelectionIndicator>());
            //MenuManager.SetActiveMenu(_armouryMenu, sender);
            MenuManager.CloseAllMenus();
        }
        public void OnProfilePressed(Button sender)
        {
            ChangeSelectedMenuIndicator(sender.GetComponent<ButtonSelectionIndicator>());
            MenuManager.SetActiveMenu(_profileMenu, sender);
        }
        public void OnOptionsPressed(Button sender)
        {
            ChangeSelectedMenuIndicator(sender.GetComponent<ButtonSelectionIndicator>());
            MenuManager.SetActiveMenu(_optionsMenu, sender);
        }
        public void OnQuitGamePressed() => _quitApplicationPub.Publish(new QuitApplicationMessage());

        #endregion

        private async UniTaskVoid TryQuickJoin()
        {
            // Try to close all menus before sending the request to prevent players from bypassing close requirements through Quick Joins.
            bool success = await MenuManager.CloseAllMenusUniTask();
            if (!success)
                return; // We can't close all our menus, so we cannot perform a quick join.

            // Prevent the player from sending additional requests.
            _rootCanvasGroup.interactable = false;
            _loadingSpinner.SetActive(true);

            // Send the request.
            await _lobbyUIMediator.QuickJoinRequest(ignoreFilters: true);

            // If we are still in the Main Menu (E.g. Request Failed), re-enable interaction.
            if (_loadingSpinner != null)
                _loadingSpinner.SetActive(false);
            if (_rootCanvasGroup != null)
                _rootCanvasGroup.interactable = true;
        }


        private void ChangeSelectedMenuIndicator(ButtonSelectionIndicator selectionIndicator)
        {
            _selectedMenuRootButton?.OnTabExited();
            selectionIndicator?.OnTabEntered();
            _selectedMenuRootButton = selectionIndicator;
        }


        public void OnChangeProfileClicked()
        {
            _uiProfileSelector.Show();
        }
    }
}