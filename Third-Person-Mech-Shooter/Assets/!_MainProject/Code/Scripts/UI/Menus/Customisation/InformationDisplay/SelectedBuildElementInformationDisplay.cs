using System.Collections.Generic;
using UnityEngine;
using UI.Tables;
using TMPro;
using Gameplay.GameplayObjects.Character.Customisation.Data;

namespace Gameplay.UI.Menus.Customisation
{
    /// <summary>
    ///     Displays information on the currently selected item.
    /// </summary>
    public class SelectedBuildElementInformationDisplay : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Basic Information")]
        [SerializeField] private TMP_Text _nameLabel;
        [SerializeField] private TMP_Text _typeLabel;   // E.g. "Aux Weapon", "Large Frame", "Core System".
        [SerializeField] private TMP_Text _descriptionLabel;


        [Header("Information Table")]
        [SerializeField] private InformationTableRow _informationRowPrefab;
        [SerializeField] private Transform _informationRowContainer;
        private List<InformationTableRow> _informationRowInstances;
        private int _firstInactiveInstanceIndex;


        private void Awake()
        {
            _informationRowInstances = new List<InformationTableRow>();
            _firstInactiveInstanceIndex = 0;

            for(int i = _informationRowContainer.childCount - 1; i >= 0; --i)
                Destroy(_informationRowContainer.GetChild(i).gameObject);
        }


        public void DisplayCustomText(string name, string type, string description)
        {
            ClearInformationTable();

            _nameLabel.text = name;
            _typeLabel.text = type;
            _descriptionLabel.text = description;
        }
        public void DisplayForElement(BaseCustomisationData customisationData)
        {
            ClearInformationTable();

            _nameLabel.text = customisationData.Name;
            _descriptionLabel.text = customisationData.Description;

            switch(customisationData)
            {
                case FrameData frameData:
                    DisplayFrame(frameData);
                    break;
                case CoreSystemData coreSystemData:
                    DisplayCoreSystem(coreSystemData);
                    break;
                case ModuleData moduleData:
                    DisplayModule(moduleData);
                    break;
                default: throw new System.NotImplementedException($"No Display Information setup for type {customisationData.GetType().ToString()}");
            }

            _canvasGroup.Show();
        }
        public void Hide()
        {
            _canvasGroup.Hide();
        }


        /// <summary>
        ///     Updates non-shared UI elements for the given Frame Data.<br/>
        ///     Shared UI elements (E.g. Name, Description) are updated in <see cref="DisplayForElement(BaseCustomisationData)"/>
        /// </summary>
        private void DisplayFrame(FrameData frameData)
        {
            _typeLabel.text = frameData.FrameSize.ToString() + " Frame";

            // Display Information:
            SetupInformationTableRow("Health", frameData.MaxHealth.ToString());
            SetupInformationTableRow("Size", frameData.FrameSize.ToString());
            SetupInformationTableRow("Speed", frameData.MovementSpeed.ToString());
            SetupInformationTableRow("Heat Capacity", frameData.HeatCapacity.ToString());
        }
        /// <summary>
        ///     Updates non-shared UI elements for the given Core System Data.<br/>
        ///     Shared UI elements (E.g. Name, Description) are updated in <see cref="DisplayForElement(BaseCustomisationData)"/>
        /// </summary>
        private void DisplayCoreSystem(CoreSystemData coreSystemData)
        {
            _typeLabel.text = "Core System";

            // Display Information:
            //  - Activation Time
            //  - Recharge Rate/Cooldown
        }
        /// <summary>
        ///     Updates non-shared UI elements for the given Module Data.<br/>
        ///     Shared UI elements (E.g. Name, Description) are updated in <see cref="DisplayForElement(BaseCustomisationData)"/>
        /// </summary>
        private void DisplayModule(ModuleData moduleData)
        {
            _typeLabel.text = moduleData.ModuleSize.ToDisplayString() + " Weapon";

            // Display Applicable Information:
            //  - Damage
        }


        private void SetupInformationTableRow(string name, string value)
        {
            if (_firstInactiveInstanceIndex == _informationRowInstances.Count)
                _informationRowInstances.Add(Instantiate(_informationRowPrefab, _informationRowContainer));

            _informationRowInstances[_firstInactiveInstanceIndex].SetText(name, value);
            _informationRowInstances[_firstInactiveInstanceIndex].Show();
            ++_firstInactiveInstanceIndex;
        }
        private void ClearInformationTable()
        {
            for(int i = 0; i < _firstInactiveInstanceIndex; ++i)
                _informationRowInstances[i].Hide();
            _firstInactiveInstanceIndex = 0;
        }
    }
}