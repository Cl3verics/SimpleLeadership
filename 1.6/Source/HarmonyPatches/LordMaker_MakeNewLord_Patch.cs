using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace SimpleLeadership
{
    [HarmonyPatch(typeof(LordMaker), nameof(LordMaker.MakeNewLord))]
    public static class LordMaker_MakeNewLord_Patch
    {
        public static void Prefix(ref LordJob lordJob, Faction faction, Map map)
        {
            if (RaidContext.InterventionTargetFaction == null)
                return;
            if (lordJob is not LordJob_AssaultColony and not LordJob_AssaultThings and not LordJob_Siege)
                return;
            var targets = map.mapPawns.AllPawnsSpawned
                .Where(p => p.Faction == RaidContext.InterventionTargetFaction)
                .Cast<Thing>()
                .ToList();
            if (targets.Count > 0)
                lordJob = new LordJob_AssaultThings(faction, targets);
        }
    }
}
