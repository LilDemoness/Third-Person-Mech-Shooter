using UnityEngine;
using Gameplay.GameplayObjects.Character;
using Gameplay.GameplayObjects.Character.Statistics;

namespace Gameplay.Passives
{
    /// <summary>
    ///     A <see cref="PassiveEffect"/> that alters a <see cref="ServerCharacter"/>'s statistics (Max Health, Damage Reduction, Base Speed, etc).
    /// </summary>
    [System.Serializable]
    public class AlterStatisticEffect : PassiveEffect
    {
        [System.Serializable, System.Flags]
        public enum DamageTypes
        {
            // Damage dealt by physical projectiles.
            Ballistic,
            // Damage dealt from explosives.
            Explosive,
            // Damage dealt to Heat by enemies (Such as Lasers). (Replace with "ExternalHeatGainRate"?)
            Heat,
            // Damage taken from Overheating or special enemy abilities.
            Overheating,
        }

        [SerializeField] private Statistic _affectedStatistic;

        [Space(5)]
        [SerializeField] private StatisticAlterationType _alterationType;
        [SerializeField] private float _alterationValue;

        [Space(5)]
        [SerializeField] private bool _removeEffectOnConditionFailed = false;



        protected override void Trigger(ServerCharacter character, float lifetime, float timeSinceDesiredUpdate) => ApplyEffect(character);
        protected virtual void ApplyEffect(ServerCharacter character)
        {
            character.CharacterStats.AddStatisticChangeServerRpc(_affectedStatistic, _alterationType, _alterationValue);
        }


        protected override void OnConditionFailed(ServerCharacter character) => SuspendEffect(character);
        protected virtual void SuspendEffect(ServerCharacter character)
        {
            if (_removeEffectOnConditionFailed)
                character.CharacterStats.RemoveStatisticChangeServerRpc(_affectedStatistic, _alterationType, _alterationValue);
        }


        public override void Stop(ServerCharacter character) => RemoveEffect(character);
        protected virtual void RemoveEffect(ServerCharacter character) => character.CharacterStats.RemoveStatisticChangeServerRpc(_affectedStatistic, _alterationType, _alterationValue);
    }

    public class DamageResistanceStatisticEffect : AlterStatisticEffect
    {
        [Space(10)]
        [SerializeField] private DamageTypes _damageTypes;
    }
}