using Gameplay.Actions.Definitions;
using Gameplay.Passives;
using UnityEngine;

namespace Gameplay.GameplayObjects.Character.Customisation.Data
{
    [CreateAssetMenu(menuName = "Data/CoreSystemData")]
    public class CoreSystemData : BaseCustomisationData
    {
        // Passive Feature.
        [field: SerializeField] public PassiveDefinition PassiveFeatureDefinition { get; private set; }


        // Active Feature.
        [field: SerializeField] public ActionDefinition ActiveActionDefinition { get; private set; }
    }
}