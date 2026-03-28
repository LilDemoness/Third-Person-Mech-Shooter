using UnityEngine;
using Gameplay.Actions.Definitions;

namespace Gameplay.GameplayObjects.Character.Customisation.Data
{
    [CreateAssetMenu(menuName = "Data/ModuleData")]
    public class ModuleData : BaseCustomisationData
    {
        [field: Space(5)]
        [field: SerializeField] public ModuleSize ModuleSize { get; private set; }
        [field: SerializeField] public ActionDefinition AssociatedAction { get; private set; }
    }
}