using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI.Menus.Options
{
    [RequireComponent(typeof(RebindingRootMenuContainer))]
    public class RebindingRootContainerMenu : ContainerMenu, IOptionsSubmenu
    {
        private RebindingRootMenuContainer _rebindingMenuContainer => (MenuContainer as RebindingRootMenuContainer);


        public bool HasChanges => _rebindingMenuContainer.HasChanges;

        public void Init() => _rebindingMenuContainer.Init();

        public void SaveAllOptionsToPrefs() => _rebindingMenuContainer.SaveAllOptionsToPrefs();
        public void LoadAllOptionsFromPrefs() => _rebindingMenuContainer.LoadAllOptionsFromPrefs();
        public void ResetAllOptions() => _rebindingMenuContainer.ResetAllOptions();
    }
}