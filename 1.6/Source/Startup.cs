using System;
using System.Collections.Generic;
using Verse;
using RimWorld;
using RimWorld.Planet;

namespace SimpleLeadership
{
    [StaticConstructorOnStartup]
    public static class Startup
    {
        static Startup()
        {
            foreach (var def in DefDatabase<WorldObjectDef>.AllDefs)
            {
                if (typeof(Site).IsAssignableFrom(def.worldObjectClass))
                {
                    def.comps ??= new List<WorldObjectCompProperties>();
                    def.comps.Add(new WorldObjectCompProperties_SiteOwnership());
                }
                else if (typeof(Settlement).IsAssignableFrom(def.worldObjectClass))
                {
                    if (def.inspectorTabs == null)
                        def.inspectorTabs = new List<Type>();
                    def.inspectorTabs.Add(typeof(WITab_FactionLeadership));

                    if (def.inspectorTabsResolved == null)
                        def.inspectorTabsResolved = new List<InspectTabBase>();
                    def.inspectorTabsResolved.Add(InspectTabManager.GetSharedInstance(typeof(WITab_FactionLeadership)));
                }
            }
        }
    }
}
