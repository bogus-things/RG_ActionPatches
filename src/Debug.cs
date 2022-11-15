using BepInEx.Logging;

namespace RGActionPatches
{
    internal static class Debug
    {
        private static ManualLogSource Log = RGActionPatchesPlugin.Log;
        internal static void PrintStatusDictionaryToLog()
        {
            Log.Log(LogLevel.Debug, "===Status Dictionary===");
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
                                Log.Log(LogLevel.Debug, prop.Name + "=" + value);
                            else
                                Log.Log(LogLevel.Debug, prop.Name + " is null!!");
                        }
                        catch { }
                    }
                    Log.Log(LogLevel.Debug, "Detail Partner Info"
                        + ", KeyID:" + status.Value.PartnerInfo.KeyID
                        + ", JobID:" + status.Value.PartnerInfo.JobID
                        + ", Sex:" + status.Value.PartnerInfo.Sex
                        + ", IndexAsMob:" + status.Value.PartnerInfo.IndexAsMob
                        );
                    Log.Log(LogLevel.Debug, "Detail Stashing Partner Info"
                        + ", KeyID:" + status.Value.StashingPartnerInfo.KeyID
                        + ", JobID:" + status.Value.StashingPartnerInfo.JobID
                        + ", Sex:" + status.Value.StashingPartnerInfo.Sex
                        + ", IndexAsMob:" + status.Value.StashingPartnerInfo.IndexAsMob
                        );

                    Log.Log(LogLevel.Debug, "===================");
                }
            }
        }

        internal static void PrintVisitorDictionaryToLog()
        {
            Log.Log(LogLevel.Debug, "===Visitor Log===");
            foreach (var dict in Manager.Game.UserFile.DicVisitors)
            {
                Log.Log(LogLevel.Debug, "Key: " + dict.Key);
                for (int i = 0; i < dict.Value.Count; i++)
                {
                    foreach (var prop in dict.Value[i].GetType().GetProperties())
                    {
                        try
                        {
                            object value = prop.GetValue(dict.Value[i], null);
                            if (value != null)
                                Log.Log(LogLevel.Debug, prop.Name + "=" + value);
                            else
                                Log.Log(LogLevel.Debug, prop.Name + " is null!!");
                        }
                        catch { }
                    }
                }
                Log.Log(LogLevel.Debug, "===================");
            }
        }
    }
}
