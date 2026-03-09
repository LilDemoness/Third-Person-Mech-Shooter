using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Gameplay.UI.Menus.Options
{
    public class OptionsMenu : ContainerMenu
    {
        [Header("Submenus")]
        [SerializeField] private OptionsSubmenu _gameplayMenu;
        [SerializeField] private OptionsSubmenu _videoMenu;
        [SerializeField] private OptionsSubmenu _audioMenu;
        [SerializeField] private OptionsSubmenu _controlsMenu;
        [SerializeField] private OptionsSubmenu _keybindingsMenu;
        [SerializeField] private OptionsSubmenu _accessibilityMenu;
        private OptionsSubmenu _currentOpenMenu;


        [Header("Highlighted Option Information")]
        [SerializeField] private GameObject _temp;



        private void Awake()
        {
            InitialiseSubmenus();

            // Subscribe to selection events.
        }
        private void OnDestroy()
        {
            // Unsubscribe from selection events.
        }


        private void InitialiseSubmenus()
        {
            _gameplayMenu.Init();
            _videoMenu.Init();
            _audioMenu.Init();
            _controlsMenu.Init();
            _keybindingsMenu.Init();
            _accessibilityMenu.Init();
        }
        private void SaveSettingsToPrefs()
        {
            _gameplayMenu.SaveAllOptionsToPrefs();
            _videoMenu.SaveAllOptionsToPrefs();
            _audioMenu.SaveAllOptionsToPrefs();
            _controlsMenu.SaveAllOptionsToPrefs();
            _keybindingsMenu.SaveAllOptionsToPrefs();
            _accessibilityMenu.SaveAllOptionsToPrefs();
        }
        private void LoadSettingsFromPrefs()
        {
            _gameplayMenu.LoadAllOptionsFromPrefs();
            _videoMenu.LoadAllOptionsFromPrefs();
            _audioMenu.LoadAllOptionsFromPrefs();
            _controlsMenu.LoadAllOptionsFromPrefs();
            _keybindingsMenu.LoadAllOptionsFromPrefs();
            _accessibilityMenu.LoadAllOptionsFromPrefs();
        }
        private void ResetCurrentSubmenuValues() => _currentOpenMenu?.ResetAllOptions();


        public override void Show()
        {
            

            base.Show();
        }
        public override async UniTask<bool> Close()
        {
            CloseAllMenus();
            bool success = await SetActiveMenu(_gameplayMenu);
            if (!success)
            {
                Debug.Log("Failed to set to Gameplay Menu");
                return false;
            }

            return await base.Close();
        }


        private void CloseAllMenus()
        {
            // Hide all menus.
            _gameplayMenu.ForceClose();
            _videoMenu.ForceClose();
            _audioMenu.ForceClose();
            _controlsMenu.ForceClose();
            _keybindingsMenu.ForceClose();
            _accessibilityMenu.ForceClose();

            // Null our current menu as none are now open.
            _currentOpenMenu = null;
        }
        private async UniTask<bool> SetActiveMenu(OptionsSubmenu optionsSubmenu)
        {
            // If we have an open menu, close it.
            if (_currentOpenMenu != null)
            {
                bool success = await _currentOpenMenu.Close();
                if (!success)
                    return false;
            }

            // Open our desired menu and cache it.
            _currentOpenMenu = optionsSubmenu;
            optionsSubmenu.Open();

            return true;
        }


        private void OnSelectedOptionChanged()
        {

        }


        protected override bool CanHideActiveChild() => PreviouslySelectedChild == null || !(PreviouslySelectedChild as OptionsSubmenu).HasChanges;


        #region UI Button Functions

        #region Open Menu Button Functions

        public async UniTaskVoid OpenGameplayMenu()         => await SetActiveMenu(_gameplayMenu);
        public async UniTaskVoid OpenVideoMenu()            => await SetActiveMenu(_videoMenu);
        public async UniTaskVoid OpenAudioMenu()            => await SetActiveMenu(_audioMenu);
        public async UniTaskVoid OpenControlsMenu()         => await SetActiveMenu(_controlsMenu);
        public async UniTaskVoid OpenKeybindingsMenu()      => await SetActiveMenu(_keybindingsMenu);
        public async UniTaskVoid OpenAccessibilityMenu()    => await SetActiveMenu(_accessibilityMenu);

        #endregion


        public void DiscardChanges() => _currentOpenMenu?.LoadAllOptionsFromPrefs();
        public void SaveChanges() => _currentOpenMenu?.SaveAllOptionsToPrefs();
        public void ResetToDefault() => _currentOpenMenu?.ResetAllOptions();

        #endregion
    }
}