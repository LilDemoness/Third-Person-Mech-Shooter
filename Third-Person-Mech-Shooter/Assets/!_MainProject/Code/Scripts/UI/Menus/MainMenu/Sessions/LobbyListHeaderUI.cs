using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI.Menus.Session
{
    public class LobbyListHeaderUI : MonoBehaviour
    {
        [SerializeField] private SessionSortField _sortField;
        public SessionSortField SortField => _sortField;
        private int _headerIndex;


        [Header("Display")]
        [SerializeField] private Image _sortOrderImage;

        [Space(5)]
        [SerializeField] private Sprite _ascendingOrderSprite;
        [SerializeField] private Sprite _descendingOrderSprite;
        [SerializeField] private Color _activeColor = Color.white;

        [Space(5)]
        [SerializeField] private Sprite _inactiveSprite;
        [SerializeField] private Color _inactiveColor = Color.grey;


        public static event System.Action<int> OnAnyHeaderSelected;

        public void Select() => OnAnyHeaderSelected?.Invoke(_headerIndex);


        public void SetHeaderIndex(int headerIndex) => _headerIndex = headerIndex;
        public void OnSortFiltersChanged(SessionSortField sortField, SessionSortOrder sortOrder)
        {
            if (sortField != this._sortField)
            {
                Deselect();
            }
            else
            {
                if (sortOrder == SessionSortOrder.Ascending)
                    SetSortOrderAscending();
                else
                    SetSortOrderDescending();
            }
        }



        public void SetSortOrderAscending() => SetSpriteAndColour(_ascendingOrderSprite, _activeColor);
        public void SetSortOrderDescending() => SetSpriteAndColour(_descendingOrderSprite, _activeColor);
        public void Deselect() => SetSpriteAndColour(_inactiveSprite, _inactiveColor);


        private void SetSpriteAndColour(Sprite sprite, Color color)
        {
            _sortOrderImage.sprite = sprite;
            _sortOrderImage.color = color;
        }
    }
}