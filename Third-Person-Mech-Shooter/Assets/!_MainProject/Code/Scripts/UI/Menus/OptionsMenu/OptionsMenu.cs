using Gameplay.UI.Popups;
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
        [SerializeField] private RebindingRootContainerMenu _keybindingsMenu;
        [SerializeField] private OptionsSubmenu _accessibilityMenu;



        protected override void Awake()
        {
            base.Awake();
            MenuContainer.SetCanHideActiveChildFunc(CanHideActiveChild);
        }
        private void Start()
        {
            InitialiseSubmenus();
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

        private IOptionsSubmenu GetCurrentSubmenu() => (MenuContainer.GetActiveChild() as IOptionsSubmenu);
        private void ResetCurrentSubmenuValues() => GetCurrentSubmenu().ResetAllOptions();



        private bool CanHideActiveChild(Menu activeChild) => activeChild == null || !(activeChild as IOptionsSubmenu).HasChanges;


        #region UI Button Functions

        public void DiscardChanges() => GetCurrentSubmenu().LoadAllOptionsFromPrefs();
        public void SaveChanges() => GetCurrentSubmenu().SaveAllOptionsToPrefs();
        public void ResetToDefault()
        {
            PopupManager.ShowPopup("Reset to Default", "Are you sure you wish to reset your options to their default values?",
                new PopupButtonParameters("Cancel", null),
                new PopupButtonParameters("Reset", ResetCurrentSubmenuValues)
                );
        }

        #endregion
    }
}