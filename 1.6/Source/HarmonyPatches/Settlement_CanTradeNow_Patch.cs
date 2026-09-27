using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace SimpleLeadership
{
    [HarmonyPatch(typeof(Settlement), nameof(Settlement.CanTradeNow), MethodType.Getter)]
    public static class Settlement_CanTradeNow_Patch
    {
        public static void Postfix(Settlement __instance, ref bool __result)
        {
            if (__result is false || Settlement_GetCaravanGizmos_Patch.GettingGizmos) return;
            if (__instance.Faction?.IsInPowerEvent(PowerEventDefOf.SL_Sanctioned) is true)
                __result = false;
            var lastRaid = WorldComponent_LeaderTracker.Instance.lastPlayerRaidTick;
            var cooldownTicks = SimpleLeadershipMod.Settings.absoluteBordersCooldownDays * GenDate.TicksPerDay;
            if (__instance.Faction?.HasDoctrine(PowerEventDefOf.SL_AbsoluteBorders) is true && lastRaid > 0 && Find.TickManager.TicksGame - lastRaid < cooldownTicks)
                __result = false;
        }
    }
}
