using Gameplay.GameplayObjects.Character;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Actions.Effects
{
    [System.Serializable]
    public class EnableEnemyOutlinesEffect : ActionEffect
    {
        public override void ApplyEffect(ServerCharacter owner, in ActionHitInformation hitInfo, float chargePercentage)
        {
            owner.EnableOutlinesForEnemyTeamRpc(owner.RpcTarget.Single(owner.OwnerClientId, RpcTargetUse.Temp));
        }

        public override void Cleanup(ServerCharacter owner)
        {
            owner.DisableOutlinesForEnemyTeamRpc(owner.RpcTarget.Single(owner.OwnerClientId, RpcTargetUse.Temp));
        }
    }
}