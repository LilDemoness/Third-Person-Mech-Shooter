using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gameplay.GameplayObjects.Character.Customisation.Data;
using Gameplay.GameplayObjects.Players;

namespace Gameplay.UI.Menus.Customisation
{
    public class BuildCustomisationUI : MonoBehaviour
    {
        private const float HEADER_TOTAL_SIZE = 40.0f + 0.0f;  // Header Size + Spacing.
        private const float BUTTON_SIZE = 40.0f;
        private const float BUTTON_SPACING = 0.0f;


        [Header("Customisation Buttons")]
        [SerializeField] private CustomiseFrameButton _frameButton;
        [SerializeField] private CustomiseCoreSystemButton _coreSystemButton;

        [Space(5)]
        [SerializeField] private CustomiseModuleButton _moduleButtonPrefab;
        private List<CustomiseModuleButton> _moduleButtonInstances;
        private int _selectedModuleButtonIndex;

        [SerializeField] private Transform _moduleButtonContainer;


        [Space(5)]
        [SerializeField] private CustomiseColourButton _customisePrimaryColourButton;
        [SerializeField] private CustomiseColourButton _customiseSecondaryColourButton;
        [SerializeField] private CustomiseColourButton _customiseTertiaryColourButton;
        [SerializeField] private CustomiseColourButton _customiseGlowColourButton;


        [Header("Information Display")]
        [SerializeField] private SelectedBuildElementInformationDisplay _selectedElementInfoDisplay;


        [Header("A")]
        [SerializeField] private CanvasGroup _selectionRootCanvasGroup;
        [SerializeField] private CustomisationOptionSelectionUI _customisationOptionSelectionUI;


        private void Awake()
        {
            _moduleButtonInstances = new List<CustomiseModuleButton>();
            _selectedModuleButtonIndex = -1;
            for(int i = _moduleButtonContainer.childCount - 1; i >= 0; --i)
                Destroy(_moduleButtonContainer.GetChild(i).gameObject);

            OpenCustomisationTypeSelectionMenu();
            SubscribeToButtonEvents();

            PersistentPlayer.OnLocalPlayerBuildChanged += OnBuildChanged;
        }
        private IEnumerator Start()
        {
            yield return null;
            OnBuildChanged(PersistentPlayer.LocalPersistentPlayer.NetworkBuildState.BuildDataReference);    // Temp - Ensure build data is loaded initially.
        }
        private void OnDestroy()
        {
            UnsubscribeToButtonEvents();

            PersistentPlayer.OnLocalPlayerBuildChanged -= OnBuildChanged;
        }

        private void SubscribeToButtonEvents()
        {
            // Frame.
            _frameButton.OnSelected += ElementButtonSelected;
            _frameButton.OnClicked += OnFrameButtonClicked;

            // Core System.
            _coreSystemButton.OnSelected += ElementButtonSelected;

            // Colour Customisation.
            _customisePrimaryColourButton.OnClicked += OnCustomiseColourButtonClicked;
            _customiseSecondaryColourButton.OnClicked += OnCustomiseColourButtonClicked;
            _customiseTertiaryColourButton.OnClicked += OnCustomiseColourButtonClicked;
            _customiseGlowColourButton.OnClicked += OnCustomiseColourButtonClicked;
        }
        private void UnsubscribeToButtonEvents()
        {
            // Frame.
            _frameButton.OnSelected -= ElementButtonSelected;
            _frameButton.OnClicked -= OnFrameButtonClicked;

            // Core System.
            _coreSystemButton.OnSelected -= ElementButtonSelected;
            
            // Modules.
            for(int i = 0; i < _moduleButtonInstances.Count; ++i)
            {
                _moduleButtonInstances[i].OnSelected -= ElementButtonSelected;
                _moduleButtonInstances[i].OnClicked -= OnModuleButtonClicked;
            }

            // Colour Customisation.
            _customisePrimaryColourButton.OnClicked -= OnCustomiseColourButtonClicked;
            _customiseSecondaryColourButton.OnClicked -= OnCustomiseColourButtonClicked;
            _customiseTertiaryColourButton.OnClicked -= OnCustomiseColourButtonClicked;
            _customiseGlowColourButton.OnClicked -= OnCustomiseColourButtonClicked;
        }


