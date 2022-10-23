using BepInEx.Logging;
using RG.Scene;
using RG.Scene.Action.Core;
using RG.Scripts;
using System;
using Il2CppSystem.Collections.Generic;


namespace RGActionPatches.TalkToSomeone
{
    class Patches
    {
        private static ManualLogSource Log = RGActionPatchesPlugin.Log;

        internal static void AddToPointCommands(ActionPoint point, Actor actor, ActionScene scene, List<ActionCommand> current)
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
        private static Func<ActionCommand, bool> GetPredicate(Actor actor)
        {
            return (ActionCommand cmd) => {
                string name = Util.GetActionName(cmd, actor);
                return name == Captions.Actions.TalkToSomeone;
            };
        }
    }
}
