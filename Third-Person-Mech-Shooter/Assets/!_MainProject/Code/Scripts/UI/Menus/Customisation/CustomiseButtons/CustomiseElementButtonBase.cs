using UnityEngine;
using UnityEngine.UI;
using Gameplay.GameplayObjects.Character.Customisation.Data;

namespace Gameplay.UI.Menus.Customisation
{
    [RequireComponent(typeof(SelectableEvents), typeof(Button))]
    public abstract class CustomiseElementButtonBase<T> : MonoBehaviour where T : BaseCustomisationData
    {
        public T CurrentData { get; private set; }

        public event System.Action<CustomiseElementButtonBase<T>> OnSelected;
        public event System.Action<CustomiseElementButtonBase<T>> OnClicked;


        private void Awake()
        {
            GetComponent<SelectableEvents>().OnSelected += SelectableEvents_OnSelected;
            GetComponent<Button>().onClick.AddListener(Button_OnClicked);
        }
        private void OnDestroy()
        {
            GetComponent<SelectableEvents>().OnSelected -= SelectableEvents_OnSelected;
            GetComponent<Button>().onClick.RemoveListener(Button_OnClicked);
        }

        private void SelectableEvents_OnSelected() => OnSelected?.Invoke(this);
        private void Button_OnClicked() => OnClicked?.Invoke(this);


        public virtual void SetCurrentData(T customisationData)
        {
            this.CurrentData = customisationData;
        }
    }
}