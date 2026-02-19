using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI.Menus
{
    [RequireComponent(typeof(Button))]
    public class MenuTabButton : MonoBehaviour
    {
        [SerializeField] private ContainerMenu _parentMenu;
        [SerializeField] private Menu _menu;


        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(OnButtonSelected);
        }
        private void OnDestroy()
        {
            GetComponent<Button>().onClick.RemoveListener(OnButtonSelected);
        }

        public void OnButtonSelected() => _parentMenu.ShowSubmenu(_menu);
    }
}