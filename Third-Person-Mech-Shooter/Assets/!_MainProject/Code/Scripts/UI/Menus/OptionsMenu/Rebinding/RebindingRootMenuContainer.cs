using UnityEngine;
using UserInput;

namespace Gameplay.UI.Menus.Options
{
    public class RebindingRootMenuContainer : MenuContainer
    {
        public bool HasChanges => PreviouslySelectedChildMenu != null && (PreviouslySelectedChildMenu as OptionsSubmenu).HasChanges;
        protected override int DefaultChildIndex => GetChildIndexForCurrentInputDevice();

        private int GetChildIndexForCurrentInputDevice()
        {
            // Find the child corresponding to the current active device type.
            for (int i = 0; i < Children.Length; ++i)
            {
                if ((Children[i].Menu as RebindingSubmenu).Device == ClientInput.LastUsedDevice)
                    return i;
            }

            // No child matches the active device type. Default to the first child and log a warning.
            Debug.LogError($"No Rebinding Submenu for Device {ClientInput.LastUsedDevice.ToString()}");
            return 0;
        }


        public void Init()
        {
            for(int i = 0; i < Children.Length; ++i)
            {
                (Children[i].Menu as RebindingSubmenu).Init();
            }
        }


        public void SaveAllOptionsToPrefs() => (PreviouslySelectedChildMenu as RebindingSubmenu).SaveAllOptionsToPrefs();
        public void LoadAllOptionsFromPrefs() => (PreviouslySelectedChildMenu as RebindingSubmenu).LoadAllOptionsFromPrefs();
        public void ResetAllOptions() => (PreviouslySelectedChildMenu as RebindingSubmenu).ResetAllOptions();


#if UNITY_EDITOR

        private void OnValidate()
        {
            for (int i = 0; i < Children.Length; ++i)
            {
                if (Children[i].Menu == null)
                    continue;

                if (!Children[i].Menu.GetType().IsAssignableFrom(typeof(RebindingSubmenu)))
                    Debug.LogWarning($"Rebinding Root Menu '{this.name}' has a non-RebindingSubmenu child menu: {Children[i].Menu.name}");
            }
        }

#endif
    }
}