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

    [Serializable]
    public class PlayerState
    {
        public double driftwood;
        public int tapLevel = 1;
        public int coveIndex;

        // Set once the FINAL cove's (Low Tide Flats) construction gate is paid — that
        // crossing unlocks the next BIOME rather than advancing coveIndex
        // (GameManager.BuildConstruction). Intermediate-cove crossings just increment
        // coveIndex directly, so no per-cove flag is needed for those.
        public bool constructionComplete;

        // Per-cove mini-boss/construction-reveal flags (CAMERA_AND_UI_SPEC.md
        // "implementation notes", HUD_AND_LANDING_COVE_LAYOUT.md §B2). Indexed by cove
        // (0=Landing Cove, 1=Debris Field, 2=Low Tide Flats) — a biome is capped at 3 coves.
        // Beating coveMinibossDefeated[coveIndex] reveals what's needed to cross to the
        // next cove (GameManager.RegisterMiniBossDefeat); coveConstructionRevealed[coveIndex]
        // gates GameManager.BuildConstruction / ConstructionGate's hut-state visual.
        public bool[] coveMinibossDefeated = new bool[3];
        public bool[] coveConstructionRevealed = new bool[3];

        // Current cove's mini-boss fight state (rubrehose_prototype.html, matching
        // Obelisk's exact model): HP persists across attempts within a cove and only
        // resets on advancing to a fresh cove — never on retreat/timeout. -1 = boss not
        // yet encountered this cove (set to full HP on the first attempt).
        public double bossHpRemaining = -1;

        // Real seconds remaining before the next fight attempt is allowed. Set to
        // GameFormulas.FightCooldownSeconds on retreat/timeout (not on defeat — no
        // cooldown once the boss is dead), ticked down in GameManager.Update().
        public float fightCooldownSeconds;

        // 0 = only Wreck Beach unlocked. Bumped when a later biome's construction
        // gate completes; drives the fast-travel ribbon (CAMERA_AND_UI_SPEC.md).
        public int biomeUnlocked;
        public double totalEarnedAllTime;
        public int totalClears;
        public long lastSaveUnixSeconds;
        public List<CrewState> crew = new List<CrewState>();
    }
}
