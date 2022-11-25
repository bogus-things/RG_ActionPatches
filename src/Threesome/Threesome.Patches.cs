using System;
using BepInEx.Logging;
using Il2CppSystem.Collections.Generic;
using RG;
using RG.Scene;
using RG.Scene.Action.Core;
using RG.Scripts;


namespace RGActionPatches.Threesome
{
    class Patches
    {
        private static ManualLogSource Log = RGActionPatchesPlugin.Log;

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
                            else if (isFFM)
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
                        && Util.CheckHasEverSex(femaleActor, maleActor1)
                        && Util.CheckHasEverSex(femaleActor, maleActor2)
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
                        && Util.CheckHasEverSex(maleActor, femaleActor1)
                        && Util.CheckHasEverSex(maleActor, femaleActor2)
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
            }
            else if (DateSpotMovement.Patches.IsDateSpot(scene._actionSettings, scene.MapID))
            {
                //Handle the FFM case in date spot scene
                commandList.Clear();
                foreach (var cmd in GetFFMTargetsForActor(scene, actor))
                    commandList.Add(cmd);
            }
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

    }
}
