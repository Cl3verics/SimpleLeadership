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

        public static void Prefix(List<Tradeable> tradeables, Faction giveTo, GlobalTargetInfo lookTarget, out (int goodwill, bool hasFood, int repatriatedBonus) __state)
        {
            var isInFamine = lookTarget.WorldObject is Settlement s && s.IsInPowerEvent(PowerEventDefOf.SL_Famine);
            var hasFood = isInFamine && tradeables.Any(t => t.ActionToDo == TradeAction.PlayerSells && t.ThingDef.IsNutritionGivingIngestible);
            var repatriatedBonus = 0;
            foreach (var t in tradeables.Where(tr => tr.ActionToDo == TradeAction.PlayerSells && tr.AnyThing is Pawn p && p.Faction == giveTo))
            {
                var pawn = (Pawn)t.AnyThing;
                var data = WorldComponent_LeaderTracker.Instance.GetLeadershipDataFor(giveTo);
                var isLeader = pawn == giveTo.leader || WorldComponent_LeaderTracker.Instance.GetSettlementsOfBaseLeader(pawn).Any() || pawn == data?.exLeader || data?.exBaseLeaders?.Contains(pawn) is true;
                var val = 0;
                if (isLeader)
                {
                    val = 25;
                    if (giveTo.HasDoctrine(PowerEventDefOf.SL_GraciousRepatriation))
                        val += Mathf.RoundToInt(val * SimpleLeadershipMod.Settings.graciousRepatriationGoodwillMultiplier);
                }
                else if (giveTo.HasDoctrine(PowerEventDefOf.SL_GraciousRepatriation))
                {
                    val = Mathf.RoundToInt(12 * SimpleLeadershipMod.Settings.graciousRepatriationGoodwillMultiplier);
                }
                repatriatedBonus += val;
            }
            if (giveTo.HasDoctrine(PowerEventDefOf.SL_FallenVeneration))
            {
                var corpses = tradeables.Where(t => t.ActionToDo == TradeAction.PlayerSells && t.AnyThing is Corpse c && c.InnerPawn?.Faction == giveTo).Sum(t => t.CountToTransferToDestination);
                if (corpses > 0)
                    Faction.OfPlayer.TryAffectGoodwillWith(giveTo, corpses * SimpleLeadershipMod.Settings.fallenVenerationCorpseGoodwill, canSendMessage: true, canSendHostilityLetter: true, reason: HistoryEventDefOf.GaveGift);
            }
            __state = (giveTo.PlayerGoodwill, hasFood, repatriatedBonus);
        }

        public static void Postfix(Faction giveTo, (int goodwill, bool hasFood, int repatriatedBonus) __state)
        {
            var gained = giveTo.PlayerGoodwill - __state.goodwill;
            if (__state.hasFood && gained > 0)
            {
                var bonus = Mathf.RoundToInt(gained * FamineBonusFactor);
                Faction.OfPlayer.TryAffectGoodwillWith(giveTo, bonus, canSendMessage: true, canSendHostilityLetter: true, reason: HistoryEventDefOf.GaveGift);
            }
            if (__state.repatriatedBonus > 0)
            {
                Faction.OfPlayer.TryAffectGoodwillWith(giveTo, __state.repatriatedBonus, canSendMessage: true, canSendHostilityLetter: true, reason: HistoryEventDefOf.GaveGift);
            }
        }
    }
}
