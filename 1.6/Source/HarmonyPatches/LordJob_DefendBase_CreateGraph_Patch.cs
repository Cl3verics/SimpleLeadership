using HarmonyLib;
using RimWorld;
using Verse.AI.Group;

namespace SimpleLeadership
{
    [HarmonyPatch(typeof(LordJob_DefendBase), nameof(LordJob_DefendBase.CreateGraph))]
    public static class LordJob_DefendBase_CreateGraph_Patch
    {
        public static void Postfix(LordJob_DefendBase __instance, StateGraph __result)
        {
            if (__instance.faction.HasDoctrine(PowerEventDefOf.SL_DefenseInDepth))
            {
                foreach (var trans in __result.transitions)
                {
                    if (trans.target is LordToil_AssaultColony)
                        trans.sources.RemoveAll(s => s is LordToil_DefendBase);
                }
                __result.transitions.RemoveAll(t => t.sources.Count == 0);
            }
        }
    }
}
