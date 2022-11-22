using System;
using BepInEx.Logging;
using Il2CppSystem.Collections.Generic;
using Manager;
using RG;
using RG.Scene;
using RG.Scene.Action.Core;
using RG.Scripts;

namespace RGActionPatches.AddCommands
{
    class Patches
    {
        private static ManualLogSource Log = RGActionPatchesPlugin.Log;

        private static readonly System.Collections.Generic.List<string> FemaleCommandsToAdd = new System.Collections.Generic.List<string>()
        {
            Captions.Actions.TakeBath,
            Captions.Actions.DoBathH,
            Captions.Actions.MoveToConferenceRoom,
            Captions.Actions.TalkToSomeone,
            Captions.Actions.OfferMMF
        };

        private static readonly System.Collections.Generic.List<string> MaleCommandsToAdd = new System.Collections.Generic.List<string>()
        {
            Captions.Actions.GoToPoleDanceFront,
            Captions.Actions.DoKitchenH,
            Captions.Actions.DoBathH,
            Captions.Actions.MoveToConferenceRoom,
            Captions.Actions.TalkToSomeone,
            Captions.Actions.OfferMMF
        };

        private static readonly System.Collections.Generic.List<string> JobRestrictedCommands = new System.Collections.Generic.List<string>()
        {
            Captions.Actions.PhysicalCheckup,
            Captions.Actions.CheckTemperature,
            Captions.Actions.TalkToPatient,
            Captions.Actions.Seduce,
            Captions.Actions.MoveToConferenceRoom,
            Captions.Actions.ReturnToRoom
        };

        // Some commands should only be added if their availability conditions are met
        private static readonly System.Collections.Generic.List<string> AvailabilityRestrictedCommands = new System.Collections.Generic.List<string>()
        {
            Captions.Actions.DoKitchenH,
            Captions.Actions.DoBathH
        };


        internal static void UpdateActorCommands(ActionScene scene, Actor actor, IReadOnlyList<ActionCommand> baseCommands, List<ActionCommand> current)
        {
            // Filter out duplicate command names
            DeDupeCommandList(current, actor);

            // Add commands from one of the above ToAdd lists
            Func<ActionCommand, bool> filterPredicate = GetAddFilterPredicate(actor);
            List<ActionCommand> commandsToAdd = Util.ReadOnlyFilter(baseCommands, filterPredicate);
            HashSet<string> existingCommandNames = new HashSet<string>();
            foreach (ActionCommand command in current)
            {
                string name = Util.GetActionName(command, actor);
                if (name != null)
                {
                    existingCommandNames.Add(name);
                }
            }
            foreach (ActionCommand command in commandsToAdd)
            {
                string name = Util.GetActionName(command, actor);
                if (name != null && !existingCommandNames.Contains(name))
                {
                    current.Insert(0, command);
                    existingCommandNames.Add(name);
                }
            }

            // Filter out some job-specific commands visitors shouldn't have
            int mapJobID = scene._actionSettings.FindJobID(actor.MapID);
            bool isVisitor = actor.JobID != mapJobID;

            // Special-case command filtering to prevent some collisions
            // Case 1: moving to the exam chair while a visitor is talking to someone
            bool examChair = false;
            if (scene.MapID == scene._actionSettings.MapID.Clinic)
            {
                ActionPoint examPoint = Game.ActionMap.APTContainer.FindFromUniID(20);
                examChair = examPoint != null && scene.ExistsActorPostedPoint(examPoint);
            }
            // Case 1: moving to the conference room if it's occupied
            bool conferenceRoom = false;
            if (scene.MapID == scene._actionSettings.MapID.Office)
            {
                ActionPoint confPoint = Game.ActionMap.APTContainer.FindFromUniID(14);
                conferenceRoom = confPoint != null && confPoint.AttachedActor != null;
            }

            Func<ActionCommand, bool> removePredicate = GetRemovePredicate(actor, isVisitor, examChair, conferenceRoom);
            current.RemoveAll(removePredicate);
        }

