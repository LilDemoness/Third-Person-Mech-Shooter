using Gameplay.GameplayObjects.Character;

namespace Gameplay.Passives
{
    /// <summary>
    ///     A <see cref="PassiveCondition"/> based on whether a <see cref="ServerCharacter"/> has done something or had something done to them recently (E.g. Dealt or Received Damage).
    /// </summary>
    [System.Serializable]
    public class RecentActionCondition : PassiveCondition
    {
        // E.g. Dealt Damage, Received Damage, etc.

        public override bool TestCondition(ServerCharacter character, float lifetime, float timeSinceLastUpdateCall, float timeSinceLastTrigger, out float timeSinceDesiredUpdate)
        {
            throw new System.NotImplementedException("Check desired recent effect");
        }
    }
}