using Gameplay.GameplayObjects;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Gameplay.Actions.Effects
{
    /// <summary>
    ///     Server logic for server-client synced spawnable mines.
    /// </summary>
    [RequireComponent(typeof(SyncedSpawnableMines_Client))]
    public class SyncedSpawnableMines_Server : SpawnableObject_Server
    {
        [System.Serializable, System.Flags]
        private enum DetonationTypes
        {
            Lifetime = 1 << 0,
            Collision = 1 << 1,
            Radius = 1 << 2,

            Everything = ~0
        }
        
        
        private SyncedSpawnableMines_Client _clientMinesScript => (ClientScript as SyncedSpawnableMines_Client);


        [Header("Detection")]
        [SerializeField] private DetonationTypes _detonationType = DetonationTypes.Lifetime | DetonationTypes.Radius;
        [SerializeField] private bool _canDetectFriendlies = false;

        [Space(5)]
        [SerializeField] private float _detectionRadius = 3.0f;
        [SerializeField] private LayerMask _validLayers;


        [Header("Arming")]
        [SerializeField] private float _armingTime = 0.0f;
        private float _armingTimeRemaining;
        private bool _isArming;


        [Header("Detonating")]
        [SerializeField] private float _detonationDelay = 0.0f;
        private float _detonationDelayRemaining;
        private bool _isDetonating;

        [SerializeReference][SubclassSelector] private ActionEffect[] _detonationEffects;


        [Header("Hit Effects")]
        [SerializeField] private AnimationCurve _distanceEffectScaleCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0.0f, 1.0f), new Keyframe(1.0f, 1.0f) });
        [SerializeReference][SubclassSelector] private ActionEffect[] _hitEffects;



        protected override void FinishSetup()
        {
            ResetAllInstanceVariables();
            _armingTimeRemaining = _armingTime;
            _isArming = true;
        }
        public override void ReturnedToPool()
        {
            base.ReturnedToPool();
            ResetAllInstanceVariables();
        }
        private void ResetAllInstanceVariables()
        {
            _isArming = false;
            _armingTimeRemaining = 0.0f;
            _isDetonating = false;
            _detonationDelayRemaining = 0.0f;
        }


        private void Update()
        {
            if (_isArming)
            {
                _armingTimeRemaining -= Time.deltaTime;

                if (_armingTimeRemaining <= 0.0f)
                    _isArming = false;
                else
                    return;
            }

            if (_isDetonating)
            {
                _detonationDelayRemaining -= Time.deltaTime;
                if (_detonationDelayRemaining <= 0.0f)
                {
                    Detonate();
                }
            }
            else if (ShouldDetonateForRadius())
            {
                StartDetonationCountdown();
            }
        }


        /// <summary>
        ///     Returns true if this instance should start detonating due to a valid entity in its Detonation Radius.</br>
        ///     Always returns false if _detonationType doesn't contain DetonationTypes.Radius.
        /// </summary>
        private bool ShouldDetonateForRadius()
        {
            if (!_detonationType.HasFlag(DetonationTypes.Radius))
                return false;

            foreach (Collider hitCollider in Physics.OverlapSphere(transform.position, _detectionRadius, _validLayers, QueryTriggerInteraction.Ignore))
            {
                if (IsColliderObstructed(hitCollider))
                    continue;   // The collider is obstructed.
                if (!ValidColliderForDetonation(hitCollider))
                    continue;

                return true;
            }

            return false;
        }
        /// <summary>
        ///     Returns true if a collider is a valid entity to detonate for.
        /// </summary>
        private bool ValidColliderForDetonation(Collider testCollider)
        {
            if (!_canDetectFriendlies && IsFriendlyEntity(testCollider))
                return false;   // We don't want to detect friendlies, and this is a friendly entity.

            return true;
        }


        protected override void OnLifetimeElapsed()
        {
            if (_detonationType.HasFlag(DetonationTypes.Lifetime))
                DetonateInstantly();
        }


        /// <summary>
        ///     Determines if a collider is obstructed.
        /// </summary>
        /// <remarks>
        ///     Assumes that the target is in range.
        /// </remarks>
        private bool IsColliderObstructed(Collider collider)
        {
            if (Physics.Linecast(transform.position, collider.transform.position, out RaycastHit hitInfo, _validLayers, QueryTriggerInteraction.Ignore))
            {
                // Potentially Obstructed.
                if (!collider.IsParentOrChildOf(hitInfo.transform))
                    return true;   // We've hit something that isn't our test collider, and so the target is obstructed.
            }

            // Target is unobstructed.
            return false;
        }
        private bool IsFriendlyEntity(Collider collider)
        {
            // Temp.
            return collider.IsParentOrChildOf(Owner.transform);
        }



        public bool StartDetonationCountdown()
        {
            if (_isArming)
                return false;
            
            _isDetonating = true;
            _detonationDelayRemaining = _detonationDelay;
            return true;
        }

        public bool DetonateInstantly() => DetonateInstantly(false);
        public bool DetonateInstantly(bool ignoreArmingState)
        {
            if (_isArming && !ignoreArmingState)
                return false;

            _isDetonating = true;
            Detonate();
            return true;
        }

        public void DetonateInstantlyOrDestroy()
        {
            if (DetonateInstantly(ignoreArmingState: false))
            { }
            else
                TriggerReturnToPool();
        }

        private void Detonate()
        {
            // Play any detonation effects.
            ActionHitInformation detonationEffectHitInfo = new ActionHitInformation(transform, transform.position, transform.up, transform.forward);
            for (int i = 0; i < _detonationEffects.Length; ++i)
                _detonationEffects[i].ApplyEffect(Owner, detonationEffectHitInfo, 1.0f);
            _clientMinesScript.TriggerDetonationVisualsClientRpc();

            // Determine Hit Entities.
            HashSet<HitInformationContainer> hitEntities = new(comparer: new HitInformationContainerEqualityComparer());
            foreach (Collider hitCollider in Physics.OverlapSphere(transform.position, _detectionRadius, _validLayers, QueryTriggerInteraction.Ignore))
            {
                if (IsColliderObstructed(hitCollider))
                    continue;   // Obstructed.
                if (!hitCollider.TryGetComponentThroughParents<IDamageable>(out IDamageable damageableScript))
                    continue;   // Not damageable.

                hitEntities.Add(new HitInformationContainer(damageableScript, hitCollider));
            }

            foreach(HitInformationContainer hitEntity in hitEntities)
            {
                Debug.Log("Hit: " + hitEntity.Collider.transform.name, hitEntity.Collider);
                Vector3 hitPoint = hitEntity.Collider.ClosestPoint(transform.position);
                Vector3 hitNormal = hitPoint != transform.position ? (hitPoint - transform.position).normalized : (hitEntity.Collider.transform.position - transform.position).normalized;
                
                PrepareAndProcessTarget(hitEntity.Collider.transform, hitPoint, hitNormal);
            }

            // Destroy Self.
            TriggerReturnToPool();
        }
        private readonly struct HitInformationContainer
        {
            public readonly IDamageable DamageableScript;
            public readonly Collider Collider;

            public HitInformationContainer(IDamageable damageable, Collider hitCollider)
            {
                this.DamageableScript = damageable;
                this.Collider = hitCollider;
            }
        }
        // Using an IEqualityComparer to allow us to specify what is compared within our HitInformationContainer when adding to a HashSet.
        private class HitInformationContainerEqualityComparer : IEqualityComparer<HitInformationContainer>
        {
            public bool Equals(HitInformationContainer? info1, HitInformationContainer? info2)
            {
                if (info1 is null || info2 is null)
                    return false;
                return Equals(info1.Value, info2.Value);
            }
            public bool Equals(HitInformationContainer info1, HitInformationContainer info2)
            {
                if (ReferenceEquals(info1, info2))
                    return true;

                return info1.DamageableScript == info2.DamageableScript;
            }
            public int GetHashCode(HitInformationContainer infoContainer) => infoContainer.DamageableScript.GetHashCode();
        }



        /// <summary>
        ///     Prepares the raycast's data into a form that lets us process a hit target, then process it.
        /// </summary>
        private void PrepareAndProcessTarget(Transform hitTransform, Vector3 hitPoint, Vector3 hitNormal)
        {
            // Calculate required information.
            float scalePercentage = _distanceEffectScaleCurve.Evaluate(Vector3.Distance(hitTransform.position, hitPoint));

            // Create the actionHitInfo & process it.
            ActionHitInformation actionHitInfo = new ActionHitInformation(hitTransform, hitPoint, hitNormal, GetHitForward(hitNormal));
            ProcessTarget(actionHitInfo, scalePercentage);


            // Helper Function to calculate the HitForward value from a given normal.
            Vector3 GetHitForward(Vector3 hitNormal)
            {
                return Mathf.Approximately(Mathf.Abs(Vector3.Dot(hitNormal, Vector3.right)), 1.0f)
                    ? Vector3.Cross(hitNormal, Vector3.left)
                    : Vector3.Cross(hitNormal, Vector3.right);
            }
        }
        /// <summary>
        ///     Process a target hit by this Action.
        /// </summary>
        private void ProcessTarget(in ActionHitInformation hitInfo, float chargePercentage)
        {
            // Notify the client-side script to play its hit effects locally on all clients.
            _clientMinesScript.TriggerHitVisualsClientRpc(Owner.OwnerClientId, hitInfo.HitPoint, hitInfo.HitNormal, chargePercentage);

            // Perform this action's effects (Damage, Applying Statuses, etc) on the server (Changes are perpetuated to clients).
            for (int i = 0; i < _hitEffects.Length; ++i)
            {
                Debug.Log("Target: " + hitInfo.Target);
                _hitEffects[i].ApplyEffect(Owner, hitInfo, chargePercentage);
            }
        }



        private void OnTriggerEnter(Collider other)
        {
            if (!_detonationType.HasFlag(DetonationTypes.Collision))
                return;
            if (_isArming)
                return;

            if (ValidColliderForDetonation(other))
                DetonateInstantly();
        }


#if UNITY_EDITOR

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _detectionRadius);
        }

#endif
    }
}