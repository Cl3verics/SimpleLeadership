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
            if (DebugSettings.ShowDevGizmos)
            {
                var gizmos = new List<Gizmo>(__result);

                foreach (var def in DefDatabase<PowerEventDef>.AllDefs)
                {
                    if (typeof(SettlementPowerEvent).IsAssignableFrom(def.workerClass))
                    {
                        var activeEvent = __instance.GetActiveEvents<SettlementPowerEvent>().FirstOrDefault(e => e.def == def);

                        if (activeEvent != null)
                        {
                            gizmos.Add(new Command_Action
                            {
                                defaultLabel = "DEV: End " + def.label,
                                action = () =>
                                {
                                    WorldComponent_LeaderTracker.Instance.EndPowerEvent(activeEvent);
                                }
                            });
                        }
                        else
                        {
                            gizmos.Add(new Command_Action
                            {
                                defaultLabel = "DEV: Start " + def.label,
                                action = () =>
                                {
                                    var activeEvents = __instance.GetActiveEvents<SettlementPowerEvent>().ToList();
                                    foreach (var ev in activeEvents)
                                    {
                                        if (ev.def != PowerEventDefOf.SL_PowerStruggle)
                                        {
                                            WorldComponent_LeaderTracker.Instance.EndPowerEvent(ev);
                                        }
                                    }
                                    WorldComponent_LeaderTracker.Instance.StartPowerEvent(def, __instance);
                                }
                            });
                        }
                    }
                }

                gizmos.Add(new Command_Action
                {
                    defaultLabel = "DEV: Start Power Void",
                    action = () =>
                    {
                        WorldComponent_LeaderTracker.Instance.StartPowerEvent(PowerEventDefOf.SL_PowerVoid, __instance.Faction);
                    }
                });

                if (__instance.Faction.IsInPowerEvent<PowerVoid>())
                {
                    gizmos.Add(new Command_Action
                    {
                        defaultLabel = "DEV: End Power Void",
                        action = () =>
                        {
                            var ev = __instance.Faction.GetActiveEvents<PowerVoid>().FirstOrDefault();
                            if (ev != null) WorldComponent_LeaderTracker.Instance.EndPowerEvent(ev);
                        }
                    });
                }

                __result = gizmos;
            }
        }
    }
}
