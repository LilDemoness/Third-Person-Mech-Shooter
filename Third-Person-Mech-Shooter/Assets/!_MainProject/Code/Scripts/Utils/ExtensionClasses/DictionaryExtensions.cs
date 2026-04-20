using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
