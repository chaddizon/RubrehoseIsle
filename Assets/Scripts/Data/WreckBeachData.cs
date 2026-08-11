namespace Rubrehose.Data
{
    // Wreck Beach = biome index 0. Cove/serpent names match the Phase 1 art checklist
    // (rubrehose_art_checklist.md): only Hatchling and Shoal-back appear in this biome.
    public static class WreckBeachData
    {
        public const int BiomeIndex = 0;
        public const string BiomeName = "Wreck Beach";
        public const string CurrencyName = "Driftwood";
        public const string ConstructionMaterial = "Bound Rope";

        public static readonly string[] CoveNames = { "Landing Cove", "Debris Field", "Low Tide Flats" };
        public static readonly string[] SerpentNames = { "Hatchling", "Hatchling", "Shoal-back" };
    }
}
