using HarmonyLib;
using RimWorld;
using RimWorld.Planet;

namespace SimpleLeadership
{
    [HarmonyPatch(typeof(Pawn_GuestTracker), "SetGuestStatus")]
    public static class Pawn_GuestTracker_SetGuestStatus_Patch
    {
        public static void Postfix(Pawn_GuestTracker __instance, Faction newHost, GuestStatus guestStatus)
        {
            if (guestStatus == GuestStatus.Prisoner && newHost == Faction.OfPlayer)
            {
                var pawn = __instance.pawn;
                Utils.CheckForBaseLeaderDefeat(pawn);
            }
        }
    }
}
