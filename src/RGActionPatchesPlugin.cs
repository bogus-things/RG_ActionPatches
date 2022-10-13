using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using BepInEx.IL2CPP;
using UnityEngine;
using UnhollowerRuntimeLib;

namespace RGActionPatches
{
    [BepInPlugin(GUID, PluginName, Version)]
    public class RGActionPatchesPlugin : BasePlugin
    {
        public const string PluginName = "RG Action Patches";
        public const string GUID = "com.bogus.RGActionPatches";
        public const string Version = "0.0.1";

        internal static new ManualLogSource Log;

        private ConfigEntry<bool> enabled;
        public GameObject BogusComponents;

        public override void Load()
        {
            Log = base.Log;

            enabled = Config.Bind(
                "General",
                "Enable this plugin",
                true,
                "If false, this plugin will do nothing (requires game restart)"
            );

            if (enabled.Value)
            {
                Harmony.CreateAndPatchAll(typeof(Hooks), GUID);
            }

            ClassInjector.RegisterTypeInIl2Cpp<StateManager>();

            BogusComponents = GameObject.Find("BogusComponents");
            if (BogusComponents == null)
            {
                BogusComponents = new GameObject("BogusComponents");
                GameObject.DontDestroyOnLoad(BogusComponents);
                BogusComponents.hideFlags = HideFlags.HideAndDontSave;
                BogusComponents.AddComponent<StateManager>();
            }
            else
            {
                BogusComponents.AddComponent<StateManager>();
            }
        }
    }
}
