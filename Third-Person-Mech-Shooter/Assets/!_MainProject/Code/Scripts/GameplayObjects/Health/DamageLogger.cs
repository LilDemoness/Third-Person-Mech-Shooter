using Gameplay.GameplayObjects.Character;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.GameplayObjects.Health
{
    public class DamageLogger : NetworkBehaviour, IDamageable
    {
        public void ReceiveDamage_Server(ServerCharacter inflicter, float damageValue, DamageTypes damageType, Vector3 damageSourceDirection)
        {
            if (!CanHaveHealthChanged())
            {
                Debug.Log($"{this.name} is Invulnerable");
                return;
            }

            IDamageable.InvokeOnAnyHealthChange(inflicter, -damageValue);
            Debug.Log($"{this.name} received {damageValue} damage from {inflicter.name}");
        }
        public void ReceiveHealing_Server(ServerCharacter inflicter, float healingValue)
        {
            if (!CanHaveHealthChanged())
            {
                Debug.Log($"{this.name} is Invulnerable");
                return;
            }

            IDamageable.InvokeOnAnyHealthChange(inflicter, healingValue);
            Debug.Log($"{this.name} received {healingValue} healing from {inflicter.name}");
        }
        public float GetMissingHealth() => 0;

        public bool CanHaveHealthChanged() => true;
        public bool CanReceiveHealing() => true;
        public bool CanTakeDamage() => true;
    }
}