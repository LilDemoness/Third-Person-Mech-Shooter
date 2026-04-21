using Gameplay.Actions;
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


        public override void Stop_Server(ServerCharacter character)
        {
            character.CancelAction_Server(_associatedAction.ActionID, cancelNonBlocking: true, forceCancel: true);
        }
        protected override void Trigger_Server(ServerCharacter character, float lifetime, float timeSinceDesiredUpdate)
        {
            ActionRequestData actionRequestData = ActionRequestData.Create(_associatedAction);
            character.PlayAction_Server(ref actionRequestData);
            //character.PlayActionServerRpc(actionRequestData);
        }
    }
}