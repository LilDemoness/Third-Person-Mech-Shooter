using UnityEngine;
using UnityEngine.EventSystems;

namespace Gameplay.UI.Menus
{
    public class MenuTab : MonoBehaviour
    {
        [SerializeField] protected CanvasGroup CanvasGroup;
        [SerializeField] protected GameObject InitialSelection;



        protected virtual void Start()
        {
            Hide();
        }

        public virtual void Show()
        {
            CanvasGroup.Show();
            EventSystem.current.SetSelectedGameObject(InitialSelection);
        }
        public virtual void Hide()
        {
            CanvasGroup.Hide();
        }
    }
}