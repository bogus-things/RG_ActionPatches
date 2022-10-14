using RG.Scene;
using RG.Scene.Action.Core;
using RG.Scripts;
using Il2CppSystem.Collections.Generic;
using RG.Scene.Action.UI;


namespace RGActionPatches.Patches
{
    class TalkTarget
    {
        internal static void unrestrictTalkTargetList(ActionScene scene, Actor actor, List<ActionCommand> commands)
        {
            commands.Clear();

            foreach (Actor a in scene._actors)
            {
                if (a.InstanceID != actor.InstanceID)
                {
                    commands.Add(a.Come2TalkCommand);
                }
            }
        }

        internal static void resetCommandList(CommandList commandList, IReadOnlyList<ActionCommand> originalCommands, ActionCommand cancelCommand)
        {
            commandList._commandList.Clear();
            Util.AddReadOnlyToList(originalCommands, commandList._commandList);
            commandList._commandList.Add(cancelCommand);
        }
    }
}
