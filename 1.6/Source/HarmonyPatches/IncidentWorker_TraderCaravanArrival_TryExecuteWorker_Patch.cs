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
