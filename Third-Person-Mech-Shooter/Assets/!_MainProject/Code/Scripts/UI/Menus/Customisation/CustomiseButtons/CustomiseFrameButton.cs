using UnityEngine;
using TMPro;
using Gameplay.GameplayObjects.Character.Customisation.Data;

namespace Gameplay.UI.Menus.Customisation
{
    public class CustomiseFrameButton : CustomiseElementButtonBase<FrameData>
    {
        [SerializeField] private TMP_Text _currentSelectionText;

        public override void SetCurrentData(FrameData customisationData)
        {
            base.SetCurrentData(customisationData);
            _currentSelectionText.text = customisationData.Name;
        }
    }
}