using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace SimpleLeadership
{
    public class DoctrineDef : Def
    {
        public Color categoryColor;
        public string shortLabel;
        public bool affectsRaidBehavior;

        public string ShortLabel => shortLabel.NullOrEmpty() is false ? shortLabel : label;
        public List<DoctrineDef> conflictingDoctrines;
        public List<string> effects;
        public TechLevel minTechLevel;

        public bool ConflictsWith(DoctrineDef other)
        {
            if (this == other)
                return true;
            return conflictingDoctrines?.Contains(other) is true ||
                    other.conflictingDoctrines?.Contains(this) is true;
        }
    }
}
