namespace Rubrehose.Data
{
    // Full 6-biome sequence from GAME_DESIGN.md's original biome table. Unused for now:
    // CORE_PROGRESSION_RESTRUCTURE.md retires "biome" as a near-term concept — Wreck Beach's
    // 4 coves (WreckBeachData.CoveNames) are the entire base game, and the fast-travel ribbon
    // (FastTravelRibbonController) now paginates those coves directly instead of this list.
    // Kept for the doc's repurposed meaning: "a future, rare, endgame-only full island
    // expansion," not part of routine progression — wire it up again if/when that gets built.
    public static class BiomeCatalog
    {
        public static readonly string[] Names =
        {
            "Wreck Beach", "The Shallows", "The Green", "The Bluffs", "The Hollow", "The Deep Reef"
        };
    }
}
