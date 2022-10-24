using HarmonyLib;
using RG.Scene;
using RG.Scene.Action.UI;


namespace RGActionPatches
{
    internal static class Hooks
    {
        // Capture CommandList instance at scene load and initialize our other patches
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CommandList), nameof(CommandList.Awake))]
        private static void AwakePost(CommandList __instance)
        {
            StateManager.Instance.currentCommandList = __instance;
            Harmony.CreateAndPatchAll(typeof(AddCommands.Hooks), AddCommands.Hooks.GUID);
            Harmony.CreateAndPatchAll(typeof(DateSpotMovement.Hooks), DateSpotMovement.Hooks.GUID);
            Harmony.CreateAndPatchAll(typeof(TalkTarget.Hooks), TalkTarget.Hooks.GUID);
            Harmony.CreateAndPatchAll(typeof(TalkToSomeone.Hooks), TalkToSomeone.Hooks.GUID);
            Harmony.CreateAndPatchAll(typeof(Guests.Hooks), Guests.Hooks.GUID);

            Guests.Patches.ChangeCommandStates(ActionScene.Instance.Actors);
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
            Harmony.UnpatchID(TalkToSomeone.Hooks.GUID);
            Harmony.UnpatchID(Guests.Hooks.GUID);
        }
    }
}
