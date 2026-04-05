using Gameplay.GameplayObjects.Character;

namespace Gameplay.Passives
{
    /// <summary>
    ///     A condition to determine when a <see cref="PassiveEffect"/> triggers.
    /// </summary>
    [System.Serializable]
    public abstract class PassiveCondition
    {
        /// <summary>
        ///     Returns true if the Passive Effect should trigger.
        /// </summary>
        /// <param name="character"> The ServerCharacter this passive call is to be applied to.</param>
        /// <param name="lifetime"> The total Lifetime of the effect.</param>
        /// <param name="timeSinceLastUpdateCall"> The time (In Seconds) since the last 'Update()' call of the passive. NOT time since the last trigger.</param>
        /// <param name="timeSinceLastUpdateCall"> The time (In Seconds) since the last successfull 'Trigger()' call of the passive.</param>
        /// <param name="timeSinceDesiredUpdate"> The time (In Seconds) since this trigger was actually supposed to be performed.</param>
        public abstract bool TestCondition(ServerCharacter character, float lifetime, float timeSinceLastUpdateCall, float timeSinceLastTrigger, out float timeSinceDesiredUpdate); // Note: 'timeSinceLastUpdateCall' may be unneccessary.
    }
}