using UnityEngine;
using TMPro;
using Gameplay.GameplayObjects.Character.Customisation.Data;

namespace Gameplay.UI.Menus.Customisation
{
    public class CustomiseModuleButton : CustomiseElementButtonBase<ModuleData>
    {
        [SerializeField] private TMP_Text _currentSelectionText;
        [SerializeField] private TMP_Text _mountSizeText;

        public override void SetCurrentData(ModuleData customisationData)
        {
            base.SetCurrentData(customisationData);
            _currentSelectionText.text = customisationData.Name;
        }
        public void SetMountSize(ModuleSize moduleSize) => _mountSizeText.text = $"({moduleSize.ToDisplayString()})";
    }
}