using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Gameplay.UI.Menus
{
    public static class MenuManager
    {
        // Back Button Handling.
        private static Stack<GameObject> _previousRootButtonSelections;
        private static Stack<GameObject> _previousMenus;
        private static GameObject _currentMenuSelection;

        public static GameObject CurrentMenu => _currentMenuSelection;
        public static bool HasBackButtonTarget => _previousMenus.TryPeek(out _);


        public static event System.Action OnActiveMenuChanged;


        static MenuManager()
        {
            _previousRootButtonSelections = new Stack<GameObject>();
            _previousMenus = new Stack<GameObject>();
        }


        public static void SetActiveMenu(Menu menuToEnable, GameObject sender = null, bool clearStacks = false, bool disablePrevious = true) => SetActiveMenu(menuToEnable.gameObject, sender, clearStacks, disablePrevious);
        public static void SetActivePopup(Menu menuToEnable) => SetActiveMenu(menuToEnable.gameObject, null, false, false);
        public static void SetActivePopup(Menu menuToEnable, GameObject sender = null, bool clearStacks = false) => SetActiveMenu(menuToEnable.gameObject, sender, clearStacks, false);
        public static void SetActiveMenu(GameObject menuToEnable, GameObject sender = null, bool clearStacks = false, bool disablePrevious = true)
        {
            // Disable Previous Menu.
            if (disablePrevious && _currentMenuSelection)
                DisableMenu(_currentMenuSelection);

            // Update Back Button Progression.
            if (!clearStacks)
            {
                if (_currentMenuSelection != null)
                {
                    _previousMenus.Push(_currentMenuSelection);
                    //if (sender != null)
                        _previousRootButtonSelections.Push(sender);
                }
            }
            _currentMenuSelection = menuToEnable;

            // Clear stacks if desired.
            if (clearStacks)
            {
                _previousRootButtonSelections.Clear();
                _previousMenus.Clear();
            }

            // Enable Desired Menu.
            EnableMenu(menuToEnable);

            // Notify listeners that we've changed menu.
            OnActiveMenuChanged?.Invoke();
        }
        public static void ReturnToPreviousMenu()
        {
            // Disable Current Menu.
            if (_currentMenuSelection)
                DisableMenu(_currentMenuSelection);
            // Enable Desired Menu.
            GameObject menuToEnable = _previousMenus.Pop(); // Retrieve desired menu.
            EnableMenu(menuToEnable);

            // Allow for Back Button Progression to submenus of this menu.
            _currentMenuSelection = menuToEnable;

            // Set the selected object to the selection when we left the menu we've returned to.
            EventSystem.current.SetSelectedGameObject(_previousRootButtonSelections.Pop());

            // Notify listeners that we've changed menu.
            OnActiveMenuChanged?.Invoke();
        }


        public static bool TryReturnToMenu(Menu menu) => TryReturnToMenu(menu.gameObject);
        public static bool TryReturnToMenu(GameObject menu)
        {
            if (CurrentMenu == menu)
                return true;

            // Check if our menu was previously open.
            if (!_previousMenus.Contains(menu))
                return false;

            // Our desired menu is one of our previous, so close all menus until we reach it, then show it.
            for(int i = 0; i < _previousMenus.Count; ++i)
            {
                GameObject menuToClose = _previousMenus.Pop();
                GameObject previousButtonSelection = _previousRootButtonSelections.Pop();

                if (menuToClose == menu)
                {
                    // Open the desired menu.
                    _currentMenuSelection = menu;
                    EnableMenu(menu);

                    // Set the selected object to the selection when we left the menu we've returned to.
                    EventSystem.current.SetSelectedGameObject(previousButtonSelection);

                    // Notify listeners that we've changed menu.
                    OnActiveMenuChanged?.Invoke();
                    return true;
                }
                else
                    DisableMenu(menuToClose);
            }

            throw new System.Exception($".Contains() check returned true, but menu '{menu.name}' was not found in stack.");
        }

        public static bool TryCloseMenu(Menu menu) => TryCloseMenu(menu.gameObject);
        public static bool TryCloseMenu(GameObject menu)
        {
            // Close menus until we reach our desired one to close, which we also close.
            if (_currentMenuSelection == menu)
            {
                DisableMenu(_currentMenuSelection);
            }
            else
            {
                // Check if our menu was previously open.
                if (!_previousMenus.Contains(menu))
                    return false;

                // Disable the current menu.
                DisableMenu(_currentMenuSelection);

                for (int i = 0; i < _previousMenus.Count; ++i)
                {
                    // Remove this menu from the stacks.
                    GameObject menuToClose = _previousMenus.Pop();
                    _previousRootButtonSelections.Pop();

                    // Disable this menu.
                    DisableMenu(menuToClose);

                    if (menuToClose == menu)
                        break;  // We've reached the menu we wanted to close, so stop closing.
                }
            }


            // Open the menu previous to the one we closed (If one exists).
            if (_previousMenus.TryPop(out GameObject menuToOpen))
            {
                Debug.Log("Menu To Open: " + menuToOpen);
                _currentMenuSelection = menuToOpen;
                EnableMenu(menuToOpen);

                // Set the selected object to the selection when we left the menu we've returned to.
                if (_previousRootButtonSelections.TryPop(out GameObject desiredButtonSelection))
                    EventSystem.current.SetSelectedGameObject(desiredButtonSelection);
            }
            else
                _currentMenuSelection = null;

            // Notify listeners that we've changed menu.
            OnActiveMenuChanged?.Invoke();
            return true;
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
        ///     Returns true if the passed component's parent menu is the active menu, or if there is no active menu. Otherwise, false.
        /// </summary>
        public static bool IsInActiveMenu(this Component component)
        {
            if (CurrentMenu == null)
                return true;

            // Try Find first menu through parents.
            if (!component.TryGetComponentThroughParents<Menu>(out Menu closestParentMenu))
                return false;   // Not within a menu.

            // Compare the component's parent menu with the active menu.
            return closestParentMenu.gameObject == CurrentMenu;
        }


        public static void ClearData()
        {
            Debug.Log("Clear Data");

            _currentMenuSelection = null;
            _previousRootButtonSelections.Clear();
            _previousMenus.Clear();
        }
    }
}