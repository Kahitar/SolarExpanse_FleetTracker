#nullable disable
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace SolarExpanseObjectInfoListExpansion
{
    [BepInPlugin("com.mod.solarexpanse.objectinfolistexpansion", "ObjectInfoListExpansion", "1.3.1")]
    public class ObjectInfoListExpansionPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            new Harmony("com.mod.solarexpanse.objectinfolistexpansion").PatchAll();
            Logger.LogInfo("ObjectInfoListExpansion loaded");
        }
    }
}
