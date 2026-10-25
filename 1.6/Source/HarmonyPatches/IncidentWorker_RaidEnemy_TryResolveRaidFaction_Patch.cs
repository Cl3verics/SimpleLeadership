using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace SimpleLeadership
{
    [HarmonyPatch(typeof(IncidentWorker_RaidEnemy), nameof(IncidentWorker_RaidEnemy.TryResolveRaidFaction))]
    public static class IncidentWorker_RaidEnemy_TryResolveRaidFaction_Patch
    {
        public static PlanetTile RaidContextTargetTile;
        internal static Settlement ChosenOriginSettlement;
        public static void Prefix(IncidentParms parms)
        {
            ChosenOriginSettlement = null;
            if (SimpleLeadershipMod.Settings.distanceWeight <= 0f)
                return;
            var targetTile = PlanetTile.Invalid;
            if (parms.target is Map map) targetTile = map.Tile;
            else if (parms.target is WorldObject worldObject) targetTile = worldObject.Tile;
            if (targetTile == PlanetTile.Invalid)
                return;
            RaidContextTargetTile = targetTile;
            if (Rand.Value >= SimpleLeadershipMod.Settings.distanceWeight)
                return;
            ChosenOriginSettlement = Find.WorldObjects.Settlements
                .Where(s => s.Faction != null && s.Faction.IsPlayer is false && s.Spawned && s.Tile.Valid)
                .MinBy(s => Utils.SafeApproxDistanceInTiles(s.Tile, targetTile));
        }

        public static void Postfix(IncidentParms parms)
        {
            RaidContextTargetTile = PlanetTile.Invalid;

            if (parms.faction == null || parms.target == null)
            {
                return;
            }
            var originSettlement = ChosenOriginSettlement ?? FindMostLikelyOriginSettlement(parms.faction, parms.target);
            ChosenOriginSettlement = null;
            RaidContext.CurrentOrigin = originSettlement;

            if (originSettlement != null)
                WorldComponent_LeaderTracker.Instance.lastRaidOrigin[parms.faction] = originSettlement;
            else
                WorldComponent_LeaderTracker.Instance.lastRaidOrigin.Remove(parms.faction);

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

            var factionSettlements = Find.WorldObjects.Settlements
                .Where(s => s.Faction == faction && s.Spawned)
                .ToList();

            if (factionSettlements.Count == 0)
                return null;

            var targetTile = PlanetTile.Invalid;
            if (target is Map map) targetTile = map.Tile;
            else if (target is WorldObject worldObject) targetTile = worldObject.Tile;

            if (targetTile != PlanetTile.Invalid)
            {
                return factionSettlements.RandomElementByWeight(s =>
                {
                    var distance = Utils.SafeApproxDistanceInTiles(s.Tile, targetTile);
                    return 1f / (distance + 1);
                });
            }
            return factionSettlements.FirstOrDefault();
        }
    }
}
