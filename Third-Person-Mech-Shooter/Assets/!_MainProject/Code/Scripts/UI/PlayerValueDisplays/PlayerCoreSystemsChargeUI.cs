using UnityEngine;
using Unity.Netcode;
using UI;
using Gameplay.GameplayObjects.Players;

namespace Gameplay.UI.Players
{
    public class PlayerCoreSystemsChargeUI : NetworkBehaviour
    {
        [Header("UI References")]
        [SerializeField] private ProgressBar _progressBar;


        public override void OnNetworkSpawn()
        {
            if (!IsClient)
            {
                // No use on non-clients.
                this.enabled = false;
            }
        }


        private void Update()
        {
            if (Player.LocalClientInstance == null)
                return;

            _progressBar.SetValues(Player.LocalClientInstance.ServerCharacter.CoreSystemCharge, 0.0f, Player.LocalClientInstance.ServerCharacter.MaxCoreSystemCharge);
        }
    }
}