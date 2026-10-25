using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace SimpleLeadership
{
    public class WorldComponent_LeaderTracker : WorldComponent
    {
        private Dictionary<Faction, FactionLeadershipData> leadershipData;
        private List<PowerEventBase> activeEvents;
        private Dictionary<object, List<PowerEventBase>> eventsByTarget = new();
        internal bool initialized;
        private List<Faction> keys = [];
        private List<FactionLeadershipData> values = [];
        private List<Faction> raidKeys = [];
        private List<int> raidValues = [];
        private List<Faction> mutualDefenseKeys = [];
        private List<int> mutualDefenseValues = [];

        private List<Settlement> settlementKeys = [];
        private List<KidnappedPrisonersList> kidnappedPrisonersValues = [];
        private List<Faction> originKeys = [];
        private List<Settlement> originSettlements = [];
        private List<PowerEventDef> randomSettlementEvents;
        public Dictionary<Faction, int> lastLeaderRaidTick = new();
        public Dictionary<Faction, Settlement> lastRaidOrigin = [];
        public Dictionary<Settlement, KidnappedPrisonersList> kidnappedPrisoners = [];
        private float MaxLeaderDistance => 60f * Mathf.Sqrt(Find.WorldGrid.TilesCount / 30000f);
        private const int MinParentAgeGapYears = 14;

        public DoctrineDef activeInvestigation;
        public Faction investigatingFaction;
        public int investigationEndTick = -1;
        public int lastPlayerRaidTick = -1;
        public Faction lastPlayerRaidVictim;
        public Dictionary<Faction, int> lastMutualDefenseRaidTick = [];
        public int lastInterventionRaidTick = -1;

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
            LongEventHandler.toExecuteWhenFinished.Add(() =>
            {
                if (initialized is false)
                {
                    InitializeLeaders();
                    initialized = true;
                }
            });
        }

        public void AssignLeaderToSettlement(Settlement settlement)
        {
            if (settlement.Faction == null || IsValidFactionForLeaders(settlement.Faction) is false)
                return;

            if (leadershipData.TryGetValue(settlement.Faction, out var data) is false)
            {
                data = new FactionLeadershipData();
                leadershipData[settlement.Faction] = data;
            }

            if (data.settlementLeaders.ContainsKey(settlement) || settlement.Tile.Valid is false)
                return;

            var basesPerLeader = SimpleLeadershipMod.Settings.basesPerLeader;
            var isOrbital = settlement.Tile.LayerDef.isSpace;
            var existingLeaders = data.settlementLeaders
                .Where(kvp => kvp.Value.IsValidLeaderCandidate())
                .Select(kvp => kvp.Value)
                .Distinct()
                .ToList();

            Pawn bestLeader = null;
            var bestDistance = float.MaxValue;

            foreach (var leader in existingLeaders)
            {
                var leaderSettlements = data.settlementLeaders
                    .Where(kvp => kvp.Value == leader)
                    .Select(kvp => kvp.Key)
                    .Where(s => s.Tile.Valid)
                    .ToList();

                if (leaderSettlements.Count >= basesPerLeader)
                    continue;

                var nearestDistance = leaderSettlements
                    .Min(s => Utils.SafeApproxDistanceInTiles(s.Tile, settlement.Tile));

                if (isOrbital is false && nearestDistance >= MaxLeaderDistance)
                    continue;

                if (nearestDistance < bestDistance)
                {
                    bestDistance = nearestDistance;
                    bestLeader = leader;
                }
            }

            bestLeader ??= GenerateBaseLeader(settlement.Faction);

            data.settlementLeaders[settlement] = bestLeader;
        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            if (Find.TickManager.TicksGame % GenDate.TicksPerDay == 0)
            {
                CleanupStaleLeaders();
            }
            randomSettlementEvents ??= DefDatabase<PowerEventDef>.AllDefs.Where(def => def.chancePerSeason > 0f && typeof(SettlementPowerEvent).IsAssignableFrom(def.workerClass)).ToList();
            for (int i = activeEvents.Count - 1; i >= 0; i--)
            {
                if (activeEvents[i].IsActive() is false)
                {
                    EndPowerEvent(activeEvents[i]);
                }
            }
            TickInvestigation();
            TryTriggerRandomEvents();
        }

        private void TryTriggerRandomEvents()
        {
            if (SimpleLeadershipMod.Settings.enableEvents is false || Find.TickManager.TicksGame % 2500 != 0) return;
            foreach (var settlement in Find.WorldObjects.Settlements)
            {
                if (IsValidFactionForLeaders(settlement.Faction) is false) continue;

                var eventDef = randomSettlementEvents.RandomElement();
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
            Scribe_References.Look(ref investigatingFaction, "investigatingFaction");
            Scribe_Defs.Look(ref activeInvestigation, "activeInvestigation");
            Scribe_Values.Look(ref investigationEndTick, "investigationEndTick", -1);
            Scribe_Values.Look(ref lastPlayerRaidTick, "lastPlayerRaidTick", -1);
            Scribe_References.Look(ref lastPlayerRaidVictim, "lastPlayerRaidVictim");
            Scribe_Collections.Look(ref lastMutualDefenseRaidTick, "lastMutualDefenseRaidTick", LookMode.Reference, LookMode.Value, ref mutualDefenseKeys, ref mutualDefenseValues);
            Scribe_Values.Look(ref lastInterventionRaidTick, "lastInterventionRaidTick", -1);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                leadershipData ??= [];
                lastLeaderRaidTick ??= new();
                lastRaidOrigin ??= new();
                kidnappedPrisoners ??= new();
                activeEvents ??= [];
                lastMutualDefenseRaidTick ??= new();
                activeEvents.RemoveAll(ev => ev.GetTarget() == null);
                eventsByTarget = new Dictionary<object, List<PowerEventBase>>();
                foreach (var ev in activeEvents)
                {
                    var target = ev.GetTarget();
                    if (target == null) continue;
                    if (eventsByTarget.TryGetValue(target, out var list) is false)
                        eventsByTarget[target] = list = new List<PowerEventBase>();
                    list.Add(ev);
                }
                foreach (var kvp in leadershipData)
                {
                    kvp.Value.doctrines ??= [];
                    kvp.Value.doctrines.RemoveAll(d => d?.def == null);
                    if (kvp.Key != null && kvp.Value.doctrinesGenerated is false)
                    {
                        GenerateDoctrinesFor(kvp.Key, kvp.Value);
                    }
                }
            }
        }

        private void InitializeLeaders()
        {
            foreach (var faction in Find.FactionManager.AllFactionsVisible)
            {
                if (IsValidFactionForLeaders(faction) is false)
                    continue;

                if (leadershipData.TryGetValue(faction, out var data) is false)
                {
                    data = new FactionLeadershipData();
                    leadershipData[faction] = data;
                }
                foreach (var key in data.settlementLeaders.Keys.Where(s => s == null).ToList())
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
                    if (data.settlementLeaders.ContainsKey(settlement) is false)
                    {
                        AssignLeaderToSettlement(settlement);
                    }
                }
                if (data.doctrinesGenerated is false)
                {
                    GenerateDoctrinesFor(faction, data);
                }
            }
        }

        private bool IsValidFactionForLeaders(Faction faction)
        {
            return faction != null && faction.def.humanlikeFaction && faction.IsPlayer is false && faction.Hidden is false && faction.def.pawnGroupMakers != null
                && SimpleLeadershipMod.Settings.factionBlacklist.Contains(faction.def.defName) is false;
        }

        public Pawn GenerateBaseLeader(Faction faction)
        {
            var leaderKind = faction.RandomPawnKind();
            if (leaderKind == null) return null;
            try
            {
                var request = new PawnGenerationRequest(leaderKind, faction, PawnGenerationContext.NonPlayer, forceGenerateNewPawn: true);
                var newLeader = PawnGenerator.GeneratePawn(request);

                if (newLeader != null && Find.WorldPawns.Contains(newLeader) is false)
                {
                    Find.WorldPawns.PassToWorld(newLeader, PawnDiscardDecideMode.KeepForever);
                    newLeader.guest.Recruitable = false;
                    if (faction.HasDoctrine(PowerEventDefOf.SL_FamilialSuccession))
                    {
                        EnsureFamilialRelation(faction.leader, newLeader);
                    }
                }

                return newLeader;
            }
            catch (Exception ex)
            {
                Log.Error($"[SimpleLeadership] Failed to generate base leader for {faction.Name}: {ex.Message}");
                return null;
            }
        }

        public void EnsureFamilialRelation(Pawn factionLeader, Pawn baseLeader)
        {
            if (factionLeader == null || baseLeader == null || factionLeader == baseLeader || factionLeader.relations.FamilyByBlood.Contains(baseLeader))
                return;
            var ageGap = factionLeader.ageTracker.AgeBiologicalYears - baseLeader.ageTracker.AgeBiologicalYears;
            if (Mathf.Abs(ageGap) >= MinParentAgeGapYears)
            {
                var elder = ageGap >= 0 ? factionLeader : baseLeader;
                var younger = elder == factionLeader ? baseLeader : factionLeader;
                younger.relations.AddDirectRelation(PawnRelationDefOf.Parent, elder);
            }
            else
            {
                var sharedParent = factionLeader.relations.GetFirstDirectRelationPawn(PawnRelationDefOf.Parent);
                if (sharedParent == null)
                {
                    var request = new PawnGenerationRequest(factionLeader.kindDef, factionLeader.Faction, PawnGenerationContext.NonPlayer, forceGenerateNewPawn: true, forceDead: true, canGeneratePawnRelations: false, fixedBiologicalAge: Mathf.Max(factionLeader.ageTracker.AgeBiologicalYears, baseLeader.ageTracker.AgeBiologicalYears) + 25f);
                    sharedParent = PawnGenerator.GeneratePawn(request);
                    Find.WorldPawns.PassToWorld(sharedParent, PawnDiscardDecideMode.KeepForever);
                    factionLeader.relations.AddDirectRelation(PawnRelationDefOf.Parent, sharedParent);
                }

                baseLeader.relations.AddDirectRelation(PawnRelationDefOf.Parent, sharedParent);
            }
        }

        public Pawn GetBaseLeader(Settlement settlement)
        {
            if (settlement?.Faction == null)
                return null;

            if (leadershipData.TryGetValue(settlement.Faction, out var data) && data.settlementLeaders.TryGetValue(settlement, out var leader))
            {
                return leader;
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
                var targetSettlement = args.OfType<Settlement>().FirstOrDefault();
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
                var targetFaction = args.OfType<Faction>().FirstOrDefault();
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
            if (def == PowerEventDefOf.SL_PowerVoid || def == PowerEventDefOf.SL_PowerStruggle)
            {
                var evTarget = newEvent.GetTarget();
                if (evTarget is Faction f && f.HasDoctrine(PowerEventDefOf.SL_LeadersLegacy) || evTarget is Settlement s && s.Faction != null && s.Faction.HasDoctrine(PowerEventDefOf.SL_LeadersLegacy))
                {
                    newEvent.ReduceDuration(SimpleLeadershipMod.Settings.leaderLegacyDurationMultiplier);
                }
            }
            newEvent.OnStart();
            activeEvents.Add(newEvent);

            var target = newEvent.GetTarget();
            if (target != null)
            {
                if (eventsByTarget.TryGetValue(target, out var list) is false)
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

        public bool IsInPowerEvent<T>(object target) where T : PowerEventBase => GetActiveEventsFor(target).OfType<T>().Any();

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

        public void GenerateDoctrinesFor(Faction faction, FactionLeadershipData data)
        {
            if (SimpleLeadershipMod.Settings.enableDoctrines is false)
                return;
            var allLeaders = new List<Pawn>();
            if (faction.leader != null) allLeaders.Add(faction.leader);
            allLeaders.AddRange(data.settlementLeaders.Values.Where(p => p != null).Distinct());
            if (allLeaders.Count == 0)
                return;

            data.doctrinesGenerated = true;
            data.doctrines ??= [];
            data.doctrines.Clear();

            var count = Rand.RangeInclusive(0, SimpleLeadershipMod.Settings.maxDoctrinesPerFaction);
            if (count == 0)
                return;

            var candidateDefs = DefDatabase<DoctrineDef>.AllDefs
                .Where(d => d.minTechLevel == TechLevel.Undefined || faction.def.techLevel >= d.minTechLevel)
                .ToList();

            for (int i = 0; i < count; i++)
            {
                var available = candidateDefs.Where(c => data.doctrines.Any(existing => existing.def.ConflictsWith(c)) is false).ToList();
                if (available.Count == 0)
                    break;
                var chosen = available.RandomElement();
                candidateDefs.Remove(chosen);
                data.doctrines.Add(new FactionDoctrine
                {
                    def = chosen,
                    proposedBy = allLeaders.RandomElement(),
                    isInvestigated = false
                });
            }
            if (data.doctrines.Any(d => d.def == PowerEventDefOf.SL_FamilialSuccession))
            {
                foreach (var baseLeader in data.settlementLeaders.Values.Distinct())
                {
                    EnsureFamilialRelation(faction.leader, baseLeader);
                }
            }
        }

        public void StartInvestigation(Faction faction, FactionDoctrine doctrine)
        {
            if (SimpleLeadershipMod.Settings.enableDoctrines is false)
                return;
            investigatingFaction = faction;
            activeInvestigation = doctrine.def;
            investigationEndTick = Find.TickManager.TicksGame + SimpleLeadershipMod.Settings.investigationDurationDays * GenDate.TicksPerDay;
        }

        public void CancelInvestigation()
        {
            investigatingFaction = null;
            activeInvestigation = null;
            investigationEndTick = -1;
        }

        public void CompleteInvestigation() => investigationEndTick = Find.TickManager.TicksGame;

        public void JumpToCurrentInvestigation()
        {
            if (investigatingFaction == null)
                return;
            var targetSettlement = Find.WorldObjects.Settlements.FirstOrDefault(s => s.Faction == investigatingFaction);
            if (targetSettlement != null)
            {
                CameraJumper.TryJumpAndSelect(targetSettlement);
            }
        }

        public bool IsInvestigationActive(Faction faction, FactionDoctrine doctrine) => activeInvestigation == doctrine.def && investigatingFaction == faction;

        private FactionDoctrine ResolveActiveDoctrine() => GetLeadershipDataFor(investigatingFaction)?.doctrines.FirstOrDefault(d => d.def == activeInvestigation);

        private void TickInvestigation()
        {
            if (SimpleLeadershipMod.Settings.enableDoctrines is false)
            {
                if (activeInvestigation != null)
                    CancelInvestigation();
            }
            else
            {
                if (activeInvestigation == null || investigationEndTick < 0)
                    return;
                if (Find.TickManager.TicksGame >= investigationEndTick)
                {
                    var faction = investigatingFaction;
                    var doctrine = ResolveActiveDoctrine();
                    CancelInvestigation();
                    if (doctrine == null)
                        return;
                    doctrine.isInvestigated = true;
                    var text = "SL_InvestigationCompletedDesc".Translate(doctrine.def.label, faction.NameColored, doctrine.proposedBy != null ? doctrine.proposedBy.LabelShortCap : "SL_NotAvailable".Translate().ToString());
                    var homeSettlement = Find.WorldObjects.Settlements.FirstOrDefault(s => s.Faction == faction);
                    if (homeSettlement != null)
                        Find.LetterStack.ReceiveLetter("SL_InvestigationCompletedLabel".Translate(), text, LetterDefOf.PositiveEvent, homeSettlement, faction);
                    else
                        Find.LetterStack.ReceiveLetter("SL_InvestigationCompletedLabel".Translate(), text, LetterDefOf.PositiveEvent);
                }
            }
        }

        public void ReplaceBaseLeader(Settlement settlement, Pawn newLeader)
        {
            var faction = settlement.Faction;
            var data = GetLeadershipDataFor(faction);
            if (data == null)
                return;
            var oldLeader = data.settlementLeaders.GetValueOrDefault(settlement);
            data.settlementLeaders[settlement] = newLeader;
            if (oldLeader != null && oldLeader != newLeader && oldLeader != faction.leader && data.exBaseLeaders.Contains(oldLeader) is false)
                data.exBaseLeaders.Add(oldLeader);
            if (oldLeader != null && oldLeader != newLeader && data.settlementLeaders.ContainsValue(oldLeader) is false && oldLeader != faction.leader)
                Notify_LeaderLost(faction, oldLeader);
        }

        public void Notify_LeaderLost(Faction faction, Pawn lostLeader)
        {
            if (SimpleLeadershipMod.Settings.enableDoctrines is false || faction == null || lostLeader == null)
                return;
            var data = GetLeadershipDataFor(faction);
            if (data == null)
                return;

            var removed = data.doctrines.Where(d => d.proposedBy == lostLeader).ToList();
            if (removed.Count == 0)
                return;

            foreach (var doc in removed)
            {
                data.doctrines.Remove(doc);
                if (activeInvestigation == doc.def)
                    CancelInvestigation();
            }
            var doctrineList = string.Join(", ", removed.Select(d => d.def.label));
            Messages.Message("SL_LeaderDoctrinesLost".Translate(lostLeader.LabelShortCap, faction.NameColored, doctrineList), MessageTypeDefOf.NeutralEvent);
        }

        private void CleanupStaleLeaders()
        {
            List<Faction> factionsToRemove = null;
            foreach (var kvp in leadershipData)
            {
                kvp.Value.exBaseLeaders.RemoveAll(p => p == null || p.Dead);
                RemoveStaleSettlements(kvp.Key, kvp.Value);
                if (kvp.Key == null || kvp.Key.defeated)
                {
                    if (kvp.Key != null && investigatingFaction == kvp.Key)
                        CancelInvestigation();
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
            lastMutualDefenseRaidTick.RemoveAll(kvp => kvp.Key == null || kvp.Key.defeated);
        }

        private void RemoveStaleSettlements(Faction faction, FactionLeadershipData data)
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
            var removedLeaders = new List<Pawn>();
            foreach (var settlement in stale)
            {
                removedLeaders.Add(data.settlementLeaders[settlement]);
                data.settlementLeaders.Remove(settlement);
            }
            foreach (var leader in removedLeaders.Distinct())
            {
                if (leader == null || leader == faction.leader)
                    continue;
                if (data.exBaseLeaders.Contains(leader) is false)
                    data.exBaseLeaders.Add(leader);
                if (data.settlementLeaders.ContainsValue(leader) is false)
                    Notify_LeaderLost(faction, leader);
            }
        }

        private bool IsStaleAssignment(Settlement settlement, Pawn leader)
        {
            if (settlement == null || settlement.Destroyed || settlement.Faction == null || settlement.Faction.defeated)
                return true;
            return leader == null || leader.Dead || leader.Destroyed;
        }

        private bool IsTrackedLeaderOrRelative(Pawn pawn)
        {
            foreach (var kvp in leadershipData)
            {
                var faction = kvp.Key;
                var data = kvp.Value;
                if (faction == null || data == null)
                    continue;
                if (data.exLeader == pawn || data.actingLeader == pawn || data.exBaseLeaders.Contains(pawn) || faction.leader != null && (faction.leader == pawn || faction.leader.relations.FamilyByBlood.Contains(pawn)) || data.actingLeader != null && data.actingLeader.relations.FamilyByBlood.Contains(pawn))
                    return true;
                foreach (var leader in data.settlementLeaders.Values)
                {
                    if (leader != null && (leader == pawn || leader.relations.FamilyByBlood.Contains(pawn)))
                        return true;
                }
            }
            return false;
        }

        private void UnpinUnreferencedLeaders()
        {
            List<Pawn> unpinned = null;
            foreach (var pawn in Find.WorldPawns.ForcefullyKeptPawns)
            {
                if (GetSettlementsOfBaseLeader(pawn).Any() is false && PawnUtility.IsFactionLeader(pawn) is false && IsTrackedLeaderOrRelative(pawn) is false)
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
