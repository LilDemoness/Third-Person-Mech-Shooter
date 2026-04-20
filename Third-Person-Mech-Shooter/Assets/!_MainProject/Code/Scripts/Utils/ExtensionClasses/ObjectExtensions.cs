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