using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI.Menus
{
    /// <summary>
    ///     A Menu that can have other Menus as its Children through interfacing with a MenuContainer component.
    /// </summary>
    [RequireComponent(typeof(MenuContainer))]
    public class ContainerMenu : Menu
    {
        private MenuContainer _menuContainer;
        public MenuContainer MenuContainer => _menuContainer;


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


        protected override void Awake()
        {
            _menuContainer = GetComponent<MenuContainer>();
            base.Awake();
        }


        public override void Open(bool selectFirstElement = true)
        {
            base.Open(selectFirstElement);
            MenuContainer.OnOpen();
        }
        public override async UniTask<bool> Close()
        {
            bool success = await base.Close();
            if (!success)
                return false;

            MenuContainer.OnClose();
            return true;
        }
        public override void Reopen(Selectable targetSelectable = null)
        {
            base.Reopen(targetSelectable);
            MenuContainer.OnReopen();
        }
    }
}