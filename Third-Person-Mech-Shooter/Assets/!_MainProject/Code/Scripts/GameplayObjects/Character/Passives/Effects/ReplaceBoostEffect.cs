using Gameplay.GameplayObjects.Character;

namespace Gameplay.Passives
{
    /// <summary>
    ///     A <see cref="PassiveEffect"/> that replaces a <see cref="ServerCharacter"/>'s Boost action with a custom one.
    /// </summary>
    [System.Serializable]
    public class ReplaceBoostEffect : PassiveEffect
    {
        // How do we handle having multiple of these effects active at the same time?
        protected override void Trigger(ServerCharacter character, float lifetime, float timeSinceDesiredUpdate)
        {
            throw new System.NotImplementedException("Replace ServerCharacter's Boost Action Here");
        }
    }
}