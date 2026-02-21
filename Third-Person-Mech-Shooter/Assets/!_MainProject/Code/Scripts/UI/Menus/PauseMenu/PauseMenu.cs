using UnityEngine;
using UserInput;

namespace Gameplay.UI.Menus.Pause
{
    public class PauseMenu : Menu
    {
        private bool _isOpen;


        private void Awake()
        {
            ClientInput.OnPauseGamePerformed += OnPauseGamePerformed;
            _isOpen = false;
            Hide();
        }
        private void OnDestroy()
        {
            ClientInput.OnPauseGamePerformed -= OnPauseGamePerformed;
        }

        public override void Show()
        {
            base.Show();

            if (!_isOpen)
            {
                // Perform game pausing logic here.
                _isOpen = true;
            }
        }
        public override void Hide()
        {
            base.Hide();

            if (_isOpen)
            {
                // Perform game resuming logic here.
                _isOpen = false;
            }
        }


        public void OnPauseGamePerformed()
        {
            if (!_isOpen)
                PauseGame();
            else if (MenuManager.CurrentMenu == this.gameObject)
                ResumeGame();
            else
                MenuManager.ReturnToPreviousMenu();
        }


        public void PauseGame() => MenuManager.SetActiveMenu(this);
        public void ResumeGame()
        {
            if (!MenuManager.TryCloseMenu(this))
                Debug.LogError("Failed to close PauseMenu");
        }
    }
}