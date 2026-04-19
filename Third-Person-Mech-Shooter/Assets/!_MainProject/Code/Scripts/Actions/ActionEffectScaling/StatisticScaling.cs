using Gameplay.GameplayObjects.Character;
using UnityEngine;

namespace Gameplay.Actions.Effects
{
    [System.Serializable]
    public class StatisticScaling : ActionEffectScaling
    {
        [System.Serializable]
        public enum StatisticToCheck
        {
            Health,
            Heat,
        }
        [SerializeField] private StatisticToCheck _statisticToCheck;
        [SerializeField] private float _minMultiplier = 0.0f;
        [SerializeField] private float _maxMultiplier = 1.0f;

        public override float GetPercentageValue(ServerCharacter character)
        {
            float percentageValue = _statisticToCheck switch
            {
                StatisticToCheck.Health => character.NetworkHealthComponent.GetHealthPercentage(),
                StatisticToCheck.Heat => character.CurrentHeat.Value / character.MaxHeat,
                _ => throw new System.NotImplementedException($"Checking '{_statisticToCheck.ToString()}' is not implemented")
            };

            return Mathf.Lerp(_minMultiplier, _maxMultiplier, percentageValue);
        }
    }
}