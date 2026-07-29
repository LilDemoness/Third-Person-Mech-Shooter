using UnityEngine;

namespace Gameplay.Animations.InverseKinematics
{
    public class IKTrigger : MonoBehaviour
    {
        [SerializeField] private IKBase[] _ikInstances;

        private void Reset() => _ikInstances = GetComponentsInChildren<IKBase>();
        private void Update()
        {
            for(int i = 0; i < _ikInstances.Length; ++i)
                if (_ikInstances[i].isActiveAndEnabled)
                    _ikInstances[i].ProcessIK(Time.deltaTime);
        }
    }
}