using System.Collections.Generic;
using UnityEngine;
using Gameplay.GameplayObjects.Character.Customisation.Data;

namespace Gameplay.UI.Menus.Customisation
{
    public class CustomisationOptionSelectionUI : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private SelectedBuildElementInformationDisplay _selectedElementInfoDisplay;


        [Header("Selection Tabs Display")]
        [SerializeField] private bool _temp;

        
        [Header("Selection Options")]
        [SerializeField] private CustomisationOptionSelectionButton _customisationOptionButtonPrefab;
        private List<CustomisationOptionSelectionButton> _customisationOptionButtonInstances;
        private int _firstInactiveButtonIndex;

        [SerializeField] private Transform _customisationOptionsContainer;


        private System.Action<BaseCustomisationData> _onElementSelectedCallback;
        public void SetElementSelectedCallback(System.Action<BaseCustomisationData> callback) => _onElementSelectedCallback = callback;


        private void Awake()
        {
            Debug.LogWarning($"Not Yet Implemented: Selection Tabs (Temp Var: {_temp})");

            _customisationOptionButtonInstances = new List<CustomisationOptionSelectionButton>();
            _firstInactiveButtonIndex = 0;

            for (int i = _customisationOptionsContainer.childCount - 1; i >= 0; --i)
                Destroy(_customisationOptionsContainer.GetChild(i).gameObject);

            Hide();
        }


        public void Show<T>(List<T> customisationDatas) where T : BaseCustomisationData
        {
            ClearInformationTable();

            for(int i = 0; i < customisationDatas.Count; ++i)
                SetupOptionButton(customisationDatas[i]);

            _canvasGroup.Show();
        }
        public void Hide() => _canvasGroup.Hide();


        private void SetupOptionButton<T>(T data) where T : BaseCustomisationData
        {
            if (_firstInactiveButtonIndex == _customisationOptionButtonInstances.Count)
            {
                CustomisationOptionSelectionButton button = Instantiate(_customisationOptionButtonPrefab, _customisationOptionsContainer);
                button.OnSelected += OnButtonSelected;
                button.OnClicked += OnButtonClicked;
                _customisationOptionButtonInstances.Add(button);
            }

            _customisationOptionButtonInstances[_firstInactiveButtonIndex].Show();
            _customisationOptionButtonInstances[_firstInactiveButtonIndex].Setup(data);
            ++_firstInactiveButtonIndex;
        }
        private void ClearInformationTable()
        {
            for (int i = 0; i < _firstInactiveButtonIndex; ++i)
                _customisationOptionButtonInstances[i].Hide();
            _firstInactiveButtonIndex = 0;
        }


        private void OnButtonSelected(BaseCustomisationData baseCustomisation) => _selectedElementInfoDisplay.DisplayForElement(baseCustomisation);
        private void OnButtonClicked(BaseCustomisationData baseCustomisation) => _onElementSelectedCallback?.Invoke(baseCustomisation);
    }
}