        internal static void AddToPointNeutralCommands(Actor actor, ActionScene scene, int type, List<ActionCommand> current)
        {
            if (type == 3 && scene._actors.Count > 1)
            {
                Func<ActionCommand, bool> predicate = Util.GetFindPredicate(actor, Captions.Actions.TalkToSomeone);
                if (!current.Exists(predicate))
                {
                    int index = Util.ReadOnlyIndexOf(actor._movement.Commands, predicate);
                    if (index > -1)
                    {
                        current.Insert(0, actor._movement.Commands[index]);
                    }
                }
            }
        }

        internal static void AddToPointSocializeCommands(Actor actor, ActionScene scene, int type, List<ActionCommand> current)
        {
            if (type == 5 && scene.MapID == scene._actionSettings.MapID.Office)
            {
                Func<ActionCommand, bool> predicate = Util.GetFindPredicate(actor, Captions.Actions.MoveToConferenceRoom);
                if (!current.Exists(predicate))
                {
                    ActionPoint entry = Game.ActionMap.APTContainer._enter[0];
                    List<ActionCommand> entryCommands = entry._dicCommandListOnPoint[actor.Sex][0];
                    int index = Util.IndexOf(entryCommands, predicate);
                    if (index > -1)
                    {
                        entryCommands[index].Info.ActionName = Captions.Actions.MoveToConferenceRoom;
                        Func<Actor, string> getName = (Actor a) => Captions.Actions.MoveToConferenceRoom;
                        entryCommands[index].Info.GetActionNameCallback = getName;
                        current.Insert(0, entryCommands[index]);
                    }
                }
            }
        }


        internal static void SpoofActorList(ActionScene scene, Actor actor, ref List<Actor> spoofActorList)
        {
            //Only apply to private room
            if (scene._actionSettings.IsPrivate(scene.MapID))
            {
                //Temporary remove all the actors other than the caller from the action scene, so that the full summon list is generated
                //Additional condition on the summon list is applied in the getSummonCommandListPost 
                spoofActorList = new List<Actor>();
                for (var n = scene._actors.Count - 1; n >= 0; n--)
                {

                    if (scene._actors[n].InstanceID != actor.InstanceID)
                    {
                        spoofActorList.Add(scene._actors[n]);
                        scene._actors.RemoveAt(n);

                    }
                }
            }
        }

        internal static void UpdateSummonCommandList(ActionScene scene, Actor actor, List<ActionCommand> commandList, List<Actor> spoofActorList)
        {
            //Only apply to private room
            if (scene._actionSettings.IsPrivate(scene.MapID))
            {
                //Attached the actors back to the scene
                foreach (var a in spoofActorList)
                {
                    scene._actors.Add(a);
                }

                if (scene._actors.Count >= 3)
                {
                    //1. the private room can only allow 3 person max
                    commandList.Clear();
                    actor._summonCommands.Clear();
                }
                else if (scene._actors.Count >= 2)
                {
                    //2. if a character has already taken the position next with the main actor, summoning female into this room is not allowed
                    //   this requirement can be viewed as fulfilling : (a) 2+ characters in the room & (b) no character is reserving in the bad friend action point   
                    if (!Util.IsBadFriendActionPointReserved(scene))
                    {
                        Dictionary<string, RG.User.Status> actorContactList = new Dictionary<string, RG.User.Status>();     //Key: name, Value: Status
                        foreach (Dictionary<string, RG.User.Relation> dictRelation in actor.Status.RelationshipParameter)
                        {
                            foreach (KeyValuePair<string, RG.User.Relation> kvp in dictRelation)
                            {
                                RG.User.Status status = Util.GetStatusFromArchive(kvp.Key);
                                if (status != null)
                                {
                                    if (!actorContactList.ContainsKey(status.FullName))
                                        actorContactList.Add(status.FullName, status);
                                }


                            }
                        }

                        //Scan through the command list and remove the command that summon female character
                        //Note: Currently using name to find out whether the character is female or not since only name is left in the command and no idea on how to create a summon command.
                        //      Will have problem if there are male and female characters with same name in the game.
                        for (var n = commandList.Count - 1; n >= 0; n--)
                        {
                            string charaName = commandList[n].Info.ActionName.Replace(Captions.Actions.SummonSomeone, string.Empty);
                            if (actorContactList.ContainsKey(charaName))
                            {
                                if (actorContactList[charaName].Sex == 1)
                                {
                                    commandList.RemoveAt(n);
                                }
                            }
                        }
                    }
                }

                actor._summonCommands = commandList;
            }
        }

