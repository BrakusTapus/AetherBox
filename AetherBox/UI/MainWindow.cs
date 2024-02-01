using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using AetherBox.Debugging;
using AetherBox.Features;
using AetherBox.FeaturesSetup;
using AetherBox.Helpers;
using AetherBox.Helpers.Extensions;
using Dalamud.Interface.Components;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using EasyCombat.UI.Helpers;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using ImGuiNET;

namespace AetherBox.UI;

public class MainWindow : Window
{
    private readonly IDalamudTextureWrap ? IconImage;
    private readonly IDalamudTextureWrap ? BannerImage;

    private static float Scale => ImGuiHelpers.GlobalScale;

    private string searchString = string.Empty;
    private readonly List<BaseFeature> FilteredFeatures = new List<BaseFeature>();
    private bool toothless;
    public OpenCatagory OpenCatagory { get; private set; }
    public string InfoMarker { get; private set; } = "More information can be found\nby either hovering the mouse over the featurename or checkbox";
    public MainWindow(IDalamudTextureWrap bannerImage, IDalamudTextureWrap iconImage)
        : base($"{AetherBox.Name} {AetherBox.P.GetType().Assembly.GetName().Version}###{AetherBox.Name}",
               ImGuiWindowFlags.AlwaysHorizontalScrollbar | ImGuiWindowFlags.AlwaysVerticalScrollbar | ImGuiWindowFlags.AlwaysUseWindowPadding,
               false)
    {
        // Set initial size and size condition
        SizeCondition = ImGuiCond.FirstUseEver;
        Size = new Vector2(750, 400);

        // Set window size constraints
        var workingSpace = System.Windows.Forms.Screen.PrimaryScreen?.WorkingArea ?? new System.Drawing.Rectangle(0, 0, 3440, 1440);
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(300, 150),
            MaximumSize = new Vector2(workingSpace.Width - 100, workingSpace.Height - 100) // Margin for maximum size
        };

