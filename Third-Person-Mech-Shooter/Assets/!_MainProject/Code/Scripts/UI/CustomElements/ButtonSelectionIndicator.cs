using UnityEngine;

namespace Gameplay.UI.Menus
{
    public class ButtonSelectionIndicator : MonoBehaviour
    {
        [SerializeField] private GameObject _selectionBackgroundObject;

        private void Awake() => OnTabExited();

        public void OnTabEntered() => _selectionBackgroundObject.SetActive(true);
        public void OnTabExited() => _selectionBackgroundObject.SetActive(false);
    }
}
