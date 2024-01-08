//using AetherBox.Debugging;
using Dalamud.Configuration;
using Dalamud.Plugin;
using ECommons.Logging;
using System;
using System.Collections.Generic;

namespace AetherBox
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public List<string> EnabledFeatures = new List<string>();
        public bool showDebugFeatures;
        [NonSerialized]
        private DalamudPluginInterface PluginInterface;
        //public DebugConfig Debugging = new DebugConfig();

        public int Version { get; set; }

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            PluginInterface = pluginInterface;
        }

        public void InfoSave()
        {
            PluginInterface.SavePluginConfig(this);
            DuoLog.Information($"Config saved.");
        }
    }
}
