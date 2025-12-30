using RimWorld;

namespace SimpleLeadership
{
    [DefOf]
    public static class PowerEventDefOf
    {
        public static PowerEventDef SL_PowerVoid;
        public static PowerEventDef SL_PowerStruggle;
        public static PowerEventDef SL_Fortifying;
        public static PowerEventDef SL_Inspection;
        public static PowerEventDef SL_Vigilant;

        static PowerEventDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(PowerEventDefOf));
        }
    }
}