using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI.Menus.Customisation
{
    public class CustomiseColourButton : MonoBehaviour
    {
        [SerializeField] private Image _colourDisplayIcon;

        public event System.Action OnClicked;
    }
}