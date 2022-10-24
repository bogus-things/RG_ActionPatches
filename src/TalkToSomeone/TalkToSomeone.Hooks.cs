using HarmonyLib;
using RG.Scene;
using RG.Scene.Action.Core;
using RG.Scripts;
using Il2CppSystem.Collections.Generic;
using BepInEx.Logging;

namespace RGActionPatches.TalkToSomeone
{
    class Hooks
    {
        internal static string GUID = RGActionPatchesPlugin.GUID + ".TalkToSomeone";
        private static ManualLogSource Log = RGActionPatchesPlugin.Log;

        // Adds "Talk to someone" to the list of commands available at a point if it's missing
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ActionPoint), nameof(ActionPoint.GetTypeCommandList), new[] { typeof(byte), typeof(RG.Define.Action.Forms), typeof(int), typeof(List<ActionCommand>) })]
        private static void GetTypeCommandListPost(ActionPoint __instance, int type, List<ActionCommand> commands)
        {
            // type 3 = move commands (talk to someone, go to casino, etc)
            if (ActionScene.Initialized && __instance.AttachedActor != null && type == 3)
            {
                Patches.AddToPointCommands(__instance, __instance.AttachedActor, ActionScene.Instance, commands);
            }
        }
    }
}
