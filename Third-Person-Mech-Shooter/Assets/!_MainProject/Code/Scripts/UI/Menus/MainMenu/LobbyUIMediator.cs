using UnityEngine;
using VContainer;
using Infrastructure;
using UnityServices.Sessions;
using Unity.Services.Multiplayer;
using Netcode.ConnectionManagement;
using Unity.Services.Core;
using UnityServices.Auth;

namespace Gameplay.UI.Menus
{
    /// <summary>
    ///     Facilitates lobby-based calls to the Multiplayer SDK.
    /// </summary>
    public class LobbyUIMediator : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private GameObject _loadingSpinner;


        #region VContainer Dependency Injection

        private AuthenticationServiceFacade _authenticationServiceFacade;
        private MultiplayerServicesFacade _multiplayerServicesFacade;
        private LocalSessionUser _localUser;
        private LocalSession _localSession;
        private ConnectionManager _connectionManager;
        private ISubscriber<ConnectStatus> _connectStatusSubscriber;

        private const string DEFAULT_SESSION_NAME = "no-name";
        private const int MAX_PLAYERS = 8;

        private ISession _session;


        [Inject]
        private void InjectDependenciesAndInitialise(
            AuthenticationServiceFacade authenticationServiceFacade,
            MultiplayerServicesFacade multiplayerServicesFacade,
            LocalSessionUser localSessionUser,
            LocalSession localSession,
            ISubscriber<ConnectStatus> connectStatusSubscriber,
            ConnectionManager connectionManager)
        {
            this._authenticationServiceFacade = authenticationServiceFacade;
            this._multiplayerServicesFacade = multiplayerServicesFacade;
            this._localUser = localSessionUser;
            this._localSession = localSession;
            this._connectStatusSubscriber = connectStatusSubscriber;
            this._connectionManager = connectionManager;

            _connectStatusSubscriber.Subscribe(OnConnectStatus);
        }

        #endregion


        private void OnConnectStatus(ConnectStatus status)
        {
            if (status is ConnectStatus.GenericDisconnect or ConnectStatus.StartClientFailed)
            {
                UnblockUIAfterLoadingIsComplete();
            }
        }

        private void OnDestroy()
        {
            _connectStatusSubscriber?.Unsubscribe(OnConnectStatus);
        }


        #region Create Requests

        public async void CreateSessionRequest(string sessionName, bool isPrivate, string sessionPassword = null)
        {
            // Before sending a request, populate an empty session name, if necessary.
            if (string.IsNullOrEmpty(sessionName))
            {
                sessionName = DEFAULT_SESSION_NAME;
            }

            BlockUIWhileLoadingIsInProgress();

            bool isPlayerAuthorized = await _authenticationServiceFacade.EnsurePlayerIsAuthorized();
            if (!isPlayerAuthorized)
            {
                UnblockUIAfterLoadingIsComplete();
            }

            _connectionManager.StartHostSession(_localUser.DisplayName);
            var result = await _multiplayerServicesFacade.TryCreateSessionAsync(sessionName, MAX_PLAYERS, isPrivate, sessionPassword);

            HandleSessionJoinResult(result);
        }

        #endregion


        #region Join Requests

        public async void JoinSessionWithCodeRequest(string sessionCode)
        {
            BlockUIWhileLoadingIsInProgress();

            bool isPlayerAuthorized = await _authenticationServiceFacade.EnsurePlayerIsAuthorized();
            if (!isPlayerAuthorized)
            {
                UnblockUIAfterLoadingIsComplete();
                return;
            }

            _connectionManager.StartClientSession(_localUser.DisplayName);

            var result = await _multiplayerServicesFacade.TryJoinSessionByCodeAsync(sessionCode);

            HandleSessionJoinResult(result);
        }