        internal static void UpdateMMFTargetCommandList(ActionScene scene, List<ActionCommand> commandList)
        {
            if (scene._actionSettings.IsPrivate(scene.MapID))
            {
                commandList.Clear();
                if (scene._femaleActors.Count == 1 && scene._maleActors.Count == 2)
                {
                    foreach (var female in scene._femaleActors)
                    {
                        commandList.Add(female.Come2TalkMMFCommand);
                    }
                }
            }
        }

        internal static void UpdateFFMTargetCommandList(ActionScene scene, Actor actor, List<ActionCommand> commandList)
        {
            if (scene._actionSettings.IsPrivate(scene.MapID))
            {
                commandList.Clear();
                if (scene._femaleActors.Count == 2 && scene._maleActors.Count == 1)
                {
                    foreach (var female in scene._femaleActors)
                    {
                        commandList.Add(female.Come2TalkFFMCommand);
                    }
                }
            }else if(DateSpotMovement.Patches.IsDateSpot(scene._actionSettings, scene.MapID)){
                //Handle the FFM case in date spot scene
                commandList.Clear();
                foreach (var cmd in GetFFMTargetsForActor(scene, actor))
                    commandList.Add(cmd);
            }
        }


        internal static void SpoofActorAsBadFriend(ActionScene scene)
        {
            if (scene._actionSettings.IsPrivate(scene.MapID))
            {
                foreach (Actor actor in scene._maleActors)
                {
                    if (!StateManager.Instance.dictPrivateRoomSpoof.ContainsKey(actor.CharaFileName))
                        StateManager.Instance.dictPrivateRoomSpoof.Add(actor.CharaFileName, actor.JobID);
                    actor._status.JobID = (int)Define.JobID.Badfriend;
                }
            }
        }

        internal static void PatchThreesomeInPublicMap(ActionScene scene, Actor actor)
        {
            if (scene._actionSettings.IsPrivate(scene.MapID))
                return;

            //Check the actor condition
            if (actor.Partner != null)
                return;

            //The threesome command is bugged if the actor just finished a threesome scene, apply fix by replacing
            if (actor.Status.AfterThreesomeState == 1)
            {
                FixAfterThreesomeCommand(scene, actor);
                return;
            }

            //add MMF or FFM command if condition fulfilled
            PatchMMFSituation(scene, actor);
            PatchFFMSituation(scene, actor);
        }

        private static void PatchMMFSituation(ActionScene scene, Actor actor)
        {
            //Check if there is any MMF condition fulfilled
            if (GetMMFTargetsForActor(scene, actor).Count > 0)
            {
                bool isExist = false;
                foreach (var ac in actor._baseCommand.Commands)
                {
                    if (ac.Info.ActionID == 316)
                        isExist = true;
                }

                if (!isExist)
                {
                    ActionInfo infoMMF = new ActionInfo();
                    infoMMF.ActionType = 3;
                    infoMMF.ActionID = 316;
                    infoMMF.ExistsChildren = false;
                    infoMMF.IgnoreOutOfFuel = false;
                    infoMMF.Order = 3;
                    infoMMF.ValidationID = -1;
                    infoMMF.PrevType = -1;
                    infoMMF.NestedActionType = -9994;
                    infoMMF.ActionName = Captions.Actions.OfferMMF;

                    actor._baseCommand.Commands.Add(new ActionCommand(infoMMF, null, DelegateActionMMFInit));
                }
            }
        }

