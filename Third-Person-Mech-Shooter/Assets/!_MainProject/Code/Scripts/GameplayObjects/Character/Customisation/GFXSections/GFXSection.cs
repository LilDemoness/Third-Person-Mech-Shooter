using UnityEngine;
using Unity.Netcode;

namespace Gameplay.GameplayObjects.Character.Customisation.Sections
{
    public abstract class GFXSection<T> : MonoBehaviour
    {
        [SerializeField] private Transform _abilityOrigin;
        private NetworkObject _parentNetworkObject;

        [field: SerializeField] public T AssociatedData { get; private set; }


        private void Awake()
        {
            _parentNetworkObject = GetComponentInParent<NetworkObject>();
            if (_parentNetworkObject == null)
                this.enabled = false;
        }


        public bool Toggle(T data)
        {
            bool shouldBeActive = ShouldBeActive(data);
            this.gameObject.SetActive(shouldBeActive);
            return shouldBeActive;
        }
        protected virtual bool ShouldBeActive(T data) => data != null && data.Equals(AssociatedData);


        public ulong GetAbilitySourceObjectId() => _parentNetworkObject.NetworkObjectId;
        public Transform GetAbilityOriginTransform() => _abilityOrigin;

        public Vector3 GetAbilityWorldOrigin() => _abilityOrigin.position;
        public Vector3 GetAbilityWorldDirection() => _abilityOrigin.forward;


#if UNITY_EDITOR
        private bool Editor_IsThisOrChildSelected()
        {
            Transform selectedTransform = UnityEditor.Selection.activeTransform;
            while (selectedTransform != null)
            {
                if (selectedTransform == this.transform)
                    return true;

                selectedTransform = selectedTransform.parent;
            }

            return false;
        }

        private void OnDrawGizmos()
        {
            if (_abilityOrigin == null)
                return;
            if (!Editor_IsThisOrChildSelected())
                return;

            Gizmos.color = Color.red;

            //if (_parentNetworkObject == null)
            //{
            Gizmos.DrawSphere(transform.TransformPoint(_abilityOrigin.localPosition), 0.05f);
            Gizmos.DrawRay(transform.TransformPoint(_abilityOrigin.localPosition), transform.TransformDirection(_abilityOrigin.forward));
            /*}
            else
            {
                Gizmos.DrawSphere(GetAbilityLocalOffset(), 0.05f);
                Gizmos.DrawRay(GetAbilityLocalOffset(), GetAbilityLocalDirection());
            }*/
        }
#endif
    }
}