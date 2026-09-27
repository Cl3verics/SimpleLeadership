using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace SimpleLeadership
{
    [HotSwappable]
    [HarmonyPatch(typeof(Settlement), nameof(Settlement.GetCaravanGizmos))]
    public static class Settlement_GetCaravanGizmos_Patch
    {
        public static bool GettingGizmos;
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> values, Settlement __instance)
        {
            var sanctioned = __instance.Faction?.IsInPowerEvent(PowerEventDefOf.SL_Sanctioned) is true;
            var lastRaid = WorldComponent_LeaderTracker.Instance.lastPlayerRaidTick;
            var cooldownTicks = SimpleLeadershipMod.Settings.absoluteBordersCooldownDays * GenDate.TicksPerDay;
            var absoluteBorders = __instance.Faction?.HasDoctrine(PowerEventDefOf.SL_AbsoluteBorders) is true && lastRaid > 0 && Find.TickManager.TicksGame - lastRaid < cooldownTicks;
            GettingGizmos = true;
            try
            {
                foreach (var gizmo in values)
                {
                    if (gizmo is Command_Action cmd && cmd.icon == CaravanVisitUtility.TradeCommandTex)
                    {
                        if (sanctioned)
                            cmd.Disable("SL_SanctionedCannotTrade".Translate());
                        else if (absoluteBorders)
                            cmd.Disable("SL_AbsoluteBordersCannotTrade".Translate());
                    }
                    yield return gizmo;
                }
            }
            finally
            {
                GettingGizmos = false;
            }
        }
    }
}
