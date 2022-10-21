using HarmonyLib;
using RG.Scene;
using RG.Scene.Action.Core;
using RG.Scripts;
using Il2CppSystem.Collections.Generic;
using RG.Scene.Action.UI;
using RG.UI;
using BepInEx.Logging;

namespace RGActionPatches
{
    class LazyHooks
    {
        private static ManualLogSource Log = RGActionPatchesPlugin.Log;

        // Rewrite the target command list, and replace with commands for all actors in the scene (minus self)
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ActionScene), nameof(ActionScene.GetActorTargetCommandList))]
        private static void GetActorTargetCommandListPost(ActionScene __instance, Actor actor, List<ActionCommand> commandList)
        {
            Patches.TalkTarget.UnrestrictTalkTargetList(__instance, actor, commandList);
        }

        // Temporarily fake the actor's JobID before autoplay decides the actions so "Talk To Someone" isn't disabled
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ActionScene), nameof(ActionScene.DecideAction))]
        private static void DecideActionPre(Actor actor, out int __state)
        {
            __state = actor.JobID;
            Patches.TalkToSomeone.SpoofJobID(actor);
        }

        // Restore the actor's JobID after the decision runs
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ActionScene), nameof(ActionScene.DecideAction))]
        private static void DecideActionPost(Actor actor, int __state)
        {
            actor._status.JobID = __state;
        }

        // Temporarily fake the actor's JobID before autoplay decides the actions so "Talk To Someone" isn't disabled
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ActionScene), nameof(ActionScene.DecideHigherPriorityAction))]
        private static void DecideHigherPriorityActionPre(Actor actor, out int __state)
        {
            __state = actor.JobID;
            Patches.TalkToSomeone.SpoofJobID(actor);
        }

        // Restore the actor's JobID after the decision runs
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ActionScene), nameof(ActionScene.DecideHigherPriorityAction))]
        private static void DecideHigherPriorityActionPost(Actor actor, int __state)
        {
            actor._status.JobID = __state;
        }

        // Overrides actor availability so auto will talk to them outside their job maps
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Actor), nameof(Actor.IsAvailableCome2Talk))]
        private static void IsAvailableCome2TalkPost(Actor __instance, ActionInfo xInfo, ref bool __result)
        {
            if(!__result)
            {
                __result = __instance.CommandState == RG.Define.Action.CommandState.Neutral;
            }
        }

        // Rewrite the command list UI to undo some target filtering that happens in this function
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CommandList), nameof(CommandList.RefreshCommands), new[] { typeof(IReadOnlyList<ActionCommand>), typeof(ActionCommand) })]
        private static void RefreshCommandsPost(CommandList __instance, IReadOnlyList<ActionCommand> commands, ActionCommand cancelCommand)
        {
            Patches.TalkTarget.ResetCommandList(__instance, commands, cancelCommand);
        }

        // Add "Talk to someone" to the list of commands if it's been filtered out
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Actor), nameof(Actor.FilterCommands))]
        private static void FilterCommandsPost(Actor __instance, IReadOnlyList<ActionCommand> commands, List<ActionCommand> dest)
        {
            if (ActionScene.Initialized)
            {
                Patches.TalkToSomeone.AddToActorCommands(__instance, ActionScene.Instance, commands, dest);
            }
        }

        // Patch the command selection to disable some of the newly-available talk commands
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ScrollCylinder), nameof(ScrollCylinder.SetTarget))]
        private static void SetTargetPost()
        {
            Patches.TalkTarget.UpdateOptionDisabledState(StateManager.Instance.currentCommandList, ActionScene.Instance);
        }

        // Adds "Talk to someone" to the list of commands available at a point if it's missing
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ActionPoint), nameof(ActionPoint.GetTypeCommandList), new[] { typeof(byte), typeof(RG.Define.Action.Forms), typeof(int), typeof(List<ActionCommand>) })]
        private static void GetTypeCommandListPost(ActionPoint __instance, int type, List<ActionCommand> commands)
        {
            // type 3 = move commands (talk to someone, go to casino, etc)
            if (ActionScene.Initialized && __instance.AttachedActor != null && type == 3)
            {
                Patches.TalkToSomeone.AddToPointCommands(__instance.AttachedActor, ActionScene.Instance, commands);
            }
        }

        // If headed toward a seat previously taken by someone in the bathroom, or if leaving
        // a table while the other seat's character is in the bathroom to talk to someone else,
        // do a seat swap to avoid any collisions between characters
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Actor), nameof(Actor.TalkTo))]
        private static void TalkToPre(Actor __instance, Actor target)
        {
            Patches.DateSpotMovement.DoSeatSwap(__instance, target, ActionScene.Instance);
        }

        // Overrides the actor's destination to the spot across from the target at date locations
        // (Cafe, Restaurant, Park) and resets their state so they walk there
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Actor), nameof(Actor.TalkTo))]
        private static void TalkToPost(Actor __instance, Actor target)
        {
            Patches.DateSpotMovement.RedirectActorToPairedPoint(__instance, target, ActionScene.Instance);
        }

        // When actor gets to the spot across from the target, updates the actor/point references
        // and starts up the conversation
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Actor), nameof(Actor.OnArrivedDestination))]
        private static void OnArrivedDestinationPost(Actor __instance)
        {
            Patches.DateSpotMovement.HandleArrivalAfterRedirect(__instance, ActionScene.Instance);
        }
    }
}
