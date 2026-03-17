using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gameplay.UI
{
    public class SelectMultiGraphicHighlighter : MonoBehaviour
    {
        [SerializeField] private SelectableEvents[] _selectionNotifiers;
        [SerializeField] private Graphic[] _graphics;


        [SerializeField] private Color _normalColor = new Color(1.0f, 1.0f, 1.0f);
        [SerializeField] private Color _highlightedColor = new Color(0.8f, 0.8f, 0.8f);
        [SerializeField] private Color _selectedColor = new Color(0.8f, 0.8f, 0.8f);
        [SerializeField] private Color _disabledColor = new Color(0.8f, 0.8f, 0.8f, 0.5f);



        private void Awake()
        {
            foreach (SelectableEvents notifier in _selectionNotifiers)
            {
                notifier.OnSelected += OnSelect;
                notifier.OnDeselected += OnDeselect;
            }
        }
        private void OnDestroy()
        {
            foreach (SelectableEvents notifier in _selectionNotifiers)
            {
                notifier.OnSelected -= OnSelect;
                notifier.OnDeselected -= OnDeselect;
            }
        }

        private void OnSelect() => SetColor(_highlightedColor);
        private void OnDeselect() => SetColor(_normalColor);
        private void OnHighlighted() => SetColor(_highlightedColor);
        private void OnDisabled() => SetColor(_disabledColor);

        private void SetColor(Color color)
        {
            foreach(Graphic graphic in _graphics)
            {
                graphic.CrossFadeColor(color, 0.0f, true, true);
            }
        }
    }
}