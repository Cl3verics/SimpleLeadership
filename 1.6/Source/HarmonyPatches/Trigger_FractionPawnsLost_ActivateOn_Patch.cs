using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace SimpleLeadership
{
    [HarmonyPatch(typeof(Trigger_FractionPawnsLost), nameof(Trigger_FractionPawnsLost.ActivateOn))]
    public static class Trigger_FractionPawnsLost_ActivateOn_Patch
    {
        public static bool Prefix(Trigger_FractionPawnsLost __instance, Lord lord, TriggerSignal signal, ref bool __result)
        {
            if (lord.faction.HasDoctrine(PowerEventDefOf.SL_ScorchedEarth) && lord.LordJob is LordJob_DefendBase && signal.type == TriggerSignalType.PawnLost && lord.numPawnsEverGained > 0 && (float)lord.numPawnsLostViolently / lord.numPawnsEverGained >= SimpleLeadershipMod.Settings.scorchedEarthCasualtyThreshold)
                Lord_GotoToil_Patch.TryTriggerScorchedEarth(lord);
            if (lord.faction.HasDoctrine(PowerEventDefOf.SL_FinalPrice) && lord.LordJob is LordJob_AssaultColony or LordJob_DefendBase)
            {
                __result = false;
                return false;
            }
            if (lord.faction.HasDoctrine(PowerEventDefOf.SL_ForcesPreservation) && lord.LordJob is LordJob_AssaultColony or LordJob_DefendBase && signal.type == TriggerSignalType.PawnLost && lord.numPawnsEverGained > 0)
            {
                __result = (float)lord.numPawnsLostViolently / lord.numPawnsEverGained >= __instance.fraction * SimpleLeadershipMod.Settings.forcesPreservationRetreatMultiplier;
                return false;
            }
            if (lord.faction.HasDoctrine(PowerEventDefOf.SL_HitAndRun) && lord.LordJob is LordJob_AssaultColony && signal.type == TriggerSignalType.PawnLost && lord.numPawnsEverGained > 0 && (float)lord.numPawnsLostViolently / lord.numPawnsEverGained >= SimpleLeadershipMod.Settings.hitAndRunCasualtyThreshold)
            {
                var comp = lord.Map.GetComponent<MapComponent_DelayedRaid>();
                if (Find.TickManager.TicksGame >= comp.hitAndRunLockoutTick)
                {
                    var parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, lord.Map);
                    parms.faction = lord.faction;
                    parms.points = StorytellerUtility.DefaultThreatPointsNow(lord.Map) * SimpleLeadershipMod.Settings.hitAndRunRegroupPointMultiplier;
                    parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
                    var delayTicks = Mathf.RoundToInt(SimpleLeadershipMod.Settings.hitAndRunRegroupDelayHours * GenDate.TicksPerHour);
                    comp.Schedule(parms, delayTicks);
                    var lockoutTicks = Mathf.RoundToInt(SimpleLeadershipMod.Settings.hitAndRunLockoutDays * GenDate.TicksPerDay);
                    comp.hitAndRunLockoutTick = Find.TickManager.TicksGame + lockoutTicks;
                    Messages.Message("SL_HitAndRunRegrouping".Translate(lord.faction.NameColored), MessageTypeDefOf.ThreatBig);
                }
                __result = true;
                return false;
            }
            return true;
        }
    }
}
