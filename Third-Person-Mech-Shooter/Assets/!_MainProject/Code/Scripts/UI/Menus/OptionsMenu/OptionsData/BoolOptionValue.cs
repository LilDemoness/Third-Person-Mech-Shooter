using UnityEngine;

/// <summary>
///     A ScriptableObject container for an option within the Options Menu that is represented as a bool.<br/>
///     <see cref="OptionsValue{T}"/>
/// </summary>
[CreateAssetMenu(menuName = "Options Values/New Bool Value", order = 0)]
public class BoolOptionValue : OptionsValue<bool>
{
    public override void SetValue(bool newValue)
    {
        // Don't update/notify if we haven't changed our value.
        if (Value == newValue)
            return;

        SetValueNoNotifyNoChecks(newValue);
        InvokeOnValueChanged();
    }


    public override void SaveToPrefs() => PlayerPrefs.SetInt(PrefsIdentifier, Value ? 1 : 0);
    public override void LoadFromPrefs()
    {
        SetValueNoNotifyNoChecks(PlayerPrefs.GetInt(PrefsIdentifier, DefaultValue ? 1 : 0) == 1);
        InvokeOnValueChanged();
    }
}
