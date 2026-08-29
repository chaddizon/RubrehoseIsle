using System;

namespace Rubrehose.Data
{
    // Compass Shard rarity tiers (NEXT_CLAUDE_CODE_PUSH.md §1c) — loosely mirrors Idle Obelisk
    // Miner's own Archaeology-Fragment tier concept, not a literal name/count match (doc:
    // "exact names TBD, doesn't need to match Obelisk's literal tier names").
    public static class RarityTier
    {
        public const string Common = "common";
        public const string Rare = "rare";
        public const string Epic = "epic";
        public const string Legendary = "legendary";

        public static readonly string[] All = { Common, Rare, Epic, Legendary };

        public static string DisplayName(string tier) => tier switch
        {
            Common => "Common",
            Rare => "Rare",
            Epic => "Epic",
            Legendary => "Legendary",
            _ => tier,
        };
    }

    [Serializable]
    public class ArtifactNodeDefinition
    {
        public string id;
        public string displayName;

        // Gated to serpentLevel milestones, not player level or cove index (locked in
        // CORE_PROGRESSION_RESTRUCTURE.md, reaffirmed in NEXT_CLAUDE_CODE_PUSH.md §1b) — this
        // is what ties Artifacts to "defeating the endless boss over and over."
        public int requiredSerpentLevel;

        public string shardTier; // RarityTier.* — which tier's appraised Shards this costs
        public int shardCost;

        // Only tap-power exists as a real stat in code today (no crit/crew-synergy trees are
        // implemented) — every node grants this for now, same limitation
        // CoveBuildingCatalog.cs already has. Reuse whichever real stat categories exist once
        // more are built, per the doc's "reuse whatever node categories
        // EXPANDED_UPGRADES_AND_BALANCE.md already establishes" instruction.
        public double tapPowerBonusPercent;

        public ArtifactNodeDefinition(string id, string displayName, int requiredSerpentLevel,
            string shardTier, int shardCost, double tapPowerBonusPercent)
        {
            this.id = id;
            this.displayName = displayName;
            this.requiredSerpentLevel = requiredSerpentLevel;
            this.shardTier = shardTier;
            this.shardCost = shardCost;
            this.tapPowerBonusPercent = tapPowerBonusPercent;
        }
    }

    // Artifacts' permanent upgrade tree (NEXT_CLAUDE_CODE_PUSH.md §1b) — the account-wide,
    // endgame-facing counterpart to Cove Buildings' per-cove wealth sink. Themed as recovering
    // pieces of the crew's own wrecked ship.
    //
    // *** NODE LIST AND EVERY NUMBER BELOW ARE ROUGH PLACEHOLDERS, NOT BALANCED OR FINALIZED. ***
    // Per the doc: "exact rarity-tier names/count and exact tree node list: not finalized, use
    // reasonable placeholders and flag them, same as CoveBuildingCatalog.cs's placeholder
    // numbers are already flagged." These also stack into the same rebalancing gap
    // CoveBuildingCatalog.cs's bonuses already created — see GameManager.TapPower's comment.
    public static class ArtifactNodeCatalog
    {
        public static readonly ArtifactNodeDefinition[] Nodes =
        {
            new ArtifactNodeDefinition("keel_fragment", "Keel Fragment", requiredSerpentLevel: 1,
                shardTier: RarityTier.Common, shardCost: 5, tapPowerBonusPercent: 0.05),
            new ArtifactNodeDefinition("compass_rose", "Compass Rose", requiredSerpentLevel: 10,
                shardTier: RarityTier.Rare, shardCost: 5, tapPowerBonusPercent: 0.10),
            new ArtifactNodeDefinition("captains_locket", "Captain's Locket", requiredSerpentLevel: 25,
                shardTier: RarityTier.Epic, shardCost: 5, tapPowerBonusPercent: 0.20),
            new ArtifactNodeDefinition("figurehead", "The Figurehead", requiredSerpentLevel: 50,
                shardTier: RarityTier.Legendary, shardCost: 5, tapPowerBonusPercent: 0.40),
        };

        public static ArtifactNodeDefinition Find(string id) => Array.Find(Nodes, n => n.id == id);
    }
}
