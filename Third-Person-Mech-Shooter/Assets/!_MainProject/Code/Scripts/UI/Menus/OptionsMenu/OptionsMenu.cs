using UnityEngine;

namespace Gameplay.UI.Menus.Options
{
    public class OptionsMenu : Menu
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
            CloseAllMenus();
            SetActiveMenu(_gameplayMenu);

            base.Show();
        }


        private void CloseAllMenus()
        {
            // Hide all menus.
            _gameplayMenu.Hide();
            _videoMenu.Hide();
            _audioMenu.Hide();
            _controlsMenu.Hide();
            _keybindingsMenu.Hide();
            _accessibilityMenu.Hide();

            // Null our current menu as none are now open.
            _currentOpenMenu = null;
        }
        private void SetActiveMenu(OptionsSubmenu optionsSubmenu)
        {
            // If we have an open menu, close it.
            if (_currentOpenMenu)
                _currentOpenMenu.Hide();

            // Open our desired menu and cache it.
            _currentOpenMenu = optionsSubmenu;
            optionsSubmenu.Show();
        }


        private void OnSelectedOptionChanged()
        {

        }


        #region UI Button Functions

        public void OpenGameplayMenu() => SetActiveMenu(_gameplayMenu);
        public void OpenVideoMenu() => SetActiveMenu(_videoMenu);
        public void OpenAudioMenu() => SetActiveMenu(_audioMenu);
        public void OpenControlsMenu() => SetActiveMenu(_controlsMenu);
        public void OpenKeybindingsMenu() => SetActiveMenu(_keybindingsMenu);
        public void OpenAccessibilityMenu() => SetActiveMenu(_accessibilityMenu);

        #endregion
    }
}