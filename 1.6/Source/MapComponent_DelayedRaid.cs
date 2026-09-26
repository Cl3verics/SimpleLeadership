using RimWorld;
using Verse;

namespace SimpleLeadership
{
    public class MapComponent_DelayedRaid : MapComponent
    {
        private int fireTick = -1;
        private IncidentParms parms;
        public int hitAndRunLockoutTick = -1;

        public bool IsScheduled => fireTick >= 0;

        public MapComponent_DelayedRaid(Map map) : base(map) { }
        public void Schedule(IncidentParms parms, int delayTicks)
        {
            this.parms = parms;
            fireTick = Find.TickManager.TicksGame + delayTicks;
        }

        public override void MapComponentTick()
        {
            if (fireTick < 0 || Find.TickManager.TicksGame < fireTick) return;
            IncidentDefOf.RaidEnemy.Worker.TryExecute(parms);
            fireTick = -1;
            parms = null;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref fireTick, "fireTick", -1);
            Scribe_Deep.Look(ref parms, "parms");
            Scribe_Values.Look(ref hitAndRunLockoutTick, "hitAndRunLockoutTick", -1);
        }
    }
}
