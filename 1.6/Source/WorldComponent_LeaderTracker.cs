using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;

namespace SimpleLeadership
{
    public class WorldComponent_LeaderTracker : WorldComponent
    {
        private Dictionary<Faction, FactionLeadershipData> leadershipData;
        private List<PowerEventBase> activeEvents;
        private Dictionary<object, List<PowerEventBase>> eventsByTarget = new();
        internal bool initialized = false;
        private List<Faction> keys = [];
        private List<FactionLeadershipData> values = [];
        private List<Faction> raidKeys = [];
        private List<int> raidValues = [];

        private List<Settlement> settlementKeys = [];
        private List<KidnappedPrisonersList> kidnappedPrisonersValues = [];
        private List<Faction> originKeys = [];
        private List<Settlement> originSettlements = [];
        private List<PowerEventDef> randomSettlementEvents;
        public Dictionary<Faction, int> lastLeaderRaidTick = new Dictionary<Faction, int>();
        public Dictionary<Faction, Settlement> lastRaidOrigin = [];
        public Dictionary<Settlement, KidnappedPrisonersList> kidnappedPrisoners = [];
        private float MaxLeaderDistance => 60f * Mathf.Sqrt(Find.WorldGrid.TilesCount / 30000f);

        public static WorldComponent_LeaderTracker Instance;

        public WorldComponent_LeaderTracker(World world) : base(world)
        {
            leadershipData = [];
            activeEvents = [];
            Instance = this;
        }

        public override void FinalizeInit(bool fromLoad)
        {
            base.FinalizeInit(fromLoad);
            LongEventHandler.toExecuteWhenFinished.Add(delegate
            {
                if (!initialized)
                {
                    InitializeLeaders();
                    initialized = true;
                }
            });
        }

        public void AssignLeaderToSettlement(Settlement settlement)
        {
            if (settlement?.Faction == null || !IsValidFactionForLeaders(settlement.Faction))
                return;

            if (!leadershipData.TryGetValue(settlement.Faction, out var data))
            {
                data = new FactionLeadershipData();
                leadershipData[settlement.Faction] = data;
            }

            if (data.settlementLeaders.ContainsKey(settlement))
                return;

            if (!settlement.Tile.Valid)
                return;

            int basesPerLeader = SimpleLeadershipMod.Settings.basesPerLeader;
            bool isOrbital = settlement.Tile.LayerDef.isSpace;
            var existingLeaders = data.settlementLeaders
                .Where(kvp => kvp.Value.IsValidLeaderCandidate())
                .Select(kvp => kvp.Value)
                .Distinct()
                .ToList();

            Pawn bestLeader = null;
            float bestDistance = float.MaxValue;

            foreach (var leader in existingLeaders)
            {
                var leaderSettlements = data.settlementLeaders
                    .Where(kvp => kvp.Value == leader)
                    .Select(kvp => kvp.Key)
                    .Where(s => s.Tile.Valid)
                    .ToList();

                if (leaderSettlements.Count >= basesPerLeader)
                    continue;

                float nearestDistance = leaderSettlements
                    .Min(s => Utils.SafeApproxDistanceInTiles(s.Tile, settlement.Tile));

                if (!isOrbital && nearestDistance >= MaxLeaderDistance)
                    continue;

                if (nearestDistance < bestDistance)
                {
                    bestDistance = nearestDistance;
                    bestLeader = leader;
                }
            }

            if (bestLeader == null)
            {
                bestLeader = GenerateBaseLeader(settlement.Faction);
            }

            data.settlementLeaders[settlement] = bestLeader;
        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            if (Find.TickManager.TicksGame % GenDate.TicksPerDay == 0)
            {
                CleanupStaleLeaders();
            }
            if (randomSettlementEvents == null)
            {
                randomSettlementEvents = DefDatabase<PowerEventDef>.AllDefs.Where(def => def.chancePerSeason > 0f && typeof(SettlementPowerEvent).IsAssignableFrom(def.workerClass)).ToList();
            }
            for (int i = activeEvents.Count - 1; i >= 0; i--)
            {
                if (!activeEvents[i].IsActive())
                {
                    EndPowerEvent(activeEvents[i]);
                }
            }
            TryTriggerRandomEvents();
        }

        private void TryTriggerRandomEvents()
        {
            if (!SimpleLeadershipMod.Settings.enableEvents) return;
            if (Find.TickManager.TicksGame % 2500 != 0) return;
            foreach (Settlement settlement in Find.WorldObjects.Settlements)
            {
                if (!IsValidFactionForLeaders(settlement.Faction)) continue;

                PowerEventDef eventDef = randomSettlementEvents.RandomElement();
                if (Rand.MTBEventOccurs(15f / eventDef.chancePerSeason, 60000f, 2500f))
                {
                    var activeEvent = GetActiveEventsFor(settlement).OfType<SettlementPowerEvent>().FirstOrDefault();
                    if (activeEvent != null)
                    {
                        EndPowerEvent(activeEvent);
                    }
                    StartPowerEvent(eventDef, settlement);
                }
            }
        }

