using RG;
using RG.Scene.Action.Core;
using RG.Scripts;
using System;
using Il2CppSystem.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Manager;
using UnityEngine;
using RG.Scene;

namespace RGActionPatches.Guests
{
    class Patches
    {
        private static System.Collections.Generic.Dictionary<string, int> PointIDMap = new System.Collections.Generic.Dictionary<string, int>()
        {
            { "f_u_4_00", 9 },
            { "f_u_4_01", 10 }
        };

        internal static void ChangeCommandStates(IReadOnlyList<Actor> actors)
        {
            int i = 0;
            while (true)
            {
                Actor a;
                try
                {
                    a = actors[i];
                }
                catch (Exception)
                {
                    break;
                }

                if (a.CommandState == Define.Action.CommandState.Welcome)
                {
                    a.CommandState = Util.ActorIsOnEntryPoint(a) && a.Partner == null ? Define.Action.CommandState.Neutral : Define.Action.CommandState.Communication;
                }

                i++;
            }
        }

        internal static int HandleJobCallRedirect(ActionScene scene, Actor actor, int state)
        {
            int newState = state;
            if (!scene._actionSettings.IsJobMap(scene.MapID))
            {
                return newState;
            }

            if (state == 15) // SummonOutsider
            {
                StateManager.Instance.userControlledActor = actor;
            }
            else if (state == 0 && Util.ActorIsOnEntryPoint(actor)) // idle on the entry point
            {
                if (PointIDMap.TryGetValue(actor.name, out int jobPointID))
                {
                    ActionPoint jobPoint = Game.ActionMap.APTContainer.FindFromUniID(jobPointID);
                    Transform dest = jobPoint?.Destination[0];
                    if (dest == null)
                    {
                        dest = jobPoint?.DestinationToTalk[0];
                    }
                    if (dest != null)
                    {
                        actor.PopScheduledPoint();
                        actor.PushSchedulingPoint(jobPoint);
                        actor.SetDestination(dest.position);
                        newState = 2; // GoToDestination
                        StateManager.Instance.redirectedGuest = actor;
                    }
                }
            }
            else if (state == 4 && actor.InstanceID == StateManager.Instance.userControlledActor?.InstanceID) // GoToSideCharacter after summon
            {
                if (StateManager.Instance.redirectedGuest != null)
                {
                    newState = 0;
                    actor.Animation.Param._exitTimeMax = 0f;
                }
                else
                {
                    StateManager.Instance.userControlledActor = null;
                }
            }

            return newState;
        }

        internal static void HandleJobCallArrival(ActionScene scene, Actor actor)
        {
            if (scene._actionSettings.IsJobMap(scene.MapID) && actor.InstanceID == StateManager.Instance.redirectedGuest?.InstanceID)
            {
                Actor caller = StateManager.Instance.userControlledActor;
                StateManager.Instance.redirectedGuest = null;
                StateManager.Instance.userControlledActor = null;
                caller.CompleteAction();
                caller.TalkTo(actor);
            }
        }
    }
}
