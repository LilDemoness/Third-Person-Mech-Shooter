using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Gameplay.UI.Menus
{
    public static class MenuManager
    {
        // Back Button Handling.
        private static Stack<GameObject> _previousRootButtonSelections;
        private static Stack<GameObject> _backButtonTargets;
        private static GameObject _currentMenuSelection;

        public static GameObject CurrentMenu => _currentMenuSelection;
        public static bool HasBackButtonTarget => _backButtonTargets.TryPeek(out _);


        public static event System.Action OnActiveMenuChanged;


        static MenuManager()
        {
            _previousRootButtonSelections = new Stack<GameObject>();
            _backButtonTargets = new Stack<GameObject>();
        }


        public static void SetActiveMenu(Menu menuToEnable, GameObject sender = null, bool clearStacks = false) => SetActiveMenu(menuToEnable.gameObject, sender, clearStacks);
        public static void SetActivePopup(Menu menuToEnable, GameObject sender = null, bool clearStacks = false) => SetActiveMenu(menuToEnable.gameObject, sender, clearStacks, false);
        public static void SetActiveMenu(GameObject menuToEnable, GameObject sender = null, bool clearStacks = false, bool disablePrevious = true)
        {
            // Disable Current Menu.
            if (disablePrevious && _currentMenuSelection)
                DisableMenu(_currentMenuSelection);
            // Enable Desired Menu.
            EnableMenu(menuToEnable);

            // Update Back Button Progression.
            if (!clearStacks)
            {
                _backButtonTargets.Push(_currentMenuSelection);
                //if (sender != null)
                    _previousRootButtonSelections.Push(sender);
            }
            _currentMenuSelection = menuToEnable.gameObject;

            // Clear stacks if desired.
            if (clearStacks)
            {
                _previousRootButtonSelections.Clear();
                _backButtonTargets.Clear();
            }

            // Notify listeners that we've changed menu.
            OnActiveMenuChanged?.Invoke();
        }
        public static void ReturnToPreviousMenu()
        {
            // Disable Current Menu.
            if (_currentMenuSelection)
                DisableMenu(_currentMenuSelection);
            // Enable Desired Menu.
            GameObject menuToEnable = _backButtonTargets.Pop(); // Retrieve desired menu.
            EnableMenu(menuToEnable);

            // Allow for Back Button Progression to submenus of this menu.
            _currentMenuSelection = menuToEnable;

            // Set the selected object to the selection when we left the menu we've returned to.
            EventSystem.current.SetSelectedGameObject(_previousRootButtonSelections.Pop());

            // Notify listeners that we've changed menu.
            OnActiveMenuChanged?.Invoke();
        }


        // Shows/Enables the desired menu as applicable.
        private static void EnableMenu(GameObject menuToEnable)
        {
            if (menuToEnable.TryGetComponent<Menu>(out Menu subMenu))
                subMenu.Show();
            else
                menuToEnable.SetActive(true);
        }
        // Hides/Disables the desired menu as applicable.
        private static void DisableMenu(GameObject menuToDisable)
        {
            if (menuToDisable.TryGetComponent<Menu>(out Menu subMenu))
                subMenu.Hide();
            else
                menuToDisable.SetActive(false);
        }



        /// <summary>
        ///     Returns true if this menu is the currently active menu.
        /// </summary>
        public static bool IsActiveMenu(this Menu menu) => menu.gameObject == CurrentMenu;

        /// <summary>
        ///     Returns true if the passed component's parent menu is the active menu. Otherwise, false.
        /// </summary>
        public static bool IsInActiveMenu(this Component component)
        {
            // Try Find first menu through parents.
            if (!component.TryGetComponentThroughParents<Menu>(out Menu closestParentMenu))
                return false;   // Not within a menu.

            // Compare the component's parent menu with the active menu.
            return closestParentMenu.gameObject == CurrentMenu;
        }
    }
}