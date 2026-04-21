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
        [SerializeField] private Statistic _affectedStatistic;

        [Space(5)]
        [SerializeField] private StatisticAlterationType _alterationType;
        [SerializeField] private float _alterationValue;

        [Space(5)]
        [SerializeField] private bool _removeEffectOnConditionFailed = false;



        protected override void Trigger_Server(ServerCharacter character, float lifetime, float timeSinceDesiredUpdate) => ApplyEffect(character);
        protected override void OnConditionFailed_Server(ServerCharacter character)
        {
            if (_removeEffectOnConditionFailed)
                SuspendEffect(character);
        }
        public override void Stop_Server(ServerCharacter character) => RemoveEffect(character);


        protected virtual void ApplyEffect(ServerCharacter character) => character.CharacterStats.AddStatisticChange(_affectedStatistic, _alterationType, _alterationValue);
        protected virtual void SuspendEffect(ServerCharacter character) => character.CharacterStats.RemoveStatisticChange(_affectedStatistic, _alterationType, _alterationValue);
        protected virtual void RemoveEffect(ServerCharacter character) => character.CharacterStats.RemoveStatisticChange(_affectedStatistic, _alterationType, _alterationValue);
    }
}