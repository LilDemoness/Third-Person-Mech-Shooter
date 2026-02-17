using Gameplay.GameState;
using TMPro;
using UnityEngine;
using VContainer;

namespace Gameplay.UI
{
    public class GameDataDisplay : MonoBehaviour
    {
        [SerializeField] private TMP_Text _gameModeText;
        [SerializeField] private TMP_Text _mapNameText;


        private PersistentGameState _persistentGameState;

        [Inject]
        private void InjectDependenciesAndInitialise(
            PersistentGameState persistentGameState)
        {
            this._persistentGameState = persistentGameState;
            _persistentGameState.SubscribeToChangeAndCall(UpdateUI);
        }
        private void OnDestroy()
        {
            if (_persistentGameState != null)
                _persistentGameState.SubscribeToChangeAndCall(UpdateUI);
        }


        private void UpdateUI()
        {
            _gameModeText.text = "Game Mode: " + _persistentGameState.GameMode.ToString();
            _mapNameText.text = "Map: " + _persistentGameState.MapName;
        }
    }
}