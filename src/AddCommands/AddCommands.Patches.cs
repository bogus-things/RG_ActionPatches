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

        internal static void UpdateFFMTargetCommandList(ActionScene scene, List<ActionCommand> commandList)
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
            for(int i = commands.Count -1; i >= 0; i--)
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
