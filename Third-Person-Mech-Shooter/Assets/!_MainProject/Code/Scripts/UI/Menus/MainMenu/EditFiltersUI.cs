using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using VContainer;
using UnityServices.Sessions;

namespace Gameplay.UI.Menus
{
    public class EditFiltersUI : Menu
    {
        [SerializeField] private TMP_Dropdown _gameModeDropdown;
        private GameMode _currentGameModeFilter;

        [SerializeField] private TMP_Dropdown _mapNameDropdown;
        private int _mapNameIndex;   // Temp.

        [SerializeField] private Toggle _showPasswordProtectedToggle;
        private bool _showPasswordProtectedLobbies;


        [Inject]
        private LobbyUIMediator _lobbyUIMediator;       // Facilitates calls to multiplayer systems.


        private void Awake()
        {
            InitialiseUI();
            InitialiseValues();
        }
        private void OnDestroy()
        {
            _gameModeDropdown.onValueChanged.RemoveListener(OnGameModeSelected);
            _mapNameDropdown.onValueChanged.RemoveListener(OnMapSelected);
            _showPasswordProtectedToggle.onValueChanged.RemoveListener(OnShowPasswordProtectedLobbiesChanged);
        }


        #region UI Initialisation

        private void InitialiseUI()
        {
            InitialiseGameModeDropdown();
            InitialiseMapDropdown();
            _showPasswordProtectedToggle.onValueChanged.AddListener(OnShowPasswordProtectedLobbiesChanged);
        }
        private void InitialiseGameModeDropdown()
        {
            _gameModeDropdown.ClearOptions();

            List<string> gameModeAcronyms = GameModeExtensions.GetAllGameModeAcronyms();
            gameModeAcronyms.Insert(0, "Any");
            _gameModeDropdown.AddOptions(gameModeAcronyms);

            _gameModeDropdown.onValueChanged.AddListener(OnGameModeSelected);
        }
        private void InitialiseMapDropdown()
        {
            _mapNameDropdown.ClearOptions();
            _mapNameDropdown.AddOptions(new List<string>() { "Any", "TestGameMap" });
            _mapNameDropdown.onValueChanged.AddListener(OnMapSelected);
        }

        #endregion

        private void InitialiseValues()
        {
            _currentGameModeFilter = GameMode.Invalid;
            _gameModeDropdown.SetValueWithoutNotify(0);

            _mapNameIndex = -1;
            _mapNameDropdown.SetValueWithoutNotify(0);

            _showPasswordProtectedLobbies = true;
            _showPasswordProtectedToggle.SetIsOnWithoutNotify(_showPasswordProtectedLobbies);
        }



        public void ShowEditFiltersUIPressed() => MenuManager.SetActivePopup(this);
        public void HideEditFiltersUIPressed() => MenuManager.ReturnToPreviousMenu();
        public void ApplyFiltersPressed()
        {
            ApplyFilters();
            HideEditFiltersUIPressed();
        }
        public void ResetFiltersPressed()
        {
            _lobbyUIMediator.ClearFilters();
            InitialiseValues();
            HideEditFiltersUIPressed();
        }

        private void ApplyFilters()
        {
            // GameMode.
            _lobbyUIMediator.SetGameModeFilter(_currentGameModeFilter);

            // Map.
            _lobbyUIMediator.SetMapFilter(_mapNameIndex == -1 ? null : _mapNameDropdown.options[_mapNameIndex + 1].text);

            // Show Password.
            Debug.Log("Show Password Protected Lobbies: " + _showPasswordProtectedLobbies);
        }


        private void OnGameModeSelected(int selectionIndex) => _currentGameModeFilter = (GameMode)(selectionIndex - 1);    // As we're adding an addition index, subtract 1 from the index for our conversion GameMode.
        private void OnMapSelected(int selectionIndex) => _mapNameIndex = selectionIndex - 1;    // As we're adding an addition index, subtract 1 to allow easy conversion to GameMode.
        private void OnShowPasswordProtectedLobbiesChanged(bool newValue) => _showPasswordProtectedLobbies = newValue;
    }
}