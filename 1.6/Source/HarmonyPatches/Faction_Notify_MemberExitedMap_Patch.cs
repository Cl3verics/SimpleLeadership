using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace SimpleLeadership
{
    [HarmonyPatch(typeof(Faction), nameof(Faction.Notify_MemberExitedMap))]
    public static class Faction_Notify_MemberExitedMap_Patch
    {
        public static void Postfix(Faction __instance, Pawn member, bool freed)
        {
            if (freed is false || member.Downed || member.Dead || member.health.hediffSet.BleedRateTotal > 0.001f)
                return;
            var data = WorldComponent_LeaderTracker.Instance.GetLeadershipDataFor(__instance);
            var isLeader = member == __instance.leader || WorldComponent_LeaderTracker.Instance.GetSettlementsOfBaseLeader(member).Any() || member == data?.exLeader || data?.exBaseLeaders?.Contains(member) is true;
            if (isLeader)
            {
                var leaderBonus = 25;
                if (__instance.HasDoctrine(PowerEventDefOf.SL_GraciousRepatriation))
                    leaderBonus += Mathf.RoundToInt(leaderBonus * SimpleLeadershipMod.Settings.graciousRepatriationGoodwillMultiplier);
                Faction.OfPlayer.TryAffectGoodwillWith(__instance, leaderBonus, canSendMessage: true, canSendHostilityLetter: true, reason: HistoryEventDefOf.MemberExitedMapHealthy);
            }
            else if (__instance.HasDoctrine(PowerEventDefOf.SL_GraciousRepatriation))
            {
                var repatBonus = Mathf.RoundToInt(12 * SimpleLeadershipMod.Settings.graciousRepatriationGoodwillMultiplier);
                Faction.OfPlayer.TryAffectGoodwillWith(__instance, repatBonus, canSendMessage: true, canSendHostilityLetter: true, reason: HistoryEventDefOf.MemberExitedMapHealthy);
            }
        }
    }
}
