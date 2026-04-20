using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Gameplay.GameplayObjects.Character;
using Gameplay.GameplayObjects.Character.Customisation.Sections;
using Gameplay.GameplayObjects.Character.Customisation.Data;
using Gameplay.Actions;
using UserInput;
using Unity.Netcode.Components;
using Netcode.ConnectionManagement;
using System.Collections;
using System.Linq;
using System;

namespace Gameplay.GameplayObjects.Players
{
    public class Player : NetworkBehaviour
    {
        public static Player LocalClientInstance { get; private set; }
        [SerializeField] private PersistentPlayerRuntimeCollection _runtimeCollection;
        private PersistentPlayer _persistentPlayer;



        [field: Header("Player Component References")]
        [field: SerializeField] public ServerCharacter ServerCharacter { get; private set; }
        [field: SerializeField] public NetworkTransform NetworkTransform { get; private set; }




        //private Dictionary<AttachmentSlotIndex, SlotGFXSection> _slotIndexToActiveGFXDict = new Dictionary<AttachmentSlotIndex, SlotGFXSection>();


        public static event System.Action OnLocalPlayerSet;



        /// <summary>
        ///     Called when we've updated this player's build.
        /// </summary>
        // Client & Server.
        public event System.Action OnThisPlayerBuildUpdated;
        /// <summary>
        ///     Called when we've updated the local player's build.
        /// </summary>
        // Client-side.
        public static event System.Action<BuildData> OnLocalPlayerBuildUpdated;

        /// <summary>
        ///     Called when any player is killed.
        /// </summary>
        // Client-side.
        public static event System.EventHandler<PlayerDeathEventArgs> OnPlayerDeath;

        /// <summary>
        ///     Called when the local player is killed.
        /// </summary>
        // Client-side.
        public static event System.EventHandler<PlayerDeathEventArgs> OnLocalPlayerDeath;

        /// <summary>
        ///     Called when the local player is revived.
        /// </summary>
        // Client-side.
        public static event System.EventHandler OnLocalPlayerRevived;


        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                LocalClientInstance = this;
                Debug.Log("Local Player", this);

                OnLocalPlayerSet?.Invoke();
            }

            if (IsServer)
            {
                ServerCharacter.NetworkHealthComponent.OnRevived += NotifyOwnerOfRespawn;
                ServerCharacter.NetworkHealthComponent.OnDied += Server_OnDied;
            }

