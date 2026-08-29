using System;
using System.Collections.Generic;

namespace Rubrehose.Core
{
    [Serializable]
    public class CrewState
    {
        public string id;
        public int level;
        public double currentCost;
    }

    // Progress on one Cove Building (CORE_PROGRESSION_RESTRUCTURE.md "Cove Buildings" —
    // a separate, optional wealth sink with no bearing on cove-unlock progression). stage 0
    // means not built at all (zero presence in the world); 1-3 are the 3 paid stages.
    [Serializable]
    public class BuildingState
    {
        public string id;
        public int stage;
    }

    [Serializable]
    public class PlayerState
    {
        public double driftwood;
        public int tapLevel = 1;

        // 0-3 across all 4 of Wreck Beach's coves now (CORE_PROGRESSION_RESTRUCTURE.md —
        // Landing Cove/Tide Pools/The Grove/The Deep Reef). 0-2 are the finite,
        // one-time-mini-boss coves; 3 is the permanent/endless cove (GameManager.IsEndlessCove).
        // Advances immediately on mini-boss defeat (GameManager.RegisterMiniBossDefeat) — the
        // old separate "construction gate" payment step before advancing was removed in the
        // 2026-08-27 revision (coves are one continuous scrollable island now, not disconnected
        // landmasses needing a bridge built between them).
        public int coveIndex;

        // Set once coveIndex reaches the endless cove (index 3). Repurposed from the old
        // per-biome-unlock meaning now that biomes are retired as a near-term concept; still
        // what MenuDrawerController reads to reveal the Captain's Log row. Renamed from
        // constructionComplete now that the construction-gate concept it referenced is gone.
        public bool reachedEndlessCove;

        // Per-cove mini-boss-defeated flags (CAMERA_AND_UI_SPEC.md "implementation notes").
        // Indexed by cove (0=Landing Cove, 1=Tide Pools, 2=The Grove) — only the 3 finite
        // coves use this one-time-defeat model; the endless 4th cove (index 3) uses
        // serpentLevel below instead and is never "defeated" in this permanent sense. Beating
        // coveMinibossDefeated[coveIndex] now immediately advances coveIndex
        // (GameManager.RegisterMiniBossDefeat) — no separate reveal/payment step anymore.
        public bool[] coveMinibossDefeated = new bool[3];

        // Current cove's mini-boss fight state (rubrehose_prototype.html, matching
        // Obelisk's exact model): HP persists across attempts within a cove and only
        // resets on advancing to a fresh cove — never on retreat/timeout. -1 = boss not
        // yet encountered this cove (set to full HP on the first attempt). Also used for
        // the endless cove: a kill there resets this back to -1 so the next attempt arms
        // the new (tougher) level's full HP, rather than meaning "permanently defeated".
        public double bossHpRemaining = -1;

        // Real seconds remaining before the next fight attempt is allowed. Set to
        // GameFormulas.FightCooldownSeconds on retreat/timeout (not on defeat — no
        // cooldown once the boss is dead, or immediately after an endless-cove kill),
        // ticked down in GameManager.Update().
        public float fightCooldownSeconds;

        // Cove 4's persistent "Obelisk" counter (CORE_PROGRESSION_RESTRUCTURE.md "Cove 4's
        // serpent") — only ever increments, never resets, and drives
        // GameFormulas.SerpentHpForLevel/SerpentArmorForLevel once GameManager.IsEndlessCove
        // is true. Default 0 means "not yet reached the endless cove"; GameManager treats
        // 0 the same as 1 (see GameManager's EffectiveSerpentLevel) and sets this to 1
        // explicitly the first time coveIndex reaches the endless cove.
        public int serpentLevel;

        // One-shot onboarding popups (CORE_PROGRESSION_RESTRUCTURE.md "Onboarding /
        // tutorial system") — each id is marked seen the instant its popup is dismissed and
        // never re-triggers after that (GameManager.HasSeenOnboarding/MarkOnboardingSeen).
        // A List<string> rather than a fixed bool array/enum: popup ids are defined where
        // they're triggered (OnboardingController), so this doesn't need to grow in lockstep
        // with a central enum every time a new contextual popup is added.
        public List<string> seenOnboardingIds = new List<string>();

        public double totalEarnedAllTime;
        public int totalClears;
        public long lastSaveUnixSeconds;
        public List<CrewState> crew = new List<CrewState>();

        // Cove Buildings progress (CORE_PROGRESSION_RESTRUCTURE.md "Cove Buildings") — a
        // List<BuildingState> rather than a fixed array, same reasoning as
        // seenOnboardingIds above: building ids are defined in CoveBuildingCatalog.cs, so
        // this doesn't need to grow in lockstep with a central enum as more coves' buildings
        // get designed. Entries are created lazily on first payment (GameManager.GetOrCreateBuildingState).
        public List<BuildingState> buildings = new List<BuildingState>();
    }
}
