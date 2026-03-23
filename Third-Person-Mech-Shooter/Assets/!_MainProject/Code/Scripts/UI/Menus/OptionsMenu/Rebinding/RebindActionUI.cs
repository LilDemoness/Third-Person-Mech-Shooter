using System;
using System.Collections.Generic;
using TMPro;
using UI.Icons;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UserInput;

// Adjusted from the NewInputSystem 'RebindUI' Sample.
/// <summary>
///     A reusable component with a self-contained UI for rebinding a single action.
/// </summary>
public class RebindActionUI : MonoBehaviour
{
    [Tooltip("Reference to action that is to be rebound from the UI.")]
    [SerializeField] private InputActionReference m_action; 
    /// <summary>
    ///     Reference to the action that is to be rebound.
    /// </summary>
    public InputActionReference ActionReference
    {
        get => m_action;
        set
        {
            m_action = value;
            UpdateActionLabel();
            UpdateBindingDisplay();
        }
    }

    

    [SerializeField] private string m_bindingId;
    /// <summary>
    ///     ID (in string form) of the binding that is to be rebound on the action.
    /// </summary>
    /// <seealso cref="InputBinding.id"/>
    public string BindingId
    {
        get => m_bindingId;
        set
        {
            m_bindingId = value;
            UpdateBindingDisplay();
        }
    }


    [SerializeField] private InputBinding.DisplayStringOptions m_displayStringOptions;
    public InputBinding.DisplayStringOptions DisplayStringOptions
    {
        get => m_displayStringOptions;
        set
        {
            m_displayStringOptions = value;
            UpdateBindingDisplay();
        }
    }

    [Tooltip("(Optional) Text label that will receive the name of the action.")]
    [SerializeField] private TMP_Text m_actionNameLabel;
    [SerializeField] private bool _includeBindingInName;
    /// <summary>
    ///     Text component that receives the name of the action.
    ///     <\br>Optional.
    /// </summary>
    public TMP_Text ActionNameLabel
    {
        get => m_actionNameLabel;
        set
        {
            m_actionNameLabel = value;
            UpdateActionLabel();
        }
    }


    [Tooltip("Text label that will receive the current, formatted binding string.")]
    [SerializeField] private TMP_Text m_bindingText;
    /// <summary>
    ///     Text component that receives the display string of the binding.
    ///     Can be <c>null</c>, in which case the component entirely relies on <see cref="updateBindingUIEvent"/>.
    /// </summary>
    public TMP_Text BindingText
    {
        get => m_bindingText;
        set
        {
            m_bindingText = value;
            UpdateBindingDisplay();
        }
    }


    [Tooltip("(Optional) Text label that will be updated with prompt for user input.")]
    [SerializeField] private TMP_Text m_rebindText;
    /// <summary>
    ///     Text component that receives a text prompt when waiting for a control to be actuated.
    ///     <\br>Optional.
    /// </summary>
    /// <seealso cref="startRebindEvent"/>
    /// <seealso cref="rebindOverlay"/>
    public TMP_Text RebindText
    {
        get => m_rebindText;
        set => m_rebindText = value;
    }


    [Tooltip("(Optional) UI that will be shown while a rebind is in progress.")]
    [SerializeField] private GameObject m_rebindOverlay;
    /// <summary>
    ///     UI that is activated when an interactive rebind is started and deactivated when the rebind
    ///     is finished. This is normally used to display an overlay over the current UI while the system is
    ///     waiting for a control to be actuated.
    ///     <\br>Optional.
    /// </summary>
    /// <remarks>    
    ///     If neither <see cref="rebindPrompt"/> nor <c>rebindOverlay</c> is set, the component will temporarily
    ///     replaced the <see cref="bindingText"/> (if not <c>null</c>) with <c>"Waiting..."</c>.
    /// </remarks>
    /// <seealso cref="startRebindEvent"/>
    /// <seealso cref="rebindPrompt"/>
    public GameObject RebindOverlay
    {
        get => m_rebindOverlay;
        set => m_rebindOverlay = value;
    }


    [Tooltip("Optional reference to default input actions containing the UI action map." +
        "The UI action map is disabled when rebinding is in progress.")]
    [SerializeField] private InputActionAsset _defaultInputActions;
    private InputActionMap _UIInputActionMap;


