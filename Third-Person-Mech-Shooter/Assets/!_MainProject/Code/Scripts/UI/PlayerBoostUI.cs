using Gameplay.GameplayObjects.Character;
using Gameplay.GameplayObjects.Players;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Players
{
    public class PlayerBoostUI : MonoBehaviour
    {
        [SerializeField] private ProgressBar _boostChargeBar;

        [SerializeField] private RectTransform _boostDivierContainer;
        private RectTransform _boostDivierPrototype;


        private float _boostChargeStartTime;    // Server Time.
        private float _boostChargeStartPercentage;
        private float _boostChargeEndTime;      // Server Time.


        private void Awake()
        {
            _boostDivierPrototype = _boostDivierContainer.GetChild(0) as RectTransform;
            _boostDivierPrototype.gameObject.SetActive(false);

            Player.OnLocalPlayerSet += Player_OnLocalPlayerSet;
            this.enabled = false;   // Disable until activated (Also makes this be disabled on non-Clients as no local player will ever be set).
        }
        private void OnDestroy()
        {
            Player.OnLocalPlayerSet -= Player_OnLocalPlayerSet;
            if (Player.LocalClientInstance != null)
            {
                Player.LocalClientInstance.ServerCharacter.Movement.OnBoostStatsChanged -= ServerCharacterMovement_OnClientBoostStatsChanged;
                Player.LocalClientInstance.ServerCharacter.Movement.OnBoostRechargeValuesChanged -= ServerCharacterMovement_OnClientBoostChargeValuesChanged;
            }
        }


        private void Player_OnLocalPlayerSet() => InitialiseUI();
        private void InitialiseUI()
        {
            this.enabled = true;

            Player.LocalClientInstance.ServerCharacter.Movement.OnBoostStatsChanged += ServerCharacterMovement_OnClientBoostStatsChanged;
            Player.LocalClientInstance.ServerCharacter.Movement.OnBoostRechargeValuesChanged += ServerCharacterMovement_OnClientBoostChargeValuesChanged;
        }


        private void ServerCharacterMovement_OnClientBoostStatsChanged(int boostCount) => SetupBoostDividers(boostCount);
        private void ServerCharacterMovement_OnClientBoostChargeValuesChanged(object sender, ServerCharacterMovement.OnBoostChargeValuesChangedEventArgs e)
        {
            this._boostChargeStartTime = e.ChargeStartTime;
            this._boostChargeStartPercentage = e.ChargeStartPercentage;
            this._boostChargeEndTime = e.ChargeEndTime;
        }


        private void SetupBoostDividers(int boostCount)
        {
            if (boostCount == 0)
            {
                // Hide Boost UI when we have no ability to boost.
                this.gameObject.SetActive(false);
                return;
            }
            if (!this.gameObject.activeSelf)
                this.gameObject.SetActive(true);

            int currentBoostDiviers = _boostDivierContainer.childCount - 1; // Child 0 is the prototype.
            
            // Disable diviers we don't need.
            for(int i = boostCount - 1; i < currentBoostDiviers; ++i)
                _boostDivierContainer.GetChild(i).gameObject.SetActive(false);
            if (boostCount == 1)
                return; // No dividers are shown for 0 or 1 boosts.

            // Create any diviers we need.
            for(int i = currentBoostDiviers; i < boostCount - 1; ++i)
                Instantiate(_boostDivierPrototype, _boostDivierContainer);

            // Enable and position required dividers.
            float spacingPercent = 1.0f / boostCount;
            float spacingValue = _boostDivierContainer.sizeDelta.x * spacingPercent;
            Vector2 startWorldPos = new Vector2(_boostDivierContainer.position.x - _boostDivierContainer.sizeDelta.x, _boostDivierContainer.position.y);
            for (int i = 1; i < boostCount; ++i)
            {
                RectTransform child = _boostDivierContainer.GetChild(i) as RectTransform;

                // Position the child.
                child.position = startWorldPos + new Vector2(spacingValue * i, 0.0f);

                // Show the child.
                child.gameObject.SetActive(true);
            }
        }


        private void Update() => UpdateBoostChargeDisplay();
        private void UpdateBoostChargeDisplay()
        {
            if (NetworkManager.Singleton == null)
                return;

            float serverTime = NetworkManager.Singleton.ServerTime.TimeAsFloat;
            if (serverTime >= _boostChargeEndTime)
            {
                // Max Boost Charge.
                _boostChargeBar.SetValues(current: 1.0f, max: 1.0f);
                return;
            }

            // Calculate and display our charge percentage, calculated from the last time we boosted.
            float chargeProgressionPercent = (serverTime - _boostChargeStartTime) / (_boostChargeEndTime - _boostChargeStartTime);
            float additionalChargePercent = (1.0f - _boostChargeStartPercentage) * chargeProgressionPercent;
            _boostChargeBar.SetValues(current: _boostChargeStartPercentage + additionalChargePercent, max: 1.0f);
        }
    }
}