using HarmonyLib;
using RG.Scene;
using RG.Scene.Action.Core;
using RG.Scene.Action.UI;
using RG.Scripts;
using RG.UI;
using Il2CppSystem.Collections.Generic;
using BepInEx.Logging;

namespace RGActionPatches.TalkTarget
{
    class Hooks
    {
        private static ManualLogSource Log = RGActionPatchesPlugin.Log;
        internal static string GUID = RGActionPatchesPlugin.GUID + ".TalkTarget";

        // Rewrite the target command list, and replace with commands for all actors in the scene (minus self)
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ActionScene), nameof(ActionScene.GetActorTargetCommandList))]
        private static void GetActorTargetCommandListPost(ActionScene __instance, Actor actor, List<ActionCommand> commandList)
        {
            Patches.UnrestrictTalkTargetList(__instance, actor, commandList);           
        }

        // Rewrite the command list UI to undo some target filtering that happens in this function
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CommandList), nameof(CommandList.RefreshCommands), new[] { typeof(IReadOnlyList<ActionCommand>), typeof(ActionCommand) })]
        private static void RefreshCommandsPost(CommandList __instance, IReadOnlyList<ActionCommand> commands, ActionCommand cancelCommand)
        {
            Patches.ResetCommandList(__instance, commands, cancelCommand);
        }

        // Patch the command selection to disable some of the newly-available talk commands
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ScrollCylinder), nameof(ScrollCylinder.SetTarget))]
        private static void SetTargetPost()
        {
            Patches.UpdateOptionDisabledState(StateManager.Instance.currentCommandList, ActionScene.Instance);
        }

        // Overrides actor availability to support auto movement
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Actor), nameof(Actor.IsAvailableCome2Talk))]
        private static void IsAvailableCome2TalkPost(Actor __instance, ActionInfo xInfo, ref bool __result)
        {
            __result = Patches.IsAvailableToTalk(__instance, __result);
        }
    }
}