    [Tooltip("Event that is triggered when the way the binding is display should be updated." +
        "This allows displaying bindings in custom ways (E.g. Using images instead of text).")]
    [SerializeField] private UpdateBindingUIEvent m_updateBindingUIEvent;
    /// <summary>
    ///     Event that is triggered every time the UI updates to reflect the current binding.
    ///     This can be used to tie custom visualizations to bindings.
    /// </summary>
    public UpdateBindingUIEvent UpdateBindingUIEvent
    {
        get
        {
            if (m_updateBindingUIEvent == null)
                m_updateBindingUIEvent = new UpdateBindingUIEvent();
            return m_updateBindingUIEvent;
        }
    }

    [Tooltip("Event that is triggered when an interactive rebind is being initiated." +
        "This can be used, for example, to implement custom UI behavior while a rebind is in progress." +
        "It can also be used to further customize the rebind.")]
    [SerializeField] private InteractiveRebindEvent m_rebindStartEvent;
    /// <summary>
    ///     Event that is triggered when an interactive rebind is started on the action.
    /// </summary>
    public InteractiveRebindEvent StartRebindEvent
    {
        get
        {
            if (m_rebindStartEvent == null)
                m_rebindStartEvent = new InteractiveRebindEvent();
            return m_rebindStartEvent;
        }
    }

    [Tooltip("Event that is triggered when an interactive rebind is complete or has been aborted.")]
    [SerializeField] private InteractiveRebindEvent m_rebindStopEvent;
    /// <summary>
    ///     Event that is triggered when an interactive rebind has been completed or canceled.
    /// </summary>
    public InteractiveRebindEvent StopRebindEvent
    {
        get
        {
            if (m_rebindStopEvent == null)
                m_rebindStopEvent = new InteractiveRebindEvent();
            return m_rebindStopEvent;
        }
    }


    private InputActionRebindingExtensions.RebindingOperation _rebindOperation;
    /// <summary>
    /// When an interactive rebind is in progress, this is the rebind operation controller.
    /// Otherwise, it is <c>null</c>.
    /// </summary>
    public InputActionRebindingExtensions.RebindingOperation OngoingRebind => _rebindOperation;



    private static List<RebindActionUI> s_rebindActionUIs;
    public static void UpdateAllBindingDisplays()
    {
        foreach(RebindActionUI rebindActionUI in s_rebindActionUIs)
            rebindActionUI.UpdateBindingDisplay();
    }
    private static bool s_initialised = false;


#if UNITY_EDITOR

    // We want the label for the action name to update in edit mode, too, so we kick that off from here.
    protected void OnValidate()
    {
        UpdateActionLabel();
        UpdateBindingDisplay();
    }

#endif

    private void Awake()
    {
        if (s_initialised)
            return;
        s_initialised = true;
        ControlsRebindingValue.Instance.SubscribeToOnValueChanged(UpdateAllBindingDisplays);
    }
    protected void OnEnable()
    {
        if (s_rebindActionUIs == null)
            s_rebindActionUIs = new List<RebindActionUI>();
        s_rebindActionUIs.Add(this);

        if (s_rebindActionUIs.Count == 1)
            InputSystem.onActionChange += OnActionChange;

        if (ClientInput.GetInputActionsInstance() != null && _UIInputActionMap == null)
            _UIInputActionMap = ClientInput.GetInputActionsInstance().UI;

        StopRebindEvent.AddListener(UpdateSettingsString);

        InputIconManager.OnSpriteAssetChanged += InputIconManager_OnSpriteAssetChanged;
        InputIconManager_OnSpriteAssetChanged();

        UpdateBindingDisplay();
    }
    protected void OnDisable()
    {
        _rebindOperation?.Dispose();
        _rebindOperation = null;

        s_rebindActionUIs.Remove(this);
        if (s_rebindActionUIs.Count == 0)
        {
            s_rebindActionUIs = null;
            InputSystem.onActionChange -= OnActionChange;
        }

        StopRebindEvent.RemoveListener(UpdateSettingsString);
        InputIconManager.OnSpriteAssetChanged -= InputIconManager_OnSpriteAssetChanged;
    }

    private void InputIconManager_OnSpriteAssetChanged() => BindingText.spriteAsset = InputIconManager.GetSpriteAsset();
    private void UpdateSettingsString(RebindActionUI rebindActionUI, InputActionRebindingExtensions.RebindingOperation operation) => ControlsRebindingValue.Instance.UpdateValue();



