using AetherBox.UI;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using ECommons;
using ECommons.DalamudServices;
using Dalamud.Interface.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using AetherBox.Features;
using System.Linq;
using System.Reflection;
using ImGuiNET;
using ECommons.Automation;
using Module = ECommons.Module;
using Lumina.Data.Parsing;

#nullable disable

namespace AetherBox;

/// <summary>
/// The AetherBox class is the main entry point of the plugin, implementing the functionality required for the plugin.
/// </summary>
/// <remarks> This class implements IDalamudPlugin and IDisposable interfaces for integration with the Dalamud plugin architecture and resource management, respectively.</remarks>
public class AetherBox : IDalamudPlugin, IDisposable
{
    //public List<FeatureProvider> FeatureProviders = [];
    public List<FeatureProvider> FeatureProviders;
    public IEnumerable<BaseFeature> Features
    {
        get
        {
            return this.FeatureProviders.Where(x => !x.Disposed).SelectMany(x => x.Features).OrderBy(x => x.Name);
        }
    }


    public static Configuration Config;
    public static string Name => nameof(AetherBox);

    public DalamudPluginInterface PluginInterface { get; private set; }

    internal WindowSystem WindowSystem;
    internal MainWindow MainWindow;
    internal OldMainWindow OldMainWindow;

    internal TaskManager TaskManager;
    internal static AetherBox Plugin;
    internal static DalamudPluginInterface pluginInterface;

    private FeatureProvider provider;
    private const string CommandName = "/atb";






    //internal static DalamudPluginInterface PluginInterface;

    //public static Configuration Config;

    //private FeatureProvider provider;

    ///public List<FeatureProvider> ? FeatureProviders;


    //internal TaskManager TaskManager;
    //public static string Name => nameof(AetherBox);

    //private IEnumerable<BaseFeature> _features;
    /*public IEnumerable<BaseFeature> Features
    {
        get
        {
            if (_features == null)
            {
                _features = FeatureProviders
                                .Where(x => !x.Disposed)
                                .SelectMany(x => x.Features)
                                .OrderBy(x => x.Name)
                                .ToList(); // ToList to materialize the query and avoid repeated evaluation
}

            return _features;
        }
    }*/
    public AetherBox(IDalamudPlugin plugin, DalamudPluginInterface pluginInterface)
    {
        try
        {
            Svc.Log.Debug($"Setting 'Plugin' to the current instance.");
            plugin = this;

            Svc.Log.Debug($"Initializing ECommonsMain with pluginInterface, current instance, and modules.");
            ECommonsMain.Init(PluginInterface, this, Module.DalamudReflector, Module.ObjectFunctions);

            Svc.Log.Debug($"Setting AetherBox's pluginInterface.");
            PluginInterface = pluginInterface;

            Svc.Log.Debug($"Creating WindowSystem with name: {Name}.");
            WindowSystem = new WindowSystem(Name);
            Svc.Log.Debug($"WindowSystem created successfully.");

            var imageClosePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "close.png");
            var closeImage = PluginInterface.UiBuilder.LoadImage(imageClosePath);
            var imagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "icon.png");
            var iconImage = PluginInterface.UiBuilder.LoadImage(imagePath);

            Svc.Log.Debug($"Creating OldMainWindow with Plugin, iconImage, and closeImage.");
            OldMainWindow = new OldMainWindow(Plugin, iconImage, closeImage);
            Svc.Log.Debug($"OldMainWindow created successfully.");


            Svc.Log.Debug($"Creating MainWindow with Plugin.");
            MainWindow = new MainWindow(Plugin);
            Svc.Log.Debug($"MainWindow created successfully.");

            Svc.Log.Debug($"Adding Window for MainWindow.");
            WindowSystem.AddWindow(MainWindow);

            Svc.Log.Debug($"Adding Window for OldMainWindow.");
            WindowSystem.AddWindow(OldMainWindow);

