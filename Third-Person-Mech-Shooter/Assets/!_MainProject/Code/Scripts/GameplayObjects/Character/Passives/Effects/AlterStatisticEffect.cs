using Gameplay.GameplayObjects.Character;

namespace Gameplay.Passives
{
    /// <summary>
    ///     A <see cref="PassiveEffect"/> that alters a <see cref="ServerCharacter"/>'s statistics (Max Health, Damage Reduction, Base Speed, etc).
    /// </summary>
    [System.Serializable]
    public class AlterStatisticEffect : PassiveEffect
    {
        /* Statistics (Unless otherwise specified: Addition, Multiplier):
         * - Max Health
         * - Damage Reduction
         * - Health Resistance Types (Damage Type | Base, Addition, Multiplier)
         * 
         * - Regenerating Health/Shield
         * - Regenerating Health/Shield Resistance Types (Damage Type | Base, Addition, Multiplier)
         * - Shielded External Heat Gain Rate
         * 
         * - Max Heat
         * - Personal Heat Gain Rate
         * - External Heat Gain Rate?
         * - Heat Loss Rate
         * 
         * - Movement Speed
         * - Boost Count (Base, Addition)
         * - Boost Recharge Rate
         */

        /* Statistic Alteration Type:
         * - Base: Overrides the default base value. If multiple bases exist, determine the base value by adding their offsets from the default and applying that (E.g. Default = 5, Overrides = 3 & 6, New Base = ((3 - 5) + (6 - 5) + 5) = (-2 + 1 + 5) = 4).
         * - Addition: Adds to the base value.
         * - Multiplier: Multiplies the value after Base + Addition (E.g. Base = 4, Addition = 2, Multiplier = 1.5, Total = (5 + 2) * 1.5 = 9).
         */

        /* Damage Types:
         * - Ballistic
         * - Explosive
         * - Heat (From enemy weapons/effects)
         * - Overheating (Replaces Heat and instead have heat from enemies managed by "external heat gain rate"?)
         */


        protected override void Trigger(ServerCharacter character, float lifetime, float timeSinceDesiredUpdate) => ApplyEffect(character);
        protected virtual void ApplyEffect(ServerCharacter character)
        {
            throw new System.NotImplementedException("Apply/Reactivate Statistic Change Here");
        }


        protected override void OnConditionFailed(ServerCharacter character) => SuspendEffect(character);
        protected virtual void SuspendEffect(ServerCharacter character)
        {
            throw new System.NotImplementedException("Suspend Statistic Change Here");
        }
    }
}