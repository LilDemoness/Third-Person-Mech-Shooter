using Gameplay.GameplayObjects;
using Gameplay.GameplayObjects.Players;
using UI;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.UI.Players
{
    public class PlayerHealthUI : NetworkBehaviour
    {
        [Header("UI References")]
        [SerializeField] private ProgressBar _healthProgressBar;
        [SerializeField] private ProgressBar _shieldsProgressBar;


        private void Awake()
        {
            Player.OnLocalPlayerSet += Player_OnLocalPlayerSet;
        }
        public override void OnNetworkSpawn()
        {
            if (!IsClient)
            {
                // No use on non-clients.
                Player.OnLocalPlayerSet -= Player_OnLocalPlayerSet;
                this.enabled = false;
                return;
            }
        }
        public override void OnNetworkDespawn()
        {
            // Unsubscribe to change events.
            if (Player.LocalClientInstance != null)
            {
                NetworkHealthComponent.OnAnyHealthChange -= OnAnyHealthChanged;
                NetworkHealthComponent.OnAnyShieldsChange -= OnAnyShieldsChanged;
            }
        }
        public override void OnDestroy()
        {
            base.OnDestroy();
            Player.OnLocalPlayerSet -= Player_OnLocalPlayerSet;
        }


        private void Player_OnLocalPlayerSet()
        {
            Debug.Log("Initialise Health UI");

            // Subscribe to change events.
            NetworkHealthComponent.OnAnyHealthChange += OnAnyHealthChanged;
            NetworkHealthComponent.OnAnyShieldsChange += OnAnyShieldsChanged;

            // Ensure that late joiners receive the initial state.
            NetworkHealthComponent localPlayerHealthComponent = Player.LocalClientInstance.ServerCharacter.NetworkHealthComponent;
            SetHealthValue(localPlayerHealthComponent.GetCurrentHealth(), localPlayerHealthComponent.MaxHealth);
            SetShieldsValue(localPlayerHealthComponent.GetCurrentShields(), localPlayerHealthComponent.MaxShields);
        }


        private void OnAnyHealthChanged(NetworkHealthComponent.AnyHealthChangeEventArgs e)
        {
            if (e.ThisCharacter != Player.LocalClientInstance.ServerCharacter)
                return;

            SetHealthValue(e.NewCurrentHealth, e.NewMaxHealth);
        }
        private void OnAnyShieldsChanged(NetworkHealthComponent.AnyShieldsChangeEventArgs e)
        {
            if (e.ThisCharacter != Player.LocalClientInstance.ServerCharacter)
                return;

            SetShieldsValue(e.NewCurrentShields, e.NewMaxShields);
        }


        private void SetHealthValue(float currentHealth, float maxHealth)
        {
            _healthProgressBar.SetValues(currentHealth, 0.0f, maxHealth);
        }
        private void SetShieldsValue(float currentShields, float maxShields)
        {
            _shieldsProgressBar.SetValues(currentShields, 0.0f, maxShields);
        }
    }
}