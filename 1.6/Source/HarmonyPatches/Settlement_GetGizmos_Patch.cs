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

                gizmos.Add(new Command_Action
                {
                    defaultLabel = "DEV: Start Power Struggle",
                    action = () =>
                    {
                        WorldComponent_LeaderTracker.Instance.StartPowerEvent(PowerEventDefOf.SL_PowerStruggle, __instance);
                    }
                });

                gizmos.Add(new Command_Action
                {
                    defaultLabel = "DEV: Start Power Void",
                    action = () =>
                    {
                        WorldComponent_LeaderTracker.Instance.StartPowerEvent(PowerEventDefOf.SL_PowerVoid, __instance.Faction);
                    }
                });

                if (__instance.IsInPowerEvent<PowerStruggle>())
                {
                    gizmos.Add(new Command_Action
                    {
                        defaultLabel = "DEV: End Power Struggle",
                        action = () =>
                        {
                            var ev = __instance.GetActiveEvents<PowerStruggle>().FirstOrDefault();
                            if (ev != null) WorldComponent_LeaderTracker.Instance.EndPowerEvent(ev);
                        }
                    });
                }

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
