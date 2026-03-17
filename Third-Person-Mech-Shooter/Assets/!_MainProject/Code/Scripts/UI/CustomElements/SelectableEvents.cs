using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
///     Provides events for when a Selectable is selected or deselected.
/// </summary>
public class SelectableEvents : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    public bool IsSelected { get; private set; }

    public event System.Action OnSelected;
    public event System.Action OnDeselected;


    public virtual void OnPointerEnter(PointerEventData eventData) { }
    public virtual void OnPointerExit(PointerEventData eventData) { }

    public virtual void OnSelect(BaseEventData eventData)
    {
        IsSelected = true;
        OnSelected?.Invoke();
    }
    public virtual void OnDeselect(BaseEventData eventData)
    {
        IsSelected = false;
        OnDeselected?.Invoke();
    }
}
