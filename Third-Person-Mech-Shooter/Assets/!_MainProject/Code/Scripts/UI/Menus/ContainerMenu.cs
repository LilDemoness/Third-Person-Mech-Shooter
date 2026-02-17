using UnityEngine;

namespace Gameplay.UI.Menus
{
    public class ContainerMenu : Menu
    {
        [SerializeField] private MenuTab[] _menuTabs;


        public override void Show()
        {
            base.Show();
            ShowDefaultTab();
        }

        public void ShowTab(int tabIndex) => ShowTab(_menuTabs[tabIndex]);
        public void ShowTab(MenuTab menuTab)
        {
            HideAllTabs();
            menuTab.Show();
        }
        private void ShowDefaultTab() => ShowTab(_menuTabs[0]);
        private void HideAllTabs()
        {
            for(int i = 0; i < _menuTabs.Length; ++i)
                _menuTabs[i].Hide();
        }
    }
}