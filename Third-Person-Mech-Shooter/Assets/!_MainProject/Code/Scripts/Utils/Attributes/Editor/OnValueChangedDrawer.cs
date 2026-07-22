using System.Reflection;
using System;
using System.Collections;
using UnityEditor;
using UnityEngine;
using System.Linq;

[CustomPropertyDrawer(typeof(OnValueChangedAttribute))]
public class OnValueChangedDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        EditorGUI.BeginChangeCheck();
        EditorGUI.PropertyField(position, property, label, true);
        if (EditorGUI.EndChangeCheck())
            // The GUI has changed (Either the property/values within the property have changed, or the property has been expanded/collapsed).
        {
            // Ensure that we will have the up-to-date values accessible when we trigger the callback method.
            property.serializedObject.ApplyModifiedProperties();

            // Cache the parent of our property (The class where our attribute is being used on a variable within).
            object parentObject = property.GetParentObject();

            // Retrieve and invoke the 'on value changed' method.
            var method = parentObject.GetType().GetMethod((attribute as OnValueChangedAttribute).Action) ?? parentObject.GetType().GetMethod((attribute as OnValueChangedAttribute).Action, BindingFlags.NonPublic | BindingFlags.Instance);
            if (method != null)
                method.Invoke(parentObject, null);
        }

        EditorGUI.EndProperty();
    }
}


// Source: 'https://discussions.unity.com/t/a-simple-way-to-access-the-class-from-the-property-drawer/900422/9'.
public static class SerializedPropertyUtils
{
    private const BindingFlags FLAGS = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;


    /// <summary>
    ///     Retrieves the parent as an object from the specified SerializedProperty.
    /// </summary>
    /// <param name="prop">The SerializedProperty to get the parent object from.</param>
    /// <returns>
    ///     The parent object of the specified SerializedProperty.
    ///     If the property is a top-level field, it returns the target object of the serializedObject.
    /// </returns>
    public static object GetParentObject(this SerializedProperty prop)
    {
        var path = prop.propertyPath;

        // Remove our property from the path, leaving us with the parent.
        var lastDot = path.LastIndexOf('.');
        if (lastDot == -1)
        {
            // The property is a top level field, so the target object is the serialized object.
            return prop.serializedObject.targetObject;
        }

        // The property has a dot.
        var parentPath = path[..lastDot];
        return GetObjectFromPropertyPath(prop.serializedObject.targetObject, parentPath);
    }


    private static object GetObjectFromPropertyPath(object root, string path)
    {
        var elements = path.Replace(".Array.data[", "[").Split('.');
        var obj = root;

        foreach (var e in elements)
        {
            if (e.Contains("["))
            {
                // handle arrays
                var eName = e[..e.IndexOf("[", StringComparison.Ordinal)];
                if (e.IndexOf("[", StringComparison.Ordinal) < 0 ||
                    e.IndexOf("[", StringComparison.Ordinal) > e.Length)
                    continue;

                var i = Convert.ToInt32(e[e.IndexOf("[", StringComparison.Ordinal)..].Replace("[", "").Replace("]", ""));

                obj = GetMemberValue(obj, eName);
                if (obj is IList l && i < l.Count)
                {
                    obj = l[i];
                }
                else
                {
                    return null;
                }
            }
            else
            {
                obj = GetMemberValue(obj, e);
                if (obj == null)
                    return null;
            }
        }

        return obj;
    }

    public static object GetMemberValue(object source, string name)
    {
        if (source == null)
            return null;
        var type = source.GetType();

        var f = type.GetField(name, FLAGS);
        if (f != null) return f.GetValue(source);

        var p = type.GetProperty(name, FLAGS);
        return p != null ? p.GetValue(source, null) : null;
    }


    public static System.Type GetPropertyType(this SerializedProperty property)
    {
        System.Type parentType = property.serializedObject.targetObject.GetType();
        System.Reflection.FieldInfo fieldInfo = parentType.GetFieldViaPath(property.propertyPath);
        return fieldInfo?.FieldType;
    }
    public static System.Reflection.FieldInfo GetFieldViaPath(this System.Type type, string path)
    {
        System.Type parentType = type;
        System.Reflection.FieldInfo fieldInfo = type.GetField(path, FLAGS);
        string[] paths = path.Split('.');

        for(int i = 0; i < paths.Length; ++i)
        {
            fieldInfo = parentType.GetField(paths[i], FLAGS);

            if (fieldInfo.FieldType.IsArray)
            {
                parentType = fieldInfo.FieldType.GetElementType();
                i += 2;
                continue;
            }
            if (fieldInfo.FieldType.IsGenericType)
            {
                parentType = fieldInfo.FieldType.GetGenericArguments()[0];
                i += 2;
                continue;
            }

            if (fieldInfo != null)
                parentType = fieldInfo.GetType();
            else
                break;
        }

        return fieldInfo;
    }


    /// <summary>
    ///     Gets the object that a SerializedProperty represents.
    /// </summary>
    /// <remarks>
    ///     Source: 'https://discussions.unity.com/t/get-a-general-object-value-from-serializedproperty/581352/3'.
    /// </remarks>
    public static object GetTargetObjectOfProperty(this SerializedProperty property)
    {
        if (property == null)
            return null;

        // Process the path to make dealing with array elements easier.
        string path = property.propertyPath.Replace(".Array.data[", "[");

        // Loop through each class in the inheritance chain of our target object.
        object obj = property.serializedObject.targetObject;
        string[] elements = path.Split('.');
        foreach(string element in elements)
        {
            if (element.Contains("["))
                // Element is an array element.
            {
                string elementName = element.Substring(0, element.IndexOf("["));
                int index = System.Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
                obj = GetValue_Imp(obj, elementName, index);
            }
            else
                obj = GetValue_Imp(obj, element);
        }

        return obj;
    }


    /// <summary>
    ///     Retrieves the value for the field/property with the given
    ///     <paramref name="name"/> in the object <paramref name="source"/>.
    /// </summary>
    private static object GetValue_Imp(object source, string name)
    {
        if (source == null)
            return null;

        System.Type type = source.GetType();
        while(type != null)
        {
            FieldInfo fieldInfo = type.GetField(name, FLAGS);
            if (fieldInfo != null)
                return fieldInfo.GetValue(source);

            PropertyInfo propertyInfo = type.GetProperty(name, FLAGS | BindingFlags.IgnoreCase);
            if (propertyInfo != null)
                return propertyInfo.GetValue(source, null);

            type = type.BaseType;
        }

        // Failed to get value.
        return null;
    }
    /// <summary>
    ///     Retrieves the value at <paramref name="index"/> within the array field/property with
    ///     the given <paramref name="name"/> in the object <paramref name="source"/>.
    /// </summary>
    private static object GetValue_Imp(object source, string name, int index)
    {
        System.Collections.IEnumerable enumerable = GetValue_Imp(source, name) as System.Collections.IEnumerable;
        if (enumerable == null)
            return null;

        System.Collections.IEnumerator enumerator = enumerable.GetEnumerator();
        for (int i = 0; i <= index; ++i)
            if (!enumerator.MoveNext())
                return null; // Reached the end of the enumerable without reaching our index.

        return enumerator.Current;
    }
}
