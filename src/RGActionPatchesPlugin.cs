using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using BepInEx.IL2CPP;


namespace RGActionPatches
{
    [BepInPlugin(GUID, PluginName, Version)]
    public class RGActionPatchesPlugin : BasePlugin
    {
        public const string PluginName = "RG Action Patches";
        public const string GUID = "com.bogus.RGActionPatches";
        public const string Version = "0.0.1";

        internal static new ManualLogSource Log;

        public override void Load()
        {
            Log = base.Log;

            RGActionPatches.Config.init(this);

            if (RGActionPatches.Config.enabled)
            {
                Harmony.CreateAndPatchAll(typeof(Hooks), GUID);
            }
        }
    }
}
