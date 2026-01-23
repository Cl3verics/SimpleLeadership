using HarmonyLib;
using Verse;

namespace SimpleLeadership
{
    [HarmonyPatch(typeof(Pawn), "Kill")]
    public static class Pawn_Kill_Patch
    {
        public static void Postfix(Pawn __instance, DamageInfo? dinfo, Hediff exactCulprit)
        {
            Utils.CheckForBaseLeaderDefeat(__instance);
        }
    }
}
