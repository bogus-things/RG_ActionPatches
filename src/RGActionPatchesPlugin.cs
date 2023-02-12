using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using BepInEx.IL2CPP;
using UnhollowerRuntimeLib;
using UnityEngine;

namespace RGActionPatches
{
    [BepInProcess("RoomGirl")]
    [BepInPlugin(GUID, PluginName, Version)]
    public class RGActionPatchesPlugin : BasePlugin
    {
        public const string PluginName = "RG Action Patches";
        public const string GUID = "com.bogus.RGActionPatches";
        public const string Version = "1.0.2";
        private const string ComponentName = "BogusComponents";

        internal static new ManualLogSource Log;
        public GameObject BogusComponents;

        public override void Load()
        {
            Log = base.Log;

            RGActionPatches.Config.Init(this);

            if (RGActionPatches.Config.Enabled)
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
            }
            StateManager.Instance = new StateManager(BogusComponents.AddComponent(Il2CppType.Of<StateManager>()).Pointer);
        }
    }
}
