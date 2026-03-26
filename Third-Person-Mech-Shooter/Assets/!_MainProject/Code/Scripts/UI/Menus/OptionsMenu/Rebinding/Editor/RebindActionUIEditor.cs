using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;

/// <summary>
///   A custom inspector for <see cref="RebindActionUI"/> which provides a more convenient way for
///     picking the binding which to rebind.  
/// </summary>
[CustomEditor(typeof(RebindActionUI))]
public class RebindActionUIEditor : Editor
{
    protected void OnEnable()
    {
        m_ActionProperty = serializedObject.FindProperty("m_action");
        m_BindingIdProperty = serializedObject.FindProperty("m_bindingId");
        m_IncludeBindingInNameProperty = serializedObject.FindProperty("_includeBindingInName");
        m_ActionLabelProperty = serializedObject.FindProperty("m_actionNameLabel");
        m_BindingTextProperty = serializedObject.FindProperty("m_bindingText");
        m_RebindOverlayProperty = serializedObject.FindProperty("m_rebindOverlay");
        m_RebindTextProperty = serializedObject.FindProperty("m_rebindText");
        m_DefaultInputActionsProperty = serializedObject.FindProperty("_defaultInputActions");
        m_UpdateBindingUIEventProperty = serializedObject.FindProperty("m_updateBindingUIEvent");
        m_RebindStartEventProperty = serializedObject.FindProperty("m_rebindStartEvent");
        m_RebindStopEventProperty = serializedObject.FindProperty("m_rebindStopEvent");
        m_DisplayStringOptionsProperty = serializedObject.FindProperty("m_displayStringOptions");

        RefreshBindingOptions();
    }

    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();

        // Binding section.
        EditorGUILayout.LabelField(m_BindingLabel, Styles.boldLabel);
        using (new EditorGUI.IndentLevelScope())
        {
            EditorGUILayout.PropertyField(m_ActionProperty);

            var newSelectedBinding = EditorGUILayout.Popup(m_BindingLabel, m_SelectedBindingOption, m_BindingOptions);
            if (newSelectedBinding != m_SelectedBindingOption)
            {
                var bindingId = m_BindingOptionValues[newSelectedBinding];
                m_BindingIdProperty.stringValue = bindingId;
                m_SelectedBindingOption = newSelectedBinding;
            }

            var optionsOld = (InputBinding.DisplayStringOptions)m_DisplayStringOptionsProperty.intValue;
            var optionsNew = (InputBinding.DisplayStringOptions)EditorGUILayout.EnumFlagsField(m_DisplayOptionsLabel, optionsOld);
            if (optionsOld != optionsNew)
                m_DisplayStringOptionsProperty.intValue = (int)optionsNew;
        }

        // UI section.
        EditorGUILayout.Space();
        EditorGUILayout.LabelField(m_UILabel, Styles.boldLabel);
        using (new EditorGUI.IndentLevelScope())
        {
            EditorGUILayout.PropertyField(m_IncludeBindingInNameProperty);
            EditorGUILayout.PropertyField(m_ActionLabelProperty);
            EditorGUILayout.PropertyField(m_BindingTextProperty);
            EditorGUILayout.PropertyField(m_RebindOverlayProperty);
            EditorGUILayout.PropertyField(m_RebindTextProperty);
            EditorGUILayout.PropertyField(m_DefaultInputActionsProperty);
        }

        // Events section.
        EditorGUILayout.Space();
        EditorGUILayout.LabelField(m_EventsLabel, Styles.boldLabel);
        using (new EditorGUI.IndentLevelScope())
        {
            EditorGUILayout.PropertyField(m_RebindStartEventProperty);
            EditorGUILayout.PropertyField(m_RebindStopEventProperty);
            EditorGUILayout.PropertyField(m_UpdateBindingUIEventProperty);
        }

        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            RefreshBindingOptions();
        }
    }

    protected void RefreshBindingOptions()
    {
        var actionReference = (InputActionReference)m_ActionProperty.objectReferenceValue;
        var action = actionReference?.action;

        if (action == null)
        {
            m_BindingOptions = new GUIContent[0];
            m_BindingOptionValues = new string[0];
            m_SelectedBindingOption = -1;
            return;
        }

        var bindings = action.bindings;
        var bindingCount = bindings.Count;

        m_BindingOptions = new GUIContent[bindingCount];
        m_BindingOptionValues = new string[bindingCount];
        m_SelectedBindingOption = -1;

        var currentBindingId = m_BindingIdProperty.stringValue;
        for (var i = 0; i < bindingCount; ++i)
        {
            var binding = bindings[i];
            var bindingId = binding.id.ToString();
            var haveBindingGroups = !string.IsNullOrEmpty(binding.groups);

            // If we don't have a binding groups (control schemes), show the device that if there are, for example,
            // there are two bindings with the display string "A", the user can see that one is for the keyboard
            // and the other for the gamepad.
            var displayOptions =
                InputBinding.DisplayStringOptions.DontUseShortDisplayNames | InputBinding.DisplayStringOptions.IgnoreBindingOverrides;
            if (!haveBindingGroups)
                displayOptions |= InputBinding.DisplayStringOptions.DontOmitDevice;

            // Create display string.
            var displayString = action.GetBindingDisplayString(i, displayOptions);

            // If binding is part of a composite, include the part name.
            if (binding.isPartOfComposite)
                displayString = $"{ObjectNames.NicifyVariableName(binding.name)}: {displayString}";

            // Some composites use '/' as a separator. When used in popup, this will lead to to submenus. Prevent
            // by instead using a backlash.
            displayString = displayString.Replace('/', '\\');

            // If the binding is part of control schemes, mention them.
            if (haveBindingGroups)
            {
                var asset = action.actionMap?.asset;
                if (asset != null)
                {
                    var controlSchemes = string.Join(", ",
                        binding.groups.Split(InputBinding.Separator)
                            .Select(x => asset.controlSchemes.FirstOrDefault(c => c.bindingGroup == x).name));

                    displayString = $"{displayString} ({controlSchemes})";
                }
            }

            m_BindingOptions[i] = new GUIContent(displayString);
            m_BindingOptionValues[i] = bindingId;

            if (currentBindingId == bindingId)
                m_SelectedBindingOption = i;
        }
    }

    private SerializedProperty m_ActionProperty;
    private SerializedProperty m_BindingIdProperty;
    private SerializedProperty m_IncludeBindingInNameProperty;
    private SerializedProperty m_ActionLabelProperty;
    private SerializedProperty m_BindingTextProperty;
    private SerializedProperty m_DefaultInputActionsProperty;
    private SerializedProperty m_RebindOverlayProperty;
    private SerializedProperty m_RebindTextProperty;
    private SerializedProperty m_RebindStartEventProperty;
    private SerializedProperty m_RebindStopEventProperty;
    private SerializedProperty m_UpdateBindingUIEventProperty;
    private SerializedProperty m_DisplayStringOptionsProperty;

    private GUIContent m_BindingLabel = new GUIContent("Binding Id");
    private GUIContent m_DisplayOptionsLabel = new GUIContent("Display Options");
    private GUIContent m_UILabel = new GUIContent("UI");
    private GUIContent m_EventsLabel = new GUIContent("Events");
    private GUIContent[] m_BindingOptions;
    private string[] m_BindingOptionValues;
    private int m_SelectedBindingOption;

    private static class Styles
    {
        public static GUIStyle boldLabel = new GUIStyle("MiniBoldLabel");
    }
}
