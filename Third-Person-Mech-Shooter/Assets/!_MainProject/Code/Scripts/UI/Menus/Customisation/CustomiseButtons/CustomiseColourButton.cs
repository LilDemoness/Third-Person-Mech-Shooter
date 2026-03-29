using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI.Menus.Customisation
{
    [RequireComponent(typeof(Button))]
    public class CustomiseColourButton : MonoBehaviour
    {
        [SerializeField] private Image _colourDisplayIcon;

        private Button _button;
        public Selectable Selectable => _button as Selectable;

        public event System.Action OnClicked;


        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(Button_OnClicked);
        }
        private void OnDestroy()
        {
            _button.onClick.RemoveListener(Button_OnClicked);
        }

        private void Button_OnClicked() => this.OnClicked?.Invoke();
    }
}