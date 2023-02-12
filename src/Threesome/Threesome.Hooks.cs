using HarmonyLib;
using RG.Scene;
using RG.Scene.Action.Core;
using RG.Scripts;
using Il2CppSystem.Collections.Generic;
using BepInEx.Logging;
using UnhollowerBaseLib;

namespace RGActionPatches.Threesome
{
    class Hooks
    {
        private static ManualLogSource Log = RGActionPatchesPlugin.Log;
        internal static string GUID = RGActionPatchesPlugin.GUID + ".Threesome";

        // Add MMF command in workplace
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Actor), nameof(Actor.FilterCommands))]
        private static void FilterCommandsPre(Actor __instance, IReadOnlyList<ActionCommand> commands, List<ActionCommand> dest)
        {
            Patches.SpoofBadFriendInPrivateRoom(__instance);
            Patches.PatchThreesomeInPublicMap(ActionScene.Instance, __instance);
        }

        //Restore the Status after the command list is populated
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Actor), nameof(Actor.FilterCommands))]
        private static void FilterCommandsPost(Actor __instance, IReadOnlyList<ActionCommand> commands, List<ActionCommand> dest)
        {
            StateManager.Instance.restoreSpoofedActors();
        }

        //Populate the H target list for the male character who is standing at the bad friend point
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ActionScene), nameof(ActionScene.GetActorHTargetCommandList))]
        private static void GetActorHTargetCommandList(ActionScene __instance, Actor actor, List<ActionCommand> commandList)
        {
            Patches.GetSpoofedBadFriendHTargetList(__instance, actor, commandList);
        }

        //for unknown reason the option list is not populated for a character with a different job id even it is changed to bad friend
        //manually create the list for the private room case
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ActionScene), nameof(ActionScene.GetActorMMFTargetCommandList))]
        private static void GetActorMMFTargetCommandListPost(ActionScene __instance, Actor actor, List<ActionCommand> commandList)
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
        private static void GetActorFFMTargetCommandListPost(ActionScene __instance, Actor actor, List<ActionCommand> commandList)
        {
            Patches.UpdateFFMTargetCommandList(__instance, actor, commandList);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(HScene), nameof(HScene.StartPointSelect))]
        private static void StartPointSelectPre(HScene __instance, ref int hpointLen, ref Il2CppReferenceArray<HPoint> hPoints, int checkCategory, HScene.AnimationListInfo info)
        {
            if (hpointLen == 0)
            {
                HPoint point = Patches.Find3PStartPoint(__instance);
                if (point != null)
                {
                    hPoints = new Il2CppReferenceArray<HPoint>(1);
                    hPoints[0] = point;
                    hpointLen = 1;
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(HScene), nameof(HScene.CheckHpoint))]
        private static void CheckHPointPre(HScene __instance, ref Il2CppReferenceArray<HPoint> hPoints)
        {
            if (hPoints.Count == 0)
            {
                HPoint point = Patches.Find3PStartPoint(__instance);
                if (point != null)
                {
                    hPoints = new Il2CppReferenceArray<HPoint>(1);
                    hPoints[0] = point;
                }
            }
        }
    }
}
