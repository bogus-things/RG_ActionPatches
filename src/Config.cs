﻿using BepInEx.Configuration;
using BepInEx.IL2CPP;


namespace RGActionPatches
{
    class Config
    {
        private static ConfigEntry<bool> _enabled;
        internal static bool Enabled { get { return _enabled.Value; } }

        internal static void Init(BasePlugin plugin)
        {
            _enabled = plugin.Config.Bind("General", "Enable this plugin", true, "If false, this plugin will do nothing (requires game restart)");
        }
    }
}