        public async void JoinSessionRequest(ISessionInfo sessionInfo)
        {
            BlockUIWhileLoadingIsInProgress();

            bool isPlayerAuthorized = await _authenticationServiceFacade.EnsurePlayerIsAuthorized();
            if (!isPlayerAuthorized)
            {
                UnblockUIAfterLoadingIsComplete();
                return;
            }

            _connectionManager.StartClientSession(_localUser.DisplayName);

            var result = await _multiplayerServicesFacade.TryJoinSessionByNameAsync(sessionInfo.Id);

            HandleSessionJoinResult(result);
        }

        public async void QuickJoinRequest(bool ignoreFilters = false)
        {
            BlockUIWhileLoadingIsInProgress();

            bool isPlayerAuthorized = await _authenticationServiceFacade.EnsurePlayerIsAuthorized();
            if (!isPlayerAuthorized)
            {
                UnblockUIAfterLoadingIsComplete();
                return;
            }

            _connectionManager.StartHostSession(_localUser.DisplayName);

            var result = await _multiplayerServicesFacade.TryQuickJoinSessionAsync(ignoreFilters);

            HandleSessionJoinResult(result);
        }

        #endregion


        #region Lobby Querying

        public async void QueryLobbiesRequest(bool blockUI)
        {
            if (Unity.Services.Core.UnityServices.State != ServicesInitializationState.Initialized)
                return; // Services are uninitialised.

            if (blockUI)
                BlockUIWhileLoadingIsInProgress();

            bool playerIsAuthorised = await _authenticationServiceFacade.EnsurePlayerIsAuthorized();
            if (blockUI && !playerIsAuthorised)
            {
                UnblockUIAfterLoadingIsComplete();
                return;
            }

            await _multiplayerServicesFacade.RetrieveAndPublishSessionListAsync();

            if (blockUI)
            {
                UnblockUIAfterLoadingIsComplete();
            }
        }



        #region Filtering

        public void ClearFilters() => _multiplayerServicesFacade.ClearFilters();

        public void SetGameModeFilter(GameMode gameMode)
        {
            if (gameMode == GameMode.Invalid)
                _multiplayerServicesFacade.ClearFilters();
            else
                _multiplayerServicesFacade.SetGameModeFilter(gameMode);
        }
        public void SetMapFilter(string mapName)
        {
            if (string.IsNullOrEmpty(mapName))
                _multiplayerServicesFacade.ClearMapFilter();
            else
                _multiplayerServicesFacade.SetMapFilter(mapName);
        }
        public void SetShowPasswordProtectedLobbies(bool newValue) => _multiplayerServicesFacade.SetShowPasswordProtectedLobbies(newValue);

        #endregion

        #region Ordering

        public void SetSortOrder(SortField field, SortOrder order) => throw new System.NotImplementedException("Changing Sort Order Not Implemented");
        public void ResetSortOrder() => _multiplayerServicesFacade.ClearSortOptions();

        #endregion

        private void OnQueryOptionsChanged()
        {
            QueryLobbiesRequest(blockUI: false);
        }

#endregion


        private void HandleSessionJoinResult((bool Success, ISession Session) result)
        {
            if (result.Success)
            {
                OnJoinedSession(result.Session);
            }
            else
            {
                _connectionManager.RequestShutdown();
                UnblockUIAfterLoadingIsComplete();
            }
        }

        private void OnJoinedSession(ISession remoteSession)
        {
            _multiplayerServicesFacade.SetRemoteSession(remoteSession);

            Debug.Log($"Joined session with ID: {_localSession.SessionID}\nName: {remoteSession.Name}");

            _connectionManager.StartClientSession(_localUser.DisplayName);
        }


        private void BlockUIWhileLoadingIsInProgress()
        {
            _canvasGroup.interactable = false;
            _loadingSpinner.SetActive(true);
        }
        private void UnblockUIAfterLoadingIsComplete()
        {
            // This callback can happen after we've already switched to a different scene,
            //  so we should check in case the canvas group is null.
            if (_canvasGroup != null)
            {
                _canvasGroup.interactable = true;
                _loadingSpinner.SetActive(false);
            }
        }
    }
}