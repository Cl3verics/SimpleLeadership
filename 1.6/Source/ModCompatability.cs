using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SimpleLeadership
{
    [StaticConstructorOnStartup]
    public static class ModCompatability
{
        private static readonly MethodInfo EligibilityMethod;
        private static readonly MethodInfo EraEligibilityMethod;
        public static bool IsActive = ModsConfig.IsActive("dame.ignorance");
        public static bool EraActive = ModsConfig.IsActive("cruesoe.eraappropriatethreats");
        static ModCompatability()
        {
            if (IsActive)
            {
                var baseType = AccessTools.TypeByName("DIgnoranceIsBliss.Core_Patches.IgnoranceBase");
                EligibilityMethod = AccessTools.Method(baseType, "FactionInEligibleTechRange", new[] { typeof(Faction) });
            }
            else if (EraActive)
            {
                var techPolicy = AccessTools.TypeByName("EraAppropriateThreats.TechPolicy");
                EraEligibilityMethod = AccessTools.Method(techPolicy, "Eligible", new[] { typeof(Faction) });
            }
        }

        public static bool FactionInEligibleTechRange(Faction faction)
        {
            if (IsActive) return (bool)EligibilityMethod.Invoke(null, new object[] { faction });
            if (EraActive) return (bool)EraEligibilityMethod.Invoke(null, new object[] { faction });
            return true;
        }
    }
}
