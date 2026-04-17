using Gameplay.GameplayObjects.Character;
using UnityEngine;

namespace Gameplay.Passives
{
    /// <summary>
    ///     Instance class container for a <see cref="PassiveDefinition"/>.<br/>
    ///     Passives are started & updated on the Server.
    /// </summary>
    public class Passive
    {
        public PassiveDefinition Definition { get; set; }

        public float TimeStarted { get; set; }
        public float TimeRunning { get; set; }

        private float[] _lastSuccessfulTriggerTimes;
        private bool[] _passiveActiveStates;


        public Passive(PassiveDefinition definition)
        {
            this.Definition = definition;
        }


        public void Start(ServerCharacter character)
        {
            TimeStarted = Time.time;
            TimeRunning = 0.0f;
            Definition.StartEffects(character, out _lastSuccessfulTriggerTimes, out _passiveActiveStates);
        }
        public void Update(ServerCharacter character, float deltaTime)
        {
            // Update definition effects.
            Definition.UpdateEffects(character, TimeRunning, deltaTime, ref _lastSuccessfulTriggerTimes, ref _passiveActiveStates);

            TimeRunning += deltaTime;
        }
        public void Stop(ServerCharacter character)
        {
            Definition.Stop(character);
        }
    }
}