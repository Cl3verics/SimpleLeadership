using HarmonyLib;
using RimWorld;
using Verse;

namespace SimpleLeadership
{
    [HarmonyPatch(typeof(IncidentWorker_VisitorGroup), nameof(IncidentWorker_VisitorGroup.TryExecuteWorker))]
    public static class IncidentWorker_VisitorGroup_TryExecuteWorker_Patch
    {
        public static void Postfix(IncidentParms parms, bool __result)
        {
            if (__result is false || parms.target is not Map map)
                return;
            Utils.TryTriggerInterventionRaid(map, parms.faction);
        }
    }
}
