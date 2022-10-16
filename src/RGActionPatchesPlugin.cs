using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using BepInEx.IL2CPP;
using UnhollowerRuntimeLib;
using UnityEngine;

namespace RGActionPatches
{
    [BepInPlugin(GUID, PluginName, Version)]
    public class RGActionPatchesPlugin : BasePlugin
    {
        public const string PluginName = "RG Action Patches";
        public const string GUID = "com.bogus.RGActionPatches";
        public const string Version = "0.0.2";
        private const string ComponentName = "BogusComponents";

        internal static new ManualLogSource Log;
        public GameObject BogusComponents;

        public override void Load()
        {
            Log = base.Log;

            RGActionPatches.Config.init(this);

            if (RGActionPatches.Config.enabled)
            {
                Harmony.CreateAndPatchAll(typeof(Hooks), GUID);
            }
            
            ClassInjector.RegisterTypeInIl2Cpp<StateManager>();

            BogusComponents = GameObject.Find(ComponentName);
            if (BogusComponents == null)
            {
                BogusComponents = new GameObject(ComponentName);
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
