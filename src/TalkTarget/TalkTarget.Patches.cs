using BepInEx.Logging;
using System;
using Il2CppSystem.Collections.Generic;
using RG.Scene;
using RG.Scene.Action.Core;
using RG.Scripts;
using RG.Scene.Action.UI;


namespace RGActionPatches.TalkTarget
{
    class Patches
    {
        private static ManualLogSource Log = RGActionPatchesPlugin.Log;

        private static readonly System.Collections.Generic.List<string> ExcludedPoints = new System.Collections.Generic.List<string>()
        {
            "poledance_actionpoint_00",
            "poledance_actionpoint_01",
            "poledance_actionpoint_02",
            "examination_actionpoint",
            "m_actionpoint_00_doctor",
            "meeting_00_01",
            "meeting_00_00"
        };

        internal static void UnrestrictTalkTargetList(ActionScene scene, Actor actor, List<ActionCommand> commands)
        {
            commands.Clear();

            foreach (Actor a in scene._actors)
            {
                // Re-add actors in the scene based on custom criteria
                if (a.InstanceID != actor.InstanceID && a.OccupiedActionPoint != null)
                {
                    //additional checking for the private room: talk to the actor in the bad friend action point will have bug so do not add it to the list
                    if (!(scene._actionSettings.IsPrivate(scene.MapID) && a.OccupiedActionPoint.UniqueID == Manager.Game.ActionMap.APTContainer._dicBadfriendActionPoint[0].UniqueID))
                        commands.Add(a.Come2TalkCommand);
                }
            }
        }

        internal static void ResetCommandList(CommandList commandList, IReadOnlyList<ActionCommand> originalCommands, ActionCommand cancelCommand)
        {
            commandList._commandList.Clear();
            Util.AddReadOnlyToList(originalCommands, commandList._commandList);
            commandList._commandList.Add(cancelCommand);
        }

        internal static void UpdateOptionDisabledState(CommandList commandList, ActionScene scene)
        {
            if (commandList != null && commandList._selectedCommand != null)
            {
                ActionCommand cmd = commandList._selectedCommand.Item1;
                CommandOption opt = commandList._selectedCommand.Item2;

                if (cmd.Info.ActionType == 3 && cmd.Info.GetActionNameCallback != null && !opt.ActiveDisablePanel)
                {
                    string actionName = cmd.Info.GetActionNameCallback.Invoke(commandList.ActorDependsOn);
                    if (actionName.Contains(Captions.Actions.SpeakWith))
                    {
                        string targetName = actionName.Split(Captions.Actions.SpeakWith.ToCharArray())[0].Trim();
                        Func<Actor, bool> predicate = delegate (Actor actor) { return targetName == actor.Status.FullName; };
                        Actor targetActor = scene._actors.Find(predicate);

                        if (targetActor != null)
                        {
                            if (targetActor.CommandState == RG.Define.Action.CommandState.InTheToilet)
                            {
                                opt.ActiveDisablePanel = true;
                                opt.DisableCaptionStr = Captions.Disabled.InTheToilet;
                            }
                            else if (targetActor.CommandState == RG.Define.Action.CommandState.Communication)
                            {
                                opt.ActiveDisablePanel = true;
                                opt.DisableCaptionStr = Captions.Disabled.TalkingToSomeone;
                            }
                            else if (targetActor.OccupiedActionPoint?.name == "examination_actionpoint" || targetActor.OccupiedActionPoint?.name == "m_actionpoint_00_doctor")
                            {
                                opt.ActiveDisablePanel = true;
                                opt.DisableCaptionStr = Captions.Disabled.InExamination;
                            }
                            else if (ExcludedPoints.Contains(targetActor.OccupiedActionPoint?.name))
                            {
                                opt.ActiveDisablePanel = true;
                                opt.DisableCaptionStr = Captions.Disabled.Unavailable;
                            }
                        }
                    }
                }
            }
        }

        internal static bool IsAvailableToTalk(Actor actor, bool currentResult = false)
        {
            if (currentResult)
            {
                return true;
            }

            return (
                actor != null &&
                actor.OccupiedActionPoint != null &&
                actor.CommandState == RG.Define.Action.CommandState.Neutral &&
                !ExcludedPoints.Contains(actor.OccupiedActionPoint.name)
            );
        }
    }
}
