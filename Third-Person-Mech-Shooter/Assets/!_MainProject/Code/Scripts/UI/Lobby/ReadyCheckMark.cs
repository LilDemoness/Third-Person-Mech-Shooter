using UnityEngine;
using TMPro;

namespace UI.Lobby
{
    public class ReadyCheckMark : MonoBehaviour
    {
        [SerializeField] private GameObject _toggleGO;
        [SerializeField] private TMP_Text _playerNameText;

        private const int DEFAULT_LINES = 2;
        private const float SINGLE_LINE_HEIGHT = 27.5f;

        public void SetToggleText(string newText, bool includeYouText)
        {
            (_playerNameText.transform as RectTransform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, SINGLE_LINE_HEIGHT * (includeYouText ? DEFAULT_LINES + 1 : DEFAULT_LINES));
            _playerNameText.text = includeYouText ? $"(You)\n{newText}" : newText;
        }
        public void SetToggleVisibility(bool showToggle) => _toggleGO.SetActive(showToggle);
    }
}