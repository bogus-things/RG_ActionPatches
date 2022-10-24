using System;
using BepInEx.Logging;
using Il2CppSystem.Collections.Generic;
using Manager;
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
            Captions.Actions.TalkToSomeone
        };

        private static readonly System.Collections.Generic.List<string> MaleCommandsToAdd = new System.Collections.Generic.List<string>()
        {
            Captions.Actions.TalkToSomeone,
            Captions.Actions.GoToPoleDanceFront
        };

        private static readonly System.Collections.Generic.List<string> JobRestrictedCommands = new System.Collections.Generic.List<string>()
        {
            Captions.Actions.PhysicalCheckup,
            Captions.Actions.CheckTemperature,
            Captions.Actions.TalkToPatient,
            Captions.Actions.Seduce
        };

        internal static void UpdateActorCommands(ActionScene scene, Actor actor, IReadOnlyList<ActionCommand> baseCommands, List<ActionCommand> current)
        {
            // add commands from one of the above ToAdd lists
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
                }
            }

            // Filter out some job-specific commands visitors shouldn't have
            int mapJobID = scene._actionSettings.FindJobID(actor.MapID);
            bool isVisitor = actor.JobID > -1 && actor.JobID != mapJobID;

            // Special-case command filtering to prevent some collisions
            // Case 1: moving to the exam chair while a visitor is talking to someone
            bool examChair = false;
            if (scene.MapID == scene._actionSettings.MapID.Clinic)
            {
                ActionPoint examPoint = Game.ActionMap.APTContainer.FindFromUniID(20);
                examChair = examPoint != null && scene.ExistsActorPostedPoint(examPoint);
            }

            Func<ActionCommand, bool> removePredicate = GetRemovePredicate(actor, isVisitor, examChair);
            current.RemoveAll(removePredicate);
        }

        internal static void SpoofJobID(ActionScene scene, Actor actor)
        {
            int mapJobID = scene._actionSettings.FindJobID(actor.MapID);
            if (actor.JobID > -1 && actor.JobID != mapJobID)
            {
                actor._status.JobID = mapJobID;
            }
        }

        internal static void SpoofForJobH(ActionScene scene, Actor main, Actor subA = null, Actor subB = null)
        {
            if(scene._actionSettings.IsJobMap(scene.MapID))
            {
                StateManager.Instance.addSpoofedActor(main, main.JobID);
                SpoofJobID(scene, main);

                if (subA != null)
                {
                    StateManager.Instance.addSpoofedActor(subA, subA.JobID);
                    SpoofJobID(scene, subA);
                }

                if (subB != null)
                {
                    StateManager.Instance.addSpoofedActor(subB, subB.JobID);
                    SpoofJobID(scene, subB);
                }
            }
        }

        internal static void RestoreSpoofed()
        {
            StateManager.Instance.restoreSpoofedActors();
        }

        private static Func<ActionCommand, bool> GetRemovePredicate(Actor actor, bool isVisitor, bool examChair)
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

                return remove;
            };
        }

        private static Func<ActionCommand, bool> GetAddFilterPredicate(Actor actor)
        {
            return (ActionCommand cmd) =>
            {
                System.Collections.Generic.List<string> toAdd = actor.Sex == 0 ? MaleCommandsToAdd : FemaleCommandsToAdd;
                string name = Util.GetActionName(cmd, actor);
                return name != null && toAdd.Contains(name);
            };
        }
    }
}
