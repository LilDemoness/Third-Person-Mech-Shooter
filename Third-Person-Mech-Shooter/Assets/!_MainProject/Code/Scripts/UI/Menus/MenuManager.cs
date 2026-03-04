using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

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
        }

        private static void Test()
        {
            string outputString = "";
            for (int i = 0; i < s_openMenus.Count; ++i)
                outputString += s_openMenus[i].name + (i == s_openMenus.Count - 1 ? "" : ",");

            Debug.Log(outputString);
        }


        public static System.Action OnActiveMenuChanged;


        private static List<Menu> s_openMenus;
        private static int s_openMenusCount;
        public static Menu SelectedMenu => s_openMenusCount > 0 ? s_openMenus[s_openMenusCount - 1] : null;

        /// <summary>
        ///     Returns true if this Component is within the active Menu/Popup.
        /// </summary>
        public static bool IsInActiveMenu(this Component componentToTest)
        {
            Menu parentMenu = componentToTest.transform.parent.GetComponentInParent<Menu>();

            if (s_blockingPopups.Count == 0)
            {
                // No obstructing popups are open, so directly compare the parent menu to the selected menu.
                if (parentMenu == SelectedMenu)
                    return true;
                else
                {
                    // Iterate upwards, checking ContainerMenus if we find them and stopping once we don't.
                    // This allows buttons in the root menu (Shared buttons) to still operate when children are open.
                    for(int i = s_openMenusCount - 2; i >= 0; --i)
                    {
                        if (s_openMenus[i] is not ContainerMenu)
                            break;

                        // This menu is a container menu. Perform a comparison check.
                        if (s_openMenus[i] == parentMenu)
                            return true;
                    }

                    return false;
                }
            }

            if (parentMenu == null)
                return false;   // An object without a menu parent will never be in focus when there is an obstructing popup open.

            // Check to see if the object is under the blocking popup.
            return parentMenu == s_blockingPopups[s_blockingPopups.Count - 1];
        }

        public static bool IsActiveMenu(this Menu menu) => SelectedMenu == menu;
        // Note: Defaults to true if no popups are active.
        public static bool IsActivePopup(this Popup popup) => s_blockingPopups.Count > 0 ? s_blockingPopups[s_blockingPopups.Count - 1] == popup : true;


        /// <summary>
        ///     Swaps to the desired menu, closing all menus to reach the menu's parent menu (If it has one, otherwise closes all menus).
        /// </summary>
        /// <param name="menuToSwap"> The menu you wish to open.</param>
        public static void SetActiveMenu(Menu menuToSwap) => SetActiveMenu(menuToSwap, menuToSwap.transform.parent.GetComponentInParent<Menu>());
        /// <summary>
        ///     Swaps to the desired menu, closing all menus to reach the menu's parent.
        /// </summary>
        /// <param name="menuToSwap"> The menu you wish to open.</param>
        /// <param name="parentMenu"> The parent menu of the menu you wish to open, or null if it doesn't have one.</param>
        public static void SetActiveMenu(Menu menuToSwap, Menu parentMenu)
        {
            Debug.Log("Set Active Menu: " + menuToSwap.name + " Parent: " + (parentMenu == null ? "NULL" : parentMenu.name));

            // Find the index of the parent menu (Our child is one below this, so 'indexToSwap = parentIndex + 1');
            int indexToSwap;
            if (parentMenu != null)
            {
                indexToSwap = -1;
                for(int i = 0; i < s_openMenusCount; ++i)
                {
                    if (s_openMenus[i] == parentMenu)
                    {
                        // Found the parent. We're swapping the parent's immediate child, so our swapping index is 'i + 1'.
                        indexToSwap = i + 1;
                        break;
                    }
                }

                if (indexToSwap == -1) 
                {
                    // Failed to find parent menu in our open menus.
                    // For now, treat this as swapping the root menu and log to notify us that this occured.
                    indexToSwap = 0;
                    Debug.LogWarning($"Parent Menu '{parentMenu.name}' doesn't exist as a menu to swap to. Treating swap to '{menuToSwap.name}' as originating from the root.");
                }
            }
            else
                indexToSwap = 0;    // We are swapping the root menu, so our indexToSwap is 0.


            // Close menus until we reach our menuToSwap's level.
            for(int i = indexToSwap; i < s_openMenusCount; ++i)
                CloseActiveMenu();

            // Swap the active menu to our desired.
            SwapMenu(menuToSwap);
        }

        /// <summary>
        ///     Swaps the menu at the current level to <paramref name="menuToSwap"/>.
        /// </summary>
        public static void SwapMenu(Menu menuToSwap)
        {
            Debug.Log("Swap Menu: " + s_openMenusCount);

            // Close the currently open menu.
            if (s_openMenusCount > 0)
            {
                s_openMenus[s_openMenusCount - 1].Close(Finish);
            }
            else
                Finish();

            void Finish()
            {
                // Open the desired menu.
                if (s_openMenusCount > 0)
                {
                    s_openMenus[s_openMenusCount - 1] = menuToSwap;
                    menuToSwap.Open();
                }
                else
                    OpenChildMenu(menuToSwap, null, true);

                OnActiveMenuChanged?.Invoke();
            }
        }


        public static void OpenChildMenu(Menu childMenu, Menu parentMenu, bool selectFirstElement)
        {
            Debug.Log("Add Child Menu: " + childMenu.name + ". Parent: " + (parentMenu == null ? "NULL" : parentMenu.name));

            if (parentMenu == null)
                AddChild();
            else
            {
                // Close up till below the parent.
                if (!CloseUntilMenu(parentMenu, false))
                {
                    // The parent is not in our hierarchy.
                    Debug.Log($"Parent '{parentMenu.name}' is not within the menu hierarchy.");
                    return;
                }
                Debug.Log("Closed to Parent Menu");

                // Add the child menu under the parent.
                AddChild();
            }

            void AddChild()
            {
                s_openMenus.Add(childMenu);
                ++s_openMenusCount;
                childMenu.Open(selectFirstElement);
                OnActiveMenuChanged?.Invoke();
            }
        }

        /// <summary>
        ///     Closes all menus up to and including the desired menu, if it is in the hierarchy.
        ///     Returns false if the passed menu is not in our hierarchy.
        /// </summary>
        public static bool CloseMenu(Menu menu, bool openNextMenu)
        {
            int countToClose = 0;
            for(int i = s_openMenusCount - 1; i >= 0; --i)
            {
                ++countToClose;
                if (s_openMenus[i] == menu)
                    break;
            }

            if (countToClose == s_openMenusCount)
                return false;   // The desired menu isn't open in our hierarchy.

            // Close all menus up to and including our desired.
            for(int i = 0; i < countToClose; ++i)
                CloseActiveMenu();
            
            // Open the next menu if we desire to and it exists.
            if (openNextMenu && s_openMenusCount > 0)
                SwapMenu(s_openMenus[s_openMenusCount - 1]);

            return true;
        }
        /// <summary>
        ///     Closes all menus up to but excluding the desired menu, if it is in the hierarchy.
        ///     Returns false if the passed menu is not in our hierarchy.
        /// </summary>
        public static bool CloseUntilMenu(Menu menu, bool openMenu)
        {
            int countToClose = 0;
            for (int i = s_openMenusCount - 1; i >= 0; --i)
            {
                if (s_openMenus[i] == menu)
                    break;

                ++countToClose;
            }

            if (countToClose == s_openMenusCount)
                return false;   // The desired menu isn't open in our hierarchy.

            // Close all menus up to and including our desired.
            for (int i = 0; i < countToClose; ++i)
                CloseActiveMenu();

            // Open the menu if we desire
            if (openMenu)
                SwapMenu(menu);

            return true;
        }
        /// <summary>
        ///     Closes the lowest level menu.<br/>
        ///     Does not open any menus or call 'OnActiveMenuChanged'.
        /// </summary>
        public static void CloseActiveMenu()
        {
            if (s_openMenusCount == 0)
                return; // No menus are active for us to close.

            int menuIndex = s_openMenusCount - 1;
            Debug.Log("Close: " + s_openMenus[menuIndex].name);
            s_openMenus[menuIndex].Close(Finish);

            void Finish()
            {
                // Open the desired menu.
                s_openMenus.RemoveAt(menuIndex);
                --s_openMenusCount;
            }
        }

        /// <summary>
        ///     Closes the current active menu and opens the one above it in s_openMenus (If it exists).
        /// </summary>
        public static void ReturnToPreviousMenu()
        {
            CloseActiveMenu();
            SelectedMenu?.Open();
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
            s_openMenus = new List<Menu>();
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