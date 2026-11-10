using HarmonyLib;
using RimWorld;
using Verse;

namespace SimpleLeadership
{
    [HarmonyPatch(typeof(Pawn), "PreKidnapped")]
    public static class Pawn_PreKidnapped_Patch
    {
        public static void Postfix(Pawn __instance, Pawn kidnapper)
        {
            Utils.CheckForBaseLeaderDefeat(__instance);

            if (kidnapper?.Faction == null) return;
            if (!WorldComponent_LeaderTracker.Instance.lastRaidOrigin.TryGetValue(kidnapper.Faction, out var origin)) return;

            if (!WorldComponent_LeaderTracker.Instance.kidnappedPrisoners.TryGetValue(origin, out var prisonerList))
            {
                prisonerList = new KidnappedPrisonersList();
                WorldComponent_LeaderTracker.Instance.kidnappedPrisoners[origin] = prisonerList;
            }
            prisonerList.prisoners.Add(__instance);

            Messages.Message("SL_KidnappedTakenToBase".Translate(kidnapper.Faction.Named("FACTION"), __instance.Named("KIDNAPPED"), origin.Named("BASE")), origin, MessageTypeDefOf.ThreatBig);
        }
    }
}
