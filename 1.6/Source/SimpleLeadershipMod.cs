using HarmonyLib;
using Verse;

namespace SimpleLeadership
{
    public class SimpleLeadershipMod : Mod
    {
        public SimpleLeadershipMod(ModContentPack content) : base(content)
        {
            new Harmony("pb3n.Taranchuk.SimpleLeadership").PatchAll();
        }
    }
}