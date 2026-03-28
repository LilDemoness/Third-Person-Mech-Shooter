using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.GameplayObjects.Character.Customisation.Data
{
    [CreateAssetMenu(menuName = "Customisation Options Database")]
    public class CustomisationOptionsDatabase : ScriptableObject
    {
        [System.NonSerialized]
        private static CustomisationOptionsDatabase  s_allOptionsDatabase;
        public static CustomisationOptionsDatabase AllOptionsDatabase
        {
            get => s_allOptionsDatabase ??= Resources.Load<CustomisationOptionsDatabase>(ALL_OPTIONS_DATABASE_PATH);
        }
        private const string ALL_OPTIONS_DATABASE_PATH = "PlayerData/AllPlayerCustomisationOptions";
        public const int MAX_SLOTTABLE_DATAS = 4;


        [field: SerializeField, ReadOnlyInPlayMode] public List<FrameData> FrameDatas;
        [System.NonSerialized] private Dictionary<FrameData, int> _frameDataToIndexDict;

        [field: SerializeField, ReadOnlyInPlayMode] public List<ModuleData> AllModuleDatas;
        private Dictionary<ModuleSize, List<ModuleData>> _moduleSizeToModuleDatasDict;
        [System.NonSerialized] private Dictionary<ModuleData, int> _moduleDataToIndexDict;


        private void InitialiseFrameDataDict()
        {
            _frameDataToIndexDict = new Dictionary<FrameData, int>();
            for (int i = 0; i < FrameDatas.Count; ++i)
                _frameDataToIndexDict.Add(FrameDatas[i], i);
        }
        private void InitialiseModuleDataDict()
        {
            _moduleDataToIndexDict = new Dictionary<ModuleData, int>();
            _moduleSizeToModuleDatasDict = new Dictionary<ModuleSize, List<ModuleData>>()
            {
                { ModuleSize.Auxilary, new List<ModuleData>() },
                { ModuleSize.Main, new List<ModuleData>() },
                { ModuleSize.Heavy, new List<ModuleData>() },
            };

            for(int i = 0; i < AllModuleDatas.Count; ++i)
            {
                _moduleDataToIndexDict.Add(AllModuleDatas[i], i);
                _moduleSizeToModuleDatasDict[AllModuleDatas[i].ModuleSize].Add(AllModuleDatas[i]);
            }
        }


        // Getters with Null Fallback for out of range indicies.
        public FrameData GetFrame(int index) => IsWithinBounds(index, FrameDatas.Count) ? FrameDatas[index] : null;
        public int GetIndexForFrameData(FrameData frameData)
        {
            if (_frameDataToIndexDict == null)
                InitialiseFrameDataDict();
            return _frameDataToIndexDict[frameData];
        }

        public ModuleData GetModuleData(int index) => IsWithinBounds(index, AllModuleDatas.Count) ? AllModuleDatas[index] : null;
        public int GetIndexForModuleData(ModuleData slottableData)
        {
            if (_moduleDataToIndexDict == null)
                InitialiseModuleDataDict();
            return _moduleDataToIndexDict[slottableData];
        }
        public List<ModuleData> GetAllModulesForSize(ModuleSize moduleSize)
        {
            if (_moduleSizeToModuleDatasDict == null)
                InitialiseModuleDataDict();
            return _moduleSizeToModuleDatasDict[moduleSize];
        }
        public List<ModuleData> GetValidModulesForSize(ModuleSize moduleSize)
        {
            if (_moduleSizeToModuleDatasDict == null)
                InitialiseModuleDataDict();
            List<ModuleData> validModules = new List<ModuleData>();

            switch(moduleSize)
            {
                default: throw new System.NotImplementedException($"No valid modules check implemented for module size '{moduleSize.ToString()}'");
                case ModuleSize.Heavy:
                    validModules.AddRange(_moduleSizeToModuleDatasDict[ModuleSize.Heavy]);
                    goto case ModuleSize.Main;  // Fallthrough.
                case ModuleSize.Main:
                    validModules.AddRange(_moduleSizeToModuleDatasDict[ModuleSize.Main]);
                    goto case ModuleSize.Auxilary;  // Fallthrough.
                case ModuleSize.Auxilary:
                    validModules.AddRange(_moduleSizeToModuleDatasDict[ModuleSize.Auxilary]);
                    break;
            }

            return validModules;
        }


        private bool IsWithinBounds(int value, int arrayLength)
        {
            return value >= 0 && value < arrayLength;
        }



        #if UNITY_EDITOR

        [ContextMenu("Update Values")]
        private void UpdateValues()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("Cannot update CustomisationOptionsDatabase while in Play Mode");
                return;
            }

            FrameDatas = new List<FrameData>(Resources.LoadAll<FrameData>(string.Empty));
            AllModuleDatas = new List<ModuleData>(Resources.LoadAll<ModuleData>(string.Empty));

            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.Selection.objects = new[] { this };
            UnityEditor.EditorApplication.delayCall += () => UnityEditor.Selection.objects = System.Array.ConvertAll(UnityEditor.Selection.objects, target => (CustomisationOptionsDatabase)target);
        }

        #endif
    }
}