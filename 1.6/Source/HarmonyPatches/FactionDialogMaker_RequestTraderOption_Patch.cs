using HarmonyLib;
using RimWorld;
using Verse;

namespace SimpleLeadership
{
    [HarmonyPatch(typeof(FactionDialogMaker), nameof(FactionDialogMaker.RequestTraderOption))]
    public static class FactionDialogMaker_RequestTraderOption_Patch
    {
        public static void Postfix(DiaOption __result, Faction faction)
        {
            if (faction.IsInPowerEvent(PowerEventDefOf.SL_Sanctioned))
            {
                __result.Disable("SL_SanctionedCannotTrade".Translate());
            }
            else
            {
                var lastRaid = WorldComponent_LeaderTracker.Instance.lastPlayerRaidTick;
                var cooldownTicks = SimpleLeadershipMod.Settings.absoluteBordersCooldownDays * GenDate.TicksPerDay;
                if (faction.HasDoctrine(PowerEventDefOf.SL_AbsoluteBorders) && lastRaid > 0 && Find.TickManager.TicksGame - lastRaid < cooldownTicks)
                {
                    __result.Disable("SL_AbsoluteBordersCannotTrade".Translate());
                }
            }
        }
    }
}
