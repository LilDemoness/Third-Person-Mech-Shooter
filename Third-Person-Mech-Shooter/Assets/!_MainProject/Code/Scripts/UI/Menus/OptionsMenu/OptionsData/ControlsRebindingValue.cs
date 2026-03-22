using UnityEngine;
using UserInput;


/// <summary>
///     A ScriptableObject container for the JSON string storing all keybinding overrides.<br/>
///     <see cref="OptionsValue{T}"/>
/// </summary>
/// <remarks> Instances are created at runtime.</remarks>
public class ControlsRebindingValue : OptionsValue<string>
{
    private const string CONTROL_BINDING_OVERRIDES_FILE_PATH = "ControlOverrides";

    [System.NonSerialized] private static ControlsRebindingValue s_instance;
    public static ControlsRebindingValue Instance
    {
        get
        {
            if (s_instance == null)
            {
                s_instance = CreateInstance<ControlsRebindingValue>();
                s_instance.Init();
            }

            return s_instance;
        }
    }


    public override void SaveToPrefs()
    {
        Debug.Log("Save Control Overrides");
        PlayerPrefs.SetString(CONTROL_BINDING_OVERRIDES_FILE_PATH, Value);
    }
    public override void LoadFromPrefs()
    {
        Debug.Log("Load Control Overrides");
        Debug.Log(PlayerPrefs.GetString(CONTROL_BINDING_OVERRIDES_FILE_PATH, Value));
        SetValueNoNotifyNoChecks(PlayerPrefs.GetString(CONTROL_BINDING_OVERRIDES_FILE_PATH, Value));
        InvokeOnValueChanged();
    }

    private void SaveToJSON() { }
    private void LoadFromJSON() { }


    public void UpdateValue() => SetValue(string.Empty);
    public override void SetValue(string _) => SetValueNoNotifyNoChecks(ClientInput.TryGetBindingOverridesAsJSON(out string jsonString) ? jsonString : string.Empty);
}
