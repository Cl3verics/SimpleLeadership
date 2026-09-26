using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace SimpleLeadership
{
    [HarmonyPatch(typeof(Trigger_FractionPawnsLost), nameof(Trigger_FractionPawnsLost.ActivateOn))]
    public static class Trigger_FractionPawnsLost_ActivateOn_Patch
    {
        private const float ForcesPreservationThresholdMultiplier = 0.5f;
        private const float HitAndRunCasualtyThreshold = 0.2f;
        private const float HitAndRunRegroupPointFactor = 0.8f;
        private const int RegroupDelayTicks = 15000;
        private const int HitAndRunChainLockoutTicks = 5 * GenDate.TicksPerDay;

        public static bool Prefix(Trigger_FractionPawnsLost __instance, Lord lord, TriggerSignal signal, ref bool __result)
        {
            if (lord.faction.HasDoctrine(PowerEventDefOf.SL_FinalPrice) && (lord.LordJob is LordJob_AssaultColony or LordJob_DefendBase))
            {
                __result = false;
                return false;
            }
            if (lord.faction.HasDoctrine(PowerEventDefOf.SL_ForcesPreservation) && (lord.LordJob is LordJob_AssaultColony or LordJob_DefendBase) && signal.type == TriggerSignalType.PawnLost && lord.numPawnsEverGained > 0)
            {
                __result = (float)lord.numPawnsLostViolently / lord.numPawnsEverGained >= __instance.fraction * ForcesPreservationThresholdMultiplier;
                return false;
            }
            if (lord.faction.HasDoctrine(PowerEventDefOf.SL_HitAndRun) && lord.LordJob is LordJob_AssaultColony && signal.type == TriggerSignalType.PawnLost && lord.numPawnsEverGained > 0 && (float)lord.numPawnsLostViolently / lord.numPawnsEverGained >= HitAndRunCasualtyThreshold)
            {
                var comp = lord.Map.GetComponent<MapComponent_DelayedRaid>();
                if (comp.IsScheduled is false && Find.TickManager.TicksGame >= comp.hitAndRunLockoutTick)
                {
                    var parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, lord.Map);
                    parms.faction = lord.faction;
                    parms.points = StorytellerUtility.DefaultThreatPointsNow(lord.Map) * HitAndRunRegroupPointFactor;
                    parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
                    comp.Schedule(parms, RegroupDelayTicks);
                    comp.hitAndRunLockoutTick = Find.TickManager.TicksGame + HitAndRunChainLockoutTicks;
                    Messages.Message("SL_HitAndRunRegrouping".Translate(lord.faction.NameColored), MessageTypeDefOf.ThreatBig);
                }
                __result = true;
                return false;
            }
            return true;
        }
    }
}
