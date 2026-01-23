using HarmonyLib;
using RimWorld;
using System.Linq;
using Verse;

namespace SimpleLeadership
{
    [HarmonyPatch(typeof(Faction), "Notify_LeaderDied")]
    public static class Faction_Notify_LeaderDied_Patch
    {
        public static bool Prefix(Faction __instance)
        {
            Pawn oldLeader = __instance.leader;
            if (oldLeader == null)
            {
                return true; // let original method handle it
            }

            string label = "LetterLeadersDeathLabel".Translate(__instance.Name, __instance.LeaderTitle).Resolve().CapitalizeFirst();
            string body = "LetterLeadersDeath".Translate(__instance.NameColored, __instance.LeaderTitle, oldLeader.Named("OLDLEADER")).Resolve().CapitalizeFirst();

            var leaderTracker = WorldComponent_LeaderTracker.Instance;
            var candidates = leaderTracker.GetBaseLeadersFor(__instance).Where(p => p != null && !p.Dead && p != oldLeader).ToList();
            
            if (candidates.Any())
            {
                var actingLeader = candidates.RandomElement();
                var data = leaderTracker.GetLeadershipDataFor(__instance);
                if (data != null)
                {
                    data.actingLeader = actingLeader;
                }
                
                string actingLeaderText = "SL_ActingLeaderChosen".Translate(__instance.Named("FACTION"), actingLeader.Named("PAWN")).Resolve();
                body += "\n\n" + actingLeaderText;
            }

            if (!__instance.temporary)
            {
                Find.LetterStack.ReceiveLetter(label, body, LetterDefOf.NeutralEvent, oldLeader, __instance);
            }
            
            leaderTracker.StartPowerEvent(PowerEventDefOf.SL_PowerVoid, __instance);
            
            __instance.leader = null;

            QuestUtility.SendQuestTargetSignals(oldLeader.questTags, "NoLongerFactionLeader", oldLeader.Named("SUBJECT"));

            return false;
        }
    }
}
