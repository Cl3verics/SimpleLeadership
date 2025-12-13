using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;

namespace SimpleLeadership
{
    public static class PowerEventExtensions
    {
        public static bool IsInPowerEvent<T>(this object obj) where T : PowerEventBase
        {
            return WorldComponent_LeaderTracker.Instance.IsInPowerEvent<T>(obj);
        }

        public static IEnumerable<T> GetActiveEvents<T>(this object obj) where T : PowerEventBase
        {
            return WorldComponent_LeaderTracker.Instance.GetActiveEventsFor(obj).OfType<T>();
        }
    }
}
