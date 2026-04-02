using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI.Menus.Customisation
{
    public class CustomisationSelectionTab : MonoBehaviour
    {
        [SerializeField] private Image _tabDisplayImage;
        [SerializeField] private GameObject _selectedBackground;

        public event System.Action<CustomisationSelectionTab> OnSelectViaButton;


        private void Awake() => SetSelected(false);
        public void Setup(Sprite sprite) => _tabDisplayImage.sprite = sprite;
        

        public void Show() => this.gameObject.SetActive(true);
        public void Hide() => this.gameObject.SetActive(false);

        public void SetSelected(bool isSelected)
        {
            _selectedBackground.SetActive(isSelected);
            if (isSelected)
                GetComponent<UIDropdownScroller>().OnSelect();
        }


        public void SelectViaButton() => OnSelectViaButton?.Invoke(this);
    }
}