using Gameplay.Actions;
using Gameplay.GameplayObjects.Character;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Passives
{
    /// <summary>
    ///     Instance class container for a <see cref="PassiveDefinition"/>.<br/>
    ///     Passives are started & updated on the Server.
    /// </summary>
    public class Passive
    {
        /// <summary>
        ///     The passive instance's corresponding definition.
        /// </summary>
        private readonly PassiveDefinition _definition;
        /// <inheritdoc cref="Passive._definition"/>
        public PassiveDefinition Definition => _definition;


        /// <inheritdoc cref="PassiveDefinition.PassiveID"/>
        public PassiveID PassiveID { get => Definition.PassiveID; }



        public float TimeStarted { get; set; }
        public float TimeRunning { get; set; }

        private float[] _lastSuccessfulTriggerTimes;
        private bool[] _passiveActiveStates;


        public Passive(PassiveDefinition definition)
        {
            this._definition = definition;
        }


        #region Server-side

        public void Start_Server(ServerCharacter character)
        {
            TimeStarted = NetworkManager.Singleton.ServerTime.TimeAsFloat;
            TimeRunning = 0.0f;
            Definition.StartEffects_Server(character, out _lastSuccessfulTriggerTimes, out _passiveActiveStates);

            character.ClientCharacter.AddPassiveClientRpc(PassiveID, TimeStarted);
        }
        public void Update_Server(ServerCharacter character, float deltaTime)
        {
            // Update definition effects.
            Definition.UpdateEffects_Server(character, TimeRunning, deltaTime, ref _lastSuccessfulTriggerTimes, ref _passiveActiveStates);

            TimeRunning += deltaTime;
        }
        public void Stop_Server(ServerCharacter character)
        {
            Definition.Stop_Server(character);
        }

        #endregion


        #region Client-side

        public void Start_Client(ClientCharacter character, float serverStartTime)
        {
            TimeStarted = serverStartTime;
            TimeRunning = 0.0f;
            Definition.StartEffects_Client(character, out _lastSuccessfulTriggerTimes, out _passiveActiveStates);
        }
        public void Update_Client(ClientCharacter character, float deltaTime)
        {
            // Update definition effects.
            Definition.UpdateEffects_Client(character, TimeRunning, deltaTime, ref _lastSuccessfulTriggerTimes, ref _passiveActiveStates);

            TimeRunning += deltaTime;
        }
        public void Stop_Client(ClientCharacter character)
        {
            Definition.Stop_Client(character);
        }

        #endregion
    }
}