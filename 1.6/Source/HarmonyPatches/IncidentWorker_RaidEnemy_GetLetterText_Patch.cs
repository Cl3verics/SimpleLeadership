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
            if (RaidContext.CurrentOrigin != null)
            {
                __result += "\n\n" + "SL_RaidOriginInfo".Translate(RaidContext.CurrentOrigin.Label, RaidContext.CurrentOrigin.Faction.Name);
                
                Pawn baseLeader = WorldComponent_LeaderTracker.Instance.GetBaseLeader(RaidContext.CurrentOrigin);
                if (baseLeader != null)
                {
                    __result += " " + "SL_RaidOriginLeaderInfo".Translate(baseLeader.LabelShort);
                }
            }
        }
    }
}
