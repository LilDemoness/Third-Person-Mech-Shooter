using Gameplay.UI.Menus;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
///     Forces a hovered selectable to be selected.
/// </summary>
public class SelectableAdditions : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] private Selectable _selectable;
    private static GameObject s_lastSelectedObject;


    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_selectable.interactable && _selectable.IsInActiveMenu(false) && !IsSelectingActiveInputDevice())
            EventSystem.current.SetSelectedGameObject(_selectable.gameObject);
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        //if (EventSystem.current.currentSelectedGameObject == this.gameObject)
        //    EventSystem.current.SetSelectedGameObject(s_lastSelectedObject);
    }

    private bool IsSelectingActiveInputDevice() => EventSystem.current.currentSelectedGameObject != null && EventSystem.current.currentSelectedGameObject.TryGetComponent(out TMPro.TMP_InputField inputField) && inputField.isFocused;

    public void OnSelect(BaseEventData eventData) { }// => s_lastSelectedObject = _selectable.gameObject;
    public void OnDeselect(BaseEventData eventData)
    {
        //if (s_lastSelectedObject == _selectable.gameObject)
        //    s_lastSelectedObject = null;
    }


#if UNITY_EDITOR

    private void Reset()
    {
        _selectable = this.GetComponentInChildren<Selectable>();
    }

#endif
}