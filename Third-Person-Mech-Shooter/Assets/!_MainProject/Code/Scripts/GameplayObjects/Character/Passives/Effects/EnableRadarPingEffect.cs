using Gameplay.GameplayObjects.Character;
using Gameplay.UI.Minimap;
using UnityEngine;

namespace Gameplay.Passives
{
    [System.Serializable]
    public class EnableRadarPingEffect : PassiveEffect
    {
        [SerializeField] private float _radarPingFrequency = 0.5f;
        [SerializeField] private bool _disableOnConditionFail = true;


        protected override void Trigger(ServerCharacter character, float lifetime, float timeSinceDesiredUpdate) => RadarManager.Instance.StartRadarPings(_radarPingFrequency);

        protected override void OnConditionFailed(ServerCharacter character)
        {
            if (_disableOnConditionFail)
                 StopPings();
        }
        public override void Stop(ServerCharacter character) => StopPings();

        private void StopPings() => RadarManager.Instance.StopRadarPings();
    }
}