using HarmonyLib;
using RG.Scene;
using RG.Scene.Action.Core;
using RG.Scene.Action.UI;
using RG.Scripts;
using Il2CppSystem.Collections.Generic;
using BepInEx.Logging;
using RG.User;
using UnhollowerBaseLib;
using RG.Scene.Action.Settings;
using UnityEngine;

namespace RGActionPatches
{
    internal static class Hooks
    {
        private static ManualLogSource Log = RGActionPatchesPlugin.Log;

        // Rewrite the target command list, and replace with commands for all actors in the scene (minus self)
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ActionScene), nameof(ActionScene.GetActorTargetCommandList))]
        private static void getActorTargetCommandListPost(ActionScene __instance, Actor actor, List<ActionCommand> commandList)
        {
            commandList.Clear();
            //Log.LogMessage("==========");
            //Log.LogMessage("POST GetActorTargetCommandList " + actor.name);
            //Log.LogMessage(actor.JobID);
            //Log.LogMessage("==========");

            foreach (Actor act in __instance._actors)
            {
                //Log.LogMessage(act.name);
                //Log.LogMessage(act.JobID);
                //Log.LogMessage("----------");

                if (act.InstanceID != actor.InstanceID)
                {
                    commandList.Add(act.Come2TalkCommand);
                }

            }
        }

        // Rewrite the command list UI to undo some target filtering that happens in this function
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CommandList), nameof(CommandList.RefreshCommands), new[] { typeof(IReadOnlyList<ActionCommand>), typeof(ActionCommand) })]
        private static void refreshCommandsPost(CommandList __instance, IReadOnlyList<ActionCommand> commands, ActionCommand cancelCommand)
        {
            __instance._commandList.Clear();
            Util.AddReadOnlyToList(commands, __instance._commandList);
            __instance._commandList.Add(cancelCommand);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Actor), nameof(Actor.FilterCommands))]
        private static void filterCommandsPost(Actor __instance, IReadOnlyList<ActionCommand> commands = null, List<ActionCommand> dest = null)
        {
            //Log.LogMessage("==========");
            //Log.LogMessage("POST " + __instance.name);
            //Log.LogMessage("POST FilterCommands commands");
            //Log.LogMessage("==========");

            //List<ActionCommand> cmds = Util.ReadOnlyToList(commands);
            //foreach (ActionCommand cmd in cmds)
            //{
            //    Log.LogMessage(cmd.Info.ActionName);
            //    Log.LogMessage(cmd.Info.ActionType);
            //    Log.LogMessage("----------");
            //}

            //Log.LogMessage("==========");
            //Log.LogMessage("POST " + __instance.name);
            //Log.LogMessage("POST FilterCommands dest");
            //Log.LogMessage("==========");

            //foreach (ActionCommand cmd in dest)
            //{
            //    Log.LogMessage(cmd.Info.ActionName);
            //    Log.LogMessage(cmd.Info.ActionType);
            //    Log.LogMessage("----------");
            //}

            System.Func<ActionCommand, bool> predicate = delegate (ActionCommand cmd) { return cmd.Info.ActionName == "人と話す"; };

            if (ActionScene.Initialized && ActionScene.Instance._actors.Count > 1)
            {
                int index = Util.readOnlyIndexOf(commands, predicate);
                if (index > -1 && !dest.Exists(predicate))
                {
                    dest.Insert(0, commands[index]);
                }
            }
        }

        // this guy patches date spot & clinic visitors to enable talk to someone
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ActionPoint), nameof(ActionPoint.GetTypeCommandList), new[] { typeof(byte), typeof(RG.Define.Action.Forms), typeof(int), typeof(List<ActionCommand>) })]
        private static void getTypeCommandListPost(ActionPoint __instance, byte sex, RG.Define.Action.Forms form, int type, List<ActionCommand> commands)
        {
            //Log.LogMessage("==========");
            //Log.LogMessage("POST GetTypeCommandList (sex,form,type)");
            //Log.LogMessage(sex);
            //Log.LogMessage(form);
            //Log.LogMessage(type);
            //Log.LogMessage("attached: " + __instance.AttachedActor.name);
            //Log.LogMessage("==========");
            //Log.LogMessage("commands ");
            //Log.LogMessage("==========");
            //foreach (ActionCommand cmd in commands)
            //{
            //    Log.LogMessage(cmd.Info.ActionName);
            //    Log.LogMessage("----------");
            //}
            //Log.LogMessage("movement ");
            //Log.LogMessage("==========");
            if (type == 3)
            {
                System.Func<ActionCommand, bool> predicate = delegate (ActionCommand cmd) { return cmd.Info.ActionName == "人と話す"; };
                if (!commands.Exists(predicate) && ActionScene.Initialized && ActionScene.Instance._actors.Count > 1)
                {
                    IReadOnlyList<ActionCommand> movementCmds = __instance.AttachedActor._movement.Commands;
                    int index = 0;
                    while (true)
                    {
                        try
                        {
                            ActionInfo info = movementCmds[index].Info;
                            string name = info.ActionName == null ? info.GetActionNameCallback.Invoke(__instance.AttachedActor) : info.ActionName;
                            //Log.LogMessage(name);
                            //Log.LogMessage("----------");
                            if (name == "人と話す")
                            {
                                info.ActionName = name;
                                break;
                            }
                        }
                        catch (System.Exception)
                        {
                            index = -1;
                            break;
                        }
                        index++;
                    }

                    if (index > -1)
                    {
                        commands.Insert(0, movementCmds[index]);
                    }
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Actor), nameof(Actor.TalkTo))]
        private static void talkToPost(Actor __instance, Actor target)
        {
            //Log.LogMessage("==========");
            //Log.LogMessage("POST TalkTo");

            ActionScene scene = ActionScene.Instance;
            ActionSettings.MapIDs mapIDs = scene._actionSettings.MapID;
            if (scene.MapID == mapIDs.Cafe || scene.MapID == mapIDs.Restaurant || scene.MapID == mapIDs.Park)
            {
                ActionPoint pairingPoint = target.OccupiedActionPoint.Pairing;
                if (pairingPoint != null && pairingPoint.IsAvailable() && __instance.OccupiedActionPoint != pairingPoint)
                {
                    //__instance.PushSchedulingPoint(pairingPoint);
                    IReadOnlyList<Transform> dest = pairingPoint.Destination;
                    __instance.PopScheduledPoint();
                    __instance.PushSchedulingPoint(pairingPoint);
                    __instance.SetDestination(dest[0].position);
                    __instance.SetDestinationForce(dest[1].position);
                    __instance.ChangeState(RG.Define.StateID.GoToDestination, true);
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Actor), nameof(Actor.OnArrivedDestination))]
        private static void onArrivedDestinationPost(Actor __instance)
        {
            //Log.LogMessage("==========");
            //Log.LogMessage("POST OnArrivedDestination");

            ActionPoint currentPoint = __instance.OccupiedActionPoint;

            ActionScene scene = ActionScene.Instance;
            ActionSettings.MapIDs mapIDs = scene._actionSettings.MapID;
            if (scene.MapID == mapIDs.Cafe || scene.MapID == mapIDs.Restaurant || scene.MapID == mapIDs.Park)
            {
                ActionScene.PairActorAndPoint(__instance, __instance.OccupiedActionPoint);
                if (currentPoint && currentPoint.Pairing && currentPoint.Pairing.AttachedActor)
                {
                    __instance.TalkTo(currentPoint.Pairing.AttachedActor);
                }
            }
        }


        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(ActionScene), nameof(ActionScene.StartTurnSequence))]
        //private static void startTurnSequencePost(Actor target, int stateID, Actor partner)
        //{
        //    Log.LogMessage("==========");
        //    Log.LogMessage("POST TryFindDestinationPoint");
        //    Log.LogMessage("actor: " + target.name);
        //    Log.LogMessage("state: " + stateID);
        //    Log.LogMessage("partner: " + partner?.name);
        //    Log.LogMessage("==========");
        //}

        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(ActionPointContainer), nameof(ActionPointContainer.TryGetSlot))]
        //private static void tryGetSlotPost(byte sex, int key, ActionPoint result, bool __result)
        //{
        //    Log.LogMessage("==========");
        //    Log.LogMessage("POST TryGetSlot");
        //    Log.LogMessage("sex: " + sex);
        //    Log.LogMessage("key: " + key);
        //    Log.LogMessage("resultPoint: " + result.name);
        //    Log.LogMessage("result: " + __result);
        //    Log.LogMessage("==========");

        //    //ActionScene scene = ActionScene.Instance;
        //    //ActionSettings.MapIDs mapIDs = scene._actionSettings.MapID;
        //    //if (scene.MapID == mapIDs.Cafe || scene.MapID == mapIDs.Restaurant || scene.MapID == mapIDs.Park)
        //    //{
        //    //    attr = 3;
        //    //}
        //}

        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(ActionPoint), nameof(ActionPoint.GetTypeCommandList), new[] { typeof(byte), typeof(int), typeof(int), typeof(List<ActionCommand>) })]
        //private static void getTypeCommandList2Pre(ActionPoint __instance, byte sex, int type, ref int attr, List<ActionCommand> commands)
        //{
        //    Log.LogMessage("==========");
        //    Log.LogMessage($"PRE GetTypeCommandList2 (sex={sex},type={type},attr={attr})");
        //    Log.LogMessage("attached: " + __instance.AttachedActor.name);
        //    Log.LogMessage("pair: " + __instance.Pairing?.name);
        //    Log.LogMessage("==========");

        //    ActionScene scene = ActionScene.Instance;
        //    ActionSettings.MapIDs mapIDs = scene._actionSettings.MapID;
        //    if (scene.MapID == mapIDs.Cafe || scene.MapID == mapIDs.Restaurant || scene.MapID == mapIDs.Park)
        //    {
        //        attr = 3;
        //    }
        //}

        // this guy patches the socialize option list so actions actually show up
        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(ActionPoint), nameof(ActionPoint.GetTypeCommandList), new[] { typeof(byte), typeof(int), typeof(int), typeof(List<ActionCommand>) })]
        //private static void getTypeCommandList2Post(ActionPoint __instance, byte sex, int type, int attr, List<ActionCommand> commands)
        //{
        //    Log.LogMessage("==========");
        //    Log.LogMessage($"POST GetTypeCommandList2 (sex={sex},type={type},attr={attr})");
        //    Log.LogMessage("attached: " + __instance.AttachedActor.name);
        //    Log.LogMessage("==========");

        //    foreach (ActionCommand cmd in commands)
        //    {
        //        Log.LogMessage(cmd.Info.ActionName);
        //        Log.LogMessage(cmd.Info.ActionType);
        //        Log.LogMessage("----------");
        //    }
        //}

        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(ActionPoint), nameof(ActionPoint.GetTypeCommandList), new[] { typeof(byte), typeof(int) })]
        //private static void getTypeCommandListPost0(ActionPoint __instance, byte sex, int type, IReadOnlyList<ActionCommand> __result)
        //{
        //    Log.LogMessage("==========");
        //    Log.LogMessage($"POST GetTypeCommandList0 (sex={sex},type={type})");
        //    Log.LogMessage("attached: " + __instance.AttachedActor.name);
        //    Log.LogMessage("==========");

        //    List<ActionCommand> commands = Util.ReadOnlyToList(__result);
        //    foreach (ActionCommand cmd in commands)
        //    {
        //        Log.LogMessage(cmd.Info.ActionName);
        //        Log.LogMessage(cmd.Info.ActionType);
        //        Log.LogMessage("----------");
        //    }
        //}

        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(ActionPoint), nameof(ActionPoint.GetTypeCommandList), new[] { typeof(byte), typeof(RG.Define.Action.Forms), typeof(int) })]
        //private static void getTypeCommandListPost1(ActionPoint __instance, byte sex, RG.Define.Action.Forms form, int type, IReadOnlyList<ActionCommand> __result)
        //{
        //    Log.LogMessage("==========");
        //    Log.LogMessage($"POST GetTypeCommandList1 (sex={sex},form={form},type={type})");
        //    Log.LogMessage("attached: " + __instance.AttachedActor.name);
        //    Log.LogMessage("==========");

        //    List<ActionCommand> commands = Util.ReadOnlyToList(__result);
        //    foreach (ActionCommand cmd in commands)
        //    {
        //        Log.LogMessage(cmd.Info.ActionName);
        //        Log.LogMessage(cmd.Info.ActionType);
        //        Log.LogMessage("----------");
        //    }
        //}



        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(ActionPoint), nameof(ActionPoint.GetTypeCommandList), new[] { typeof(byte), typeof(RG.Define.Action.Forms), typeof(int), typeof(int), typeof(List<ActionCommand>) })]
        //private static void getTypeCommandListPost4(ActionPoint __instance, byte sex, RG.Define.Action.Forms form, int type, int attr, List<ActionCommand> commands)
        //{
        //    Log.LogMessage("==========");
        //    Log.LogMessage($"POST GetTypeCommandList4 (sex={sex},form={form},type={type},attr={attr})");
        //    Log.LogMessage("attached: " + __instance.AttachedActor.name);
        //    Log.LogMessage("==========");

        //    foreach (ActionCommand cmd in commands)
        //    {
        //        Log.LogMessage(cmd.Info.ActionName);
        //        Log.LogMessage(cmd.Info.ActionType);
        //        Log.LogMessage("----------");
        //    }
        //}




        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(ActionScene), nameof(ActionScene.GetSummonCommandList))]
        //private static void getSummonCommandListPost(ActionScene __instance, Actor actor, List<ActionCommand> commandList)
        //{
        //    Log.LogMessage("==========");
        //    Log.LogMessage("POST GetSummonCommandList " + actor.name);
        //    Log.LogMessage("==========");

        //    foreach (ActionCommand cmd in commandList)
        //    {
        //        Log.LogMessage(cmd.Info.ActionName);
        //        Log.LogMessage(cmd.Info.ActionType);
        //        Log.LogMessage("----------");
        //    }
        //}

        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(ActionScene), nameof(ActionScene.GetActorTargetCommandList))]
        //private static void postfix(Actor actor, List<ActionCommand> __state)
        //{
        //    if (actor != null && __state != null)
        //    {
        //        Log.LogMessage("========================");
        //        Log.LogMessage("PRE " + actor.Chara.name);
        //        Log.LogMessage("========================");

        //        foreach (ActionCommand cmd in __state)
        //        {
        //            Log.LogMessage(cmd.Info.ActionType);
        //            Log.LogMessage("-------------------------");
        //        }
        //    }
        //}



        // nothing, just for logging
        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(CommandList), nameof(CommandList.RefreshCommands), new[] { typeof(IReadOnlyList<ActionCommand>), typeof(ActionCommand) })]
        //private static void refreshCommandsPreList(CommandList __instance)
        //{
        //    Log.LogMessage("==========");
        //    Log.LogMessage("PRE " + __instance.ActorDependsOn.name);
        //    Log.LogMessage("PRE RefreshCommands list");
        //    Log.LogMessage("==========");
        //}



        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(Status), nameof(Status.JobID), MethodType.Getter)]
        //private static void getJobIDPost(Status __instance, ref int __result)
        //{
        //    if (StateManager.Instance.CurrentMapID != __instance.MapID)
        //    {
        //        StateManager.Instance.CurrentMapID = __instance.MapID;
        //        StateManager.Instance.JobIDForCurrentMap = ActionScene.Instance._actionSettings.FindJobID(__instance.MapID);
        //        StateManager.Instance.CurrentMapIsPrivate = ActionScene.IsPrivateMap(__instance.MapID);
        //    }

        //    __result = Util.getWorkerJobIDForMap(__result);
        //}

        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(Actor), nameof(Actor.FilterDefaultCommands))]
        //private static void filterDefaultCommandsPre(Actor __instance, MutableCommandGroup source, Actor.CommandProcesses process, ref MutableCommandGroup __result)
        //{
        //    Log.LogMessage("==========");
        //    Log.LogMessage("PRE FilterDefaultCommands");
        //    Log.LogMessage( __instance.name);
        //    Log.LogMessage("==========");
        //    Log.LogMessage("source: " + source.Header);
        //    List<ActionCommand> srcCommands = Util.ReadOnlyToList(source.Commands);
        //    foreach(ActionCommand cmd in srcCommands) {
        //        Log.LogMessage(cmd.Info.ActionName);
        //        Log.LogMessage(cmd.Info.ActionType);
        //        Log.LogMessage("----------");
        //    }

        //    Log.LogMessage("result: " + __result.Header);
        //    List<ActionCommand> resultCommands = Util.ReadOnlyToList(__result.Commands);
        //    foreach (ActionCommand cmd in resultCommands)
        //    {
        //        Log.LogMessage(cmd.Info.ActionName);
        //        Log.LogMessage(cmd.Info.ActionType);
        //        Log.LogMessage("----------");
        //    }
        //}


    }
}
