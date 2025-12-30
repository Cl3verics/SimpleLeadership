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

        public static bool IsInPowerEvent(this object obj, PowerEventDef def)
        {
            if (obj == null || def == null)
                return false;
            return WorldComponent_LeaderTracker.Instance.GetActiveEventsFor(obj).Any(ev => ev.def == def);
        }

        public static IEnumerable<T> GetActiveEvents<T>(this object obj) where T : PowerEventBase
        {
            return WorldComponent_LeaderTracker.Instance.GetActiveEventsFor(obj).OfType<T>();
        }
    }
}
