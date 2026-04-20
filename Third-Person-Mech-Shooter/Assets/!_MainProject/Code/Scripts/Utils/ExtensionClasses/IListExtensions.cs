using System.Collections.Generic;
using UnityEngine;

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
