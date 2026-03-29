using UnityEngine;
using UnityEngine.UI;
using Gameplay.GameplayObjects.Character.Customisation.Data;

namespace Gameplay.UI.Menus.Customisation
{
    [RequireComponent(typeof(SelectableEvents), typeof(Button))]
    public class CustomisationOptionSelectionButton : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        private BaseCustomisationData _customisationData;


        private Button _button;
        public Selectable Selectable => _button as Selectable;


        public System.Action<BaseCustomisationData> OnSelected;
        public System.Action<BaseCustomisationData> OnClicked;


        private void Awake()
        {
            GetComponent<SelectableEvents>().OnSelected += SelectableEvents_OnSelected;

            _button = GetComponent<Button>();
            _button.onClick.AddListener(Button_OnClicked);
        }
        private void OnDestroy()
        {
            GetComponent<SelectableEvents>().OnSelected -= SelectableEvents_OnSelected;
            _button.onClick.RemoveListener(Button_OnClicked);
        }

        private void SelectableEvents_OnSelected() => OnSelected?.Invoke(_customisationData);
        private void Button_OnClicked() => OnClicked?.Invoke(_customisationData);


        public void Setup(BaseCustomisationData customisationData)
        {
            this._customisationData = customisationData;
            this._icon.sprite = customisationData.Sprite;
        }

        public void Show() => this.gameObject.SetActive(true);
        public void Hide() => this.gameObject.SetActive(false);
    }
}