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


        #region Empty Server Functions

        protected override void Trigger_Server(ServerCharacter character, float lifetime, float timeSinceDesiredUpdate) { }

        #endregion


        protected override void Trigger_Client(ClientCharacter character, float lifetime, float timeSinceDesiredUpdate)
        {
            if (!character.IsLocalPlayer())
                return; // Only trigger on local player.

            RadarManager.Instance.StartRadarPings(_radarPingFrequency);
        }

        protected override void OnConditionFailed_Client(ClientCharacter character)
        {
            if (!character.IsLocalPlayer())
                return; // Only trigger on local player.

            if (_disableOnConditionFail)
                 StopPings();
        }
        public override void Stop_Client(ClientCharacter character)
        {
            if (!character.IsLocalPlayer())
                return; // Only trigger on local player.

            StopPings();
        }

        private void StopPings() => RadarManager.Instance.StopRadarPings();
    }
}