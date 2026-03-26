#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Linq;

/// <summary>
///     An Editor-only class that ensures all instances of <see cref="BaseOptionsValue"/> are reset to their defaults upon the editor stopping.
/// </summary>
[InitializeOnLoad]
public static class OptionsDataValueCleanup
{
    static OptionsDataValueCleanup()
    {
        EditorApplication.playModeStateChanged += ModeChanged;
    }

    private static void ModeChanged(PlayModeStateChange stateChange)
    {
        if (stateChange != PlayModeStateChange.ExitingPlayMode || stateChange != PlayModeStateChange.ExitingEditMode)
            return;
        
        ResetAllOptionsDataInstances();
    }

    private static void ResetAllOptionsDataInstances()
    {
        Debug.Log("Resetting all OptionsValue instance values");

        System.Type[] allTypes = System.Reflection.Assembly.GetExecutingAssembly().GetTypes();
        System.Type[] optionValueChildTypes = (from System.Type type in allTypes where type.IsSubclassOf(typeof(BaseOptionsValue)) select type).ToArray();

        foreach(var optionValueChildType in optionValueChildTypes)
        {
            foreach (var optionsValue in Resources.LoadAll("", optionValueChildType))
            {
                (optionsValue as BaseOptionsValue).Editor_ResetValuesNoNotify();
            }
        }
    }
}
#endif