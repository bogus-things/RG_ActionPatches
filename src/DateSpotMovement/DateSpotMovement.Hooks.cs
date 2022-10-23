using BepInEx.Logging;
using HarmonyLib;
using RG.Scene;
using RG.Scene.Action.Core;

namespace RGActionPatches.DateSpotMovement
{
    class Hooks
    {
        private static ManualLogSource Log = RGActionPatchesPlugin.Log;

        internal static string GUID = RGActionPatchesPlugin.GUID + ".DateSpotMovement";

        // TalkTo is only called after user selected actions, so we save the user-controlled
        // actor to the state manager for reference upon arrival
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Actor), nameof(Actor.TalkTo))]
        private static void TalkToPre(Actor __instance, Actor target)
        {
            StateManager.Instance.userControlledActor = __instance;
        }

        // When actor gets to the spot across from the target, updates the actor/point references
        // and starts up the conversation
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Actor), nameof(Actor.OnArrivedDestination))]
        private static void OnArrivedDestinationPost(Actor __instance)
        {
            Patches.HandleArrivalAfterRedirect(__instance, ActionScene.Instance);
        }

        // Temporarily fake the actor's MapID before autoplay decides the actions so talk isn't disabled
        // (Used in tandem with AddCommands.Patches.SpoofJobID)
        // (I can't believe this actually works lol)
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ActionScene), nameof(ActionScene.DecideAction))]
        private static void DecideActionPre(ActionScene __instance, Actor actor)
        {
            Patches.SpoofMapID(__instance, actor);
        }

        // Restore the actor's MapID after the decision runs
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ActionScene), nameof(ActionScene.DecideAction))]
        private static void DecideActionPost(ActionScene __instance, Actor actor)
        {
            actor.MapID = __instance.MapID;
        }

        // Intercept GoToSideCharacter actions at date spots and change them to GoToDestination
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ActionScene), nameof(ActionScene.ReserveAction))]
        private static void ReserveActionPre(Actor actor, ref RG.Define.StateID stateID)
        {
            ActionScene scene = ActionScene.Instance;
            if (Patches.IsDateSpot(scene._actionSettings, scene.MapID) && stateID == RG.Define.StateID.GoSideCharacter)
            {
                stateID = RG.Define.StateID.GoToDestination;
            }
        }

        // Overrides the actor's destination to the spot across from the target at date spots
        // Also if headed toward a seat previously taken by someone in the bathroom, or if leaving
        // a table while the other seat's character is in the bathroom to talk to someone else,
        // do a seat swap to avoid any collisions between characters
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ActionScene), nameof(ActionScene.ReserveAction))]
        private static void ReserveActionPost(Actor actor, RG.Define.StateID stateID)
        {
            ActionScene scene = ActionScene.Instance;
            Patches.HandleTalkMovement(scene, actor, stateID);
        }
    }
}
