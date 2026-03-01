using ApplicationLifecycle.Messages;
using Infrastructure;
using Netcode.ConnectionManagement;
using UnityEngine;
using UserInput;
using VContainer;

namespace Gameplay.UI.Menus.Pause
{
    public class PauseMenu : Menu
    {
        private const ClientInput.ActionTypes LOCKING_TYPES = ClientInput.ActionTypes.Everything & ~ClientInput.ActionTypes.UI;

        private bool _isOpen;
        private CursorLockMode _previousLockMode;


        [SerializeField] private Menu _optionsMenu;


        [Inject]
        ConnectionManager _connectionManager;
        [Inject]
        IPublisher<QuitApplicationMessage> _quitApplicationPub;


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

                _previousLockMode = Cursor.lockState;
                Cursor.lockState = CursorLockMode.None;

                ClientInput.AddActionPrevention(typeof(PauseMenu), LOCKING_TYPES);
            }
        }
        public override void Hide()
        {
            base.Hide();

            if (_isOpen)
            {
                // Perform game resuming logic here.
                _isOpen = false;

                Cursor.lockState = _previousLockMode;

                ClientInput.RemoveActionPrevention(typeof(PauseMenu), LOCKING_TYPES);
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

        public void ShowOptionsMenu() => MenuManager.SetActiveMenu(_optionsMenu, disablePrevious: false);

        public void OnExitToMainMenuPressed() => _connectionManager.RequestShutdown();
        public void OnExitToDesktopPressed() => _quitApplicationPub.Publish(new QuitApplicationMessage());
    }
}