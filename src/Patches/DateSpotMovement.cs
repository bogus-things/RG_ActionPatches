using RG.Scene;
using RG.Scene.Action.Core;
using RG.Scene.Action.Settings;
using Il2CppSystem.Collections.Generic;
using UnityEngine;


namespace RGActionPatches.Patches
{
    class DateSpotMovement
    {
        private static bool isDateSpot(ActionSettings settings, int mapID)
        {
            ActionSettings.MapIDs MapIDs = settings.MapID;
            return mapID == MapIDs.Cafe || mapID == MapIDs.Restaurant || mapID == MapIDs.Park;
        }

        internal static void redirectActorToPairedPoint(Actor actor, Actor target, ActionScene scene)
        {
            if (isDateSpot(scene._actionSettings, scene.MapID))
            {
                // if the spot across from the target is available
                // (and if the actor isn't already sitting there)
                ActionPoint pairingPoint = target.OccupiedActionPoint.Pairing;
                if (pairingPoint != null && pairingPoint.IsAvailable() && actor.OccupiedActionPoint != pairingPoint)
                {
                    // override the actor's movement target and their action state
                    IReadOnlyList<Transform> dest = pairingPoint.Destination;
                    actor.PopScheduledPoint();
                    actor.PushSchedulingPoint(pairingPoint);
                    actor.SetDestination(dest[0].position);
                    actor.SetDestinationForce(dest[1].position);
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
                // if another actor had that spot, do a seat swap
                foreach(Actor a in scene._actors)
                {
                    if (a.PostedActionPoint == currentPoint)
                    {
                        a.PostedActionPoint = actor.PostedActionPoint;
                        break;
                    }
                }

                // assign some point references to clean things up
                actor.PostedActionPoint = currentPoint;
                ActionScene.PairActorAndPoint(actor, actor.OccupiedActionPoint);
                // then reinitiate the talk
                actor.TalkTo(currentPoint.Pairing.AttachedActor);
            }
        }
    }
}
