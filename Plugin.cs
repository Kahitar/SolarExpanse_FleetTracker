#nullable disable
using BepInEx;
using HarmonyLib;
using SolarExpanseFleetTracker.UI;

namespace SolarExpanseFleetTracker
{
    [BepInPlugin("com.mod.solarexpanse.fleettracker", "FleetTracker", "1.4.0")]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            var harmony = new Harmony("com.mod.solarexpanse.fleettracker");
            harmony.PatchAll();
            Patches.PauseScreenEscPatch.Apply(harmony, Logger);
            Logger.LogInfo("FleetTracker loaded");
        }
    }
}
