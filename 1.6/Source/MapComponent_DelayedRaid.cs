using System.Collections.Generic;
using RimWorld;
using Verse;

namespace SimpleLeadership
{
    public class ScheduledDelayedRaid : IExposable
    {
        public int fireTick = -1;
        public string tag;
        public IncidentParms parms;

        public void ExposeData()
        {
            Scribe_Values.Look(ref fireTick, "fireTick", -1);
            Scribe_Values.Look(ref tag, "tag");
            Scribe_Deep.Look(ref parms, "parms");
        }
    }

    public class MapComponent_DelayedRaid : MapComponent
    {
        private List<ScheduledDelayedRaid> scheduledRaids = [];
        public int hitAndRunLockoutTick = -1;
        public List<int> scorchedEarthTriggeredLordIDs = [];

        public bool HasScheduledTag(string tag) => scheduledRaids.Any(r => r.tag == tag);

        public MapComponent_DelayedRaid(Map map) : base(map) { }

        public void Schedule(IncidentParms parms, int delayTicks, string tag = null)
        {
            scheduledRaids.Add(new ScheduledDelayedRaid
            {
                parms = parms,
                tag = tag,
                fireTick = Find.TickManager.TicksGame + delayTicks
            });
        }

        public override void MapComponentTick()
        {
            for (var i = scheduledRaids.Count - 1; i >= 0; i--)
            {
                var entry = scheduledRaids[i];
                if (Find.TickManager.TicksGame >= entry.fireTick)
                {
                    scheduledRaids.RemoveAt(i);
                    IncidentDefOf.RaidEnemy.Worker.TryExecute(entry.parms);
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref scheduledRaids, "scheduledRaids", LookMode.Deep);
            Scribe_Values.Look(ref hitAndRunLockoutTick, "hitAndRunLockoutTick", -1);
            Scribe_Collections.Look(ref scorchedEarthTriggeredLordIDs, "scorchedEarthTriggeredLordIDs", LookMode.Value);
            scheduledRaids ??= [];
            scorchedEarthTriggeredLordIDs ??= [];
        }
    }
}
