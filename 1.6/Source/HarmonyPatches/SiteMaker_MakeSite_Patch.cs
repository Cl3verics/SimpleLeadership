using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace SimpleLeadership
{
    [HarmonyPatch(typeof(SiteMaker), "MakeSite", new Type[] { typeof(IEnumerable<SitePartDefWithParams>), typeof(PlanetTile), typeof(Faction), typeof(bool), typeof(WorldObjectDef) })]
    public class SiteMaker_MakeSite_Patch
    {
        public static void Prefix(ref IEnumerable<SitePartDefWithParams> siteParts, PlanetTile tile, Faction faction)
        {
            if (faction == null || faction.IsPlayer)
                return;

            Settlement nearestSettlement = Find.WorldObjects.Settlements
                .Where(s => s.Faction == faction && s.Tile != tile)
                .OrderBy(s => Find.WorldGrid.ApproxDistanceInTiles(s.Tile, tile))
                .FirstOrDefault();

            if (nearestSettlement != null)
            {
                if (nearestSettlement.IsInPowerEvent<PowerStruggle>())
                {
                    List<SitePartDefWithParams> modifiedSiteParts = [];
                    foreach (var part in siteParts)
                    {
                        if (part.parms != null && part.parms.threatPoints > 0)
                        {
                            part.parms.threatPoints *= 0.6f;
                        }
                        modifiedSiteParts.Add(part);
                    }
                    siteParts = modifiedSiteParts;
                }
            }
        }

        public static void Postfix(Site __result, IEnumerable<SitePartDefWithParams> siteParts, PlanetTile tile, Faction faction)
        {
            if (__result == null || faction == null || faction.IsPlayer)
                return;

            Settlement nearestSettlement = Find.WorldObjects.Settlements
                .Where(s => s.Faction == faction && s.Tile != tile)
                .OrderBy(s => Find.WorldGrid.ApproxDistanceInTiles(s.Tile, tile))
                .FirstOrDefault();

            if (nearestSettlement != null)
            {
                var siteOwnershipComp = __result.GetComponent<WorldObjectComp_SiteOwnership>();
                if (siteOwnershipComp != null)
                {
                    siteOwnershipComp.owningSettlement = nearestSettlement;
                }
            }
        }
    }
}
