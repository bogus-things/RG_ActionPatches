using HarmonyLib;
using RG.Scene;
using RG.Scene.Action.UI;
using BepInEx.Logging;
using RG.Scene.Action.Core;

namespace RGActionPatches
{
    internal static class Hooks
    {
        private static ManualLogSource Log = RGActionPatchesPlugin.Log;
        private const string LazyGUID = RGActionPatchesPlugin.GUID + ".lazy";

        // Capture CommandList instance at scene load and initialize our other patches
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CommandList), nameof(CommandList.Awake))]
        private static void AwakePost(CommandList __instance)
        {
            StateManager.Instance.currentCommandList = __instance;
            Harmony.CreateAndPatchAll(typeof(LazyHooks), LazyGUID);
        }

        // Release CommandList instance on scene destroy and unpatch
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ActionScene), nameof(ActionScene.OnDestroy))]
        private static void DestroyPre()
        {
            StateManager.Instance.currentCommandList = null;
            Harmony.UnpatchID(LazyGUID);
        }
    }
}
