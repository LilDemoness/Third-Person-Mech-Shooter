using Gameplay.Actions.Definitions;
using Gameplay.Passives;
using UnityEngine;

namespace Gameplay.GameplayObjects.Character.Customisation.Data
{
    [CreateAssetMenu(menuName = "Data/CoreSystemData")]
    public class CoreSystemData : BaseCustomisationData
    {
        [field: Header("Passive Feature")]
        [field: SerializeField] public PassiveDefinition PassiveFeatureDefinition { get; private set; }


        [field: Header("Active Feature")]
        [field: SerializeField] public ActionDefinition ActiveActionDefinition { get; private set; }


        [field: Space(5)]
        [field: SerializeField]
        [field: Tooltip("1 second = 1 point. 1 dmg = 2 points.")]
        public float CoreSystemCost { get; private set; } = 1000.0f;

        public const float TIME_TO_CHARGE_MULTIPLIER = 1.0f;
        public const float DAMAGE_TO_CHARGE_MULTIPLIER = 2.0f;
        public const float HEALING_TO_CHARGE_MULTIPLIER = 2.0f;


        [field: Space(5)]
        [field: SerializeField, Range(0.0f, 1.0f)]
        public float MinActivationPercentage { get; private set; } = 1.0f;


        [field: Space(5)]
        [field: SerializeField] public bool FullyDrainOnEnd { get; private set; } = true;
        [field: SerializeField] public float PowerPercentageDrainRate { get; private set; } = 0.0f;
    }
}