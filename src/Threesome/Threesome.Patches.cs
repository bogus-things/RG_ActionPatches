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

        //The Action Point names of host and guest in private room
        private static readonly System.Collections.Generic.List<string> PrivateBadFriendValidTargetHPointNames = new System.Collections.Generic.List<string>()
        {
            "host_actionpoint_00",
            "guest_actionpoint_00",
            "host_actionpoint",
            "guest_actionpoint",
        };

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

            foreach (Actor targetActor in scene._actors)
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
                    if (scene._femaleActors[0].Partner != null)
                    {
                        //the non bad friend characters need to be in pair
                        foreach (var female in scene._femaleActors)
                        {
                            commandList.Add(female.Come2TalkMMFCommand);
                        }
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
                    if (scene._femaleActors[0].Partner != null)
                    {
                        //the two female characters need to be in pair
                        foreach (var female in scene._femaleActors)
                        {
                            commandList.Add(female.Come2TalkFFMCommand);
                        }
                    }
                }
            }
            else //if (DateSpotMovement.Patches.IsDateSpot(scene._actionSettings, scene.MapID))
            {
                //Handle the FFM case in date spot scene
                commandList.Clear();
                foreach (var cmd in GetFFMTargetsForActor(scene, actor))
                    commandList.Add(cmd);
            }
        }

        internal static HPoint Find3PStartPoint(HScene scene)
        {
            bool is3p = scene._hSceneManager.ActorFemales.Count + scene._hSceneManager.ActorMales.Count == 3;
            if (!is3p)
            {
                return null;
            }

            HPointCtrl ctrl = scene.HPointCtrl;
            foreach (KeyValuePair<int, HPointList.HPointPlaceInfo> kv in ctrl.HPointList.Lst)
            {
                foreach(HPoint p in kv.Value.HPoints)
                {
                    if (p.name != null && p.name.Contains("3p"))
                    {
                        return p;
                    }
                }
            }

            return null;
        }

        internal static void SpoofBadFriendInPrivateRoom(Actor actor)
        {
            if (ActionScene.Instance._actionSettings.IsPrivate(ActionScene.Instance.MapID))
            {
                int badFriendPointID;
                Guests.Patches.TryGetBadfriendPointID(ActionScene.Instance, out badFriendPointID);

                if (actor.OccupiedActionPoint?.UniqueID == badFriendPointID && ActionScene.Instance._femaleActors.Count > 0)
                {
                    //Spoof the male character at bad friend point to be a bad friend
                    Actor f = ActionScene.Instance._femaleActors[0];
                    StateManager.Instance.addSpoofedActor(actor, actor.JobID, actor._status.KeyID, actor._status.IndexAsMob);
                    actor._status.JobID = f.MyBadfriendA.JobID;
                    actor._status.KeyID = f.MyBadfriendA.KeyID;
                    actor._status.IndexAsMob = f.MyBadfriendA.IndexAsMob;

                    if (ActionScene.Instance._maleActors.Count > 1 && f.Partner != null)
                    {
                        //Spoof the partner male to be a bad friend too if MMF situation is possible
                        Actor m2 = f.Partner;

                        StateManager.Instance.addSpoofedActor(m2, m2.JobID, m2._status.KeyID, m2._status.IndexAsMob);
                        m2._status.JobID = f.MyBadfriendB.JobID;
                        m2._status.KeyID = f.MyBadfriendB.KeyID;
                        m2._status.IndexAsMob = f.MyBadfriendB.IndexAsMob;
                    }
                }
            }
        }

        internal static void GetSpoofedBadFriendHTargetList(ActionScene scene, Actor actor, List<ActionCommand> commandList)
        {
            if (ActionScene.Instance._actionSettings.IsPrivate(ActionScene.Instance.MapID))
            {
                int badFriendPointID;
                Guests.Patches.TryGetBadfriendPointID(scene, out badFriendPointID);

                if (actor.OccupiedActionPoint?.UniqueID == badFriendPointID && ActionScene.Instance._femaleActors.Count > 0)
                {
                    commandList.Clear();
                    foreach (Actor f in scene._femaleActors)
                    {
                        //Only allow the spoofed bad friend character to initiate H action if the target female character is located in host point or guest point to avoid complicated scenario.
                        if (PrivateBadFriendValidTargetHPointNames.Contains(f.OccupiedActionPoint?.name))
                            commandList.Add(f.Come2TalkHCommand);
                    }
                }
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
