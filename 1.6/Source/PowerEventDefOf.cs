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
        public static PowerEventDef SL_Famine;
        public static PowerEventDef SL_Sanctioned;
        public static PowerEventDef SL_Support;
        public static PowerEventDef SL_PrisonerTransfer;
        public static PowerEventDef SL_Reinforcements;

        public static DoctrineDef SL_FinalPrice;
        public static DoctrineDef SL_Intervention;
        public static DoctrineDef SL_ScorchedEarth;
        public static DoctrineDef SL_NoMercy;
        public static DoctrineDef SL_ForcesPreservation;
        public static DoctrineDef SL_HitAndRun;
        public static DoctrineDef SL_FamilialSuccession;
        public static DoctrineDef SL_GraciousRepatriation;
        public static DoctrineDef SL_FallenVeneration;
        public static DoctrineDef SL_LeadersLegacy;
        public static DoctrineDef SL_AbsoluteBorders;
        public static DoctrineDef SL_DefenseInDepth;
        public static DoctrineDef SL_AttritionWarfare;
        public static DoctrineDef SL_MutualDefense;
        public static RaidStrategyDef Siege;

        static PowerEventDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(PowerEventDefOf));
        }
    }
}
