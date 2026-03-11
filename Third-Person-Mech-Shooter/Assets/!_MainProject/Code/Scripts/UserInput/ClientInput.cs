using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

namespace UserInput
{
    /// <summary>
    ///     Script handling the creation of a <see cref="PlayerInputActions"/> action map & relaying it's button presses through events.
    /// </summary>
    public class ClientInput : MonoBehaviour
    {
        // Prevent Multiple Instances
        private static ClientInput s_instance;
        public static bool HasInputActions => s_inputActions != null;


        public static Vector2 MovementInput { get; private set; }
        public static event System.Action OnMovementInputChanged;
        private static Vector2 s_previousMovementInput;

        public static Vector2 LookInput { get; private set; }


        #region Combat Events

        public static event System.Action<int> OnActivateSlotStarted;
        public static event System.Action<int> OnActivateSlotCancelled;

        #endregion

        #region UI Events


        public static event System.Action<Vector2> OnNavigatePerformed;
        
        // Tabs.
        public static event System.Action OnNextTabPerformed;
        public static event System.Action OnPreviousTabPerformed;

        // Confirmation.
        public static event System.Action OnConfirmPerformed;

        // Customisation UI.
        public static event System.Action OnOpenFrameSelectionPerformed;

        // Other.
        public static event System.Action OnToggleLeaderboardPerformed;

        #endregion

        #region Multiplayer Chat Events

        public static event System.Action OnOpenChatPerformed;
        public static event System.Action OnSubmitChatPerformed;
        public static event System.Action OnCancelChatPerformed;

        #endregion

        #region Game State Events

        public static event System.Action OnPauseGamePerformed;

        #endregion


        #region Input Device Type

        private const string MOUSE_AND_KEYBOARD_CONTROL_SCHEME_NAME = "MnK";
        private const string GAMEPAD_CONTROL_SCHEME_NAME = "Gamepad";

        private static InputControlScheme s_currentControlScheme = default;
        public static InputControl CurrentInputDevice {  get; private set; }
        public static event Action OnInputDeviceChanged;

        public enum DeviceType { MnK, Gamepad }
        public static DeviceType LastUsedDevice => s_currentControlScheme.name == MOUSE_AND_KEYBOARD_CONTROL_SCHEME_NAME ? DeviceType.MnK : DeviceType.Gamepad;
        public static string LastUsedDeviceName => s_currentControlScheme.name;

        #endregion


        static ClientInput()
        {
            s_currentControlScheme = new InputControlScheme(MOUSE_AND_KEYBOARD_CONTROL_SCHEME_NAME);
            InitialiseInputPrevention();
        }


