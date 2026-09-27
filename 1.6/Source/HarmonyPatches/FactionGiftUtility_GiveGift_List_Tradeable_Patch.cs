using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace SimpleLeadership
{
    [HarmonyPatch(typeof(FactionGiftUtility), nameof(FactionGiftUtility.GiveGift), new[] { typeof(List<Tradeable>), typeof(Faction), typeof(GlobalTargetInfo) })]
    public static class FactionGiftUtility_GiveGift_List_Tradeable_Patch
    {
        private const float FamineBonusFactor = 0.4f;

        public static void Prefix(List<Tradeable> tradeables, Faction giveTo, GlobalTargetInfo lookTarget, out (int goodwill, bool hasFood, bool hasRepatriated) __state)
        {
            var isInFamine = lookTarget.WorldObject is Settlement s && s.IsInPowerEvent(PowerEventDefOf.SL_Famine);
            var hasFood = isInFamine && tradeables.Any(t => t.ActionToDo == TradeAction.PlayerSells && t.ThingDef.IsNutritionGivingIngestible);
            var hasRepatriated = tradeables.Any(t => t.ActionToDo == TradeAction.PlayerSells && t.AnyThing is Pawn p && p.Faction == giveTo);
            if (giveTo.HasDoctrine(PowerEventDefOf.SL_FallenVeneration))
            {
                var corpses = tradeables.Where(t => t.ActionToDo == TradeAction.PlayerSells && t.AnyThing is Corpse c && c.InnerPawn?.Faction == giveTo).Sum(t => t.CountToTransferToDestination);
                if (corpses > 0)
                    Faction.OfPlayer.TryAffectGoodwillWith(giveTo, corpses * SimpleLeadershipMod.Settings.fallenVenerationCorpseGoodwill, canSendMessage: true, canSendHostilityLetter: true, reason: HistoryEventDefOf.GaveGift);
            }
            __state = (giveTo.PlayerGoodwill, hasFood, hasRepatriated);
        }

        public static void Postfix(Faction giveTo, (int goodwill, bool hasFood, bool hasRepatriated) __state)
        {
            var gained = giveTo.PlayerGoodwill - __state.goodwill;
            if (gained <= 0) return;
            if (__state.hasFood)
            {
                var bonus = Mathf.RoundToInt(gained * FamineBonusFactor);
                Faction.OfPlayer.TryAffectGoodwillWith(giveTo, bonus, canSendMessage: true, canSendHostilityLetter: true, reason: HistoryEventDefOf.GaveGift);
            }
            if (__state.hasRepatriated && giveTo.HasDoctrine(PowerEventDefOf.SL_GraciousRepatriation))
            {
                var repatBonus = Mathf.RoundToInt(gained * SimpleLeadershipMod.Settings.graciousRepatriationGoodwillMultiplier);
                Faction.OfPlayer.TryAffectGoodwillWith(giveTo, repatBonus, canSendMessage: true, canSendHostilityLetter: true, reason: HistoryEventDefOf.GaveGift);
            }
        }
    }
}
