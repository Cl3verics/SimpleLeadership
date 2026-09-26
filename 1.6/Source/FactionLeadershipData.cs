using System.Collections.Generic;
using RimWorld.Planet;
using Verse;

namespace SimpleLeadership
{
    public class FactionLeadershipData : IExposable
    {
        public Dictionary<Settlement, Pawn> settlementLeaders = [];
        public List<FactionDoctrine> doctrines = [];
        public bool doctrinesGenerated;
        public Pawn actingLeader;
        public Pawn exLeader;

        public void ExposeData()
        {
            Scribe_Collections.Look(ref doctrines, "doctrines", LookMode.Deep);
            Scribe_Values.Look(ref doctrinesGenerated, "doctrinesGenerated");
            Scribe_Collections.Look(ref settlementLeaders, "settlementLeaders", LookMode.Reference, LookMode.Reference);
            Scribe_References.Look(ref actingLeader, "actingLeader");
            Scribe_References.Look(ref exLeader, "exLeader");
        }
    }
}
