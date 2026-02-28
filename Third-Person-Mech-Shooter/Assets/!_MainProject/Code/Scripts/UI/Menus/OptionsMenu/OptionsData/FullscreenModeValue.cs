using System.Collections.Generic;
using UnityEngine;

/// <summary>
///     A ScriptableObject container for an Option within the OptionsMenu that controls the Resolution.<br/>
///     <see cref="OptionsValue{T}"/>
/// </summary>
[CreateAssetMenu(menuName = "Options Values/Specific/New FullscreenMode Value", order = 1)]
public class FullscreenModeOptionValue : OptionsValue<FullScreenMode>, IDropdownSupportingOptionValue
{
    private const bool ALLOW_MAXIMISED_WINDOW = false;


    public void SetValue(int dropdownIndex) => SetValue((FullScreenMode)dropdownIndex);
    public override void SetValue(FullScreenMode newFullscreenMode)
    {
#if !ALLOW_MAXIMSED_WINDOW
        EnsureValueIsNotInvalid(ref newFullscreenMode);
#endif

        // Don't update/notify if we haven't changed our value.
        if (m_Value == newFullscreenMode)
            return;

        Screen.fullScreenMode = newFullscreenMode;

        m_Value = newFullscreenMode;
        InvokeOnValueChanged();
    }


    public int GetSelectedOptionIndex() => (int)m_Value;
    public List<string> GetDropdownOptions() => new()
    {
        "Exclusive Fullscreen", // 0 = FullScreenMode.ExclusiveFullScreen
        "Fullscreen Window",    // 1 = FullScreenMode.FullScreenWindow
#if ALLOW_MAXIMISED_WINDOW
        "Maximised Window",     // 2 = FullScreenMode.MaximizedWindow
#endif
        "Windowed",             // 3 (Or 2) = FullScreenMode.Windowed
    };


    public override void SaveToPrefs()
    {
#if !ALLOW_MAXIMSED_WINDOW
        EnsureValueIsNotInvalid(ref m_Value);
#endif
        PlayerPrefs.SetInt(PrefsIdentifier, (int)m_Value);
    }
    public override void LoadFromPrefs()
    {
        m_Value = (FullScreenMode)PlayerPrefs.GetInt(PrefsIdentifier, (int)DefaultValue);
#if !ALLOW_MAXIMSED_WINDOW
        EnsureValueIsNotInvalid(ref m_Value);
#endif

        Screen.fullScreenMode = m_Value;
        InvokeOnValueChanged();
    }


#if !ALLOW_MAXIMISED_WINDOW
    private void EnsureValueIsNotInvalid(ref FullScreenMode fullScreenMode)
    {
        if (fullScreenMode == FullScreenMode.MaximizedWindow)
            fullScreenMode = FullScreenMode.Windowed;
    }
#endif
}


public interface IDropdownSupportingOptionValue
{
    public void SetValue(int dropdownIndex);
    public int GetSelectedOptionIndex();
    public List<string> GetDropdownOptions();
}