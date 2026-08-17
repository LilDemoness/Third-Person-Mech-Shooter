using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ShowIfAttribute), true)]
public class ShowIfAttributeDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!EvaluateCondition(property, out string errorMessage))
            // We do not wish to draw the property.
        {
            return errorMessage != null
                ? base.GetPropertyHeight(property, label)   // We have an error message we wish to draw.
                : 0.0f;                                     // We aren't wishing to draw anything.
        }

        // We wish to draw the property.
        return base.GetPropertyHeight(property, label);
    }
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (!EvaluateCondition(property, out string errorMessage))
        {
            if (errorMessage != null)
            {
                // We encountered an error. Display it in the inspector so the user knows.
                EditorGUILayout.HelpBox(errorMessage, MessageType.Error);
            }
            return;
        }

        EditorGUI.PropertyField(position, property, label);
    }


    private bool EvaluateCondition(SerializedProperty property, out string errorMessage)
        => AttributeUtils.EvaluateConditionToValue(property, (attribute as ShowIfAttribute).Condition, (attribute as ShowIfAttribute).Value ?? true, out errorMessage);
}