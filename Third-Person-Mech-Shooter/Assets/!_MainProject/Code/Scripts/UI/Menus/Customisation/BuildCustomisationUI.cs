using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gameplay.GameplayObjects.Character.Customisation.Data;
using Gameplay.GameplayObjects.Players;

namespace Gameplay.UI.Menus.Customisation
{
    public class BuildCustomisationUI : MenuContainer
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
        [SerializeField] private Menu _customisationTypeSelectionMenu;
        [SerializeField] private CustomisationOptionSelectionUI _customisationOptionSelectionUI;


        protected override void Awake()
        {
            base.Awake();

            _moduleButtonInstances = new List<CustomiseModuleButton>();
            _selectedModuleButtonIndex = -1;
            for(int i = _moduleButtonContainer.childCount - 1; i >= 0; --i)
                Destroy(_moduleButtonContainer.GetChild(i).gameObject);

            SubscribeToButtonEvents();

            PersistentPlayer.OnLocalPlayerBuildChanged += OnBuildChanged;
            _customisationOptionSelectionUI.OnCustomisationTabButtonPressed += CustomisationOptionSelectionUI_OnCustomisationTabButtonPressed;
        }
        private IEnumerator Start()
        {
            yield return null;
            EnterChild(0);
            OnBuildChanged(PersistentPlayer.LocalPersistentPlayer.NetworkBuildState.BuildDataReference);    // Temp - Ensure build data is loaded initially.
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnsubscribeToButtonEvents();

            PersistentPlayer.OnLocalPlayerBuildChanged -= OnBuildChanged;
            _customisationOptionSelectionUI.OnCustomisationTabButtonPressed -= CustomisationOptionSelectionUI_OnCustomisationTabButtonPressed;
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


            // Create the required Module Buttons (Created separately from updating to allow easier navigation setup).
            int desiredButtonsCount = frameData.AttachmentPoints.Length;
            int moduleButtonsCount = _moduleButtonInstances.Count;
            for(int i = 0; i < desiredButtonsCount - moduleButtonsCount; ++i)
                CreateModuleButton();

            // Update Module Buttons.
            for(int i = 0; i < Mathf.Max(desiredButtonsCount, moduleButtonsCount); ++i)
            {
                if (i >= desiredButtonsCount) // This button is unneeded and should be set as inactive.
                    _moduleButtonInstances[i].gameObject.SetActive(false);
                else // This button should be active.
                {
                    // Set up the button's data.
                    _moduleButtonInstances[i].SetCurrentData(buildData.GetSlottableData(i.ToSlotIndex()));
                    _moduleButtonInstances[i].SetMountSize(frameData.AttachmentPoints[i].MaxModuleSize);

                    // Setup the button's navigation.
                    _moduleButtonInstances[i].Selectable.AddNavigation(
                        onUp: (i == 0 ? _coreSystemButton.Selectable : _moduleButtonInstances[i - 1].Selectable),
                        //onDown: (i == desiredButtonsCount - 1 ? _customisePrimaryColourButton.Selectable : _moduleButtonInstances[i + 1].Selectable)); // Re-enable once customisation is implemented.
                        onDown: (i == desiredButtonsCount - 1 ? _frameButton.Selectable : _moduleButtonInstances[i + 1].Selectable));

                    // Show the button.
                    _moduleButtonInstances[i].gameObject.SetActive(true);
                }
            }

            // Resize the module button container to ensure there isn't overlap between sections.
            (_moduleButtonContainer.transform.parent as RectTransform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, HEADER_TOTAL_SIZE + (BUTTON_SIZE * desiredButtonsCount) + (BUTTON_SPACING * Mathf.Max(desiredButtonsCount - 1, 0)));


            // Setup Variable Navigation of other buttons.
            //_frameButton.Selectable.AddNavigation(onUp: _customiseGlowColourButton.Selectable); // Re-enable once customisation is implemented.
            //_coreSystemButton.Selectable.AddNavigation(onDown: (desiredButtonsCount > 0 ? _moduleButtonInstances[0].Selectable : _customisePrimaryColourButton.Selectable));  // Re-enable once customisation is implemented.
            //_customisePrimaryColourButton.Selectable.AddNavigation(onUp: (desiredButtonsCount > 0 ? _moduleButtonInstances[desiredButtonsCount - 1].Selectable : _coreSystemButton.Selectable));  // Re-enable once customisation is implemented.
            //_customiseGlowColourButton.Selectable.AddNavigation(onDown: _frameButton.Selectable);  // Re-enable once customisation is implemented.
            _frameButton.Selectable.AddNavigation(onUp: _moduleButtonInstances[desiredButtonsCount - 1].Selectable);
            _coreSystemButton.Selectable.AddNavigation(onDown: _moduleButtonInstances[0].Selectable);



            // Update Customisation Selection Tabs.
            _customisationOptionSelectionUI.SetDisplayedTabs(frameData.AttachmentPoints);


            // Reselect our currently selected gameobject to update our information display.
            StartCoroutine(ReselectCurrentSelection());
        }
        private IEnumerator ReselectCurrentSelection()
        {
            GameObject selectedObject = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            yield return null;
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(selectedObject);
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


        private void CustomisationOptionSelectionUI_OnCustomisationTabButtonPressed(int moduleIndex)
        {
            if (moduleIndex == -1)
                SelectFrameMenu();
            else
                SelectModuleMenu(moduleIndex);
        }


        private void ElementButtonSelected<T>(CustomiseElementButtonBase<T> button) where T : BaseCustomisationData
        {
            if (button.CurrentData == null)
                return;

            _selectedElementInfoDisplay.DisplayForElement(button.CurrentData);
        }


        private void OnFrameButtonClicked(CustomiseElementButtonBase<FrameData> button) => SelectFrameMenu();
        private void SelectFrameMenu()
        {
            _selectedModuleButtonIndex = -1;
            OpenCustomisationElementSelectionMenu(CustomisationOptionsDatabase.AllOptionsDatabase.FrameDatas, "Frames");
            _customisationOptionSelectionUI.SetElementSelectedCallback(OnFrameSelected);
        }
        private void OnModuleButtonClicked(CustomiseElementButtonBase<ModuleData> button)
        {
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
            SelectModuleMenu(_selectedModuleButtonIndex);
        }
        private void SelectModuleMenu(int moduleIndex)
        {
            // Get the selected frame.
            FrameData frameData = CustomisationOptionsDatabase.AllOptionsDatabase.GetFrame(PersistentPlayer.LocalPersistentPlayer.NetworkBuildState.ActiveFrameIndex.Value);

            _selectedModuleButtonIndex = moduleIndex;
            OpenCustomisationElementSelectionMenu(frameData.AttachmentPoints[_selectedModuleButtonIndex].ValidModuleDatas, string.Format("Attachment Slot {0}", _selectedModuleButtonIndex));
            _customisationOptionSelectionUI.SetElementSelectedCallback(OnModuleSelected);
        }

        public void OnCustomiseColourButtonClicked() { }


        public void SelectPreviousElement()
        {
            // Frame > Modules.
            if(_selectedModuleButtonIndex == 0)
            {
                // Select Frame.
                SelectFrameMenu();
            }
            else if(_selectedModuleButtonIndex == -1)
            {
                // Select Last Module.
                FrameData frameData = CustomisationOptionsDatabase.AllOptionsDatabase.GetFrame(PersistentPlayer.LocalPersistentPlayer.NetworkBuildState.ActiveFrameIndex.Value);
                SelectModuleMenu(frameData.AttachmentPoints.Length - 1);
            }
            else
            {
                // Select Previous Module.
                SelectModuleMenu(_selectedModuleButtonIndex - 1);
            }
        }
        public void SelectNextElement()
        {
            // Frame > Modules.
            FrameData frameData = CustomisationOptionsDatabase.AllOptionsDatabase.GetFrame(PersistentPlayer.LocalPersistentPlayer.NetworkBuildState.ActiveFrameIndex.Value);
            if (_selectedModuleButtonIndex == frameData.AttachmentPoints.Length - 1)
            {
                // Select Frame.
                SelectFrameMenu();
            }
            else if (_selectedModuleButtonIndex == -1)
            {
                // Select First Module.
                SelectModuleMenu(0);
            }
            else
            {
                // Select Next Module.
                SelectModuleMenu(_selectedModuleButtonIndex + 1);
            }
        }



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


        public void OpenCustomisationTypeSelectionMenu() => EnterChild(_customisationTypeSelectionMenu);
        public void CloseCustomisationElementSelectionMenu() => EnterChild(_customisationTypeSelectionMenu);
        public void OpenCustomisationElementSelectionMenu<T>(List<T> customisationDatas, string name) where T : BaseCustomisationData
        {
            EnterChild(_customisationOptionSelectionUI, UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject?.GetComponent<UnityEngine.UI.Selectable>());
            _customisationOptionSelectionUI.SetDisplayedOptions(customisationDatas, name, _selectedModuleButtonIndex);
        }
    }
}