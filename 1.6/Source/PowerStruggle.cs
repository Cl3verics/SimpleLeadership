using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;

namespace SimpleLeadership
{
    public class PowerStruggle : PowerEventBase
    {
        public Settlement settlement;

        public override void SetParameters(params object[] args)
        {
            if (args.Length > 0 && args[0] is Settlement s)
            {
                settlement = s;
            }
        }

        public override bool IsDuplicate(PowerEventBase other)
        {
            return other is PowerStruggle otherStruggle && otherStruggle.settlement == settlement;
        }

        public override void OnStart()
        {
            Messages.Message("SL_PowerStruggleStarted".Translate(settlement.Label), MessageTypeDefOf.NegativeEvent);
        }

        public override void OnResolve()
        {
            Faction faction = settlement.Faction;
            var leaderTracker = WorldComponent_LeaderTracker.Instance;
            var data = leaderTracker.GetLeadershipDataFor(faction);

            if (data != null)
            {
                Pawn newLeader = leaderTracker.GenerateBaseLeader(faction);
                data.settlementLeaders[settlement] = newLeader;
            }
            Messages.Message("SL_PowerStruggleEnded".Translate(settlement.Label), MessageTypeDefOf.NeutralEvent);
        }

        public override bool IsTarget(object target)
        {
            return target is Settlement s && s == settlement;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref settlement, "settlement");
        }
    }
}