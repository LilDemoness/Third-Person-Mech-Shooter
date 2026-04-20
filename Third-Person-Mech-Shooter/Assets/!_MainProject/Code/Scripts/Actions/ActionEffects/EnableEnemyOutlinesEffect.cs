using Gameplay.GameplayObjects.Character;
using UnityEngine;

namespace Gameplay.Actions.Effects
{
    [System.Serializable]
    public class EnableEnemyOutlinesEffect : ActionEffect
    {
        public override void ApplyEffect(ServerCharacter owner, in ActionHitInformation hitInfo, float chargePercentage)
        {
            ServerCharacter.EnableOutlinesForEnemyTeam(owner);
        }

        public override void Cleanup(ServerCharacter owner)
        {
            ServerCharacter.DisableOutlinesForEnemyTeam();
        }
    }
}