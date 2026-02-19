using Gameplay.UI.Menus;
using TMPro;
using UI.Icons;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    ///     A button that can be pressed by a mouse and triggered via a PlayerInputAction, but that cannot be selected via navigation
    /// </summary>
    public class NonNavigableButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [System.Serializable]
        private enum InputActionType
        {
            /// <summary> Trigger the action without a check.</summary>
            Press,

            /// <summary> Trigger the action if there is any horizontal input.</summary>
            Horizonal,
            /// <summary> Trigger the action if there is positive horizontal input.</summary>
            HorizonalPositive,
            /// <summary> Trigger the action if there is negative horizontal input.</summary>
            HorizonalNegative,

            /// <summary> Trigger the action if there is any vertical input.</summary>
            Vertical,
            /// <summary> Trigger the action if there is positive vertical input.</summary>
            VerticalPositive,
            /// <summary> Trigger the action if there is negative vertical input.</summary>
            VerticalNegative,
        }

        private static NonNavigableButton s_currentHighlightedButton;
        static NonNavigableButton()
        {
            MenuManager.OnActiveMenuChanged -= OnActiveMenuChanged;
            MenuManager.OnActiveMenuChanged += OnActiveMenuChanged;
        }
        private static void OnActiveMenuChanged() => s_currentHighlightedButton?.OnPointerExit(null);


        [SerializeField] private bool _isInteractable = true;
        public bool IsInteractable
        {
            get => _isInteractable;
            set
            {
                _isInteractable = true;
                _graphic.CrossFadeColor(_isInteractable ? _normalColor : _disabledColor, 0.0f, true, true);
            }
        }


        [Space(10)]
        [Tooltip("(Optional) The input action that this button will be triggered by.")]
        [SerializeField] private InputActionReference _inputAction;
        [Tooltip("The type of check that will be run when the input action is performed.")]
        [SerializeField] private InputActionType _inputActionType;
        [SerializeField] private bool _allowInputWhenNotInFocus = false;

        [Space(10)]
        [SerializeField] private UnityEvent _onButtonTriggered;


        [Header("Hover Iteraction Indication")]
        [SerializeField] private Graphic _graphic;
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _highlightedColor = new Color(0.8f, 0.8f, 0.8f);
        [SerializeField] private Color _pressedColor = new Color(0.6f, 0.6f, 0.6f);
        [SerializeField] private Color _disabledColor = new Color(0.8f, 0.8f, 0.8f, 0.5f);


        [Header("Text Updating")]
        [SerializeField] private TMP_Text _iconDisplayText;
        [SerializeField][TextArea] private string _textFormattingString = "{0}";


        [Header("Text Updating")]
        [SerializeField] private Image _iconDisplayImage;


        private void Awake()
        {
            if (_inputAction != null)
            {
                InputIconManager.OnShouldUpdateSpriteIdentifiers += UpdateSpriteIdentifiers;
                InputIconManager.OnSpriteAssetChanged += UpdateIconDisplayText;

                UpdateSpriteIdentifiers();
            }

            _graphic.CrossFadeColor(_isInteractable ? _normalColor : _disabledColor, 0.0f, true, true);
        }
        private void OnEnable()
        {
            if (_inputAction != null)
            {
                _inputAction.action.Enable();
                _inputAction.action.performed += Action_performed;
            }
        }
        private void OnDisable()
        {
            if (_inputAction != null)
                _inputAction.action.performed -= Action_performed;
            if (s_currentHighlightedButton == this)
                s_currentHighlightedButton = null;
        }
        private void OnDestroy()
        {
            InputIconManager.OnShouldUpdateSpriteIdentifiers -= UpdateSpriteIdentifiers;
            InputIconManager.OnSpriteAssetChanged -= UpdateIconDisplayText;
        }


        private void Action_performed(InputAction.CallbackContext ctx)
        {
            if (!_isInteractable)
                return; // The action is not interractable.

            if (!CanUseInput_Focus())
                return; // We are not in focus and aren't allowing out-of-focus input.

            if (!IsValidActionInput(ref ctx))
                return; // Invalid input for the action trigger type.

            // Valid input. Trigger our callback.
            _onButtonTriggered?.Invoke();
        }

        private bool _isPressed = false;
        private bool _isHighlighted = false;
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_isInteractable)
                return; // The button is not interractable.
            if (!CanUseInput_Focus())
                return;

            _onButtonTriggered?.Invoke();
        }
        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_isInteractable)
                return; // The button is not interractable.
            if (!CanUseInput_Focus())
                return;

            _isPressed = true;
            UpdateSelectionIndicator();
        }
        public void OnPointerUp(PointerEventData eventData)
        {
            _isPressed = false;
            UpdateSelectionIndicator();
        }
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_isInteractable)
                return; // The button is not interractable.
            if (!CanUseInput_Focus())
                return;

            _isHighlighted = true;
            UpdateSelectionIndicator();
            s_currentHighlightedButton = this;
        }
        public void OnPointerExit(PointerEventData eventData)
        {
            /*if (!_isInteractable)
                return; // The button is not interractable.
            if (!CanUseInput_Focus())
                return;*/

            _isHighlighted = false;
            UpdateSelectionIndicator();

            if (s_currentHighlightedButton == this)
                s_currentHighlightedButton = null;
        }

        private void UpdateSelectionIndicator()
        {
            if (_isPressed)
                _graphic.CrossFadeColor(_pressedColor, 0.0f, true, true);
            else if (_isHighlighted)
                _graphic.CrossFadeColor(_highlightedColor, 0.0f, true, true);
            else
                _graphic.CrossFadeColor(_normalColor, 0.0f, true, true);
        }


        /// <summary>
        ///     Returns true if this NonNavigableButton isn't being prevented from providing input from focus state.
        /// </summary>
        private bool CanUseInput_Focus() => _allowInputWhenNotInFocus || this.IsInActiveMenu();


        private bool IsValidActionInput(ref InputAction.CallbackContext ctx) => _inputActionType switch
        {
            InputActionType.Press => true,  // No required checks.

            // Horizontal.
            InputActionType.Horizonal => !Mathf.Approximately(ctx.ReadValue<Vector2>().x, 0.0f),// True if there is any horizontal input.
            InputActionType.HorizonalPositive => ctx.ReadValue<Vector2>().x > 0.0f,             // True for positive horizontal input.
            InputActionType.HorizonalNegative => ctx.ReadValue<Vector2>().x < 0.0f,             // True for negative horizontal input.
            // Vertical
            InputActionType.Vertical => !Mathf.Approximately(ctx.ReadValue<Vector2>().y, 0.0f), // True if there is any vertical input.
            InputActionType.VerticalPositive => ctx.ReadValue<Vector2>().y > 0.0f,              // True for positive vertical input.
            InputActionType.VerticalNegative => ctx.ReadValue<Vector2>().y < 0.0f,              // True for negative vertical input.

            _ => throw new System.NotImplementedException()
        };


        private void UpdateSpriteIdentifiers()
        {
            if (_iconDisplayText)
                UpdateIconDisplayText();
            if (_iconDisplayImage)
                UpdateIconDisplayImage();
        }
        private void UpdateIconDisplayText()
        {
            _iconDisplayText.text = InputIconManager.FormatTextForIconFromInputAction(_textFormattingString, _inputAction);
            _iconDisplayText.spriteAsset = InputIconManager.GetSpriteAsset();
        }
        private void UpdateIconDisplayImage() => _iconDisplayImage.sprite = InputIconManager.GetIconForAction(_inputAction);


#if UNITY_EDITOR

        private void OnValidate()
        {
            if (_graphic == null)
                Debug.LogWarning($"NonNavigableButton '{this.gameObject.name}' has no graphic set", this.gameObject);

            if (_iconDisplayText != null)
            {
                if (!Editor_IsFormattingStringValid())
                    Debug.LogWarning("NonNavigableButton has an invalid entry string", this.gameObject);
            }
        }

        private bool Editor_IsFormattingStringValid()
        {
            return _textFormattingString.Contains("{0}");
        }

#endif
    }
}