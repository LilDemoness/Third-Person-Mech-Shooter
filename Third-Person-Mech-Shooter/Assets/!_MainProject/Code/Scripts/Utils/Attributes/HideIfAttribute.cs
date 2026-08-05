using System;
using System.Diagnostics;
using UnityEngine;

[DontApplyToListElements]
[Conditional("UNITY_EDITOR")]
public class HideIfAttribute : PropertyAttribute
{
    /// <summary>
    ///     The condition to evaluate.<br/>
    ///     E.g. The name of a property or non-void function.
    /// </summary>
    public string Condition;

    /// <summary>
    ///     An optional value to compare the condition against, rather than just evaluating if it is true.
    /// </summary>
    public object Value;


    public HideIfAttribute(string condition)
    {
        Condition = condition;
        Value = null;
    }
    public HideIfAttribute(string condition, object optionalValue)
    {
        Condition = condition;
        Value = optionalValue;
    }
}