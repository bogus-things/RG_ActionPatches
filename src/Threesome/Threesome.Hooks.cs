using HarmonyLib;
using RG.Scene;
using RG.Scene.Action.Core;
using RG.Scripts;
using Il2CppSystem.Collections.Generic;
using BepInEx.Logging;

namespace RGActionPatches.Threesome
{
    class Hooks
    {
        private static ManualLogSource Log = RGActionPatchesPlugin.Log;
        internal static string GUID = RGActionPatchesPlugin.GUID + ".Threesome";

        // Add MMF command in workplace
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Actor), nameof(Actor.FilterCommands))]
        private static void FilterCommandsPost(Actor __instance, IReadOnlyList<ActionCommand> commands, List<ActionCommand> dest)
        {
            Patches.PatchThreesomeInPublicMap(ActionScene.Instance, __instance);
        }

        //for unknown reason the option list is not populated for a character with a different job id even it is changed to bad friend
        //manually create the list for the private room case
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ActionScene), nameof(ActionScene.GetActorMMFTargetCommandList))]
        private static void getActorMMFTargetCommandListPost(ActionScene __instance, Actor actor, List<ActionCommand> commandList)
        {
            Patches.UpdateMMFTargetCommandList(__instance, commandList);
        }

        //Generate MMF command list in public room
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ActionScene), nameof(ActionScene.GetActorMMFEventTargetCommandList))]
        private static void GetActorMMFEventTargetCommandListPost(ActionScene __instance, Actor actor, List<ActionCommand> commandList)
        {
            Patches.UpdateMMFTargetCommandListInPublicRoom(__instance, actor, commandList);
        }

        //for unknown reason the option list is not populated for a character with a different job id even it is changed to bad friend
        //manually create the list for the private room case
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ActionScene), nameof(ActionScene.GetActorFFMTargetCommandList))]
        private static void getActorFFMTargetCommandListPost(ActionScene __instance, Actor actor, List<ActionCommand> commandList)
        {
            Patches.UpdateFFMTargetCommandList(__instance, actor, commandList);
        }
    }
}
