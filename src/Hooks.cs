using BepInEx.Logging;
using HarmonyLib;
using RG.Scene;
using RG.Scene.Action.UI;

namespace RGActionPatches
{
    internal static class Hooks
    {
        private static ManualLogSource Log = RGActionPatchesPlugin.Log;
        // Capture CommandList instance at scene load and initialize our other patches
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CommandList), nameof(CommandList.Awake))]
        private static void AwakePost(CommandList __instance)
        {
            StateManager.Instance.currentCommandList = __instance;
            Harmony.CreateAndPatchAll(typeof(AddCommands.Hooks), AddCommands.Hooks.GUID);
            Harmony.CreateAndPatchAll(typeof(DateSpotMovement.Hooks), DateSpotMovement.Hooks.GUID);
            Harmony.CreateAndPatchAll(typeof(TalkTarget.Hooks), TalkTarget.Hooks.GUID);
            Harmony.CreateAndPatchAll(typeof(Guests.Hooks), Guests.Hooks.GUID);
            Harmony.CreateAndPatchAll(typeof(ADV.Hooks), ADV.Hooks.GUID);

            Guests.Patches.ChangeCommandStates(ActionScene.Instance.Actors);
            Guests.Patches.RecoverGuestDictionary();
        }

        // Release CommandList instance on scene destroy and unpatch
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ActionScene), nameof(ActionScene.OnDestroy))]
        private static void DestroyPre()
        {
            StateManager.Instance.currentCommandList = null;
            Harmony.UnpatchID(AddCommands.Hooks.GUID);
            Harmony.UnpatchID(DateSpotMovement.Hooks.GUID);
            Harmony.UnpatchID(TalkTarget.Hooks.GUID);
            Harmony.UnpatchID(Guests.Hooks.GUID);
            Harmony.UnpatchID(ADV.Hooks.GUID);
        }

        //For fixing the bug that load a guest wrongly in private room due to no sub-map case handling when loading guests
        [HarmonyPrefix]
        [HarmonyPatch(typeof(HomeScene), nameof(HomeScene.GoIntoMap), new System.Type[] { typeof(int), typeof(byte), typeof(int), typeof(int), typeof(int), typeof(int) })]
        private static void GoIntoMap(int mapID, byte sex, int memberKey, int charaJobID, int charaIndexAsMob, int subMapID)
        {
            Guests.Patches.AlterGuestDictionaryWhenEnteringMap(mapID, subMapID);
        }


    }
}
