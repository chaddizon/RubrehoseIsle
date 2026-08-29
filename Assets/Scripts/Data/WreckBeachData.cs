namespace Rubrehose.Data
{
    // Wreck Beach's 4 coves ARE the entire base game now (CORE_PROGRESSION_RESTRUCTURE.md
    // "The core change") — not a tutorial leading into 5 more biomes. Cove/serpent names
    // match the doc's unlock table; only Landing Cove (index 0) has real world geometry so
    // far (LandingCoveBuilder.cs). Tide Pools/Foraging Grounds are the doc's own working
    // names, "open to change"; the 4th cove's name is explicitly left blank by the doc
    // ("needs a name") — "The Deep" below is a placeholder, not a creative decision, same
    // for its serpent's "Leviathan".
    public static class WreckBeachData
    {
        public const int BiomeIndex = 0;
        public const string BiomeName = "Wreck Beach";
        public const string CurrencyName = "Driftwood";

        // Index 3 (the last entry) is the permanent/endless cove — GameManager.IsEndlessCove.
        // Indices 0-2 keep the one-time mini-boss model (PlayerState.coveMinibossDefeated,
        // sized 3) — mini-boss defeat now advances coveIndex immediately, no separate
        // construction-gate payment step (removed 2026-08-27, see
        // CORE_PROGRESSION_RESTRUCTURE.md).
        public static readonly string[] CoveNames = { "Landing Cove", "Tide Pools", "Foraging Grounds", "The Deep" };
        public static readonly string[] SerpentNames = { "Hatchling", "Hatchling", "Shoal-back", "Leviathan" };
    }
}
