using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gameplay.UI.Menus
{
    /// <summary>
    ///     A menu that contains other menus as its children, with <see cref="MenuTabButton"/> instances allowing you to swap between them.
    /// </summary>
    public abstract class ContainerMenu : Menu
    {
        public Menu[] Children;
        public MenuTabButton[] Buttons;
        [SerializeField] private bool _enterChildOnOpen = true;
        private int _previouslySelectedChildIndex;
        protected Menu PreviouslySelectedChild => Children[_previouslySelectedChildIndex];

        protected virtual int DefaultChildIndex => 0;


        /// <summary>
        ///     What should occur when this menu's children are closed without new ones being opened.
        /// </summary>
        [System.Serializable]
        public enum ChildClosedFallback
        {
            // Do nothing.
            None = 0,
            // Closes self when a child is closed and no others take its place.
            CloseSelf = 1,
            // Reopens the default child (Default: Child 0). If the default child was the open child, instead closes self.
            OpenDefaultChild = 2,
        }
        [SerializeField] private ChildClosedFallback _childClosedFallback = ChildClosedFallback.CloseSelf;
        public ChildClosedFallback OnChildClosedFallback => _childClosedFallback;



        private void Awake()
        {
            _previouslySelectedChildIndex = DefaultChildIndex;
        }


        public override void Open(bool selectFirstElement = true)
        {
            Show();
            HideAllChildren();

            MenuManager.OnActiveMenuChanged += UpdateHighlightedButton;


            _previouslySelectedChildIndex = DefaultChildIndex;
            if (_enterChildOnOpen)
                EnterChild(Children[DefaultChildIndex]);
            else if (selectFirstElement)
                EventSystem.current.SetSelectedGameObject(Buttons[DefaultChildIndex].gameObject);
            else
                Debug.Log("Don't select first element");
        }
        public override async UniTask<bool> Close()
        {
            MenuManager.OnActiveMenuChanged -= UpdateHighlightedButton;

            HideAllChildren();
            _previouslySelectedChildIndex = DefaultChildIndex;

            return await base.Close();
        }
        public void ReopenWithDefaultChild(Selectable targetSelectable = null)
        {
            HideAllChildren();
            ShowChild(DefaultChildIndex);
            base.Reopen(targetSelectable);
        }


        private void UpdateHighlightedButton()
        {
            for(int i = 0; i < Children.Length; ++i)
            {
                if (MenuManager.IsActiveMenuHierarchy(Children[i]))
                {
                    Buttons[i]?.OnTabEntered();
                }
                else
                {
                    Buttons[i]?.OnTabExited();
                }
            }
        }


        /// <summary>
        ///     Returns true if the passed child is the default child (Default: Child 0).
        /// </summary>
        public bool IsDefaultChild(Menu childMenu) => GetChildIndex(childMenu) == DefaultChildIndex;


        /// <summary>
        ///     Shows the desired child. <br/>
        ///     Doesn't 'enter' the child through the <see cref="MenuManager"/>. Instead, hides other children and shows the desired child.
        /// </summary>
        public void ShowChild(Menu childMenu) => ShowChild(GetChildIndex(childMenu));
        /// <summary>
        ///     Shows the child with the given index. <br/>
        ///     Doesn't 'enter' the child through the <see cref="MenuManager"/>. Instead, hides other children and shows the desired child.
        /// </summary>
        public virtual void ShowChild(int childIndex)
        {
            if (!CanHideActiveChild())
                return;

            _previouslySelectedChildIndex = childIndex;
            HideAllChildren();
            Children[childIndex].Show();
        }
        /// <summary>
        ///     Enters the desired child via the <see cref="MenuManager"/>.
        /// </summary>
        public void EnterChild(Menu childMenu) => EnterChild(GetChildIndex(childMenu));
        /// <summary>
        ///     Enters the child with the given index via the <see cref="MenuManager"/>.
        /// </summary>
        public virtual void EnterChild(int childIndex)
        {
            _previouslySelectedChildIndex = childIndex;
            MenuManager.OpenChildMenu(Children[childIndex], Buttons[childIndex]?.GetComponent<Button>(), this);
        }


        /// <summary>
        ///     Enter the next child by index, looping to the first child from the last.
        /// </summary>
        public virtual void EnterNextChild() => EnterChild(_previouslySelectedChildIndex < Children.Length - 1 ? _previouslySelectedChildIndex + 1 : 0);
        /// <summary>
        ///     Enter the previous child by index, looping to the last child from the first.
        /// </summary>
        public virtual void EnterPreviousChild() => EnterChild(_previouslySelectedChildIndex > 0 ? _previouslySelectedChildIndex - 1 : Children.Length - 1);


        /// <summary>
        ///     Returns true if this ContainerMenu can hide its child and show another.<br/>
        ///     Does not affect entering children, only showing.
        /// </summary>
        protected virtual bool CanHideActiveChild() => true;
        /// <summary>
        ///     Hides (Not Closes) all this menu's children.
        /// </summary>
        protected void HideAllChildren()
        {
            for (int i = 0; i < Children.Length; ++i)
                Children[i].Hide();
        }


        /// <summary>
        ///     Returns the index for the passed Menu, throwing an exception if it does not exist under this ContainerMenu.
        /// </summary>
        protected int GetChildIndex(Menu childMenu)
        {
            for (int i = 0; i < Children.Length; ++i)
                if (Children[i] == childMenu)
                    return i;

            throw new System.ArgumentException($"Passed Menu \"{childMenu.name}\" is not within \"{this.name}\"'s 'Children'");
        }
    }
}