using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace Gameplay.Passives
{
    [CustomPropertyDrawer(typeof(AlterDamageTakenStatisticEffect), true)]
    public class AlterDamageTakenStatisticEffectDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, true) + 5.0f;
        }


        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            {
			    Rect changeTypeLabel = new Rect(position);
			    changeTypeLabel.height = EditorGUIUtility.singleLineHeight;

                GUIStyle style = new GUIStyle()
                {
                    richText = true,
                };

                float alterationValue = property.FindPropertyRelative("_alterationValue").floatValue;
                string outputValue = "<u>" + (
                alterationValue > 1.0f ?
                    outputValue = $"<b>Vulnerability</b>: {(alterationValue - 1.0f) * 100.0f}%"
                : alterationValue > 0.0f ?
                    outputValue = $"<b>Resistance</b>: {(1.0f - alterationValue) * 100.0f}%"
                : outputValue = "<b>Immunity</b>"
                ) + "</u>";

                EditorGUI.LabelField(changeTypeLabel, outputValue, style);
            }

            Rect childPosition = position;
            const float SPACING = 5.0f;
            childPosition.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing + SPACING;
            foreach (SerializedProperty childProperty in GetChildProperties(property))
            {
                float height = EditorGUI.GetPropertyHeight(childProperty, new GUIContent(childProperty.displayName, childProperty.tooltip), true);
                childPosition.height = height;
                EditorGUI.PropertyField(childPosition, childProperty, true);

                childPosition.y += height + EditorGUIUtility.standardVerticalSpacing;
            }

            EditorGUI.EndProperty();
        }

        public static IEnumerable<SerializedProperty> GetChildProperties(SerializedProperty parent, int depth = 1)
        {
            parent = parent.Copy();

            int depthOfParent = parent.depth;
            var enumerator = parent.GetEnumerator();

            while (enumerator.MoveNext())
            {
                if (enumerator.Current is not SerializedProperty childProperty)
                {
                    continue;
                }
                if (childProperty.depth > (depthOfParent + depth))
                {
                    continue;
                }
                yield return childProperty.Copy();
            }
        }
    }
}