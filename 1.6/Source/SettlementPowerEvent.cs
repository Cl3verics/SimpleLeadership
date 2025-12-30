using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;

namespace SimpleLeadership
{
    public class SettlementPowerEvent : PowerEventBase
    {
        public Settlement settlement;

        public override void SetParameters(params object[] args)
        {
            if (args.Length > 0 && args[0] is Settlement s)
            {
                settlement = s;
            }
        }

        public override bool IsDuplicate(PowerEventBase other)
        {
            return other.def == def && other is SettlementPowerEvent otherSettlement && otherSettlement.settlement == settlement;
        }

        public override bool ShouldGiveMessage()
        {
            var playerSettlements = Find.WorldObjects.Settlements.Where(s => s.Faction == Faction.OfPlayer);
            if (!playerSettlements.Any()) return false;

            var closestPlayerSettlement = playerSettlements.MinBy(s => Find.WorldGrid.ApproxDistanceInTiles(s.Tile, settlement.Tile));
            return Find.WorldGrid.ApproxDistanceInTiles(closestPlayerSettlement.Tile, settlement.Tile) < 30;
        }

        protected override string GetFormattedMessage(string message)
        {
            return string.Format(message, settlement.Label);
        }

        public override bool IsTarget(object target)
        {
            return target is Settlement s && s == settlement;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref settlement, "settlement");
        }
    }
}
