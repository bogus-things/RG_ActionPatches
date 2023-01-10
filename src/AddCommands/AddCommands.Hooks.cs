using HarmonyLib;
using RG.Scene;
using RG.Scene.Action.Core;
using RG.Scripts;
using Il2CppSystem.Collections.Generic;
using BepInEx.Logging;
using RG;
using System;

namespace RGActionPatches.AddCommands
{
    class Hooks
    {
        private static ManualLogSource Log = RGActionPatchesPlugin.Log;
        internal static string GUID = RGActionPatchesPlugin.GUID + ".AddCommands";

        //Force male actors to be  bad friend in a private room
        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(Actor), nameof(Actor.FilterCommands))]
        //private static void FilterCommandsPre(Actor __instance, ref int __state)
        //{
        //    __state = __instance.JobID;
        //    Patches.DoBadfriendSpoof(ActionScene.Instance, __instance);
        //}

        // Add "Talk to someone" to the list of commands if it's been filtered out
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Actor), nameof(Actor.FilterCommands))]
        private static void FilterCommandsPost(Actor __instance, IReadOnlyList<ActionCommand> commands, List<ActionCommand> dest)
        {
            Patches.UpdateActorCommands(ActionScene.Instance, __instance, commands, dest);
        }

        // Catch & Suppress an error thrown inside FilterCommands when it's patched by Harmony
        [HarmonyFinalizer]
        [HarmonyPatch(typeof(ParameterConditions), nameof(ParameterConditions.IF), new[] { typeof(Define.TableData.Category), typeof(int), typeof(Actor), typeof(Actor), typeof(ActionInfo) })]
        private static Exception WhoNamesAMethodIF()
        {
            return null;
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
