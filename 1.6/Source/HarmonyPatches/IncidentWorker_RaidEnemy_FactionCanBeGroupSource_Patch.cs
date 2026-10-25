using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace SimpleLeadership
{
    [HarmonyPatch(typeof(IncidentWorker_RaidEnemy), nameof(IncidentWorker_RaidEnemy.FactionCanBeGroupSource))]
    public static class IncidentWorker_RaidEnemy_FactionCanBeGroupSource_Patch
    {
        public static void Postfix(ref bool __result, Faction f, IncidentParms parms, bool desperate = false)
        {
            if (__result is false || f == null || desperate) return;

            if (f.IsInPowerEvent<PowerVoid>())
            {
                __result = false;
            }
            else
            {
                var playerBases = Find.WorldObjects.Settlements
                    .Where(s => s.Faction == Faction.OfPlayer).ToList();
                if (playerBases.Count == 0) return;

                var nearestFactionBase = Find.WorldObjects.Settlements
                    .Where(s => s.Faction == f && s.Spawned && s.Tile.Valid)
                    .OrderBy(s => playerBases.Min(p => Utils.SafeApproxDistanceInTiles(p.Tile, s.Tile)))
                    .FirstOrDefault();

                if (nearestFactionBase == null) return;

                var nearestDistance = playerBases.Min(p => Utils.SafeApproxDistanceInTiles(p.Tile, nearestFactionBase.Tile));
                if (nearestDistance > 30f && parms.forced is false)
                {
                    var suppressChance = Mathf.Clamp01((nearestDistance - 30f) / 90f) * 0.8f;
                    if (Rand.Chance(suppressChance))
                        __result = false;
                }
            }
        }
    }
}
