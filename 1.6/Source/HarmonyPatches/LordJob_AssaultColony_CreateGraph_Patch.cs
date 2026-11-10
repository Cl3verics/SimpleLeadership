using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse.AI.Group;

namespace SimpleLeadership
{
    [HarmonyPatch(typeof(LordJob_AssaultColony), nameof(LordJob_AssaultColony.CreateGraph))]
    public static class LordJob_AssaultColony_CreateGraph_Patch
    {
        public static void Postfix(LordJob_AssaultColony __instance, StateGraph __result)
        {
            if (__instance.assaulterFaction.HasDoctrine(PowerEventDefOf.SL_FinalPrice))
            {
                __result.transitions.RemoveAll(t => t.target is LordToil_ExitMap && t.triggers.Any(tr => tr is Trigger_TicksPassed or Trigger_FractionColonyDamageTaken));
            }
        }
    }
}
