using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
        [SerializeField] private Selectable _firstSelectedElement;
        protected GameObject FirstSelectedElement => _firstSelectedElement.gameObject;

        public GameObject NavigationTarget => FirstSelectedElement;

        protected virtual void Start()
        {
            Hide();
        }


        // Open this menu.
        public virtual void Open(bool selectFirstElement = true)
        {
            Debug.Log(this.name + " Select First?: " + selectFirstElement);
            Show();

            if (selectFirstElement)
                EventSystem.current.SetSelectedGameObject(FirstSelectedElement);
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