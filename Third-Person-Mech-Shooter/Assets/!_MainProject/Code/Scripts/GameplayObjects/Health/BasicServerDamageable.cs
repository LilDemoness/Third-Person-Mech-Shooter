using Gameplay.GameplayObjects.Character;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Gameplay.GameplayObjects.Health
{
    /// <summary>
    ///     A server-side component which handles basic health functionality of an entity.
    /// </summary>
    public class BasicServerDamageable : MonoBehaviour, IDamageable
    {
        public Transform Transform => transform;


        [SerializeField] private float _maxHealth;
        private float _currentHealth;

        [SerializeField] private bool _isInvulnerable = false;
        [SerializeField] private bool _canBeHealed = false;


        public UnityEvent<IDamageable> OnHealthChanged;
        public UnityEvent<IDamageable> OnDied;


        private void Awake()
        {
            if (!NetworkManager.Singleton.IsServer)
            {
                // Only exist on the server.
                Destroy(this);
                return;
            }

            ResetHealthToMaximum();
        }

        public void ResetHealthToMaximum() => _currentHealth = _maxHealth;


        public bool CanHaveHealthChanged() => !_isInvulnerable;
        public bool CanReceiveHealing() => _canBeHealed;
        public bool CanTakeDamage() => !_isInvulnerable;

        public float GetMissingHealth() => Mathf.Max(_maxHealth - _currentHealth, 0.0f);

        public void ReceiveDamage_Server(ServerCharacter inflicter, float damageValue, DamageTypes damageType, Vector3 damageSourceDirection)
        {
            if (!CanTakeDamage())
                return;

            _currentHealth = Mathf.Max(_currentHealth - damageValue, 0.0f);
            IDamageable.InvokeOnAnyHealthChange(this, inflicter, -damageValue);

            if (Mathf.Approximately(_currentHealth, 0.0f))
            {
                OnDied?.Invoke(this);
            }
            else
                OnHealthChanged?.Invoke(this);
        }
        public void ReceiveHealing_Server(ServerCharacter inflicter, float healingValue)
        {
            if (!CanReceiveHealing())
                return;

            IDamageable.InvokeOnAnyHealthChange(this, inflicter, healingValue);
            _currentHealth = Mathf.Min(_currentHealth + healingValue, _maxHealth);
            OnHealthChanged?.Invoke(this);
        }
    }
}