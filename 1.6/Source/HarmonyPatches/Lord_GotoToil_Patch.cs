using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;

namespace SimpleLeadership
{
    [HarmonyPatch(typeof(Lord), nameof(Lord.GotoToil))]
    public static class Lord_GotoToil_Patch
    {
        private const float ScorchedEarthFireSizeMin = 0.5f;
        private const float ScorchedEarthFireSizeMax = 1.5f;
        private const float ScorchedEarthMinFlammability = 0.5f;
        public static bool Prefix(Lord __instance, LordToil newLordToil)
        {
            var map = __instance.Map;

            if (__instance.LordJob is LordJob_DefendBase && newLordToil is LordToil_PanicFlee && __instance.faction.HasDoctrine(PowerEventDefOf.SL_ScorchedEarth))
            {
                var targets = map.listerBuildings.allBuildingsNonColonist
                    .Where(b => b.def.building.isNaturalRock is false && b.GetStatValue(StatDefOf.Flammability) >= ScorchedEarthMinFlammability)
                    .Take(SimpleLeadershipMod.Settings.scorchedEarthMaxFires)
                    .ToList();
                foreach (var b in targets)
                {
                    FireUtility.TryStartFireIn(b.Position, map, Rand.Range(ScorchedEarthFireSizeMin, ScorchedEarthFireSizeMax), null);
                }
                if (targets.Count > 0)
                    Messages.Message("SL_ScorchedEarthTriggered".Translate(__instance.faction.NameColored), MessageTypeDefOf.ThreatBig);
            }

            if (__instance.LordJob is LordJob_DefendBase && newLordToil is LordToil_AssaultColony && __instance.Map.Parent is Settlement reinforceSettlement && reinforceSettlement.IsInPowerEvent(PowerEventDefOf.SL_Reinforcements))
            {
                var reinforceComp = __instance.Map.GetComponent<MapComponent_DelayedRaid>();
                if (reinforceComp.IsScheduled is false)
                {
                    var reinforceParms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, __instance.Map);
                    reinforceParms.faction = reinforceSettlement.Faction;
                    reinforceParms.points = StorytellerUtility.DefaultThreatPointsNow(__instance.Map) * 0.75f;
                    reinforceParms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;

                    reinforceComp.Schedule(reinforceParms, 5000);

                    Find.LetterStack.ReceiveLetter("SL_ReinforcementsCalledLabel".Translate(), "SL_ReinforcementsCalledBody".Translate(reinforceSettlement.Label, 5000.ToStringTicksToPeriod()), LetterDefOf.ThreatBig, reinforceSettlement);
                }
            }

            if (__instance.faction.HasDoctrine(PowerEventDefOf.SL_FinalPrice) && (newLordToil is LordToil_PanicFlee && __instance.LordJob is LordJob_AssaultColony or LordJob_DefendBase || newLordToil is LordToil_ExitMap && __instance.LordJob is LordJob_AssaultColony && __instance.ownedPawns.Any(p => p.carryTracker.CarriedThing is Pawn carried && carried.Faction == Faction.OfPlayer) is false) || __instance.LordJob is LordJob_DefendBase && newLordToil is LordToil_AssaultColony && __instance.faction.HasDoctrine(PowerEventDefOf.SL_DefenseInDepth))
                return false;

            return true;
        }
    }
}