        private static void PatchFFMSituation(ActionScene scene, Actor actor)
        {
            //For FFM, it is already available in workplace and private room, we only have to add it back if it is in public map (cafe, restaurant, park)
            if (!DateSpotMovement.Patches.IsDateSpot(scene._actionSettings, scene.MapID))
                return;

            if (GetFFMTargetsForActor(scene, actor).Count > 0)
            {
                bool isExist = false;
                foreach (var ac in actor._baseCommand.Commands)
                {
                    if (ac.Info.ActionID == 182)
                        isExist = true;
                }

                if (!isExist)
                {
                    ActionInfo infoFFM = new ActionInfo();
                    infoFFM.ActionType = 3;
                    infoFFM.ActionID = 182;
                    infoFFM.ExistsChildren = false;
                    infoFFM.IgnoreOutOfFuel = false;
                    infoFFM.Order = 2;
                    infoFFM.ValidationID = -1;
                    infoFFM.PrevType = -1;
                    infoFFM.NestedActionType = -9998;
                    infoFFM.ActionName = Captions.Actions.OfferFFM;

                    actor._baseCommand.Commands.Add(new ActionCommand(infoFFM, null, DelegateActionMMFInit));
                }
            }
        }

        private static void FixAfterThreesomeCommand(ActionScene scene, Actor actor)
        {
            foreach (var a in scene._actors)
            {
                if (actor.ThreesomeTarget?.InstanceID == a.InstanceID)
                {
                    Actor target = a;
                    if (a.OccupiedActionPoint == null)
                        target = a.Partner;

                    //determine it is FFM or MMF
                    int maleCount, femaleCount;
                    Util.CountMaleFemale(actor, target, target.Partner, out maleCount, out femaleCount);
                    bool isMMF = maleCount == 2 && femaleCount == 1;
                    bool isFFM = maleCount == 1 && femaleCount == 2;

                    //In the base command, look for the H command (type = -7)
                    foreach (var cmd in actor._baseCommand.Commands)
                    {
                        if (cmd.Info.ActionType == -7)
                        {
                            if (isMMF)
                            {
                                cmd.Execute = target.Come2TalkMMFCommand.Execute;
                                cmd.Info.ActionName = Captions.Actions.OfferMMF;
                            }
                            else if(isFFM)
                            {
                                cmd.Execute = target.Come2TalkFFMCommand.Execute;
                                cmd.Info.ActionName = Captions.Actions.OfferFFM;
                            }
                        }
                    }
                    break;
                }
            }
        }

        private static List<ActionCommand> GetMMFTargetsForActor(ActionScene scene, Actor actor)
        {
            List<int> instanceIDAdded = new List<int>();
            List<ActionCommand> result = new List<ActionCommand>();

            foreach (var targetActor in scene._actors)
            {
                if (actor.InstanceID != targetActor.InstanceID && targetActor.Partner != null && !instanceIDAdded.Contains(targetActor.InstanceID) && targetActor.OccupiedActionPoint != null)
                {
                    //Check if the paired actors + selected actors fulfilled the requirement of MMF situation
                    //1. Requires 2 male and 1 female 
                    int maleCount, femaleCount;
                    Util.CountMaleFemale(actor, targetActor, targetActor.Partner, out maleCount, out femaleCount);
                    if (!(maleCount == 2 && femaleCount == 1))
                    {
                        continue;
                    }

                    //2. Relationship condition: require the female actor have the relationship "Friend" and already have sex with both male
                    Actor femaleActor, maleActor1, maleActor2;
                    if (actor.Sex == 1)
                    {
                        femaleActor = actor;
                        maleActor1 = targetActor;
                        maleActor2 = targetActor.Partner;
                    }
                    else if (targetActor.Sex == 1)
                    {
                        femaleActor = targetActor;
                        maleActor1 = targetActor.Partner;
                        maleActor2 = actor;
                    }
                    else
                    {
                        femaleActor = targetActor.Partner;
                        maleActor1 = targetActor;
                        maleActor2 = actor;
                    }

                    if (ActionScene.IsGTERelationState(femaleActor, maleActor1, Define.Action.RelationType.Friend)
                        && ActionScene.IsGTERelationState(femaleActor, maleActor2, Define.Action.RelationType.Friend)
                        && CheckHasEverSex(femaleActor, maleActor1)
                        && CheckHasEverSex(femaleActor, maleActor2)
                    )
                    {
                        //all condition passed, add the actor to the list
                        result.Add(targetActor.Come2TalkMMFCommand);

                        //record instanceID to avoid duplication
                        instanceIDAdded.Add(targetActor.InstanceID);
                    }
                }
            }

            return result;
        }

