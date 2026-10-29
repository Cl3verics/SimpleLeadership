using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SimpleLeadership
{
    [StaticConstructorOnStartup]
    public static class IgnoranceIsBlissCompat
    {
        private static readonly MethodInfo EligibilityMethod;
        public static bool IsActive = ModsConfig.IsActive("dame.ignorance");
        static IgnoranceIsBlissCompat()
        {
            if (!IsActive) return;
            var baseType = AccessTools.TypeByName("DIgnoranceIsBliss.IgnoranceBase");
            EligibilityMethod = AccessTools.Method(baseType, "FactionInEligibleTechRange", new[] { typeof(Faction) });
        }

        public static bool FactionInEligibleTechRange(Faction faction)
        {
            if (!IsActive) return true;
            return (bool)EligibilityMethod.Invoke(null, new object[] { faction });
        }
    }
}
