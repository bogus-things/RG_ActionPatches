using Il2CppSystem.Collections.Generic;
using BepInEx.Logging;
using RG.Scene.Action.Settings;
using RG.Scene.Action.Core;
using RG.Scene;
using UnityEngine;

namespace RGActionPatches.DateSpotMovement
{
    class Patches
    {
        private static ManualLogSource Log = RGActionPatchesPlugin.Log;
        internal static bool IsDateSpot(ActionSettings settings, int mapID)
        {
            ActionSettings.MapIDs MapIDs = settings.MapID;
            return mapID == MapIDs.Cafe || mapID == MapIDs.Restaurant || mapID == MapIDs.Park;
        }

        internal static void HandleTalkMovement(ActionScene scene, Actor actor, RG.Define.StateID stateID)
        {
            if (IsDateSpot(scene._actionSettings, scene.MapID) && stateID == RG.Define.StateID.GoToDestination)
            {
                Actor target = actor.RecentScheduledPoint?.AttachedActor;
                ActionPoint pairing = actor.RecentScheduledPoint?.Pairing;
                if (target != null && pairing != null)
                {
                    DoSeatSwap(actor, target, ActionScene.Instance);
                    RedirectActorToPairedPoint(actor, target, ActionScene.Instance);
                }
            }
        }

        internal static void DoSeatSwap(Actor actor, Actor target, ActionScene scene)
        {
            if (IsDateSpot(scene._actionSettings, scene.MapID) && actor.OccupiedActionPoint != target.OccupiedActionPoint.Pairing)
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

        internal static void RedirectActorToPairedPoint(Actor actor, Actor target, ActionScene scene)
        {
            if (IsDateSpot(scene._actionSettings, scene.MapID))
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

        internal static void HandleArrivalAfterRedirect(Actor actor, ActionScene scene)
        {
            ActionPoint currentPoint = actor.OccupiedActionPoint;

            // should only run in date spots, should only run after moving to a spot across
            // from another actor, and should not run when the actor is returning to a spot
            bool conditions = (
                IsDateSpot(scene._actionSettings, scene.MapID) &&
                currentPoint != null &&
                currentPoint != actor.PostedActionPoint &&
                currentPoint.Pairing?.AttachedActor != null
            );

            if (conditions)
            {
                // assign some references to clean things up
                Actor target = currentPoint.Pairing?.AttachedActor;
                ActionScene.UnpairActorAndPoint(actor, actor.PostedActionPoint);
                ActionScene.SetPostedPointIntoActor(actor, currentPoint);
                ActionScene.PairActorAndPoint(actor, currentPoint);
                actor.TalkSidePosition = SidePosition.Facing;
                target.TalkSidePosition = SidePosition.Facing;
                actor.MakePair(target);

                // then reinitiate the talk

                // if this arrival is the result of a user-controlled action,
                // reinitiate by changing some states then clear the state manager
                if (StateManager.Instance.userControlledActor?.InstanceID == actor.InstanceID)
                {
                    actor.ChangeState(RG.Define.StateID.TalkStart);
                    target.ChangeState(RG.Define.StateID.Empty);

                    StateManager.Instance.userControlledActor = null;
                }
                else // if it's the result of auto-movement, start up a talk action using the scene
                {
                    ActionScene.TalkAfterAction(actor);
                }
            }
        }

        internal static void SpoofMapID(ActionScene scene, Actor actor)
        {
            if (IsDateSpot(scene._actionSettings, scene.MapID))
            {
                actor.MapID = scene._actionSettings.MapID.Casino;
            }
        }
    }
}