        private static PlayerInputActions s_inputActions;
        private void Awake()
        {
            if (s_instance != null)
            {
                Destroy(this.gameObject);
                return;
            }
            else
            {
                s_instance = this;
                DontDestroyOnLoad(this.gameObject);
            }

            CreateInputActions();

            InputSystem.onDeviceChange += InputSystem_onDeviceChange;
            InputUser.onUnpairedDeviceUsed += InputUser_onUnpairedDeviceUsed;
            InputUser.listenForUnpairedDeviceActivity = 1;
        }
        private void OnDestroy()
        {
            if (s_instance != this)
                return; // Not the instance.
            
            if (s_inputActions != null)
                DestroyInputActions();  // Dispose of our InputActionMap.

            InputSystem.onDeviceChange -= InputSystem_onDeviceChange;
            InputUser.onUnpairedDeviceUsed -= InputUser_onUnpairedDeviceUsed;
        }
        private void CreateInputActions()
        {
            // Create the InputActionMap.
            s_inputActions = new PlayerInputActions();


            // Subscribe to Input Events.
            #region Combat Events

            s_inputActions.Combat.ActivateSlot0.started  += ActivateSlot0_started;
            s_inputActions.Combat.ActivateSlot0.canceled += ActivateSlot0_cancelled;

            s_inputActions.Combat.ActivateSlot1.started  += ActivateSlot1_started;
            s_inputActions.Combat.ActivateSlot1.canceled += ActivateSlot1_cancelled;

            s_inputActions.Combat.ActivateSlot2.started  += ActivateSlot2_started;
            s_inputActions.Combat.ActivateSlot2.canceled += ActivateSlot2_cancelled;

            s_inputActions.Combat.ActivateSlot3.started  += ActivateSlot3_started;
            s_inputActions.Combat.ActivateSlot3.canceled += ActivateSlot3_cancelled;

            #endregion

            #region UI Events

            s_inputActions.UI.Confirm.performed += Confirm_performed;
            s_inputActions.UI.NextTab.performed += NextTab_performed;
            s_inputActions.UI.PreviousTab.performed += PreviousTab_performed;
            s_inputActions.UI.Navigate.performed += Navigate_performed;
            s_inputActions.UI.ToggleLeaderboardUI.performed += ToggleLeaderboardUI_performed;

            #endregion

            s_inputActions.MainMenu.OpenFrameSelection.performed += OpenFrameSelection_performed;

            #region Multiplayer Chat Events

            s_inputActions.MultiplayerChat.OpenChat.performed   += OpenChat_performed;
            s_inputActions.MultiplayerChat.SubmitChat.performed += SubmitChat_performed;
            s_inputActions.MultiplayerChat.CancelChat.performed += CancelChat_performed;

            #endregion

            s_inputActions.GameState.PauseGame.performed += PauseGame_perfored;


            // Enable the Input Actions.
            s_inputActions.Enable();

            // Ensure that the correct maps are enabled based on toggles.
            EnsureCorrectInputMapActivation();
        }
        private void DestroyInputActions()
        {
            // Unsubscribe from Input Events.
            #region Combat Events

            s_inputActions.Combat.ActivateSlot0.started  -= ActivateSlot0_started;
            s_inputActions.Combat.ActivateSlot0.canceled -= ActivateSlot0_cancelled;

            s_inputActions.Combat.ActivateSlot1.started  -= ActivateSlot1_started;
            s_inputActions.Combat.ActivateSlot1.canceled -= ActivateSlot1_cancelled;

            s_inputActions.Combat.ActivateSlot2.started  -= ActivateSlot2_started;
            s_inputActions.Combat.ActivateSlot2.canceled -= ActivateSlot2_cancelled;

            s_inputActions.Combat.ActivateSlot3.started  -= ActivateSlot3_started;
            s_inputActions.Combat.ActivateSlot3.canceled -= ActivateSlot3_cancelled;

            #endregion

            #region UI Events

            s_inputActions.UI.Confirm.performed                 -= Confirm_performed;
            s_inputActions.UI.NextTab.performed                 -= NextTab_performed;
            s_inputActions.UI.PreviousTab.performed             -= PreviousTab_performed;
            s_inputActions.UI.Navigate.performed                -= Navigate_performed;
            s_inputActions.UI.ToggleLeaderboardUI.performed     -= ToggleLeaderboardUI_performed;

            #endregion

            s_inputActions.MainMenu.OpenFrameSelection.performed      -= OpenFrameSelection_performed;

            #region Multiplayer Chat Events

            s_inputActions.MultiplayerChat.OpenChat.performed   -= OpenChat_performed;
            s_inputActions.MultiplayerChat.SubmitChat.performed -= SubmitChat_performed;
            s_inputActions.MultiplayerChat.CancelChat.performed -= CancelChat_performed;

            #endregion

            s_inputActions.GameState.PauseGame.performed -= PauseGame_perfored;


            // Dispose of the Input Actions.
            s_inputActions.Dispose();

            // Remove our Reference.
            s_inputActions = null;
        }


        private void Update()
        {
            if (s_inputActions == null)
                return;

            CheckFocus();

            // Cache our movement input & notify listeners if it's changed since the last notification.
            MovementInput = s_inputActions.Movement.Movement.ReadValue<Vector2>();
            if (MovementInput != s_previousMovementInput)
            {
                OnMovementInputChanged?.Invoke();
                s_previousMovementInput = MovementInput;
            }

            LookInput = s_inputActions.Camera.LookInput.ReadValue<Vector2>();
        }


        #region Combat Event Functions

        private void ActivateSlot0_started(InputAction.CallbackContext obj)     => OnActivateSlotStarted?.Invoke(0);
        private void ActivateSlot0_cancelled(InputAction.CallbackContext obj)   => OnActivateSlotCancelled?.Invoke(0);

