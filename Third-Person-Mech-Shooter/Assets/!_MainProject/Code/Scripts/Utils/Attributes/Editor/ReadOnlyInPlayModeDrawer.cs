using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(ReadOnlyInPlayModeAttribute))]
public class ReadOnlyInPlayModeDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (Application.isPlaying)
        {
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true;
        }
        else
        {
            EditorGUI.PropertyField(position, property, label, true);
        }
    }
}
