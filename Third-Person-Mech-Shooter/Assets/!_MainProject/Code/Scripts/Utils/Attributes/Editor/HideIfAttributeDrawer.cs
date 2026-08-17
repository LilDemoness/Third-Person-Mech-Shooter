using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(HideIfAttribute), true)]
public class HideIfAttributeDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (EvaluateCondition(property, out string errorMessage))
            // Value is true: We don't wish to draw the property.
            return 0.0f;

        return base.GetPropertyHeight(property, label);
    }
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (EvaluateCondition(property, out string errorMessage))
            // Value is true: Hide.
            return;

        if (errorMessage != null)
            // We encountered an error. Display it in the inspector so the user knows.
        {
            EditorGUILayout.HelpBox(errorMessage, MessageType.Error);
            return;
        }

        EditorGUI.PropertyField(position, property, label);
    }


    private bool EvaluateCondition(SerializedProperty property, out string errorMessage)
        => AttributeUtils.EvaluateConditionToValue(property, (attribute as HideIfAttribute).Condition, (attribute as HideIfAttribute).Value ?? true, out errorMessage);
}