        private void ActivateSlot1_started(InputAction.CallbackContext obj)     => OnActivateSlotStarted?.Invoke(1);
        private void ActivateSlot1_cancelled(InputAction.CallbackContext obj)   => OnActivateSlotCancelled?.Invoke(1);

        private void ActivateSlot2_started(InputAction.CallbackContext obj)     => OnActivateSlotStarted?.Invoke(2);
        private void ActivateSlot2_cancelled(InputAction.CallbackContext obj)   => OnActivateSlotCancelled?.Invoke(2);

        private void ActivateSlot3_started(InputAction.CallbackContext obj)     => OnActivateSlotStarted?.Invoke(3);
        private void ActivateSlot3_cancelled(InputAction.CallbackContext obj)   => OnActivateSlotCancelled?.Invoke(3);

        #endregion

        #region UI Event Functions

        private void Navigate_performed(InputAction.CallbackContext obj) => OnNavigatePerformed?.Invoke(obj.ReadValue<Vector2>());
        private void NextTab_performed(InputAction.CallbackContext obj) => OnNextTabPerformed?.Invoke();
        private void PreviousTab_performed(InputAction.CallbackContext obj) => OnPreviousTabPerformed?.Invoke();

        private void OpenFrameSelection_performed(InputAction.CallbackContext obj) => OnOpenFrameSelectionPerformed?.Invoke();
        private void Confirm_performed(InputAction.CallbackContext obj) => OnConfirmPerformed?.Invoke();

        private void ToggleLeaderboardUI_performed(InputAction.CallbackContext obj) => OnToggleLeaderboardPerformed?.Invoke();

        #endregion

        #region Multiplayer Chat Event Functions

        private void OpenChat_performed(InputAction.CallbackContext obj) => OnOpenChatPerformed?.Invoke();
        private void SubmitChat_performed(InputAction.CallbackContext obj) => OnSubmitChatPerformed?.Invoke();
        private void CancelChat_performed(InputAction.CallbackContext obj) => OnCancelChatPerformed?.Invoke();

        #endregion

        #region Game State Event Functions

        private void PauseGame_perfored(InputAction.CallbackContext ctx) => OnPauseGamePerformed?.Invoke();

        #endregion



        private bool _isInputFieldFocused;
        /// <summary>
        ///     Checks if the player is currently selecting a InputField or TMP_InputField and adds an input prevention if they are.</br>
        ///     If the player isn't, we instead remove that prevention.
        /// </summary>
        private void CheckFocus()
        {
            GameObject selected = EventSystem.current.currentSelectedGameObject;
            if (selected != null)
            {
                if (selected.TryGetComponent<UnityEngine.UI.InputField>(out var inputField) && inputField.isFocused)
                    HandleInputFieldFocused();
                else if (selected.TryGetComponent<TMPro.TMP_InputField>(out var tmpInputField) && tmpInputField.isFocused)
                    HandleInputFieldFocused();
                else
                    HandleNoInputFieldFocused();
            }
            else
                HandleNoInputFieldFocused();
        }
        private void HandleInputFieldFocused()
        {
            if (_isInputFieldFocused)
                return;
            _isInputFieldFocused = true;

            AddActionPrevention(typeof(ClientInput), ActionTypes.Everything);
        }
        private void HandleNoInputFieldFocused()
        {
            if (!_isInputFieldFocused)
                return;
            _isInputFieldFocused = false;

            RemoveActionPrevention(typeof(ClientInput), ActionTypes.Everything);
        }



        #region Input Prevention

        private static void InitialiseInputPrevention()
        {
            foreach (var actionType in VALID_ACTION_TYPES)
            {
                if (!s_actionPreventionDictionaries.ContainsKey(actionType))
                    throw new System.NotImplementedException($"No Dictionary Implemented for ActionTypes {actionType.ToString()}");

                s_actionPreventionDictionaries[actionType] = new Dictionary<Type, int>();
            }
        }
        /// <summary>
        ///     Reset all instances of input prevention.
        /// </summary>
        public static void ResetInputPrevention()
        {
            foreach (var dictionary in s_actionPreventionDictionaries.Values)
            {
                dictionary.Clear();
            }
        }
        /// <summary>
        ///     Ensure that all Input Action Maps are correctly activated/deactivated based on their current preventions.
        /// </summary>
        public static void EnsureCorrectInputMapActivation()
        {
            if (s_inputActions == null)
                return; // No InputActions have been created.

            // Ensure all actions map preventions are correct for all our Action Types.
            foreach (ActionTypes actionType in s_actionPreventionDictionaries.Keys)
            {
                if (s_actionPreventionDictionaries[actionType].Count > 0)
                    DisableActionMap(actionType);
                else
                    EnableActionMap(actionType);
            }
        }


