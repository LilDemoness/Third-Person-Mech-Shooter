using Gameplay.GameplayObjects.Character;
using UnityEngine;

namespace Gameplay.Passives
{
    /// <summary>
    ///     Instance class container for a <see cref="PassiveDefinition"/>.
    /// </summary>
    public class Passive
    {
        public PassiveDefinition Definition { get; set; }

        public float TimeStarted { get; set; }
        public float TimeRunning { get; set; }

        private float[] _lastSuccessfulTriggerTimes;



        public void Start(ServerCharacter serverCharacter)
        {
            TimeStarted = Time.time;
            Definition.StartEffects(serverCharacter, out _lastSuccessfulTriggerTimes);
        }
        public void Update(ServerCharacter character, float deltaTime)
        {
            // Update definition effects.
            Definition.UpdateEffects(character, TimeRunning, deltaTime, ref _lastSuccessfulTriggerTimes);

            TimeRunning += deltaTime;
        }
    }
}