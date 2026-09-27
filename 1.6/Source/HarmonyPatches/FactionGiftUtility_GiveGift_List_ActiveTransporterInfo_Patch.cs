using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace SimpleLeadership
{
    [HarmonyPatch(typeof(FactionGiftUtility), nameof(FactionGiftUtility.GiveGift), new[] { typeof(List<ActiveTransporterInfo>), typeof(Settlement) })]
    public static class FactionGiftUtility_GiveGift_List_ActiveTransporterInfo_Patch
    {
        private const float FamineBonusFactor = 0.4f;

        public static void Prefix(List<ActiveTransporterInfo> pods, Settlement giveTo, out (int goodwill, bool hasFood, int repatriatedBonus) __state)
        {
            var hasFood = giveTo.IsInPowerEvent(PowerEventDefOf.SL_Famine) &&
                pods.Any(pod => pod.innerContainer.Any(t => t.def.IsNutritionGivingIngestible));
            var repatriatedBonus = 0;
            var faction = giveTo.Faction;
            foreach (var pawn in pods.SelectMany(pod => pod.innerContainer.OfType<Pawn>()))
            {
                if (pawn.Faction != faction) continue;
                var data = WorldComponent_LeaderTracker.Instance.GetLeadershipDataFor(faction);
                var isLeader = pawn == faction.leader || WorldComponent_LeaderTracker.Instance.GetSettlementsOfBaseLeader(pawn).Any() || pawn == data?.exLeader || data?.exBaseLeaders?.Contains(pawn) is true;
                var val = 0;
                if (isLeader)
                {
                    val = 25;
                    if (faction.HasDoctrine(PowerEventDefOf.SL_GraciousRepatriation))
                        val += Mathf.RoundToInt(val * SimpleLeadershipMod.Settings.graciousRepatriationGoodwillMultiplier);
                }
                else if (faction.HasDoctrine(PowerEventDefOf.SL_GraciousRepatriation))
                {
                    val = Mathf.RoundToInt(12 * SimpleLeadershipMod.Settings.graciousRepatriationGoodwillMultiplier);
                }
                repatriatedBonus += val;
            }
            if (faction.HasDoctrine(PowerEventDefOf.SL_FallenVeneration))
            {
                var corpses = pods.Sum(pod => pod.innerContainer.Count(t => t is Corpse c && c.InnerPawn?.Faction == giveTo.Faction));
                if (corpses > 0)
                    Faction.OfPlayer.TryAffectGoodwillWith(giveTo.Faction, corpses * SimpleLeadershipMod.Settings.fallenVenerationCorpseGoodwill, canSendMessage: true, canSendHostilityLetter: true, reason: HistoryEventDefOf.GaveGift);
            }
            __state = (giveTo.Faction.PlayerGoodwill, hasFood, repatriatedBonus);
        }

        public static void Postfix(Settlement giveTo, (int goodwill, bool hasFood, int repatriatedBonus) __state)
        {
            var gained = giveTo.Faction.PlayerGoodwill - __state.goodwill;
            if (__state.hasFood && gained > 0)
            {
                var bonus = Mathf.RoundToInt(gained * FamineBonusFactor);
                Faction.OfPlayer.TryAffectGoodwillWith(giveTo.Faction, bonus, canSendMessage: true, canSendHostilityLetter: true, reason: HistoryEventDefOf.GaveGift);
            }
            if (__state.repatriatedBonus > 0)
            {
                Faction.OfPlayer.TryAffectGoodwillWith(giveTo.Faction, __state.repatriatedBonus, canSendMessage: true, canSendHostilityLetter: true, reason: HistoryEventDefOf.GaveGift);
            }
        }
    }
}