    private void UpdateActionLabel()
    {
        if (ActionNameLabel != null)
        {
            if (_includeBindingInName)
            {
                if (ResolveActionAndBinding(out InputAction action, out int bindingIndex))
                {
                    string bindingName = action.bindings[bindingIndex].name;
                    string displayName = bindingName != "" && bindingName != null ? action.name + " " + bindingName.FirstCharToUpper() : action.name;
                    ActionNameLabel.text = displayName;
                }
                else
                    ActionNameLabel.text = string.Empty;
            }
            else
            {
                InputAction action = ActionReference?.action;
                ActionNameLabel.text = action != null ? action.name : string.Empty;
            }
        }
    }

    /// <summary>
    ///     Return the action and binding index for the binding that is targeted by the component.
    /// </summary>
    /// <param name="action"></param>
    /// <param name="bindingIndex"></param>
    /// <returns></returns>
    public bool ResolveActionAndBinding(out InputAction action, out int bindingIndex)
    {
        bindingIndex = -1;

        action = GetInputAction();
        if (action == null)
            return false;

        if (string.IsNullOrEmpty(BindingId))
            return false;

        // Look up binding index.
        var bindingId = new Guid(BindingId);
        bindingIndex = action.bindings.IndexOf(x => x.id == bindingId);
        if (bindingIndex == -1)
        {
            Debug.LogError($"Cannot find binding with ID '{bindingId}' on '{action}'", this);
            return false;
        }

        return true;
    }

    /// <summary>
    ///     Trigger a refresh of the currently displayed binding.
    /// </summary>
    public void UpdateBindingDisplay()
    {
        var displayString = string.Empty;
        var deviceLayoutName = default(string);
        var controlPath = default(string);

        // Get display string from action.
        InputAction action = GetInputAction();
        if (action != null)
        {
            var bindingIndex = action.bindings.IndexOf(x => x.id.ToString() == BindingId);
            if (bindingIndex != -1)
                displayString = action.GetBindingDisplayString(bindingIndex, out deviceLayoutName, out controlPath, DisplayStringOptions);
        }

        // Set on label (if any).
        if (BindingText != null)
            UpdateBindingText(displayString, controlPath);

        // Give listeners a chance to configure UI in response.
        UpdateBindingUIEvent?.Invoke(this, displayString, deviceLayoutName, controlPath);
    }
    private void UpdateBindingText(string displayString, string controlPath)
    {
        if (controlPath != default(string) && Application.isPlaying)
        {
            displayString = InputIconManager.GetIconIdentifierForAction(controlPath);
        }

        BindingText.text = displayString;
    }

    /// <summary>
    ///     Remove currently applied binding overrides.
    /// </summary>
    public void ResetToDefault()
    {
        if (!ResolveActionAndBinding(out var action, out var bindingIndex))
            return;

        if (action.bindings[bindingIndex].isComposite)
        {
            // It's a composite. Remove overrides from part bindings.
            for (var i = bindingIndex + 1; i < action.bindings.Count && action.bindings[i].isPartOfComposite; ++i)
                action.RemoveBindingOverride(i);
        }
        else
        {
            action.RemoveBindingOverride(bindingIndex);
        }

        UpdateBindingDisplay();
        ControlsRebindingValue.Instance.UpdateValue();
    }
    public void UnbindAction()
    {
        if (!ResolveActionAndBinding(out var action, out var bindingIndex))
            return;

        if (action.bindings[bindingIndex].isComposite)
        {
            var nextBindingIndex = bindingIndex + 1;
            while (nextBindingIndex < action.bindings.Count && action.bindings[nextBindingIndex].isPartOfComposite)
            {
                action.ApplyBindingOverride(nextBindingIndex, "");
            }
        }
        else
        {
            action.ApplyBindingOverride(bindingIndex, "");
        }

        UpdateBindingDisplay();
        ControlsRebindingValue.Instance.UpdateValue();
    }

    /// <summary>
    ///     Initiate an interactive rebind that lets the player actuate a control to choose a new binding
    ///     for the action.
    /// </summary>
    public void StartInteractiveRebind()
    {
        if (!ResolveActionAndBinding(out var action, out var bindingIndex))
            return;
        _rebindOperation?.Cancel(); // Will null out m_RebindOperation.


        // If the binding is a composite, we need to rebind each part in turn.
        if (action.bindings[bindingIndex].isComposite)
        {
            var firstPartIndex = bindingIndex + 1;
            if (firstPartIndex < action.bindings.Count && action.bindings[firstPartIndex].isPartOfComposite)
                PerformInteractiveRebind(action, firstPartIndex, allCompositeParts: true);
        }
        else
        {
            PerformInteractiveRebind(action, bindingIndex);
        }

        UpdateBindingDisplay();
    }

