using UnityEngine;
using Gameplay.GameplayObjects.Character;
using Gameplay.GameplayObjects.Character.Statistics;

namespace Gameplay.Passives
{
    [System.Serializable]
    public class AlterDamageTakenStatisticEffect : PassiveEffect
    {
        [SerializeField] private DamageTakenStatistic _affectedStatistic;
        [SerializeField] private DamageTypes _damageTypes;

        [Space(5)]
        [SerializeField, Min(0)] private float _alterationValue = 1.0f;

        [Space(5)]
        [SerializeField] private bool _removeEffectOnConditionFailed = false;


        [Space(10)]
        [SerializeField] private bool _hasDirectionalCondition = false;
        [SerializeField] private DirectionalCondition _directionalCondition;


        protected override void Trigger_Server(ServerCharacter character, float lifetime, float timeSinceDesiredUpdate) => ApplyEffect(character);
        protected override void OnConditionFailed_Server(ServerCharacter character)
        {
            if (_removeEffectOnConditionFailed)
                SuspendEffect(character);
        }
        public override void Stop_Server(ServerCharacter character) => RemoveEffect(character);


        protected virtual void ApplyEffect(ServerCharacter owner) => owner.CharacterStats.AddDamageTakenStatisticChange(owner, _affectedStatistic, _alterationValue, _damageTypes, _hasDirectionalCondition ? _directionalCondition : null);
        protected virtual void SuspendEffect(ServerCharacter owner) => owner.CharacterStats.RemoveDamageTakenStatisticChange(owner, _affectedStatistic, _alterationValue, _damageTypes, _hasDirectionalCondition ? _directionalCondition : null);
        protected virtual void RemoveEffect(ServerCharacter owner) => owner.CharacterStats.RemoveDamageTakenStatisticChange(owner, _affectedStatistic, _alterationValue, _damageTypes, _hasDirectionalCondition ? _directionalCondition : null);
    }
}