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
        [SerializeField, Range(0.0f, 1.0f)] private float _minPercentage;
        [SerializeField, Range(0.0f, 1.0f)] private float _maxPercentage;


        public override bool TestCondition(ServerCharacter character, float lifetime, float timeSinceLastUpdateCall, float timeSinceLastTrigger, out float timeSinceDesiredUpdate)
        {
            throw new System.NotImplementedException("Check desired ServerCharacter statistic");
        }
    }
}