using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Gameplay.UI.Menus
{
    /*public class Menu : MonoBehaviour
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
    }*/
    public abstract class Menu : MonoBehaviour
    {
        [field: SerializeField] protected CanvasGroup CanvasGroup { get; private set; }
        [field: SerializeField] protected GameObject FirstSelectedElement { get; private set; }

        public GameObject NavigationTarget => FirstSelectedElement;

        protected virtual void Start()
        {
            Hide();
        }


        // Open this menu.
        public virtual void Open(bool selectFirstElement = true)
        {
            if (selectFirstElement)
                EventSystem.current.SetSelectedGameObject(FirstSelectedElement);
            Show();
        }
        // Close this menu.
        public virtual void Close(System.Action onCompleteCallback)
        {
            Hide();
            onCompleteCallback?.Invoke();
        }


        // Shows the menu without performing the rest of its opening logic.
        public virtual void Show() => CanvasGroup.Show();
        // Hides the menu without performing the rest of its closing logic.
        public virtual void Hide() => CanvasGroup.Hide();
    }
}