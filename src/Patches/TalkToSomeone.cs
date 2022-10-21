using RG.Scene;
using RG.Scripts;
using System;
using Il2CppSystem.Collections.Generic;
using RG.Scene.Action.Core;
using BepInEx.Logging;
using RG.Scene.Action.Settings;

namespace RGActionPatches.Patches
{
    class TalkToSomeone
    {
        private static ManualLogSource Log = RGActionPatchesPlugin.Log;
        private const string ActionName = "人と話す";

        private static Func<ActionCommand, bool> GetPredicate(Actor actor) {
            return (ActionCommand cmd) => {
                if (cmd.Info.ActionName != null)
                {
                    return cmd.Info.ActionName == ActionName;
                }

                try
                {
                    return cmd.Info.GetActionNameCallback.Invoke(actor) == ActionName;
                }
                catch (Exception)
                {
                    return false;
                }
            };
        }

        internal static void AddToActorCommands(Actor actor, ActionScene scene, IReadOnlyList<ActionCommand> baseCommands, List<ActionCommand> current)
        {
            Func<ActionCommand, bool> predicate = GetPredicate(actor);

            int index = Util.ReadOnlyIndexOf(baseCommands, predicate);
            if (index > -1 && !current.Exists(predicate))
            {
                current.Insert(0, baseCommands[index]);
            }
        }


        internal static void AddToPointCommands(Actor actor, ActionScene scene, List<ActionCommand> current)
        {
            Func<ActionCommand, bool> predicate = GetPredicate(actor);
            if (!current.Exists(predicate) && scene._actors.Count > 1)
            {
                int index = Util.ReadOnlyIndexOf(actor._movement.Commands, predicate);
                if (index > -1)
                {
                    current.Insert(0, actor._movement.Commands[index]);
                }
            }
        }

        internal static void SpoofJobID(Actor actor)
        {
            int mapJobID = ActionScene.Instance._actionSettings.FindJobID(actor.MapID);
            if (actor.JobID > -1 && actor.JobID != mapJobID)
            {
                actor._status.JobID = mapJobID;
            }
        }
    }
}
