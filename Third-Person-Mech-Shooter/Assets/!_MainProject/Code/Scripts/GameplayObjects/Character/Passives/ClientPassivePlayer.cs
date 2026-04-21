using System.Collections.Generic;
using Gameplay.GameplayObjects.Character;

namespace Gameplay.Passives
{
    public class ClientPassivePlayer
    {
        private ClientCharacter _clientCharacter;

        private List<Passive> _activePassives;


        public ClientPassivePlayer(ClientCharacter clientCharacter)
        {
            this._clientCharacter = clientCharacter;

            _activePassives = new List<Passive>();
        }
        public void OnUpdate(float deltaTime)
        {
            foreach (Passive passive in _activePassives)
                passive.Update_Client(_clientCharacter, deltaTime);
        }


        public void AddPassive(PassiveDefinition definition, float serverStartTime)
        {
            Passive passive = new Passive(definition);

            passive.Start_Client(_clientCharacter, serverStartTime);
            _activePassives.Add(passive);
        }
        public void SuppressPassive(PassiveDefinition definition)
        {
            throw new System.NotImplementedException("Passive Suppression isn't implemented");
        }

        public void RemovePassive(PassiveDefinition definition)
        {
            for (int i = _activePassives.Count - 1; i >= 0; --i)
                if (_activePassives[i].Definition == definition)
                    RemovePassive(i);
        }
        public void RemovePassive(PassiveID passiveID)
        {
            for (int i = 0; i < _activePassives.Count; ++i)
                if (_activePassives[i].PassiveID == passiveID)
                    RemovePassive(i);
        }
        private void RemovePassive(int passiveIndex)
        {
            _activePassives[passiveIndex].Stop_Client(_clientCharacter);
            _activePassives.RemoveAt(passiveIndex);
        }

        public void ClearAllPassives()
        {
            for (int i = _activePassives.Count - 1; i >= 0; --i)
                RemovePassive(i);
        }
    }
}