using HarmonyLib;
using RimWorld;
using Verse;

namespace SimpleLeadership
{
    [HarmonyPatch(typeof(Pawn), "PreKidnapped")]
    public static class Pawn_PreKidnapped_Patch
    {
        public static void Postfix(Pawn __instance, Pawn kidnapper)
        {
            BaseLeaderDefeatChecker.CheckForBaseLeaderDefeat(__instance);
        }
    }
}
