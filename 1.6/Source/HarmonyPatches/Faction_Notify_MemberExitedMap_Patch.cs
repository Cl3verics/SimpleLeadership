using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace SimpleLeadership
{
    [HarmonyPatch(typeof(Faction), nameof(Faction.Notify_MemberExitedMap))]
    public static class Faction_Notify_MemberExitedMap_Patch
    {
        public static void Prefix(Faction __instance, out int __state) => __state = __instance.PlayerGoodwill;

        public static void Postfix(Faction __instance, Pawn member, bool freed, int __state)
        {
            if (freed is false || __instance.HasDoctrine(PowerEventDefOf.SL_GraciousRepatriation) is false || member.Downed || member.Dead || member.health.hediffSet.BleedRateTotal > 0.001f)
                return;
            var gained = __instance.PlayerGoodwill - __state;
            if (gained <= 0)
                return;
            var bonus = Mathf.RoundToInt(gained * SimpleLeadershipMod.Settings.graciousRepatriationGoodwillMultiplier);
            Faction.OfPlayer.TryAffectGoodwillWith(__instance, bonus, canSendMessage: true, canSendHostilityLetter: true, reason: HistoryEventDefOf.MemberExitedMapHealthy);
        }
    }
}
