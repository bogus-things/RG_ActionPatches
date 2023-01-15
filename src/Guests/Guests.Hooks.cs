﻿using BepInEx.Logging;
using HarmonyLib;
using RG.Scene;
using RG.Scene.Action.Core;
using Il2CppSystem.Collections.Generic;
using RG.Scripts;
using RG;

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

        // Track the entering guest for some future handling & spoof guest job ID
        // in living room to bypass buggy animation
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ActionScene), nameof(ActionScene.PopGuest))]
        private static void PopGuestPre(Actor actor, ref (int, int, int) __state)
        {
            ActionScene scene = ActionScene.Instance;
            StateManager.Instance.guestActor = actor;
            __state = (actor.JobID, actor.PrivateID, actor.WorkplaceID);

            Patches.DoBadfriendSpoof(scene, actor);
            Patches.DoLivingRoomSpoof(scene, actor);
        }

        // Restore job IDs and such, then update visitors dicts so people don't disappear
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ActionScene), nameof(ActionScene.PopGuest))]
        private static void PopGuestPost(Actor actor, (int, int, int) __state)
        {
            actor._status.JobID = __state.Item1;
            actor.PrivateID = __state.Item2;
            actor.WorkplaceID = __state.Item3;

            Patches.AddJobMapGuestToVisitors(ActionScene.Instance, actor);
            Patches.AddPrivateRoomGuestToVisitors(ActionScene.Instance, actor);
        }

        // On job maps, override some state changes to allow called actors
        // to go to their usual spots if it's their workplace & private guests to go to badfriend spots
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Actor), nameof(Actor.ChangeState), new[] { typeof(int), typeof(bool) })]
        private static void ChangeStatePre(Actor __instance, ref int stateType)
        {
            stateType = Patches.HandleGuestRedirect(ActionScene.Instance, __instance, stateType);
        }

        // When called workers get to their spots, have the caller go talk
        // to them like usual
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Actor), nameof(Actor.OnArrivedDestination))]
        private static void OnArrivedDestinationPost(Actor __instance)
        {
            Patches.HandleJobCallArrival(ActionScene.Instance, __instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ActionPoint), nameof(ActionPoint.GetTypeCommandList), new[] { typeof(byte), typeof(Define.Action.Forms), typeof(int), typeof(List<ActionCommand>) })]
        private static void GetTypeCommandListPost(ActionPoint __instance, byte sex, Define.Action.Forms form, int type, List<ActionCommand> commands)
        {
            Patches.SpoofEntryCommandList(ActionScene.Instance, __instance, sex, form, type, commands);
        }
    }
}
