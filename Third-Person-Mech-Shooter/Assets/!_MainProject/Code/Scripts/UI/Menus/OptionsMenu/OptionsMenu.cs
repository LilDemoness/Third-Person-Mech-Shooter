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
        [SerializeField] private RebindingRootMenu _keybindingsMenu;
        [SerializeField] private OptionsSubmenu _accessibilityMenu;
        private IOptionsSubmenu _currentOpenMenu;



        private void Awake() => InitialiseSubmenus();


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



        public override void ShowChild(int childIndex)
        {
            _currentOpenMenu = Children[childIndex] as IOptionsSubmenu;
            base.ShowChild(childIndex);
        }
        public override void EnterChild(int childIndex)
        {
            _currentOpenMenu = Children[childIndex] as IOptionsSubmenu;
            base.EnterChild(childIndex);
        }


        protected override bool CanHideActiveChild() => PreviouslySelectedChild == null || !(PreviouslySelectedChild as IOptionsSubmenu).HasChanges;


        #region UI Button Functions

        public void DiscardChanges() => _currentOpenMenu?.LoadAllOptionsFromPrefs();
        public void SaveChanges() => _currentOpenMenu?.SaveAllOptionsToPrefs();
        public void ResetToDefault()
        {
            PopupManager.ShowPopupPanel("Reset to Default", "Are you sure you wish to reset your options to their default values?",
                new PopupButtonParameters("Cancel", null),
                new PopupButtonParameters("Reset", ResetCurrentSubmenuValues)
                );
        }

        #endregion
    }
}