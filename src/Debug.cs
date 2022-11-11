using BepInEx.Logging;

namespace RGActionPatches
{
    internal static class Debug
    {
        private static ManualLogSource Log = RGActionPatchesPlugin.Log;
        internal static void PrintStatusDictionaryToLog()
        {
            Log.Log(LogLevel.All, "===Status Dictionary===");
            foreach (var dict in Manager.Game.UserFile.DicStatusArchive)
            {
                foreach (var status in dict)
                {
                    foreach (var prop in status.Value.GetType().GetProperties())
                    {
                        try
                        {
                            //cmd.Execute.Target
                            object value = prop.GetValue(status.Value, null);
                            if (value != null)
                                Log.Log(LogLevel.All, prop.Name + "=" + value);
                            else
                                Log.Log(LogLevel.All, prop.Name + " is null!!");
                        }
                        catch { }

                    }
                    Log.Log(LogLevel.All, "===================");
                }
            }
        }

        internal static void PrintVisitorDictionaryToLog()
        {
            Log.Log(LogLevel.All, "===Visitor Log===");
            foreach (var dict in Manager.Game.UserFile.DicVisitors)
            {
                Log.Log(LogLevel.All, "Key: " + dict.Key);
                for(int i=0; i< dict.Value.Count; i++)
                {
                    foreach (var prop in dict.Value[i].GetType().GetProperties())
                    {
                        try
                        {
                            //cmd.Execute.Target
                            object value = prop.GetValue(dict.Value[i], null);
                            if (value != null)
                                Log.Log(LogLevel.All, prop.Name + "=" + value);
                            else
                                Log.Log(LogLevel.All, prop.Name + " is null!!");
                        }
                        catch { }
                    }
                }
                Log.Log(LogLevel.All, "===================");
            }
        }
    }
}