        public override void ExposeData()
        {
            Instance = this;
            base.ExposeData();
            Scribe_Collections.Look(ref leadershipData, "baseLeaderData", LookMode.Reference, LookMode.Deep, ref keys, ref values);
            Scribe_Collections.Look(ref activeEvents, "activeEvents", LookMode.Deep);
            Scribe_Values.Look(ref initialized, "initialized", defaultValue: false);

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                lastLeaderRaidTick.RemoveAll(x => x.Key == null);
            }
            Scribe_Collections.Look(ref lastLeaderRaidTick, "lastLeaderRaidTick", LookMode.Reference, LookMode.Value, ref raidKeys, ref raidValues);
            Scribe_Collections.Look(ref lastRaidOrigin, "lastRaidOrigin", LookMode.Reference, LookMode.Reference, ref originKeys, ref originSettlements);
            Scribe_Collections.Look(ref kidnappedPrisoners, "kidnappedPrisoners", LookMode.Reference, LookMode.Deep, ref settlementKeys, ref kidnappedPrisonersValues);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                leadershipData ??= [];
                lastLeaderRaidTick ??= new Dictionary<Faction, int>();
                lastRaidOrigin ??= new Dictionary<Faction, Settlement>();
                kidnappedPrisoners ??= new Dictionary<Settlement, KidnappedPrisonersList>();
                activeEvents ??= [];
                activeEvents.RemoveAll(ev => ev.GetTarget() == null);
                eventsByTarget = new Dictionary<object, List<PowerEventBase>>();
                foreach (var ev in activeEvents)
                {
                    var target = ev.GetTarget();
                    if (target == null) continue;
                    if (!eventsByTarget.TryGetValue(target, out var list))
                        eventsByTarget[target] = list = new List<PowerEventBase>();
                    list.Add(ev);
                }
            }
        }

        private void InitializeLeaders()
        {
            foreach (Faction faction in Find.FactionManager.AllFactionsVisible)
            {
                if (!IsValidFactionForLeaders(faction))
                    continue;

                if (!leadershipData.TryGetValue(faction, out var data))
                {
                    data = new FactionLeadershipData();
                    leadershipData[faction] = data;
                }

                var keysToRemove = data.settlementLeaders.Keys.Where(s => s == null).ToList();
                foreach (var key in keysToRemove)
                {
                    data.settlementLeaders.Remove(key);
                }

                var factionSettlements = Find.WorldObjects.Settlements
                    .Where(s => s.Faction == faction && s.Tile.Valid)
                    .OrderBy(s => Find.WorldGrid.GetTileCenter(s.Tile).x)
                    .ThenBy(s => Find.WorldGrid.GetTileCenter(s.Tile).z)
                    .ToList();

                foreach (var settlement in factionSettlements)
                {
                    if (!data.settlementLeaders.ContainsKey(settlement))
                    {
                        AssignLeaderToSettlement(settlement);
                    }
                }
            }
        }

        private bool IsValidFactionForLeaders(Faction faction)
        {
            return faction != null && faction.def.humanlikeFaction && !faction.IsPlayer && !faction.Hidden && faction.def.pawnGroupMakers != null
                && !SimpleLeadershipMod.Settings.factionBlacklist.Contains(faction.def.defName);
        }

        public Pawn GenerateBaseLeader(Faction faction)
        {
            PawnKindDef leaderKind = faction.RandomPawnKind();
            if (leaderKind == null) return null;
            try
            {
                PawnGenerationRequest request = new PawnGenerationRequest(leaderKind, faction, PawnGenerationContext.NonPlayer, forceGenerateNewPawn: true);
                Pawn newLeader = PawnGenerator.GeneratePawn(request);

                if (newLeader != null && !Find.WorldPawns.Contains(newLeader))
                {
                    Find.WorldPawns.PassToWorld(newLeader, PawnDiscardDecideMode.KeepForever);
                    newLeader.guest.Recruitable = false;
                }

                return newLeader;
            }
            catch (Exception ex)
            {
                Log.Error($"[SimpleLeadership] Failed to generate base leader for {faction.Name}: {ex.Message}");
                return null;
            }
        }

        public Pawn GetBaseLeader(Settlement settlement)
        {
            if (settlement?.Faction == null)
                return null;

            if (leadershipData.TryGetValue(settlement.Faction, out var data))
            {
                if (data.settlementLeaders.TryGetValue(settlement, out var leader))
                {
                    return leader;
                }
            }
            return null;
        }

        public IEnumerable<Settlement> GetSettlementsOfBaseLeader(Pawn pawn)
        {
            foreach (var factionEntry in leadershipData)
            {
                foreach (var settlementEntry in factionEntry.Value.settlementLeaders)
                {
                    if (settlementEntry.Value == pawn)
                    {
                        yield return settlementEntry.Key;
                    }
                }
            }
        }

        public FactionLeadershipData GetLeadershipDataFor(Faction faction)
        {
            leadershipData.TryGetValue(faction, out var data);
            return data;
        }

        public List<Pawn> GetBaseLeadersFor(Faction faction)
        {
            List<Pawn> leaders = [];
            if (leadershipData.TryGetValue(faction, out var data))
            {
                leaders.AddRange(data.settlementLeaders.Values.Distinct());
            }
            return leaders;
        }

        public void StartPowerEvent(PowerEventDef def, params object[] args)
        {
            if (def == PowerEventDefOf.SL_PowerStruggle)
            {
                Settlement targetSettlement = args.OfType<Settlement>().FirstOrDefault();
                if (targetSettlement != null)
                {
                    for (int i = activeEvents.Count - 1; i >= 0; i--)
                    {
                        if (activeEvents[i] is SettlementPowerEvent && activeEvents[i].IsTarget(targetSettlement))
                            EndPowerEvent(activeEvents[i]);
                    }
                }
            }
            else if (def == PowerEventDefOf.SL_PowerVoid)
            {
                Faction targetFaction = args.OfType<Faction>().FirstOrDefault();
                if (targetFaction != null)
                {
                    for (int i = activeEvents.Count - 1; i >= 0; i--)
                    {
                        if (activeEvents[i].IsTarget(targetFaction))
                            EndPowerEvent(activeEvents[i]);
                    }
                }
            }

            var newEvent = (PowerEventBase)Activator.CreateInstance(def.workerClass);
            if (activeEvents.Any(e => e.IsDuplicate(newEvent))) return;
            newEvent.Initialize(def, args);
            newEvent.OnStart();
            activeEvents.Add(newEvent);

            var target = newEvent.GetTarget();
            if (target != null)
            {
                if (!eventsByTarget.TryGetValue(target, out var list))
                    eventsByTarget[target] = list = new List<PowerEventBase>();
                list.Add(newEvent);
            }
        }

        public List<PowerEventBase> GetActiveEventsFor(object target)
        {
            if (target == null) return new List<PowerEventBase>();
            return eventsByTarget.TryGetValue(target, out var list)
                ? list
                : new List<PowerEventBase>();
        }

        public bool IsInPowerEvent<T>(object target) where T : PowerEventBase
        {
            return GetActiveEventsFor(target).OfType<T>().Any();
        }

        public void EndPowerEvent(PowerEventBase eventToEnd)
        {
            var target = eventToEnd.GetTarget();
            if (target != null && eventsByTarget.TryGetValue(target, out var list))
            {
                list.Remove(eventToEnd);
                if (list.Count == 0) eventsByTarget.Remove(target);
            }
            eventToEnd.OnResolve();
            activeEvents.Remove(eventToEnd);
        }

        private void CleanupStaleLeaders()
        {
            List<Faction> factionsToRemove = null;
            foreach (var kvp in leadershipData)
            {
                RemoveStaleSettlements(kvp.Value);
                if (kvp.Key == null || kvp.Key.defeated)
                {
                    factionsToRemove ??= [];
                    factionsToRemove.Add(kvp.Key);
                }
            }
            UnpinUnreferencedLeaders();
            if (factionsToRemove != null)
            {
                foreach (var faction in factionsToRemove)
                {
                    leadershipData.Remove(faction);
                }
            }
            lastRaidOrigin.RemoveAll(kvp => kvp.Key == null || kvp.Key.defeated || kvp.Value == null || kvp.Value.Destroyed);
            lastLeaderRaidTick.RemoveAll(kvp => kvp.Key == null || kvp.Key.defeated);
            kidnappedPrisoners.RemoveAll(kvp => kvp.Key == null || kvp.Key.Destroyed);
        }

        private void RemoveStaleSettlements(FactionLeadershipData data)
        {
            List<Settlement> stale = null;
            foreach (var kvp in data.settlementLeaders)
            {
                if (IsStaleAssignment(kvp.Key, kvp.Value))
                {
                    stale ??= [];
                    stale.Add(kvp.Key);
                }
            }
            if (stale == null)
                return;
            foreach (var settlement in stale)
            {
                data.settlementLeaders.Remove(settlement);
            }
        }

        private bool IsStaleAssignment(Settlement settlement, Pawn leader)
        {
            if (settlement == null || settlement.Destroyed || settlement.Faction == null || settlement.Faction.defeated)
                return true;
            return leader == null || leader.Dead || leader.Destroyed;
        }

        private void UnpinUnreferencedLeaders()
        {
            List<Pawn> unpinned = null;
            foreach (var pawn in Find.WorldPawns.ForcefullyKeptPawns)
            {
                if (!GetSettlementsOfBaseLeader(pawn).Any() && !PawnUtility.IsFactionLeader(pawn))
                {
                    unpinned ??= [];
                    unpinned.Add(pawn);
                }
            }
            if (unpinned == null)
                return;
            foreach (var pawn in unpinned)
            {
                Find.WorldPawns.ForcefullyKeptPawns.Remove(pawn);
            }
        }
    }
}
