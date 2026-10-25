using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace SimpleLeadership
{
    [HarmonyPatch(typeof(Trigger_ThingsDamageTaken), nameof(Trigger_ThingsDamageTaken.ActivateOn))]
    public static class Trigger_ThingsDamageTaken_ActivateOn_Patch
    {
        public static void Postfix(Lord lord, ref bool __result)
        {
            if (__result || lord.LordJob is not LordJob_AssaultThings assaultThings)
                return;

            if (assaultThings.things.All(t => t == null || t.Destroyed || (t is Pawn p && (p.Dead || p.Downed || p.Spawned is false))))
                __result = true;
        }
    }
}
