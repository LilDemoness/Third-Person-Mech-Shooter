using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Gameplay.UI.Menus
{
    public class MenuTab : MonoBehaviour
    {
        [SerializeField] protected CanvasGroup CanvasGroup;
        [SerializeField] protected GameObject InitialSelection;


        public UnityEvent OnShow;
        public UnityEvent OnHide;


        protected virtual void Start()
        {
            Hide();
        }

        public virtual void Show()
        {
            CanvasGroup.Show();
            EventSystem.current.SetSelectedGameObject(InitialSelection);
            OnShow?.Invoke();
        }
        public virtual void Hide()
        {
            CanvasGroup.Hide();
            OnHide?.Invoke();
        }
    }
}