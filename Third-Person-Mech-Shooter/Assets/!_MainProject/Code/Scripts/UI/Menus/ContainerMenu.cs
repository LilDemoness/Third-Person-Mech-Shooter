using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gameplay.UI.Menus
{
    public abstract class ContainerMenu : Menu
    {
        public Menu[] Children;
        public MenuTabButton[] Buttons;
        [SerializeField] private bool _enterChildOnOpen = true;
        private int _previouslySelectedChildIndex = 0;
        protected Menu PreviouslySelectedChild => Children[_previouslySelectedChildIndex];


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
        public override async UniTask<bool> Close()
        {
            HideAllChildren();
            _previouslySelectedChildIndex = 0;

            return await base.Close();
        }


        // Note: Alternatively, call Show() on the child menu in ShowChild(), and then open the child menu in Hide().

        public void ShowChild(Menu childMenu) => ShowChild(GetChildIndex(childMenu));
        // Shows the child with the given index (Doesn't enter it).
        // Note: This was causing issues with the LobbyBrowserUI due to calling immediately before EnterChild().
        public virtual void ShowChild(int childIndex)
        {
            if (!CanHideActiveChild())
                return;

            Debug.Log($"'{this.name}' Showing Child '{childIndex}'", this);
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
            MenuManager.OpenChildMenu(Children[childIndex], Buttons[childIndex]?.GetComponent<Button>(), this);
        }


        /// <summary>
        ///     Returns true if this ContainerMenu can hide its child and show another.<br/>
        ///     Does not affect entering children, only showing.
        /// </summary>
        protected virtual bool CanHideActiveChild() => true;
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