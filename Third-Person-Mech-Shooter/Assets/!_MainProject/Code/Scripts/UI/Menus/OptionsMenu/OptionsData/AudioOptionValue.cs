using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(menuName = "Options Values/Specific/New Audio Value", order = 2)]
public class AudioOptionValue : FloatOptionValue
{
    [SerializeField] private AudioMixer _audioMixer;
    [SerializeField] private VolumeIdentifier _volumeIdentifier;

    protected override void SetValueNoNotifyNoChecks(float newValue)
    {
        base.SetValueNoNotifyNoChecks(newValue);
        _audioMixer.SetFloat(_volumeIdentifier.ToIdentifier(), ConvertPercentageToLinearAudioValue(newValue));
    }

    private static float ConvertPercentageToLinearAudioValue(float percentageValue) => Mathf.Max(Mathf.Log10(percentageValue) * 19.9316f, -80.0f);


#if UNITY_EDITOR

    private void OnValidate()
    {
        m_MinValue = 0.0f;
        m_MaxValue = 1.0f;
    }
    private void Reset() => DefaultValue = 1.0f;

#endif


}
public enum VolumeIdentifier
{
    MasterVolume = 0
}
public static class VolumeIdentifierExtensions
{
    public static string ToIdentifier(this VolumeIdentifier volumeIdentifier) => volumeIdentifier switch
    {
        VolumeIdentifier.MasterVolume => "MasterVolume",
        _ => throw new System.NotImplementedException(),
    };
}
