using Gameplay.GameplayObjects.Character;
using UnityEngine;

namespace Gameplay.Actions.Effects
{
    /// <summary>
    ///     Base class for all game-state affecting effects created by an action.
    /// </summary>
    [System.Serializable]
    public abstract class ActionEffect
    {
        [SerializeReference, SubclassSelector] private ActionEffectScaling[] _actionEffectScaling;
        

        public void ApplyEffect(ServerCharacter owner, in ActionHitInformation[] hitInfoArray, float chargePercentage)
        {
            for(int i = 0; i < hitInfoArray.Length; ++i)
            {
                ApplyEffect(owner, hitInfoArray[i], chargePercentage);
            }
        }
        public abstract void ApplyEffect(ServerCharacter owner, in ActionHitInformation hitInfo, float chargePercentage);


        /// <summary>
        ///     Cleanup the ActionEffect to be used the next time the Action is created.
        /// </summary>
        public virtual void Cleanup(ServerCharacter owner) { }


        protected float GetScalingValue(ServerCharacter owner)
        {
            float total = 1.0f;
            foreach (ActionEffectScaling scaling in _actionEffectScaling)
                total *= scaling.GetPercentageValue(owner);

            return total;
        }
    }
}