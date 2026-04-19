using Gameplay.GameplayObjects;
using Gameplay.GameplayObjects.Character;
using Gameplay.GameplayObjects.Players;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using static Gameplay.GameplayObjects.NetworkHealthComponent;

namespace UI.Debugging
{
    /// <summary>
    ///     Displays data from a player that will be useful for debugging (E.g. Health, Heat, Speed, ect)
    /// </summary>
    public class PlayerStateUI : NetworkBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text _healthText;
        private string _healthFormattingString;

        [SerializeField] private TMP_Text _shieldsText;
        private string _shieldsFormattingString;

        [SerializeField] private TMP_Text _heatText;
        private string _heatFormattingString;

        [SerializeField] private TMP_Text _currentSpeedText;

        [SerializeField] private ProgressBar _chargeProgressBar;


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
        private void Player_OnLocalPlayerSet()
        {
            Debug.Log("Initialise UI");

            // Subscribe to change events.
            NetworkHealthComponent.OnAnyHealthChange += OnAnyHealthChanged;
            NetworkHealthComponent.OnAnyShieldsChange += OnAnyShieldsChanged;
            Player.LocalClientInstance.ServerCharacter.OnHeatChanged += OnHeatChanged;

            // Ensure that late joiners receive the initial state.
            NetworkHealthComponent localPlayerHealthComponent = Player.LocalClientInstance.ServerCharacter.NetworkHealthComponent;
            SetHealthText(localPlayerHealthComponent.GetCurrentHealth(), localPlayerHealthComponent.MaxHealth);
            SetHeatText(0.0f, Player.LocalClientInstance.ServerCharacter.MaxHeat);
        }
        public override void OnNetworkDespawn()
        {
            // Unsubscribe to change events.
            if (Player.LocalClientInstance != null)
            {
                NetworkHealthComponent.OnAnyHealthChange -= OnAnyHealthChanged;
                NetworkHealthComponent.OnAnyShieldsChange -= OnAnyShieldsChanged;
                Player.LocalClientInstance.ServerCharacter.OnHeatChanged -= OnHeatChanged;
            }
        }
        public override void OnDestroy()
        {
            base.OnDestroy();
            Player.OnLocalPlayerSet -= Player_OnLocalPlayerSet;
        }

        private void Update()
        {
            if (Player.LocalClientInstance == null)
                return;

            _chargeProgressBar.SetValues(Player.LocalClientInstance.ServerCharacter.CoreSystemCharge, 0.0f, Player.LocalClientInstance.ServerCharacter.MaxCoreSystemCharge);
        }


        private void OnAnyHealthChanged(AnyHealthChangeEventArgs e)
        {
            if (e.ThisCharacter != Player.LocalClientInstance.ServerCharacter)
                return;

            SetHealthText(e.NewCurrentHealth, e.NewMaxHealth);
        }
        private void OnAnyShieldsChanged(AnyShieldsChangeEventArgs e)
        {
            if (e.ThisCharacter != Player.LocalClientInstance.ServerCharacter)
                return;

            SetShieldsText(e.NewCurrentShields, e.NewMaxShields);
        }
        private void OnHeatChanged(float currentValue, float maxValue) => SetHeatText(currentValue, maxValue);


        private void SetHealthText(float currentHealth, float maxHealth)
        {
            _healthFormattingString = GetFormattingString(Player.LocalClientInstance.ServerCharacter.NetworkHealthComponent.MaxHealth); // Call when 'MaxHealth' is changed.
            _healthText.text = string.Concat("Health: ", currentHealth.ToString(_healthFormattingString), "/", maxHealth.ToString(_healthFormattingString));
        }
        private void SetShieldsText(float currentShields, float maxShields)
        {
            _shieldsFormattingString = GetFormattingString(Player.LocalClientInstance.ServerCharacter.NetworkHealthComponent.MaxShields);
            _shieldsText.text = string.Concat("Shields: ", currentShields.ToString(_shieldsFormattingString), "/", maxShields.ToString(_shieldsFormattingString));
        }
        private void SetHeatText(float currentHeat, float maxHeat)
        {
            _heatFormattingString = GetFormattingString(Player.LocalClientInstance.ServerCharacter.MaxHeat); // Call when 'MaxHeat' is changed.
            _heatText.text = string.Concat("Heat: ", currentHeat.ToString(_heatFormattingString), "/", maxHeat.ToString(_heatFormattingString));
        }
        private string CreateSpeedString(float currentSpeed) => string.Concat("Speed: ", (Mathf.Round(currentSpeed / 10.0f) * 10.0f).ToString(), Units.SPEED_UNITS);

        private string GetFormattingString(float value)
        {
            if (value == 0.0f)
                return "0";

            int significantFigureCount = CalculateSignificantFigureCount(value) + 1;
            return new string('0', significantFigureCount);
        }
        // Source: 'https://stackoverflow.com/questions/374316/round-a-double-to-x-significant-figures/374470#374470'.
        int CalculateSignificantFigureCount(float value)
        {
            float scale = Mathf.Pow(10, Mathf.Floor(Mathf.Log10(Mathf.Abs(value))) + 1);
            return Mathf.RoundToInt(value * Mathf.Floor(value / scale));
        }
    }
}