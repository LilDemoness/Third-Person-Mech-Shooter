using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gameplay.UI.Menus
{
    /*public static class MenuManager
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
            //if (CurrentMenu == null)
            //    return true;

            // Try Find first menu through parents.
            if (!component.TryGetComponentThroughParents<Menu>(out Menu closestParentMenu))
            //    return false;   // Not within a menu.
                return CurrentMenu == null;   // Not within a menu.

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
    }*/





    public static class MenuManager
    {
        static MenuManager()
        {
            ClearData();
            OnActiveMenuChanged += Test;

            UserInput.ClientInput.OnPauseGamePerformed += ReturnToPreviousMenu_P;
        }

        /*
         * When the menu below is opened:
         * - Reselect the menu
         * - Reselect the SelectableTargetForReopen (Or default selection if is null)
        */
        public class MenuData
        {
            public Menu Menu;
            public Selectable SelectableTargetForReopen;    // Populated when a menu is closed.

            public MenuData(Menu menu)
            {
                this.Menu = menu;
                this.SelectableTargetForReopen = null;
            }
            // Called when the containing menu opens a child menu.
            public void OnOpenedChildMenu(Selectable sourceSelectable) => SelectableTargetForReopen = sourceSelectable;
        }



        private static void Test()
        {
            string outputString = "";
            for (int i = 0; i < s_openMenuData.Count; ++i)
                outputString += s_openMenuData[i].Menu.name + (i == s_openMenuData.Count - 1 ? "" : ",");

            Debug.Log(outputString);
        }


        public static System.Action OnActiveMenuChanged;


        private static Selectable s_baseSelectable;     // For returning from the root menu to no menus open.
        private static List<MenuData> s_openMenuData;   // Data on all our open menus.
        private static int s_openMenusCount;            // How many menus are currently open. 0 is none.
        private static bool s_parentMenuIsContainer => s_openMenusCount > 1 && s_openMenuData[s_openMenusCount - 2].Menu is ContainerMenu;

        public static MenuData ActiveMenuData => s_openMenusCount > 0 ? s_openMenuData[s_openMenusCount - 1] : null;


        /// <summary>
        ///     Returns true if this Component is within the active Menu/Popup.
        /// </summary>
        public static bool IsInActiveMenu(this Component componentToTest)
        {
            Menu parentMenu = componentToTest.transform.parent.GetComponentInParent<Menu>();

            if (s_blockingPopups.Count == 0)
            {
                // No obstructing popups are open, so directly compare the parent menu to the selected menu.
                if (parentMenu == ActiveMenuData?.Menu)
                    return true;
                else if (ActiveMenuData != null)
                {
                    // Iterate upwards, checking ContainerMenus if we find them and stopping once we don't.
                    // This allows buttons in the root menu (Shared buttons) to still operate when children are open.
                    for(int i = s_openMenusCount - 2; i >= 0; --i)
                    {
                        if (s_openMenuData[i].Menu is not ContainerMenu)
                            break;

                        // This menu is a container menu. Perform a comparison check.
                        if (s_openMenuData[i].Menu == parentMenu)
                            return true;
                    }
                }

                return false;
            }

            if (parentMenu == null)
                return false;   // An object without a menu parent will never be in focus when there is an obstructing popup open.

            // Check to see if the object is under the blocking popup.
            return parentMenu == s_blockingPopups[s_blockingPopups.Count - 1];
        }

        public static bool IsActiveMenu(this Menu menu) => ActiveMenuData.Menu == menu;
        // Note: Defaults to true if no popups are active.
        public static bool IsActivePopup(this Popup popup) => s_blockingPopups.Count > 0 ? s_blockingPopups[s_blockingPopups.Count - 1] == popup : true;


        /// <summary>
        ///     Swaps to the desired menu, closing all menus to reach the menu's parent menu (If it has one, otherwise closes all menus).
        /// </summary>
        /// <param name="menuToSwap"> The menu you wish to open.</param>
        public static void SetActiveMenu(Menu menuToSwap, Selectable triggeringSelectable) => SetActiveMenu(menuToSwap, triggeringSelectable, menuToSwap.transform.parent.GetComponentInParent<Menu>());
        /// <summary>
        ///     Swaps to the desired menu, closing all menus to reach the menu's parent.
        /// </summary>
        /// <param name="menuToSwap"> The menu you wish to open.</param>
        /// <param name="parentMenu"> The parent menu of the menu you wish to open, or null if it doesn't have one.</param>
        public static void SetActiveMenu(Menu menuToSwap, Selectable triggeringSelectable, Menu parentMenu)
        {
            Debug.Log("Set Active Menu: " + menuToSwap.name + " Parent: " + (parentMenu == null ? "NULL" : parentMenu.name));

            // Find the index of the parent menu (Our child is one below this, so 'indexToSwap = parentIndex + 1');
            int indexToSwap = parentMenu != null ? GetIndexOfMenu(parentMenu) : 0;
            
            if (indexToSwap == -1) 
            {
                // Failed to find parent menu in our open menus.
                // For now, treat this as swapping the root menu and log to notify us when this occurs.
                indexToSwap = 0;
                Debug.LogWarning($"Parent Menu '{parentMenu.name}' doesn't exist as a menu to swap to. Treating swap to '{menuToSwap.name}' as originating from the root.");
            }


            // Close menus until we reach our menuToSwap's level.
            // To-do: Account for menus that have logic that needs to be performed when closing (Such as OptionsMenu requesting changes).
            for(int i = indexToSwap; i < s_openMenusCount; ++i)
                CloseActiveMenu(null, false, false);

            // Swap the active menu to our desired.
            SwapMenu(menuToSwap, triggeringSelectable);
        }

        /// <summary>
        ///     Swaps the menu at the current level to <paramref name="menuToSwap"/>.
        /// </summary>
        public static void SwapMenu(Menu menuToSwap, Selectable sourceSelectable)
        {
            CloseActiveMenu(FinishSwap, true, false);
            void FinishSwap() => OpenChildMenu(menuToSwap, sourceSelectable, null);
        }



        /// <summary>
        ///     Opens the desired menu.
        /// </summary>
        /// <param name="menu"> The menu you wish to open.</param>
        /// <param name="selectFirstElement"></param>
        /// <param name="sourceSelectable"> The selectable that triggered this opening, or null if no selectable triggered it. Used when returning to the previous menu.</param>
        public static void OpenMenu(Menu menu, bool selectFirstElement, Selectable sourceSelectable)
        {
            // Cache the selectable for the purposes of returning.
            if (ActiveMenuData == null)
                s_baseSelectable = sourceSelectable;
            else
                ActiveMenuData.OnOpenedChildMenu(sourceSelectable);

            // Add the new menu.
            s_openMenuData.Add(new MenuData(menu));
            ++s_openMenusCount;

            // Open the new menu.
            menu.Open(selectFirstElement);
            OnActiveMenuChanged?.Invoke();
        }
        /// <summary>
        ///     Opens a child menu under the desired parent, closing any menus to reach it as required.
        /// </summary>
        /// <param name="child"> The menu you wish to open.</param>
        /// <param name="sourceSelectable"> The selectable that triggered this opening, or null if no selectable triggered it. Used when returning to the previous menu.</param>
        /// <param name="parent"> The existing menu that the new menu should be opened under</param>
        /// <returns> True if the operation was successful. Otherwise, false.</returns>
        public static bool OpenChildMenu(Menu child, Selectable sourceSelectable, Menu parent)
        {
            if (parent == null)
                OpenMenu(child, true, sourceSelectable);
            else
            {
                // The menu has a desired parent.
                // Close to reach the parent without closing the parent.
                bool success = CloseMenusToReach(parent, MenuOperation.None);
                if (!success)
                    return false;   // Failed to find the desired parent in our open menus.

                // Open our child under the parent.
                OpenMenu(child, true, sourceSelectable);
            }

            return true;
        }


        /// <summary>
        ///     Re-opens the currently active menu.
        /// </summary>
        private static void ReopenActiveMenu()
        {
            if (ActiveMenuData != null)
                ActiveMenuData.Menu.Reopen(ActiveMenuData.SelectableTargetForReopen);
            else
                EventSystem.current.SetSelectedGameObject(s_baseSelectable?.gameObject);
        }


        enum MenuOperation { None, Close, Reopen }
        /// <summary>
        ///     Closes any menus required to reach the desired menu, then performs the relevant operation for <paramref name="targetMenuOperation"/>.
        /// </summary>
        /// <param name="menu"> The menu you wish to close until.</param>
        /// <param name="targetMenuOperation"> The operation you wish to perform on the target menu.</param>
        /// <returns> True if the operation was successful. Otherwise, false.</returns>
        private static bool CloseMenusToReach(Menu menu, MenuOperation targetMenuOperation)
        {
            // Find out how many menus we need to close.
            int menuIndex = GetIndexOfMenu(menu);
            if (menuIndex == -1)
                return false;   // The desired menu isn't open in our hierarchy.


            // Close menus until we reach our desired.
            while(s_openMenusCount - 1 > menuIndex)
            {
                // Close the menu.
                // To-do: Pause if the menu needs to run any multi-frame logic (Such as receiving confirmation from the player).
                CloseActiveMenu(null, awaitClose: false, reopenParentMenu: false, forceIgnoreContainerChildRequirements: true);
            }

            // Perform our desired operation on our target menu.
            switch (targetMenuOperation)
            {
                case MenuOperation.Close:
                    // Close the target menu and reopen its parent.
                    // To-do: Pause if the menu needs to run any multi-frame logic (Such as receiving confirmation from the player).
                    CloseActiveMenu(null, awaitClose: false, reopenParentMenu: true);
                    break;
                case MenuOperation.Reopen:
                    // Reopen the target menu.
                    ReopenActiveMenu();
                    break;
            }


            return true;
        }
        /// <summary>
        ///     Closes the lowest level menu.
        /// </summary>
        /// <param name="onCompleteCallback"></param>
        /// <param name="awaitClose"> Whether the menu should wait wait until the menu finalises closing before actually removing it.</param>
        /// <param name="reopenParentMenu"> Whether the menu above the active menu should be reopened once the active is closed, or left as inactive.</param>
        private static void CloseActiveMenu(System.Action onCompleteCallback, bool awaitClose = false, bool reopenParentMenu = true, bool forceIgnoreContainerChildRequirements = false)
        {
            if (s_openMenusCount == 0)
            {
                Debug.Log("No menu to close");
                onCompleteCallback?.Invoke();
                return; // No menus are active for us to close.
            }

            // Close the menu.
            if (awaitClose)
                ActiveMenuData.Menu.Close(Finish);
            else
            {
                ActiveMenuData.Menu.Close(null);
                Finish();
            }

            void Finish()
            {
                Debug.Log("Closed: " + ActiveMenuData.Menu.name);
                s_openMenuData.RemoveAt(s_openMenusCount - 1);
                --s_openMenusCount;

                if (!forceIgnoreContainerChildRequirements && ActiveMenuData != null && ActiveMenuData.Menu is ContainerMenu && !(ActiveMenuData.Menu as ContainerMenu).ChildrenCanBeClosed)
                {
                    // Our parent is a ContainerMenu that cannot be open without its children, so close it too.
                    CloseActiveMenu(onCompleteCallback, awaitClose, reopenParentMenu);
                }
                else
                {
                    // We don't need to do anything else. Closing completed.

                    if (reopenParentMenu)
                        ReopenActiveMenu();
                    onCompleteCallback?.Invoke();
                }
            }
        }



        #region External Access Functions

        /// <summary>
        ///     Closes the desired menu and reopens its parent (If it has one).
        /// </summary>
        public static void CloseMenu(Menu menu) => CloseMenusToReach(menu, MenuOperation.Close);

        /// <summary>
        ///     Closes the current active menu and opens the one above it in s_openMenus (If it exists).
        /// </summary>
        public static void ReturnToPreviousMenu() { }// => CloseActiveMenu(null, true, true);
        public static void ReturnToPreviousMenu_P() => CloseActiveMenu(null, true, true);

        #endregion



        /// <summary>
        ///     Returns the index for the desired menu, or -1 if it does not appear in 's_openMenus'.
        /// </summary>
        private static int GetIndexOfMenu(Menu menu)
        {
            for(int i = 0; i < s_openMenusCount; ++i)
                if (s_openMenuData[i].Menu == menu)
                    return i;

            return -1;
        }




        /// <summary>
        ///     Iterates up the parents of both objects to determine if they share a parent.
        /// </summary>
        /// <remarks> O(n^2) Complexity based on the number of parents each transform has?</remarks>
        private static bool SharesAParent(this Transform thisTransform, Transform other)
        {
            // Iterate up 'other's parents, returning true if any match the testing transform.
            Transform testParent = other;
            while(testParent != null)
            {
                if (testParent == thisTransform)
                    return true;

                testParent = testParent.parent;
            }

            return thisTransform.parent != null && thisTransform.parent.SharesAParent(other);
        }

    

        // Handle popups per menu? (Allows popups to appear behind menus?)
        public static List<Popup> s_blockingPopups; // List so that we can remove popups even if they aren't the open one.
        public static void CreatePopup(Popup popup)
        {
            s_blockingPopups.Add(popup);
            popup.OnClose += ClosePopup;
        }
        private static void ClosePopup(Popup popup)
        {
            s_blockingPopups.Remove(popup);
            popup.OnClose -= ClosePopup;
        }


        /// <summary>
        ///     Reinitialises stored data to prevent hanging references.
        /// </summary>
        public static void ClearData()
        {
            s_openMenuData = new List<MenuData>();
            s_openMenusCount = 0;

            if (s_blockingPopups != null)
            {
                for (int i = 0; i < s_blockingPopups.Count; ++i)
                    s_blockingPopups[i].Close();
                s_blockingPopups.Clear();
            }
            else
                s_blockingPopups = new List<Popup>();
        }
    }
    public class Popup : MonoBehaviour
    {
        public System.Action<Popup> OnClose;

        private void OnDestroy() => Close();

        public virtual void Open() => this.gameObject.SetActive(true);
        public virtual void Close()
        {
            this.gameObject.SetActive(false);
            OnClose?.Invoke(this);
        }
    }
}