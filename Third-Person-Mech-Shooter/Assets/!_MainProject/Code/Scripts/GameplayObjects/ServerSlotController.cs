using System.Collections;
using Unity.Netcode;
using UnityEngine;
using Gameplay.Actions;
using Gameplay.GameplayObjects.Players;
using Gameplay.GameplayObjects.Character.Customisation.Sections;

namespace Gameplay.GameplayObjects.Character
{
    /// <summary>
    ///     Server-side script to handles the processing of input for the activation of Slottables such as weapons and abilities.
    /// </summary>
    /// <remarks>
    ///     There is one non-server function: 'ActivateSlot'; which can trigger a call client-side for action anticipation.
    /// </remarks>
    public class ServerSlotController : NetworkBehaviour
    {
        [SerializeField] private ServerCharacter _serverCharacter;
        [SerializeField] private Player _playerManager;

        
        private bool[] _activationRequests = new bool[0];
        private bool _coreSystemActivationRequest;


        private void Awake()
        {
            _playerManager.OnThisPlayerBuildUpdated += OnPlayerBuildChanged;
        }
        public override void OnDestroy()
        {
            base.OnDestroy();
            _playerManager.OnThisPlayerBuildUpdated -= OnPlayerBuildChanged;
        }
        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                this.enabled = false;
                return;
            }
        }

        private void OnPlayerBuildChanged()
        {
            _activationRequests = new bool[_serverCharacter.GetActiveFrame().GetSlotGFXCount()];
            _coreSystemActivationRequest = false;
        }


        public void ActivateSlot(int slotIndex)
        {
            if (slotIndex >= _activationRequests.Length)
                return; // Outwith our slots count.

            // Anticipate the weapon's effect (For audio & hit markers).
            if (_serverCharacter.CanPerformActionInstantly)
                _serverCharacter.ClientCharacter.AnticipateActionOwnerRpc(CreateRequestData(slotIndex.ToSlotIndex()));

            ActivateSlotServerRpc(slotIndex);
        }
        [Rpc(SendTo.Server)]
        private void ActivateSlotServerRpc(int slotIndex, RpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != this.OwnerClientId)
                return; // Not sent by the correct client.
            if (slotIndex >= _activationRequests.Length)
                return; // Outwith our slot count.

            // Valid activation input.
            StartUsingSlottable(slotIndex.ToSlotIndex());
        }
        [Rpc(SendTo.Server)]
        public void DeactivateSlotServerRpc(int slotIndex, RpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != this.OwnerClientId)
                return; // Not sent by the correct client.
            if (slotIndex >= _activationRequests.Length)
                return; // Outwith our slot count.

            // Valid deactivation input.
            StopUsingSlottable(slotIndex.ToSlotIndex());
        }

        private void StartUsingSlottable(AttachmentSlotIndex attachmentSlotIndex)
        {
            ActionRequestData actionRequestData = CreateRequestData(attachmentSlotIndex);

            if (_serverCharacter.ActionPlayer.IsActionOnCooldown(actionRequestData.ActionID, actionRequestData.AttachmentSlotIndex))
            {
                // Our action is currently on cooldown. Cache our desire to activate this action.
                _activationRequests[attachmentSlotIndex.GetSlotIndex()] = true;
            }
            else
            {
                // Request to play our action.
                _activationRequests[attachmentSlotIndex.GetSlotIndex()] = false;
                _serverCharacter.PlayAction_Server(ref actionRequestData);
            }
        }
        private ActionRequestData CreateRequestData(AttachmentSlotIndex attachmentSlotIndex)
        {
            ActionRequestData actionRequestData = ActionRequestData.Create(_serverCharacter.GetModuleDataForSlotIndex(attachmentSlotIndex).AssociatedAction.ActionID, _serverCharacter.GetSourceObjectIDForSlotIndex(attachmentSlotIndex));

            // Setup the ActionRequestData.
            actionRequestData.AttachmentSlotIndex = attachmentSlotIndex;
            actionRequestData.TransformRelation = attachmentSlotIndex.ToTransformRelation();

            return actionRequestData;
        }
        private void StopUsingSlottable(AttachmentSlotIndex attachmentSlotIndex)
        {
            _activationRequests[attachmentSlotIndex.GetSlotIndex()] = false;
            if (_serverCharacter.GetModuleDataForSlotIndex(attachmentSlotIndex).AssociatedAction.ActivationStyle != ActionActivationStyle.Held)
                return; // Don't cancel this action on release.


            // Cancel the action triggered from this slot.
            _serverCharacter.CancelActionBySlotServerRpc(attachmentSlotIndex);
        }



        public void ActivateCoreSystem()
        {
            // Anticipate the weapon's effect (For audio & hit markers).
            if (_serverCharacter.CanPerformActionInstantly)
                _serverCharacter.ClientCharacter.AnticipateActionOwnerRpc(CreateCoreSystemRequestData());

            ActivateCoreSystemServerRpc();
        }
        public void DeactivateCoreSystem()
        {
            DeactivateCoreSystemServerRpc();
        }
        [Rpc(SendTo.Server)]
        private void ActivateCoreSystemServerRpc(RpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != this.OwnerClientId)
                return; // Not sent by the correct client.

            // Valid activation input.
            StartUsingCoreSystem();
        }
        [Rpc(SendTo.Server)]
        private void DeactivateCoreSystemServerRpc(RpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != this.OwnerClientId)
                return; // Not sent by the correct client.

            // Valid deactivation input.
            StopUsingCoreSystem();
        }

        private void StartUsingCoreSystem()
        {
            ActionRequestData actionRequestData = CreateCoreSystemRequestData();

            if (_serverCharacter.ActionPlayer.IsActionOnCooldown(actionRequestData.ActionID, actionRequestData.AttachmentSlotIndex))
            {
                // Our action is currently on cooldown. Cache our desire to activate this action.
                _coreSystemActivationRequest = true;
            }
            else
            {
                // Request to play our action.
                _coreSystemActivationRequest = false;
                _serverCharacter.PlayAction_Server(ref actionRequestData);
            }
        }
        private ActionRequestData CreateCoreSystemRequestData()
        {
            ActionRequestData actionRequestData = ActionRequestData.Create(_serverCharacter.GetCoreSystemData().ActiveActionDefinition.ActionID, _serverCharacter.GetSourceObjectIDForCoreSystem());
            actionRequestData.TransformRelation = TransformRelation.CoreSystem;

            return actionRequestData;
        }
        private void StopUsingCoreSystem()
        {
            _coreSystemActivationRequest = false;
            if (_serverCharacter.GetCoreSystemData().ActiveActionDefinition.ActivationStyle != ActionActivationStyle.Held)
                return; // Don't cancel this action on release.

            // Cancel the action triggered from this slot.
            _serverCharacter.CancelActionByIDServerRpc(_serverCharacter.GetCoreSystemData().ActiveActionDefinition.ActionID);
        }


        private void Update()
        {
            for(int i = 0; i < _activationRequests.Length; ++i)
            {
                if (_activationRequests[i] == true)
                {
                    StartUsingSlottable(i.ToSlotIndex());
                }
            }

            if (_coreSystemActivationRequest)
                StartUsingCoreSystem();
        }
    }
}