using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace SimpleLeadership
{
    [HarmonyPatch(typeof(ChoiceLetter), nameof(ChoiceLetter.GetMouseoverText))]
    public static class ChoiceLetter_GetMouseoverText_Patch
    {
        public static void Postfix(ChoiceLetter __instance, ref string __result)
        {
            Faction faction = null;
            if (__instance.lookTargets?.PrimaryTarget.Thing is Pawn p)
            {
                faction = p.Faction;
            }
            else if (__instance.lookTargets?.PrimaryTarget.WorldObject is Settlement s)
            {
                faction = s.Faction;
            }

            var hover = Utils.GetActiveModifiersHoverText(faction);
            if (hover is not null)
            {
                __result += $"\n{hover}";
            }
        }
    }
}
