using RG.Scene;
using RG.Scripts;
using System;
using Il2CppSystem.Collections.Generic;
using RG.Scene.Action.Core;


namespace RGActionPatches.Patches
{
    class TalkToSomeone
    {
        private const string ActionName = "人と話す";

        private static Func<ActionCommand, bool> getPredicate(Actor actor) {
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

        internal static void addToActorCommands(Actor actor, ActionScene scene, IReadOnlyList<ActionCommand> baseCommands, List<ActionCommand> current)
        {
            Func<ActionCommand, bool> predicate = getPredicate(actor);
            if (scene._actors.Count > 1)
            {
                int index = Util.readOnlyIndexOf(baseCommands, predicate);
                if (index > -1 && !current.Exists(predicate))
                {
                    current.Insert(0, baseCommands[index]);
                }
            }
        }


        internal static void addToPointCommands(Actor actor, ActionScene scene, List<ActionCommand> current)
        {
            Func<ActionCommand, bool> predicate = getPredicate(actor);
            if (!current.Exists(predicate) && scene._actors.Count > 1)
            {
                int index = Util.readOnlyIndexOf(actor._movement.Commands, predicate);
                if (index > -1)
                {
                    current.Insert(0, actor._movement.Commands[index]);
                }
            }
        }
    }
}
