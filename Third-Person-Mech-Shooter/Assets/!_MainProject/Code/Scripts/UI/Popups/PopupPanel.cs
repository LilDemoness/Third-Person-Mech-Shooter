using Gameplay.UI.Menus;
using System.Collections.Generic;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI.Popups
{
    /// <summary>
    ///     A simple Popup Panel to display information to players.
    /// </summary>
    public class PopupPanel : Popup
    {
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _mainText;

        [SerializeField] private Button _buttonPrefab;
        [SerializeField] private NonNavigableButton _nonNavigableButtonPrefab;
        [SerializeField] private Transform _buttonContainer;

        [SerializeField] private GameObject _loadingSpinner;


        private bool _isDisplaying;
        public bool IsDisplaying => _isDisplaying;

        private bool _closableByUser;
        private bool _obstructInput;


        public void OnConfirmClick()
        {
            if (!_closableByUser)
                return;

            // If we're NOT obstructing input (Displaying through the MenuManager), OR are the active popup, then close ourselves. Otherwise, we've failed to close.
            if (!_obstructInput || this.IsActivePopup())
                Close();
            else
                Debug.Log("Failed to Close");
        }


        /// <summary>
        ///     Setup and show the Popup Panel.
        /// </summary>
        public void SetupPopupPanel(string titleText, string mainText, bool closeableByUser = true, bool obstructInput = true, params PopupButtonParameters[] popupButtonParameters)
        {
            _titleText.text = titleText;
            _mainText.text = mainText;
            _closableByUser = closeableByUser;
            _obstructInput = obstructInput;

            if (closeableByUser)
            {
                List<PopupButtonParameters> popupParams = new List<PopupButtonParameters>(popupButtonParameters);
                popupParams.Insert(0, new PopupButtonParameters("Close", Close));
                SetupPopupButtons(popupParams.ToArray());
            }
            else
                SetupPopupButtons(popupButtonParameters);

            if (_loadingSpinner)
                _loadingSpinner.SetActive(!_closableByUser);

            // If we're obstructing input, achieve this by opening through the MenuManager. Otherwise, show normally.
            if (obstructInput)
                MenuManager.LinkPopup(this);
            
            Open();
        }
        public void SetupPopupButtons(params PopupButtonParameters[] popupButtonParameters)
        {
            if (popupButtonParameters == null)
                return;
            
            foreach (PopupButtonParameters parameter in popupButtonParameters)
            {
                Debug.Log($"Param Text: " + parameter.ButtonText);
                CreateButton(parameter);
            }

            SetupButtonNavigation();
        }


        private void CreateButton(PopupButtonParameters setupParameters)
        {
            if (setupParameters.TriggerButtonInput != null)
                CreateNonNavigableButton(setupParameters);
            else
                CreateNavigableButton(setupParameters);
        }
        private Button CreateNavigableButton(PopupButtonParameters setupParameters)
        {
            // Create and setup the button.
            Button button = Instantiate<Button>(_buttonPrefab, _buttonContainer);
            button.GetComponentInChildren<TMP_Text>().text = setupParameters.ButtonText;
            button.onClick.AddListener(CompleteCallback);

            // Return the newly created button.
            return button;


            void CompleteCallback()
            {
                Close();
                setupParameters.OnPressedCallback?.Invoke();
            }
        }
        private NonNavigableButton CreateNonNavigableButton(PopupButtonParameters setupParameters)
        {
            // Create and setup the button.
            NonNavigableButton button = Instantiate<NonNavigableButton>(_nonNavigableButtonPrefab, _buttonContainer);
            button.GetComponentInChildren<TMP_Text>().text = setupParameters.ButtonText;
            button.OnButtonTriggered += CompleteCallback;

            // Return the newly created button.
            return button;


            void CompleteCallback()
            {
                Close();
                setupParameters.OnPressedCallback?.Invoke();
            }
        }
        private void SetupButtonNavigation()
        {
            Debug.Log("Double check that this returns children in order.");
            Selectable[] selectableChildren = _buttonContainer.GetComponentsInChildren<Selectable>();
            int finalSelectableIndex = selectableChildren.Length - 1;

            // No selectables under our button container.
            if (finalSelectableIndex == -1)
                return;
            // Only one selectable, so no navigation is needed.
            else if (finalSelectableIndex == 0)
            {
                selectableChildren[0].RemoveNavigation();
                return;
            }

            // Multiple selectables. Setup their horizontal navigation.
            for (int i = 0; i <= finalSelectableIndex; ++i)
            {
                if (i == 0)
                    selectableChildren[i].SetNavigation(onLeft: selectableChildren[finalSelectableIndex], onRight: selectableChildren[i + 1]);
                else if (i == finalSelectableIndex)
                    selectableChildren[i].SetNavigation(onLeft: selectableChildren[i - 1], onRight: selectableChildren[0]);
                else
                    selectableChildren[i].SetNavigation(onLeft: selectableChildren[i - 1], onRight: selectableChildren[i + 1]);
            }

            // Select the first button.
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(selectableChildren[0].gameObject);
        }
        private void CleanupButtons()
        {
            if (!_buttonContainer)
                return;

            for (int i = _buttonContainer.childCount - 1; i >= 0; --i)
            {
                Destroy(_buttonContainer.GetChild(i).gameObject);
            }
        }


        public override void Open()
        {
            _isDisplaying = true;
            base.Open();
        }
        public override void Close()
        {
            _isDisplaying = false;
            base.Close();

            CleanupButtons();
        }
    }


    [System.Serializable]
    public readonly struct PopupButtonParameters
    {
        public readonly string ButtonText;
        public readonly System.Action OnPressedCallback;
        public readonly UnityEngine.InputSystem.InputAction TriggerButtonInput;



        private PopupButtonParameters(PopupButtonParameters other)
        {
            throw new System.Exception("Copy Constructor used");
        }

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