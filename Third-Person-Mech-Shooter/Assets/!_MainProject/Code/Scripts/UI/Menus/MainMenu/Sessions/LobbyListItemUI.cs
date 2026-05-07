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
                _lobbyGameModeText.SetText(GetGameModeString(data));
                _lobbyMapText.SetText(GetMapNameString(data));
            }

            _passwordProtectedCheckGO.SetActive(data.HasPassword);
        }

        private string GetGameModeString(ISessionInfo data)
        {
            const string ERROR_VALUE = "Error";
            if (!data.Properties.TryGetValue("GameMode", out SessionProperty gameModeProperty))
                return ERROR_VALUE;
            if (!System.Enum.TryParse<GameMode>(gameModeProperty.Value, out GameMode gameMode))
                return ERROR_VALUE;

            return gameMode.ToDisplayName();
        }
        private string GetMapNameString(ISessionInfo data)
        {
            const string ERROR_VALUE = "Error";

            if (!data.Properties.TryGetValue("Map", out SessionProperty mapProperty))
                return ERROR_VALUE;

            // Generate a display string for the map name, treating Uppercase Characters followed by Lowercase Characters as the start of a new word.
            // E.g.
            //  ASimpleTest -> A Simple Test
            //  OrAnFPSCheck -> Or An FPS Check
            //  TestingA -> Testing A
            string displayString = mapProperty.Value[0].ToString();
            for(int i = 1; i < mapProperty.Value.Length - 1; ++i)
            {
                if (char.IsUpper(mapProperty.Value[i]) && !char.IsUpper(mapProperty.Value[i + 1]))
                {
                    // This character symbolises the start of a new word as it is uppercase while the next is lowercase.
                    // Add a space before inserting this character.
                    displayString += ' ';
                }
                
                displayString += mapProperty.Value[i];
            }

            // Add the final character. If that character is uppercase but the previous wasn't, then treat it as the start of a new word.
            if (mapProperty.Value.Length > 1 && char.IsUpper(mapProperty.Value[mapProperty.Value.Length - 1]) && !char.IsUpper(mapProperty.Value[mapProperty.Value.Length - 2]))
                displayString += ' ';
            displayString += mapProperty.Value[mapProperty.Value.Length - 1];


            // Return our map name display string.
            return displayString;
        }


        public void OnClick()
        {
            _lobbyUIMediator.JoinSessionRequest(_data);
        }
    }
}