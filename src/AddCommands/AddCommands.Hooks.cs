using HarmonyLib;
using RG.Scene;
using RG.Scene.Action.Core;
using RG.Scripts;
using Il2CppSystem.Collections.Generic;
using BepInEx.Logging;
using RG;

namespace RGActionPatches.AddCommands
{
    class Hooks
    {
        private static ManualLogSource Log = RGActionPatchesPlugin.Log;
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
            Util.SpoofJobID(ActionScene.Instance, actor);
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
            Util.SpoofJobID(ActionScene.Instance, actor);
        }

        // Restore the actor's JobID after the decision runs
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ActionScene), nameof(ActionScene.DecideHigherPriorityAction))]
        private static void DecideHigherPriorityActionPost(Actor actor, int __state)
        {
            actor._status.JobID = __state;
        }

        // Add "Talk to someone" to the list of commands available at a point if it's missing
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ActionPoint), nameof(ActionPoint.GetTypeCommandList), new[] { typeof(byte), typeof(Define.Action.Forms), typeof(int), typeof(List<ActionCommand>) })]
        [HarmonyPriority(Priority.Low)]
        private static void GetTypeCommandList4Post(ActionPoint __instance, int type, List<ActionCommand> commands)
        {
            Patches.AddToPointNeutralCommands(__instance.AttachedActor, ActionScene.Instance, type, commands);
        }

        // Add "Move to conference room" to list of commands for office points
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ActionPoint), nameof(ActionPoint.GetTypeCommandList), new[] { typeof(byte), typeof(int), typeof(int), typeof(List<ActionCommand>) })]
        private static void GetTypeCommandList3Post(ActionPoint __instance, int type, List<ActionCommand> commands)
        {
            Patches.AddToPointSocializeCommands(__instance.AttachedActor, ActionScene.Instance, type, commands);
        }
    }
}
