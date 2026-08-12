namespace Rubrehose.Data
{
    // Full 6-biome sequence from GAME_DESIGN.md's biome table — the order is already
    // locked, even though only Wreck Beach (index 0) has real terrain/content built.
    // Drives the fast-travel ribbon (CAMERA_AND_UI_SPEC.md), which reads
    // PlayerState.biomeUnlocked to decide how many of these are shown.
    public static class BiomeCatalog
    {
        public static readonly string[] Names =
        {
            "Wreck Beach", "The Shallows", "The Green", "The Bluffs", "The Hollow", "The Deep Reef"
        };
    }
}
