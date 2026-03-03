using Gameplay.UI.Popups;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI.Menus.Options
{
    // Doesn't inherit from Menu as we don't want to open via MenuManager
    //  so that we can preserve our back button going straight to the main pause menu.
    public class OptionsSubmenu : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        private BaseSetOption[] _optionSetters;
        private static bool _hasChanges = false;


        private void OnDestroy() => BaseSetOption.OnAnyChanged -= OnAnyOptionChanged;


        public void Open()
        {
            _hasChanges = false;
            BaseSetOption.OnAnyChanged += OnAnyOptionChanged;
            Show();
        }
        public void ForceClose()
        {
            //LoadAllOptionsFromPrefs();
            CompleteClose(null);
        }
        public void Close(System.Action onClosedCallback)
        {
            if (_hasChanges)
            {
                PopupManager.ShowUnsavedChangesOptionsPanel(null, OnDiscard, OnSave);

                void OnSave() { SaveAllOptionsToPrefs(); CompleteClose(onClosedCallback); }
                void OnDiscard() { LoadAllOptionsFromPrefs(); CompleteClose(onClosedCallback); }
            }
            else
                CompleteClose(onClosedCallback);
        }
        private void CompleteClose(System.Action onClosedCallback)
        {
            _hasChanges = false;
            BaseSetOption.OnAnyChanged -= OnAnyOptionChanged;
            
            Hide();
            onClosedCallback?.Invoke();
        }

        private void Show() => _canvasGroup.Show();
        private void Hide() => _canvasGroup.Hide();


        public void Init()
        {
            _optionSetters = GetComponentsInChildren<BaseSetOption>();
            InitialiseAllOptions();
        }
        private void InitialiseAllOptions()
        {
            for(int i = 0; i < _optionSetters.Length; ++i)
            {
                _optionSetters[i].Initialise();
            }
        }


        public void SaveAllOptionsToPrefs()
        {
            for (int i = 0; i < _optionSetters.Length; ++i)
            {
                _optionSetters[i].SaveToPrefs();
            }
            // We've saved all our changes, so no new changes exist.
            _hasChanges = false;
        }
        public void LoadAllOptionsFromPrefs()
        {
            for (int i = 0; i < _optionSetters.Length; ++i)
            {
                _optionSetters[i].LoadFromPrefs();
            }
            // We've just reloaded our saved options, so we don't have any changes from them.
            _hasChanges = false;
        }

        public void ResetAllOptions()
        {
            for (int i = 0; i < _optionSetters.Length; ++i)
            {
                _optionSetters[i].ResetValue();
            }
        }



        private void OnAnyOptionChanged()
        {
            Debug.Log("Option Changed");
            _hasChanges = true;
        }
    }
}