    private void PerformInteractiveRebind(InputAction action, int bindingIndex, bool allCompositeParts = false)
    {
        _rebindOperation?.Cancel(); // Will null out m_RebindOperation.

        void CleanUp()
        {
            _rebindOperation?.Dispose();
            _rebindOperation = null;

            action.actionMap.Enable();
            _UIInputActionMap?.Enable();
        }

        // An "InvalidOperationException: Cannot rebind action x while it is enabled" will
        // be thrown if rebinding is attempted on an action that is enabled.
        //
        // On top of disabling the target action while rebinding, it is recommended to
        // disable any actions (or action maps) that could interact with the rebinding UI
        // or gameplay - it would be undesirable for rebinding to cause the player
        // character to jump.
        //
        // In this example, we explicitly disable both the UI input action map and
        // the action map containing the target action.
        action.actionMap.Disable();
        _UIInputActionMap?.Disable();

        // Configure the rebind.
        _rebindOperation = action.PerformInteractiveRebinding(bindingIndex)
            .OnCancel(
                operation =>
                {
                    StopRebindEvent?.Invoke(this, operation);
                    if (RebindOverlay != null)
                        RebindOverlay.SetActive(false);
                    UpdateBindingDisplay();
                    CleanUp();
                })
            .OnComplete(
                operation =>
                {
                    if (RebindOverlay != null)
                        RebindOverlay.SetActive(false);
                    StopRebindEvent?.Invoke(this, operation);
                    UpdateBindingDisplay();
                    CleanUp();

                    // If there's more composite parts we should bind, initiate a rebind
                    // for the next part.
                    if (allCompositeParts)
                    {
                        var nextBindingIndex = bindingIndex + 1;
                        if (nextBindingIndex < action.bindings.Count && action.bindings[nextBindingIndex].isPartOfComposite)
                            PerformInteractiveRebind(action, nextBindingIndex, true);
                    }
                });

        // If it's a part binding, show the name of the part in the UI.
        var partName = default(string);
        if (action.bindings[bindingIndex].isPartOfComposite)
            partName = $"Binding '{action.bindings[bindingIndex].name}'. ";

        // Bring up rebind overlay, if we have one.
        if (RebindOverlay != null)
            RebindOverlay.SetActive(true);
        if (RebindText != null)
        {
            var text = !string.IsNullOrEmpty(_rebindOperation.expectedControlType)
                ? $"{partName}Waiting for {_rebindOperation.expectedControlType} input..."
                : $"{partName}Waiting for input...";
            RebindText.text = text;
        }

        // If we have no rebind overlay and no callback but we have a binding text label,
        // temporarily set the binding text label to "<Waiting>".
        if (RebindOverlay == null && RebindText == null && StartRebindEvent == null && BindingText != null)
            BindingText.text = "<Waiting...>";

        // Give listeners a chance to act on the rebind starting.
        StartRebindEvent?.Invoke(this, _rebindOperation);

        _rebindOperation.Start();
    }

    // When the action system re-resolves bindings, we want to update our UI in response. While this will
    // also trigger from changes we made ourselves, it ensures that we react to changes made elsewhere. If
    // the user changes keyboard layout, for example, we will get a BoundControlsChanged notification and
    // will update our UI to reflect the current keyboard layout.
    private static void OnActionChange(object obj, InputActionChange change)
    {
        if (change != InputActionChange.BoundControlsChanged)
            return;

        var action = obj as InputAction;
        var actionMap = action?.actionMap ?? obj as InputActionMap;
        var actionAsset = actionMap?.asset ?? obj as InputActionAsset;

        for (var i = 0; i < s_rebindActionUIs.Count; ++i)
        {
            var component = s_rebindActionUIs[i];
            var referencedAction = component.ActionReference?.action;
            if (referencedAction == null)
                continue;

            if (referencedAction == action ||
                referencedAction.actionMap == actionMap ||
                referencedAction.actionMap?.asset == actionAsset)
                component.UpdateBindingDisplay();
        }
    }


    private InputAction GetInputAction()
    {
        if (ActionReference == null)
            return null;

        if (ClientInput.GetInputActionsInstance() != null)
            return ClientInput.GetInputActionsInstance().FindAction(ActionReference.action.id.ToString(), true);
        else
            return ActionReference.action;
    }
}
[System.Serializable]
public class UpdateBindingUIEvent : UnityEvent<RebindActionUI, string, string, string>
{ }
[System.Serializable]
public class InteractiveRebindEvent : UnityEvent<RebindActionUI, InputActionRebindingExtensions.RebindingOperation>
{ }