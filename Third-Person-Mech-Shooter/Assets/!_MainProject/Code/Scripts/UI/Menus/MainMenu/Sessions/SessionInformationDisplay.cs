using Cysharp.Threading.Tasks;
using Gameplay.GameState;
using TMPro;
using Unity.Netcode;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityServices.Sessions;
using VContainer;

namespace Gameplay.UI.Menus
{
    public class SessionInformationDisplay : MonoBehaviour
    {
        [SerializeField] private Gameplay.GameplayObjects.PersistentPlayerRuntimeCollection _persistentPlayerCollection;


        [SerializeField] private TMP_Text _sessionNameLabel;


        [SerializeField] private TMP_Text _playerNameLabel;
        private const string PLAYER_NAME_PREFIX_TEXT = "You: ";

        [SerializeField] private TMP_Text _joinCodeLabel;
        private const string JOIN_CODE_PREFIX_TEXT = "Join Code: ";
        private const string JOIN_CODE_FALLBACK_TEXT = "N/A";

        [SerializeField] private TMP_Text _gameModeLabel;
        private const string GAME_MODE_PREFIX_TEXT = "Game Mode: ";

        [SerializeField] private TMP_Text _mapNameLabel;
        private const string MAP_NAME_PREFIX_TEXT = "Map: ";


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

            if (_persistentPlayerCollection.TryGetPlayer(NetworkManager.Singleton.LocalClientId, out GameplayObjects.Players.PersistentPlayer persistentPlayer))
            {
                persistentPlayer.NetworkNameState.Name.OnValueChanged += UpdatePlayerName;
                UpdatePlayerName(default, persistentPlayer.NetworkNameState.Name.Value);
            }
            else
                _persistentPlayerCollection.ItemAdded += PersistentPlayerCollection_ItemAdded;

            if (_multiplayerServicesFacade.CurrentUnitySession != null)
                UpdateSessionInfoUI();
            else
                InitialiseDataUniTask().Forget();
        }

        private void PersistentPlayerCollection_ItemAdded(GameplayObjects.Players.PersistentPlayer persistentPlayer)
        {
            if (persistentPlayer.OwnerClientId != NetworkManager.Singleton.LocalClientId)
                return;

            persistentPlayer.NetworkNameState.Name.OnValueChanged += UpdatePlayerName;
            UpdatePlayerName(default, persistentPlayer.NetworkNameState.Name.Value);
            _persistentPlayerCollection.ItemAdded -= PersistentPlayerCollection_ItemAdded;
        }


        private void OnDestroy()
        {
            if (_persistentGameState != null)
                _persistentGameState.OnGameStateDataChanged -= UpdateLocalUI;
            if (_multiplayerServicesFacade != null)
                _multiplayerServicesFacade.OnSessionUpdated -= UpdateSessionInfoUI;

            if (NetworkManager.Singleton != null
                && _persistentPlayerCollection != null
                && _persistentPlayerCollection.TryGetPlayer(NetworkManager.Singleton.LocalClientId, out GameplayObjects.Players.PersistentPlayer persistentPlayer)
                && persistentPlayer.NetworkNameState != null
                && persistentPlayer.NetworkNameState.Name != null)
                persistentPlayer.NetworkNameState.Name.OnValueChanged -= UpdatePlayerName;
        }


        private async UniTaskVoid InitialiseDataUniTask()
        {
            UpdateJoinCode(JOIN_CODE_FALLBACK_TEXT);

            string hostName = await GetHostNameUniTask();
            Debug.Log(hostName);
            UpdateSessionName($"{hostName}'s Session");
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
        private void UpdatePlayerName(Utils.FixedPlayerName oldName, Utils.FixedPlayerName fixedPlayerName) => _playerNameLabel.text = string.Concat(PLAYER_NAME_PREFIX_TEXT, fixedPlayerName);
        private void UpdateJoinCode(string joinCode) => _joinCodeLabel.text = string.Concat(JOIN_CODE_PREFIX_TEXT, joinCode);
        private void UpdateGameMode(GameMode gameMode) => _gameModeLabel.text = string.Concat(GAME_MODE_PREFIX_TEXT, gameMode.ToDisplayName());
        private void UpdateMapName(string mapName) => _mapNameLabel.text = string.Concat(MAP_NAME_PREFIX_TEXT, mapName);


        private async UniTask<string> GetHostNameUniTask()
        {
            float startTime = Time.time;
            const float MAX_WAIT_TIME = 0.5f;
            NetworkClient hostClient = null;
            do
            {
                hostClient = NetworkManager.Singleton.ConnectedClients[NetworkManager.Singleton.CurrentSessionOwner];

                await UniTask.WaitForEndOfFrame();
            } while((hostClient == null || hostClient.PlayerObject == null) && (Time.time - startTime < MAX_WAIT_TIME));


            if (hostClient != null && hostClient.PlayerObject != null)
            {
                return hostClient.PlayerObject.GetComponent<Utils.NetworkNameState>().Name.Value;
            }
            else
            {
                Debug.LogWarning("Warning: No host client found for session");
                return null;
            }
        }
    }
}