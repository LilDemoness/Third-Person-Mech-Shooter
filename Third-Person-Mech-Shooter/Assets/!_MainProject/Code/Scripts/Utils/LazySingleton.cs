using UnityEngine;

/// <summary>
///     A Singleton for a type that doesn't exist in a scene until access is first attempted.<br/>
///     Inheritors of this type should NOT be added to objects or prefabs manually.
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class LazySingleton<T> : MonoBehaviour where T : Component
{
    private static T s_instance;
    public static T Instance
    {
        get
        {
            if (s_instance == null)
            {
                s_instance = new GameObject($"Singleton-{typeof(T).GetType().ToString()}").AddComponent<T>();
                DontDestroyOnLoad(s_instance.gameObject);
            }

            return s_instance;
        }
    }
}