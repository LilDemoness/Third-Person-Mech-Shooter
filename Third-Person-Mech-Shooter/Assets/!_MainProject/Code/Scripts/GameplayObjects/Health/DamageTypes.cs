namespace Gameplay
{
    [System.Serializable, System.Flags]
    public enum DamageTypes
    {
        [System.NonSerialized] None = 0,

        // Damage dealt by physical projectiles.
        Ballistic = 1 << 0,
        // Damage dealt from explosives.
        Explosive = 1 << 1,
        // Damage dealt to Heat by enemies (Such as Lasers). (Replace with "ExternalHeatGainRate"?)
        Heat = 1 << 2,
        // Damage taken from Overheating or special enemy abilities.
        Overheating = 1 << 3,

        [System.NonSerialized] AllDamage = ~0
    }
}