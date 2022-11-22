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

        //Force male actors to be  bad friend in a private room
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Actor), nameof(Actor.FilterCommands))]
        private static void FilterCommandsPre(Actor __instance, IReadOnlyList<ActionCommand> commands, List<ActionCommand> dest)
        {
            Patches.SpoofActorAsBadFriend(ActionScene.Instance);
        }

        // Add "Talk to someone" to the list of commands if it's been filtered out
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Actor), nameof(Actor.FilterCommands))]
        private static void FilterCommandsPost(Actor __instance, IReadOnlyList<ActionCommand> commands, List<ActionCommand> dest)
        {
            Patches.UpdateActorCommands(ActionScene.Instance, __instance, commands, dest);
            // Add MMF command in job job room
            Patches.PatchThreesomeInPublicMap(ActionScene.Instance, __instance);
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

        //To generate full summon list by altering the actor list
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ActionScene), nameof(ActionScene.GetSummonCommandList))]
        private static void getSummonCommandListPre(ActionScene __instance, Actor actor, List<ActionCommand> commandList, ref List<Actor> __state)
        {
            Patches.SpoofActorList(__instance, actor, ref __state);
        }

        /***
         * Allow summoning the 2nd summoning character to be non-bad-friend
         * Private room rule 
         * 1. the private room can only allow 3 person max
         * 2. if a character has already taken the position next with the main actor, summoning female into this room is not allowed (due to have bugs if female standing in bad friend position)
        **/
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ActionScene), nameof(ActionScene.GetSummonCommandList))]
        private static void getSummonCommandListPost(ActionScene __instance, Actor actor, List<ActionCommand> commandList, List<Actor> __state)
        {
            Patches.UpdateSummonCommandList(__instance, actor, commandList, __state);
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

        //Restore the job id of the character occupying the bad friend action point when the actor leave the private room
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Actor), nameof(Actor.OnExit))]
        private static void OnExit(Actor __instance)
        {
            Guests.Patches.RestoreActorFromBadFriend(__instance);
        }

        //Restore the job id of the characters occupying the bad friend action point in case the player exit the private room and go back to town map
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ActionScene), nameof(ActionScene.OnDestroy))]
        private static void OnDestroyPre()
        {
            Guests.Patches.RestoreActorFromBadFriend();
        }

        //////For checking the method that should be called by action delegate when creating ActionCommand
        ////[HarmonyPrefix]
        ////[HarmonyPatch(typeof(ActionPoint.__c__DisplayClass150_1), nameof(ActionPoint.__c__DisplayClass150_1._Init_b__1))]
        ////private static void _Init_b__1(ActionPoint.__c__DisplayClass150_1 __instance, Actor actor, ActionInfo xInfo)
        ////{
        ////    Log.Log(LogLevel.Info, "===Check _Init_b__1===");
        ////    if (actor != null)
        ////        Log.Log(LogLevel.Info, "actor : " + actor.Status.FullName);
        ////    if (xInfo != null)
        ////        Log.Log(LogLevel.Info, "xInfo : " + xInfo.ActionID);
        ////    if (__instance.action != null)
        ////    {
        ////        Log.Log(LogLevel.Info, "action full name : " + __instance.action.Method.DeclaringType.FullName);
        ////        Log.Log(LogLevel.Info, "action signature : " + __instance.action.Method.FormatNameAndSig(true));
        ////    }
        ////}

    }
}
