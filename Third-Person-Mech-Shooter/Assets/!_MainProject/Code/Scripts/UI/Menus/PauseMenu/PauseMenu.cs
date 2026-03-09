using ApplicationLifecycle.Messages;
using Infrastructure;
using Netcode.ConnectionManagement;
using UnityEngine;
using UnityEngine.UI;
using UserInput;
using VContainer;

namespace Gameplay.UI.Menus.Pause
{
    public class PauseMenu : ContainerMenu
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

            if (_isOpen)
                FinishResume();
        }

        public override void Show()
        {
            base.Show();

            if (!_isOpen && _resumeAfterFrameCoroutine == null)
            {
                // Perform game pausing logic here.
                _isOpen = true;

                _previousLockMode = Cursor.lockState;
                Cursor.lockState = CursorLockMode.None;

                Debug.Log("Prevention Added");
                ClientInput.AddActionPrevention(typeof(PauseMenu), LOCKING_TYPES);
            }
        }
        public override void Hide()
        {
            base.Hide();

            if (_isOpen && _resumeAfterFrameCoroutine == null)
            {
                // Properly resume the game after a frame to prevent immediately re-opening cause we receive the input for PauseGame after we were closed.
                _resumeAfterFrameCoroutine = StartCoroutine(ResumeAfterFrame());
            }
        }


        private Coroutine _resumeAfterFrameCoroutine;
        private System.Collections.IEnumerator ResumeAfterFrame()
        {
            yield return null;
            FinishResume();
        }
        private void FinishResume()
        {
            // Perform game resuming logic here.
            _isOpen = false;

            Cursor.lockState = _previousLockMode;

            Debug.Log("Prevention Removed");
            ClientInput.RemoveActionPrevention(typeof(PauseMenu), LOCKING_TYPES);

            // Reset the resume delay coroutine to prevent it getting stack with a invalid but non-null value.
            _resumeAfterFrameCoroutine = null;
        }


        public void OnPauseGamePerformed()
        {
            if (!_isOpen)
                PauseGame();
        }


        public void PauseGame() => MenuManager.SetActiveMenu(this, null);
        public void ResumeGame()
        {
            MenuManager.CloseMenu(this);
        }

        //public void ShowOptionsMenu(UnityEngine.UI.Selectable sender) => MenuManager.OpenMenu(_optionsMenu, true, sender, hideCurrent: true);
        public void ShowOptionsMenu(UnityEngine.UI.Selectable sender) => EnterChild(_optionsMenu);

        public void OnExitToMainMenuPressed() => _connectionManager.RequestShutdown();
        public void OnExitToDesktopPressed() => _quitApplicationPub.Publish(new QuitApplicationMessage());
    }
}