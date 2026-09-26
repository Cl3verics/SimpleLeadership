using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace SimpleLeadership
{
    [HarmonyPatch(typeof(JobGiver_AIFightEnemy), nameof(JobGiver_AIFightEnemy.TryGiveJob))]
    public static class JobGiver_AIFightEnemy_TryGiveJob_Patch
    {
        private const float MaxExecuteDistance = 15f;
        private const float ExecuteAdjacencyRadius = 3f;

        public static void Postfix(Pawn pawn, ref Job __result)
        {
            if (pawn.Faction == null || pawn.Faction.HasDoctrine(PowerEventDefOf.SL_NoMercy) is false || pawn.HostileTo(Faction.OfPlayer) is false)
                return;

            var target = GenClosest.ClosestThingReachable(
                pawn.Position,
                pawn.Map,
                ThingRequest.ForGroup(ThingRequestGroup.Pawn),
                PathEndMode.Touch,
                TraverseParms.For(pawn),
                MaxExecuteDistance,
                t => t is Pawn p && p.Faction == Faction.OfPlayer && p.RaceProps.Humanlike && p.Downed && pawn.CanReserveAndReach(p, PathEndMode.Touch, Danger.Deadly));

            if (target is Pawn downedPawn && (__result == null || pawn.Position.DistanceTo(downedPawn.Position) <= ExecuteAdjacencyRadius))
            {
                __result = JobMaker.MakeJob(JobDefOf.AttackMelee, downedPawn);
                __result.killIncappedTarget = true;
            }
        }
    }
}
