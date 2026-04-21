using Gameplay.GameplayObjects.Character;
using UnityEngine;

namespace Gameplay.Passives
{
    /// <summary>
    ///     The Definition for a Passive that affects a Character.
    /// </summary>
    [CreateAssetMenu(menuName = "Passives/Passive")]
    public class PassiveDefinition : ScriptableObject
    {
        /// <summary>
        ///     An index into the <see cref="Gameplay.GameplayObjects.GameDataSource"/> array of Passive prototypes.<br/>
        ///     Set at runetime by <see cref="Gameplay.GameplayObjects.GameDataSource"/>
        ///     If the passive is not itself a prototype, it will contain the PassiveID of the prototype reference.<br/>
        ///     
        ///     This field is used to identify passive in a way that can be sent over the network.
        /// </summary>
        /// <remarks> Non-serialized, so it doesn't get saved between editor sessions.</remarks>
        [field: System.NonSerialized] public PassiveID PassiveID { get; set; }

        [SerializeReference, SubclassSelector] private PassiveEffect[] _effects;


        #region Server-side

        public void StartEffects_Server(ServerCharacter character, out float[] lastSuccessfulTriggerTimes, out bool[] passiveActiveStates)
        {
            int repeatingEffectsCount = 0;
            for(int i = 0; i < _effects.Length; ++i)
            {
                _effects[i].Start_Server(character);

                if (_effects[i].PerformsUpdates())
                    ++repeatingEffectsCount;
            }

            // Construct an array of length equal to the number of actions that will trigger multiple times for use in the 'UpdateEffects' function to track successful trigger timings.
            lastSuccessfulTriggerTimes = new float[repeatingEffectsCount];
            passiveActiveStates = new bool[repeatingEffectsCount];
        }
        public void UpdateEffects_Server(ServerCharacter character, float lifetime, float deltaTime, ref float[] lastSuccessfulTriggerTimes, ref bool[] passiveActiveStates)
        {
            int updatedEffectsIndex = 0;
            for(int i = 0; i < _effects.Length; ++i)
            {
                if (!_effects[i].PerformsUpdates())
                    continue;   // Effect is only triggered on start.

                float timeSinceLastTrigger = Time.time - lastSuccessfulTriggerTimes[updatedEffectsIndex];

                if (_effects[i].Update_Server(character, lifetime, deltaTime, timeSinceLastTrigger, ref passiveActiveStates[i]))
                    lastSuccessfulTriggerTimes[updatedEffectsIndex] = Time.time;    // Successfully triggered.

                ++updatedEffectsIndex;
            }
        }
        public void Stop_Server(ServerCharacter character)
        {
            for(int i = 0; i < _effects.Length; ++i)
                _effects[i].Stop_Server(character);
        }

        #endregion


        #region Client-side

        public void StartEffects_Client(ClientCharacter character, out float[] lastSuccessfulTriggerTimes, out bool[] passiveActiveStates)
        {
            int repeatingEffectsCount = 0;
            for (int i = 0; i < _effects.Length; ++i)
            {
                _effects[i].Start_Client(character);

                if (_effects[i].PerformsUpdates())
                    ++repeatingEffectsCount;
            }

            // Construct an array of length equal to the number of actions that will trigger multiple times for use in the 'UpdateEffects' function to track successful trigger timings.
            lastSuccessfulTriggerTimes = new float[repeatingEffectsCount];
            passiveActiveStates = new bool[repeatingEffectsCount];
        }
        public void UpdateEffects_Client(ClientCharacter character, float lifetime, float deltaTime, ref float[] lastSuccessfulTriggerTimes, ref bool[] passiveActiveStates)
        {
            int updatedEffectsIndex = 0;
            for (int i = 0; i < _effects.Length; ++i)
            {
                if (!_effects[i].PerformsUpdates())
                    continue;   // Effect is only triggered on start.

                float timeSinceLastTrigger = Time.time - lastSuccessfulTriggerTimes[updatedEffectsIndex];

                if (_effects[i].Update_Client(character, lifetime, deltaTime, timeSinceLastTrigger, ref passiveActiveStates[i]))
                    lastSuccessfulTriggerTimes[updatedEffectsIndex] = Time.time;    // Successfully triggered.

                ++updatedEffectsIndex;
            }
        }
        public void Stop_Client(ClientCharacter character)
        {
            for (int i = 0; i < _effects.Length; ++i)
                _effects[i].Stop_Client(character);
        }

        #endregion
    }
}