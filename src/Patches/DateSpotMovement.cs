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
                // (includes a check whether actor is already there to prevent infinite looping)
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
            if (isDateSpot(scene._actionSettings, scene.MapID))
            {
                ActionPoint currentPoint = actor.OccupiedActionPoint;
                // pairing here seems to clean up the actor/point state
                ActionScene.PairActorAndPoint(actor, actor.OccupiedActionPoint);
                if (currentPoint && currentPoint.Pairing && currentPoint.Pairing.AttachedActor)
                {
                    // resend the talk action so the convo starts on arrival
                    actor.TalkTo(currentPoint.Pairing.AttachedActor);
                }
            }
        }
    }
}
