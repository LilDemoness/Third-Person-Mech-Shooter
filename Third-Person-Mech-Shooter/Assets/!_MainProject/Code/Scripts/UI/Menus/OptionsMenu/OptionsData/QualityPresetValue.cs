using System.Collections.Generic;
using UnityEngine;

/// <summary>
///     A ScriptableObject container for an Option within the OptionsMenu that controls the Resolution.<br/>
///     <see cref="OptionsValue{T}"/>
/// </summary>
[CreateAssetMenu(menuName = "Options Values/Specific/New Quality Preset Value", order = 3)]
public class QualityPresetOptionValue : OptionsValue<int>, IDropdownSupportingOptionValue
{
    [System.NonSerialized] private int _customValueIndex = -1;


    public override void SetValue(int newQualityPreset)
    {
        // Any invalid value should be set to the custom value.
        if (newQualityPreset < 0 || newQualityPreset > _customValueIndex)
            newQualityPreset = _customValueIndex;

        // Don't update/notify if we haven't changed our value.
        if (Value == newQualityPreset)
            return;

        SetValueNoNotifyNoChecks(newQualityPreset);
        InvokeOnValueChanged();
    }
    protected override void SetValueNoNotifyNoChecks(int newQualityPreset)
    {
        base.SetValueNoNotifyNoChecks(newQualityPreset);

        if (newQualityPreset != _customValueIndex)
            QualitySettings.SetQualityLevel(newQualityPreset);
    }


    public int GetSelectedOptionIndex() => Value;
    public List<string> GetDropdownOptions()
    {
        List<string> names = new List<string>(QualitySettings.count + 1);
        for(int i = 0; i < QualitySettings.count; ++i)
            names.Add(QualitySettings.names[i]);

        // Custom Option.
        _customValueIndex = QualitySettings.count;
        names.Add("Custom");

        return names;
    }


    public override void SaveToPrefs() => PlayerPrefs.SetInt(PrefsIdentifier, Value);
    public override void LoadFromPrefs()
    {
        SetValueNoNotifyNoChecks(PlayerPrefs.GetInt(PrefsIdentifier, QualitySettings.GetQualityLevel()));
        InvokeOnValueChanged();
    }


    public void OnAnyQualitySettingChanged() => SetValue(_customValueIndex);
}