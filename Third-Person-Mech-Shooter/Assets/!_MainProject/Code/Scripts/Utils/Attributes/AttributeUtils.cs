using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class AttributeUtils
{
    public static bool EvaluateConditionToValue(SerializedProperty property, string condition, object comparison, out string errorMessage)
    {
        object parentObject = property.GetParentObject();

        if (!TryGetValue_Iterative(parentObject, condition, out object value))
        {
            // We were unable to find a matching Field, Property, or Method in this element's target.
            errorMessage = $"Unable to find Field, Property, or Method matching condition: {condition}";
            return false;
        }

        if (value.GetType() != comparison.GetType())
        {
            // Failed to cast to a boolean.
            errorMessage = $"Invalid Field/Property/Method Type '{value.GetType()}' for test condition '{comparison.GetType()}'";
            return false;
        }

        errorMessage = null;
        return value.Equals(comparison);
    }


    // Locates the value of our condition, even if 'obj' is only inheriting from the class where our condition is present.
    // Iterates up through all the parent object's inherited types to allow for locating private fields, properties, and functions.
    public static bool TryGetValue_Iterative(object obj, string condition, out object value)
    {
        System.Type type = obj.GetType();
        while (type != null)
        {
            if (TryGetValueForCondition(obj, type, condition, out value))
                return true;

            // Continue iterating up.
            type = type.BaseType;
        }

        // Didn't locate our condition.
        value = null;
        return false;
    }
    public static bool TryGetValueForCondition(object obj, System.Type type, string condition, out object value)
    {
        const BindingFlags BINDING_ATTRIBUTES = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

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
