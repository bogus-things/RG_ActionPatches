using RG.Scene;
using RG.Scene.Action.Core;
using RG.Scripts;
using Il2CppSystem.Collections.Generic;
using RG.Scene.Action.UI;
using BepInEx.Logging;
using System;

namespace RGActionPatches.Patches
{
    class TalkTarget
    {
        private static ManualLogSource Log = RGActionPatchesPlugin.Log;
        private const string TalkToSuffix = "に声をかける";
        private const string InTheToiletCaption = "トイレ中";
        private const string TalkingToSomeoneCaption = "会話中";
        private const string InExaminationCaption = "診察中";

        internal static void UnrestrictTalkTargetList(ActionScene scene, Actor actor, List<ActionCommand> commands)
        {
            commands.Clear();

            foreach (Actor a in scene._actors)
            {
                // add anyone else in the scene who's on a point
                if (a.InstanceID != actor.InstanceID && a.OccupiedActionPoint)
                {
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
                    if (actionName.Contains(TalkToSuffix))
                    {
                        string targetName = actionName.Split(TalkToSuffix.ToCharArray())[0].Trim();
                        Func<Actor, bool> predicate = delegate (Actor actor) { return targetName == actor.Status.FullName; };
                        Actor targetActor = scene._actors.Find(predicate);

                        if (targetActor != null)
                        {
                            if (targetActor.CommandState == RG.Define.Action.CommandState.InTheToilet)
                            {
                                opt.ActiveDisablePanel = true;
                                opt.DisableCaptionStr = InTheToiletCaption;
                            }
                            else if (targetActor.CommandState == RG.Define.Action.CommandState.Communication)
                            {
                                opt.ActiveDisablePanel = true;
                                opt.DisableCaptionStr = TalkingToSomeoneCaption;
                            }
                            else if (targetActor.OccupiedActionPoint?.name == "examination_actionpoint")
                            {
                                opt.ActiveDisablePanel = true;
                                opt.DisableCaptionStr = InExaminationCaption;
                            }
                        }
                    }
                }
            }
        }
    }
}
