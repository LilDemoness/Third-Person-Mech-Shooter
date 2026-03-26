namespace Gameplay.UI.Popups
{
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