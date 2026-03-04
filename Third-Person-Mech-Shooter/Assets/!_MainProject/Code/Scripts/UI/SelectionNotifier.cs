using UnityEngine;
using UnityEngine.EventSystems;

namespace Gameplay.UI
{
    public class SelectionNotifier : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        public event System.Action<BaseEventData> OnSelected;
        public event System.Action<BaseEventData> OnDeselected;

        
        public void OnSelect(BaseEventData eventData) => OnSelected?.Invoke(eventData);
        public void OnDeselect(BaseEventData eventData) => OnDeselected?.Invoke(eventData);
    }
}