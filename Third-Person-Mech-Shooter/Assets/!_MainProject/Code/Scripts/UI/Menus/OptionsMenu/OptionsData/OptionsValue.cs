using System;
using UnityEngine;


/// <summary>
///     Used soely so that we can reference <see cref="OptionsValue{T}"/> without needing to specify a type.<br/>
///     Don't inherit from this class directly.
/// </summary>
/// <remarks>
///     Values such as 'Initialised' and 'm_Value' (See <see cref="OptionsValue{T}"/>) are reset upon the editor mode changing while in the editor.
/// </remarks>
public abstract class BaseOptionsValue : ScriptableObject
{
    [field: NonSerialized] public bool Initialised { get; protected set; }


    public virtual void Init()
    {
        if (Initialised)
            return;
        Initialised = true;
        Debug.Log("Init");

        LoadFromPrefs();
    }
    public abstract void ResetValue();
    public abstract void SaveToPrefs();
    public abstract void LoadFromPrefs();

    protected abstract string GetValueString();


    private event System.Action onValueChanged;
    protected void InvokeOnValueChanged() => onValueChanged?.Invoke();

    /// <summary>
    ///     Subscribes to this instance's 'onValueChanged' event with the given callback.
    ///     Triggers the passed callback if this instance has been initialised.
    /// </summary>
    /// <param name="callback"> The function to subscribe to the 'onValueChanged' event.</param>
    public void SubscribeToOnValueChangedAndTryTrigger(System.Action callback)
    {
        onValueChanged += callback;
        if (Initialised)
            callback?.Invoke();
    }
    /// <summary>
    ///     Unsubscribes the passed callback from this instance's 'onValueChanged' event.
    /// </summary>
    public void UnsubscribeFromOnValueChanged(System.Action callback) => onValueChanged -= callback;


#if UNITY_EDITOR

    /// <summary>
    ///     Cleans up values after/before PlayMode while playing in the editor, ensuring that no unwanted data persists between sessions.
    /// </summary>
    public virtual void Editor_ResetValuesNoNotify() => Initialised = false;
    


    [UnityEditor.CustomEditor(typeof(BaseOptionsValue), true)]
    public class OptionsValueEditor : UnityEditor.Editor
    {
        // Ensure that the inspector updates when the value of the SO is changed (Otherwise it only updates on selection change/hover).
        private void OnEnable() => (target as BaseOptionsValue).onValueChanged += Repaint;
        private void OnDisable() => (target as BaseOptionsValue).onValueChanged -= Repaint;
        

        public override void OnInspectorGUI()
        {
            UnityEditor.EditorGUILayout.LabelField("Is Initialised?: " + ((target as BaseOptionsValue).Initialised ? "True" : "False"));
            UnityEditor.EditorGUILayout.LabelField("Value: " + (target as BaseOptionsValue).GetValueString());
            DrawHorizontalLine();
            base.OnInspectorGUI();
        }


        private void DrawHorizontalLine()
        {
            // Create the Horizontal Line.
            GUIStyle horizontalLine = new GUIStyle();
            horizontalLine.normal.background = UnityEditor.EditorGUIUtility.whiteTexture;
            horizontalLine.margin = new RectOffset(0, 0, 0, 4);
            horizontalLine.fixedHeight = 1;

            // Display the Horizontal Line.
            Color previousColour = GUI.color;
            GUI.color = Color.black;
            GUILayout.Box(GUIContent.none, horizontalLine);
            GUI.color = previousColour;
        }
    }

#endif
}

/// <summary>
///     The generic base for a ScriptableObject container for the value of an option in the Options Menu.
///     
///     Allows for subscribing to OnChange events without needing tight coupling to the options menu or a manager class,
///     along with containing methods for saving & loading data and having default values.
/// </summary>
public abstract class OptionsValue<T> : BaseOptionsValue
{
    //[NonSerialized] protected T m_Value;
    [NonSerialized] private T m_Value;
    public T Value
    {
        get => m_Value;
        set => SetValue(value);
    }


    [field: SerializeField] protected string PrefsIdentifier { get; private set; }

    [field: Space(5)]
    [field: SerializeField] protected T DefaultValue
    {
        get;
#if UNITY_EDITOR
        set;
#else
        private set;
#endif
    }


    protected override string GetValueString() => m_Value.ToString();
    public override void ResetValue()
    {
        m_Value = DefaultValue;
        InvokeOnValueChanged();
    }



    public abstract void SetValue(T newValue);
    protected virtual void SetValueNoNotifyNoChecks(T newValue) => m_Value = newValue;


#if UNITY_EDITOR

    public override void Editor_ResetValuesNoNotify()
    {
        base.Editor_ResetValuesNoNotify();
        m_Value = default(T);
    }

#endif
}
