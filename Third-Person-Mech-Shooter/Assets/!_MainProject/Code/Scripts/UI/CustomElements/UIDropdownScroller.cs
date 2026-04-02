using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// From: 'https://www.youtube.com/watch?v=P8hx343kIGg'
public class UIDropdownScroller : MonoBehaviour, ISelectHandler
{
    private ScrollRect _scrollRect;
    [SerializeField] private float _spacing = 25.0f;


    private void Awake()
    {
        _scrollRect = GetComponentInParent<ScrollRect>(includeInactive: true);
        foreach (SelectableEvents selectionNotifier in GetComponentsInChildren<SelectableEvents>())
            selectionNotifier.OnSelected += OnSelect;
    }
    private void OnDestroy()
    {
        foreach (SelectableEvents selectionNotifier in GetComponentsInChildren<SelectableEvents>())
            selectionNotifier.OnSelected -= OnSelect;
    }


    public void OnSelect(BaseEventData _) => OnSelect();
    public void OnSelect()
    {
        if (!_scrollRect) return; // Prevents an error when this object is first loaded.

        // Change to: 'https://discussions.unity.com/t/scrollview-scroll-position/659577/5'
        _scrollRect.ScrollTo(this.transform, _spacing);
    }
}