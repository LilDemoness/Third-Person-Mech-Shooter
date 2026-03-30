using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI.Menus
{
    /// <summary>
    ///     A object that contains menus as its children, with instances of <see cref="MenuTabButton"/> allowing you to swap between them through input.<br/>
    ///     Can also be a Menu, or can be just a UI element.
    /// </summary>
    public class MenuContainer : MonoBehaviour
    {
        [field: SerializeField] protected MenuButtonPair[] Children { get; private set; }
        [SerializeField] private bool _enterChildOnOpen = true;


        private int _previouslySelectedChildIndex;
        protected int PreviouslySelectedChildIndex => _previouslySelectedChildIndex;
        protected Menu PreviouslySelectedChildMenu => Children[_previouslySelectedChildIndex].Menu;

        protected virtual int DefaultChildIndex => 0;


        #region Funcs

        /// <summary>
        ///     Returns true if this ContainerMenu can hide its child and show another.<br/>
        ///     Does not affect entering children, only showing.
        /// </summary>
        private System.Func<Menu, bool> _canHideActiveChildFunc;

        #endregion



        protected virtual void Awake()
        {
            _previouslySelectedChildIndex = DefaultChildIndex;
        }
        protected virtual void OnDestroy()
        {
            MenuManager.OnActiveMenuChanged -= UpdateHighlightedButton;
        }


        public void SetCanHideActiveChildFunc(System.Func<Menu, bool> func) => _canHideActiveChildFunc = func;
        public Menu GetActiveChild() => PreviouslySelectedChildMenu;


        public virtual void OnOpen()
        {
            HideAllChildren();

            MenuManager.OnActiveMenuChanged += UpdateHighlightedButton;


            _previouslySelectedChildIndex = DefaultChildIndex;
            if (_enterChildOnOpen)
                EnterChild(DefaultChildIndex);
        }
        public virtual void OnClose()
        {
            MenuManager.OnActiveMenuChanged -= UpdateHighlightedButton;

            HideAllChildren();
            _previouslySelectedChildIndex = DefaultChildIndex;
        }
        public virtual void OnReopen() => EnterChild(DefaultChildIndex);


        #region Showing Child Menus

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
            if (_canHideActiveChildFunc != null && !_canHideActiveChildFunc(PreviouslySelectedChildMenu))
                return;

            _previouslySelectedChildIndex = childIndex;
            HideAllChildren();
            Children[childIndex].Menu.Show();
        }

        #endregion

        #region Entering Child Menus

        /// <summary>
        ///     Enters the desired child via the <see cref="MenuManager"/>.
        /// </summary>
        public void EnterChild(Menu childMenu, Selectable selectableOverride = null) => EnterChild(GetChildIndex(childMenu), selectableOverride);
        /// <summary>
        ///     Enters the child with the given index via the <see cref="MenuManager"/>.
        /// </summary>
        public virtual void EnterChild(int childIndex, Selectable selectableOverride = null)
        {
            _previouslySelectedChildIndex = childIndex;
            Debug.Log("Open Child " + childIndex);
            MenuManager.OpenChildMenu(Children[childIndex].Menu, selectableOverride ?? Children[childIndex].Button?.GetComponent<Button>(), this);
        }


        /// <summary>
        ///     Enter the next child by index, looping to the first child from the last.
        /// </summary>
        public virtual void EnterNextChild() => EnterChild(_previouslySelectedChildIndex < Children.Length - 1 ? _previouslySelectedChildIndex + 1 : 0);
        /// <summary>
        ///     Enter the previous child by index, looping to the last child from the first.
        /// </summary>
        public virtual void EnterPreviousChild() => EnterChild(_previouslySelectedChildIndex > 0 ? _previouslySelectedChildIndex - 1 : Children.Length - 1);

        #endregion



        private void UpdateHighlightedButton()
        {
            for (int i = 0; i < Children.Length; ++i)
            {
                if (MenuManager.IsInActiveMenuHierarchy(Children[i].Menu))
                {
                    Children[i].Button?.OnTabEntered();
                }
                else
                {
                    Children[i].Button?.OnTabExited();
                }
            }
        }

        
        /// <summary>
        ///     Hides (Not Closes) all this menu's children.
        /// </summary>
        protected void HideAllChildren()
        {
            for (int i = 0; i < Children.Length; ++i)
                Children[i].Menu.Hide();
        }


        /// <summary>
        ///     Returns true if the passed child is the default child (Default: Child 0).
        /// </summary>
        public bool IsDefaultChild(Menu childMenu) => GetChildIndex(childMenu) == DefaultChildIndex;
        public bool IsOpenChildDefault() => PreviouslySelectedChildIndex == DefaultChildIndex;

        /// <summary>
        ///     Returns the index for the passed Menu, or -1 if it does not exist under this ContainerMenu.
        /// </summary>
        protected int GetChildIndex(Menu childMenu)
        {
            for (int i = 0; i < Children.Length; ++i)
                if (Children[i].Menu == childMenu)
                    return i;

            return -1;
        }
    }


    [System.Serializable]
    public struct MenuButtonPair
    {
        public Menu Menu;
        public MenuTabButton Button;
    }
}