        private static List<ActionCommand> GetFFMTargetsForActor(ActionScene scene, Actor actor)
        {
            List<int> instanceIDAdded = new List<int>();
            List<ActionCommand> result = new List<ActionCommand>();

            foreach (var targetActor in scene._actors)
            {
                if (actor.InstanceID != targetActor.InstanceID && targetActor.Partner != null && !instanceIDAdded.Contains(targetActor.InstanceID) && targetActor.OccupiedActionPoint != null)
                {
                    //Check if the paired actors + selected actors fulfilled the requirement of MMF situation
                    //1. Requires 1 male and 2 female 
                    int maleCount, femaleCount;
                    Util.CountMaleFemale(actor, targetActor, targetActor.Partner, out maleCount, out femaleCount);
                    if (!(maleCount == 1 && femaleCount == 2))
                    {
                        continue;
                    }

                    //2. Relationship condition: require the male actor have the relationship "Friend" and already have sex with both female
                    Actor femaleActor1, femaleActor2, maleActor;
                    if (actor.Sex == 0)
                    {
                        maleActor = actor;
                        femaleActor1 = targetActor;
                        femaleActor2 = targetActor.Partner;
                    }
                    else if (targetActor.Sex == 0)
                    {
                        maleActor = targetActor;
                        femaleActor1 = targetActor.Partner;
                        femaleActor2 = actor;
                    }
                    else
                    {
                        maleActor = targetActor.Partner;
                        femaleActor1 = targetActor;
                        femaleActor2 = actor;
                    }

                    if (ActionScene.IsGTERelationState(maleActor, femaleActor1, Define.Action.RelationType.Friend)
                        && ActionScene.IsGTERelationState(maleActor, femaleActor2, Define.Action.RelationType.Friend)
                        && CheckHasEverSex(maleActor, femaleActor1)
                        && CheckHasEverSex(maleActor, femaleActor2)
                    )
                    {
                        //all condition passed, add the actor to the list
                        result.Add(targetActor.Come2TalkFFMCommand);

                        //record instanceID to avoid duplication
                        instanceIDAdded.Add(targetActor.InstanceID);
                    }
                }
            }

            return result;
        }

        internal static void UpdateMMFTargetCommandListInPublicRoom(ActionScene scene, Actor actor, List<ActionCommand> commandList)
        {
            List<ActionCommand> toAdd = GetMMFTargetsForActor(scene, actor);
            foreach (var cmd in toAdd)
                commandList.Add(cmd);
        }

        //There is one function in the Illusion code but no idea how to call it properly
        static bool CheckHasEverSex(Actor actor1, Actor actor2)
        {
            foreach (var dict in actor1.Status.RelationshipParameter)
            {
                if (dict.ContainsKey(actor2.CharaFileName))
                {
                    return dict[actor2.CharaFileName].HasEverSex;
                }
            }

            return false;
        }

        #region MMF Delegates
        static Il2CppSystem.Action<Actor, ActionInfo> DelegateActionMMFInit = (Il2CppSystem.Action<Actor, ActionInfo>)delegate (Actor actor, ActionInfo xInfo)
        {
            ActionPoint.__c__DisplayClass150_1 c = new ActionPoint.__c__DisplayClass150_1();
            c.action = DelegateActionMMF;
            c.field_Public___c__DisplayClass150_0_0 = new ActionPoint.__c__DisplayClass150_0();
            c.field_Public___c__DisplayClass150_0_0.__4__this = actor.OccupiedActionPoint;

            c._Init_b__1(actor, xInfo);
        };

