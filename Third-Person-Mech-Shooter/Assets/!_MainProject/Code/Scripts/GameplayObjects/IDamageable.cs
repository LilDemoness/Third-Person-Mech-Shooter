using UnityEngine;
using Gameplay.GameplayObjects.Character;

namespace Gameplay.GameplayObjects
{
    public interface IDamageable
    {
        public static event System.Action<ServerCharacter, float> OnAnyHealthChange;
        protected static void InvokeOnAnyHealthChange(ServerCharacter inflicter, float healthChange) => OnAnyHealthChange?.Invoke(inflicter, healthChange);

        void ReceiveDamage_Server(ServerCharacter influencer, float damageValue, DamageTypes damageType, Vector3 damageSourceDirection);
        void ReceiveHealing_Server(ServerCharacter influencer, float healingValue);

        float GetMissingHealth();

        bool CanHaveHealthChanged();
        bool CanTakeDamage();
        bool CanReceiveHealing();
    }
}