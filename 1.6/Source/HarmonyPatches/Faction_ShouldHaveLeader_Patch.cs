using HarmonyLib;
using RimWorld;

namespace SimpleLeadership
{
    [HarmonyPatch(typeof(Faction), "ShouldHaveLeader", MethodType.Getter)]
    public static class Faction_ShouldHaveLeader_Patch
    {
        public static void Postfix(Faction __instance, ref bool __result)
        {
            if (__result && __instance.IsInPowerEvent<PowerVoid>())
            {
                __result = false;
            }
        }
    }
}
