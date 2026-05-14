using BepInEx.Logging;
using HarmonyLib;
using SolarExpanseFleetTracker.UI;
using Manager;

namespace SolarExpanseFleetTracker.Patches
{
    [HarmonyPatch(typeof(NotificationManager), "Awake")]
    internal static class FleetTrackerPatch
    {
        internal static ManualLogSource Log = BepInEx.Logging.Logger.CreateLogSource("FleetTracker");

        [HarmonyPostfix]
        static void Postfix(NotificationManager __instance)
        {
            Log.LogInfo("[FleetTracker] NotificationManager.Awake postfix - injecting");
            FleetTrackerInjector.Inject(__instance, Log);
        }
    }
}
