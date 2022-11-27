using BepInEx.Logging;

namespace RGActionPatches
{
    internal static class Debug
    {
        private static ManualLogSource Log = RGActionPatchesPlugin.Log;
        internal static void PrintStatusDictionaryToLog()
        {
            Log.Log(LogLevel.Info, "===Status Dictionary===");
            foreach (var dict in Manager.Game.UserFile.DicStatusArchive)
            {
                foreach (var status in dict)
                {
                    foreach (var prop in status.Value.GetType().GetProperties())
                    {
                        try
                        {
                            object value = prop.GetValue(status.Value, null);
                            if (value != null)
                                Log.Log(LogLevel.Info, prop.Name + "=" + value);
                            else
                                Log.Log(LogLevel.Info, prop.Name + " is null!!");
                        }
                        catch { }
                    }
                    Log.Log(LogLevel.Info, "Detail Partner Info"
                        + ", KeyID:" + status.Value.PartnerInfo.KeyID
                        + ", JobID:" + status.Value.PartnerInfo.JobID
                        + ", Sex:" + status.Value.PartnerInfo.Sex
                        + ", IndexAsMob:" + status.Value.PartnerInfo.IndexAsMob
                        );
                    Log.Log(LogLevel.Info, "Detail Stashing Partner Info"
                        + ", KeyID:" + status.Value.StashingPartnerInfo.KeyID
                        + ", JobID:" + status.Value.StashingPartnerInfo.JobID
                        + ", Sex:" + status.Value.StashingPartnerInfo.Sex
                        + ", IndexAsMob:" + status.Value.StashingPartnerInfo.IndexAsMob
                        );
                    Log.Log(LogLevel.Info, "ThreesomeTargetInfo Info"
                        + ", KeyID:" + status.Value.ThreesomeTargetInfo.KeyID
                        + ", JobID:" + status.Value.ThreesomeTargetInfo.JobID
                        + ", Sex:" + status.Value.ThreesomeTargetInfo.Sex
                        + ", IndexAsMob:" + status.Value.ThreesomeTargetInfo.IndexAsMob
                        );
                    
                    Log.Log(LogLevel.Info, "===================");
                }
            }
        }

        internal static void PrintVisitorDictionaryToLog()
        {
            Log.Log(LogLevel.Info, "===Visitor Log===");
            foreach (var dict in Manager.Game.UserFile.DicVisitors)
            {
                Log.Log(LogLevel.Info, "Key: " + dict.Key);
                for (int i = 0; i < dict.Value.Count; i++)
                {
                    foreach (var prop in dict.Value[i].GetType().GetProperties())
                    {
                        try
                        {
                            object value = prop.GetValue(dict.Value[i], null);
                            if (value != null)
                                Log.Log(LogLevel.Info, prop.Name + "=" + value);
                            else
                                Log.Log(LogLevel.Info, prop.Name + " is null!!");
                        }
                        catch { }
                    }
                }
                Log.Log(LogLevel.Info, "===================");
            }
        }
        internal static void PrintActionCommand(RG.Scripts.ActionCommand a, bool isPrintFull = false)
        {
            Log.Log(LogLevel.Info, "Command name: " + a.Info.ActionName);
            if (isPrintFull)
            {
                foreach (var prop in a.Info.GetType().GetProperties())
                {
                    try
                    {

                        object value = prop.GetValue(a.Info, null);
                        if (value != null)
                            Log.Log(LogLevel.Info, prop.Name + "=" + value);
                        else
                            Log.Log(LogLevel.Info, prop.Name + " is null!!");
                    }
                    catch { }
                }
                Log.Log(LogLevel.Info, "Method name: " + a.Execute.Method.Name);
                Log.Log(LogLevel.Info, "DeclaringType: " + a.Execute.Method.DeclaringType.FullName);
                Log.Log(LogLevel.Info, "FormatNameAndSig: " + a.Execute.Method.FormatNameAndSig(true));

                Log.Log(LogLevel.Info, "===========");
            }
        }

        internal static void PrintActorCommand(RG.Scene.Action.Core.Actor actor)
        {
            int i = 0;

            Log.Log(LogLevel.Info, "Base Command: " + actor._baseCommand.Commands.Count);
            for (int counter = 0; counter < actor._baseCommand.Commands.Count; counter++)
            {
                PrintActionCommand(actor._baseCommand.Commands[counter], true);
            }

            Log.Log(LogLevel.Info, "Command Cache: " + actor._commandCache.Count);
            foreach (var a in actor._commandCache)
            {

                Log.Log(LogLevel.Info, "Key: " + a.Key);
                if (a.Value != null)
                {
                    Log.Log(LogLevel.Info, "Header: " + a.Value.Header + ", SelectedID: " + a.Value.SelectedID);

                    i = 0;
                    while (true)
                    {
                        try
                        {
                            PrintActionCommand(a.Value.Commands[i], true);
                            i++;
                        }
                        catch { break; }
                    }
                }
            }
        }
    }
}
