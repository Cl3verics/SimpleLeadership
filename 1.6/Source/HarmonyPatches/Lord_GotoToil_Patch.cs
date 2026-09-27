using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace SimpleLeadership
{
    [HarmonyPatch(typeof(Lord), nameof(Lord.GotoToil))]
    public static class Lord_GotoToil_Patch
    {
        private const string ReinforceRaidTag = "Reinforce";
        private const float ScorchedEarthFireSizeMin = 1.5f;
        private const float ScorchedEarthFireSizeMax = 3f;
        private const float ScorchedEarthMinFlammability = 0.5f;

        public static bool Prefix(Lord __instance, LordToil newLordToil)
        {
            if (__instance.LordJob is LordJob_DefendBase && newLordToil is LordToil_AssaultColony or LordToil_PanicFlee && __instance.faction.HasDoctrine(PowerEventDefOf.SL_ScorchedEarth))
                TryTriggerScorchedEarth(__instance);

            if (__instance.LordJob is LordJob_DefendBase && newLordToil is LordToil_AssaultColony && __instance.Map.Parent is Settlement reinforceSettlement && reinforceSettlement.IsInPowerEvent(PowerEventDefOf.SL_Reinforcements))
            {
                var reinforceComp = __instance.Map.GetComponent<MapComponent_DelayedRaid>();
                if (reinforceComp.HasScheduledTag(ReinforceRaidTag) is false)
                {
                    var reinforceParms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, __instance.Map);
                    reinforceParms.faction = reinforceSettlement.Faction;
                    reinforceParms.points = StorytellerUtility.DefaultThreatPointsNow(__instance.Map) * 0.75f;
                    reinforceParms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;

                    reinforceComp.Schedule(reinforceParms, 5000, ReinforceRaidTag);

                    Find.LetterStack.ReceiveLetter("SL_ReinforcementsCalledLabel".Translate(), "SL_ReinforcementsCalledBody".Translate(reinforceSettlement.Label, 5000.ToStringTicksToPeriod()), LetterDefOf.ThreatBig, reinforceSettlement);
                }
            }

            if (__instance.faction.HasDoctrine(PowerEventDefOf.SL_FinalPrice) && (newLordToil is LordToil_PanicFlee && __instance.LordJob is LordJob_AssaultColony or LordJob_DefendBase || newLordToil is LordToil_ExitMap && __instance.LordJob is LordJob_AssaultColony && __instance.ownedPawns.Any(p => p.carryTracker.CarriedThing is Pawn carried && carried.Faction == Faction.OfPlayer) is false))
                return false;

            return true;
        }

        public static void TryTriggerScorchedEarth(Lord lord)
        {
            var map = lord.Map;
            var comp = map.GetComponent<MapComponent_DelayedRaid>();
            if (comp.scorchedEarthTriggeredLordIDs.Contains(lord.loadID))
                return;
            comp.scorchedEarthTriggeredLordIDs.Add(lord.loadID);
            var targets = map.listerBuildings.allBuildingsNonColonist
                .Where(b => b.def.building.isNaturalRock is false && b.GetStatValue(StatDefOf.Flammability) >= ScorchedEarthMinFlammability)
                .Take(SimpleLeadershipMod.Settings.scorchedEarthMaxFires)
                .ToList();
            foreach (var b in targets)
            {
                FireUtility.TryStartFireIn(b.Position, map, Rand.Range(ScorchedEarthFireSizeMin, ScorchedEarthFireSizeMax), null);
            }
            foreach (var p in lord.ownedPawns.Where(p => p.Dead is false && p.Downed is false))
            {
                var nearby = GenClosest.ClosestThingReachable(p.Position, map, ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial), PathEndMode.Touch, TraverseParms.For(p), 10f, t => t.def.building.isNaturalRock is false && t.GetStatValue(StatDefOf.Flammability) > 0.1f);
                if (nearby != null)
                {
                    FireUtility.TryStartFireIn(nearby.Position, map, ScorchedEarthFireSizeMax, p);
                }
            }
            if (targets.Count > 0)
                Messages.Message("SL_ScorchedEarthTriggered".Translate(lord.faction.NameColored), MessageTypeDefOf.ThreatBig);
        }
    }
}
