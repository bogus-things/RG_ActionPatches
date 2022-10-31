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
            Captions.Actions.TalkToSomeone
        };

        private static readonly System.Collections.Generic.List<string> MaleCommandsToAdd = new System.Collections.Generic.List<string>()
        {
            Captions.Actions.GoToPoleDanceFront,
            Captions.Actions.DoKitchenH,
            Captions.Actions.DoBathH,
            Captions.Actions.MoveToConferenceRoom,
            Captions.Actions.TalkToSomeone
        };

        private static readonly System.Collections.Generic.List<string> JobRestrictedCommands = new System.Collections.Generic.List<string>()
        {
            Captions.Actions.PhysicalCheckup,
            Captions.Actions.CheckTemperature,
            Captions.Actions.TalkToPatient,
            Captions.Actions.Seduce,
            Captions.Actions.MoveToConferenceRoom,
        };

        // Some commands should only be added if their availability conditions are met
        private static readonly System.Collections.Generic.List<string> AvailabilityRestrictedCommands = new System.Collections.Generic.List<string>()
        {
            Captions.Actions.DoKitchenH,
            Captions.Actions.DoBathH
        };


        internal static void UpdateActorCommands(ActionScene scene, Actor actor, IReadOnlyList<ActionCommand> baseCommands, List<ActionCommand> current)
        {
            Log.LogMessage(actor.name);
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
                Log.LogMessage(name);
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

            Log.LogMessage("-----");
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
