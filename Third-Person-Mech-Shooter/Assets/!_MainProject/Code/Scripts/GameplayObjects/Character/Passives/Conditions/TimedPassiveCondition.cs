using Gameplay.GameplayObjects.Character;
using UnityEngine;

namespace Gameplay.Passives
{
    /// <summary>
    ///     A <see cref="PassiveCondition"/> based on triggering after a fixed period of time since the last successful update.
    /// </summary>
    [System.Serializable]
    public class TimedPassiveCondition : PassiveCondition
    {
        [SerializeField, Min(0)] private float _period;

        public override bool TestCondition(ServerCharacter character, float lifetime, float timeSinceLastUpdateCall, float timeSinceLastSuccessfulUpdate, out float timeSinceDesiredUpdate)
        {
            // Calculate if we've passed an update period since the last 'Update' call.
            timeSinceDesiredUpdate = Mathf.Max(timeSinceLastSuccessfulUpdate - _period, 0.0f);
            return timeSinceLastSuccessfulUpdate >= _period;
        }
    }
}