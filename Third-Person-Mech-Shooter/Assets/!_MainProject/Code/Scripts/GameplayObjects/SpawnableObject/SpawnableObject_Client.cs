using UnityEngine;
using Unity.Netcode;
using UnityEngine.SocialPlatforms;
using VisualEffects;

namespace Gameplay.Actions.Effects
{
    // Visuals of a SpawnableObject.
    public class SpawnableObject_Client : NetworkBehaviour
    {
        private Transform _attachedTransform;
        private Vector3 _localPosition;
        private Vector3 _localUp;
        private Vector3 _localForward;

        private int _onDestroyFXIndex;


        public override void OnNetworkSpawn()
        {
            if (!IsClient)
            {
                this.enabled = false;
                return;
            }
        }


        [Rpc(SendTo.ClientsAndHost)]
        public void SpawnRpc(Vector3 pos, Vector3 forward, Vector3 up, int destroyFXIndex)
        {
            this.gameObject.SetActive(true);

            this._attachedTransform = null;

            this.transform.position = pos;
            this.transform.rotation = Quaternion.LookRotation(forward, up);


            this._onDestroyFXIndex = destroyFXIndex;
        }
        [Rpc(SendTo.ClientsAndHost)]
        public void SpawnRpc(ulong attachmentObjectID, Vector3 localPos, Vector3 localForward, Vector3 localUp, int destroyFXIndex)
        {
            this.gameObject.SetActive(true);

            this._attachedTransform = NetworkManager.SpawnManager.SpawnedObjects[attachmentObjectID].transform;

            this._localPosition = localPos;
            this._localForward = localForward;
            this._localUp = localUp;


            this._onDestroyFXIndex = destroyFXIndex;
        }
        [Rpc(SendTo.ClientsAndHost)]
        public void ReturnedToPoolRpc()
        {
            // Reset attachment variables.
            _attachedTransform = null;
            this.gameObject.SetActive(false);

            PlayFXGraphic(SpecialFXPoolManager.GetFromPrefab(SpecialFXList.AllOptionsDatabase.SpecialFXGraphics[_onDestroyFXIndex]));
        }


        private void PlayFXGraphic(SpecialFXGraphic graphic)
        {
            graphic.SpecialFXListIndex = _onDestroyFXIndex;
            graphic.OnShutdownComplete += SpecialFXGraphic_OnShutdownComplete;
            graphic.transform.position = transform.position;
            graphic.transform.rotation = transform.rotation;
            graphic.Play();
        }
        private void SpecialFXGraphic_OnShutdownComplete(SpecialFXGraphic graphicInstance)
        {
            graphicInstance.OnShutdownComplete -= SpecialFXGraphic_OnShutdownComplete;
            SpecialFXPoolManager.ReturnFromIndex(graphicInstance.SpecialFXListIndex, graphicInstance);
        }


        private void LateUpdate()
        {
            if (_attachedTransform == null)
            {
                return;
            }

            transform.position = _attachedTransform.TransformPoint(_localPosition);
            transform.rotation = Quaternion.LookRotation(_attachedTransform.TransformDirection(_localForward), _attachedTransform.TransformDirection(_localUp));
        }
    }
}