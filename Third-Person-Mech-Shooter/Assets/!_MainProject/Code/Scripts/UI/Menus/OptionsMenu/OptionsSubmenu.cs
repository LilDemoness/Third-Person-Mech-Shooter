using UnityEngine;

namespace Gameplay.UI.Menus.Options
{
    // Doesn't inherit from Menu as we don't want to open via MenuManager
    //  so that we can preserve our back button going straight to the main pause menu.
    public class OptionsSubmenu : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        private BaseSetOption[] _optionSetters;


        public void Show() => _canvasGroup.Show();
        public void Hide() => _canvasGroup.Hide();


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
        }
        public void LoadAllOptionsFromPrefs()
        {
            for (int i = 0; i < _optionSetters.Length; ++i)
            {
                _optionSetters[i].LoadFromPrefs();
            }
        }

        public void ResetAllOptions()
        {
            for (int i = 0; i < _optionSetters.Length; ++i)
            {
                _optionSetters[i].ResetValue();
            }
        }
    }
}