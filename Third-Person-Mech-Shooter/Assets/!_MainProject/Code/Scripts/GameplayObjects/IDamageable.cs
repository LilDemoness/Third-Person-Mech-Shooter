using UnityEngine;
using Gameplay.GameplayObjects.Character;

namespace Gameplay.GameplayObjects
{
    public interface IDamageable
    {
        // <Source Damageable, Inflicting Character, Health Change>
        public static event System.Action<IDamageable, ServerCharacter, float> OnAnyHealthChange;
        protected static void InvokeOnAnyHealthChange(IDamageable sender, ServerCharacter inflicter, float healthChange) => OnAnyHealthChange?.Invoke(sender, inflicter, healthChange);

        Transform Transform { get; }


        void ReceiveDamage_Server(ServerCharacter influencer, float damageValue, DamageTypes damageType, Vector3 damageSourceDirection);
        void ReceiveHealing_Server(ServerCharacter influencer, float healingValue);

        float GetMissingHealth();

        bool CanHaveHealthChanged();
        bool CanTakeDamage();
        bool CanReceiveHealing();
    }
}