using Gameplay.Actions.Visuals;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Actions.Effects
{
    public class SyncedSpawnableMines_Client : SpawnableObject_Client
    {
        [SerializeReference][SubclassSelector] private ActionVisual[] _detonationVisuals;
        [SerializeReference][SubclassSelector] private ActionVisual[] _hitVisuals;



        [Rpc(SendTo.ClientsAndHost)]
        public void TriggerDetonationVisualsClientRpc()
        {
            for(int i = 0; i < _detonationVisuals.Length; ++i)
            {
                _detonationVisuals[i].OnClientUpdate(null, transform.position, transform.up);
            }
        }

        [Rpc(SendTo.ClientsAndHost)]
        public void TriggerHitVisualsClientRpc(ulong triggeringClientId, Vector3 hitPoint, Vector3 hitNormal, float chargePercentage)
        {
            HitEffectManager.PlayHitEffectsLocally(_hitVisuals, triggeringClientId, hitPoint, hitNormal, chargePercentage);
        }
    }
}