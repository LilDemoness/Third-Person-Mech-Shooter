using UnityEngine;

/// <summary>
///     An attribute that makes a public/serialized field visible in the inspector only editable during Edit Mode, not during Play Mode.
/// </summary>
public class ReadOnlyInPlayModeAttribute : PropertyAttribute
{ }