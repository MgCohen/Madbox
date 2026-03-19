namespace Madbox.Battle
{
    public sealed record WeaponProfile(
        WeaponId Id,
        float CooldownSeconds,
        float Range,
        float MovementSpeedMultiplier,
        float AttackTimingNormalized);
}
