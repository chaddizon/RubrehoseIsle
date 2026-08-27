using System;

namespace Rubrehose.Data
{
    // Direct port of the locked formulas from GAME_DESIGN.md. BiomeMultiplier/ClearMultiplier
    // cover all 6 biomes since Armor/HP/construction costs are biome-generic, even though
    // only biome 0 (Wreck Beach) is wired up in this vertical slice.
    public static class GameFormulas
    {
        public static readonly double[] BiomeMultiplier = { 1, 9.5, 90.25, 857.4, 8145, 77380 };
        public static readonly double[] ClearMultiplier = { 5, 15, 35, 105, 315, 945 };

        // Base coefficients (36000 HP / 15 Armor for cove 0 of biome 0) deliberately retuned
        // far above GAME_DESIGN.md's original 50/8 — see the "why is the first serpent so
        // easy" discussion this replaces. With SalvagePower's early curve and a real 20-min
        // cooldown between attempts (GameManager.CanAttemptFight/FightCooldownSeconds), the
        // goal is Landing Cove taking on the order of 50-60 real attempts to clear: a floor of
        // several real days even for someone hitting every cooldown window during all their
        // waking hours, and multiple weeks for a couple-check-ins-a-day casual player — slow,
        // long-term progression is the point (art-production pacing + Obelisk-style long-haul
        // investment), not a bug. Armor still requires a small early tap-level investment
        // before any damage lands (~5 upgrades, well under 200 driftwood) so the fight isn't
        // silently a no-op before that, matching the pre-existing "damage below armor deals
        // zero" rule. This is a first-pass estimate, not playtested over real elapsed days —
        // retune these two coefficients (only) once real clear times are observed; the
        // per-cove/per-biome growth curves below are unchanged on purpose.
        public static double SerpentHp(int biomeIndex, int coveIndex) =>
            Math.Round(36000 * Math.Pow(2.5, coveIndex) * BiomeMultiplier[biomeIndex]);

        public static double SerpentArmor(int biomeIndex, int coveIndex) =>
            Math.Round(15 * Math.Pow(2.8, coveIndex) * BiomeMultiplier[biomeIndex]);

        public static double SalvagePower(int tapLevel) =>
            (tapLevel + 1) * (tapLevel + 2) / 2.0;

        public static double TapUpgradeCost(int tapLevel) =>
            Math.Round(20 * Math.Pow(1.15, tapLevel));

        public static double ConstructionCost(int biomeIndex) =>
            500 * ClearMultiplier[biomeIndex];

        public static double SerpentClearReward(int biomeIndex) =>
            50 * ClearMultiplier[biomeIndex];

        public const float FightDurationSeconds = 30f;

        // "Cooldown = 20 minutes between attempts on same cove's serpent" (GAME_DESIGN.md),
        // matches Obelisk's real model exactly (rubrehose_prototype.html).
        public const float FightCooldownSeconds = 20f * 60f;
    }
}
