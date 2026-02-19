using Gameplay.UI.Menus;
using TMPro;
using UnityEngine;

namespace Gameplay.UI.Popups
{
    /// <summary>
    ///     A simple Popup Panel to display information to players.
    /// </summary>
    public class PopupPanel : Menu
    {
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _mainText;
        [SerializeField] private GameObject _confirmButton;
        [SerializeField] private GameObject _loadingSpinner;


        private bool _isDisplaying;
        public bool IsDisplaying => _isDisplaying;

        private bool _closableByUser;
        private bool _obstructInput;


        protected override void Start() { } // Override start method to prevent hiding ourselves when being instantiated.
        public void OnConfirmClick()
        {
            if (!_closableByUser)
                return;

            // If we're obstructing input (Displaying through the MenuManager), then return through the MenuManager. Otherwise, hide normally.
            if (_obstructInput && MenuManager.CurrentMenu == this.gameObject)
                MenuManager.ReturnToPreviousMenu();
            else
                Hide();
        }


        /// <summary>
        ///     Setup and show the Popup Panel.
        /// </summary>
        public void SetupPopupPanel(string titleText, string mainText, bool closeableByUser = true, bool obstructInput = true)
        {
            _titleText.text = titleText;
            _mainText.text = mainText;
            _closableByUser = closeableByUser;
            _obstructInput = obstructInput;

            _confirmButton.SetActive(_closableByUser);
            _loadingSpinner.SetActive(!_closableByUser);

            // If we're obstructing input, achieve this by opening through the MenuManager. Otherwise, show normally.
            if (obstructInput)
                MenuManager.SetActivePopup(this);
            else
                Show();
        }


        public override void Show()
        {
            base.Show();
            _isDisplaying = true;
        }
        public override void Hide()
        {
            base.Hide();
            _isDisplaying = false;
        }
    }
}