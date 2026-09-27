using HarmonyLib;
using RimWorld;
using Verse;

namespace SimpleLeadership
{
    [HarmonyPatch(typeof(IncidentWorker_TraderCaravanArrival), nameof(IncidentWorker_TraderCaravanArrival.TryExecuteWorker))]
    public static class IncidentWorker_TraderCaravanArrival_TryExecuteWorker_Patch
    {
        public static bool Prefix(IncidentParms parms)
        {
            if (parms.faction?.IsInPowerEvent(PowerEventDefOf.SL_Sanctioned) is true)
            {
                return false;
            }
            var lastRaid = WorldComponent_LeaderTracker.Instance.lastPlayerRaidTick;
            var cooldownTicks = SimpleLeadershipMod.Settings.absoluteBordersCooldownDays * GenDate.TicksPerDay;
            if (parms.faction?.HasDoctrine(PowerEventDefOf.SL_AbsoluteBorders) is true && parms.target is Map map && map.IsPlayerHome && lastRaid > 0 && Find.TickManager.TicksGame - lastRaid < cooldownTicks)
                return false;
            return true;
        }

        public static void Postfix(IncidentParms parms, bool __result)
        {
            if (__result is false || parms.target is not Map map)
                return;

            Utils.TryTriggerInterventionRaid(map, parms.faction);
        }
    }
}
