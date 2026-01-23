using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SimpleLeadership
{
    [HarmonyPatch(typeof(IncidentWorker_RaidEnemy), "GetLetterText")]
    public class IncidentWorker_RaidEnemy_GetLetterText_Patch
    {
        public static void Postfix(ref string __result, IncidentParms parms, List<Pawn> pawns)
        {
            if (RaidContext.CurrentOrigin == null)
            {
                return;
            }

            var coloredBaseName = RaidContext.CurrentOrigin.Label.Colorize(ColorLibrary.RedReadable);
            __result += "\n\n" + string.Format("SL_RaidOriginInfo".Translate(), coloredBaseName);

            Pawn baseLeader = WorldComponent_LeaderTracker.Instance.GetBaseLeader(RaidContext.CurrentOrigin);
            if (baseLeader != null && pawns.Contains(baseLeader))
            {
                var coloredLeaderName = baseLeader.LabelShort.Colorize(ColoredText.NameColor);
                __result += "\n\n" + string.Format("SL_RaidOriginLeaderInfo".Translate(), coloredLeaderName);
            }
        }
    }
}
