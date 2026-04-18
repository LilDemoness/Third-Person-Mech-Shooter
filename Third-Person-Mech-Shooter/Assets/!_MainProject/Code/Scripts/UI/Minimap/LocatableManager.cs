using System.Collections.Generic;

namespace Gameplay.UI.Minimap
{
    /// <summary>
    ///     Holds a list of all <see cref="BaseLocatable"/> instances and provides events for when any are added or removed.
    /// </summary>
    public static class LocatableManager
    {
        public static HashSet<BaseLocatable> Locatables { get; private set; } = new();
        public static System.Action<BaseLocatable> OnLocatableAdded;
        public static System.Action<BaseLocatable> OnLocatableRemoved;


        public static void Register(BaseLocatable locatable)
        {
            if (Locatables.Contains(locatable))
                return;

            Locatables.Add(locatable);
            OnLocatableAdded?.Invoke(locatable);
        }

        public static void Deregister(BaseLocatable locatable)
        {
            if (!Locatables.Contains(locatable))
                return;

            Locatables.Remove(locatable);
            OnLocatableRemoved?.Invoke(locatable);
        }
    }
}