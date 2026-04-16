using UnityEngine;
using Gameplay.GameplayObjects.Character;
using Gameplay.Actions.Definitions;

namespace Gameplay.Passives
{
    /// <summary>
    ///     A <see cref="PassiveEffect"/> that replaces a <see cref="ServerCharacter"/>'s Boost action with a custom one.
    /// </summary>
    [System.Serializable]
    public class ReplaceBoostEffect : PassiveEffect
    {
        // How do we handle having multiple of these effects active at the same time?
        [SerializeField] private ActionDefinition _boostReplacementAction;

        public override void Stop(ServerCharacter character)
        {
            character.Movement.ClearBoostActionOverrideServerRpc();
        }

        protected override void Trigger(ServerCharacter character, float lifetime, float timeSinceDesiredUpdate)
        {
            character.Movement.SetBoostActionServerRpc(_boostReplacementAction.ActionID);
        }
    }
}