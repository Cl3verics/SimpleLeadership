using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SimpleLeadership
{
    [HarmonyPatch(typeof(IncidentWorker_RaidEnemy), nameof(IncidentWorker_RaidEnemy.GetLetterText))]
    public class IncidentWorker_RaidEnemy_GetLetterText_Patch
    {
        public static void Postfix(ref string __result, IncidentParms parms, List<Pawn> pawns)
        {
            if (RaidContext.CurrentOrigin != null)
            {
                var coloredBaseName = RaidContext.CurrentOrigin.Label.Colorize(ColorLibrary.RedReadable);
                __result += "\n\n" + "SL_RaidOriginInfo".Translate(coloredBaseName).Resolve();
            }

            var doctrines = parms.faction.GetDoctrines().Where(d => d.def.affectsRaidBehavior).ToList();
            if (doctrines.Count > 0)
            {
                var formatted = string.Join(", ", doctrines.Select(d => d.def.ShortLabel.CapitalizeFirst().Colorize(d.def.categoryColor)));
                __result += "\n\n" + "SL_ActiveModifiers".Translate(formatted).Resolve();
            }

            if (RaidContext.CurrentOrigin != null)
            {
                var baseLeader = WorldComponent_LeaderTracker.Instance.GetBaseLeader(RaidContext.CurrentOrigin);
                if (baseLeader != null && pawns.Contains(baseLeader))
                {
                    var coloredLeaderName = baseLeader.LabelShort.Colorize(ColoredText.NameColor);
                    __result += "\n\n" + "SL_RaidOriginLeaderInfo".Translate(coloredLeaderName).Resolve();
                }
            }
        }
    }
}