        private void OnBuildChanged(BuildData buildData)
        {
            Debug.Log("Build Changed");

            // Update Frame & Core System Buttons.
            FrameData frameData = CustomisationOptionsDatabase.AllOptionsDatabase.GetFrame(buildData.ActiveFrameIndex);
            _frameButton.SetCurrentData(frameData);
            _coreSystemButton.SetCurrentData(frameData.CoreSystem);

            // Update Module Buttons.
            int moduleButtonsCount = _moduleButtonInstances.Count;
            int desiredButtonsCount = frameData.AttachmentPoints.Length;
            for(int i = 0; i < Mathf.Max(desiredButtonsCount, moduleButtonsCount); ++i)
            {
                if (i >= moduleButtonsCount)
                    CreateModuleButton();

                if (i >= desiredButtonsCount)
                    _moduleButtonInstances[i].gameObject.SetActive(false);  // We're not wanting this button to be active.
                else
                {
                    // This button should be active. Set it up and show it.
                    _moduleButtonInstances[i].SetCurrentData(buildData.GetSlottableData(i.ToSlotIndex()));
                    _moduleButtonInstances[i].SetMountSize(frameData.AttachmentPoints[i].MaxModuleSize);
                    _moduleButtonInstances[i].gameObject.SetActive(true);
                }
            }

            (_moduleButtonContainer.transform.parent as RectTransform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, HEADER_TOTAL_SIZE + (BUTTON_SIZE * desiredButtonsCount) + (BUTTON_SPACING * Mathf.Max(desiredButtonsCount - 1, 0)));
        }
        /// <summary>
        ///     Creates a new Module Button Instance, subscribes to its events, and adds it to the Module Button Instances list.
        /// </summary>
        private void CreateModuleButton()
        {
            CustomiseModuleButton moduleButton = Instantiate(_moduleButtonPrefab, _moduleButtonContainer);
            moduleButton.OnSelected += ElementButtonSelected;
            moduleButton.OnClicked += OnModuleButtonClicked;

            _moduleButtonInstances.Add(moduleButton);
        }


        private void ElementButtonSelected<T>(CustomiseElementButtonBase<T> button) where T : BaseCustomisationData
        {
            _selectedElementInfoDisplay.DisplayForElement(button.CurrentData);
        }


        private void OnFrameButtonClicked(CustomiseElementButtonBase<FrameData> button)
        {
            OpenCustomisationElementSelectionMenu(CustomisationOptionsDatabase.AllOptionsDatabase.FrameDatas);
            _customisationOptionSelectionUI.SetElementSelectedCallback(OnFrameSelected);
        }
        private void OnModuleButtonClicked(CustomiseElementButtonBase<ModuleData> button)
        {
            // Get the selected frame.
            FrameData frameData = CustomisationOptionsDatabase.AllOptionsDatabase.GetFrame(PersistentPlayer.LocalPersistentPlayer.NetworkBuildState.ActiveFrameIndex.Value);

            // Get the index of the button.
            _selectedModuleButtonIndex = -1;
            for(int i = 0; i < _moduleButtonInstances.Count; ++i)
            {
                if (_moduleButtonInstances[i] == button)
                {
                    _selectedModuleButtonIndex = i;
                    break;
                }
            }

            // Show the valid module datas.
            OpenCustomisationElementSelectionMenu(frameData.AttachmentPoints[_selectedModuleButtonIndex].ValidModuleDatas);
            _customisationOptionSelectionUI.SetElementSelectedCallback(OnModuleSelected);
        }

        public void OnCustomiseColourButtonClicked() { }



        private void OnFrameSelected(BaseCustomisationData baseCustomisationData)
        {
            if (baseCustomisationData.GetType() != typeof(FrameData))
                throw new System.ArgumentException($"You are trying to use a non-FrameData BaseCustomisationData as a FrameData. Type: {baseCustomisationData.GetType().ToString()}");

            int frameDataIndex = CustomisationOptionsDatabase.AllOptionsDatabase.GetIndexForFrameData(baseCustomisationData as FrameData);
            PersistentPlayer.LocalPersistentPlayer.NetworkBuildState.SelectFrame(frameDataIndex);
        }
        private void OnModuleSelected(BaseCustomisationData baseCustomisationData)
        {
            if (baseCustomisationData.GetType() != typeof(ModuleData))
                throw new System.ArgumentException($"You are trying to use a non-ModuleData BaseCustomisationData as a ModuleData. Type: {baseCustomisationData.GetType().ToString()}");

            int moduleIndex = CustomisationOptionsDatabase.AllOptionsDatabase.GetIndexForModuleData(baseCustomisationData as ModuleData);
            PersistentPlayer.LocalPersistentPlayer.NetworkBuildState.SelectSlottableData(_selectedModuleButtonIndex.ToSlotIndex(), moduleIndex);
        }


        public void OpenCustomisationTypeSelectionMenu() => CloseCustomisationElementSelectionMenu();
        public void CloseCustomisationElementSelectionMenu()
        {
            _selectionRootCanvasGroup.Show();
            _customisationOptionSelectionUI.Hide();
        }
        public void OpenCustomisationElementSelectionMenu<T>(List<T> customisationDatas) where T : BaseCustomisationData
        {
            _selectionRootCanvasGroup.Hide();
            _customisationOptionSelectionUI.Show(customisationDatas);
        }
    }
}