using Verse;

namespace SimpleLeadership
{
    public class FactionDoctrine : IExposable
    {
        public DoctrineDef def;
        public Pawn proposedBy;
        public bool isInvestigated;

        public void ExposeData()
        {
            Scribe_Defs.Look(ref def, "def");
            Scribe_References.Look(ref proposedBy, "proposedBy");
            Scribe_Values.Look(ref isInvestigated, "isInvestigated");
        }
    }
}
