using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

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

        public override bool IsDuplicate(PowerEventBase other) => other is PowerVoid otherVoid && otherVoid.faction == faction;

        public override void OnStart()
        {
            if (faction != null)
            {
                faction.leader = null;
            }
            base.OnStart();
        }

        protected override string GetFormattedMessage(string message) => string.Format(message, faction.Name);

        public override void OnResolve()
        {
            var leaderTracker = WorldComponent_LeaderTracker.Instance;
            var data = leaderTracker.GetLeadershipDataFor(faction);
            Pawn newLeader = null;

            if (data?.actingLeader != null && data.actingLeader.Dead is false)
            {
                newLeader = data.actingLeader;
                faction.leader = newLeader;
                data.actingLeader = null;
            }
            else
            {
                var candidates = leaderTracker.GetBaseLeadersFor(faction)
                    .Where(p => p.IsValidLeaderCandidate()).ToList();

                if (candidates.Count > 0)
                {
                    if (data?.exLeader != null && faction.HasDoctrine(PowerEventDefOf.SL_FamilialSuccession))
                    {
                        var relatives = candidates.Where(c => c.relations.FamilyByBlood.Contains(data.exLeader)).ToList();
                        if (relatives.Count > 0)
                            candidates = relatives;
                    }
                    newLeader = candidates.RandomElement();
                    faction.leader = newLeader;
                }
            }

            if (newLeader != null)
            {
                var label = "SL_PowerVoidEndedLetterLabel".Translate(faction.Named("FACTION"));
                var body = "SL_NewLeaderElectedLetterBody".Translate(newLeader.Named("PAWN"));
                Find.LetterStack.ReceiveLetter(label, body, LetterDefOf.NeutralEvent, newLeader, faction);
                if (faction.HasDoctrine(PowerEventDefOf.SL_FamilialSuccession))
                {
                    foreach (var baseLeader in leaderTracker.GetBaseLeadersFor(faction))
                    {
                        leaderTracker.EnsureFamilialRelation(newLeader, baseLeader);
                    }
                }
                foreach (var settlement in leaderTracker.GetSettlementsOfBaseLeader(newLeader).ToList())
                {
                    data.settlementLeaders.Remove(settlement);
                    leaderTracker.StartPowerEvent(PowerEventDefOf.SL_PowerStruggle, settlement);
                }
            }

            base.OnResolve();
        }

        public override bool IsTarget(object target) => target is Faction f && f == faction;

        public override object GetTarget() => faction;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref faction, "faction");
        }
    }
}
