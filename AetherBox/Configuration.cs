using System;
using System.Collections.Generic;
using AetherBox.Debugging;
using Dalamud.Configuration;
using Dalamud.Plugin;

namespace AetherBox;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public List<string> EnabledFeatures = new List<string>();

    public bool ShowDebugFeatures;

    [NonSerialized]
    private IDalamudPluginInterface pluginInterface;

    public DebugConfig Debugging = new DebugConfig();

    public int Version { get; set; }

    public void Initialize(IDalamudPluginInterface pluginInterface)
    {
        this.pluginInterface = pluginInterface;
    }

    public void Save()
    {
        pluginInterface.SavePluginConfig(this);
    }
}

