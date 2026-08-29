using System;

namespace Rubrehose.Data
{
    [Serializable]
    public class CoveBuildingStage
    {
        public double cost;

        // Percentage bonus (0.05 = +5%) folded into GameManager.TapPower once this stage is
        // paid — thematically tap-power for Landing Cove's Hut specifically, per
        // CORE_PROGRESSION_RESTRUCTURE.md's worked example ("a proper home base sharpens
        // focus"). Other coves' buildings should tie their bonus to something cove-local
        // instead once designed (doc: "make it make sense per cove", e.g. Tide Pools' building
        // boosting Tidepooling yield) — not modeled generically yet since only the Hut exists;
        // GameManager.BuildingTapPowerBonusSum will need a rework once a second, differently-
        // themed building bonus type exists.
        public double tapPowerBonusPercent;

        // Flavor-only reward text shown in the onboarding "stage complete" popup — Postcards
        // and Companions (EXPANDED_UPGRADES_AND_BALANCE.md) aren't implemented as real systems
        // anywhere in code yet, so this is NOT a real grant, just a promised description. Wire
        // this to an actual Postcard/Companion catalog once those systems exist. Null for
        // stages with no cosmetic/lore reward.
        public string cosmeticRewardLabel;

        public CoveBuildingStage(double cost, double tapPowerBonusPercent, string cosmeticRewardLabel)
        {
            this.cost = cost;
            this.tapPowerBonusPercent = tapPowerBonusPercent;
            this.cosmeticRewardLabel = cosmeticRewardLabel;
        }
    }

    [Serializable]
    public class CoveBuildingDefinition
    {
        public string id;
        public string displayName;

        // Which cove this building belongs to — must be reached (GameManager.State.coveIndex
        // >= coveIndex) before it's payable at all, matching "the world itself stays silent
        // until something is actually earned" (no point offering a cove's building before the
        // player has even arrived there).
        public int coveIndex;

        // Always 3 entries, per CORE_PROGRESSION_RESTRUCTURE.md's "Cost/reward shape".
        public CoveBuildingStage[] stages;

        public CoveBuildingDefinition(string id, string displayName, int coveIndex, CoveBuildingStage[] stages)
        {
            this.id = id;
            this.displayName = displayName;
            this.coveIndex = coveIndex;
            this.stages = stages;
        }
    }

    // Cove Buildings (CORE_PROGRESSION_RESTRUCTURE.md "Cove Buildings — separate from
    // cove-unlock progression entirely"): a parallel, optional wealth sink with NO bearing on
    // advancing between coves — one building per cove, 3 paid stages each, granting real
    // numeric bonuses (not purely cosmetic, reversing an earlier draft of that doc).
    //
    // Only Landing Cove's Hut is defined below — the doc explicitly leaves the other 3
    // coves' buildings "TBD, not needed until those coves are actually built" (exact concept
    // per cove still undesigned). Add new entries here once each cove's building is designed;
    // GameManager/CoveBuildingVisual/BuildingsMenuPanel are already fully generic over
    // buildingId and need no code changes to pick up a new entry.
    //
    // *** COST/REWARD NUMBERS BELOW ARE ROUGH PLACEHOLDERS, NOT BALANCED. ***
    // The doc's own "Rebalancing requirement" section flags that base tap-power/serpent-curve
    // formulas need to come down once these building bonuses stack on top of them, verified via
    // simulation (rubrehose_prototype.html's Balance tab or an equivalent Unity-side sim) — NOT
    // done here or anywhere else in this codebase yet. Treat every number below as provisional
    // shape only (Stage 1 modest/achievable, Stage 2 a serious investment, Stage 3 a long-term
    // achievement plausibly not reached until well after all of Wreck Beach is unlocked), same
    // spirit as GameFormulas.SerpentHp/SerpentArmor's own "first-pass estimate" caveat. Do not
    // treat these as tuned; the required rebalancing pass is a separate follow-up task.
    public static class CoveBuildingCatalog
    {
        public static readonly CoveBuildingDefinition[] Buildings =
        {
            new CoveBuildingDefinition("hut", "The Hut", coveIndex: 0, stages: new[]
            {
                // Stage 1: modest cost, real but achievable early — reward: small tap-power bump.
                new CoveBuildingStage(cost: 300, tapPowerBonusPercent: 0.05, cosmeticRewardLabel: null),
                // Stage 2: serious investment — reward: larger tap-power bump + a Postcard (flavor only, see above).
                new CoveBuildingStage(cost: 5000, tapPowerBonusPercent: 0.15, cosmeticRewardLabel: "a Postcard"),
                // Stage 3: major long-term cost — reward: further tap-power bump + a Companion + a cosmetic crew accent (flavor only).
                new CoveBuildingStage(cost: 100000, tapPowerBonusPercent: 0.30, cosmeticRewardLabel: "a Companion + a cosmetic crew accent"),
            }),
        };

        public static CoveBuildingDefinition Find(string id) => Array.Find(Buildings, b => b.id == id);
    }
}
