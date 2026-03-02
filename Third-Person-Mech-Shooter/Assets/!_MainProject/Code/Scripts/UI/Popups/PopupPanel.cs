using Gameplay.UI.Menus;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI.Popups
{
    /// <summary>
    ///     A simple Popup Panel to display information to players.
    /// </summary>
    public class PopupPanel : Menu
    {
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _mainText;

        [SerializeField] private Button _buttonPrefab;
        [SerializeField] private Transform _buttonContainer;

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

            if (closeableByUser)
                CreateButton(new PopupButtonParameters("Close", Hide));
            _loadingSpinner.SetActive(!_closableByUser);

            SetupButtonNavigation();

            // If we're obstructing input, achieve this by opening through the MenuManager. Otherwise, show normally.
            if (obstructInput)
                MenuManager.SetActivePopup(this);
            else
                Show();
        }


        private Button CreateButton(PopupButtonParameters setupParameters)
        {
            // Create and setup the button.
            Button button = Instantiate<Button>(_buttonPrefab, _buttonPrefab.transform.parent);
            button.GetComponentInChildren<TMP_Text>().text = setupParameters.ButtonText;
            button.onClick.AddListener(setupParameters.OnPressedCallback.Invoke);

            // Return the newly created button.
            return button;
        }
        private void SetupButtonNavigation()
        {
            Debug.Log("Double check that this returns children in order.");
            Selectable[] selectableChildren = _buttonContainer.GetComponentsInChildren<Selectable>();
            int finalSelectableIndex = selectableChildren.Length - 1;
            for (int i = 0; i <= finalSelectableIndex; ++i)
            {
                if (i == 0)
                    selectableChildren[i].SetNavigation(onLeft: selectableChildren[finalSelectableIndex], onRight: selectableChildren[i + 1]);
                else if (i == finalSelectableIndex)
                    selectableChildren[i].SetNavigation(onLeft: selectableChildren[i - 1], onRight: selectableChildren[0]);
                else
                    selectableChildren[i].SetNavigation(onLeft: selectableChildren[i - 1], onRight: selectableChildren[i + 1]);
            }
        }
        private void CleanupButtons()
        {
            for (int i = _buttonContainer.childCount - 1; i >= 0; --i)
            {
                Destroy(_buttonContainer.GetChild(i));
            }
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

            CleanupButtons();
        }
    }


    public readonly struct PopupButtonParameters
    {
        public readonly string ButtonText;
        public readonly System.Action OnPressedCallback;
        public readonly UnityEngine.InputSystem.InputAction TriggerButtonInput;


        public PopupButtonParameters(string buttonText, System.Action onPressedCallback) : this(buttonText, onPressedCallback, null)
        { }
        public PopupButtonParameters(string buttonText, System.Action onPressedCallback, UnityEngine.InputSystem.InputAction triggerButtonInput)
        {
            this.ButtonText = buttonText;
            this.OnPressedCallback = onPressedCallback;
            this.TriggerButtonInput = triggerButtonInput;
        }
    }
}