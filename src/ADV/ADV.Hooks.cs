using ADV;
using BepInEx.Logging;
using HarmonyLib;
using RG.Scene;
using RG.Scene.Action.Core;
using System;

namespace RGActionPatches.ADV
{
    class Hooks
    {
        private static ManualLogSource Log = RGActionPatchesPlugin.Log;
        internal static string GUID = RGActionPatchesPlugin.GUID + ".ADV";

        // Temporarily fake the actors' JobIDs before starting the ADV so text lookup doesn't fail
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ActionScene), nameof(ActionScene.BeginHSceneADV))]
        private static void BeginHSceneADVPre(ref int eventID, Actor main, Actor subA, ref Actor subB)
        {
            eventID = Patches.SpoofForJobH(ActionScene.Instance, eventID, main, subA, subB);
            subB = Patches.PatchActorsForPrivateMMF(ActionScene.Instance, eventID, main, subA, subB);
            eventID = Patches.SpoofForPrivateH(ActionScene.Instance, eventID, main, subA, subB);
        }

        // Temporarily fake the actors' JobIDs before starting the ADV so text lookup doesn't fail
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ActionScene), nameof(ActionScene.BeginHSceneOutADV))]
        private static void BeginHSceneOutADVPre(int eventID, Actor main, Actor subA, Actor subB)
        {
            Patches.SpoofForJobH(ActionScene.Instance, eventID, main, subA, subB);
            Patches.SpoofForPrivateH(ActionScene.Instance, eventID, main, subA, subB);
        }

        // Restore any faked JobIDs upon leaving ADV
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ActionScene), nameof(ActionScene.BackFromADVScene))]
        private static void BackFromADVScenePost()
        {
            Patches.RestoreSpoofed();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(TextScenario), nameof(TextScenario.LoadFile))]
        private static void LoadFilePre(ref string bundle, ref string asset)
        {
            Patches.RedirectMissingAssets(ref bundle, ref asset);
        }


        [HarmonyFinalizer]
        [HarmonyPatch(typeof(TextScenario), nameof(TextScenario.LoadFile))]
        private static Exception CatchLoadErrors(Exception __exception, string bundle, string asset, ref bool __result)
        {
            if (__exception != null)
            {
                __result = false;
                Log.LogWarning($"Failed to load (bundle: {bundle}, asset: {asset}) for ADV scenario, skipping");
            }
            return null;
        }
    }
}
