using Cysharp.Threading.Tasks;
using Gameplay.UI.Popups;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI.Menus.Options
{
    public class OptionsSubmenu : Menu
    {
        private BaseSetOption[] _optionSetters;
        private static bool _hasChanges = false;
        public bool HasChanges => _hasChanges;


        protected virtual void OnDestroy() => BaseSetOption.OnAnyChanged -= OnAnyOptionChanged;


        public override void Open(bool selectFirstElement = true)
        {
            _hasChanges = false;
            BaseSetOption.OnAnyChanged += OnAnyOptionChanged;
            base.Open(selectFirstElement);
        }
        public void ForceClose()
        {
            //LoadAllOptionsFromPrefs();
            base.Close().Forget();
        }
        public override async UniTask<bool> Close()
        {
            Debug.Log("Try Close: " + _hasChanges);
            if (_hasChanges)
            {
                bool? success = null;
                PopupManager.ShowUnsavedChangesOptionsPanel(OnCancel, OnDiscard, OnSave);
                await UniTask.WaitUntil(() => success.HasValue);

                if (!success.Value)
                    return false;   // Closing was cancelled.

                // Closing successfully completed.
                await base.Close();
                FinishClose();
                return true;

                void OnCancel() => success = false;
                void OnDiscard() { LoadAllOptionsFromPrefs(); success = true; }
                void OnSave() { SaveAllOptionsToPrefs(); success = true; }
            }
            else
            {
                await base.Close();
                FinishClose();
                return true;
            }
        }
        protected virtual void FinishClose()
        {
            _hasChanges = false;
            BaseSetOption.OnAnyChanged -= OnAnyOptionChanged;
        }


        public void Init()
        {
            _optionSetters = GetComponentsInChildren<BaseSetOption>();
            InitialiseAllOptions();
            SetupNavigation();
        }
        protected virtual void InitialiseAllOptions()
        {
            for(int i = 0; i < _optionSetters.Length; ++i)
            {
                _optionSetters[i].Initialise();
            }
        }
        protected virtual void SetupNavigation()
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


        public virtual void SaveAllOptionsToPrefs()
        {
            for (int i = 0; i < _optionSetters.Length; ++i)
            {
                _optionSetters[i].SaveToPrefs();
            }

            // We've saved all our changes, so no new changes exist.
            _hasChanges = false;
        }
        public virtual void LoadAllOptionsFromPrefs()
        {
            for (int i = 0; i < _optionSetters.Length; ++i)
            {
                _optionSetters[i].LoadFromPrefs();
            }

            // We've just reloaded our saved options, so we don't have any changes from them.
            _hasChanges = false;
        }

        public virtual void ResetAllOptions()
        {
            for (int i = 0; i < _optionSetters.Length; ++i)
            {
                _optionSetters[i].ResetValue();
            }

            // We've just performed a change, so ensure that we are aware of this.
            _hasChanges = true;
        }



        protected void OnAnyOptionChanged() => _hasChanges = true;
    }
}