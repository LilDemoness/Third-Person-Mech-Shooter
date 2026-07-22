using System;
using System.Diagnostics;
using UnityEngine;

[DontApplyToListElements]
[Conditional("UNITY_EDITOR")]
public class ShowIfAttribute : PropertyAttribute
{
    /// <summary>
    ///     The condition to evaluate.
    /// </summary>
    public string Condition;

    /// <summary>
    ///     An optional value to compare the condition against, rather than just evaluating if it is true.
    /// </summary>
    public object Value;


    public ShowIfAttribute(string condition)
    {
        Condition = condition;
        Value = null;
    }
    public ShowIfAttribute(string condition, object optionalValue)
    {
        Condition = condition;
        Value = optionalValue;
    }
}