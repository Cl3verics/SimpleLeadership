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
                float spawnChance = Utils.CalculateSpawnChance(factionLeader, parms.faction, settlement);
                if (Rand.Chance(spawnChance))
                {
                    pawnList.Add(factionLeader);
                    __result = pawnList;
                }
            }

            Pawn baseLeader = WorldComponent_LeaderTracker.Instance.GetBaseLeader(settlement);
            if (baseLeader != null && !baseLeader.Dead && !baseLeader.Spawned && !pawnList.Contains(baseLeader))
            {
                float spawnChance = Utils.CalculateSpawnChance(baseLeader, parms.faction, settlement);
                if (Rand.Chance(spawnChance))
                {
                    pawnList.Add(baseLeader);
                    __result = pawnList;
                }
            }
        }
    }
}
