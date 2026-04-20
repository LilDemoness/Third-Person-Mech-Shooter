using Gameplay.GameplayObjects.Character;
using UnityEngine;

namespace Gameplay.StatusEffects.Definitions
{
    [CreateAssetMenu(menuName = "Status Effect/New Intangibility Status Effect")]
    public class Intangibility : StatusEffectDefinition
    {
        public override void OnStart(ServerCharacter serverCharacter) => serverCharacter.IsIntangible.Value = true;
        public override void OnEnd(ServerCharacter serverCharacter) => EndIntangibility(serverCharacter);
        public override void OnCancel(ServerCharacter serverCharacter) => EndIntangibility(serverCharacter);


        /// <summary>
        ///     End the Intangible effect on the passed character.
        /// </summary>
        private void EndIntangibility(ServerCharacter serverCharacter)
        {
            serverCharacter.IsIntangible.Value = false;
        }


        public override void OnStartClient(ClientCharacter clientCharacter) { }
    }
}