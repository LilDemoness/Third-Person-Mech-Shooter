using UnityEngine.UI;

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
