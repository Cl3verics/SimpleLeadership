using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace SimpleLeadership
{
    [HarmonyPatch(typeof(SettlementUtility), nameof(SettlementUtility.AffectRelationsOnAttacked))]
    public static class SettlementUtility_AffectRelationsOnAttacked_Patch
    {
        private const float MutualDefenseRetaliationChance = 0.4f;
        private const int MutualDefenseRetaliationDelayMinTicks = 1000;
        private const int MutualDefenseRetaliationDelayMaxTicks = 5000;
        private const int MutualDefenseHostilityDrop = -100;
        public static void Postfix(MapParent mapParent)
        {
            WorldComponent_LeaderTracker.Instance.lastPlayerRaidTick = Find.TickManager.TicksGame;
            if (mapParent is Settlement attackedSettlement && attackedSettlement.Faction != null)
            {
                WorldComponent_LeaderTracker.Instance.lastPlayerRaidVictim = attackedSettlement.Faction;
                var retaliationTarget = Find.AnyPlayerHomeMap;
                if (retaliationTarget != null)
                {
                    foreach (var faction in Find.FactionManager.AllFactionsVisible)
                    {
                        if (faction == attackedSettlement.Faction || faction.HasDoctrine(PowerEventDefOf.SL_MutualDefense) is false || faction.RelationKindWith(attackedSettlement.Faction) != FactionRelationKind.Ally || Rand.Chance(MutualDefenseRetaliationChance) is false)
                            continue;
                        if (faction.HostileTo(Faction.OfPlayer) is false)
                        {
                            faction.TryAffectGoodwillWith(Faction.OfPlayer, MutualDefenseHostilityDrop, canSendMessage: true, canSendHostilityLetter: true, reason: HistoryEventDefOf.AttackedSettlement);
                            if (faction.HostileTo(Faction.OfPlayer) is false)
                                continue;
                        }
                        var incidentParms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, retaliationTarget);
                        incidentParms.faction = faction;
                        Find.Storyteller.incidentQueue.Add(IncidentDefOf.RaidEnemy, Find.TickManager.TicksGame + Rand.Range(MutualDefenseRetaliationDelayMinTicks, MutualDefenseRetaliationDelayMaxTicks), incidentParms);
                        Find.LetterStack.ReceiveLetter("SL_MutualDefenseLetterLabel".Translate(), "SL_MutualDefenseLetterBody".Translate(faction.NameColored, attackedSettlement.Faction.NameColored), LetterDefOf.ThreatBig);
                    }
                }
            }

            if (mapParent is not Settlement settlement || settlement.HasMap is false)
                return;

            if (settlement.IsInPowerEvent(PowerEventDefOf.SL_Support) is false || settlement.Faction.HostileTo(Faction.OfPlayer) is false)
                return;

            MapGenerator_GenerateMap_Patch.DoSupportArriving(settlement.Map, settlement);
        }
    }
}
