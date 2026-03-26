using UnityEngine;

/// <summary>
///     A ScriptableObject container for an option within the Options Menu that is represented as an integer>.<br/>
///     A range is specified, forcing values to be between a minimum and maximum number.<br/>
///     <see cref="OptionsValue{T}"/>
/// </summary>
[CreateAssetMenu(menuName = "Options Values/New Int Value", order = 1)]
public class IntOptionValue : OptionsValue<int>
{
    [SerializeField] private int m_minValue;
    [SerializeField] private int m_maxValue;

    public int MinValue => m_minValue;
    public int MaxValue => m_maxValue;
    public bool HasLimits => !Mathf.Approximately(MinValue, MaxValue);

    public override void SetValue(int newValue)
    {
        // Check if we should clamp, and do so if we should.
        if (HasLimits)
            newValue = Mathf.Clamp(newValue, MinValue, MaxValue);

        // Don't update/notify if we haven't changed our value.
        if (Value == newValue)
            return;
        
        SetValueNoNotifyNoChecks(newValue);
        InvokeOnValueChanged();
    }


    public override void SaveToPrefs() => PlayerPrefs.SetInt(PrefsIdentifier, Value);
    public override void LoadFromPrefs()
    {
        SetValueNoNotifyNoChecks(PlayerPrefs.GetInt(PrefsIdentifier, DefaultValue));
        InvokeOnValueChanged();
    }


#if UNITY_EDITOR

    private void OnValidate()
    {
        if (HasLimits)
        {
            if (DefaultValue < m_minValue)
                Debug.LogError($"Default Value of {this.name} is below the minimum value");
            else if (DefaultValue > m_maxValue)
                Debug.LogError($"Default Value of {this.name} exceeds the maximum value");
        }
    }

#endif
}