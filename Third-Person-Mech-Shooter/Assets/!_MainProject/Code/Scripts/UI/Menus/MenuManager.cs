using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System.Linq;

namespace Gameplay.UI.Menus
{
    public static class MenuManager
    {
        static MenuManager()
        {
            ClearData();
            OnActiveMenuChanged += Test;

            CacheLastSelectedGameobject().Forget();

            UserInput.ClientInput.OnPauseGamePerformed += ReturnToPreviousMenu_Performed;
        }


        private static async UniTaskVoid CacheLastSelectedGameobject()
        {
            s_priorSelectedObject = null;
            GameObject lastSelection = null;
            GameObject currentSelection;
            while(EventSystem.current != null)
            {
                currentSelection = EventSystem.current.currentSelectedGameObject;
                if (lastSelection != null && currentSelection != lastSelection)
                {
                    // Selection is not null & differs from last frame's.
                    // Set our cached prior selection to the last frame's selection.
                    Debug.Log("Update Selection: " + lastSelection.name);
                    s_priorSelectedObject = lastSelection.GetComponent<Selectable>();
                }

                await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);
                lastSelection = currentSelection;
            }

            Debug.LogWarning("Stopping Selected GameObject Detection");
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

            Debug.Log("Active Menus: " + outputString);
        }


        public static System.Action OnActiveMenuChanged;


        private static Selectable s_baseSelectable;     // For returning from the root menu to no menus open.
        private static List<MenuData> s_openMenuData;   // Data on all our open menus.
        private static int s_openMenusCount;            // How many menus are currently open. 0 is none.
        private static bool s_parentMenuIsContainer => s_openMenusCount > 1 && s_openMenuData[s_openMenusCount - 2].Menu is ContainerMenu;

        private static List<MenuData> s_cachedMenuData; // Data on all our open menus from before the current operation.
        private static Selectable s_priorSelectedObject;

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

        public static bool IsActiveMenu(this Menu menu) => s_openMenusCount > 0 ? ActiveMenuData.Menu == menu : false;
        // Note: Defaults to true if no popups are active.
        public static bool IsActivePopup(this Popup popup) => s_blockingPopups.Count > 0 ? s_blockingPopups[s_blockingPopups.Count - 1] == popup : true;

        public static bool IsInOpenMenus(this Menu menu) => s_openMenusCount > 0 ? s_openMenuData.Any(t => t.Menu == menu) : false;



        /// <summary>
        ///     Swaps to the desired menu, closing all menus to reach the menu's parent menu (If it has one, otherwise closes all menus).
        /// </summary>
        /// <param name="menuToSwap"> The menu you wish to open.</param>
        public static void SetActiveMenu(Menu menuToSwap, Selectable triggeringSelectable) => SetActiveMenuUniTask(menuToSwap, triggeringSelectable).Forget();
        /// <inheritdoc cref="SetActiveMenu(Menu, Selectable)"/>
        private static async UniTask<bool> SetActiveMenuUniTask(Menu menuToSwap, Selectable triggeringSelectable) => await SetActiveMenuUniTask(menuToSwap, triggeringSelectable, menuToSwap.transform.parent.GetComponentInParent<Menu>());

        /// <summary>
        ///     Swaps to the desired menu, closing all menus to reach the menu's parent.
        /// </summary>
        /// <param name="menuToSwap"> The menu you wish to open.</param>
        /// <param name="parentMenu"> The parent menu of the menu you wish to open, or null if it doesn't have one.</param>
        public static void SetActiveMenu(Menu menuToSwap, Selectable triggeringSelectable, Menu parentMenu) => SetActiveMenuUniTask(menuToSwap, triggeringSelectable, parentMenu).Forget();
        /// <inheritdoc cref="SetActiveMenu(Menu, Selectable, Menu)"/>
        public static async UniTask<bool> SetActiveMenuUniTask(Menu menuToSwap, Selectable triggeringSelectable, Menu parentMenu)
        {
            Debug.Log("Set Active Menu: " + menuToSwap.name + " Parent: " + (parentMenu == null ? "NULL" : parentMenu.name));
            CacheCurrentData();

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
            bool success;
            for(int i = indexToSwap; i < s_openMenusCount; ++i)
            {
                success = await CloseActiveMenuUniTask(false, false);
                if (!success)
                {
                    Debug.Log("Fail");
                    RevertOperation();
                    return false; // Failed to close a menu, so cancel the exit.
                }
            }

            // Swap the active menu to our desired.
            success = await SwapMenuUniTask(menuToSwap, triggeringSelectable);
            Debug.Log("Swap: " + success);
            if (!success)
            {
                RevertOperation();
                return false;
            }

            DiscardCachedData();
            return true;
        }

