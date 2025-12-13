using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

namespace SimpleLeadership
{
    public class WorldObjectCompProperties_SiteOwnership : WorldObjectCompProperties
    {
        public WorldObjectCompProperties_SiteOwnership()
        {
            compClass = typeof(WorldObjectComp_SiteOwnership);
        }
    }
    public class WorldObjectComp_SiteOwnership : WorldObjectComp
    {
        public Settlement owningSettlement;
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look(ref owningSettlement, "owningSettlement");
        }

        public override string CompInspectStringExtra()
        {
            if (owningSettlement != null)
            {
                return "SL_SiteOwnershipInfo".Translate(owningSettlement.Label, owningSettlement.Faction.Name);
            }
            return null;
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            if (owningSettlement != null)
            {
                yield return new Command_Action
                {
                    defaultLabel = "SL_JumpToBase".Translate(),
                    defaultDesc = "SL_JumpToBaseDesc".Translate(),
                    icon = WorldGizmoUtility.JumpToCommand,
                    action = delegate
                    {
                        CameraJumper.TryJump(owningSettlement);
                    }
                };
            }
        }
    }
}
