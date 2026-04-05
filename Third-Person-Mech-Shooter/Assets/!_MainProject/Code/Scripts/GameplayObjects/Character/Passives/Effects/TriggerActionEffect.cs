using Gameplay.Actions.Definitions;
using Gameplay.GameplayObjects.Character;
using UnityEngine;

namespace Gameplay.Passives
{
    /// <summary>
    ///     A <see cref="PassiveEffect"/> that starts an action when triggered.
    /// </summary>
    [System.Serializable]
    public class TriggerActionEffect : PassiveEffect
    {
        [SerializeField] private ActionDefinition _associatedAction;


        protected override void Trigger(ServerCharacter character, float lifetime, float timeSinceDesiredUpdate)
        {
            throw new System.NotImplementedException("Trigger Action Here");
            // Q: How will we handle action persistance/updates?
            //  A: Triggering the action through the character should do this automatically.
        }
    }
}