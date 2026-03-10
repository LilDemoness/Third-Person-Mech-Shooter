using Unity.Services.Multiplayer;

/// <summary>
///     Holds constant values for shared parameters.
/// </summary>
public static class Constants
{
    public const float TARGET_ESTIMATION_RANGE = 150.0f;


    public const SortField GAME_MODE_SORT_FIELD = SortField.StringIndex1;
    public const PropertyIndex GAME_MODE_PROPERTY_INDEX = PropertyIndex.String1;

    public const SortField MAP_SORT_FIELD = SortField.StringIndex2;
    public const PropertyIndex MAP_PROPERTY_INDEX = PropertyIndex.String2;
}