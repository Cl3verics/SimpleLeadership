using HarmonyLib;
using RimWorld;
using Verse;

namespace SimpleLeadership
{
    [HarmonyPatch(typeof(IncidentWorker_TraderCaravanArrival), nameof(IncidentWorker_TraderCaravanArrival.TryExecuteWorker))]
    public static class IncidentWorker_TraderCaravanArrival_TryExecuteWorker_Patch
    {
        public static void Postfix(IncidentParms parms, bool __result)
        {
            if (__result is false || parms.target is not Map map)
                return;

            Utils.TryTriggerInterventionRaid(map, parms.faction);
        }
    }

    [HarmonyPatch(typeof(IncidentWorker_NeutralGroup), "TryResolveParmsGeneral")]
    public static class IncidentWorker_NeutralGroup_TryResolveParmsGeneral_Patch
    {
        public static void Postfix(IncidentWorker_NeutralGroup __instance, IncidentParms parms, ref bool __result)
        {
            if (__result is false || __instance is not IncidentWorker_TraderCaravanArrival || parms.faction == null)
                return;
            if (parms.faction.IsInPowerEvent(PowerEventDefOf.SL_Sanctioned))
            {
                __result = false;
                return;
            }
            var lastRaid = WorldComponent_LeaderTracker.Instance.lastPlayerRaidTick;
            var cooldownTicks = SimpleLeadershipMod.Settings.absoluteBordersCooldownDays * GenDate.TicksPerDay;
            if (parms.faction.HasDoctrine(PowerEventDefOf.SL_AbsoluteBorders) && parms.target is Map map && map.IsPlayerHome && lastRaid > 0 && Find.TickManager.TicksGame - lastRaid < cooldownTicks)
                __result = false;
        }
    }
}
