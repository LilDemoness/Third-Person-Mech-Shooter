using Gameplay.GameplayObjects.Character;
using Gameplay.GameState;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI.Menus.Pause
{
    [RequireComponent(typeof(Button))]
    public class RespawnButton : MonoBehaviour
    {
        [SerializeField] private PauseMenu _pauseMenu;
        private Button _thisButton;

        private void Awake()
        {
            _thisButton = GetComponent<Button>();

            GameStateBehaviour.OnActiveStateChanged += UpdateActiveState;
            UpdateActiveState();
        }
        private void OnDestroy()
        {
            _thisButton.onClick.RemoveListener(OnRespawnPressed);
            GameStateBehaviour.OnActiveStateChanged -= UpdateActiveState;
        }

        private void UpdateActiveState()
        {
            if (GameStateBehaviour.ActiveGameState == GameState.GameState.InGameplay)
            {
                _thisButton.interactable = true;
                _thisButton.onClick.AddListener(OnRespawnPressed);
            }
            else
            {
                this.enabled = false;
                _thisButton.interactable = false;
            }
        }
        private void OnRespawnPressed()
        {
            ServerCharacter.ForceRespawn();
            _pauseMenu.ResumeGame();
        }
    }
}