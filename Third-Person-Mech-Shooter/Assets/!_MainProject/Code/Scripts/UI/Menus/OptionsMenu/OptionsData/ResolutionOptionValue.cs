using System.Collections.Generic;
using UnityEngine;

/// <summary>
///     A ScriptableObject container for an Option within the OptionsMenu that controls the Resolution.<br/>
///     <see cref="OptionsValue{T}"/>
/// </summary>
[CreateAssetMenu(menuName = "Options Values/Specific/New Resolution Value", order = 0)]
public class ResolutionOptionValue : OptionsValue<Resolution>, IDropdownSupportingOptionValue
{
    private const string WIDTH_PREFS_SUFFIX = "-Width";
    private const string HEIGHT_PREFS_SUFFIX = "-Height";

    private const string DROPDOWN_TEXT_FORMATTING_STRING = "{0}x{1}";

    public Resolution[] Resolutions;

    public override void Init()
    {
        if (Initialised)
            return;

        base.Init();
        Resolutions = Screen.resolutions;
    }


    public void SetValue(int dropdownIndex) => SetValue(Resolutions[dropdownIndex]);
    public override void SetValue(Resolution newValue)
    {
        // Don't update/notify if we haven't changed our value.
        if (newValue.width == Value.width && newValue.height == Value.height)
            return;

        SetValueNoNotifyNoChecks(newValue);
        InvokeOnValueChanged();
    }

    protected override void SetValueNoNotifyNoChecks(Resolution newValue)
    {
        base.SetValueNoNotifyNoChecks(newValue);
        Screen.SetResolution(newValue.width, newValue.height, Screen.fullScreenMode);
    }


    public int GetSelectedOptionIndex()
    {
        // Find the index corresponding to the value's resolution.
        int resolutionValueIndex = -1;
        for (int i = 0; i < Resolutions.Length; ++i)
        {
            if (Value.width == Resolutions[i].width && Value.height == Resolutions[i].height)
            {
                resolutionValueIndex = i;
                break;
            }
        }

        return resolutionValueIndex;
    }
    public List<string> GetDropdownOptions()
    {
        // Create our options using our desired formatting.
        List<string> options = new List<string>(Resolutions.Length);
        for (int i = 0; i < Resolutions.Length; ++i)
        {
            options.Add(string.Format(DROPDOWN_TEXT_FORMATTING_STRING, Resolutions[i].width, Resolutions[i].height));
        }

        return options;
    }


    public override void SaveToPrefs()
    {
        // Protect against saving default or invalid values.
        if (Value.width <= 0 || Value.height <= 0)
            return;

        // We cannot save the Resolution struct as is, so break it into width and height for saving.
        PlayerPrefs.SetInt(PrefsIdentifier + WIDTH_PREFS_SUFFIX, Value.width);
        PlayerPrefs.SetInt(PrefsIdentifier + HEIGHT_PREFS_SUFFIX, Value.height);
    }
    public override void LoadFromPrefs()
    {
        Resolution resolution = new Resolution();
        resolution.width = PlayerPrefs.GetInt(PrefsIdentifier + WIDTH_PREFS_SUFFIX, -1);
        resolution.height = PlayerPrefs.GetInt(PrefsIdentifier + HEIGHT_PREFS_SUFFIX, -1);

        // Check for unset values. If any are unset, default to the screen's current resolution.
        if (resolution.width <= 0 || resolution.height <= 0)
        {
            resolution = Screen.currentResolution;
        }

        SetValueNoNotifyNoChecks(resolution);
        InvokeOnValueChanged();
    }
}