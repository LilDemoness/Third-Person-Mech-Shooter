using System.Collections;
using UnityEngine;
using Unity.Netcode;
using Gameplay.GameplayObjects.Character;
using UnityEngine.Events;

namespace Gameplay.Actions.Effects
{
    // Server Logic of a SpawnableObject.
    [RequireComponent(typeof(SpawnableObject_Client))]
    public class SpawnableObject_Server : NetworkBehaviour
    {
        protected ServerCharacter Owner;
        protected SpawnableObject_Client ClientScript;


        // Attachment.
        private Transform _attachedTransform;
        private bool _hasAttachedTransform;

        private Vector3 _localPosition;
        private Vector3 _localUp;
        private Vector3 _localForward;


        // Lifetime.
        private Coroutine _handleLifetimeCoroutine;


        public event System.Action<ServerCharacter, SpawnableObject_Server> OnShouldReturnToPool;
        public UnityEvent<SpawnableObject_Server> OnReturnedToPool;


        private void Awake()
        {
            ClientScript = this.GetComponent<SpawnableObject_Client>();
        }
        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                this.enabled = false;
                return;
            }
        }
        /// <summary>
        ///     Setup a SpawnableObject instance after being instantiated or retrieved from an ObjectPool..
        /// </summary>
        /// <param name="owner"> The ServerCharacter who created this object.</param>
        /// <param name="specialFXIndex"> The SpecialFXList's SpecialFXGraphics Index of the OnDestroy VFX.</param>
        /// <param name="lifetime"> How long this object lasts before destroying itself. Leave at 0.0f for infinite duration.</param>
        public void Setup(ServerCharacter owner, int specialFXIndex, float lifetime = 0.0f)
        {
            this.Owner = owner;

            if (_handleLifetimeCoroutine != null)
                StopCoroutine(_handleLifetimeCoroutine);
            if (lifetime > 0.0f)
            {
                _handleLifetimeCoroutine = StartCoroutine(HandleLifetime(lifetime));
            }

            this.ClientScript.SpawnRpc(transform.position, transform.forward, transform.up, specialFXIndex);
            FinishSetup();
        }
        /// <inheritdoc cref="Setup(ServerCharacter, int, float)"/>
        /// <param name="parentObject"> The NetworkObject this SpawnableObject is to be "parented" to.</param>
        public void Setup(ServerCharacter owner, NetworkObject parentObject, int specialFXIndex, float lifetime = 0.0f)
        {
            this.Owner = owner;

            if (_handleLifetimeCoroutine != null)
                StopCoroutine(_handleLifetimeCoroutine);
            if (lifetime > 0.0f)
            {
                _handleLifetimeCoroutine = StartCoroutine(HandleLifetime(lifetime));
            }

            // Note: Also handles calling 'ClientScript.SpawnRpc()'.
            AttachToTransform(parentObject, specialFXIndex);

            // Run any child-specific logic.
            FinishSetup();
        }
        /// <summary>
        ///     Override to perform any child-specific logic during setup.
        /// </summary>
        protected virtual void FinishSetup() { }

        /// <summary>
        ///     Called when the SpawnableObject is returned to its ObjectPool.</br>
        ///     Resets all variables & notifies the Client-side Script.
        /// </summary>
        public virtual void ReturnedToPool()
        {
            // Notify attached objects that we've been returned to the pool.
            OnReturnedToPool?.Invoke(this);

            // Reset variables & ensure that coroutines have stopped (Just in case).
            _attachedTransform = null;
            _hasAttachedTransform = false;

            if (_handleLifetimeCoroutine != null)
                StopCoroutine(_handleLifetimeCoroutine);


            // Notify client-only visuals script.
            ClientScript.ReturnedToPoolRpc();
        }


        /// <summary>
        ///     Attach this spawnable object to a transform and spawn it.
        ///     Similar to parenting them, but done this way to prevent NetworkObject parenting issues.
        /// </summary>
        private void AttachToTransform(NetworkObject parentObject, int specialFXIndex)
        {
            this._attachedTransform = parentObject.transform;
            this._hasAttachedTransform = true;
            if (_attachedTransform.TryGetComponentThroughParents<SpawnableObject_Server>(out SpawnableObject_Server parentSpawnableObject))
            {
                if (!parentSpawnableObject.gameObject.activeSelf)
                {
                    // We're trying to parent to a SpawnableObject that is disabled (Meaning that the spawning of this object caused it to despawn).
                    // Despawn.
                    TriggerReturnToPool();
                    return;
                }

                // Despawn when our parent is returned to the pool (We would lose our parent reference, but due to it being pooled this is how we are checking).
                parentSpawnableObject.OnReturnedToPool.AddListener(ParentSpawnableObject_OnReturnedToPool);
            }
            

            this._localPosition = _attachedTransform.InverseTransformPoint(transform.position);
            this._localForward = _attachedTransform.InverseTransformDirection(transform.forward);
            this._localUp = _attachedTransform.InverseTransformDirection(transform.up);


            // Notify client-only visuals script.
            this.ClientScript.SpawnRpc(parentObject.NetworkObjectId, _localPosition, _localForward, _localUp, specialFXIndex);
        }


        /// <summary>
        ///     Called when the "Parent" SpawnableObject is returned to its ObjectPool.
        /// </summary>
        private void ParentSpawnableObject_OnReturnedToPool(SpawnableObject_Server parentInstance)
        {
            parentInstance.OnReturnedToPool.RemoveListener(ParentSpawnableObject_OnReturnedToPool);
            TriggerReturnToPool();
        }

        private void LateUpdate()
        {
            if (_attachedTransform == null) // Check that our attached transform is still valid.
            {
                // We don't have an attached transform.
                if (_hasAttachedTransform)  // If we used to have an attached transform, then our parent was destroyed and we should destroy ourself.
                {
                    // Notify for returning to the pool only once.
                    TriggerReturnToPool();
                    _hasAttachedTransform = false;
                    return; // We're wishing to destroy ourselves, so don't want to call further logic. [To Confirm]
                }
            }
            else
            {
                // We have a parent transform. Keep our local position consistent.
                HandleParentMotion();
            }
        }
        /// <summary>
        ///     Moves & Rotates this SpawnableObject to match that of it's attached transform.
        /// </summary>
        private void HandleParentMotion()
        {
            transform.position = _attachedTransform.TransformPoint(_localPosition);
            transform.rotation = Quaternion.LookRotation(_attachedTransform.TransformDirection(_localForward), _attachedTransform.TransformDirection(_localUp));
        }

        /// <summary>
        ///     Return ourselves to the pool once the specified lifetime has elapsed.
        /// </summary>
        private IEnumerator HandleLifetime(float lifetime)
        {
            yield return new WaitForSeconds(lifetime);
            OnLifetimeElapsed();
            TriggerReturnToPool();
        }
        protected virtual void OnLifetimeElapsed() { }


        /// <summary>
        ///     Notifies that this SpawnableObject should return to its object pool.
        /// </summary>
        protected void TriggerReturnToPool() => this.OnShouldReturnToPool?.Invoke(Owner, this);
    }
}