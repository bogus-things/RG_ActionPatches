using BepInEx.Logging;
using HarmonyLib;
using Manager;
using RG.Scene;
using RG.Scene.Action.Core;
using RG.User;
using System;
using Il2CppSystem.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RG.Scripts;
using Il2CppSystem.Collections.ObjectModel;
using UnhollowerBaseLib;
using RG;
using UnityEngine;
using RG.Scene.Action.Core.State;

namespace RGActionPatches.Guests
{
    class Hooks
    {
        internal static string GUID = RGActionPatchesPlugin.GUID + ".Guests";
        private static ManualLogSource Log = RGActionPatchesPlugin.Log;

        // Check if anybody is in the "Welcome" command state and override it
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ActionScene), nameof(ActionScene.RefreshNextMove))]
        private static void RefreshNextMovePre(IReadOnlyList<Actor> actors)
        {
            Patches.ChangeCommandStates(actors);
        }

        // On job maps, override some state changes to allow called actors
        // to go to their usual spots if it's their workplace
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Actor), nameof(Actor.ChangeState), new[] { typeof(int), typeof(bool) })]
        private static void ChangeStatePre(Actor __instance, ref int stateType)
        {
            stateType = Patches.HandleJobCallRedirect(ActionScene.Instance, __instance, stateType);
        }

        // When called workers get to their spots, have the caller go talk
        // to them like usual
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Actor), nameof(Actor.OnArrivedDestination))]
        private static void OnArrivedDestinationPost(Actor __instance)
        {
            Patches.HandleJobCallArrival(ActionScene.Instance, __instance);
        }

        // next up:
        // populate jobID map
        // override getTypeCommandList results to unlock everything
        // figure out if we can do the same stuff to badfriend spots
        // figure out how to call people and occupy badfriend spots
        // see about updating mmf/ffm
        // this is going to lock some chars out of go to conference room, so try and add that action back in

        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(Actor), nameof(Actor.ChangeState), new[] { typeof(int), typeof(bool) })]
        //private static void ChangeStatePost2(Actor __instance, bool __state)
        //{
        //    if (__state)
        //    {
        //        GoSideCharacter ctrl = (GoSideCharacter)__instance.Controller;
        //        Log.LogMessage(ctrl.TimeToSlee)
        //    }
        //}

        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(Actor), nameof(Actor.Initialize))]
        //private static void testing(Actor __instance)
        //{
        //    Log.LogMessage("init " + __instance.name);
        //}


        //    [HarmonyPostfix]
        //[HarmonyPatch(typeof(Actor), nameof(Actor.Initialize))]
        //private static void testing1(Actor __instance)
        //{
        //    Log.LogMessage($"initpost {__instance.name} {__instance._stateID}");
        //    Log.LogMessage("tosync: " + __instance.ActionPointToSync?.name);
        //    Log.LogMessage("recent: " + __instance.RecentScheduledPoint?.name);
        //    Log.LogMessage("reserved: " + __instance.ReservedActionPoint?.name);
        //}




        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(Actor), nameof(Actor.PrecalcCommands))]
        //private static void uhhhh1(Actor __instance, int category)
        //{
        //    Log.LogMessage($"precalc {__instance.name} {category} {__instance.CommandState}");
        //}

        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(ActionPoint), nameof(ActionPoint.GetTypeCommandList), new[] { typeof(byte), typeof(RG.Define.Action.Forms), typeof(int), typeof(List<ActionCommand>) })]
        //private static void GetTypeCommandListPost(ActionPoint __instance, int type, List<ActionCommand> commands)
        //{
        //    Log.LogMessage($"gettypecommandlist {__instance.name} {type}");
        //}
        //{
        //    //if (__instance.AttachedActor)
        //    //{
        //    //    Log.LogMessage($"gettypecommandlist {__instance.name} {__instance.AttachedActor.name} {type}");
        //    //    foreach(ActionCommand cmd in commands)
        //    //    {
        //    //        Log.LogMessage(Util.GetActionName(cmd, __instance.AttachedActor));
        //    //    }
        //    //    Log.LogMessage("----");
        //    //}

        //    if (__instance.name.Contains("enter"))
        //    {
        //        //Log.LogMessage($"{__instance.name} {type}");
        //        //foreach(ActionCommand cmd in commands)
        //        //{
        //        //    Log.LogMessage(Util.GetActionName(cmd, __instance.AttachedActor));
        //        //}
        //        //Log.LogMessage("----");

        //        //Log.LogMessage("diconpoint");
        //        //for (int i = 0; i < __instance._dicCommandListOnPoint.Count; i++)
        //        //{
        //        //    Il2CppReferenceArray<List<ActionCommand>> list1 = __instance._dicCommandListOnPoint[i];
        //        //    for (int j = 0; j < list1.Count; j++)
        //        //    {
        //        //        List<ActionCommand> list2 = list1[j];
        //        //        for (int k = 0; k < list2.Count; k++)
        //        //        {
        //        //            ActionCommand cmd = list2[k];
        //        //            Log.LogMessage($"{i} {j} {k} {Util.GetActionName(cmd, __instance.AttachedActor)}");
        //        //        }
        //        //    }
        //        //}
        //        //Log.LogMessage("-----");
        //    }

        //}

        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(Actor), nameof(Actor.InitializeCommands))]
        //private static void testing(Actor __instance)
        //{
        //    Log.LogMessage($"init {__instance.name} {__instance.CommandState}");
        //    //if (__instance.OccupiedActionPoint != null)
        //    //{
        //    //    bool found = false;
        //    //    foreach (ActionPoint enterPoint in Game.ActionMap.APTContainer._enter)
        //    //    {
        //    //        if (__instance.OccupiedActionPoint.UniqueID == enterPoint.UniqueID)
        //    //        {
        //    //            found = true;
        //    //            break;
        //    //        }
        //    //    }

        //    //    if (found)
        //    //    {
        //    //        Log.LogMessage($"guest {__instance.name}");
        //    //        int i = 0;
        //    //        while (true)
        //    //        {
        //    //            ActionCommand cmd;
        //    //            try
        //    //            {
        //    //                cmd = __instance._default.Commands[i];
        //    //            }
        //    //            catch (Exception)
        //    //            {
        //    //                break;
        //    //            }
        //    //            string name = Util.GetActionName(cmd, __instance);
        //    //            Log.LogMessage(name);
        //    //            __instance._summonCommands.Add(cmd);
        //    //            i++;
        //    //        }
        //    //        Log.LogMessage("-----");
        //    //    }
        //    //}

        //    //foreach (ActionPoint point in Game.ActionMap.APTContainer._actionPoints)
        //    //{
        //    //    Log.LogMessage(point.name);
        //    //}
        //}

        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(Actor), nameof(Actor.PrecalcCommands))]
        //private static void testing2(Actor __instance, int category, bool filter)
        //{
        //    Log.LogMessage($"precalc {__instance.name} {category} {filter}");
        //}

        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(ActionPoint), nameof(ActionPoint.GetCommandListOnPoint))]
        //private static void testing2(ActionPoint __instance, byte sex, RG.Define.Action.Forms form, int attrType)
        //{
        //    Log.LogMessage($"getcommandlistonpoint {__instance.name} {sex} {form} {attrType}");
        //}

        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(ActionPointContainer), nameof(ActionPointContainer.AppropriateEnterPoint), new[] { typeof(Actor) })]
        //private static void testing(ActionPointContainer __instance, Actor actor, ActionPoint __result)
        //{
        //    Log.LogMessage($"appropriateenterpoint {actor.name} {actor.OccupiedActionPoint?.name} {__result?.name}");

        //    Log.LogMessage("enter points");
        //    foreach(ActionPoint point in __instance._enter)
        //    {
        //        Log.LogMessage($"{point.name} {point.UniqueID}");
        //    }
        //    Log.LogMessage("-----");
        //}
    }
}
