using RimWorld;
using Verse;

namespace SimpleLeadership
{
    public class Rioting : SettlementPowerEvent
    {
        public override void OnResolve()
        {
            if (settlement?.Faction == null) return;
            var faction = settlement.Faction;
            var leaderTracker = WorldComponent_LeaderTracker.Instance;
            var data = leaderTracker.GetLeadershipDataFor(faction);

            if (data != null)
            {
                if (Rand.Chance(0.2f))
                {
                    var newLeader = leaderTracker.GenerateBaseLeader(faction);
                    leaderTracker.ReplaceBaseLeader(settlement, newLeader);

                    SendMessage("SL_RiotingSuccess".Translate(settlement.Label, newLeader.Named("PAWN")), MessageTypeDefOf.NeutralEvent);
                }
                else
                {
                    SendMessage("SL_RiotingFailed".Translate(settlement.Label), MessageTypeDefOf.NeutralEvent);
                }
            }
        }
    }
}
