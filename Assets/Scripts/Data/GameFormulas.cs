using System;

namespace Rubrehose.Data
{
    // Direct port of the locked formulas from GAME_DESIGN.md. BiomeMultiplier/ClearMultiplier
    // cover all 6 biomes since Armor/HP/ClearsNeeded are biome-generic, even though only
    // biome 0 (Wreck Beach) is wired up in this vertical slice.
    public static class GameFormulas
    {
        public static readonly double[] BiomeMultiplier = { 1, 9.5, 90.25, 857.4, 8145, 77380 };
        public static readonly double[] ClearMultiplier = { 5, 15, 35, 105, 315, 945 };

        public static double SerpentHp(int biomeIndex, int coveIndex) =>
            Math.Round(50 * Math.Pow(2.5, coveIndex) * BiomeMultiplier[biomeIndex]);

        public static double SerpentArmor(int biomeIndex, int coveIndex) =>
            Math.Round(8 * Math.Pow(2.8, coveIndex) * BiomeMultiplier[biomeIndex]);

        public static int ClearsNeeded(int biomeIndex, int coveIndex) =>
            (int)Math.Round((coveIndex + 1) * ClearMultiplier[biomeIndex]);

        public static double SalvagePower(int tapLevel) =>
            (tapLevel + 1) * (tapLevel + 2) / 2.0;

        public static double TapUpgradeCost(int tapLevel) =>
            Math.Round(20 * Math.Pow(1.15, tapLevel));

        public static double ConstructionCost(int biomeIndex) =>
            500 * ClearMultiplier[biomeIndex];

        public static double SerpentClearReward(int biomeIndex) =>
            50 * ClearMultiplier[biomeIndex];

        public const float FightDurationSeconds = 30f;
    }
}
