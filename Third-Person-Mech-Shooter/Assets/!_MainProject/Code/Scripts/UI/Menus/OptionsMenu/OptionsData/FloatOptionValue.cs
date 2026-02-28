using UnityEngine;

/// <summary>
///     A ScriptableObject container for an option within the Options Menu that is represented as a float>.<br/>
///     A range is specified, forcing values to be between a minimum and maximum number.<br/>
///     <see cref="OptionsValue{T}"/>
/// </summary>
[CreateAssetMenu(menuName = "Options Values/New Float Value", order = 2)]
public class FloatOptionValue : OptionsValue<float>
{
    [SerializeField] private float m_minValue;
    [SerializeField] private float m_maxValue;

    public float MinValue => m_minValue;
    public float MaxValue => m_maxValue;
    public bool HasLimits => !Mathf.Approximately(MinValue, MaxValue);


    [Space(5)]
    [SerializeField] private float m_roundingValue = 0.1f;


    public override void SetValue(float newValue)
    {
        // Check if we should clamp, and do so if we should.
        if (HasLimits)
            newValue = Mathf.Clamp(newValue, MinValue, MaxValue);
        // Check if we should round, and do so if we should.
        if (!Mathf.Approximately(m_roundingValue, 0.00f))
        {
            newValue = Mathf.Round(newValue / m_roundingValue) * m_roundingValue;
            Debug.Log("Rounded Value: " + newValue);
        }

        // Don't update/notify if we haven't changed our value.
        if (m_Value == newValue)
            return;

        m_Value = newValue;
        InvokeOnValueChanged();
    }


    public override void SaveToPrefs() => PlayerPrefs.SetFloat(PrefsIdentifier, m_Value);
    public override void LoadFromPrefs()
    {
        m_Value = PlayerPrefs.GetFloat(PrefsIdentifier, DefaultValue);
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
