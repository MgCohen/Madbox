namespace Madbox.Battle
{
    public static class WeaponProfiles
    {
        public static WeaponProfile CurvedSword { get; } = CreateCurvedSword();

        public static WeaponProfile GreatSword { get; } = CreateGreatSword();

        public static WeaponProfile LongSword { get; } = CreateLongSword();

        private static WeaponProfile CreateCurvedSword()
        {
            WeaponId id = new WeaponId("curved-sword");
            return new WeaponProfile(
                id,
                CooldownSeconds: 0.45f,
                Range: 3.5f,
                MovementSpeedMultiplier: 1.1f,
                AttackTimingNormalized: 0.4f);
        }

        private static WeaponProfile CreateGreatSword()
        {
            WeaponId id = new WeaponId("great-sword");
            return new WeaponProfile(
                id,
                CooldownSeconds: 0.75f,
                Range: 4.2f,
                MovementSpeedMultiplier: 0.9f,
                AttackTimingNormalized: 0.55f);
        }

        private static WeaponProfile CreateLongSword()
        {
            WeaponId id = new WeaponId("long-sword");
            return new WeaponProfile(
                id,
                CooldownSeconds: 0.55f,
                Range: 3.8f,
                MovementSpeedMultiplier: 1f,
                AttackTimingNormalized: 0.45f);
        }
    }
}

