using Gameplay.GameplayObjects.Players;
using Unity.Netcode;
using UnityEngine;

namespace UI.Debugging
{
    public class PlayerHeatUI : NetworkBehaviour
    {
        [SerializeField] private ProgressBar _progressBar;

        [Space(5)]
        [SerializeField, Range(0.0f, 1.0f)]
        [Tooltip("The percentage of heat vs max heat that the UI should start flashing an overheat warning at.")]
        private float _warningPercentageValue = 0.8f;

        [SerializeField] private OverheatWarningUI _overheatWarning;


        private void Awake()
        {
            Player.OnLocalPlayerSet += Player_OnLocalPlayerSet;
            HideOverheatWarning();
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
        private void Player_OnLocalPlayerSet()
        {
            Debug.Log("Initialise UI");
            var serverCharacter = Player.LocalClientInstance.ServerCharacter;

            // Subscribe to change events.
            serverCharacter.OnHeatChanged += OnHeatChanged;

            // Ensure that late joiners receive the initial state.
            OnHeatChanged(serverCharacter.CurrentHeat.Value, serverCharacter.MaxHeat);
        }
        public override void OnNetworkDespawn()
        {
            // Unsubscribe to change events.
            if (Player.LocalClientInstance != null)
                Player.LocalClientInstance.ServerCharacter.OnHeatChanged -= OnHeatChanged;
        }
        public override void OnDestroy()
        {
            base.OnDestroy();
            Player.OnLocalPlayerSet -= Player_OnLocalPlayerSet;
        }


        private void OnHeatChanged(float currentValue, float maxValue)
        {
            float heatPercentage = currentValue / maxValue;
            if (heatPercentage >= _warningPercentageValue)
                ShowOverheatWarning(heatPercentage);
            else
                HideOverheatWarning();

            _progressBar.SetValues(currentValue, 0.0f, maxValue);
        }


        private void ShowOverheatWarning(float currentHeatPercent)
        {
            _overheatWarning.Show();
            _overheatWarning.UpdateFlashRate(Mathf.InverseLerp(_warningPercentageValue, 1.0f, currentHeatPercent));
        }
        private void HideOverheatWarning() => _overheatWarning.Hide();
    }
}