using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;

namespace SimpleLeadership
{
    public class PowerVoid : PowerEventBase
    {
        public Faction faction;

        public override void SetParameters(params object[] args)
        {
            if (args.Length > 0 && args[0] is Faction f)
            {
                faction = f;
            }
        }

        public override bool IsDuplicate(PowerEventBase other)
        {
            return other is PowerVoid otherVoid && otherVoid.faction == faction;
        }

        public override void OnStart()
        {
            if (faction != null)
            {
                faction.leader = null;
            }
            base.OnStart();
        }

        protected override string GetFormattedMessage(string message)
        {
            return string.Format(message, faction.Name);
        }

        public override void OnResolve()
        {
            var leaderTracker = WorldComponent_LeaderTracker.Instance;
            var data = leaderTracker.GetLeadershipDataFor(faction);
            Pawn newLeader = null;

            if (data != null && data.actingLeader != null && !data.actingLeader.Dead)
            {
                newLeader = data.actingLeader;
                faction.leader = newLeader;
                data.actingLeader = null;
            }
            else
            {
                List<Pawn> candidates = leaderTracker.GetBaseLeadersFor(faction)
                    .Where(p => p != null && !p.Dead).ToList();
        
                if (candidates.Any())
                {
                    newLeader = candidates.RandomElement();
                    faction.leader = newLeader;
                }
            }

            if (newLeader != null)
            {
                string label = "SL_PowerVoidEndedLetterLabel".Translate(faction.Named("FACTION"));
                string body = "SL_NewLeaderElectedLetterBody".Translate(newLeader.Named("PAWN"));
                Find.LetterStack.ReceiveLetter(label, body, LetterDefOf.NeutralEvent, newLeader, faction);

                Settlement oldSettlement = leaderTracker.GetSettlementOfBaseLeader(newLeader);
                if (oldSettlement != null)
                {
                    if (data != null)
                    {
                        data.settlementLeaders.Remove(oldSettlement);
                    }
                    leaderTracker.StartPowerEvent(PowerEventDefOf.SL_PowerStruggle, oldSettlement);
                }
            }
    
            base.OnResolve();
        }

        public override bool IsTarget(object target)
        {
            return target is Faction f && f == faction;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref faction, "faction");
        }
    }
}
