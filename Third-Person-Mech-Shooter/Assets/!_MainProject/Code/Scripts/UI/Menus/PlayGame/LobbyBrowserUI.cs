using UnityEngine;
using Gameplay.UI.Menus.Session;
using VContainer;
using Infrastructure;
using UnityServices.Sessions;
using System.Collections.Generic;
using Unity.Services.Multiplayer;
using UnityEngine.UI;

namespace Gameplay.UI.Menus
{
    /* Class Responsibilities:
    - Show the available lobbies.
    - Refreshing.
    - Facilitate filtering lobbies.
    - Quick Join.
    - Join with Code.
     */
    public class LobbyBrowserUI : MonoBehaviour
    {
        [Header("Lobby List")]
        [SerializeField] private LobbyListItemUI _lobbyListItemPrototype;
        private List<LobbyListItemUI> _lobbyListItems = new List<LobbyListItemUI>();

        [SerializeField] private Graphic _noLobbiesLabel;


        [Header("Submenu References")]
        [SerializeField] private Menu _customiseFiltersMenu;
        [SerializeField] private JoinLobbyWithCodeUI _joinLobbyWithCodeMenu;


        // VContainer Dependency Injection Elements.
        private IObjectResolver _container;             // Used to inject created components (E.g. Lobby List Items).
        private LobbyUIMediator _lobbyUIMediator;       // Facilitates calls to multiplayer systems.
        private UpdateRunner _updateRunner;             // Used for performing periodic refreshes of the list of lobbies.
        private ISubscriber<SessionListFetchedMessage> _localSessionsRefreshedSubscriber;   // Transports lobby data from refresh requests.

        [Inject]
        private void InjectDependenciesAndInitialize(
            IObjectResolver container,
            LobbyUIMediator lobbyUIMediator,
            UpdateRunner updateRunner,
            ISubscriber<SessionListFetchedMessage> localSessionsRefreshedSubscriber)
        {
            this._container = container;
            this._lobbyUIMediator = lobbyUIMediator;
            this._updateRunner = updateRunner;
            this._localSessionsRefreshedSubscriber = localSessionsRefreshedSubscriber;

            Debug.Log("Subscribe");
            _localSessionsRefreshedSubscriber.Subscribe(UpdateUI);
        }


        private void Awake()
        {
            // Hide the prototype list item.
            _lobbyListItemPrototype.gameObject.SetActive(false);
        }
        private void OnDisable()
        {
            if (_updateRunner != null)
                _updateRunner.Unsubscribe(PeriodicRefresh);
        }




        /// <summary>
        ///     Performs a soft refresh (Doesn't lock UI elements).
        /// </summary>
        private void PeriodicRefresh(float _) => _lobbyUIMediator.QueryLobbiesRequest(blockUI: false);
        /// <summary>
        ///     Performs a hard refresh (Locks UI elements).
        /// </summary>
        // Called from UI Button.
        public void OnRefreshButtonPressed() => _lobbyUIMediator.QueryLobbiesRequest(blockUI: true);



        /// <summary>
        ///     Updates the list of <see cref="LobbyListItemUI"/> elements with the given message contents.
        /// </summary>
        private void UpdateUI(SessionListFetchedMessage message)
        {
            // Update all UI Slots, creating & enabling/disabling as necessary.
            EnsureNumberOfActiveUISlots(message.LocalSessions.Count);
            for (int i = 0; i < message.LocalSessions.Count; ++i)
            {
                ISessionInfo localSession = message.LocalSessions[i];
                _lobbyListItems[i].SetData(localSession);
            }

            // Toggle the empty sessions label as required.
            _noLobbiesLabel.enabled = message.LocalSessions.Count == 0;
        }

        /// <summary>
        ///     Ensure that there are the required number of <see cref="LobbyListItemUI"/> instances.<br/>
        ///     Creates new instances to reach the required amount and disables those over the count.
        /// </summary>
        private void EnsureNumberOfActiveUISlots(int requiredNumber)
        {
            int delta = requiredNumber - _lobbyListItems.Count;

            // Create required new instances.
            for (int i = 0; i < delta; ++i)
            {
                _lobbyListItems.Add(CreateSessionListItem());
            }

            // Enable/Disable instances.
            for (int i = 0; i < _lobbyListItems.Count; ++i)
            {
                _lobbyListItems[i].gameObject.SetActive(i < requiredNumber);
            }
        }
        private LobbyListItemUI CreateSessionListItem()
        {
            LobbyListItemUI listItem = Instantiate(_lobbyListItemPrototype.gameObject, _lobbyListItemPrototype.transform.parent).GetComponent<LobbyListItemUI>();
            listItem.gameObject.SetActive(true);

            _container.Inject(listItem);

            return listItem;
        }


        public void OnQuickJoinPressed() => _lobbyUIMediator.QuickJoinRequest();
    }
}