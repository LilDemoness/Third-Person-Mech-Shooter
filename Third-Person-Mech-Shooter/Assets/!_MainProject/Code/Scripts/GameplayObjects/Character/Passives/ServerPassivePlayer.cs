using System.Collections.Generic;
using Gameplay.GameplayObjects.Character;
using UnityEngine;

namespace Gameplay.Passives
{
    public class ServerPassivePlayer
    {
        private ServerCharacter _serverCharacter;

        private List<Passive> _activePassives;


        public ServerPassivePlayer(ServerCharacter serverCharacter)
        {
            this._serverCharacter = serverCharacter;

            _activePassives = new List<Passive>();
        }
        public void OnUpdate(float deltaTime)
        {
            foreach(Passive passive in _activePassives)
                passive.Update(_serverCharacter, deltaTime);
        }


        public void AddPassive(PassiveDefinition definition)
        {
            Passive passive = new Passive(definition);

            passive.Start(_serverCharacter);
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
        private void RemovePassive(int passiveIndex)
        {
            _activePassives[passiveIndex].Stop(_serverCharacter);
            _activePassives.RemoveAt(passiveIndex);
        }

        public void ClearAllPassives()
        {
            for(int i = _activePassives.Count - 1; i >= 0; --i)
                RemovePassive(i);
        }
    }
}