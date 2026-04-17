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
        [SerializeReference, SubclassSelector] private PassiveEffect[] _effects;


        public void StartEffects(ServerCharacter character, out float[] lastSuccessfulTriggerTimes, out bool[] passiveActiveStates)
        {
            int repeatingEffectsCount = 0;
            for(int i = 0; i < _effects.Length; ++i)
            {
                _effects[i].Start(character);

                if (_effects[i].PerformsUpdates())
                    ++repeatingEffectsCount;
            }

            // Construct an array of length equal to the number of actions that will trigger multiple times for use in the 'UpdateEffects' function to track successful trigger timings.
            lastSuccessfulTriggerTimes = new float[repeatingEffectsCount];
            passiveActiveStates = new bool[repeatingEffectsCount];
        }
        public void UpdateEffects(ServerCharacter character, float lifetime, float deltaTime, ref float[] lastSuccessfulTriggerTimes, ref bool[] passiveActiveStates)
        {
            int updatedEffectsIndex = 0;
            for(int i = 0; i < _effects.Length; ++i)
            {
                if (!_effects[i].PerformsUpdates())
                    continue;   // Effect is only triggered on start.

                float timeSinceLastTrigger = Time.time - lastSuccessfulTriggerTimes[updatedEffectsIndex];

                if (_effects[i].Update(character, lifetime, deltaTime, timeSinceLastTrigger, ref passiveActiveStates[i]))
                    lastSuccessfulTriggerTimes[updatedEffectsIndex] = Time.time;    // Successfully triggered.

                ++updatedEffectsIndex;
            }
        }
        public void Stop(ServerCharacter character)
        {
            for(int i = 0; i < _effects.Length; ++i)
                _effects[i].Stop(character);
        }
    }
}