using RimWorld.Planet;
using Verse;

namespace SimpleLeadership
{
    public static class BaseLeaderDefeatChecker
    {
        public static void CheckForBaseLeaderDefeat(Pawn pawn)
        {
            if (pawn == null || pawn.Faction == null)
                return;

            WorldComponent_LeaderTracker leaderTracker = WorldComponent_LeaderTracker.Instance;
            Settlement leaderSettlement = leaderTracker.GetSettlementOfBaseLeader(pawn);

            if (leaderSettlement != null)
            {
                if (pawn != pawn.Faction.leader)
                {
                    leaderTracker.StartPowerEvent(PowerEventDefOf.SL_PowerStruggle, leaderSettlement);
                }
            }
        }
    }
}
