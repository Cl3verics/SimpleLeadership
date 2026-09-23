using System.Collections.Generic;
using Verse;

namespace SimpleLeadership
{
    public class KidnappedPrisonersList : IExposable
    {
        public List<Pawn> prisoners = new List<Pawn>();

        public KidnappedPrisonersList()
        {
        }

        public void ExposeData()
        {
            Scribe_Collections.Look(ref prisoners, "prisoners", LookMode.Reference);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                prisoners ??= new List<Pawn>();
                prisoners.RemoveAll(p => p == null);
            }
        }
    }
}
