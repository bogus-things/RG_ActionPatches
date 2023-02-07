using RG;
using RG.Scene.Action.Core;
using RG.Scripts;
using System;
using Il2CppSystem.Collections.Generic;
using Manager;
using UnityEngine;
using RG.Scene;
using BepInEx.Logging;
using RG.Scene.Action.Settings;
using Illusion.Collections.Generic.Optimized;
using RG.User;

namespace RGActionPatches.Guests
{
    class Patches
    {
        private static ManualLogSource Log = RGActionPatchesPlugin.Log;
        private static System.Collections.Generic.Dictionary<string, int> JobPointIDMap = new System.Collections.Generic.Dictionary<string, int>()
        {
            // office
            { "f_u_0_00", 0 },
            { "f_u_0_01", 1 },
            { "f_u_0_02", 2 },
            { "m_u_0_00", 3 },
            { "m_u_0_01", 4 },
            { "m_u_0_02", 7 },
            // clinic
            { "f_u_1_00", 11 },
            { "f_u_1_01", 12 },
            { "f_u_1_02", 13 },
            { "m_u_1_00", 19 },
            { "m_u_1_01", 15 },
            { "m_u_1_02", 16 },
            // seminar
            { "f_u_2_00", 8 },
            { "f_u_2_01", 9 },
            { "m_u_2_00", 11 },
            { "m_u_2_01", 10 },
            { "m_u_2_02", 12 },
            // club
            { "f_u_3_00", 10 },
            { "f_u_3_01", 11 },
            { "m_u_3_00", 12 },
            { "m_u_3_01", 13 },
            { "m_u_3_02", 14 },
            // casino
            { "f_u_4_00", 9 },
            { "f_u_4_01", 10 },
            { "m_u_4_00", 15 },
            { "m_u_4_01", 13 },
            { "m_u_4_02", 20 },
            // living room
            { "f_u_5_00", 21 },
            { "m_u_5_00", 22 }
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

                if (a.CommandState == Define.Action.CommandState.Welcome && Util.ActorIsOnEntryPoint(a) && a.Partner == null)
                {
                    a.CommandState = Define.Action.CommandState.Neutral;
                }

                i++;
            }
        }

