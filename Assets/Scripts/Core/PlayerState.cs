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
        public double totalEarnedAllTime;
        public int totalClears;
        public long lastSaveUnixSeconds;
        public List<CrewState> crew = new List<CrewState>();
    }
}
