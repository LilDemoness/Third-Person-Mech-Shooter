using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gameplay.UI.Menus
{
    /*[RequireComponent(typeof(Button))]
    public class MenuTabButton : MonoBehaviour
    {
        [SerializeField] private ContainerMenu _parentMenu;
        [SerializeField] private Menu _menu;


        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(OnButtonSelected);
        }
        private void OnDestroy()
        {
            GetComponent<Button>().onClick.RemoveListener(OnButtonSelected);
        }

        public void OnButtonSelected() => _parentMenu.ShowSubmenu(_menu);
    }*/

    [RequireComponent(typeof(Button))]
    public class MenuTabButton : MonoBehaviour, ISelectHandler, IPointerClickHandler
    {
        /* Input:
         * - Select:            Enter Submenu
         * - Navigate Right:    Enter Submenu
         * - Back:              Previous Submenu
         * - Navigate Left:     Previous Submenu
         * - Hover W/ Mouse:    Highlight
         * - Navigate W/ Keyboard/Gamepad: Show Submenu (Don't enter)
         */

        [SerializeField] private ContainerMenu _parentContainerMenu;
        [SerializeField] private Menu _associatedChildMenu;

        [Space(5)]
        [SerializeField] private bool _showOnSelect = true;


        protected void Awake()
        {
            // Subscribe to navigation events.
        }
        protected void OnDestroy()
        {
            // Unsubscribe from navigation events.
        }


        public void OnSelect(BaseEventData _)
        {
            if (_showOnSelect)
                _parentContainerMenu.ShowChild(_associatedChildMenu);
        }
        public void OnPointerClick(PointerEventData _)
        {
            _parentContainerMenu.EnterChild(_associatedChildMenu);
        }
    }
}