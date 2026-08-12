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

        // Legacy repeated-clears gating (coveClears/constructionComplete). Superseded at
        // the cove level by the mini-boss -> construction-reveal model below
        // (CAMERA_AND_UI_SPEC.md "Cove-gate structure"), kept for now since FightController
        // and GameManager still read them — rewiring that logic is a separate follow-up.
        public int coveClears;
        public bool constructionComplete;

        // Per-cove mini-boss/construction-reveal flags (CAMERA_AND_UI_SPEC.md
        // "implementation notes", HUD_AND_LANDING_COVE_LAYOUT.md §B2). Indexed by cove
        // (0=Landing Cove, 1=Debris Field, 2=Low Tide Flats) — a biome is capped at 3 coves.
        // Beating coveMinibossDefeated[coveIndex] reveals what's needed to cross to the
        // next cove; coveConstructionRevealed[coveIndex] tracks whether that's been shown yet.
        public bool[] coveMinibossDefeated = new bool[3];
        public bool[] coveConstructionRevealed = new bool[3];

        // 0 = only Wreck Beach unlocked. Bumped when a later biome's construction
        // gate completes; drives the fast-travel ribbon (CAMERA_AND_UI_SPEC.md).
        public int biomeUnlocked;
        public double totalEarnedAllTime;
        public int totalClears;
        public long lastSaveUnixSeconds;
        public List<CrewState> crew = new List<CrewState>();
    }
}
