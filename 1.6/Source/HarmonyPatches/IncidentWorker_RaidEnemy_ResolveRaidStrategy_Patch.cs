using HarmonyLib;
using RimWorld;
using Verse;

namespace SimpleLeadership
{
    [HarmonyPatch(typeof(IncidentWorker_RaidEnemy), nameof(IncidentWorker_RaidEnemy.ResolveRaidStrategy))]
    public static class IncidentWorker_RaidEnemy_ResolveRaidStrategy_Patch
    {
        public static void Postfix(IncidentParms parms, PawnGroupKindDef groupKind)
        {
            if (parms.faction.HasDoctrine(PowerEventDefOf.SL_AttritionWarfare) is false || parms.faction.HostileTo(Faction.OfPlayer) is false || parms.raidStrategy == RaidStrategyDefOf.ImmediateAttackFriendly || parms.raidStrategy == PowerEventDefOf.Siege)
                return;
            if (Rand.Chance(SimpleLeadershipMod.Settings.attritionSiegeChance) && PowerEventDefOf.Siege.Worker.CanUseWith(parms, groupKind))
                parms.raidStrategy = PowerEventDefOf.Siege;
        }
    }
}
