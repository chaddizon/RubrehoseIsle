using System;

namespace Rubrehose.Data
{
    [Serializable]
    public class CrewDefinition
    {
        public string id;
        public string displayName;
        public double baseCost;
        public double ratePerSecond; // Driftwood/s per level, before any future multipliers

        public CrewDefinition(string id, string displayName, double baseCost, double ratePerSecond)
        {
            this.id = id;
            this.displayName = displayName;
            this.baseCost = baseCost;
            this.ratePerSecond = ratePerSecond;
        }
    }

    // BBW & BBC are the only crew recruitable at Wreck Beach (GAME_DESIGN.md crew table).
    public static class CrewCatalog
    {
        public static readonly CrewDefinition[] WreckBeachCrew =
        {
            new CrewDefinition("bbw", "BBW", 25, 0.5),
            new CrewDefinition("bbc", "Big Bad 'Coon", 25, 0.5),
        };

        public static CrewDefinition Find(string id) => Array.Find(WreckBeachCrew, d => d.id == id);
    }
}
