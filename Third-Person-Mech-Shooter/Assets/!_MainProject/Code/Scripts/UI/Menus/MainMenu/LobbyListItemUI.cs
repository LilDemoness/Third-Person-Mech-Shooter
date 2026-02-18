using TMPro;
using Unity.Services.Multiplayer;
using UnityEngine;
using VContainer;

namespace Gameplay.UI.Menus.Session
{
    /// <summary>
    ///     An individual Lobby UI element in the list of available Lobbies.
    /// </summary>
    public class LobbyListItemUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _lobbyNameText;
        [SerializeField] private TextMeshProUGUI _lobbyPlayerCountText;
        [SerializeField] private TextMeshProUGUI _lobbyGameModeText;
        [SerializeField] private TextMeshProUGUI _lobbyMapText;
        [SerializeField] private GameObject _passwordProtectedCheckGO;

        [Inject]
        private LobbyUIMediator _lobbyUIMediator;

        private ISessionInfo _data;


        public void SetData(ISessionInfo data)
        {
            _data = data;

            _lobbyNameText.SetText(data.Name);
            _lobbyPlayerCountText.SetText($"{data.MaxPlayers - data.AvailableSlots}/{data.MaxPlayers}");

            if (data.Properties != null)
            {
                if (data.Properties.TryGetValue("GameMode", out SessionProperty gameModeProperty))
                    _lobbyGameModeText.SetText(gameModeProperty.Value);
                else
                    _lobbyGameModeText.SetText("Error");

                if (data.Properties.TryGetValue("Map", out SessionProperty mapProperty))
                    _lobbyMapText.SetText(mapProperty.Value);
                else
                    _lobbyMapText.SetText("Error");
            }

            _passwordProtectedCheckGO.SetActive(data.HasPassword);
        }

        public void OnClick()
        {
            _lobbyUIMediator.JoinSessionRequest(_data);
        }
    }
}