using System;
using System.Diagnostics;
using UnityEngine;

/// <summary>
///     An attribute that calls a function when a variable's value is changed through the inspector only.
/// </summary>
[DontApplyToListElements] // Don't trigger for list elements, just the list itself.
[AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = true)] // Always valid, can be applied multiple times, & apply to children.
[Conditional("UNITY_EDITOR")] // Only include in the editor.
public class OnValueChangedAttribute : PropertyAttribute
{
    /// <summary>
    ///     A resolved string that defines the action to perform when the value is changed, such as an expression or method invocation.
    /// </summary>
    public string Action;

    /// <summary>
    ///     Whether to perform the action when a child value of the property is changed.
    /// </summary>
    public bool IncludeChildren;

    /// <summary>
    ///     Whether to perform the action when an undo or a redo event occurs in the editor.<br/>
    ///     True by default.
    /// </summary>
    //public bool InvokeOnUndoRedo = true;

    /// <summary>
    ///     Whether to perform the action when the property is initialised.<br/>
    ///     
    ///     This will generally happen when the property is first viewed/queried
    ///     (I.E. When the inspector is first opened, or when its containing foldout
    ///     is first opened, etc), and whenever its type or a parent type changes,
    ///     or when it is otherwise forced to rebuild.
    /// </summary>
    //public bool InvokeOnInitialize;


    /// <summary>
    ///     Adds a callback for when the property's value is changed in the inspector.
    /// </summary>
    /// <param name="action"> A resolved string that defines the action to perform when the value is changed, such as an expression or method invocation.</param>
    /// <param name="includeChildren"> Whether to perform the action when a child value of the property is changed.</param>
    public OnValueChangedAttribute(string action, bool includeChildren = false)
    {
        Action = action;
        IncludeChildren = includeChildren;
    }
}



/// <summary>
///     Applied to other Attributes to signal that they should
///     be only applied to the list and not its individual elements.
///     
///     NOT IMPLEMENTED YET.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class DontApplyToListElementsAttribute : Attribute
{}