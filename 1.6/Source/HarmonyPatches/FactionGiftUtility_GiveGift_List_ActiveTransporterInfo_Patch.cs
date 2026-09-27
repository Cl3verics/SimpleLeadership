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

        public static void Prefix(List<ActiveTransporterInfo> pods, Settlement giveTo, out (int goodwill, bool hasFood, bool hasRepatriated) __state)
        {
            var hasFood = giveTo.IsInPowerEvent(PowerEventDefOf.SL_Famine) &&
                pods.Any(pod => pod.innerContainer.Any(t => t.def.IsNutritionGivingIngestible));
            var hasRepatriated = pods.Any(pod => pod.innerContainer.OfType<Pawn>().Any(p => p.Faction == giveTo.Faction));
            if (giveTo.Faction.HasDoctrine(PowerEventDefOf.SL_FallenVeneration))
            {
                var corpses = pods.Sum(pod => pod.innerContainer.Count(t => t is Corpse c && c.InnerPawn?.Faction == giveTo.Faction));
                if (corpses > 0)
                    Faction.OfPlayer.TryAffectGoodwillWith(giveTo.Faction, corpses * SimpleLeadershipMod.Settings.fallenVenerationCorpseGoodwill, canSendMessage: true, canSendHostilityLetter: true, reason: HistoryEventDefOf.GaveGift);
            }
            __state = (giveTo.Faction.PlayerGoodwill, hasFood, hasRepatriated);
        }

        public static void Postfix(Settlement giveTo, (int goodwill, bool hasFood, bool hasRepatriated) __state)
        {
            var gained = giveTo.Faction.PlayerGoodwill - __state.goodwill;
            if (gained <= 0) return;
            if (__state.hasFood)
            {
                var bonus = Mathf.RoundToInt(gained * FamineBonusFactor);
                Faction.OfPlayer.TryAffectGoodwillWith(giveTo.Faction, bonus, canSendMessage: true, canSendHostilityLetter: true, reason: HistoryEventDefOf.GaveGift);
            }
            if (__state.hasRepatriated && giveTo.Faction.HasDoctrine(PowerEventDefOf.SL_GraciousRepatriation))
            {
                var repatBonus = Mathf.RoundToInt(gained * SimpleLeadershipMod.Settings.graciousRepatriationGoodwillMultiplier);
                Faction.OfPlayer.TryAffectGoodwillWith(giveTo.Faction, repatBonus, canSendMessage: true, canSendHostilityLetter: true, reason: HistoryEventDefOf.GaveGift);
            }
        }
    }
}
