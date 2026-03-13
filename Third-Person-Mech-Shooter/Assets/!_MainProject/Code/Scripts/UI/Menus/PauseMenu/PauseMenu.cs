using ApplicationLifecycle.Messages;
using Cysharp.Threading.Tasks;
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
        private const ClientInput.ActionTypes LOCKING_TYPES = ClientInput.ActionTypes.Everything & ~(ClientInput.ActionTypes.UI | ClientInput.ActionTypes.MenuNavigation);

        private bool _isOpen;
        private bool _isPerformingOpenCloseOperation;
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
            _isPerformingOpenCloseOperation = false;

            FinishClose().Forget();
        }
        private void OnDestroy()
        {
            ClientInput.OnPauseGamePerformed -= OnPauseGamePerformed;

            if (_isOpen)
                FinishClose().Forget();
        }

        public override void Open(bool selectFirstElement = true)
        {
            if (_isOpen || _isPerformingOpenCloseOperation)
                return;

            // Properly pause the game after a frame to prevent immediately re-opening cause we receive the input for PauseGame after we were just opened.
            DelayFinishOpenForFrame(selectFirstElement).Forget();
        }
        private async UniTaskVoid DelayFinishOpenForFrame(bool selectFirstElement)
        {
            _isPerformingOpenCloseOperation = true;
            await UniTask.Yield();
            _isPerformingOpenCloseOperation = false;

            FinishPause(selectFirstElement);
        }
        private void FinishPause(bool selectFirstElement)
        {
            base.Open(selectFirstElement);

            // Perform game pausing logic here.
            _isOpen = true;

            _previousLockMode = Cursor.lockState;
            Cursor.lockState = CursorLockMode.None;

            Debug.Log("Prevention Added");
            ClientInput.AddActionPrevention(typeof(PauseMenu), LOCKING_TYPES);
        }


        public override async UniTask<bool> Close()
        {
            if (!_isOpen || _isPerformingOpenCloseOperation)
                return false;   // Failed to close as we are either already closed, in the process of closing, or in the process of opening.

            // Properly resume the game after a frame to prevent immediately re-opening cause we receive the input for PauseGame after we were closed.
            _isPerformingOpenCloseOperation = true;
            await UniTask.Yield();
            _isPerformingOpenCloseOperation = false;

            await FinishClose();
            return true;
        }
        private async UniTask FinishClose()
        {
            await base.Close();

            // Perform game resuming logic here.
            _isOpen = false;

            Cursor.lockState = _previousLockMode;

            Debug.Log("Prevention Removed");
            ClientInput.RemoveActionPrevention(typeof(PauseMenu), LOCKING_TYPES);
        }
        


        public void OnPauseGamePerformed()
        {
            if (!_isOpen)
                PauseGame();
        }


        public void PauseGame() => MenuManager.SetActiveMenu(this, null);
        public void ResumeGame() => MenuManager.CloseMenu(this);


        public void ShowOptionsMenu(UnityEngine.UI.Selectable sender) => EnterChild(_optionsMenu);

        public void OnExitToMainMenuPressed() => _connectionManager.RequestShutdown();
        public void OnExitToDesktopPressed() => _quitApplicationPub.Publish(new QuitApplicationMessage());
    }
}