using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gameplay.UI.Menus
{
    /// <summary>
    ///     Base class for any Menu: UI elements that can be opened and closed where one takes input priority over others.
    /// </summary>
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


        /// <summary>
        ///     Open this menu.
        /// </summary>
        /// <param name="selectFirstElement">
        ///     If true, then we select our <see cref="FirstSelectedElement"/> when opened, if there is one.<br/>
        ///     Otherwise, the selected GameObject is untouched.
        /// </param>
        public virtual void Open(bool selectFirstElement = true)
        {
            Show();

            if (selectFirstElement)
                EventSystem.current.SetSelectedGameObject(FirstSelectedElement);
        }
        /// <summary>
        ///     Re-opens this menu.<br/>
        ///     Should be called from the MenuManager when returning to this menu from a menu open above it (Created more recently).
        /// </summary>
        /// <param name="targetSelectable"> The Selectable that we should set as the selected gameobject. If null, defaults to <see cref="FirstSelectedElement"/></param>
        public virtual void Reopen(Selectable targetSelectable = null)
        {
            Show();
            EventSystem.current.SetSelectedGameObject(targetSelectable != null ? targetSelectable.gameObject : FirstSelectedElement);
        }
#pragma warning disable CS1998 // The base implementation of this method is synchronus, but children may not be. Suppress the warning: "Async method lacks 'await' operators and will run synchronously".
        /// <summary>
        ///     Close this menu.
        /// </summary>
        public virtual async UniTask<bool> Close()
#pragma warning restore CS1998 
        {
            Hide();
            return true;
        }


        /// <summary>
        ///     Shows the menu without performing the rest of its opening logic.
        /// </summary>
        public virtual void Show() => CanvasGroup.Show();
        /// <summary>
        ///     Hides the menu without performing the rest of its closing logic.
        /// </summary>
        public virtual void Hide() => CanvasGroup.Hide();



        [ContextMenu("Debug/Log Open Menus")]
        private void DebugLog() => MenuManager.LogOpenMenus();

        protected void SetFirstSelectedElement(Selectable selectable) => _firstSelectedElement = selectable;
    }
}