        internal static int HandleGuestRedirect(ActionScene scene, Actor actor, int state)
        {
            int newState = state;
            ActionSettings settings = scene._actionSettings;
            if (!settings.IsJobMap(scene.MapID))
            {
                return newState;
            }

            if (state == 15) // SummonOutsider
            {
                StateManager.Instance.userControlledActor = actor;
            }
            else if (StateManager.Instance.guestActor?.InstanceID == actor.InstanceID && state == 0 && Util.ActorIsOnEntryPoint(actor)) // idle on the entry point
            {
                if (StateManager.Instance.livingRoomGuestSpoof)
                {
                    StateManager.Instance.livingRoomGuestSpoof = false;
                }
                else if ((actor.JobID == settings.FindJobID(scene.MapID) && !settings.IsPrivate(scene.MapID)) && JobPointIDMap.TryGetValue(actor.name, out int jobPointID))
                {
                    ActionPoint jobPoint = Game.ActionMap.APTContainer.FindFromUniID(jobPointID);
                    Transform dest = jobPoint?.Destination[0];
                    if (dest == null)
                    {
                        dest = jobPoint?.DestinationToTalk[0];
                    }
                    if (dest != null)
                    {
                        StateManager.Instance.redirectedGuestActor = StateManager.Instance.guestActor;
                        actor.PopScheduledPoint();
                        actor.PushSchedulingPoint(jobPoint);
                        actor.SetDestination(dest.position);
                        newState = 2; // GoToDestination
                    }
                }
                else if (StateManager.Instance.badfriendSpoof && TryGetBadfriendPointID(scene, out int badfriendPointID))
                {
                    ActionPoint badfriendPoint = Game.ActionMap.APTContainer.FindFromUniID(badfriendPointID);
                    Transform dest = badfriendPoint?.Destination[0];
                    if (dest == null)
                    {
                        dest = badfriendPoint?.DestinationToTalk[0];
                    }
                    if (dest != null)
                    {
                        //Make the male actor to go to the bad friend point properly
                        ActionScene.PairActorAndPoint(actor, badfriendPoint);
                        ActionScene.SetPostedPointIntoActor(actor, badfriendPoint);

                        actor.PopScheduledPoint();
                        actor.PushSchedulingPoint(badfriendPoint);
                        actor.SetDestination(dest.position);
                        newState = 2; // GoToDestination
                    }
                    StateManager.Instance.badfriendSpoof = false;
                }
                StateManager.Instance.guestActor = null;
            }
            else if (state == 4 && actor.InstanceID == StateManager.Instance.userControlledActor?.InstanceID) // GoToSideCharacter after summon
            {
                if (StateManager.Instance.redirectedGuestActor != null)
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
            if (scene._actionSettings.IsJobMap(scene.MapID))
            {
                if (actor.InstanceID == StateManager.Instance.redirectedGuestActor?.InstanceID)
                {
                    Actor caller = StateManager.Instance.userControlledActor;
                    StateManager.Instance.redirectedGuestActor = null;
                    StateManager.Instance.userControlledActor = null;

                    ActionScene.PairActorAndPoint(actor, actor.OccupiedActionPoint);
                    ActionScene.SetPostedPointIntoActor(actor, actor.OccupiedActionPoint);

                    foreach (ActionPoint entryPoint in Game.ActionMap.APTContainer._enter)
                    {
                        if (entryPoint.KeptActor?.InstanceID == actor.InstanceID)
                        {
                            entryPoint.AttachedActor = null;
                            entryPoint._keptActor = null;
                            entryPoint._prevAttachedActor = null;
                        }
                    }

                    if (caller != null)
                    {
                        caller.CompleteAction();
                        //Special case: in private room do not go to talk with the entering guest as talking in bad friend action point is bugged
                        if (!scene._actionSettings.IsPrivate(scene.MapID))
                            caller.TalkTo(actor);
                    }
                }
            }
        }

        internal static void SpoofEntryCommandList(ActionScene scene, ActionPoint point, byte sex, Define.Action.Forms form, int type, List<ActionCommand> commands)
        {
            if (Util.IsEntryPoint(point))
            {
                ActionPoint spoofPoint = GetSpoofPoint(scene);
                if (spoofPoint != null)
                {
                    ActionCommand leave = commands.Find(Util.GetFindPredicate(point.AttachedActor, Captions.Actions.Leave));
                    commands.Clear();
                    spoofPoint.GetTypeCommandList(sex, form, type, commands);
                    if (leave != null)
                    {
                        commands.Add(leave);
                    }
                }
            }
        }

        internal static void DoLivingRoomSpoof(ActionScene scene, Actor actor)
        {
            if (scene.MapID == scene._actionSettings.MapID.LivingRoom && actor.JobID != scene._actionSettings.FindJobID(scene.MapID))
            {
                Util.SpoofJobID(scene, actor);
                StateManager.Instance.livingRoomGuestSpoof = true;
            }
        }

        internal static void AddGuestToVisitors(ActionScene scene, Actor actor)
        {

            Int32KeyDictionary<MemberInfoList> visitors = Game.UserFile.DicVisitors;
            MemberInfoList infoList = null;
            foreach (int mapID in visitors.Keys)
            {
                if (mapID == ActionScene.Instance.MapID)

                {
                    infoList = visitors[mapID];
                    break;
                }
            }
            if (infoList == null)
            {
                infoList = new MemberInfoList();
                visitors.Add(ActionScene.Instance.MapID, infoList);
            }
            infoList.Add(actor.MyMemberInfo);


        }

        //need to add to guest dict if the entering character is going to occupy the bad friend action point
        internal static void AddPrivateRoomGuestToVisitors(ActionScene scene, Actor actor)
        {
            if (scene._actionSettings.IsPrivate(scene.MapID))
            {
                //For bad friend character, do not add to guest list or it will render duplicate actor when re-entering the scene
                if (scene._actors.Count > 2 && actor.JobID != (int)Define.JobID.Badfriend)
                {
                    AddGuestToVisitors(scene, actor);
                }
            }
        }

        internal static void AddJobMapGuestToVisitors(ActionScene scene, Actor actor)
        {
            if (scene._actionSettings.IsJobMap(scene.MapID) && !scene._actionSettings.IsPrivate(scene.MapID))
            {
                int mapJobID = scene._actionSettings.FindJobID(scene.MapID);
                if (actor.JobID > -1 && actor.JobID != mapJobID)
                {
                    AddGuestToVisitors(scene, actor);
                }
            }
        }

        internal static void SpoofEntryAutoCommands(ActionScene scene, Actor actor)
        {
            if (scene._actionSettings.IsJobMap(scene.MapID) && Util.ActorIsOnEntryPoint(actor))
            {
                List<ActionCommand> talkCommands = GetAutoSpoofCommands(scene, actor.Sex);

                actor.OccupiedActionPoint._autoCommands[actor.Sex][0].Clear();
                foreach (ActionCommand cmd in talkCommands)
                {
                    if (cmd.Info.ActionType == 5 && cmd.Info.ActionType == 1)
                    {
                        actor.OccupiedActionPoint._autoCommands[actor.Sex][0].Add(cmd);
                    }
                }
            }
        }

        internal static void OverrideGuestAutoDecision(ActionScene scene, Actor actor)
        {
            if (scene._actionSettings.IsJobMap(scene.MapID))
            {
                if (Util.ActorIsOnEntryPoint(actor) && actor.ActionInfo.ActionType == 1)
                {
                    actor.Animation.Param._exitTimeMax = 0f;
                    actor.ChangeState(Define.StateID.Idle, true);

                    System.Random rnd = new System.Random();
                    Actor talkTarget = scene._actors[rnd.Next(0, scene._actors.Count)];
                    bool talkConditions = (
                        talkTarget.InstanceID != actor.InstanceID &&
                        talkTarget.CommandState == Define.Action.CommandState.Neutral &&
                        (talkTarget.ActionInfo.ActionType >= -1 && talkTarget.ActionInfo.ActionType <= 1)
                    );
                    if (talkConditions)
                    {
                        ActionPoint target = talkTarget.OccupiedActionPoint;
                        Transform dest = target?.DestinationToTalk[0];
                        if (dest != null)
                        {
                            talkTarget.ChangeState(Define.StateID.Idle, true);
                            talkTarget.TalkReserver = actor;
                            actor.ReservedTalkTarget = talkTarget;
                            actor.PopScheduledPoint();
                            actor.PushSchedulingPoint(target);
                            actor.SetDestination(dest.position);
                            ActionScene.ReserveAction(actor, Define.StateID.GoSideCharacter, true);
                        }
                    }
                }
                else if (actor.TalkReserver != null && Util.IsEntryPoint(actor.TalkReserver.PostedActionPoint))
                {
                    actor.Animation.Param._exitTimeMax = 0f;
                    actor.ChangeState(Define.StateID.Idle, true);
                }
            }
        }

        private static ActionPoint GetSpoofPoint(ActionScene scene)
        {
            int mapID = scene.MapID;
            ActionSettings.MapIDs mapIDs = scene._actionSettings.MapID;
            ActionPoint point = null;
            if (mapID == mapIDs.Office)
            {
                point = Game.ActionMap.APTContainer.FindFromUniID(18);
            }
            else if (mapID == mapIDs.Clinic)
            {
                point = Game.ActionMap.APTContainer.FindFromUniID(25);
            }
            else if (mapID == mapIDs.Seminar)
            {
                point = Game.ActionMap.APTContainer.FindFromUniID(18);
            }
            else if (mapID == mapIDs.Club)
            {
                point = Game.ActionMap.APTContainer.FindFromUniID(5);
            }
            else if (mapID == mapIDs.Casino)
            {
                point = Game.ActionMap.APTContainer.FindFromUniID(7);
            }
            else if (mapID == mapIDs.LivingRoom)
            {
                point = Game.ActionMap.APTContainer.FindFromUniID(18);
            }
            return point;
        }

        private static List<ActionCommand> GetAutoSpoofCommands(ActionScene scene, byte sex)
        {
            int mapID = scene.MapID;
            ActionSettings.MapIDs mapIDs = scene._actionSettings.MapID;
            ActionPoint point = null;
            string nameToSpoof = null;
            if (mapID == mapIDs.Office)
            {
                nameToSpoof = sex == 0 ? "m_u_0_00" : "f_u_0_00";
            }
            else if (mapID == mapIDs.Clinic)
            {
                nameToSpoof = sex == 0 ? "m_u_1_00" : "f_u_1_00";
            }
            else if (mapID == mapIDs.Seminar)
            {
                nameToSpoof = sex == 0 ? "m_u_2_00" : "f_u_2_00";
            }
            else if (mapID == mapIDs.Club)
            {
                nameToSpoof = sex == 0 ? "m_u_3_00" : "f_u_3_00";
            }
            else if (mapID == mapIDs.Casino)
            {
                nameToSpoof = sex == 0 ? "m_u_4_00" : "f_u_4_00";
            }
            else if (mapID == mapIDs.LivingRoom)
            {
                nameToSpoof = sex == 0 ? "m_u_5_00" : "f_u_5_00";
            }

            if (nameToSpoof != null && JobPointIDMap.TryGetValue(nameToSpoof, out int pointID))
            {
                point = Game.ActionMap.APTContainer.FindFromUniID(pointID);
            }
            return point._autoCommands[sex][0];
        }

        private static bool TryGetBadfriendPointID(ActionScene scene, out int pointID)
        {
            int mapID = scene.MapID;
            ActionSettings.MapIDs mapIDs = scene._actionSettings.MapID;
            pointID = -1;
            if (mapID == mapIDs.OldApartment) // Cheap apartment
            {
                pointID = 20;
            }
            else if (mapID == mapIDs.LargeApartment) // Condominium
            {
                pointID = 22;
            }
            else if (mapID == mapIDs.Apartment) // Apartment
            {
                pointID = 22;
            }
            else if (mapID == mapIDs.HighRiseCondo) // Luxury apartment
            {
                pointID = 20;
            }
            else if (mapID == mapIDs.ComDorm) // Employee dormitory
            {
                pointID = 21;
            }
            else if (mapID == mapIDs.OwnRoom) // Private room
            {
                pointID = 12;
            }
            
            return pointID > -1;
        }

        internal static void DoBadfriendSpoof(ActionScene scene, Actor actor)
        {
            if (scene._actionSettings.IsPrivate(scene.MapID) && scene._actors.Count > 2)
            {
                actor._status.JobID = (int)Define.JobID.Badfriend;
                StateManager.Instance.badfriendSpoof = true;
            }
        }

        internal static void RemoveGuestsDoNotBelongToScene(ActionScene scene, List<Actor> actors)
        {
            if (scene._actionSettings.IsPrivate(scene.MapID))
            {
                for (var n = actors.Count - 1; n >= 0; n--)
                {
                    if (actors[n].SubMapID != scene.PrivateKeyID)
                    {
                        scene.Unload(actors[n]);
                        actors.RemoveAt(n);
                    }
                }
            }
        }

        //////Obsolete as a more straight forward method is found
        ////internal static void AlterGuestDictionaryWhenEnteringMap(int mapID, int subMapID)
        ////{
        ////    if (Util.IsPrivateMap(mapID))
        ////    {
        ////        if (Manager.Game.UserFile.DicVisitors.ContainsKey(mapID))
        ////        {
        ////            RG.User.MemberInfoList list = Manager.Game.UserFile.DicVisitors[mapID];
        ////            //Back up the list
        ////            for (int i = 0; i < list.Count; i++)
        ////            {
        ////                StateManager.Instance.privateRoomGuestListBackup.Add(Util.CloneMemberInfo(list[i]));
        ////            }
        ////            //Now remove the item from DicVisitor if the sub map ID not match
        ////            for (int n = list.Count - 1; n >= 0; n--)
        ////            {
        ////                foreach (var kvpStatus in Manager.Game.UserFile.DicStatusArchive[mapID])
        ////                {
        ////                    RG.User.Status status = kvpStatus.Value;
        ////                    if (list[n].IsSame(status))
        ////                    {
        ////                        if (status.SubMapID != subMapID)
        ////                        {
        ////                            //Remove the item from DicVisitor due to submap id not match so that it wont load the wrong guest
        ////                            list.RemoveAt(n);
        ////                        }
        ////                        break;
        ////                    }
        ////                }
        ////            }
        ////        }
        ////    }
        ////}

        //////Obsolete as a more straight forward method is found
        ////internal static void RecoverGuestDictionary()
        ////{
        ////    //add back the member info once the map is loaded
        ////    if (ActionScene.IsCurrentPrivateMap())
        ////    {
        ////        var mapIDs = ActionScene.Instance._actionSettings._mapIDs;
        ////        if (StateManager.Instance.privateRoomGuestListBackup.Count > 0)
        ////        {
        ////            Manager.Game.UserFile.DicVisitors.Remove(ActionScene.Instance.MapID);
        ////            Manager.Game.UserFile.DicVisitors[ActionScene.Instance.MapID] = StateManager.Instance.privateRoomGuestListBackup;

        ////            StateManager.Instance.privateRoomGuestListBackup = new RG.User.MemberInfoList();

        ////        }
        ////    }
        ////}
    }
}