        [System.Serializable] [System.Flags]
        public enum ActionTypes
        {
            None = 0,

            Movement    = 1 << 0,   // Standard Character Movement (WASD/Stick)
            Camera      = 1 << 1,   // Camera Controls
            Combat      = 1 << 2,   // Weapon/Ability Activation
            UI          = 1 << 3,   // Main, Pause, and Customisation Menus
            MultiplayerChat = 1 << 4,   // Text and Voice Chat Input

            Respawning = ActionTypes.Movement | ActionTypes.Camera | ActionTypes.Combat,

            Everything = ~0
        }
        private static readonly ActionTypes[] VALID_ACTION_TYPES =
        {
            ActionTypes.Movement,
            ActionTypes.Camera,
            ActionTypes.Combat,
            ActionTypes.UI,
            ActionTypes.MultiplayerChat,
        };
        private static Dictionary<ActionTypes, Dictionary<Type, int>> s_actionPreventionDictionaries = new()
        {
            { ActionTypes.Movement, new Dictionary<Type, int>() },
            { ActionTypes.Camera, new Dictionary<Type, int>() },
            { ActionTypes.Combat, new Dictionary<Type, int>() },
            { ActionTypes.UI, new Dictionary<Type, int>() },
            { ActionTypes.MultiplayerChat, new Dictionary<Type, int>() },
        };

        private static void EnableActionMap(ActionTypes actionType)
        {
            switch (actionType)
            {
                case ActionTypes.Movement:
                    s_inputActions.Movement.Enable();
                    break;
                case ActionTypes.Camera:
                    s_inputActions.Camera.Enable();
                    break;
                case ActionTypes.Combat:
                    s_inputActions.Combat.Enable();
                    break;
                case ActionTypes.UI:
                    s_inputActions.UI.Enable();
                    s_inputActions.MainMenu.Enable();
                    break;
                case ActionTypes.MultiplayerChat:
                    s_inputActions.MultiplayerChat.Enable();
                    break;
                default: throw new System.NotImplementedException($"No Map Disabling Setup for ActionTypes {actionType.ToString()}");
            };
        }
        private static void DisableActionMap(ActionTypes actionType)
        {
            switch (actionType)
            {
                case ActionTypes.Movement:
                    s_inputActions.Movement.Disable();
                    break;
                case ActionTypes.Camera:
                    s_inputActions.Camera.Disable();
                    break;
                case ActionTypes.Combat:
                    s_inputActions.Combat.Disable();
                    break;
                case ActionTypes.UI:
                    s_inputActions.UI.Disable();
                    s_inputActions.MainMenu.Disable();
                    break;
                case ActionTypes.MultiplayerChat:
                    s_inputActions.MultiplayerChat.Disable();
                    break;
                default: throw new System.NotImplementedException($"No Map Disabling Setup for ActionTypes {actionType.ToString()}");
            };
        }



