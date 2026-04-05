using Gameplay.GameplayObjects.Character;
using UnityEngine;

namespace Gameplay.Passives
{
    /// <summary>
    ///     An effect within a <see cref="ServerCharacter"/>'s Passive, such as Damage Reduction or periodically triggering an action.
    /// </summary>
    [System.Serializable]
    public abstract class PassiveEffect
    {
        [SerializeField] private bool _updates = false;
        [SerializeField] private bool _triggerAtStart = false;
        [SerializeReference, SubclassSelector] private PassiveCondition[] _passiveConditions;


        public bool PerformsUpdates() => _updates;

        public void Start(ServerCharacter character)
        {
            if (!_triggerAtStart)
                return;

            throw new System.NotImplementedException("PassiveEffect Start() Not Implemented");
        }
        public bool Update(ServerCharacter character, float lifetime, float timeSinceLastUpdateCall, float timeSinceLastTrigger)
        {
            if (!_updates)
                return false;

            // Evaluate Conditions.
            float maxTimeSinceDesiredUpdate = 0.0f;
            for(int i = 0; i < _passiveConditions.Length; ++i)
            {
                if (!_passiveConditions[i].TestCondition(character, lifetime, timeSinceLastUpdateCall, timeSinceLastTrigger, out float timeSinceDesiredUpdate))
                {
                    OnConditionFailed(character);
                    return false; // A Condition Failed.
                }

                if (timeSinceDesiredUpdate > maxTimeSinceDesiredUpdate)
                    maxTimeSinceDesiredUpdate = timeSinceDesiredUpdate;
            }

            Trigger(character, lifetime, maxTimeSinceDesiredUpdate);
            return true;
        }



        /// <summary>
        ///     Triggers the Passive Effect.<br/>
        ///     For passives that only trigger once, this is called when initialised/applied. Otherwise, this is applied whenever the passive's condition returns true.
        /// </summary>
        /// <param name="character"> The ServerCharacter this passive call is to be applied to.</param>
        /// <param name="lifetime"> The total Lifetime of the effect.</param>
        /// <param name="timeSinceDesiredUpdate"> The time (In Seconds) since this trigger was actually supposed to be performed.</param>
        protected abstract void Trigger(ServerCharacter character, float lifetime, float timeSinceDesiredUpdate);
        /// <summary>
        ///     Called when a condition is failed when the passive is updating.
        /// </summary>
        /// <param name="character"> The ServerCharacter this passive call is to be applied to.</param>
        protected virtual void OnConditionFailed(ServerCharacter character) { }
    }
}