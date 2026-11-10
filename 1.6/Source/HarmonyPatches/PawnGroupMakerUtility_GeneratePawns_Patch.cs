using System;
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
        public static void Prefix(PawnGroupMakerParms parms, ref List<Pawn> __state)
        {
            if (parms?.faction is null || parms.faction.def.humanlikeFaction is false
                || parms.groupKind != PawnGroupKindDefOf.Combat && parms.groupKind != PawnGroupKindDefOf.Settlement)
                return;
            try
            {
                Settlement settlement = RaidContext.CurrentOrigin;
                if (settlement == null || settlement.Faction != parms.faction)
                {
                    var factionSettlements = Find.WorldObjects.Settlements.Where(s => s.Faction == parms.faction && s.Tile.Valid);
                    if (factionSettlements.Any())
                    {
                        settlement = factionSettlements.MinBy(s => Utils.SafeApproxDistanceInTiles(parms.tile, s.Tile));
                    }
                }
                if (settlement == null) return;

                bool isRaidingPlayer = false;
                MapParent mapParent = Find.WorldObjects.MapParentAt(parms.tile);
                if (mapParent != null && mapParent.HasMap && mapParent.Map.ParentFaction == Faction.OfPlayer)
                {
                    isRaidingPlayer = true;
                }

                if (isRaidingPlayer)
                {
                    var tracker = WorldComponent_LeaderTracker.Instance;
                    if (tracker.lastLeaderRaidTick.TryGetValue(parms.faction, out int lastTick) && Find.TickManager.TicksGame < lastTick + 30000)
                    {
                        return;
                    }
                    tracker.lastLeaderRaidTick[parms.faction] = Find.TickManager.TicksGame;
                }

                __state = new List<Pawn>();
                var budget = parms.points;
                var minGroupPoints = parms.faction.def.MinPointsToGeneratePawnGroup(parms.groupKind);

                Pawn factionLeader = parms.faction.leader;
                if (factionLeader?.Dead is false && factionLeader.Spawned is false && budget - factionLeader.kindDef.combatPower >= minGroupPoints)
                {
                    float spawnChance = Utils.CalculateSpawnChance(factionLeader, parms.faction, settlement, isRaidingPlayer);
                    if (Rand.Chance(spawnChance))
                    {
                        __state.Add(factionLeader);
                        budget -= factionLeader.kindDef.combatPower;
                    }
                }

                Pawn baseLeader = WorldComponent_LeaderTracker.Instance.GetBaseLeader(settlement);
                if (baseLeader?.Dead is false && baseLeader.Spawned is false && !__state.Contains(baseLeader) && budget - baseLeader.kindDef.combatPower >= minGroupPoints)
                {
                    float spawnChance = Utils.CalculateSpawnChance(baseLeader, parms.faction, settlement, isRaidingPlayer);
                    if (Rand.Chance(spawnChance))
                    {
                        __state.Add(baseLeader);
                        budget -= baseLeader.kindDef.combatPower;
                    }
                }

                parms.points = budget;
            }
            catch (Exception ex)
            {
                Log.Error("[SimpleLeadership] Error in PawnGroupMakerUtility_GeneratePawns_Patch: " + ex.Message);
            }
        }

        public static void Postfix(ref IEnumerable<Pawn> __result, List<Pawn> __state)
        {
            if (__state is null || __state.Count == 0)
                return;
            var pawnList = __result.ToList();
            foreach (var leader in __state)
            {
                if (!leader.Dead && !leader.Spawned && !pawnList.Contains(leader))
                    pawnList.Add(leader);
            }
            __result = pawnList;
        }
    }
}
