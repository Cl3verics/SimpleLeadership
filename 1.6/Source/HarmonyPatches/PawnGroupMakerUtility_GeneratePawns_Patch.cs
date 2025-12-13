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
            Settlement settlement = Find.WorldObjects.Settlements
            .Where(s => s.Tile == parms.tile && s.Faction == parms.faction)
            .FirstOrDefault();
            if (settlement is null) return;
            
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
                    if (Rand.Chance(spawnChance))
                    {
                        pawnList.Add(factionLeader);
                        __result = pawnList;
                    }
                }
            }

            Pawn baseLeader = WorldComponent_LeaderTracker.Instance.GetBaseLeader(settlement);
            if (baseLeader == null)
                return;
                
            Pawn pawnToReplace = pawnList
                .Where(p => p != factionLeader && p.kindDef.combatPower >= baseLeader.kindDef.combatPower * 0.8f)
                .OrderByDescending(p => p.kindDef.combatPower)
                .FirstOrDefault();

            if (pawnToReplace != null)
            {
                int index = pawnList.IndexOf(pawnToReplace);
                if (index >= 0)
                {
                    pawnList[index] = baseLeader;
                    __result = pawnList;
                }
            }
            else if (pawnList.Count > 0)
            {
                pawnList.Add(baseLeader);
                __result = pawnList;
            }
        }
    }
}
