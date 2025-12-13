using System;
using System.Collections.Generic;
using Verse;
using UnityEngine;

namespace SimpleLeadership
{
    public class PowerEventDef : Def
    {
        public Type workerClass;
        public List<string> effects;
        public FloatRange durationDays;
        public string iconPath;

        [Unsaved(false)]
        private Texture2D icon;

        public Texture2D Icon
        {
            get
            {
                if (icon == null)
                {
                    icon = ContentFinder<Texture2D>.Get(iconPath);
                }
                return icon;
            }
        }
    }
}