using Gameplay.GameplayObjects.Character;

namespace Gameplay.Actions.Effects
{
    [System.Serializable]
    public abstract class ActionEffectScaling
    {
        public abstract float GetPercentageValue(ServerCharacter character);
    }
}