using UnityEngine;
using Gameplay.UI.Menus.Session;
using VContainer;
using Infrastructure;
using UnityServices.Sessions;
using System.Collections.Generic;
using Unity.Services.Multiplayer;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Gameplay.UI.Menus
{
    /* Class Responsibilities:
    - Show the available lobbies.
    - Refreshing.
    - Facilitate filtering lobbies.
    - Quick Join.
    - Join with Code.
     */
    public class LobbyBrowserUI : Menu
    {
        [Header("Lobby List")]
        [SerializeField] private LobbyListItemUI _lobbyListItemPrototype;
        private List<LobbyListItemUI> _lobbyListItems;

        [Space(5)]
        [SerializeField] private LobbyListHeaderUI[] _lobbyListHeaders;
        private int _selectedHeader;

        [Space(5)]
        [SerializeField] private Graphic _noLobbiesLabel;



        private bool m_lobbyOrderInverted;
        private bool _lobbyOrderInverted
        {
            get => m_lobbyOrderInverted;
            set
            {
                m_lobbyOrderInverted = value;
                UpdateSortOrderUI();
            }
        }


        [Header("Submenu References")]
        [SerializeField] private EditFiltersUI _customiseFiltersMenu;

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

            _lobbyListItems ??= new List<LobbyListItemUI>();

            //_updateRunner.Subscribe(PeriodicRefresh, 20.0f);
            _localSessionsRefreshedSubscriber.Subscribe(UpdateUI);
        }


        private void Awake()
        {
            // Initialise & Subscribe to LobbyListHeaderUI elements for sorting lobbies.
            for(int i = 0; i < _lobbyListHeaders.Length; ++i)
                _lobbyListHeaders[i].SetHeaderIndex(i);
            LobbyListHeaderUI.OnAnyHeaderSelected += SetSelectedLobbyHeader;
            SetSelectedLobbyHeader(-1);

            // Hide the prototype list item.
            _lobbyListItemPrototype.gameObject.SetActive(false);
        }
        private void OnDisable()
        {
            LobbyListHeaderUI.OnAnyHeaderSelected -= SetSelectedLobbyHeader;

            if (_updateRunner != null)
                _updateRunner.Unsubscribe(PeriodicRefresh);
            if (_localSessionsRefreshedSubscriber != null)
                _localSessionsRefreshedSubscriber.Unsubscribe(UpdateUI);
        }


        public override void Show()
        {
            base.Show();
            OnRefreshButtonPressed();
        }


        /// <summary>
        ///     Performs a soft refresh (Doesn't lock UI elements).
        /// </summary>
        private void PeriodicRefresh(float _) => _lobbyUIMediator.QueryLobbiesRequest(blockUI: false).Forget();
        /// <summary>
        ///     Performs a hard refresh (Locks UI elements).
        /// </summary>
        // Called from UI Button.
        public void OnRefreshButtonPressed() => _lobbyUIMediator.QueryLobbiesRequest(blockUI: true).Forget();



        /// <summary>
        ///     Updates the list of <see cref="LobbyListItemUI"/> elements with the given message contents.
        /// </summary>
        private void UpdateUI(SessionListFetchedMessage message)
        {
            // Update all UI Slots, creating & enabling/disabling as necessary.
            EnsureNumberOfActiveUISlots(message.LocalSessions.Count);
            for (int i = 0; i < message.LocalSessions.Count; ++i)
            {
                _lobbyListItems[i].SetData(message.LocalSessions[i]);
            }

            if (message.LocalSessions.Count == 0)
            {
                // No sessions.
                _noLobbiesLabel.enabled = message.LocalSessions.Count == 0;
                EventSystem.current.SetSelectedGameObject(null);
            }
            else
                EventSystem.current.SetSelectedGameObject(_lobbyListItems[0].gameObject);
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



        public void EditFiltersInputPerformed()
        {
            if (EventSystem.current.currentSelectedGameObject != null && EventSystem.current.currentSelectedGameObject.transform.IsChildOf(_customiseFiltersMenu.transform))
                ExitEditFiltersUI();    // We are within the edit filters UI. Exit it.
            else
                EnterEditFiltersUI();   // We are not in the edit filters UI. Enter it.
        }
        private void EnterEditFiltersUI() => EventSystem.current.SetSelectedGameObject(_customiseFiltersMenu.FirstSelectedElement.gameObject);
        private void ExitEditFiltersUI() => EventSystem.current.SetSelectedGameObject(_lobbyListItems.Count > 0 && _lobbyListItems[0].gameObject.activeInHierarchy ? _lobbyListItems[0].gameObject : null);

        public void OpenJoinCodePopup() => MenuManager.OpenChildMenu(_joinLobbyWithCodeMenu, null, this);


        public void SetSelectedLobbyHeader(int headerIndex)
        {
            if (_selectedHeader == headerIndex)
                IncrementSortOrder();
            else
            {
                _selectedHeader = headerIndex;
                SetSortOrder(GetSortField());
            }
        }
        public void IncrementSelectedLobbyHeader() => SetSelectedLobbyHeader(MathUtils.Loop(_selectedHeader + 1, -1, _lobbyListHeaders.Length));
        public void DecrementSelectedLobbyHeader() => SetSelectedLobbyHeader(MathUtils.Loop(_selectedHeader - 1, -1, _lobbyListHeaders.Length));


        public void SetSortOrder(SessionSortField sortField)
        {
            _lobbyOrderInverted = false;   // Calls UpdateSortOrderUI, so we don't need to do that here too.
            _lobbyUIMediator.SetSortOrder(sortField.ToSortField(), _lobbyOrderInverted);
        }
        public void InvertSortOrder()
        {
            _lobbyOrderInverted = !_lobbyOrderInverted;
            _lobbyUIMediator.InvertSortOrder(_lobbyOrderInverted);
        }
        /// <summary>
        ///     Moves the Sort Order from Default > Inverted > Deselect Selected Element.
        /// </summary>
        private void IncrementSortOrder()
        {
            if (_lobbyOrderInverted)
                SetSelectedLobbyHeader(-1); // Already inverted, so instead deselect.
            else
            {
                // The sort order isn't inverted, so we can invert it.
                _lobbyOrderInverted = true;
                _lobbyUIMediator.InvertSortOrder(_lobbyOrderInverted);
            }
        }


        private SessionSortField GetSortField() => _selectedHeader == -1 ? SessionSortField.Default : _lobbyListHeaders[_selectedHeader].SortField;
        private void UpdateSortOrderUI()
        {
            SessionSortField sortField = GetSortField();
            SessionSortOrder sortOrder = _lobbyOrderInverted ? SessionSortOrder.Descending : SessionSortOrder.Ascending;

            for (int i = 0; i < _lobbyListHeaders.Length; ++i)
                _lobbyListHeaders[i].OnSortFiltersChanged(sortField, sortOrder);
        }
    }
}