using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gameplay.UI
{
    public class SelectMultiGraphicHighlighter : MonoBehaviour
    {
        [SerializeField] private SelectionNotifier[] _selectionNotifiers;
        [SerializeField] private Graphic[] _graphics;


        [SerializeField] private Color _normalColor;
        [SerializeField] private Color _highlightedColor;
        [SerializeField] private Color _selectedColor;
        [SerializeField] private Color _disabledColor;



        private void Awake()
        {
            foreach (SelectionNotifier notifier in _selectionNotifiers)
            {
                notifier.OnSelected += OnSelect;
                notifier.OnDeselected += OnDeselect;
            }
        }
        private void OnDestroy()
        {
            foreach (SelectionNotifier notifier in _selectionNotifiers)
            {
                notifier.OnSelected -= OnSelect;
                notifier.OnDeselected -= OnDeselect;
            }
        }

        private void OnSelect(BaseEventData _) => SetColor(_highlightedColor);
        private void OnDeselect(BaseEventData _) => SetColor(_normalColor);
        private void OnHighlighted(BaseEventData _) => SetColor(_highlightedColor);
        private void OnDisabled(BaseEventData _) => SetColor(_disabledColor);

        private void SetColor(Color color)
        {
            foreach(Graphic graphic in _graphics)
            {
                graphic.CrossFadeColor(color, 0.0f, true, true);
            }
        }
    }
}