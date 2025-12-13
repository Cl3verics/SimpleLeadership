using HarmonyLib;
using RimWorld;
using Verse;

namespace SimpleLeadership
{
    [HarmonyPatch(typeof(IncidentWorker_RaidEnemy), "FactionCanBeGroupSource")]
    public static class IncidentWorker_RaidEnemy_FactionCanBeGroupSource_Patch
    {
        public static void Postfix(ref bool __result, Faction f)
        {
            if (__result || f == null)
                return;

            if (f.IsInPowerEvent<PowerVoid>())
            {
                __result = false;
            }
        }
    }
}
