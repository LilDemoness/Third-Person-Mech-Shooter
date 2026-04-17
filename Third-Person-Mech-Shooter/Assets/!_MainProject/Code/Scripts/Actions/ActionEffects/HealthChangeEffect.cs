using UnityEngine;
using Gameplay.GameplayObjects.Character;
using Gameplay.GameplayObjects;

namespace Gameplay.Actions.Effects
{
    /// <summary>
    ///     Heals (Increases the health) of the hit <see cref="IDamageable"/>.
    /// </summary>
    [System.Serializable]
    public class HealingEffect : ActionEffect
    {
        [SerializeField] private float _healingValue;
        [SerializeField] private bool _scaleValueWithCharge = true;


        public override void ApplyEffect(ServerCharacter owner, in ActionHitInformation hitInfo, float chargePercentage)
        {
            if (hitInfo.Target.TryGetComponentThroughParents<IDamageable>(out IDamageable damageable))
            {
                damageable.ReceiveHealing_Server(owner, _scaleValueWithCharge ? chargePercentage * _healingValue : _healingValue);
            }
        }
    }

    /// <summary>
    ///     Damages (Decreases the health) of the hit <see cref="IDamageable"/>.
    /// </summary>
    [System.Serializable]
    public class DamageEffect : ActionEffect
    {
        [SerializeField] private float _damageValue;
        [SerializeField] private bool _scaleValueWithCharge = true;
        [SerializeField] private DamageTypes _damageType;


        public override void ApplyEffect(ServerCharacter owner, in ActionHitInformation hitInfo, float chargePercentage)
        {
            if (hitInfo.Target.TryGetComponentThroughParents<IDamageable>(out IDamageable damageable))
            {
                damageable.ReceiveDamage_Server(owner, _scaleValueWithCharge ? chargePercentage * _damageValue : _damageValue, _damageType, -hitInfo.HitNormal);
            }
        }
    }
}