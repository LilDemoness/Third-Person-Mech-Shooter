using UnityEngine;

public static class ComponentExtensions
{
    /// <summary>
    ///     Attempt to retrieve the first component of the desired type, searching this component's
    ///     parent's until there are none left or it finds an instance of the desired component type.<br/>
    ///     Also searches the origin transform.
    /// </summary>
    public static bool TryGetComponentThroughParents<T>(this Component activeComponent, out T component)
    {
        component = default(T);

        if (activeComponent.TryGetComponent(out component))
            return true;

        if (activeComponent.transform.parent == null)
            return false;

        return activeComponent.transform.parent.TryGetComponentThroughParents<T>(out component);
    }
    /// <summary>
    ///     Attempt to retrieve the first component of the desired type, searching this component's
    ///     parent's until there are none left or it finds an instance of the desired component type.<br/>
    ///     Doesn't search the origin transform.
    /// </summary>
    public static bool TryGetComponentThroughParentsExclusive<T>(this Component activeComponent, out T component)
    {
        component = default(T);

        if (activeComponent.transform.parent == null)
            return false;

        return activeComponent.transform.parent.TryGetComponentThroughParents<T>(out component);
    }
    public static bool TryGetComponentInChildren<T>(this Component activeComponent, out T component)
    {
        // This object has the component.
        if (activeComponent.TryGetComponent(out component))
            return true;

        // Search our children for the component.
        foreach(Transform child in activeComponent.transform)
        {
            if (child.TryGetComponentInChildren<T>(out component))
            {
                // This child has our component.
                return true;
            }
        }

        // Failed to find a child with the component.
        component = default(T);
        return false;
    }

    /// <summary>
    ///     Returns true if this component is any child of (Or on the same object of) the object with the passed transform.
    /// </summary>
    public static bool IsChildOf(this Component activeComponent, Transform transformToCheck)
    {
        if (activeComponent.transform == transformToCheck)
            return true;

        if (activeComponent.transform.parent == null)
            return false;

        return activeComponent.transform.parent.IsChildOf(transformToCheck);
    }
    /// <summary>
    ///     Returns true if this component is any parent of (Or on the same object of) the object with the passed transform.
    /// </summary>
    public static bool IsParentOf(this Component activeComponent, Transform transformToCheck)
    {
        if (activeComponent.transform == transformToCheck)
            return true;

        if (activeComponent.transform.childCount == 0)
            return false;

        // Check each child.
        for(int i = 0; i < activeComponent.transform.childCount; ++i)
        {
            if (activeComponent.transform.GetChild(i).IsParentOf(transformToCheck))
                return true;    // This child contains the transformToCheck
        }

        // No child contains the transformToCheck.
        return false;
    }



    /// <summary>
    ///     Returns true if this component is any child, any parent, or is on the same object as the passed transform.
    /// </summary>
    public static bool IsParentOrChildOf(this Component activeComponent, Transform transformToCheck) => activeComponent.IsParentOf(transformToCheck) || activeComponent.IsChildOf(transformToCheck);
}
