using HarmonyLib;
using RimWorld;
using Verse;

namespace SimpleLeadership
{
    [HarmonyPatch(typeof(Pawn_GuestTracker), "get_Resistance")]
    public static class Pawn_GuestTracker_get_Resistance_Patch
    {
        public static void Postfix(ref float __result, Pawn_GuestTracker __instance)
        {
            Pawn pawn = __instance.pawn;
            if (pawn?.Faction == null)
                return;

            if (pawn.Faction.IsInPowerEvent<PowerVoid>())
            {
                __result *= 0.3f;
            }
        }
    }
}
