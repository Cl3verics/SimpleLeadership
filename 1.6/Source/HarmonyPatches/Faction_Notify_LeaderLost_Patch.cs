using HarmonyLib;
using RimWorld;
using Verse;

namespace SimpleLeadership
{
    [HarmonyPatch(typeof(Faction), "Notify_LeaderLost")]
    public static class Faction_Notify_LeaderLost_Patch
    {
        public static void Postfix(Faction __instance)
        {
            WorldComponent_LeaderTracker.Instance.StartPowerEvent(PowerEventDefOf.SL_PowerVoid, __instance);
        }
    }
}
