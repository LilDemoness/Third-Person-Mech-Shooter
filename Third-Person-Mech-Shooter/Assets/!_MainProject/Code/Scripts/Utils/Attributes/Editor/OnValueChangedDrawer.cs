using System.Reflection;
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
