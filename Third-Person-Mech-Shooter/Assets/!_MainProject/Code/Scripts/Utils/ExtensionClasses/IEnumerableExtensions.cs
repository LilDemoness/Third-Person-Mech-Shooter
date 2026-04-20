using System.Collections.Generic;
using System.Linq;

public static class IEnumerableExtensions
{
    /// <summary>
    ///     Returns true if the IEnumerable is null or has no values.
    /// </summary>
    public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable) => enumerable == null || !enumerable.Any();

    public static bool TryGetFirst<T>(this IEnumerable<T> enumerable, out T value)
    {
        if (enumerable.IsNullOrEmpty())
        {
            value = default(T);
            return false;
        }

        value = enumerable.First();
        return true;
    }
}
