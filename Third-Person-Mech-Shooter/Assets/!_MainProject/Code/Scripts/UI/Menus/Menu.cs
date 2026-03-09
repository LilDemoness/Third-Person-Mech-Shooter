using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gameplay.UI.Menus
{
    public class Menu : MonoBehaviour
    {
        [field: SerializeField] protected CanvasGroup CanvasGroup { get; private set; }
        [SerializeField] private Selectable _firstSelectedElement;
        protected GameObject FirstSelectedElement => _firstSelectedElement?.gameObject;

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
        public virtual void Reopen(Selectable targetSelectable = null)
        {
            Show();
            EventSystem.current.SetSelectedGameObject(targetSelectable != null ? targetSelectable.gameObject : FirstSelectedElement);
        }
        // Close this menu.
        public virtual async UniTask<bool> Close()
        {
            Debug.Log("Close: " + this.name);
            Hide();
            return true;
        }


        // Shows the menu without performing the rest of its opening logic.
        public virtual void Show() => CanvasGroup.Show();
        // Hides the menu without performing the rest of its closing logic.
        public virtual void Hide() => CanvasGroup.Hide();
    }
}