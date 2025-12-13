using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;

namespace SimpleLeadership
{
    public class WorldComponent_LeaderTracker : WorldComponent
    {
        private Dictionary<Faction, FactionLeadershipData> leadershipData;
        private List<PowerEventBase> activeEvents;
        private bool initialized = false;
        private List<Faction> keys = [];
        private List<FactionLeadershipData> values = [];

        public static WorldComponent_LeaderTracker Instance => Find.World.GetComponent<WorldComponent_LeaderTracker>();

        public WorldComponent_LeaderTracker(World world) : base(world)
        {
            leadershipData = [];
            activeEvents = [];
        }

        public override void FinalizeInit(bool fromLoad)
        {
            base.FinalizeInit(fromLoad);
            if (!initialized)
            {
                InitializeLeaders();
                initialized = true;
            }
        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();

            for (int i = activeEvents.Count - 1; i >= 0; i--)
            {
                if (!activeEvents[i].IsActive())
                {
                    EndPowerEvent(activeEvents[i]);
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref leadershipData, "baseLeaderData", LookMode.Reference, LookMode.Deep, ref keys, ref values);
            Scribe_Collections.Look(ref activeEvents, "activeEvents", LookMode.Deep);
            Scribe_Values.Look(ref initialized, "initialized", defaultValue: false);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                leadershipData ??= [];
                activeEvents ??= [];
            }
        }

        private void InitializeLeaders()
        {
            foreach (Faction faction in Find.FactionManager.AllFactionsVisible)
            {
                if (IsValidFactionForLeaders(faction))
                {
                    if (!leadershipData.TryGetValue(faction, out var data))
                    {
                        data = new FactionLeadershipData();
                        leadershipData[faction] = data;
                    }

                    foreach (Settlement settlement in Find.WorldObjects.Settlements)
                    {
                        if (settlement.Faction == faction)
                        {
                            if (!data.settlementLeaders.ContainsKey(settlement) || data.settlementLeaders[settlement] == null)
                            {
                                Pawn newLeader = GenerateBaseLeader(faction);
                                data.settlementLeaders[settlement] = newLeader;
                            }
                        }
                    }

                    if (faction.leader == null || faction.leader.Dead)
                    {
                        if (data.settlementLeaders.Values.Any())
                        {
                            faction.leader = data.settlementLeaders.Values.RandomElement();
                        }
                    }
                }
            }
        }

        private bool IsValidFactionForLeaders(Faction faction)
        {
            return faction != null && faction.def.humanlikeFaction && !faction.IsPlayer && !faction.Hidden && faction.def.pawnGroupMakers != null;
        }

        public Pawn GenerateBaseLeader(Faction faction)
        {
            PawnKindDef leaderKind = faction.RandomPawnKind();
            PawnGenerationRequest request = new PawnGenerationRequest(leaderKind, faction, PawnGenerationContext.NonPlayer, forceGenerateNewPawn: true);
            Pawn newLeader = PawnGenerator.GeneratePawn(request);

            if (newLeader != null && !Find.WorldPawns.Contains(newLeader))
            {
                Find.WorldPawns.PassToWorld(newLeader, PawnDiscardDecideMode.KeepForever);
            }

            return newLeader;
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

        public Settlement GetSettlementOfBaseLeader(Pawn pawn)
        {
            foreach (var factionEntry in leadershipData)
            {
                foreach (var settlementEntry in factionEntry.Value.settlementLeaders)
                {
                    if (settlementEntry.Value == pawn)
                    {
                        return settlementEntry.Key;
                    }
                }
            }
            return null;
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
                leaders.AddRange(data.settlementLeaders.Values);
            }
            return leaders;
        }

        public void StartPowerEvent(PowerEventDef def, params object[] args)
        {
            var newEvent = (PowerEventBase)Activator.CreateInstance(def.workerClass);
            newEvent.Initialize(def, args);
            if (activeEvents.Any(e => e.IsDuplicate(newEvent))) return;
            newEvent.OnStart();
            activeEvents.Add(newEvent);
        }

        public List<PowerEventBase> GetActiveEventsFor(object target)
        {
            return activeEvents.Where(ev => ev.IsTarget(target)).ToList();
        }

        public bool IsInPowerEvent<T>(object target) where T : PowerEventBase
        {
            return GetActiveEventsFor(target).OfType<T>().Any();
        }

        public void EndPowerEvent(PowerEventBase eventToEnd)
        {
            eventToEnd.OnResolve();
            activeEvents.Remove(eventToEnd);
        }

    }
}
