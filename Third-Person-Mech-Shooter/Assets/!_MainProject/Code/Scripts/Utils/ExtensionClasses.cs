using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public static class ComponentExtensions
{
    /// <summary>
    ///     Attempt to retrieve the first component of the desired type,
    ///     searching this component's parent's until there are none left or it finds an instance of the desired component type.
    /// </summary>
    public static bool TryGetComponentThroughParents<T>(this Component activeComponent, out T component, bool checkSelf = false)
    {
        component = default(T);

        if (checkSelf && activeComponent.TryGetComponent(out component))
            return true;

        if (activeComponent.transform.parent == null)
            return false;

        return activeComponent.transform.parent.TryGetComponentThroughParents<T>(out component, checkSelf: true);
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


public static class SelectableExtensions
{
    /// <summary>
    ///     Setup the Navigation parameter of this Selectable with the given inputs.
    /// </summary>
    public static void SetNavigation(this Selectable selectable, Selectable onLeft = null, Selectable onRight = null, Selectable onUp = null, Selectable onDown = null)
    {
        // Create and setup the new navigation.
        Navigation navigation = new Navigation();
        navigation.mode = Navigation.Mode.Explicit;
        navigation.selectOnLeft     = onLeft;
        navigation.selectOnRight    = onRight;
        navigation.selectOnUp       = onUp;
        navigation.selectOnDown     = onDown;

        // Set our selectable's navigation.
        selectable.navigation = navigation;
    }
    /// <summary>
    ///     Overrides the elements of the Navigation of the Selectable to the the non-null elements passed.
    /// </summary>
    public static void AddNavigation(this Selectable selectable, Selectable onLeft = null, Selectable onRight = null, Selectable onUp = null, Selectable onDown = null)
    {
        // Create and setup the new navigation.
        Navigation navigation = new Navigation();
        navigation.mode = Navigation.Mode.Explicit;
        navigation.selectOnLeft     = onLeft    ?? selectable.navigation.selectOnLeft;
        navigation.selectOnRight    = onRight   ?? selectable.navigation.selectOnRight;
        navigation.selectOnUp       = onUp      ?? selectable.navigation.selectOnUp;
        navigation.selectOnDown     = onDown    ?? selectable.navigation.selectOnDown;

        // Set our selectable's navigation.
        selectable.navigation = navigation;
    }


    /// <summary>
    ///     Sets a Selectable's navigation mode to <see cref="Navigation.Mode.None"/>.
    /// </summary>
    public static void RemoveNavigation(this Selectable selectable)
    {
        Navigation navigation = new Navigation();
        navigation.mode = Navigation.Mode.None;
        selectable.navigation = navigation;
    }
}


public static class ScrollRectExtensions
{
    /// <inheritdoc cref="ScrollTo(ScrollRect, RectTransform, float)"/>
    public static void ScrollTo(this ScrollRect scrollRect, Transform target, float padding = 0.0f) => scrollRect.ScrollTo(target as RectTransform, padding);
    /// <summary>
    ///     Scrolls a RectTransform along the horizontal and vertical axies to have <paramref name="child"/> be visible with the desired padding.<br/>
    ///     Ignores horizontal movement if the scroll rect has horizontal disabled, with the same corresponding to vertical.
    /// </summary>
    /// <param name="scrollRect"></param>
    /// <param name="child"></param>
    /// <param name="padding"></param>
    public static void ScrollTo(this ScrollRect scrollRect, RectTransform child, float padding = 0.0f)
    {
        Canvas.ForceUpdateCanvases();
        // Cache repeatedly required values)
        float viewportWidth = scrollRect.viewport.rect.width;
        float viewportHeight = scrollRect.viewport.rect.height;
        Vector2 scrollPosition = scrollRect.content.anchoredPosition;

        // Calculate the Top-Left and Bottom-Right corners of our child element.
        Vector2 elementTopLeft = child.anchoredPosition + new Vector2(-child.pivot.x * child.rect.width, (1.0f - child.pivot.y) * child.rect.height);
        Vector2 elementBottomright = elementTopLeft + new Vector2(child.rect.width, -child.rect.height);

        // Calculate the Top-Left and Bottom-Right corners of the visible area (Accounting for padding).
        Vector2 visibleContentTopLeft = new Vector2(
            -scrollPosition.x + padding,
            -scrollPosition.y - padding
            );
        Vector2 visibleContentBottomRight = new Vector2(
            -scrollPosition.x + viewportWidth - padding,
            -scrollPosition.y - viewportHeight + padding
            );

        if (scrollRect.vertical)
        {
            // Handle keeping the child in view vertically.
            float scrollDelta =
                elementTopLeft.y > visibleContentTopLeft.y ? visibleContentTopLeft.y - elementTopLeft.y :
                elementBottomright.y < visibleContentBottomRight.y ? visibleContentBottomRight.y - elementBottomright.y :
                0f;

            scrollPosition.y += scrollDelta;
        }
        if (scrollRect.horizontal)
        {
            // Handle keeping the child in view horizontally.
            float scrollDelta =
                elementTopLeft.x < visibleContentTopLeft.x ? visibleContentTopLeft.x - elementTopLeft.x :
                elementBottomright.x > visibleContentBottomRight.x ? visibleContentBottomRight.x - elementBottomright.x :
                0f;

            scrollPosition.x += scrollDelta;
        }

        // Set the position to keep the child in view.
        scrollRect.content.anchoredPosition = scrollPosition;
    }
}



public static class DictionaryExtensions
{
    /// <summary>
    ///     Returns true if this dictionary contains a null key.
    /// </summary>
    public static bool HasNullKey<TKey, TValue>(this Dictionary<TKey, TValue> dictionary) => dictionary.Keys.Any(t => t.Equals(null));

    /// <summary>
    ///     Removes all null keys from this dictionary, if it has any.
    /// </summary>
    public static void RemoveNullKeys<TKey, TValue>(this Dictionary<TKey, TValue> dictionary)
    {
        if (!dictionary.HasNullKey())
            return;

        // Remove all null keys from the dictionary.
        dictionary = dictionary
            .Where(kvp => !kvp.Key.Equals(null))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }



    /// <summary>
    ///     Tries to retrieve the value for the desired key, creating and returning a new instance if no already exist.
    /// </summary>
    public static TValue GetOrCreateAndReturnValue<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key) where TValue : new()
    {
        if (dict.TryGetValue(key, out TValue value))
            return value;

        TValue newValue = new TValue();
        dict.Add(key, newValue);
        return newValue;
    }


    public static void AddOrSet<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue value)
    {
        if (!dict.TryAdd(key, value))
            dict[key] = value;
    }
    public static void AddRange<TKey, TValue>(this Dictionary<TKey, TValue> dict, ICollection<TKey> keys, TValue value)
    {
        foreach(TKey key in keys)
        {
            Debug.Log("Add: " + key.ToString());
            dict.TryAdd(key, value);
        }
    }


    public static void LogPairs<TKey, TValue>(this Dictionary<TKey, TValue> dict, bool logIndividually = false)
    {
        if (logIndividually)
        {
            foreach(var kvp in dict)
                Debug.Log(kvp.Key.ToString() + ": " + kvp.Value.ToString());
        }
        else
        {
            string output = string.Empty;
            foreach(var kvp in dict)
                output += kvp.Key.ToString() + ": " + kvp.Value.ToString() + "\n";
            
            Debug.Log(output);
        }
    }
}



