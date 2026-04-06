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


        public override void Stop(ServerCharacter character)
        {
            character.ActionPlayer.CancelRunningActionsByID(_associatedAction.ActionID, cancelNonBlocking: true, forceCancel: true);
        }
        protected override void Trigger(ServerCharacter character, float lifetime, float timeSinceDesiredUpdate)
        {
            ActionRequestData actionRequestData = ActionRequestData.Create(_associatedAction);
            character.ActionPlayer.PlayAction(ref actionRequestData);
            //character.PlayActionServerRpc(actionRequestData);
        }
    }
}