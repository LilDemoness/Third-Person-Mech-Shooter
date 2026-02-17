using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Gameplay.UI.Menus
{
    public class Menu : MonoBehaviour
    {
        [SerializeField] protected CanvasGroup CanvasGroup;
        [SerializeField] protected GameObject InitialSelection;


        public event System.Action OnShow;
        public event System.Action OnHide;

        public UnityEvent OnReturnToRootMenuPerformed;



        protected virtual void Start()
        {
            Hide();
        }


        public virtual void ReturnToRootMenu() => OnReturnToRootMenuPerformed?.Invoke();

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