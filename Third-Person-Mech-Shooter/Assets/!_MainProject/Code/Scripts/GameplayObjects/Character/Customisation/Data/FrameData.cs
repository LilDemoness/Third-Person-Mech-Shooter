using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.GameplayObjects.Character.Customisation.Data
{
    [CreateAssetMenu(menuName = "Data/FrameData")]
    public class FrameData : BaseCustomisationData
    {
        public enum SizeCategory { Small, Medium, Large }

        [field: Header("Frame Data")]
        [field: SerializeField] public SizeCategory FrameSize { get; private set; }
        [field: SerializeField] public int MaxHealth { get; private set; }
        [field: SerializeField] public int HeatCapacity { get; private set; }
        [field: SerializeField] public float MovementSpeed { get; private set; }


        [field:Space(10)]
        [field: SerializeField] public AttachmentPoint[] AttachmentPoints { get; private set; }
        [field: SerializeField] public CoreSystemData CoreSystem { get; private set; }


        [field: Header("Frame Camera Settings")]
        [field: SerializeField] public Vector3 ThirdPersonCameraOffset { get; private set; }
        [field: SerializeField] public float CameraVerticalArmLength { get; private set; }
        [field: SerializeField] public float CameraDistance { get; private set; }
    }

    [System.Serializable]
    public class AttachmentPoint
    {
        [field: SerializeField] public ModuleSize MaxModuleSize { get; private set; }

        public List<ModuleData> ValidModuleDatas => CustomisationOptionsDatabase.AllOptionsDatabase.GetValidModulesForSize(MaxModuleSize);
    }

    [System.Serializable]
    public enum ModuleSize
    {
        Auxilary,
        Main,
        Heavy
    }
    public static class ModuleSizeExtensions
    {
        public static string ToDisplayString(this ModuleSize moduleSize) => moduleSize switch
        {
            ModuleSize.Auxilary => "Aux",
            ModuleSize.Main => "Main",
            ModuleSize.Heavy => "Heavy",
            _ => throw new System.NotImplementedException($"No Display String for Module Size: {moduleSize.ToString()}")
        };
    }
}