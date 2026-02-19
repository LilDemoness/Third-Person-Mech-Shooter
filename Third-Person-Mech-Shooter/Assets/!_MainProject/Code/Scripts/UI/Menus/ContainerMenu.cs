using UnityEngine;

namespace Gameplay.UI.Menus
{
    public class ContainerMenu : Menu
    {
        [SerializeField] private Menu[] _subMenus;


        public override void Show()
        {
            base.Show();
            ShowDefaultSubmenu();
        }

        public void ShowSubmenu(int submenuIndex) => ShowSubmenu(_subMenus[submenuIndex]);
        public void ShowSubmenu(Menu submenu)
        {
            HideAllSubmenus();
            MenuManager.SetActiveMenu(submenu.gameObject, null, false, false);
        }
        private void ShowDefaultSubmenu() => ShowSubmenu(_subMenus[0]);
        private void HideAllSubmenus()
        {
            for(int i = 0; i < _subMenus.Length; ++i)
                _subMenus[i].Hide();
        }
    }
}