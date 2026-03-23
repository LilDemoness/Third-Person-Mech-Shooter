using UnityEngine;
using UnityEngine.UI;
using UserInput;

namespace Gameplay.UI.Menus.Options
{
    public class RebindingRootMenu : ContainerMenu, IOptionsSubmenu
    {
        public bool HasChanges => PreviouslySelectedChild != null && (PreviouslySelectedChild as OptionsSubmenu).HasChanges;
        protected override int DefaultChildIndex => GetChildIndexForCurrentInputDevice();

        private int GetChildIndexForCurrentInputDevice()
        {
            // Find the child corresponding to the current active device type.
            for(int i = 0; i < Children.Length; ++i)
            {
                if ((Children[i] as RebindingSubmenu).Device == ClientInput.LastUsedDevice)
                    return i;
            }

            // No child matches the active device type. Default to the first child and log a warning.
            Debug.LogError($"No Rebinding Submenu for Device {ClientInput.LastUsedDevice.ToString()}");
            return 0;
        }

        public void Init()
        {
            foreach(OptionsSubmenu child in Children)
                child.Init();
        }

        public void SaveAllOptionsToPrefs() => (PreviouslySelectedChild as OptionsSubmenu).SaveAllOptionsToPrefs();
        public void LoadAllOptionsFromPrefs() => (PreviouslySelectedChild as OptionsSubmenu).LoadAllOptionsFromPrefs();
        public void ResetAllOptions() => (PreviouslySelectedChild as OptionsSubmenu).ResetAllOptions();
    }
}