        /// <summary>
        ///     Swaps the menu at the current level to <paramref name="menuToSwap"/>.
        /// </summary>
        public static async UniTask<bool> SwapMenuUniTask(Menu menuToSwap, Selectable sourceSelectable)
        {
            bool success = await CloseActiveMenuUniTask(true, false);
            if (!success)
                return false;

            success = await OpenChildMenuUniTask(menuToSwap, sourceSelectable, null);
            return success;
        }



        /// <summary>
        ///     Opens the desired menu.<br/>
        ///     Doesn't close the active menu.
        /// </summary>
        /// <param name="menu"> The menu you wish to open.</param>
        /// <param name="selectFirstElement"></param>
        /// <param name="sourceSelectable"> The selectable that triggered this opening, or null if no selectable triggered it. Used when returning to the previous menu.</param>
        public static void OpenMenu(Menu menu, bool selectFirstElement, Selectable sourceSelectable, bool hideCurrent = true)
        {
            // Cache the selectable for the purposes of returning.
            if (ActiveMenuData == null)
                s_baseSelectable = sourceSelectable;
            else
                ActiveMenuData.OnOpenedChildMenu(sourceSelectable);

            // Hide our current menu, if desired.
            if (hideCurrent)
                ActiveMenuData?.Menu.Hide();    // Close instead?

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
        public static void OpenChildMenu(Menu child, Selectable sourceSelectable, Menu parent) => OpenChildMenuUniTask(child, sourceSelectable, parent).Forget();
        /// <inheritdoc cref="OpenChildMenu(Menu, Selectable, Menu)"/>
        private static async UniTask<bool> OpenChildMenuUniTask(Menu child, Selectable sourceSelectable, Menu parent)
        {
            if (parent == null)
                OpenMenu(child, true, sourceSelectable, hideCurrent: false);
            else
            {
                // The menu has a desired parent.
                // Close to reach the parent without closing the parent.
                bool success = await CloseMenusToReachUniTask(parent, MenuOperation.None);
                if (!success)
                {
                    Debug.LogWarning("Failed to open parent");
                    return false;
                }

                // Open our child under the parent.
                OpenMenu(child, true, sourceSelectable, hideCurrent: false);
            }

            return true;
        }


        /// <summary>
        ///     Re-opens the currently active menu.
        /// </summary>
        private static void ReopenActiveMenu()
        {
            if (ActiveMenuData != null)
            {
                Debug.Log("Selectable Target: " + ActiveMenuData.SelectableTargetForReopen?.name);
                ActiveMenuData.Menu.Reopen(ActiveMenuData.SelectableTargetForReopen);
            }
            else
                EventSystem.current.SetSelectedGameObject(s_baseSelectable?.gameObject);
        }
        private static bool CacheCurrentData()
        {
            if (s_cachedMenuData != null)
                return false;   // We are already caching data.
            if (s_openMenuData == null || s_openMenusCount == 0)
                return true;    // We are the primary cacher, but there is nothing to cache.

            // We have data to cache. Cache it.
            s_cachedMenuData = s_openMenuData;
            Debug.Log("Selected: " + s_priorSelectedObject?.name);
            s_cachedMenuData[s_cachedMenuData.Count - 1].SelectableTargetForReopen = s_priorSelectedObject;
            return true;
        }
        private static void DiscardCachedData() => s_cachedMenuData = null;
        private static void RevertOperation()
        {
            Debug.Log("Revert Operation");
            if (s_cachedMenuData == null)
                return;

            // Hide the current menu.
            if (ActiveMenuData != null)
                ActiveMenuData.Menu.Hide();

            // Revert the menu order, opening where required.
            int cachedMenusCount = s_cachedMenuData.Count;
            Debug.Log(cachedMenusCount + " | " + s_openMenusCount);
            for(int i = s_openMenusCount; i < cachedMenusCount; ++i)
            {
                OpenMenu(s_cachedMenuData[i].Menu, false, i == 0 ? s_baseSelectable : s_cachedMenuData[i - 1].SelectableTargetForReopen, hideCurrent: false);
            }

            // Repen the new current menu.
            ReopenActiveMenu();
            DiscardCachedData();
        }


        enum MenuOperation { None, Close, Reopen }
        /// <summary>
        ///     Closes any menus required to reach the desired menu, then performs the relevant operation for <paramref name="targetMenuOperation"/>.
        /// </summary>
        /// <param name="menu"> The menu you wish to close until.</param>
        /// <param name="targetMenuOperation"> The operation you wish to perform on the target menu.</param>
        private static void CloseMenusToReach(Menu menu, MenuOperation targetMenuOperation) => CloseMenusToReachUniTask(menu, targetMenuOperation).Forget();
        /// <inheritdoc cref="CloseActiveMenuUniTask(bool, bool)"/>
        private static async UniTask<bool> CloseMenusToReachUniTask(Menu menu, MenuOperation targetMenuOperation)
        {
            // Find out how many menus we need to close.
            int menuIndex = GetIndexOfMenu(menu);
            if (menuIndex == -1)
                return false; // The desired menu isn't open in our hierarchy.

            CacheCurrentData();
            bool success;

            // Close menus until we reach our desired.
            while(s_openMenusCount - 1 > menuIndex)
            {
                // Close the menu.
                success = await CloseActiveMenuUniTask(reopenParentMenu: false, preventClosingOfChildlessContainer: true);
                if (!success)
                {
                    RevertOperation();
                    return false;
                }
            }

            // Perform our desired operation on our target menu.
            switch (targetMenuOperation)
            {
                case MenuOperation.Close:
                    // Close the target menu and reopen its parent.
                    success = await CloseActiveMenuUniTask(reopenParentMenu: true, preventClosingOfChildlessContainer: false);
                    if (!success)
                    {
                        RevertOperation();
                        return false;
                    }
                    break;
                case MenuOperation.Reopen:
                    // Reopen the target menu.
                    ReopenActiveMenu();
                    break;
            }


            DiscardCachedData();
            return true;
        }
        /// <summary>
        ///     Closes the lowest level menu.
        /// </summary>
        /// <param name="reopenParentMenu"> Whether the menu above the active menu should be reopened once the active is closed, or left as inactive.</param>
        /// <param name="preventClosingOfChildlessContainer"> Should we prevent closing the parent if it is a ContainerMenu that cannot be open without a child? Default: False.</param>
        private static async UniTask<bool> CloseActiveMenuUniTask(bool reopenParentMenu = true, bool preventClosingOfChildlessContainer = false)
        {
            if (s_openMenusCount == 0)
            {
                Debug.Log("No menu to close");
                return true; // No menus are active for us to close.
            }
            bool isPrimaryCacher = CacheCurrentData();
            Debug.Log("Primary Cacher: " + isPrimaryCacher);

            // Close the menu.
            bool success = await ActiveMenuData.Menu.Close();
            if (!success)
            {
                if (isPrimaryCacher) { RevertOperation(); }
                Debug.Log("Failure");
                return false;
            }

            Debug.Log("Closed: " + ActiveMenuData.Menu.name);
            s_openMenuData.RemoveAt(s_openMenusCount - 1);
            --s_openMenusCount;

            Debug.Log(
                "Don't Ignore: " + (!preventClosingOfChildlessContainer)
                + "\n Has Active Menu: " + (ActiveMenuData != null)
                + "\n Active is Container: " + (ActiveMenuData != null && ActiveMenuData.Menu is ContainerMenu)
                + "\n Active's Children Cannot be Closed: " + (ActiveMenuData != null && ActiveMenuData.Menu is ContainerMenu && !(ActiveMenuData.Menu as ContainerMenu).ChildrenCanBeClosed)
                );
            if (!preventClosingOfChildlessContainer && ActiveMenuData != null && ActiveMenuData.Menu is ContainerMenu && !(ActiveMenuData.Menu as ContainerMenu).ChildrenCanBeClosed)
            {
                // Our parent is a ContainerMenu that cannot be open without its children, so close it too.
                success = await CloseActiveMenuUniTask(reopenParentMenu);
                if (!success)
                {
                    if (isPrimaryCacher) { RevertOperation(); }
                    return false;
                }
            }
            else
            {
                // We don't need to do anything else. Closing completed.

                if (reopenParentMenu)
                    ReopenActiveMenu();
            }

            if (isPrimaryCacher) { DiscardCachedData(); }
            return true;
        }



        #region External Access Functions

        /// <summary>
        ///     Closes the desired menu and reopens its parent (If it has one).
        /// </summary>
        public static void CloseMenu(Menu menu) => CloseMenusToReachUniTask(menu, MenuOperation.Close).Forget();
        public static void ReturnToPreviousMenu() => CloseActiveMenuUniTask(reopenParentMenu: true).Forget();

        /// <summary>
        ///     Closes the current active menu and opens the one above it in s_openMenus (If it exists).
        /// </summary>
        public static void ReturnToPreviousMenu_Performed()
        {
            // Quick and dirty way to force our back target selection to be correct when we are using a non-button to trigger.
            s_priorSelectedObject = EventSystem.current.currentSelectedGameObject ? EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>() : s_priorSelectedObject;
            CloseActiveMenuUniTask(reopenParentMenu: true).Forget();
        }

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

            s_priorSelectedObject = null;

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