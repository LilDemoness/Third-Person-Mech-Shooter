using UnityEngine;
using Gameplay.GameplayObjects.Character;
using VisualEffects;

namespace Gameplay.Actions.Visuals
{
    /// <summary>
    ///     Spawns and deletes a SpecialFXGraphic.
    /// </summary>
    [System.Serializable]
    public class SpecialFXActionVisual : ActionVisual
    {
        [SerializeField] private SpecialFXGraphic _specialFXGraphic;

        [SerializeField] private bool _parentToOriginator = false;

        [Space(5)]
        [SerializeField] private Vector3 _originOffset = Vector3.zero;
        [SerializeField] private bool _localOffset = false;


        protected override void Trigger(ClientCharacter clientCharacter, in Vector3 origin, in Vector3 direction) => ActivateSpecialFXGraphic(clientCharacter, origin, direction);
        


        private void ActivateSpecialFXGraphic(ClientCharacter clientCharacter, in Vector3 origin, in Vector3 direction)
        {
            if (_specialFXGraphic == null)
                return;

            SpecialFXGraphic specialFXGraphic = SpecialFXPoolManager.GetFromPrefab(_specialFXGraphic);
            if (_parentToOriginator)
                specialFXGraphic.transform.SetParent(clientCharacter.ServerCharacter.Movement.RotationPivot);

            specialFXGraphic.transform.position = origin;
            specialFXGraphic.transform.rotation = Quaternion.LookRotation(direction);

            if (_originOffset != Vector3.zero)
            {
                if (_localOffset)
                    specialFXGraphic.transform.localPosition += _originOffset;
                else    
                    specialFXGraphic.transform.position += _originOffset;
            }


            specialFXGraphic.OnShutdownComplete += SpecialFXGraphic_OnShutdownComplete;

            specialFXGraphic.Play();
        }
        private void SpecialFXGraphic_OnShutdownComplete(SpecialFXGraphic graphicInstance)
        {
            graphicInstance.OnShutdownComplete -= SpecialFXGraphic_OnShutdownComplete;
            SpecialFXPoolManager.ReturnFromPrefab(_specialFXGraphic, graphicInstance);
        }
    }
}