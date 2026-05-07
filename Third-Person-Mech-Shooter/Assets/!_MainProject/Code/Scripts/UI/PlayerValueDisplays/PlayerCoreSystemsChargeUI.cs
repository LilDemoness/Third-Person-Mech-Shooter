using UnityEngine;
using Unity.Netcode;
using UI;
using Gameplay.GameplayObjects.Players;
using Gameplay.GameplayObjects.Character;

namespace Gameplay.UI.Players
{
    public class PlayerCoreSystemsChargeUI : NetworkBehaviour
    {
        [Header("UI References")]
        [SerializeField] private ProgressBar _progressBar;

        [Space(5)]
        [SerializeField] private CanvasGroup _keybindIndicatorCanvasGroup;
        [SerializeField] private float _keybindIndicatorShowTime = 0.2f;
        [SerializeField] private float _keybindIndicatorFadeTime = 0.1f;

        [Space(5)]
        [SerializeField] private RectTransform _requiredChargeIndicatorTransform;


        private void Awake() => _keybindIndicatorCanvasGroup.alpha = 0.0f;
        public override void OnNetworkSpawn()
        {
            if (!IsClient)
            {
                // No use on non-clients.
                this.enabled = false;
            }

            Player.OnLocalPlayerBuildUpdated += Player_OnLocalPlayerBuildUpdated;
            if (Player.LocalClientInstance != null)
                UpdateChargeIndicator();
        }

        private void Player_OnLocalPlayerBuildUpdated(GameplayObjects.Character.Customisation.Data.BuildData _) => UpdateChargeIndicator();
        private void UpdateChargeIndicator()
        {
            float minActivationPercentage = Player.LocalClientInstance.ServerCharacter.GetCoreSystemData().MinActivationPercentage;
            if (minActivationPercentage <= 0.0f || minActivationPercentage >= 1.0f)
            {
                // At these values hide the indicator as it looks better.
                _requiredChargeIndicatorTransform.gameObject.SetActive(false);
                return;
            }

            // The indicator should be shown.
            _requiredChargeIndicatorTransform.gameObject.SetActive(true);

            // Position the indicator (Assumes that the indicator's anchor is centre-left of the progress bar).
            float xOffset = (_progressBar.transform as RectTransform).rect.width * minActivationPercentage;
            _requiredChargeIndicatorTransform.anchoredPosition = Vector3.right * xOffset;
        }
        


        private void Update()
        {
            if (Player.LocalClientInstance == null)
                return;

            ServerCharacter playerCharacter = Player.LocalClientInstance.ServerCharacter;
            _progressBar.SetValues(playerCharacter.CoreSystemCharge, 0.0f, playerCharacter.MaxCoreSystemCharge);
            UpdateKeybindIndicator(playerCharacter);
        }

        private void UpdateKeybindIndicator(ServerCharacter playerCharacter)
        {
            bool shouldShowKeybindIndicator =  playerCharacter.CanUseCoreSystem() || (playerCharacter.CanCancelCoreSystem() && !playerCharacter.CompareCoreSystemActivationStyle(Actions.ActionActivationStyle.Pressed));
            
            if (shouldShowKeybindIndicator && _keybindIndicatorCanvasGroup.alpha < 1.0f)
                _keybindIndicatorCanvasGroup.alpha += Time.deltaTime / _keybindIndicatorShowTime;
            else if (!shouldShowKeybindIndicator && _keybindIndicatorCanvasGroup.alpha > 0.0f)
                _keybindIndicatorCanvasGroup.alpha -= Time.deltaTime / _keybindIndicatorFadeTime;
        }
    }
}