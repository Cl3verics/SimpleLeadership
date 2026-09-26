using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

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
                    def.inspectorTabs ??= new List<Type>();
                    def.inspectorTabs.Add(typeof(WITab_FactionLeadership));

                    def.inspectorTabsResolved ??= new List<InspectTabBase>();
                    def.inspectorTabsResolved.Add(InspectTabManager.GetSharedInstance(typeof(WITab_FactionLeadership)));
                }
            }
        }

        public static float SafeApproxDistanceInTiles(PlanetTile a, PlanetTile b)
        {
            if (a.Layer == b.Layer)
                return Find.WorldGrid.ApproxDistanceInTiles(a, b);
            return int.MaxValue;
        }

        public static bool HasDoctrine(this Faction faction, DoctrineDef def)
        {
            if (faction == null)
                return false;
            var data = WorldComponent_LeaderTracker.Instance.GetLeadershipDataFor(faction);
            return data?.doctrines.Any(d => d.def == def) is true;
        }

        public static List<FactionDoctrine> GetDoctrines(this Faction faction)
        {
            if (faction == null)
                return [];
            var data = WorldComponent_LeaderTracker.Instance.GetLeadershipDataFor(faction);
            return data?.doctrines ?? [];
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

        public static bool IsValidLeaderCandidate(this Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.IsPrisonerOfColony || pawn.MapHeld != null && pawn.MapHeld.IsPlayerHome)
                return false;
            return true;
        }

        public static float CalculateSpawnChance(Pawn leader, Faction faction, Settlement settlement, bool isRaidingPlayer = false)
        {
            if (leader == null || leader.Dead || leader.Spawned)
                return 0f;

            float spawnChance;
            if (isRaidingPlayer)
            {
                var baseChance = SimpleLeadershipMod.Settings.leaderSpawnChance;
                spawnChance = leader == faction.leader ? baseChance * 0.2f : baseChance;
            }
            else
            {
                var leaderTracker = WorldComponent_LeaderTracker.Instance;
                if (leader == faction.leader)
                {
                    var settlementCount = Find.WorldObjects.Settlements.Count(s => s.Faction == faction);
                    spawnChance = settlementCount > 0 ? 1f / settlementCount : 0f;
                }
                else
                {
                    var controlledBases = Find.WorldObjects.Settlements.Count(s => s.Faction == faction && leaderTracker.GetBaseLeader(s) == leader);
                    spawnChance = controlledBases > 0 ? 1f / controlledBases : 0f;
                }
            }

            if (settlement?.IsInPowerEvent(PowerEventDefOf.SL_Inspection) is true)
            {
                spawnChance += 0.3f;
            }

            return spawnChance;
        }

        public static void CheckForBaseLeaderDefeat(Pawn pawn)
        {
            if (pawn.Faction == null)
                return;

            var leaderTracker = WorldComponent_LeaderTracker.Instance;
            var leaderSettlements = leaderTracker.GetSettlementsOfBaseLeader(pawn).ToList();

            if (leaderSettlements.Count > 0 && pawn != pawn.Faction.leader)
            {
                foreach (var settlement in leaderSettlements)
                {
                    if (SimpleLeadershipMod.Settings.enableEvents)
                        leaderTracker.StartPowerEvent(PowerEventDefOf.SL_PowerStruggle, settlement);
                }
                leaderTracker.Notify_LeaderLost(pawn.Faction, pawn);
            }
        }

        public static void HandleLeaderLost(Faction faction, Pawn oldLeader, string labelKey, string bodyKey, string leaderNamedKey)
        {
            var label = labelKey.Translate(faction.Name, faction.LeaderTitle).Resolve().CapitalizeFirst();
            var body = bodyKey.Translate(faction.NameColored, faction.LeaderTitle, oldLeader.Named(leaderNamedKey)).Resolve().CapitalizeFirst();

            var leaderTracker = WorldComponent_LeaderTracker.Instance;
            var candidates = leaderTracker.GetBaseLeadersFor(faction).Where(p => p.IsValidLeaderCandidate() && p != oldLeader).ToList();

            if (candidates.Count > 0)
            {
                var actingLeader = candidates.RandomElement();
                var data = leaderTracker.GetLeadershipDataFor(faction);
                if (data != null)
                {
                    data.exLeader = oldLeader;
                    data.actingLeader = actingLeader;
                }

                var actingLeaderText = "SL_ActingLeaderChosen".Translate(faction.LeaderTitle.Named("LEADERTITLE"), actingLeader.Named("PAWN")).Resolve();
                body += "\n\n" + actingLeaderText;
            }

            if (faction.temporary is false)
            {
                Find.LetterStack.ReceiveLetter(label, body, LetterDefOf.NeutralEvent, oldLeader, faction);
            }

            if (SimpleLeadershipMod.Settings.enableEvents)
                leaderTracker.StartPowerEvent(PowerEventDefOf.SL_PowerVoid, faction);

            leaderTracker.Notify_LeaderLost(faction, oldLeader);
            faction.leader = null;
        }

        private const float InterventionTriggerChance = 0.35f;
        private const int InterventionRaidDelayMinTicks = 1000;
        private const int InterventionRaidDelayMaxTicks = 3000;

        public static void TryTriggerInterventionRaid(Map map, Faction visitingFaction)
        {
            var hostileInterventionist = Find.FactionManager.AllFactionsVisible
                .FirstOrDefault(f => f.HasDoctrine(PowerEventDefOf.SL_Intervention) && f.HostileTo(visitingFaction) && f.HostileTo(Faction.OfPlayer));
            if (hostileInterventionist == null || Rand.Chance(InterventionTriggerChance) is false)
                return;
            var raidParms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, map);
            raidParms.faction = hostileInterventionist;
            raidParms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
            Find.Storyteller.incidentQueue.Add(IncidentDefOf.RaidEnemy, Find.TickManager.TicksGame + Rand.Range(InterventionRaidDelayMinTicks, InterventionRaidDelayMaxTicks), raidParms);
            Find.LetterStack.ReceiveLetter("SL_InterventionRaidLetterLabel".Translate(), "SL_InterventionRaidLetterBody".Translate(hostileInterventionist.NameColored, visitingFaction.NameColored), LetterDefOf.ThreatBig);
        }

    }
}
