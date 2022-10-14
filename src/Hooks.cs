using HarmonyLib;
using RG.Scene;
using RG.Scene.Action.Core;
using RG.Scene.Action.UI;
using RG.Scripts;
using Il2CppSystem.Collections.Generic;
using BepInEx.Logging;


namespace RGActionPatches
{
    internal static class Hooks
    {
        private static ManualLogSource Log = RGActionPatchesPlugin.Log;

        // Rewrite the target command list, and replace with commands for all actors in the scene (minus self)
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ActionScene), nameof(ActionScene.GetActorTargetCommandList))]
        private static void getActorTargetCommandListPost(ActionScene __instance, Actor actor, List<ActionCommand> commandList)
        {
            Patches.TalkTarget.unrestrictTalkTargetList(__instance, actor, commandList);
        }

        // Rewrite the command list UI to undo some target filtering that happens in this function
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CommandList), nameof(CommandList.RefreshCommands), new[] { typeof(IReadOnlyList<ActionCommand>), typeof(ActionCommand) })]
        private static void refreshCommandsPost(CommandList __instance, IReadOnlyList<ActionCommand> commands, ActionCommand cancelCommand)
        {
            Patches.TalkTarget.resetCommandList(__instance, commands, cancelCommand);
        }

        // Add "Talk to someone" to the list of commands if it's been filtered out
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Actor), nameof(Actor.FilterCommands))]
        private static void filterCommandsPost(Actor __instance, IReadOnlyList<ActionCommand> commands, List<ActionCommand> dest)
        {
            if (ActionScene.Initialized)
            {
                Patches.TalkToSomeone.addToActorCommands(__instance, ActionScene.Instance, commands, dest);
            }
        }

        // Adds "Talk to someone" to the list of commands available at a point if it's missing
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ActionPoint), nameof(ActionPoint.GetTypeCommandList), new[] { typeof(byte), typeof(RG.Define.Action.Forms), typeof(int), typeof(List<ActionCommand>) })]
        private static void getTypeCommandListPost(ActionPoint __instance, int type, List<ActionCommand> commands)
        {
            // type 3 = move commands (talk to someone, go to casino, etc)
            if (ActionScene.Initialized && __instance.AttachedActor != null && type == 3)
            {
                Patches.TalkToSomeone.addToPointCommands(__instance.AttachedActor, ActionScene.Instance, commands);
            }
        }

        // Overrides the actor's destination to the spot across from the target at date locations
        // (Cafe, Restaurant, Park) and resets their state so they walk there
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Actor), nameof(Actor.TalkTo))]
        private static void talkToPost(Actor __instance, Actor target)
        {
            Patches.DateSpotMovement.redirectActorToPairedPoint(__instance, target, ActionScene.Instance);
        }

        // When actor gets to the spot across from the target, updates the actor/point references
        // and starts up the conversation
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Actor), nameof(Actor.OnArrivedDestination))]
        private static void onArrivedDestinationPost(Actor __instance)
        {
            Patches.DateSpotMovement.handleArrivalAfterRedirect(__instance, ActionScene.Instance);
        }
    }
}
