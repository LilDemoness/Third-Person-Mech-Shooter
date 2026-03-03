using UnityEngine;

namespace Gameplay.UI.Menus
{
    /*public class ContainerMenu : Menu
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
    }*/
    public abstract class ContainerMenu : Menu
    {
        public Menu[] Children;


        public override void Show()
        {
            base.Show();
            HideAllChildren();
            //ShowChild(Children[0]);
            EnterChild(Children[0]);
        }
        public override void Close(System.Action onCompleteCallback)
        {
            Hide();
            HideAllChildren();

            onCompleteCallback?.Invoke();
        }


        // Note: Alternatively, call Show() on the child menu in ShowChild(), and then open the child menu in Hide().

        public void ShowChild(Menu childMenu) => ShowChild(GetChildIndex(childMenu));
        // Shows the child with the given index (Doesn't enter it).
        // Note: This was causing issues with the LobbyBrowserUI due to calling immediately before EnterChild().
        public virtual void ShowChild(int childIndex) { }// => MenuManager.OpenChildMenu(Children[childIndex], this, false);
        public void EnterChild(Menu childMenu) => EnterChild(GetChildIndex(childMenu));
        public virtual void EnterChild(int childIndex) => MenuManager.OpenChildMenu(Children[childIndex], this, true);


        private void HideAllChildren()
        {
            for (int i = 0; i < Children.Length; ++i)
                Children[i].Hide();
        }


        protected int GetChildIndex(Menu childMenu)
        {
            for (int i = 0; i < Children.Length; ++i)
                if (Children[i] == childMenu)
                    return i;

            throw new System.ArgumentException($"Passed Menu \"{childMenu.name}\" is not within \"{this.name}\"'s 'Children'");
        }
    }
}