using System.Collections.Generic;
using Infrastructure;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.UI;
using UnityServices.Sessions;
using VContainer;

namespace Gameplay.UI.Menus.Session
{
    /// <summary>
    ///     Handles the list of <see cref="LobbyListItemUI"/> elements and ensures it stays synchonised with the Session list from the service.
    /// </summary>
    public class SessionJoiningUI : Menu
    {
        [Space(5)]
        [SerializeField] private LobbyListItemUI _sessionListItemPrototype;

        [Space(5)]
        [SerializeField] private Graphic _emptySessionListLabel;


        private IObjectResolver _container;
        //private SessionUIMediator _sessionUIMediator;
        private UpdateRunner _updateRunner;
        private ISubscriber<SessionListFetchedMessage> _localSessionsRefreshedSubscriber;

        private List<LobbyListItemUI> _sessionListItems = new List<LobbyListItemUI>();


        [Inject]
        private void InjectDependenciesAndInitialize(
            IObjectResolver container,
            //SessionUIMediator sessionUIMediator,
            UpdateRunner updateRunner,
            ISubscriber<SessionListFetchedMessage> localSessionsRefreshedSubscriber)
        {
            this._container = container;
            //this._sessionUIMediator = sessionUIMediator;
            this._updateRunner = updateRunner;
            this._localSessionsRefreshedSubscriber = localSessionsRefreshedSubscriber;

            _localSessionsRefreshedSubscriber.Subscribe(UpdateUI);
        }



        private void Awake()
        {
            _sessionListItemPrototype.gameObject.SetActive(false);
        }
        private void OnDisable()
        {
            if (_updateRunner != null)
                _updateRunner.Unsubscribe(PeriodicRefresh);
        }


        



        public void JoinWithCode(string sanitisedString)
        {
            //_sessionUIMediator.JoinSessionWithCodeRequest(sanitisedString);
        }


        /// <summary>
        ///     Perform a soft refresh without needing to lock the UI and such.
        /// </summary>
        /// <param name="_"></param>
        private void PeriodicRefresh(float _)
        {
            //_sessionUIMediator.QuerySessionRequest(blockUI: false);
        }

        // Called from UI Button.
        public void OnRefreshButtonPressed()
        {
            //_sessionUIMediator.QuerySessionRequest(blockUI: true);
        }

        /// <summary>
        ///     Updates the list of <see cref="LobbyListItemUI"/> elements with the given message contents.
        /// </summary>
        private void UpdateUI(SessionListFetchedMessage message)
        {
            // Update all UI Slots, creating & enabling/disabling as necessary.
            EnsureNumberOfActiveUISlots(message.LocalSessions.Count);
            for(int i = 0; i < message.LocalSessions.Count; ++i)
            {
                ISessionInfo localSession = message.LocalSessions[i];
                _sessionListItems[i].SetData(localSession);
            }

            // Toggle the empty sessions label as required.
            _emptySessionListLabel.enabled = message.LocalSessions.Count == 0;
        }

        /// <summary>
        ///     Ensure that there are the required number of <see cref="LobbyListItemUI"/> instances.<br/>
        ///     Creates new instances to reach the required amount and disables those over the count.
        /// </summary>
        private void EnsureNumberOfActiveUISlots(int requiredNumber)
        {
            int delta = requiredNumber - _sessionListItems.Count;

            // Create required new instances.
            for(int i = 0; i < delta; ++i)
            {
                _sessionListItems.Add(CreateSessionListItem());
            }
            
            // Enable/Disable instances.
            for(int i = 0; i < _sessionListItems.Count; ++i)
            {
                _sessionListItems[i].gameObject.SetActive(i < requiredNumber);
            }
        }

        private LobbyListItemUI CreateSessionListItem()
        {
            LobbyListItemUI listItem = Instantiate(_sessionListItemPrototype.gameObject, _sessionListItemPrototype.transform.parent).GetComponent<LobbyListItemUI>();
            listItem.gameObject.SetActive(true);

            _container.Inject(listItem);

            return listItem;
        }


        public void OnQuickJoinPressed()
        {
            //_sessionUIMediator.QuickJoinRequest();
        }


        public override void Show()
        {
            base.Show();
            _updateRunner.Subscribe(PeriodicRefresh, 10.0f);
        }
        public override void Hide()
        {
            base.Hide();
            _updateRunner.Unsubscribe(PeriodicRefresh);
        }
    }
}