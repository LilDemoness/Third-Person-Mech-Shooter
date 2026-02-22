using Gameplay.GameState;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityServices.Sessions;
using VContainer;

namespace Gameplay.UI.Menus
{
    public class SessionInformationDisplay : MonoBehaviour
    {
        [SerializeField] private TMP_Text _sessionNameLabel;

        [SerializeField] private TMP_Text _joinCodeLabel;
        private const string JOIN_CODE_FORMATTING_TEXT = "Join Code: ";
        private const string JOIN_CODE_FALLBACK_TEXT = "N/A";

        [SerializeField] private TMP_Text _gameModeLabel;
        private const string GAME_MODE_FORMATTING_TEXT = "Game Mode: ";

        [SerializeField] private TMP_Text _mapNameLabel;
        private const string MAP_NAME_FORMATTING_TEXT = "Map: ";


        private PersistentGameState _persistentGameState;
        private MultiplayerServicesFacade _multiplayerServicesFacade;

        [Inject]
        private void InjectDependenciesAndInitialise(
            PersistentGameState persistentGameState,
            MultiplayerServicesFacade multiplayerServicesFacade)
        {
            this._persistentGameState = persistentGameState;
            this._multiplayerServicesFacade = multiplayerServicesFacade;


            _persistentGameState.SubscribeToChangeAndCall(UpdateLocalUI);
            _multiplayerServicesFacade.OnSessionUpdated += UpdateSessionInfoUI;

            if (_multiplayerServicesFacade.CurrentUnitySession != null)
                UpdateSessionInfoUI();
            else
                InitialiseData();
        }
        private void OnDestroy()
        {
            if (_persistentGameState != null)
                _persistentGameState.OnGameStateDataChanged -= UpdateLocalUI;
            if (_multiplayerServicesFacade != null)
                _multiplayerServicesFacade.OnSessionUpdated -= UpdateSessionInfoUI;
        }


        private void InitialiseData()
        {
            UpdateSessionName(GetHostName());
            UpdateJoinCode(JOIN_CODE_FALLBACK_TEXT);
        }


        private void UpdateLocalUI()
        {
            UpdateGameMode(_persistentGameState.GameMode);
            UpdateMapName(_persistentGameState.MapName);
        }
        private void UpdateSessionInfoUI()
        {
            if (_multiplayerServicesFacade == null || _multiplayerServicesFacade.CurrentUnitySession == null)
                return;

            UpdateSessionName(_multiplayerServicesFacade.CurrentUnitySession.Name);
            UpdateJoinCode(_multiplayerServicesFacade.CurrentUnitySession.Code);
        }

        private void UpdateSessionName(string sessionName) => _sessionNameLabel.text = sessionName;
        private void UpdateJoinCode(string joinCode) => _joinCodeLabel.text = string.Concat(JOIN_CODE_FORMATTING_TEXT, joinCode);
        private void UpdateGameMode(GameMode gameMode) => _gameModeLabel.text = string.Concat(GAME_MODE_FORMATTING_TEXT, gameMode.ToDisplayName());
        private void UpdateMapName(string mapName) => _mapNameLabel.text = string.Concat(MAP_NAME_FORMATTING_TEXT, mapName);


        private string GetHostName()
        {
            var hostClient = NetworkManager.Singleton.ConnectedClients[NetworkManager.Singleton.CurrentSessionOwner];

            if (hostClient != null)
                return hostClient.PlayerObject.GetComponent<Utils.NetworkNameState>().Name.Value;
            else
            {
                Debug.LogWarning("Warning: No host client found for session");
                return null;
            }
        }
    }
}