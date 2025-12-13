using System.Collections.Generic;
using Verse;
using RimWorld.Planet;

namespace SimpleLeadership
{
    public class FactionLeadershipData : IExposable
    {
        public Dictionary<Settlement, Pawn> settlementLeaders = [];

        public void ExposeData()
        {
            Scribe_Collections.Look(ref settlementLeaders, "settlementLeaders", LookMode.Reference, LookMode.Reference);
        }
    }
}
