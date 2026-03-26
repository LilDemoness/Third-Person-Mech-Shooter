using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// From: 'https://www.youtube.com/watch?v=P8hx343kIGg'
public class UIDropdownScroller : MonoBehaviour, ISelectHandler
{
    private ScrollRect _scrollRect;
    private float _scrollPosition;


    private void Awake()
    {
        foreach(SelectableEvents selectionNotifier in GetComponentsInChildren<SelectableEvents>())
            selectionNotifier.OnSelected += OnSelect;
    }
    private void OnDestroy()
    {
        foreach (SelectableEvents selectionNotifier in GetComponentsInChildren<SelectableEvents>())
            selectionNotifier.OnSelected -= OnSelect;
    }
    private void Start()
    {
        _scrollRect = GetComponentInParent<ScrollRect>(includeInactive: true);

        int actualChildCount = _scrollRect.content.childCount - 1;
        //int visibleChildCount = Mathf.FloorToInt(_scrollRect.GetComponent<RectTransform>().sizeDelta.y / GetComponent<RectTransform>().sizeDelta.y);

        int childIndex = transform.GetSiblingIndex();
        childIndex = (childIndex <= 3) ? childIndex - 1 : childIndex;  // Adjusts child index to make the scroll bar look better.
        //childIndex = Mathf.Max(childIndex - visibleChildCount, 0);

        _scrollPosition = 1 - ((float)childIndex / (float)actualChildCount);
    }


    public void OnSelect(BaseEventData _) => OnSelect();
    public void OnSelect()
    {
        if (!_scrollRect) return; // Prevents an error when this object is first loaded.
        Debug.Log(_scrollPosition, this);
        _scrollRect.verticalScrollbar.value = _scrollPosition;
    }
}