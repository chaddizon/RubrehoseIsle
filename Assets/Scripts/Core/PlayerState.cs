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
        public int coveClears;
        public bool constructionComplete;

        // 0 = only Wreck Beach unlocked. Bumped when a later biome's construction
        // gate completes; drives the fast-travel ribbon (CAMERA_AND_UI_SPEC.md).
        public int biomeUnlocked;
        public double totalEarnedAllTime;
        public int totalClears;
        public long lastSaveUnixSeconds;
        public List<CrewState> crew = new List<CrewState>();
    }
}
