public static class ObjectExtensions
{
    public static bool TryCastToType<T>(this object obj, out T castResult)
    {
        if (!typeof(T).IsAssignableFrom(obj.GetType()))
        {
            castResult = default(T);
            return false;
        }

        castResult = (T)obj;
        return true;
    }
}