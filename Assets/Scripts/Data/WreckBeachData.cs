namespace Rubrehose.Data
{
    // Wreck Beach's 4 coves ARE the entire base game now (CORE_PROGRESSION_RESTRUCTURE.md
    // "The core change") — not a tutorial leading into 5 more biomes. Cove names match the
    // doc's locked table (The Grove/The Deep Reef were locked 2026-08-27, updated here
    // 2026-08-29 — this had been flagged as a stale-vs-doc inconsistency since Tide Pools was
    // built and is now resolved). "Leviathan" for the endless cove's base serpent name is
    // still a placeholder — the doc's real serpent-tier flavor names
    // (Hatchling -> Shoal-back -> Tide-coiled -> Bramblefang -> Storm-wound -> Cave-blind ->
    // Abyssal Coil) aren't wired to serpentLevel milestones yet, deliberately deferred per the
    // doc ("needs a milestone table once real pacing data exists").
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
        public static readonly string[] CoveNames = { "Landing Cove", "Tide Pools", "The Grove", "The Deep Reef" };
        public static readonly string[] SerpentNames = { "Hatchling", "Hatchling", "Shoal-back", "Leviathan" };
    }
}
