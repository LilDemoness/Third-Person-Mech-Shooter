using UnityEngine;

namespace Gameplay.UI.Menus.Options
{
    // Don't inherit from Menu as we don't want to open via MenuManager
    //  so that we can preserve our back button going straight to the main pause menu.
    public class OptionsSubmenu : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;

        public void Show() => _canvasGroup.Show();
        public void Hide() => _canvasGroup.Hide();
    }
}