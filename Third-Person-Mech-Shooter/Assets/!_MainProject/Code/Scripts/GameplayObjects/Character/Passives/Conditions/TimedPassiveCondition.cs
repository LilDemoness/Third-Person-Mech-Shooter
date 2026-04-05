using Gameplay.GameplayObjects.Character;
using UnityEngine;

namespace Gameplay.Passives
{
    /// <summary>
    ///     A <see cref="PassiveCondition"/> based on triggering after fixed periods of time.
    /// </summary>
    [System.Serializable]
    public class TimedPassiveCondition : PassiveCondition
    {
        [SerializeField, Min(0)] private float _period;

        public override bool TestCondition(ServerCharacter character, float lifetime, float timeSinceLastUpdateCall, float timeSinceLastTrigger, out float timeSinceDesiredUpdate)
        {
            // Calculate if we've passed an update period since the last 'Update' call.
            Debug.LogWarning("This doesn't consider what happens if the Trigger is postponed by another condition. It will treat the passive as having been triggered and will wait.");
            timeSinceDesiredUpdate = lifetime % _period;
            return timeSinceDesiredUpdate <= timeSinceLastTrigger;
        }
    }
}