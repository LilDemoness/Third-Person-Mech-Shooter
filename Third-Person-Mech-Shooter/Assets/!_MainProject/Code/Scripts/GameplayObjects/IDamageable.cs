using UnityEngine;
using Gameplay.GameplayObjects.Character;

namespace Gameplay.GameplayObjects
{
    public interface IDamageable
    {
        void ReceiveDamage_Server(ServerCharacter influencer, float damageValue, DamageTypes damageType, Vector3 damageSourceDirection);
        void ReceiveHealing_Server(ServerCharacter influencer, float healingValue);

        float GetMissingHealth();

        bool CanHaveHealthChanged();
        bool CanTakeDamage();
        bool CanReceiveHealing();
    }
}