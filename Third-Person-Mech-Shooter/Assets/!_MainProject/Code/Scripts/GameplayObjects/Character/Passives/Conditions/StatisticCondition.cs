using Gameplay.GameplayObjects.Character;
using UnityEngine;

namespace Gameplay.Passives
{
    /// <summary>
    ///     A <see cref="PassiveCondition"/> based on a <see cref="ServerCharacter"/>'s statistics (E.g. Remaining Health).
    /// </summary>
    [System.Serializable]
    public class StatisticCondition : PassiveCondition
    {
        [System.Serializable]
        public enum StatisticToCheck
        {
            Health,
            Heat,
        }
        [SerializeField] private StatisticToCheck _statisticToCheck;
        [SerializeField, Range(0.0f, 1.0f)] private float _minPercentage;   // Inclusive.
        [SerializeField, Range(0.0f, 1.0f)] private float _maxPercentage;   // Inclusive.


        public override bool TestCondition(ServerCharacter character, float lifetime, float timeSinceLastUpdateCall, float timeSinceLastTrigger, out float timeSinceDesiredUpdate)
        {
            float percentageValue = _statisticToCheck switch
            {
                StatisticToCheck.Health => character.NetworkHealthComponent.GetHealthPercentage(),
                StatisticToCheck.Heat => character.CurrentHeat.Value / character.MaxHeat,
                _ => throw new System.NotImplementedException($"Checking '{_statisticToCheck.ToString()}' is not implemented")
            };

            timeSinceDesiredUpdate = 0.0f;
            return percentageValue >= _minPercentage && percentageValue <= _maxPercentage;
        }
    }
}