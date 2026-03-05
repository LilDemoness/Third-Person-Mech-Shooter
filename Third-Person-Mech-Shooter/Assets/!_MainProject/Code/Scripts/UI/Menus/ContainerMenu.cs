using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
        public MenuTabButton[] Buttons;
        [SerializeField] private bool _enterChildOnOpen = true;
        private int _previouslySelectedChildIndex = 0;

        [SerializeField] private bool _childrenCanBeClosed = false;
        public bool ChildrenCanBeClosed => _childrenCanBeClosed;


        public override void Open(bool selectFirstElement = true)
        {
            Show();
            HideAllChildren();


            if (_enterChildOnOpen)
                EnterChild(Children[_previouslySelectedChildIndex]);
            else if (selectFirstElement)
                EventSystem.current.SetSelectedGameObject(Buttons[_previouslySelectedChildIndex].gameObject);
            else
                Debug.Log("Don't select first element");
            //    ShowChild(Children[_previouslySelectedChildIndex]);
        }
        public override void Close(System.Action onCompleteCallback)
        {
            Debug.Log("Close");
            Hide();
            HideAllChildren();
            _previouslySelectedChildIndex = 0;

            onCompleteCallback?.Invoke();
        }


        // Note: Alternatively, call Show() on the child menu in ShowChild(), and then open the child menu in Hide().

        public void ShowChild(Menu childMenu) => ShowChild(GetChildIndex(childMenu));
        // Shows the child with the given index (Doesn't enter it).
        // Note: This was causing issues with the LobbyBrowserUI due to calling immediately before EnterChild().
        public virtual void ShowChild(int childIndex)
        {
            Debug.Log("Show Child");
            _previouslySelectedChildIndex = childIndex;
            HideAllChildren();
            Children[childIndex].Show();
            //MenuManager.OpenChildMenu(Children[childIndex], this, false);
        }
        public void EnterChild(Menu childMenu) => EnterChild(GetChildIndex(childMenu));
        public virtual void EnterChild(int childIndex)
        {
            Debug.Log("Enter Child");
            _previouslySelectedChildIndex = childIndex;
            MenuManager.OpenChildMenu(Children[childIndex], Buttons[childIndex].GetComponent<Button>(), this);
        }


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