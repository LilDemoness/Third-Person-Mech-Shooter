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
        [SerializeField] private TextMeshProUGUI _sessionNameText;
        [SerializeField] private TextMeshProUGUI _sessionPlayerCountText;
        [SerializeField] private GameObject _passwordProtectedCheckGO;

        [Inject]
        private LobbyUIMediator _lobbyUIMediator;

        private ISessionInfo _data;


        public void SetData(ISessionInfo data)
        {
            _data = data;
            _sessionNameText.SetText(data.Name);
            _sessionPlayerCountText.SetText($"{data.MaxPlayers - data.AvailableSlots}/{data.MaxPlayers}");
            _passwordProtectedCheckGO.SetActive(data.HasPassword);
        }

        public void OnClick()
        {
            _lobbyUIMediator.JoinSessionRequest(_data);
        }
    }
}