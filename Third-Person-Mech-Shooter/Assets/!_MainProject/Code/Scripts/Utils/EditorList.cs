#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Utils
{
    public static class EditorList
    {
        const float LIST_SIZE_WIDTH = 30.0f;
        const float MINI_BUTTON_WIDTH = 20.0f;

        private static Color s_backgroundColor = new Color(0.25f, 0.25f, 0.25f, 1.0f);

        private static GUIContent s_moveButtonContent = new GUIContent("\u21b4", "Move Down");
        private static GUIContent s_duplicateButtonContent = new GUIContent("+", "Duplicate");
        private static GUIContent s_deleteButtonContent = new GUIContent("-", "Delete");

        private static GUIContent s_addButtonContent = new GUIContent("+", "Add Element");
        private static GUIContent s_removeButtonContent = new GUIContent("-", "Remove Element");


        /// <summary>
        ///     Draw a <paramref name="list"/> with the passed <paramref name="options"/>, starting with the given <paramref name="rect"/>.
        /// </summary>
        public static void DrawList(Rect rect, SerializedProperty list, GUIContent label, EditorListOptions options)
        {
            ShowFoldout(rect, list, label);
            HandleListFoldout(list, options);
        }
        /// <summary>
        ///     Draw a <paramref name="list"/> with the passed <paramref name="options"/>.
        /// </summary>
        public static void DrawList(SerializedProperty list, GUIContent label, EditorListOptions options)
        {
            ShowFoldoutLabel(list, label);
            HandleListFoldout(list, options);
        }

        /// <summary>
        ///     Draw a foldout (With label & size property) at the given rect, passing the value into 'isExpanded' of <paramref name="listProp"/>.
        /// </summary>
        private static void ShowFoldout(Rect rect, SerializedProperty property, GUIContent label)
        {
            // Draw the list's name label.
            Rect foldoutRect = new Rect(rect);
            foldoutRect.width -= LIST_SIZE_WIDTH;
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

            // Draw the 'Array Size' field.
            Rect arraySizeRect = new Rect(rect);
            arraySizeRect.position += Vector2.right * (foldoutRect.width);
            arraySizeRect.width = LIST_SIZE_WIDTH;
            EditorGUI.PropertyField(arraySizeRect, property.FindPropertyRelative("Array.size"), new GUIContent(""));
        }
        /// <summary>
        ///     Draw a foldout property (With label & size property), passing the value into 'isExpanded' of <paramref name="listProp"/>.
        /// </summary>
        private static void ShowFoldoutLabel(SerializedProperty listProp, GUIContent label)
        {
            // Draw the list's name label.
            listProp.isExpanded = EditorGUILayout.Foldout(listProp.isExpanded, label, true);

            // Draw the 'Array Size' field.
            EditorGUILayout.PropertyField(listProp.FindPropertyRelative("Array.size"), new GUIContent(""));
        }

        /// <summary>
        ///     Handles the foldout section of a list (Not the foldout's label).
        /// </summary>
        private static void HandleListFoldout(SerializedProperty listProp, EditorListOptions options)
        {
            if (!listProp.isExpanded)
                return;

            ++EditorGUI.indentLevel;

            ShowElementsWithBackground(listProp, options);

            if (options.HasFlag(EditorListOptions.ListButtons))
            {
                EditorGUILayout.Space(listProp.arraySize > 0 ? -1.0f : 1.0f); // Have the outline of our elements merge with the outline of our buttons.
                ShowListButtonsWithBackground(listProp);
            }

            EditorGUILayout.Space(5.0f);
            --EditorGUI.indentLevel;
        }

        private static void ShowElements(SerializedProperty listProp, EditorListOptions options)
        {
            bool showElementLabels = options.HasFlag(EditorListOptions.ElementLabels);
            bool showElementButtons = options.HasFlag(EditorListOptions.ElementButtons);


            Rect showElementsRect;
            for (int i = 0; i < listProp.arraySize; ++i)
            {
                if (showElementButtons)
                {
                    showElementsRect = EditorGUILayout.BeginVertical();
                    // Draw the element's buttons before drawing the property as Unity's IMGUI elements consume click events in the priority they were created (Older elements consume clicks first).
                    ShowElementButtons(showElementsRect, listProp, ref i);

                    if (i == -1) // We've removed the only element, meaning that there are none we wish to draw.
                        continue;
                }

                // Draw the list element.
                if (showElementLabels)
                    EditorGUILayout.PropertyField(listProp.GetArrayElementAtIndex(i), true);
                else
                    EditorGUILayout.PropertyField(listProp.GetArrayElementAtIndex(i), GUIContent.none, true);

                if (showElementButtons)
                    EditorGUILayout.EndVertical();
            }
        }
        /// <inheritdoc cref="ShowElements(SerializedProperty, EditorListOptions)"/>
        private static void ShowElementsWithBackground(SerializedProperty listProp, EditorListOptions options)
        {
            Rect verticalRect = EditorGUILayout.BeginVertical();

            // Draw a rect as a background.
            EditorGUI.DrawRect(new Rect(verticalRect.x - 1.0f, verticalRect.y, verticalRect.width + 2, verticalRect.height + 2), Color.black);  // Outline.
            EditorGUI.DrawRect(new Rect(verticalRect.x, verticalRect.y + 1.0f, verticalRect.width, verticalRect.height), s_backgroundColor);    // Background.

            EditorGUILayout.Space(2.0f);

            ShowElements(listProp, options);

            EditorGUILayout.EndVertical();
        }


        /// <summary>
        ///     Draws the 'MoveNext', 'DuplicateElement', and 'DeleteElement' buttons for an element within a list.
        /// </summary>
        /// <param name="list"> The SerializedProperty referencing the list/</param>
        /// <param name="index"> A reference to the index of the element. Decreased if an element is removed.</param>
        private static void ShowElementButtons(Rect rect, SerializedProperty list, ref int index)
        {
            const float RIGHT_EDGE_OFFSET = 3.0f;
            float startingX = EditorGUIUtility.currentViewWidth - (MINI_BUTTON_WIDTH * 3) - RIGHT_EDGE_OFFSET;

            //Rect moveRect = new Rect(rect.x + (rect.width - MINI_BUTTON_WIDTH * 3), rect.y, MINI_BUTTON_WIDTH, rect.height);
            Rect moveRect = new Rect(startingX, rect.y, MINI_BUTTON_WIDTH, rect.height);
            Rect duplicateRect = new Rect(startingX + MINI_BUTTON_WIDTH, rect.y, MINI_BUTTON_WIDTH, rect.height);
            Rect deleteRect = new Rect(startingX + MINI_BUTTON_WIDTH * 2.0f, rect.y, MINI_BUTTON_WIDTH, rect.height);

            if (GUI.Button(moveRect, s_moveButtonContent, EditorStyles.miniButtonLeft))
                list.MoveArrayElement(index, index + 1);

            if (GUI.Button(duplicateRect, s_duplicateButtonContent, EditorStyles.miniButtonMid))
                list.InsertArrayElementAtIndex(index);

            if (GUI.Button(deleteRect, s_deleteButtonContent, EditorStyles.miniButtonRight))
            {
                list.DeleteArrayElementAtIndex(index);
                --index;
            }
        }


        /// <summary>
        ///     Draws the 'AddElement' and 'RemoveElement' buttons for a list.
        /// </summary>
        /// <remarks>
        ///     'AddElement' creates a new instance when adding an array element, rather than
        ///     utilising Unity's default implementation, in order to allow newly added values
        ///     to be populated from our default values, rather than Unity's default implementation
        ///     which sets all element values to a copy of the last element's, or reset the values to
        ///     defaults of their type (Rather than what is in the constructor).
        ///     https://discussions.unity.com/t/values-passed-to-constructor-of-a-class-are-lost/870910/2
        /// </remarks>
        private static void ShowListButtons(Rect rect, SerializedProperty listProp)
        {
            Rect addRect = new Rect(rect);
            addRect.width = MINI_BUTTON_WIDTH;
            if (GUI.Button(addRect, s_addButtonContent, EditorStyles.miniButtonLeft))
                // Add a new element to the list.
            {
                listProp.arraySize += 1;

                // Ensure that the added element uses the constructor of the desired type, rather than Unity's serializer overriding its values.
                // (The serializer would otherwise would copy the values of the previous element in the list if one exists, otherwise ignore our desired values and resetting them all to default(T)).
                listProp.GetArrayElementAtIndex(listProp.arraySize - 1).boxedValue = System.Activator.CreateInstance(listProp.GetPropertyType().GenericTypeArguments[0]);

                listProp.serializedObject.ApplyModifiedProperties();
            }

            Rect removeRect = new Rect(rect);
            removeRect.x += MINI_BUTTON_WIDTH;
            removeRect.width = MINI_BUTTON_WIDTH;
            if (GUI.Button(removeRect, s_removeButtonContent, EditorStyles.miniButtonRight))
                // Remove the last element from the list.
            {
                listProp.arraySize -= 1;
                listProp.serializedObject.ApplyModifiedProperties();
            }
        }
        /// <inheritdoc cref="ShowListButtons(Rect, SerializedProperty)"/>
        private static void ShowListButtonsWithBackground(SerializedProperty listProp)
        {
            const float HORIZONTAL_MARGIN = 4.0f;
            const float LIST_BUTTON_RIGHT_OFFSET = 15.0f;

            Rect rect = EditorGUILayout.BeginHorizontal();
            rect.x += rect.width - (MINI_BUTTON_WIDTH * 2.0f + HORIZONTAL_MARGIN + LIST_BUTTON_RIGHT_OFFSET);
            rect.width = MINI_BUTTON_WIDTH * 2.0f + HORIZONTAL_MARGIN;

            // Draw a container for the buttons.
            Rect outlineRect = new Rect(rect.x - 1.0f, rect.y, rect.width + 2, rect.height + 2);
            Rect backgroundRect = new Rect(rect.x, rect.y + 1.0f, rect.width, rect.height);
            EditorGUI.DrawRect(outlineRect, Color.black);           // Outline.
            EditorGUI.DrawRect(backgroundRect, s_backgroundColor);  // Background.

            // Draw the List Buttons.
            Rect listButtonsRect = backgroundRect;
            listButtonsRect.x += HORIZONTAL_MARGIN * 0.5f;
            listButtonsRect.y += EditorGUIUtility.standardVerticalSpacing;
            listButtonsRect.height -= EditorGUIUtility.standardVerticalSpacing * 2.0f;
            ShowListButtons(listButtonsRect, listProp);

            // Ensure that the editor accounts for the size of our buttons (Dont manually as we're not using EditorGUILayout for drawing the buttons, so Uity doesn't automatically account for it).
            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing * 2.0f);
            EditorGUILayout.EndHorizontal();
        }



        [System.Serializable, System.Flags]
        public enum EditorListOptions
        {
            None = 0,

            ListSize = 1 << 0,
            ListLabel = 1 << 1,
            ListButtons = 1 << 2,

            ElementLabels = 1 << 3,
            ElementButtons = 1 << 4,

            Default = ListSize | ListLabel | ListButtons | ElementLabels,
            NoElementLabels = ListSize | ListLabel | ListButtons,
            All = Default | ElementButtons,
        }
    }
}
#endif