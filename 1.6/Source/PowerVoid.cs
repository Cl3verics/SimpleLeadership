using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

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
            Messages.Message("SL_PowerVoidStarted".Translate(faction.Name), MessageTypeDefOf.NegativeEvent);
        }

        public override void OnResolve()
        {
            List<Pawn> candidates = WorldComponent_LeaderTracker.Instance.GetBaseLeadersFor(faction)
                .Where(p => p != null && !p.Dead).ToList();
            
            if (candidates.Any())
            {
                faction.leader = candidates.RandomElement();
            }
            Messages.Message("SL_PowerVoidEnded".Translate(faction.Name), MessageTypeDefOf.NeutralEvent);
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