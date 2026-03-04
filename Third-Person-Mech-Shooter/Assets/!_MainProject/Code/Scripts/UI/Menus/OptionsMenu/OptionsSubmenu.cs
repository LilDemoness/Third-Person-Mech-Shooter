using Gameplay.UI.Popups;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI.Menus.Options
{
    public class OptionsSubmenu : Menu
    {
        private BaseSetOption[] _optionSetters;
        private static bool _hasChanges = false;


        private void OnDestroy() => BaseSetOption.OnAnyChanged -= OnAnyOptionChanged;


        public override void Open(bool selectFirstElement = true)
        {
            _hasChanges = false;
            BaseSetOption.OnAnyChanged += OnAnyOptionChanged;
            base.Open(selectFirstElement);
        }
        public void ForceClose()
        {
            //LoadAllOptionsFromPrefs();
            CompleteClose(null);
        }
        public override void Close(System.Action onCompleteCallback)
        {
            if (_hasChanges)
            {
                PopupManager.ShowUnsavedChangesOptionsPanel(null, OnDiscard, OnSave);

                void OnSave() { SaveAllOptionsToPrefs(); CompleteClose(onCompleteCallback); }
                void OnDiscard() { LoadAllOptionsFromPrefs(); CompleteClose(onCompleteCallback); }
            }
            else
                CompleteClose(onCompleteCallback);
        }
        private void CompleteClose(System.Action onClosedCallback)
        {
            _hasChanges = false;
            BaseSetOption.OnAnyChanged -= OnAnyOptionChanged;

            base.Close(onClosedCallback);
        }


        public void Init()
        {
            _optionSetters = GetComponentsInChildren<BaseSetOption>();
            InitialiseAllOptions();
            SetupNavigation();
        }
        private void InitialiseAllOptions()
        {
            for(int i = 0; i < _optionSetters.Length; ++i)
            {
                _optionSetters[i].Initialise();
            }
        }
        private void SetupNavigation()
        {
            if (_optionSetters.Length <= 1)
                return;

            for(int i = 0; i < _optionSetters.Length; ++i)
            {
                if (i == 0)
                    _optionSetters[i].SetupNavigation(_optionSetters[_optionSetters.Length - 1], _optionSetters[i + 1]);
                else if (i == _optionSetters.Length - 1)
                    _optionSetters[i].SetupNavigation(_optionSetters[i - 1], _optionSetters[0]);
                else
                    _optionSetters[i].SetupNavigation(_optionSetters[i - 1], _optionSetters[i + 1]);
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