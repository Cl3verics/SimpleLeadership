using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace SimpleLeadership
{
    [HarmonyPatch(typeof(PawnGroupKindWorker_Normal), "GeneratePawns")]
    public static class PawnGroupKindWorker_Normal_GeneratePawns_Patch
    {
        public static void Prefix(PawnGroupMakerParms parms, PawnGroupMaker groupMaker)
        {
            if (parms == null || parms.faction == null || parms.groupKind != PawnGroupKindDefOf.Settlement)
                return;
            Settlement settlement = Find.WorldObjects.Settlements
                .Where(s => s.Tile == parms.tile && s.Faction == parms.faction)
                .FirstOrDefault();

            if (settlement != null && settlement.IsInPowerEvent<PowerStruggle>())
            {
                parms.points *= 0.6f;
            }
        }
    }
}
