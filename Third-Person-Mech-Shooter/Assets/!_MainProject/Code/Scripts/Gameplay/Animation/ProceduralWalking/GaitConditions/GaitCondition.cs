namespace Gameplay.Animations.ProceduralAnimations
{
    [System.Serializable]
    public abstract class GaitCondition
    {
        public abstract bool TestCondition(MechBody body);


        #if UNITY_EDITOR

        /// <summary>
        ///     If true, then this Condition conflicts with <paramref name="conditionToCheck"/>.
        /// </summary>
        public abstract bool Editor_HasConflict(GaitCondition conditionToCheck, out string errorMessage);

        #endif
    }
}