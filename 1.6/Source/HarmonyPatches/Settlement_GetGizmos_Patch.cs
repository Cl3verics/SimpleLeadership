using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace SimpleLeadership
{
    [HarmonyPatch(typeof(Settlement), nameof(Settlement.GetGizmos))]
    public static class Settlement_GetGizmos_Patch
    {
        public static void Postfix(Settlement __instance, ref IEnumerable<Gizmo> __result)
        {
            if (DebugSettings.ShowDevGizmos is false) return;

            var gizmos = new List<Gizmo>(__result);
            var tracker = WorldComponent_LeaderTracker.Instance;

            gizmos.Add(new Command_Action
            {
                defaultLabel = "DEV: Generate New Base Leader",
                action = () =>
                {
                    var data = tracker.GetLeadershipDataFor(__instance.Faction);
                    if (data == null) return;
                    var newLeader = tracker.GenerateBaseLeader(__instance.Faction);
                    if (newLeader != null)
                        tracker.ReplaceBaseLeader(__instance, newLeader);
                }
            });

            foreach (var def in DefDatabase<PowerEventDef>.AllDefs)
            {
                object target = typeof(SettlementPowerEvent).IsAssignableFrom(def.workerClass)
                    ? __instance
                    : __instance.Faction;

                var activeEvent = target.GetActiveEvents<PowerEventBase>().FirstOrDefault(e => e.def == def);

                if (activeEvent != null)
                {
                    gizmos.Add(new Command_Action
                    {
                        defaultLabel = "DEV: End " + def.label,
                        action = () => WorldComponent_LeaderTracker.Instance.EndPowerEvent(activeEvent)
                    });
                }
                else
                {
                    gizmos.Add(new Command_Action
                    {
                        defaultLabel = "DEV: Start " + def.label,
                        action = () => WorldComponent_LeaderTracker.Instance.StartPowerEvent(def, target)
                    });
                }
            }

            if (tracker.activeInvestigation != null)
            {
                gizmos.Add(new Command_Action
                {
                    defaultLabel = "DEV: Complete Investigation",
                    action = () => tracker.CompleteInvestigation()
                });
            }

            gizmos.Add(new Command_Action
            {
                defaultLabel = "DEV: Reroll Doctrines",
                action = () =>
                {
                    var data = tracker.GetLeadershipDataFor(__instance.Faction);
                    if (data == null) return;
                    tracker.GenerateDoctrinesFor(__instance.Faction, data);
                }
            });

            gizmos.Add(new Command_Action
            {
                defaultLabel = "DEV: Add Doctrine",
                action = () =>
                {
                    var data = tracker.GetLeadershipDataFor(__instance.Faction);
                    if (data == null) return;
                    var options = new List<FloatMenuOption>();
                    foreach (var def in DefDatabase<DoctrineDef>.AllDefs)
                    {
                        if (data.doctrines.Any(d => d.def == def)) continue;
                        options.Add(new FloatMenuOption(def.label, () =>
                        {
                            if (data.doctrines.Any(d => d.def.ConflictsWith(def))) return;
                            data.doctrines.Add(new FactionDoctrine
                            {
                                def = def,
                                proposedBy = __instance.Faction.leader
                            });
                        }));
                    }
                    if (options.Count > 0)
                        Find.WindowStack.Add(new FloatMenu(options));
                }
            });

            gizmos.Add(new Command_Action
            {
                defaultLabel = "DEV: Remove Doctrine",
                action = () =>
                {
                    var data = tracker.GetLeadershipDataFor(__instance.Faction);
                    if (data == null || data.doctrines.Count == 0) return;
                    var options = new List<FloatMenuOption>();
                    foreach (var doc in data.doctrines)
                    {
                        options.Add(new FloatMenuOption(doc.def.label, () =>
                        {
                            data.doctrines.Remove(doc);
                            if (tracker.activeInvestigation == doc.def)
                                tracker.CancelInvestigation();
                        }));
                    }
                    Find.WindowStack.Add(new FloatMenu(options));
                }
            });

            __result = gizmos;
        }
    }
}
