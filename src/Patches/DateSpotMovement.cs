using RG.Scene;
using RG.Scene.Action.Core;
using RG.Scene.Action.Settings;
using Il2CppSystem.Collections.Generic;
using UnityEngine;
using BepInEx.Logging;

namespace RGActionPatches.Patches
{
    class DateSpotMovement
    {
        private static ManualLogSource Log = RGActionPatchesPlugin.Log;
        private static bool isDateSpot(ActionSettings settings, int mapID)
        {
            ActionSettings.MapIDs MapIDs = settings.MapID;
            return mapID == MapIDs.Cafe || mapID == MapIDs.Restaurant || mapID == MapIDs.Park;
        }

        internal static void doSeatSwap(Actor actor, Actor target, ActionScene scene)
        {
            if (isDateSpot(scene._actionSettings, scene.MapID) && actor.OccupiedActionPoint != target.OccupiedActionPoint.Pairing)
            {
                // if another actor had the destination spot spot or if they're leaving someone 
                // who's in the bathroom to move to another table, do a seat swap
                foreach (Actor a in scene._actors)
                {
                    bool swapConditions = (
                        a.PostedActionPoint == target.OccupiedActionPoint.Pairing || (
                            actor.PostedActionPoint.Pairing != null &&
                            a.PostedActionPoint == actor.PostedActionPoint.Pairing
                        )
                    );
                    if (swapConditions)
                    {
                        a.PostedActionPoint = actor.PostedActionPoint;
                        a.StashedPartner = null;
                        break;
                    }
                }
            }
        }

        internal static void redirectActorToPairedPoint(Actor actor, Actor target, ActionScene scene)
        {
            if (isDateSpot(scene._actionSettings, scene.MapID))
            {
                // if the spot across from the target is available
                // and if the actor isn't already sitting there
                ActionPoint pairingPoint = target.OccupiedActionPoint.Pairing;
                if (pairingPoint != null && pairingPoint.IsAvailable() && actor.OccupiedActionPoint != pairingPoint)
                {
                    // override the actor's movement target and their action state
                    IReadOnlyList<Transform> dest = pairingPoint.Destination;
                    actor.PopScheduledPoint();
                    actor.PushSchedulingPoint(pairingPoint);
                    actor.SetDestination(dest[0].position);
                    actor.ChangeState(RG.Define.StateID.GoToDestination, true);
                }
            }
        }

        internal static void handleArrivalAfterRedirect(Actor actor, ActionScene scene)
        {
            ActionPoint currentPoint = actor.OccupiedActionPoint;

            // should only run in date spots, should only run after moving to a spot across
            // from another actor, and should not run when the actor is returning to a spot
            bool conditions = (
                isDateSpot(scene._actionSettings, scene.MapID) &&
                currentPoint != null &&
                currentPoint != actor.PostedActionPoint &&
                currentPoint.Pairing?.AttachedActor != null
            );

            if (conditions)
            {
                // assign some references to clean things up
                Actor target = currentPoint.Pairing?.AttachedActor;
                actor.PostedActionPoint = currentPoint;
                actor.MakePair(target);
                actor.TalkSidePosition = SidePosition.Facing;
                target.TalkSidePosition = SidePosition.Facing;
                // then reinitiate the talk
                actor.ChangeState(RG.Define.StateID.TalkStart, true);
            }
        }
    }
}
