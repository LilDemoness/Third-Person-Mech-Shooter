using System.Collections.Generic;
using UnityEngine;
using Gameplay.Actions.Definitions;
using Gameplay.Actions;
using Gameplay.Passives;

namespace Gameplay.GameplayObjects
{
    public class GameDataSource : MonoBehaviour
    {
        public static GameDataSource Instance { get; private set; }


        //[SerializeField] private ActionDefinition m_generalChaseActionDefinition;
        //[SerializeField] private ActionDefinition m_generalTargetActionDefinition;
        //[SerializeField] private ActionDefinition m_stunnedActionDefinition;


        //[Tooltip("All Action Prototype Scriptable Objects")]
        //[SerializeField] private ActionDefinition[] _actionDefinitions;


        //public ActionDefinition GeneralChaseActionDefinition => m_generalChaseActionDefinition;
        //public ActionDefinition GeneralTargetActionDefinition => m_generalTargetActionDefinition;
        //public ActionDefinition StunnedActionDefinition => m_stunnedActionDefinition;

        private List<ActionDefinition> _allActionDefinitions;
        private List<PassiveDefinition> _allPassiveDefinitions;


        private void Awake()
        {
            if (Instance != null)
            {
                throw new System.Exception("Multiple GameDataSources defined");
            }

            BuildActionIDs();
            BuildPassiveIDs();

            DontDestroyOnLoad(this.gameObject);
            Instance = this;
        }
        private void BuildActionIDs()
        {
            HashSet<ActionDefinition> uniqueDefinitions = new HashSet<ActionDefinition>(Resources.LoadAll<ActionDefinition>(""));

            // Add our General Action Prototypes.
            //uniqueDefinitions.Add(m_generalChaseActionDefinition);
            //uniqueDefinitions.Add(m_generalTargetActionDefinition);
            //uniqueDefinitions.Add(m_stunnedActionDefinition);



            // Add all our unique actions to '_allActions' and set their IDs to match.
            int i = 0;
            _allActionDefinitions = new List<ActionDefinition>(uniqueDefinitions.Count);
            foreach(ActionDefinition uniqueDefinition in uniqueDefinitions)
            {
                uniqueDefinition.ActionID = new ActionID(i);
                _allActionDefinitions.Add(uniqueDefinition);
                ++i;
            }
        }
        private void BuildPassiveIDs()
        {
            HashSet<PassiveDefinition> uniqueDefinitions = new HashSet<PassiveDefinition>(Resources.LoadAll<PassiveDefinition>(""));

            // Add all our unique actions to '_allActions' and set their IDs to match.
            int i = 0;
            _allPassiveDefinitions = new List<PassiveDefinition>(uniqueDefinitions.Count);
            foreach (PassiveDefinition uniqueDefinition in uniqueDefinitions)
            {
                uniqueDefinition.PassiveID = new PassiveID(i);
                _allPassiveDefinitions.Add(uniqueDefinition);
                ++i;
            }
        }


        public ActionDefinition GetActionDefinitionByID(ActionID index) => _allActionDefinitions[index.ID];
        public bool TryGetActionDefinitionById(ActionID index, out ActionDefinition definition)
        {
            for(int i = 0; i < _allActionDefinitions.Count; ++i)
            {
                if (_allActionDefinitions[i].ActionID == index)
                {
                    definition = _allActionDefinitions[i];
                    return true;
                }
            }

            definition = null;
            return false;
        }


        public PassiveDefinition GetPassiveDefinitionByID(PassiveID index) => _allPassiveDefinitions[index.ID];
        public bool TryGetPassiveDefinitionById(PassiveID index, out PassiveDefinition definition)
        {
            for (int i = 0; i < _allPassiveDefinitions.Count; ++i)
            {
                if (_allPassiveDefinitions[i].PassiveID == index)
                {
                    definition = _allPassiveDefinitions[i];
                    return true;
                }
            }

            definition = null;
            return false;
        }
    }
}