using System.Collections.Generic;
using UnityEngine;
using Gameplay.GameplayObjects.Character.Customisation.Data;
using UnityEngine.EventSystems;
using TMPro;

namespace Gameplay.UI.Menus.Customisation
{
    public class CustomisationOptionSelectionUI : Menu
    {
        [SerializeField] private SelectedBuildElementInformationDisplay _selectedElementInfoDisplay;


        [Header("Selection Tabs Display")]
        [SerializeField] private TMP_Text _selectedTabLabel;

        [Space(5)]
        [SerializeField] private CustomisationSelectionTab _selectedTabPrefab;
        private List<CustomisationSelectionTab> _selectedTabInstances;

        [SerializeField] private Transform _selectedTabsContainer;


        [Space(5)]
        [SerializeField] private Sprite _frameSprite;
        [SerializeField] private Sprite _moduleSprite;


        [Header("Selection Options")]
        [SerializeField] private CustomisationOptionSelectionButton _customisationOptionButtonPrefab;
        private List<CustomisationOptionSelectionButton> _customisationOptionButtonInstances;

        [SerializeField] private Transform _customisationOptionsContainer;


        private System.Action<BaseCustomisationData> _onElementSelectedCallback;
        public void SetElementSelectedCallback(System.Action<BaseCustomisationData> callback) => _onElementSelectedCallback = callback;


        protected override void Awake()
        {
            base.Awake();

            // Customisation Category/Type Buttons.
            _selectedTabInstances = new List<CustomisationSelectionTab>();
            for(int i = _selectedTabsContainer.childCount - 1; i >= 0; --i)
                Destroy(_selectedTabsContainer.GetChild(i).gameObject);


            // Customisation Selection Buttons.
            _customisationOptionButtonInstances = new List<CustomisationOptionSelectionButton>();
            for (int i = _customisationOptionsContainer.childCount - 1; i >= 0; --i)
                Destroy(_customisationOptionsContainer.GetChild(i).gameObject);
        }


        public void SetDisplayedTabs(AttachmentPoint[] attachmentSlots)
        {
            const int DEFAULT_TABS_COUNT = 1;
            int requiredButtonsCount = DEFAULT_TABS_COUNT + attachmentSlots.Length;
            int currentButtonsCount = _selectedTabInstances.Count;
            for(int i = 0; i < requiredButtonsCount - currentButtonsCount; ++i)
                CreateTabButton();

            // Frame.
            _selectedTabInstances[0].Show();
            _selectedTabInstances[0].Setup(_frameSprite);   // Set Graphic.

            // Modules (Attachment Slots).
            for(int i = 0; i < attachmentSlots.Length; ++i)
            {
                _selectedTabInstances[i + 1].Show();
                _selectedTabInstances[i + 1].Setup(_moduleSprite);   // Set Graphic.
            }

            // Customisation?
        }
        private void CreateTabButton()
        {
            CustomisationSelectionTab tab = Instantiate(_selectedTabPrefab, _selectedTabsContainer);
            _selectedTabInstances.Add(tab);
        }


        public void SetDisplayedOptions<T>(List<T> customisationDatas, string tabName, int tabIndex) where T : BaseCustomisationData
        {
            int currentSelectedElementIndex = 0;
            int customisationDataCount = customisationDatas.Count;
            int optionButtonsCount = _customisationOptionButtonInstances.Count;

            // Create enough button instances (Created separately from showing/hiding/updating to allow easier navigation setup).
            for(int i = 0; i < customisationDataCount - optionButtonsCount; ++i)
                CreateOptionButton();

            // Show/Hide & Update Instance.
            for (int i = 0; i < Mathf.Max(customisationDataCount, optionButtonsCount); ++i)
            {
                if (i >= customisationDataCount)
                {
                    // This button is unneeded and should be set as inactive.
                    _customisationOptionButtonInstances[i].Hide();
                    continue;
                }

                // This button should be active.
                _customisationOptionButtonInstances[i].Show();
                _customisationOptionButtonInstances[i].Setup(customisationDatas[i]);

                // Setup the button's navigation.
                _customisationOptionButtonInstances[i].Selectable.SetNavigation(
                    onUp: (i == 0 ? _customisationOptionButtonInstances[customisationDataCount - 1] : _customisationOptionButtonInstances[i - 1]).Selectable,
                    onDown: (i == customisationDataCount - 1 ? _customisationOptionButtonInstances[0] : _customisationOptionButtonInstances[i + 1]).Selectable);

                // Select this button if it is our desired.
                if (i == currentSelectedElementIndex)
                    EventSystem.current.SetSelectedGameObject(_customisationOptionButtonInstances[i].gameObject);
            }


            // Highlight the proper tab.
            for(int i = 0; i < _selectedTabInstances.Count; ++i)
                _selectedTabInstances[i].SetSelected(i == tabIndex + 1);

            _selectedTabLabel.text = tabName;
        }
        private void CreateOptionButton()
        {
            CustomisationOptionSelectionButton button = Instantiate(_customisationOptionButtonPrefab, _customisationOptionsContainer);
            button.OnSelected += OnButtonSelected;
            button.OnClicked += OnButtonClicked;
            _customisationOptionButtonInstances.Add(button);
        }


        private void OnButtonSelected(BaseCustomisationData baseCustomisation) => _selectedElementInfoDisplay.DisplayForElement(baseCustomisation);
        private void OnButtonClicked(BaseCustomisationData baseCustomisation) => _onElementSelectedCallback?.Invoke(baseCustomisation);
    }
}