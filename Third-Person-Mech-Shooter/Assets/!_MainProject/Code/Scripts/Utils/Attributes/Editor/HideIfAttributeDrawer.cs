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
        => EvaluateConditionToValue(property, (attribute as HideIfAttribute).Value ?? true, out errorMessage);
    
    private bool EvaluateConditionToValue(SerializedProperty property, object testCondition, out string errorMessage)
    {
        object parentObject = property.GetParentObject();

        if (!TryGetValue_Iterative(parentObject, out object value))
        {
            // We were unable to find a matching Field, Property, or Method in this element's target.
            errorMessage = $"Unable to find Field, Property, or Method matching condition: {(attribute as HideIfAttribute).Condition}";
            return false;
        }

        if (value.GetType() != testCondition.GetType())
        {
            // Failed to cast to a boolean.
            errorMessage = $"Invalid Field/Property/Method Type '{value.GetType()}' for test condition '{testCondition.GetType()}'";
            return false;
        }

        errorMessage = null;
        return value.Equals(testCondition);
    }


    // Locates the value of our condition, even if 'obj' is only inheriting from the class where our condition is present.
    // Iterates up through all the parent object's inherited types to allow for locating private fields, properties, and functions.
    private bool TryGetValue_Iterative(object obj, out object value)
    {
        System.Type type = obj.GetType();
        while (type != null)
        {
            if (TryGetValueForCondition(obj, type, out value))
                return true;

            // Continue iterating up.
            type = type.BaseType;
        }

        // Didn't locate our condition.
        value = null;
        return false;
    }
    private bool TryGetValueForCondition(object obj, System.Type type, out object value)
    {
        const BindingFlags BINDING_ATTRIBUTES = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        string condition = (attribute as HideIfAttribute).Condition;


        // Check for a Field.
        FieldInfo field = type.GetField(condition, BINDING_ATTRIBUTES);
        if (field != null)
            // Value is a field.
        {
            value = field.GetValue(obj);
            return true;
        }

        // Check for a Property.
        PropertyInfo property = type.GetProperty(condition, BINDING_ATTRIBUTES);
        if (property != null)
            // Value is a property.
        {
            value = property.GetValue(obj, null);
            return true;
        }

        // Check for Method.
        MethodInfo method = type.GetMethod(condition, BINDING_ATTRIBUTES);
        if (method != null)
            // Value is a method.
        {
            value = method.Invoke(obj, null);
            return true;
        }

        // Unable to get value (Not a Field, Property, or Method).
        value = null;
        return false;
    }
}