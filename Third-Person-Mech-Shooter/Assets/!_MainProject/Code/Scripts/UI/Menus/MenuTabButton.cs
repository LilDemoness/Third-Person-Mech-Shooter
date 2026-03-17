using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gameplay.UI.Menus
{
    public class MenuTabButton : MonoBehaviour, ISelectHandler
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


        public void OnSelect(BaseEventData _)
        {
            if (_showOnSelect)
                _parentContainerMenu.ShowChild(_associatedChildMenu);
        }
        public void OnButtonClicked()
        {
            _parentContainerMenu.EnterChild(_associatedChildMenu);
        }
    }
}