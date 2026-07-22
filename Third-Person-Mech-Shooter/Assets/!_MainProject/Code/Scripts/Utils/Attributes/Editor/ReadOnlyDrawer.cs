using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        => InspectorUtils.DrawProperty(position, property, label, true);
}


public static class InspectorUtils
{
    public static Rect DrawWithCallback(Rect position, SerializedPropertyType propertyType, System.Action<Rect> drawCallback, GUIContent label, bool drawDisabled = false)
    {
        if (ChangesWhenThin(propertyType) && !EditorGUIUtility.wideMode)
            return DrawThin(position, drawCallback, label, drawDisabled);
        else
            return DrawWide(position, drawCallback, label, drawDisabled);
    }

    public static Rect DrawProperty(Rect position, SerializedProperty property, GUIContent label, bool drawDisabled = false)
        => DrawWithCallback(position, property.propertyType, (Rect propertyRect) => EditorGUI.PropertyField(propertyRect, property, GUIContent.none, true), label, drawDisabled);


    private static bool ChangesWhenThin(SerializedProperty property) => ChangesWhenThin(property.propertyType);
    private static bool ChangesWhenThin(SerializedPropertyType propertyType)
    => propertyType switch
    {
        SerializedPropertyType.Vector2 => true,
        SerializedPropertyType.Vector3 => true,
        SerializedPropertyType.Vector4 => true,

        _ => false
    };

    /// <summary>
    ///     Displays the label and property on the same line.
    /// </summary>
    private static Rect DrawWide(Rect totalPosition, System.Action<Rect> drawCallback, GUIContent label, bool drawDisabled = false)
    {
        float indent = EditorGUI.indentLevel * 15.0f;
        const float padding = 2.0f;

        EditorGUILayout.BeginHorizontal();

        // Label.
        Rect labelRect = new Rect(totalPosition.x, totalPosition.y, EditorGUIUtility.labelWidth, totalPosition.height);
        EditorGUI.LabelField(labelRect, label);

        // Property Field (Drawn Disabled).
        GUI.enabled = !drawDisabled;
        Rect propertyRect = new Rect(totalPosition.x + EditorGUIUtility.labelWidth + padding - indent, totalPosition.y, totalPosition.width - labelRect.width - padding, totalPosition.height);
        drawCallback.Invoke(propertyRect);
        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();

        Rect result = new Rect(totalPosition);
        result.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        return result;
    }
    /// <summary>
    ///     Displays the label and property on different lines.<br/>
    ///     
    ///     Utilised by field types such as Vector2, Vector3, and Vector4.
    /// </summary>
    private static Rect DrawThin(Rect totalPosition, System.Action<Rect> drawCallback, GUIContent label, bool drawDisabled = false)
    {
        // Draw Label.
        Rect labelRect = new Rect(totalPosition.x, totalPosition.y - (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) / 2.0f, totalPosition.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.LabelField(labelRect, label);

        // Draw Property (Drawn Disabled).
        Rect propertyRect = new Rect(totalPosition.x, totalPosition.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing, totalPosition.width, totalPosition.height);
        GUI.enabled = !drawDisabled;
        drawCallback.Invoke(propertyRect);
        GUI.enabled = true;

        Rect result = new Rect(totalPosition);
        result.y += (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 2.0f;
        return result;
    }
}