            // Cache the corresponding PersistentPlayer and perform any required initialisations.
            // Performed after an 'Initialisation Frame' (1 Frame) to allow for the PersistentPlayer instances to populate the collection on a connecting client.
            StartCoroutine(GetPersistentPlayerAfterInitialisationFrame());
        }
        private IEnumerator GetPersistentPlayerAfterInitialisationFrame()
        {
            yield return null;  // Wait a frame to allow for the PersistentPlayerRunetimeCollection to be setup on the connecting client.

            // Cache our linked PersistentPlayer.
            if (!_runtimeCollection.TryGetPlayer(this.OwnerClientId, out _persistentPlayer))
                throw new System.Exception($"No PersistenPlayer Reference for Client: {OwnerClientId}");

            // When the PersistentPlayer's Build changes, update this player instance's build
            _persistentPlayer.NetworkBuildState.OnBuildChanged += OnBuildChanged;
            OnBuildChanged(_persistentPlayer.NetworkBuildState.BuildDataReference); // Ensure that we sync our initial state (Change to trigger after a frame if we are having issues here).
        }
        public override void OnNetworkDespawn()
        {
            if (_persistentPlayer != null)
                _persistentPlayer.NetworkBuildState.OnBuildChanged -= OnBuildChanged;

            if (IsServer)
            {
                // Update our Player Data (If there is one).
                Transform movementTransform = ServerCharacter.Movement.transform;
                SessionPlayerData? sessionPlayerData = SessionManager<SessionPlayerData>.Instance.GetPlayerData(OwnerClientId);
                if (sessionPlayerData.HasValue)
                {
                    // Update the Player Data struct with Runtime Values (Position, Health, Etc; NOT: Build, Name, and other persistent values).
                    SessionPlayerData playerData = sessionPlayerData.Value;
                    playerData.PlayerPosition = movementTransform.position;
                    playerData.PlayerRotation = movementTransform.rotation;
                    playerData.CurrentHealth = ServerCharacter.NetworkHealthComponent.GetCurrentHealth();
                    playerData.HasCharacterSpawned = true;

                    // Set the player data to its updated value.
                    SessionManager<SessionPlayerData>.Instance.SetPlayerData(OwnerClientId, playerData);
                }

                if (ServerCharacter != null && ServerCharacter.NetworkHealthComponent != null)
                {
                    ServerCharacter.NetworkHealthComponent.OnDied -= Server_OnDied;
                    ServerCharacter.NetworkHealthComponent.OnRevived -= NotifyOwnerOfRespawn;
                }
            }

            if (IsClient)
            {

            }
        }



        /// <summary>
        ///     The player's build has been changed. Update the player's graphics and subsequent cached values.
        /// </summary>
        private void OnBuildChanged(BuildData buildData)
        {
            Debug.Log("Build Changed");
            ServerCharacter.BuildDataReference = buildData;

            OnThisPlayerBuildUpdated?.Invoke();
            if (IsOwner)
                OnLocalPlayerBuildUpdated?.Invoke(buildData);
        }


        // Server-only. Notify clients of death.
        private void Server_OnDied(NetworkHealthComponent.BaseDamageReceiverEventArgs e)
        {
            NotifyClientsOfDeath(e.Inflicter);
            //NotifyOwnerOfDeath(e.Inflicter);
            //OnPlayerDeath?.Invoke(this, new PlayerDeathEventArgs(this.ServerCharacter, e.Inflicter));
        }

        // Server-only. Passes to Clients & Host.
        private void NotifyClientsOfDeath(ServerCharacter inflicter)
        {
            if (inflicter != null)
                NotifyOfDeathClientRpc(this.ServerCharacter.OwnerClientId, inflicter.NetworkObjectId);
            else
                NotifyOfDeathByGameClientRpc(this.ServerCharacter.OwnerClientId);
        }
        /// <summary>
        ///     Called on clients when this player dies.
        /// </summary>
        /// <remarks> If there is no Inflicter Object (Such as if the change was caused by the server), instead use <see cref="OnPlayerDiedByGameOwnerRpc()"/></remarks>
        [Rpc(SendTo.ClientsAndHost)]
        public void NotifyOfDeathClientRpc(ulong targetClientId, ulong inflicterObjectId)
        {
            if (targetClientId != this.OwnerClientId)
                return;

            ServerCharacter inflicter = NetworkManager.Singleton.SpawnManager.SpawnedObjects[inflicterObjectId].GetComponent<ServerCharacter>();
            if (IsOwner)
                OnLocalPlayerDied(inflicter);
            OnPlayerDied(inflicter);
        }
        /// <summary>
        ///     Called on clients when this player dies from an unknown source, such as the Game itself.
        /// </summary>
        [Rpc(SendTo.ClientsAndHost)]
        public void NotifyOfDeathByGameClientRpc(ulong targetClientId)
        {
            if (targetClientId != this.OwnerClientId)
                return;

            Debug.Log($"{this.GetComponent<Utils.NetworkNameState>().Name.Value} Killed by Game");
            if (IsOwner)
                OnLocalPlayerDied(null);
            OnPlayerDied(null);
        }

        private void OnPlayerDied(ServerCharacter inflicter) => OnPlayerDeath?.Invoke(this, new PlayerDeathEventArgs(this.ServerCharacter, inflicter));



        /// <summary>
        ///     Notify the owning client that their player has died.<br/>
        ///     Argument passing choice is handled within this function.
        /// </summary>
        private void NotifyOwnerOfDeath(ServerCharacter inflicter)
        {
            if (inflicter == null)
                OnPlayerDiedByGameOwnerRpc();
            else
                OnPlayerDiedOwnerRpc(inflicter.NetworkObjectId);
        }
        /// <summary>
        ///     Called on the owning client when the player dies.
        /// </summary>
        /// <remarks> If there is no Inflicter Object (Such as if the change was caused by the server), instead use <see cref="OnPlayerDiedByGameOwnerRpc()"/></remarks>
        [Rpc(SendTo.Owner)]
        public void OnPlayerDiedOwnerRpc(ulong inflicterObjectId)
        {
            Debug.Log("Died");
            OnLocalPlayerDied(NetworkManager.Singleton.SpawnManager.SpawnedObjects[inflicterObjectId].GetComponent<ServerCharacter>());
        }
        /// <summary>
        ///     Called on the owning client when the player dies from an unknown source, such as the Game itself.
        /// </summary>
        [Rpc(SendTo.Owner)]
        public void OnPlayerDiedByGameOwnerRpc() => OnLocalPlayerDied(null);
        /// <summary>
        ///     Handles player-specific client-side death logic (Input, Notifying the Respawn Screen, etc).<br/>
        ///     Called when the local player has died.
        /// </summary>
        /// <param name="inflicter"> The ServerCharacter that caused this player to die.</param>
        private void OnLocalPlayerDied(ServerCharacter inflicter)
        {
            Debug.Log("Local Death");
            ClientInput.AddActionPrevention(typeof(Player), ClientInput.ActionTypes.Respawning);         // Prevent Input.
            OnLocalPlayerDeath?.Invoke(this, new PlayerDeathEventArgs(ServerCharacter, inflicter)); // Notify Listeners (Respawn Screen, etc).
        }

        private void NotifyOwnerOfRespawn(NetworkHealthComponent.BaseDamageReceiverEventArgs _) => OnPlayerRespawnPerformedOwnerRpc();
        /// <summary>
        ///     Notify the owning client that their player has been respawned.
        /// </summary>
        private void NotifyOwnerOfRespawn() => OnPlayerRespawnPerformedOwnerRpc();
        /// <summary>
        ///     Called on the owning client when the player has been respawned.
        /// </summary>
        [Rpc(SendTo.Owner)]
        private void OnPlayerRespawnPerformedOwnerRpc()
        {
            Debug.Log("Is Owner: " + IsOwner);
            ClientInput.RemoveActionPrevention(typeof(Player), ClientInput.ActionTypes.Respawning);
            OnLocalPlayerRevived?.Invoke(this, System.EventArgs.Empty);
        }



        /// <summary>
        ///     Forces a respawn of the local player's ServerCharacter.
        /// </summary>
        public static void ForceRespawn() => LocalClientInstance.ForceRespawnServerRpc();
        [Rpc(SendTo.Server)]
        private void ForceRespawnServerRpc(RpcParams rpcParams = default)
        {
            ServerCharacter.NetworkHealthComponent.SetLifeState_Server(ServerCharacter, LifeState.Dead);
        }



        public Vector3 GetPosition() => transform.position;
        public Vector3 GetForward() => ServerCharacter.Movement.RotationPivot.forward;


        public class PlayerDeathEventArgs : CharacterDeadEventArgs
        {
            public PlayerDeathEventArgs(CharacterDeadEventArgs baseArgs) : base(baseArgs.Character, baseArgs.Inflicter)
            { }
            public PlayerDeathEventArgs(ServerCharacter character, ServerCharacter inflicter) : base(character, inflicter)
            { }
        }
        /*
        public class PlayerDeathEventArgs : System.EventArgs
        {
            ServerCharacter.CharacterDeadEventArgs CharacterDeathArgs;

            public PlayerDeathEventArgs(ServerCharacter.CharacterDeadEventArgs characterDeathArgs)
            {
                this.CharacterDeathArgs = characterDeathArgs;
            }
        }
        */
    }
}