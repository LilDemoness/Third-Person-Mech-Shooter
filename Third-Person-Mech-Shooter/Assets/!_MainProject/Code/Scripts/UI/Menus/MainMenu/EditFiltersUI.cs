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
        [SerializeField] private MultiselectDropdown _gameModeDropdown;
        [SerializeField] private MultiselectDropdown _mapNameDropdown;
        [SerializeField] private Toggle _showPasswordProtectedToggle;


        [Inject]
        private MultiplayerServicesFacade _multiplayerServicesFacade;


        private void Awake() => InitialiseUI();
        private void InitialiseUI()
        {
            InitialiseGameModeDropdown();
            InitialiseMapDropdown();
            _showPasswordProtectedToggle.onValueChanged.AddListener(OnShowPasswordProtectedLobbiesChanged);
        }
        private void InitialiseGameModeDropdown()
        {
            _gameModeDropdown.ClearOptions();
            _gameModeDropdown.AddOptions(GameModeExtensions.GetAllGameModeAcronyms());
            _gameModeDropdown.onValueChanged.AddListener(OnGameModeSelected);
        }
        private void InitialiseMapDropdown()
        {
            _mapNameDropdown.ClearOptions();
            _mapNameDropdown.AddOptions(new List<string>() { "TestGameMap" });
            _mapNameDropdown.onValueChanged.AddListener(OnGameModeSelected);
        }

        private void OnDestroy()
        {
            _gameModeDropdown.onValueChanged.RemoveListener(OnGameModeSelected);
            _mapNameDropdown.onValueChanged.RemoveListener(OnGameModeSelected);
            _showPasswordProtectedToggle.onValueChanged.RemoveListener(OnShowPasswordProtectedLobbiesChanged);
        }


        public void ShowEditFiltersUIPressed() => MenuManager.SetActivePopup(this);


        private void OnGameModeSelected(uint selectionIndex)
        {
            Debug.Log("New GameMode Selection: " + selectionIndex);
        }
        private void OnMapSelected(uint selectionIndex)
        {
            Debug.Log("New Map Selection: " + selectionIndex);
        }
        private void OnShowPasswordProtectedLobbiesChanged(bool newValue) => Debug.Log("Show Password Protected Lobbies?: " + newValue);
    }
}