public static class IListExtensions
{
    /// <summary>
	///     Randomly shuffles the element order of the specified list.
	/// </summary>
    // From: 'https://discussions.unity.com/t/clever-way-to-shuffle-a-list-t-in-one-line-of-c-code/535113/2'.
    public static void Shuffle<T>(this IList<T> listToSort)
    {
        var count = listToSort.Count;
        var last = count - 1;
        for (var i = 0; i < last; ++i)
        {
            var randomIndex = Random.Range(i, count);
            var tmp = listToSort[i];
            listToSort[i] = listToSort[randomIndex];
            listToSort[randomIndex] = tmp;
        }
    }
}


public static class CanvasGroupExtensions
{
    /// <summary>
    ///     Shows a Canvas Group by setting it's alpha to 1 and enabling interactability.
    /// </summary>
    public static void Show(this CanvasGroup canvasGroup)
    {
        canvasGroup.alpha = 1.0f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }
    /// <summary>
    ///     Hides a Canvas Group by setting it's alpha to 0 and disabling interactability.
    /// </summary>
    public static void Hide(this CanvasGroup canvasGroup)
    {
        canvasGroup.alpha = 0.0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
}


public static class ObjectExtensions
{
    public static bool TryCastToType<T>(this object obj, out T castResult) where T : class
    {
        if (obj.GetType().IsAssignableFrom(typeof(T)))
        {
            castResult = default(T);
            return false;
        }

        castResult = (obj as T);
        return true;
    }
}