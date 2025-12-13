using RimWorld;

namespace SimpleLeadership
{
    [DefOf]
    public static class PowerEventDefOf
    {
        public static PowerEventDef SL_PowerVoid;
        public static PowerEventDef SL_PowerStruggle;

        static PowerEventDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(PowerEventDefOf));
        }
    }
}