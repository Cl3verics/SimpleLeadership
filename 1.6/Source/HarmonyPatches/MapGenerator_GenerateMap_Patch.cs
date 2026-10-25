using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace SimpleLeadership
{
    [HarmonyPatch(typeof(MapGenerator), nameof(MapGenerator.GenerateMap))]
    public static class MapGenerator_GenerateMap_Patch
    {
        public static void Postfix(Map __result)
        {
            if (__result == null || __result.Parent is not Settlement settlement)
                return;

            if (settlement.IsInPowerEvent(PowerEventDefOf.SL_Support) && settlement.Faction.HostileTo(Faction.OfPlayer))
            {
                DoSupportArriving(__result, settlement);
            }

            if (settlement.IsInPowerEvent(PowerEventDefOf.SL_PrisonerTransfer))
            {
                SpawnPrisoners(__result, settlement.Faction);
            }

            var tracker = WorldComponent_LeaderTracker.Instance;
            if (tracker.kidnappedPrisoners.TryGetValue(settlement, out var prisonerList) && prisonerList.prisoners.Count > 0)
            {
                SpawnKidnappedPrisoners(__result, prisonerList.prisoners.ToList());
                tracker.kidnappedPrisoners.Remove(settlement);
            }

            if (__result.Parent is Site site)
            {
                var ownerComp = site.GetComponent<WorldObjectComp_SiteOwnership>();
                if (ownerComp?.owningSettlement != null
                    && ownerComp.owningSettlement.IsInPowerEvent(PowerEventDefOf.SL_Support)
                    && site.Faction != null
                    && site.Faction.HostileTo(Faction.OfPlayer))
                {
                    DoSupportArriving(__result, ownerComp.owningSettlement);
                }
            }
        }

        private const string SupportRaidTag = "Support";

        public static void DoSupportArriving(Map map, Settlement settlement)
        {
            var comp = map.GetComponent<MapComponent_DelayedRaid>();
            if (comp.HasScheduledTag(SupportRaidTag)) return;

            var totalCombatPower = 0f;
            foreach (var pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (pawn.HostileTo(Faction.OfPlayer))
                    totalCombatPower += pawn.kindDef.combatPower;
            }

            var parms = new IncidentParms();
            parms.target = map;
            parms.faction = settlement.Faction;
            parms.points = totalCombatPower * 0.5f;
            parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;

            comp.Schedule(parms, 200, SupportRaidTag);

            Messages.Message("SL_SupportArriving".Translate(settlement.Label), MessageTypeDefOf.ThreatBig);
        }

        private static List<Building_Bed> FindPrisonerBeds(Map map, Faction faction)
        {
            var factionBeds = map.listerBuildings.allBuildingsNonColonist
                .OfType<Building_Bed>()
                .Where(b => b.Faction == faction)
                .ToList();

            if (factionBeds.Count == 0)
                return new List<Building_Bed>();

            var bedsByRoom = factionBeds
                .GroupBy(b => b.Position.GetRoom(map))
                .Where(g => g.Key != null)
                .OrderByDescending(g => g.Count())
                .ToList();

            List<Building_Bed> selectedBeds = null;
            foreach (var roomGroup in bedsByRoom)
            {
                var bedCount = roomGroup.Count();
                if (bedCount >= 3 && bedCount <= 5)
                {
                    selectedBeds = roomGroup.ToList();
                    break;
                }
            }

            if (selectedBeds == null)
            {
                var allBeds = factionBeds.ToList();
                var takeCount = Math.Min(allBeds.Count, Rand.RangeInclusive(3, 5));
                selectedBeds = allBeds.Take(takeCount).ToList();
            }

            return selectedBeds;
        }

        private static void SpawnKidnappedPrisoners(Map map, List<Pawn> prisoners)
        {
            var faction = map.Parent.Faction;
            var selectedBeds = FindPrisonerBeds(map, faction);

            for (var i = 0; i < prisoners.Count; i++)
            {
                var prisoner = prisoners[i];
                IntVec3 spawnPos;
                if (i < selectedBeds.Count)
                {
                    var bed = selectedBeds[i];
                    bed.ForPrisoners = true;
                    spawnPos = bed.Position;
                }
                else
                {
                    spawnPos = selectedBeds[0].Position;
                }
                if (prisoner.Spawned is false)
                    GenSpawn.Spawn(prisoner, spawnPos, map);
                if (prisoner.guest != null)
                    prisoner.guest.SetGuestStatus(faction, GuestStatus.Slave);
                prisoner.mindState.WillJoinColonyIfRescued = true;
            }

            Messages.Message("SL_KidnappedColonistsDetected".Translate(), MessageTypeDefOf.NeutralEvent);
        }

        private static void SpawnPrisoners(Map map, Faction faction)
        {
            foreach (var bed in FindPrisonerBeds(map, faction))
            {
                bed.ForPrisoners = true;
                var request = new PawnGenerationRequest(PawnKindDefOf.Slave, faction, PawnGenerationContext.NonPlayer, -1, true);
                var prisoner = PawnGenerator.GeneratePawn(request);
                prisoner.SetFaction(Faction.OfAncients);
                GenSpawn.Spawn(prisoner, bed.Position, map);
                if (prisoner.guest != null)
                    prisoner.guest.SetGuestStatus(faction, GuestStatus.Prisoner);
            }

            Messages.Message("SL_PrisonersDetected".Translate(), MessageTypeDefOf.NeutralEvent);
        }
    }
}
