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
        [SerializeField] private bool _triggerAtStart = true;
        [SerializeReference, SubclassSelector] private PassiveCondition[] _passiveConditions;


        public bool PerformsUpdates() => _updates;

        #region Server-side

        public void Start_Server(ServerCharacter character)
        {
            if (!_triggerAtStart)
                return;

            Trigger_Server(character, 0.0f, 0.0f);
        }
        public bool Update_Server(ServerCharacter character, float lifetime, float timeSinceLastUpdateCall, float timeSinceLastTrigger, ref bool passiveActiveState)
        {
            if (!_updates)
                return false;

            // Evaluate Conditions.
            float minTimeSinceDesiredUpdate = float.PositiveInfinity;
            for(int i = 0; i < _passiveConditions.Length; ++i)
            {
                if (!_passiveConditions[i].TestCondition(character, lifetime, timeSinceLastUpdateCall, timeSinceLastTrigger, out float timeSinceDesiredUpdate))
                {
                    if (passiveActiveState)
                    {
                        Debug.Log("Condition Failed");
                        OnConditionFailed_Server(character);
                    }

                    passiveActiveState = false;
                    return false; // A Condition Failed.
                }

                if (timeSinceDesiredUpdate < minTimeSinceDesiredUpdate)
                    minTimeSinceDesiredUpdate = timeSinceDesiredUpdate;
            }
            if (!passiveActiveState)
            {
                Debug.Log("Condition Passed");
                Trigger_Server(character, lifetime, minTimeSinceDesiredUpdate);
            }
            
            passiveActiveState = true;
            return true;
        }
        public virtual void Stop_Server(ServerCharacter character) { }



        /// <summary>
        ///     Triggers the Passive Effect.<br/>
        ///     For passives that only trigger once, this is called when initialised/applied. Otherwise, this is applied whenever the passive's condition returns true.
        /// </summary>
        /// <param name="character"> The ServerCharacter this passive call is to be applied to.</param>
        /// <param name="lifetime"> The total Lifetime of the effect.</param>
        /// <param name="timeSinceDesiredUpdate"> The time (In Seconds) since this trigger was actually supposed to be performed.</param>
        protected abstract void Trigger_Server(ServerCharacter character, float lifetime, float timeSinceDesiredUpdate);
        /// <summary>
        ///     Called when a condition is failed when the passive is updating.
        /// </summary>
        /// <param name="character"> The ServerCharacter this passive call is to be applied to.</param>
        protected virtual void OnConditionFailed_Server(ServerCharacter character) { }

        #endregion


        #region Client-side

        public void Start_Client(ClientCharacter character)
        {
            if (!_triggerAtStart)
                return;

            Trigger_Client(character, 0.0f, 0.0f);
        }
        public bool Update_Client(ClientCharacter character, float lifetime, float timeSinceLastUpdateCall, float timeSinceLastTrigger, ref bool passiveActiveState)
        {
            if (!_updates)
                return false;

            // Evaluate Conditions.
            float minTimeSinceDesiredUpdate = float.PositiveInfinity;
            for (int i = 0; i < _passiveConditions.Length; ++i)
            {
                if (!_passiveConditions[i].TestCondition(character.ServerCharacter, lifetime, timeSinceLastUpdateCall, timeSinceLastTrigger, out float timeSinceDesiredUpdate))
                {
                    if (passiveActiveState)
                    {
                        Debug.Log("Condition Failed");
                        OnConditionFailed_Client(character);
                    }

                    passiveActiveState = false;
                    return false; // A Condition Failed.
                }

                if (timeSinceDesiredUpdate < minTimeSinceDesiredUpdate)
                    minTimeSinceDesiredUpdate = timeSinceDesiredUpdate;
            }
            if (!passiveActiveState)
            {
                Debug.Log("Condition Passed");
                Trigger_Client(character, lifetime, minTimeSinceDesiredUpdate);
            }

            passiveActiveState = true;
            return true;
        }
        public virtual void Stop_Client(ClientCharacter character) { }



        /// <summary>
        ///     Triggers the Passive Effect on Clients.<br/>
        ///     For passives that only trigger once, this is called when initialised/applied. Otherwise, this is applied whenever the passive's condition returns true.<br/>
        ///     Note: This calls on ALL ClientCharacters, so for effects that only apply on the local ClientCharacter, check using character.IsLocalPlayer().
        /// </summary>
        /// <param name="character"> The <see cref="ClientCharacter"/> this passive call is to be applied to.</param>
        /// <param name="lifetime"> The total Lifetime of the effect.</param>
        /// <param name="timeSinceDesiredUpdate"> The time (In Seconds) since this trigger was actually supposed to be performed.</param>
        protected virtual void Trigger_Client(ClientCharacter character, float lifetime, float timeSinceDesiredUpdate) { }
        /// <summary>
        ///     Called when a condition is failed when the passive is updating on Clients.<br/>
        ///     Note: This calls on ALL ClientCharacters, so for effects that only apply on the local ClientCharacter, check using character.IsLocalPlayer().
        /// </summary>
        /// <param name="character"> The <see cref="ClientCharacter"/> this passive call is to be applied to.</param>
        protected virtual void OnConditionFailed_Client(ClientCharacter character) { }

        #endregion
    }
}