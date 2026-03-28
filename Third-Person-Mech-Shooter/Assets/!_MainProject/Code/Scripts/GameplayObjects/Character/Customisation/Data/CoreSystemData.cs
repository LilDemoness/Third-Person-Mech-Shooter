using Gameplay.Actions.Definitions;
using UnityEngine;

namespace Gameplay.GameplayObjects.Character.Customisation.Data
{
    [CreateAssetMenu(menuName = "Data/CoreSystemData")]
    public class CoreSystemData : BaseCustomisationData
    {
        // Passive Feature.


        // Active Feature.
        [field: SerializeField] public ActionDefinition ActiveActionDefinition { get; private set; }
    }
}