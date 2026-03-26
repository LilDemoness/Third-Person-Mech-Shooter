using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UserInput;

public class MultiActionButton : Selectable, IPointerEnterHandler, IPointerExitHandler
{
    public ActionToEventPair[] HoverEvents;
    public ActionToEventPair[] SelectedEvents;

    private bool _isHovered;


    // Note: Requirement Func setting doesn't work if this is a struct, so it is a class.
    [System.Serializable]
    public class ActionToEventPair
    {
        [field:SerializeField] public InputActionReference InputAction { get; private set; }
        [field:SerializeField] public UnityEvent Event { get; private set; }
        

        private System.Func<bool> _requirement;
        public void SetRequirement(System.Func<bool> requirement) => _requirement = requirement;


        public void OnActionPerformed(InputAction.CallbackContext ctx)
        {
            if (_requirement != null && _requirement())
                Event.Invoke();
        }
    }


    protected override void Awake()
    {
        base.Awake();

        if (Application.isPlaying)
            SubscribeToEvents();
    }
    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (Application.isPlaying)
            UnsubscribeFromEvents();
    }
    private void SubscribeToEvents()
    {
        for(int i = 0; i < HoverEvents.Length; ++i)
        {
            ClientInput.GetReferenceForAction(HoverEvents[i].InputAction).performed += HoverEvents[i].OnActionPerformed;
            HoverEvents[i].SetRequirement(HoverRequirement);
        }

        for (int i = 0; i < SelectedEvents.Length; ++i)
        {
            ClientInput.GetReferenceForAction(SelectedEvents[i].InputAction).performed += SelectedEvents[i].OnActionPerformed;
            SelectedEvents[i].SetRequirement(SelectionRequirement);
        }
    }
    private void UnsubscribeFromEvents()
    {
        for (int i = 0; i < HoverEvents.Length; ++i)
        {
            if (ClientInput.HasInputActions)
                ClientInput.GetReferenceForAction(HoverEvents[i].InputAction).performed -= HoverEvents[i].OnActionPerformed;

            HoverEvents[i].SetRequirement(null);
        }
        for (int i = 0; i < SelectedEvents.Length; ++i)
        {
            if (ClientInput.HasInputActions)
                ClientInput.GetReferenceForAction(SelectedEvents[i].InputAction).performed -= SelectedEvents[i].OnActionPerformed;

            SelectedEvents[i].SetRequirement(null);
        }
    }


    public override void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true;
        base.OnPointerEnter(eventData);
    }
    public override void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
        base.OnPointerExit(eventData);
    }


    private bool HoverRequirement() => _isHovered;
    private bool SelectionRequirement() => EventSystem.current.currentSelectedGameObject == this.gameObject;
}