        /// <summary>
        ///     Prevent the desired actions from being performed.
        /// </summary>
        /// <param name="lockingType"> The type of the object that is locking these actions.</param>
        /// <param name="actionsToLock"> The types of actions that we are locking.</param>
        public static void AddActionPrevention(Type lockingType, ActionTypes actionsToLock)
        {
            // Loop through all selected values of our enum.
            //foreach (var actionType in Enum.GetValues(typeof(ActionTypes)).Cast<ActionTypes>().Where(x => (actionsToLock & x) > 0))
            foreach (var actionType in VALID_ACTION_TYPES)
            {
                if (!actionsToLock.HasFlag(actionType))
                    continue;   // Only process our desired action types.

                if (!s_actionPreventionDictionaries.TryGetValue(actionType, out var dictionary))
                    throw new System.NotImplementedException($"No Dictionary assigned for ActionTypes {actionType.ToString()}");


                if (!dictionary.TryAdd(lockingType, 1)) // If we have no kvp with key 'lockingType' initialise one with 1 count.
                {
                    // We already have a kvp with key 'lockingType', so increment instead.
                    ++dictionary[lockingType];
                }

                if (s_inputActions != null)
                {
                    // At least one type is disabling this ActionType.
                    // Disable our corresponding map.
                    DisableActionMap(actionType);
                }
            }
        }
        /// <summary>
        ///     Remove one count of prevention from the desired types of actions. <br/>
        ///     Enables the actions if there is no longer any types wishing for prevention.
        /// </summary>
        /// <param name="lockingType"> The type of the object that had locked these actions.</param>
        /// <param name="actionsToUnlock"> The types of actions that we are unlocking.</param>
        public static void RemoveActionPrevention(Type lockingType, ActionTypes actionsToUnlock)
        {
            // Loop through all selected values of our enum.
            //foreach (var actionType in Enum.GetValues(typeof(ActionTypes)).Cast<ActionTypes>().Where(x => (actionsToUnlock & x) > 0))
            foreach (var actionType in VALID_ACTION_TYPES)
            {
                if (!actionsToUnlock.HasFlag(actionType))
                    continue;   // Only process our desired action types.

                if (!s_actionPreventionDictionaries.TryGetValue(actionType, out var dictionary))
                    throw new System.NotImplementedException($"No Dictionary assigned for ActionTypes {actionType.ToString()}");


                if (dictionary.ContainsKey(lockingType))
                {
                    --dictionary[lockingType];

                    if (dictionary[lockingType] <= 0)
                    {
                        dictionary.Remove(lockingType);
                    }
                }

                if (dictionary.Count == 0 && s_inputActions != null)
                {
                    // There are no longer any types wishing to disable this actionType's associated controls.
                    // Enable the corresponding map.
                    EnableActionMap(actionType);
                }
            }
        }


#if UNITY_EDITOR

        [ContextMenu(itemName: "Display Active Locks")]
        private void DisplayLocks()
        {
            foreach (var kvp in s_actionPreventionDictionaries)
            {
                Debug.Log(kvp.Key.ToString() + ": " + kvp.Value.Count + "\n" + string.Concat(kvp.Value.Keys));
            }
        }

#endif

        #endregion


        #region Active Device Detection

        private void InputSystem_onDeviceChange(InputDevice inputDevice, InputDeviceChange inputDeviceChange)
        {
            switch (inputDeviceChange)
            {
                case InputDeviceChange.Disconnected:
                    Debug.Log("User's device was disconnected");
                    break;
            }
        }
        private void InputUser_onUnpairedDeviceUsed(InputControl inputControl, UnityEngine.InputSystem.LowLevel.InputEventPtr inputEventPtr)
        {
            // Get the control scheme associated with the used device.
            InputControlScheme deviceControlScheme = s_inputActions.controlSchemes.Where(t => t.SupportsDevice(inputControl.device)).FirstOrDefault();
            CurrentInputDevice = inputControl.device;

            if (deviceControlScheme != default && deviceControlScheme != s_currentControlScheme)
            {
                // A input device belonging to a different control scheme has been used.
                s_currentControlScheme = deviceControlScheme;
                Debug.Log("New Used. Scheme Name: " + deviceControlScheme.name);

                OnInputDeviceChanged?.Invoke();
            }
        }

        #endregion


        public static InputAction GetSlotActivationAction(AttachmentSlotIndex slotIndex) => slotIndex switch
        {
            AttachmentSlotIndex.Primary => s_inputActions.Combat.ActivateSlot0,
            AttachmentSlotIndex.Secondary => s_inputActions.Combat.ActivateSlot1,
            AttachmentSlotIndex.Tertiary => s_inputActions.Combat.ActivateSlot2,
            AttachmentSlotIndex.Quaternary => s_inputActions.Combat.ActivateSlot3,

            _ => throw new System.NotImplementedException(),
        };


        public static InputAction GetReferenceForAction(InputAction inputAction) => s_inputActions.FindAction(inputAction.name, true);
    }
}