#nullable disable
using BepInEx;
using HarmonyLib;
using SolarExpanseFleetTracker.UI;

namespace SolarExpanseFleetTracker
{
    [BepInPlugin("com.mod.solarexpanse.fleettracker", "FleetTracker", "1.3.0")]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            new Harmony("com.mod.solarexpanse.fleettracker").PatchAll();
            Logger.LogInfo("FleetTracker loaded");
        }
    }
}