        static Il2CppSystem.Action<Actor, ActionPoint, ActionInfo> DelegateActionMMF = (Il2CppSystem.Action<Actor, ActionPoint, ActionInfo>)delegate (Actor actor, ActionPoint point, ActionInfo xInfo)
        {
            ActionPoint.__c c = new ActionPoint.__c();
            c.__cctor_b__206_132(actor, point, xInfo);
        };
        #endregion

        #region FFM Delegates
        static Il2CppSystem.Action<Actor, ActionInfo> DelegateActionFFMInit = (Il2CppSystem.Action<Actor, ActionInfo>)delegate (Actor actor, ActionInfo xInfo)
        {
            ActionPoint.__c__DisplayClass150_1 c = new ActionPoint.__c__DisplayClass150_1();
            c.action = DelegateActionFFM;
            c.field_Public___c__DisplayClass150_0_0 = new ActionPoint.__c__DisplayClass150_0();
            c.field_Public___c__DisplayClass150_0_0.__4__this = actor.OccupiedActionPoint;

            c._Init_b__1(actor, xInfo);
        };

        static Il2CppSystem.Action<Actor, ActionPoint, ActionInfo> DelegateActionFFM = (Il2CppSystem.Action<Actor, ActionPoint, ActionInfo>)delegate (Actor actor, ActionPoint point, ActionInfo xInfo)
        {
            ActionPoint.__c c = new ActionPoint.__c();
            c.__cctor_b__206_132(actor, point, xInfo);
        };
        #endregion

        private static Func<ActionCommand, bool> GetRemovePredicate(Actor actor, bool isVisitor, bool examChair, bool conferenceRoom)
        {
            return (ActionCommand cmd) =>
            {
                string cmdName = Util.GetActionName(cmd, actor);
                bool remove = false;
                if (cmdName == null)
                {
                    return remove;
                }

                if (isVisitor)
                {
                    remove = JobRestrictedCommands.Contains(cmdName);
                }
                if (!remove && examChair)
                {
                    remove = cmdName == Captions.Actions.GoToExamChair;
                }
                if (!remove && conferenceRoom)
                {
                    remove = cmdName == Captions.Actions.MoveToConferenceRoom;
                }

                return remove;
            };
        }

        private static Func<ActionCommand, bool> GetAddFilterPredicate(Actor actor)
        {
            return (ActionCommand cmd) =>
            {
                bool include = false;
                System.Collections.Generic.List<string> toAdd = actor.Sex == 0 ? MaleCommandsToAdd : FemaleCommandsToAdd;
                string name = Util.GetActionName(cmd, actor);
                include = name != null && toAdd.Contains(name);

                //special case handling for MMF in private room
                if (include && name == Captions.Actions.OfferMMF)
                {
                    if (ActionScene.Instance._actionSettings.IsPrivate(ActionScene.Instance.MapID))
                    {
                        if (ActionScene.Instance._femaleActors.Count == 1 && ActionScene.Instance._maleActors.Count == 2)
                            return true;
                    }
                    return false;
                }

                if (include && AvailabilityRestrictedCommands.Contains(name))
                {
                    include = cmd.IsAvailableCondition.Invoke(actor, cmd.Info);
                }
                return include;
            };
        }

        private static void DeDupeCommandList(List<ActionCommand> commands, Actor actor)
        {
            HashSet<string> names = new HashSet<string>();
            for (int i = commands.Count - 1; i >= 0; i--)
            {
                ActionCommand cmd = commands[i];
                string name = Util.GetActionName(cmd, actor);

                if (names.Contains(name))
                {
                    commands.RemoveAt(i);
                }
                else
                {
                    names.Add(name);
                }
            }
        }
    }
}
