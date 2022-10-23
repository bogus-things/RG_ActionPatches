using HarmonyLib;
using RG.Scene;
using RG.Scene.Action.Core;
using RG.Scripts;
using Il2CppSystem.Collections.Generic;

namespace RGActionPatches.AddCommands
{
    class Hooks
    {
        internal static string GUID = RGActionPatchesPlugin.GUID + ".AddCommands";

        // Add "Talk to someone" to the list of commands if it's been filtered out
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Actor), nameof(Actor.FilterCommands))]
        private static void FilterCommandsPost(Actor __instance, IReadOnlyList<ActionCommand> commands, List<ActionCommand> dest)
        {
            Patches.UpdateActorCommands(ActionScene.Instance, __instance, commands, dest);
        }

        // Temporarily fake the actor's JobID before autoplay decides the actions so job-restricted actions aren't disabled
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ActionScene), nameof(ActionScene.DecideAction))]
        [HarmonyPriority(Priority.Low)]
        private static void DecideActionPre(Actor actor, out int __state)
        {
            __state = actor.JobID;
            Patches.SpoofJobID(ActionScene.Instance, actor);
        }

        // Restore the actor's JobID after the decision runs
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ActionScene), nameof(ActionScene.DecideAction))]
        private static void DecideActionPost(Actor actor, int __state)
        {
            actor._status.JobID = __state;
            actor.MapID = ActionScene.Instance.MapID;
        }

        // Temporarily fake the actor's JobID before autoplay decides the actions so job-restricted actions aren't disabled
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ActionScene), nameof(ActionScene.DecideHigherPriorityAction))]
        private static void DecideHigherPriorityActionPre(Actor actor, out int __state)
        {
            __state = actor.JobID;
            Patches.SpoofJobID(ActionScene.Instance, actor);
        }

        // Restore the actor's JobID after the decision runs
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ActionScene), nameof(ActionScene.DecideHigherPriorityAction))]
        private static void DecideHigherPriorityActionPost(Actor actor, int __state)
        {
            actor._status.JobID = __state;
        }

        // Temporarily fake the actors' JobIDs before starting the ADV so text lookup doesn't fail
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ActionScene), nameof(ActionScene.BeginHSceneADV))]
        private static void BeginHSceneADVPre(Actor main, Actor subA, Actor subB)
        {
            Patches.SpoofMultiple(ActionScene.Instance, main, subA, subB);
        }

        // Temporarily fake the actors' JobIDs before starting the ADV so text lookup doesn't fail
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ActionScene), nameof(ActionScene.BeginHSceneOutADV))]
        private static void BeginHSceneOutADVPre(Actor main, Actor subA, Actor subB)
        {
            Patches.SpoofMultiple(ActionScene.Instance, main, subA, subB);
        }

        // Restore any faked JobIDs upon leaving ADV
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ActionScene), nameof(ActionScene.BackFromADVScene))]
        private static void BackFromADVScenePost()
        {
            Patches.RestoreSpoofed();
        }
    }
}
