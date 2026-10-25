using RimWorld;
using Verse;

namespace SimpleLeadership
{
    public class PowerStruggle : SettlementPowerEvent
    {
        public override void OnResolve()
        {
            if (settlement?.Faction != null)
            {
                var faction = settlement.Faction;
                var leaderTracker = WorldComponent_LeaderTracker.Instance;
                var data = leaderTracker.GetLeadershipDataFor(faction);
                if (data != null)
                {
                    var newLeader = leaderTracker.GenerateBaseLeader(faction);
                    leaderTracker.ReplaceBaseLeader(settlement, newLeader);
                }
            }
            base.OnResolve();
        }
    }
}
