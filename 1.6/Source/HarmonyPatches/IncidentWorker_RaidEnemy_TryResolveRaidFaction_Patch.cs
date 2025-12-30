using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace SimpleLeadership
{
    [HarmonyPatch(typeof(IncidentWorker_RaidEnemy), "TryResolveRaidFaction")]
    public static class IncidentWorker_RaidEnemy_TryResolveRaidFaction_Patch
    {
        public static void Postfix(IncidentParms parms)
        {
            if (parms.faction == null || parms.target == null)
                return;
            Settlement originSettlement = FindMostLikelyOriginSettlement(parms.faction, parms.target);
            RaidContext.CurrentOrigin = originSettlement;

            if (originSettlement != null)
            {
                if (originSettlement.IsInPowerEvent<PowerStruggle>())
                {
                    parms.points *= 0.6f;
                }
                else if (originSettlement.IsInPowerEvent(PowerEventDefOf.SL_Fortifying))
                {
                    parms.points *= 2f;
                }
            }
        }

        private static Settlement FindMostLikelyOriginSettlement(Faction faction, IIncidentTarget target)
        {
            if (faction == null)
                return null;

            List<Settlement> factionSettlements = Find.WorldObjects.Settlements
                .Where(s => s.Faction == faction && s.Spawned)
                .ToList();

            if (factionSettlements.Count == 0)
                return null;
            if (target is Map map)
            {
                return factionSettlements
                    .OrderBy(s => Find.WorldGrid.ApproxDistanceInTiles(s.Tile, map.Tile))
                    .FirstOrDefault();
            }
            if (target is WorldObject worldObject)
            {
                return factionSettlements
                    .OrderBy(s => Find.WorldGrid.ApproxDistanceInTiles(s.Tile, worldObject.Tile))
                    .FirstOrDefault();
            }
            return factionSettlements.FirstOrDefault();
        }
    }
}