            Svc.Log.Debug($"Get a previously saved plugin configuration or null if none was saved before.");
            if (PluginInterface.GetPluginConfig() is not Configuration configuration) configuration = new Configuration();
            Config = configuration;

            Svc.Log.Debug($"Initialize Config's pluginInterface");
            Config.Initialize(Svc.PluginInterface);

            Svc.Log.Debug($"Adding command /atb");
            Svc.Commands.AddHandler("/atb", new CommandInfo(new CommandInfo.HandlerDelegate(OnCommandMainUI))
            {
                HelpMessage = "Opens the " + Name + " menu.",
                ShowInHelp = true
            });

            Svc.Log.Debug($"Adding command /atbold");
            Svc.Commands.AddHandler("/atbold", new CommandInfo(new CommandInfo.HandlerDelegate(OnCommandOldMainUI))
            {
                HelpMessage = "Opens the Old" + Name + " menu.",
                ShowInHelp = true
            });

            Svc.Log.Debug($"Subscribing to UI builder's Draw events'");
            Svc.PluginInterface.UiBuilder.Draw += new Action(WindowSystem.Draw);

            Svc.Log.Debug($"Subscribing to UI builder's  OpenConfigUi events'");
            Svc.PluginInterface.UiBuilder.OpenConfigUi += new Action(DrawConfigUI);

            Svc.Log.Debug($"Subscribing to UI builder's OpenMainUI events'");
            Svc.PluginInterface.UiBuilder.OpenMainUi -= new Action(DrawConfigUI);

            Svc.Log.Debug($"Getting the assembly that the current code is running from.");
            provider = new FeatureProvider(Assembly.GetExecutingAssembly());

            Svc.Log.Debug($"loading features from assembly'");
            provider.LoadFeatures();

            Svc.Log.Debug($"Adding given object to end of this list.");
            FeatureProviders.Add(provider);

