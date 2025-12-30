using System;
using System.Text;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace SimpleLeadership
{
    [HarmonyPatch(typeof(CaravanVisibilityCalculator), "Visibility", new Type[] { typeof(Caravan), typeof(StringBuilder) })]
    public static class CaravanVisibilityCalculator_Visibility_Patch
    {
        public static void Postfix(Caravan caravan, StringBuilder explanation, ref float __result)
        {
            if (caravan == null)
                return;

            foreach (Settlement settlement in Find.WorldObjects.Settlements)
            {
                if (settlement.Faction == null || settlement.Faction.IsPlayer)
                    continue;

                if (settlement.IsInPowerEvent(PowerEventDefOf.SL_Vigilant))
                {
                    float distance = Find.WorldGrid.ApproxDistanceInTiles(caravan.Tile, settlement.Tile);
                    if (distance <= 10f)
                    {
                        float multiplier = 1f + (10f - distance) * 0.1f;
                        __result *= multiplier;
                        if (explanation != null)
                        {
                            explanation.AppendLine();
                            explanation.Append("SL_VigilantBonus".Translate(settlement.Label) + ": " + multiplier.ToStringPercent());
                        }
                    }
                }
            }
        }
    }
}