        // Initialize other properties
        RespectCloseHotkey = true;
        IconImage = iconImage;
        BannerImage = bannerImage; // Assign the passed banner image
        OnCloseSfxId = 24;
        OnOpenSfxId = 23;
        AllowPinning = true;
        AllowClickthrough = true;
    }

    public static void Dispose()
    {
    }

    public override void Draw()
    {
        var contentRegionAvail = ImGui.GetContentRegionAvail();
        _ = ref ImGui.GetStyle().ItemSpacing;
        var topLeftSideHeight = contentRegionAvail.Y;
        if (!ImGui.BeginTable("$" + AetherBox.Name + "TableContainer", 2, ImGuiTableFlags.Resizable))
        {
            return;
        }
        try
        {

            ImGui.TableSetupColumn("###LeftColumn", ImGuiTableColumnFlags.WidthFixed, ImGui.GetWindowWidth() / 2f);
            ImGui.TableNextColumn();
            Vector2 regionSize = ImGui.GetContentRegionAvail();
            ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.5f, 0.5f));
            string str_id = "###" + AetherBox.Name + "Left";
            Vector2 size = regionSize;
            size.Y = topLeftSideHeight;
            if (ImGui.BeginChild(str_id, size, border: false, ImGuiWindowFlags.NoDecoration))
            {
                DrawHeader();
                foreach (object window in Enum.GetValues(typeof(OpenCatagory)))
                {
                    if ((OpenCatagory)window != 0 && ImGui.Selectable($"{window}", OpenCatagory == (OpenCatagory)window))
                    {
                        OpenCatagory = (OpenCatagory)window;
                    }
                }
                ImGui.Spacing();
                ImGui.SetCursorPosY(ImGui.GetContentRegionMax().Y - 45f);
                ImGuiEx.ImGuiLineCentered("###Search", delegate
                {
                    ImGui.Text("Search");
                    ImGuiComponents.HelpMarker("Searches feature names and descriptions for a given word or phrase.");
                });
                ImGuiEx.SetNextItemFullWidth();
                if (ImGui.InputText("###FeatureSearch", ref searchString, 500u))
                {
                    if (searchString.Equals("toothless", StringComparison.CurrentCultureIgnoreCase) && !toothless)
                    {
                        toothless = true;
                        var YTurl = "https://www.youtube.com/watch?v=4t7BgyA7IOI";
                        Util.OpenLink(YTurl);
                    }
                    else
                    {
                        toothless = false;
                    }
                    FilteredFeatures.Clear();
                    if (searchString.Length > 0)
                    {
                        foreach (BaseFeature feature in AetherBox.P.Features)
                        {
                            if (feature.FeatureType != FeatureType.Commands && feature.FeatureType != 0 && (feature.Description.Contains(searchString, StringComparison.CurrentCultureIgnoreCase) || feature.Name.Contains(searchString, StringComparison.CurrentCultureIgnoreCase)))
                            {
                                FilteredFeatures.Add(feature);
                            }
                        }
                    }
                }
            }
            ImGui.EndChild();
            ImGui.PopStyleVar();
            ImGui.TableNextColumn();
            if (ImGui.BeginChild("###" + AetherBox.Name + "Right", Vector2.Zero, false, ImGuiWindowFlags.NoDecoration))
            {
                DrawBanner();
                if (FilteredFeatures?.Count > 0)
                {
                    DrawFeatures(FilteredFeatures?.ToArray());
                }
                else
                {
                    switch (OpenCatagory)
                    {
                        case OpenCatagory.Actions:
                            DrawFeatures(AetherBox.P.Features.Where((BaseFeature x) => x.FeatureType == FeatureType.Actions && (!x.isDebug || global::AetherBox.AetherBox.Config.ShowDebugFeatures)).ToArray());
                            break;
                        case OpenCatagory.UI:
                            DrawFeatures(AetherBox.P.Features.Where((BaseFeature x) => x.FeatureType == FeatureType.UI && (!x.isDebug || global::AetherBox.AetherBox.Config.ShowDebugFeatures)).ToArray());
                            break;
                        case OpenCatagory.Other:
                            DrawFeatures(AetherBox.P.Features.Where((BaseFeature x) => x.FeatureType == FeatureType.Other && (!x.isDebug || global::AetherBox.AetherBox.Config.ShowDebugFeatures)).ToArray());
                            break;
                        case OpenCatagory.Targets:
                            DrawFeatures(AetherBox.P.Features.Where((BaseFeature x) => x.FeatureType == FeatureType.Targeting && (!x.isDebug || global::AetherBox.AetherBox.Config.ShowDebugFeatures)).ToArray());
                            break;
                        case OpenCatagory.Chat:
                            DrawFeatures(AetherBox.P.Features.Where((BaseFeature x) => x.FeatureType == FeatureType.ChatFeature && (!x.isDebug || global::AetherBox.AetherBox.Config.ShowDebugFeatures)).ToArray());
                            break;
                        case OpenCatagory.Achievements:
                            DrawFeatures(AetherBox.P.Features.Where((BaseFeature x) => x.FeatureType == FeatureType.Achievements && (!x.isDebug || global::AetherBox.AetherBox.Config.ShowDebugFeatures)).ToArray());
                            break;
                        case OpenCatagory.Commands:
                            DrawCommands(AetherBox.P.Features.Where((BaseFeature x) => x.FeatureType == FeatureType.Commands && (!x.isDebug || global::AetherBox.AetherBox.Config.ShowDebugFeatures)).ToArray());
                            break;
                        case OpenCatagory.Debug:
                            DrawDebug(AetherBox.P.Features.Where((BaseFeature x) => (x.isDebug)).ToArray());
                            break;
                    }
                }
            }
            ImGui.EndChild();
        }
        catch (Exception ex)
        {
            ex.Log();
            ImGui.EndTable();
        }
        ImGui.EndTable();
    }

    private static void DrawDebug(BaseFeature[] features)
    {
        try
        {
            ImGui.Spacing();
            ImGuiExtra.TextCentered("Enable the forbidden features.");
            ImGui.Spacing();
            //ImGui.Checkbox("Show debug features and commands", ref AetherBox.Config.ShowDebugFeatures);

            if (features == null || !features.Any() || features.Length == 0)
                return;
            var interpolatedStringHandler1 = new DefaultInterpolatedStringHandler(13, 1);
            interpolatedStringHandler1.AppendLiteral("featureHeader");
            interpolatedStringHandler1.AppendFormatted(features.First().FeatureType);
            ImGuiEx.ImGuiLineCentered(interpolatedStringHandler1.ToStringAndClear(), () =>
            {
                var interpolatedStringHandler2 = new DefaultInterpolatedStringHandler(0, 1);
                interpolatedStringHandler2.AppendFormatted(features.First().FeatureType);
                ImGui.Text(interpolatedStringHandler2.ToStringAndClear());
            });
            ImGui.Separator();
            if (!ImGui.BeginTable("###CommandsTable", 5, ImGuiTableFlags.Borders))
                return;
            ImGui.TableSetupColumn("Name");
            ImGui.TableSetupColumn("Command");
            ImGui.TableSetupColumn("Parameters");
            ImGui.TableSetupColumn("Description");
            ImGui.TableSetupColumn("Aliases");
            ImGui.TableHeadersRow();
            foreach (var commandFeature in features.Cast<CommandFeature>())
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TextWrapped(commandFeature.Name);
                ImGui.TableNextColumn();
                ImGui.TextWrapped(commandFeature.Command);
                ImGui.TableNextColumn();
                ImGui.TextWrapped(string.Join(", ", (IEnumerable<string>)commandFeature.Parameters));
                ImGui.TableNextColumn();
                ImGui.TextWrapped(commandFeature.Description ?? "");
                ImGui.TableNextColumn();
                ImGui.TextWrapped(string.Join(", ", commandFeature.Alias) ?? "");
            }
            ImGui.EndTable();


        }
        catch (Exception ex)
        {
            Svc.Log.Error($"{ex}, Error at DrawAbout");
        }
    }

    private static void DrawCommands(BaseFeature[] features)
    {
        try
        {
            if (features == null || !features.Any() || features.Length == 0)
                return;
            var interpolatedStringHandler1 = new DefaultInterpolatedStringHandler(13, 1);
            interpolatedStringHandler1.AppendLiteral("featureHeader");
            interpolatedStringHandler1.AppendFormatted(features.First().FeatureType);
            ImGuiEx.ImGuiLineCentered(interpolatedStringHandler1.ToStringAndClear(), () =>
            {
                var interpolatedStringHandler2 = new DefaultInterpolatedStringHandler(0, 1);
                interpolatedStringHandler2.AppendFormatted(features.First().FeatureType);
                ImGui.Text(interpolatedStringHandler2.ToStringAndClear());
            });
            ImGui.Separator();
            if (!ImGui.BeginTable("###CommandsTable", 5, ImGuiTableFlags.Borders))
                return;
            ImGui.TableSetupColumn("Name");
            ImGui.TableSetupColumn("Command");
            ImGui.TableSetupColumn("Parameters");
            ImGui.TableSetupColumn("Description");
            ImGui.TableSetupColumn("Aliases");
            ImGui.TableHeadersRow();
            foreach (var commandFeature in features.Cast<CommandFeature>())
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TextWrapped(commandFeature.Name);
                ImGui.TableNextColumn();
                ImGui.TextWrapped(commandFeature.Command);
                ImGui.TableNextColumn();
                ImGui.TextWrapped(string.Join(", ", (IEnumerable<string>)commandFeature.Parameters));
                ImGui.TableNextColumn();
                ImGui.TextWrapped(commandFeature.Description ?? "");
                ImGui.TableNextColumn();
                ImGui.TextWrapped(string.Join(", ", commandFeature.Alias) ?? "");
            }
            ImGui.EndTable();
        }
        catch (Exception ex)
        {
            Svc.Log.Error($"{ex}, Error at DrawCommands");
        }
    }




    private void DrawFeatures(IEnumerable<BaseFeature> features)
    {
        try
        {
            if (features == null || !features.Any())
                return;

            // Header
            var featureType = features.First().FeatureType;
            var headerText = FilteredFeatures?.Count > 0 ? "Search Results" : featureType.ToString();
            ImGuiHelper.ImGuiLineCentered("featureHeader" + featureType, () => ImGui.Text(headerText));
            ImGuiHelper.HelpMarker(InfoMarker ?? "");
            ImGuiHelper.SeperatorWithSpacing();

            // Feature List
            foreach (var feature in features)
            {
                DrawFeature(feature);
            }
        }
        catch (Exception ex)
        {
            Svc.Log.Error($"{ex}, Error at DrawFeatures");
        }
    }

    private void DrawFeature(BaseFeature feature)
    {
        var enabled = feature.Enabled;
        if (ImGui.Checkbox("###" + feature.Name, ref enabled))
        {
            ToggleFeature(feature, enabled);
        }
        ImGuiHelper.ColoredTextTooltip(feature.Description ?? "", AetherColor.GhostType);
        ImGui.SameLine();
        feature.DrawConfig(ref enabled);
        ImGuiHelper.SeperatorWithSpacing();
    }

    private void ToggleFeature(BaseFeature feature, bool enabled)
    {
        try
        {
            if (enabled)
            {
                feature.Enable();
                if (feature.Enabled)
                {
                    AetherBox.Config?.EnabledFeatures.Add(feature.GetType().Name);
                }
            }
            else
            {
                feature.Disable();
                AetherBox.Config.EnabledFeatures.RemoveAll(x => x == feature.GetType().Name);
            }

            AetherBox.Config.Save();
        }
        catch (Exception ex)
        {
            Svc.Log.Error(ex, "Failed to enable/disable " + feature.Name);
        }
    }




    /* // testing other method
        private void DrawFeatures(IEnumerable<BaseFeature> features)
        {
            try
            {
                if (features == null || !features.Any() || !features.Any())
                    return;
                var interpolatedStringHandler1 = new DefaultInterpolatedStringHandler(13, 1);
                interpolatedStringHandler1.AppendLiteral("featureHeader");
                interpolatedStringHandler1.AppendFormatted(features.First().FeatureType);
                ImGuiEx.ImGuiLineCentered(interpolatedStringHandler1.ToStringAndClear(), () =>
                {
                    if (FilteredFeatures?.Count > 0)
                    {
                        ImGui.Text("Search Results");
                    }
                    else
                    {
                        var interpolatedStringHandler2 = new DefaultInterpolatedStringHandler(0, 1);
                        interpolatedStringHandler2.AppendFormatted(features.First().FeatureType);
                        ImGui.Text(interpolatedStringHandler2.ToStringAndClear());
                    }
                });
                ImGui.Separator();
                foreach (var feature1 in features)
                {
                    var feature = feature1;
                    var enabled = feature.Enabled;
                    if (ImGui.Checkbox("###" + feature.Name, ref enabled))
                    {
                        if (enabled)
                        {
                            try
                            {
                                feature.Enable();
                                if (feature.Enabled)
                                {
                                    AetherBox.Config?.EnabledFeatures.Add(feature.GetType().Name);
                                }
                            }
                            catch (Exception ex)
                            {
                                Svc.Log.Error(ex, "Failed to enabled " + feature.Name);
                            }
                        }
                        else
                        {
                            try
                            {
                                feature.Disable();
                                AetherBox.Config.EnabledFeatures.RemoveAll(x => x == feature.GetType().Name);
                            }
                            catch (Exception ex)
                            {
                                Svc.Log.Error(ex, "Failed to enabled " + feature.Name);
                            }
                        }
                        AetherBox.Config.Save();
                    }
                    ImGui.SameLine();
                    feature.DrawConfig(ref enabled);
                    ImGui.Spacing();
                    ImGui.TextWrapped(feature.Description ?? "");
                    ImGui.Separator();
                }
            }
            catch (Exception ex)
            {
                Svc.Log.Error($"{ex}, Error at DrawFeatures");
            }
        }
    */

    private void DrawHeader()
    {
        try
        {
            // Calculate the available width for the header and constrain the image to that width while maintaining aspect ratio
            var availableWidth = ImGui.GetContentRegionAvail().X;
            if (IconImage != null)
            {
                var aspectRatio = (float)IconImage.Width / IconImage.Height;
                var imageWidth = availableWidth;
                var imageHeight = imageWidth / aspectRatio;

                // Ensure the image is not taller than a certain threshold, e.g., 100 pixels
                var maxHeight = 100.0f * Scale;
                if (imageHeight > maxHeight)
                {
                    imageHeight = maxHeight;
                    imageWidth = imageHeight * aspectRatio;
                }

                // Center the image in the available space
                var spaceBeforeImage = (availableWidth - imageWidth) * 0.5f;
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + spaceBeforeImage);

                // Draw the image
                ImGui.Image(this.IconImage.ImGuiHandle, new Vector2(imageWidth, imageHeight));
            }
            else
            {
                Svc.Log.Error("Icon Image is Null!");
            }
        }
        catch (Exception ex)
        {
            Svc.Log.Error($"{ex}, Error at DrawHeader");
        }

    }

    private void DrawBanner()
    {
        try
        {
            // Calculate the available width for the header and constrain the image to that width while maintaining aspect ratio
            var availableWidth = ImGui.GetContentRegionAvail().X;
            if (BannerImage != null)
            {
                var aspectRatio = (float)BannerImage.Width / BannerImage.Height;
                var imageWidth = availableWidth;
                var imageHeight = imageWidth / aspectRatio;

                // Ensure the image is not taller than a certain threshold, e.g., 100 pixels
                var maxHeight = 100.0f * Scale;
                if (imageHeight > maxHeight)
                {
                    imageHeight = maxHeight;
                    imageWidth = imageHeight * aspectRatio;
                }

                // Center the image in the available space
                var spaceBeforeImage = (availableWidth - imageWidth) * 0.5f;
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + spaceBeforeImage);

                // Draw the image
                ImGui.Image(this.BannerImage.ImGuiHandle, new Vector2(imageWidth, imageHeight));
            }
            else
            {
                Svc.Log.Error("Icon Image is Null!");
            }
        }
        catch (Exception ex)
        {
            Svc.Log.Error($"{ex}, Error at DrawHeader");
        }

    }

    /// <summary>
    /// Code to be executed when the window is closed.
    /// </summary>
    public override void OnClose()
    {
        try
        {
            AetherBox.Config.Save();
        }
        catch (Exception ex)
        {
            Svc.Log.Error($"{ex}, Error at OnClose");
        }
    }
}