            //this.DebugWindow = new DebugWindow();
            //this.WindowSystem.AddWindow((Window)this.DebugWindow);
            //this.TaskManager = new TaskManager();
            //PandorasBoxIPC.Init();
            //Common.Setup();
        }
        catch (Exception ex)
        {
            Svc.Log.Error($"{ex}, Error during Plugin initialisation");
        }
    }

    public AetherBox()
    {
    }

    public AetherBox(List<FeatureProvider> featureProviders, FeatureProvider provider)
    {
        FeatureProviders = featureProviders ?? throw new ArgumentNullException(nameof(featureProviders));
        this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        try
        {
            GC.SuppressFinalize(this);
            Svc.Log.Debug($"Removing Command Handler '/atb''");
            Svc.Commands.RemoveHandler("/atb");

            Svc.Log.Debug($"Removing Command Handler '/atb'old'");
            Svc.Commands.RemoveHandler("/atbold");

            Svc.Log.Debug($"Disabling each BaseFeature");
            foreach (var baseFeature in Features.Where(x => x != null && x.Enabled)) baseFeature.Disable();

            Svc.Log.Debug($"Unloading Features");
            provider.UnloadFeatures();

            Svc.Log.Debug($"Unsubscribing from UI Builder's draw events");
            Svc.PluginInterface.UiBuilder.Draw -= new Action(WindowSystem.Draw);

            Svc.Log.Debug($"Unsubscribing from UI Builder's OpenConfigUi events");
            Svc.PluginInterface.UiBuilder.OpenConfigUi -= new Action(DrawConfigUI);

            Svc.Log.Debug($"Unsubscribing from UI Builder's OpenMainUi events");
            Svc.PluginInterface.UiBuilder.OpenMainUi -= new Action(DrawMainUI);

            Svc.Log.Debug($"Removing all windows from this Dalamud.Interface.Windowing.WindowSystem.");
            WindowSystem.RemoveAllWindows();

            Svc.Log.Debug($"Setting Property 'OldMainWindow' to null!");
            OldMainWindow = null;

            Svc.Log.Debug($"Setting Property 'MainWindow' to null!");
            MainWindow = null;

            Svc.Log.Debug($"Setting Property 'WindowSystem' to null!");
            WindowSystem = null;

            Svc.Log.Debug($"Initiating disposal of ECommons features.");
            ECommonsMain.Dispose();

            Svc.Log.Debug($"Clearing the content of the FeatureProviders list.");
            FeatureProviders.Clear();

            Svc.Log.Debug($"Setting Property 'Plugin' to null!");
            //Plugin = null;

            //PandorasBoxIPC.Dispose();
            //this.DebugWindow = (DebugWindow)null;
            //Common.Shutdown();
        }
        catch (Exception ex)
        {
            Svc.Log.Error($"{ex}, Error during Disposing");
        }
    }


    internal void OnCommandMainUI(string command, string args)
    {
        try
        {
            //if ((args == "debug" || args == "d") && AetherBox.Config.showDebugFeatures)
            //DebugWindow.IsOpen = !DebugWindow.IsOpen;
            //else
            MainWindow.IsOpen = !MainWindow.IsOpen;
        }
        catch (Exception ex)
        {
            Svc.Log.Error($"{ex}, Error with 'OnCommand'");
        }
    }

    internal void OnCommandOldMainUI(string command, string args)
    {
        try
        {
            OldMainWindow.IsOpen = !OldMainWindow.IsOpen;
        }
        catch (Exception ex)
        {
            Svc.Log.Error($"{ex}, Error with 'OnCommandMainUI'");
        }
    }

    public void DrawConfigUI()
    {
        try
        {
            MainWindow.IsOpen = !MainWindow.IsOpen;
            if (!Svc.PluginInterface.IsDevMenuOpen || (!Svc.PluginInterface.IsDev && !AetherBox.Config.showDebugFeatures) || !ImGui.BeginMainMenuBar())
                return;
            if (ImGui.MenuItem(AetherBox.Name))
            {
                //if (ImGui.GetIO().KeyShift)
                //this.DebugWindow.IsOpen = !this.DebugWindow.IsOpen;
                //else
                MainWindow.IsOpen = !MainWindow.IsOpen;
            }
            ImGui.EndMainMenuBar();
        }
        catch (Exception ex)
        {
            Svc.Log.Warning($"{ex}, Error in DrawConfigUI");
        }
    }

    public void DrawMainUI()
    {
        try
        {
            OldMainWindow.IsOpen = !OldMainWindow.IsOpen;
            if (!Svc.PluginInterface.IsDevMenuOpen || (!Svc.PluginInterface.IsDev && !AetherBox.Config.showDebugFeatures) || !ImGui.BeginMainMenuBar())
                return;
            if (ImGui.MenuItem(AetherBox.Name))
            {
                OldMainWindow.IsOpen = !OldMainWindow.IsOpen;
            }
            ImGui.EndMainMenuBar();
        }
        catch (Exception ex)
        {
            Svc.Log.Warning($"{ex}, Error in DrawMainUI");
        }
    }

    /// <summary>
    /// Method: Loads an image from the 'Images' folder within the assembly directory.
    /// </example>
    public IDalamudTextureWrap LoadImage(string imageName)
    {
        try
        {
            // Assuming the 'Images' folder is in the same directory as the assembly
            var imagesDirectory = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!);
            var imagePath = Path.Combine(imagesDirectory, imageName);

            // Check if the file exists before trying to load it
            if (File.Exists(imagePath))
            {
                try
                {
                    return PluginInterface.UiBuilder.LoadImage(imagePath);
                }
                catch (Exception ex)
                {
                    Svc.Log.Warning($"{ex}, Error loading image");
                    return null;
                }
            }
            else
            {
                Svc.Log.Error($"Image not found: {imagePath}");
                return null;
            }
        }
        catch (Exception ex)
        {
            Svc.Log.Warning($"{ex}, Error in LoadImage");
            return null;
        }
    }
}
