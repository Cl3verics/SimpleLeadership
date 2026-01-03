using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace SimpleLeadership
{
    [HarmonyPatch(typeof(PawnGroupMakerUtility), "GeneratePawns")]
    public static class PawnGroupMakerUtility_GeneratePawns_Patch
    {
        public static void Postfix(ref IEnumerable<Pawn> __result, PawnGroupMakerParms parms)
        {
            if (parms == null || parms.faction == null)
                return;
            Settlement settlement = RaidContext.CurrentOrigin;
            if (settlement == null || settlement.Faction != parms.faction)
            {
                settlement = Find.WorldObjects.Settlements
                    .Where(s => s.Faction == parms.faction)
                    .MinBy(s => Find.WorldGrid.ApproxDistanceInTiles(parms.tile, s.Tile));
            }
            if (settlement == null) return;
            
            List<Pawn> pawnList = __result.ToList();

            Pawn factionLeader = parms.faction.leader;
            if (factionLeader != null && !factionLeader.Dead && !factionLeader.Spawned && !__result.Contains(factionLeader))
            {
                int settlementCount = Find.WorldObjects.Settlements
                    .Where(s => s.Faction == parms.faction)
                    .Count();

                if (settlementCount > 0)
                {
                    float spawnChance = 1f / settlementCount;
                    if (settlement.IsInPowerEvent(PowerEventDefOf.SL_Inspection))
                    {
                        spawnChance += 0.3f;
                    }
                    if (Rand.Chance(spawnChance))
                    {
                        pawnList.Add(factionLeader);
                        __result = pawnList;
                    }
                }
            }

            Pawn baseLeader = WorldComponent_LeaderTracker.Instance.GetBaseLeader(settlement);
            if (Rand.Chance(0.1f) && baseLeader != null && !baseLeader.Dead && !baseLeader.Spawned && !pawnList.Contains(baseLeader))
            {
                var tracker = WorldComponent_LeaderTracker.Instance;
                var factionSettlements = Find.WorldObjects.Settlements.Where(s => s.Faction == parms.faction).ToList();
                int controlledBases = factionSettlements.Count(s => tracker.GetBaseLeader(s) == baseLeader);

                if (controlledBases > 0)
                {
                    float spawnChance = 1f / controlledBases;
                    if (Rand.Chance(spawnChance))
                    {
                        pawnList.Add(baseLeader);
                        __result = pawnList;
                    }
                }
            }
        }
    }
}
