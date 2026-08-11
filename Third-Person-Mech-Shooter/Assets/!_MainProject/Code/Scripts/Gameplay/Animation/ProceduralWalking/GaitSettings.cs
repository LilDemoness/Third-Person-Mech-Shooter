using UnityEngine;

namespace Gameplay.Animations.ProceduralAnimations
{
    [CreateAssetMenu(menuName = "Procedural Animation/Gait Settings", fileName = "NewGaitSettings")]
    public class GaitSettings : ScriptableObject
    {
        [field: SerializeField] public float MaxBodyDistanceFromGround { get; private set; } = 0.25f;


        [field: Tooltip("The multiplier applied to the body's current speed when determining how fast the legs should move.")]
        [field: SerializeField] public float LegSpeedMultiplier { get; private set; } = 2.5f;

        [field: Tooltip("The maximum height that the legs are raised during a step.")]
        [field: SerializeField] public float LegLiftHeight { get; private set; } = 0.35f;

        [field: SerializeField] public float ComfortZoneRadius { get; private set; } = 1.2f;


        [field: Tooltip("If true, fallback positions for leg placements are considered if the main candidate is invalid.")]
        [field: SerializeField] public bool LegScanAlternativeGround { get; private set; } = true;
        [field: SerializeField] public float LegScanHeightBias { get; private set; } = 0.5f;


        [field: SerializeField] public float LegLookAheadFraction { get; private set; } = 0.6f;


        [field: SerializeField] public float SamePairCooldown { get; private set; } = 1.0f;
        [field: SerializeField] public float AdjacentPairCooldown { get; private set; } = 1.0f;
    }
}