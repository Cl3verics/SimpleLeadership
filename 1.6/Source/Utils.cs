using System;
using System.Collections.Generic;
using Verse;
using RimWorld;
using RimWorld.Planet;
using System.Linq;

namespace SimpleLeadership
{
    [StaticConstructorOnStartup]
    public static class Utils
    {
        static Utils()
        {
            foreach (var def in DefDatabase<WorldObjectDef>.AllDefs)
            {
                if (typeof(Site).IsAssignableFrom(def.worldObjectClass))
                {
                    def.comps ??= new List<WorldObjectCompProperties>();
                    def.comps.Add(new WorldObjectCompProperties_SiteOwnership());
                }
                else if (typeof(Settlement).IsAssignableFrom(def.worldObjectClass))
                {
                    if (def.inspectorTabs == null)
                        def.inspectorTabs = new List<Type>();
                    def.inspectorTabs.Add(typeof(WITab_FactionLeadership));

                    if (def.inspectorTabsResolved == null)
                        def.inspectorTabsResolved = new List<InspectTabBase>();
                    def.inspectorTabsResolved.Add(InspectTabManager.GetSharedInstance(typeof(WITab_FactionLeadership)));
                }
            }
        }


        public static bool IsInPowerEvent<T>(this object obj) where T : PowerEventBase
        {
            return WorldComponent_LeaderTracker.Instance.IsInPowerEvent<T>(obj);
        }

        public static bool IsInPowerEvent(this object obj, PowerEventDef def)
        {
            if (obj == null || def == null)
                return false;
            return WorldComponent_LeaderTracker.Instance.GetActiveEventsFor(obj).Any(ev => ev.def == def);
        }

        public static IEnumerable<T> GetActiveEvents<T>(this object obj) where T : PowerEventBase
        {
            return WorldComponent_LeaderTracker.Instance.GetActiveEventsFor(obj).OfType<T>();
        }

        public static float CalculateSpawnChance(Pawn leader, Faction faction, Settlement settlement)
        {
            if (leader == null || leader.Dead || leader.Spawned)
                return 0f;

            var leaderTracker = WorldComponent_LeaderTracker.Instance;
            float spawnChance = 0f;

            if (leader == faction.leader)
            {
                int settlementCount = Find.WorldObjects.Settlements.Count(s => s.Faction == faction);
                if (settlementCount > 0)
                {
                    spawnChance = 1f / (float)settlementCount;
                }
            }
            else
            {
                int controlledBases = Find.WorldObjects.Settlements.Count(s => s.Faction == faction && leaderTracker.GetBaseLeader(s) == leader);
                if (controlledBases > 0)
                {
                    spawnChance = 1f / controlledBases;
                }
            }

            if (settlement != null && settlement.IsInPowerEvent(PowerEventDefOf.SL_Inspection))
            {
                spawnChance += 0.3f;
            }

            return spawnChance;
        }

        public static void CheckForBaseLeaderDefeat(Pawn pawn)
        {
            if (pawn.Faction == null)
                return;

            WorldComponent_LeaderTracker leaderTracker = WorldComponent_LeaderTracker.Instance;
            Settlement leaderSettlement = leaderTracker.GetSettlementOfBaseLeader(pawn);

            if (leaderSettlement != null)
            {
                if (pawn != pawn.Faction.leader)
                {
                    leaderTracker.StartPowerEvent(PowerEventDefOf.SL_PowerStruggle, leaderSettlement);
                }
            }
        }
    }
}
