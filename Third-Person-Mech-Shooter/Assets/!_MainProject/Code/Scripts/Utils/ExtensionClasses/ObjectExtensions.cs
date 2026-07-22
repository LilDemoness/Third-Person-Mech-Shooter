public static class ObjectExtensions
{
    public static bool TryCastToType<T>(this object obj, out T castResult)
    {
        if (!obj.GetType().IsAssignableFrom(typeof(T)))
        {
            castResult = default(T);
            return false;
        }

        castResult = (T)obj;
        return true;
    }
}