﻿using System.Numerics;
using AetherBox.Helpers;
using Dalamud.Interface.Internal;
using Dalamud.Utility;
using ImGuiNET;
using static System.Net.WebRequestMethods;

namespace AetherBox.UI;
internal static class QuickLinks
{
    public class LinkInfo
    {
        public string Url { get; set; }
        public string ImageUrl { get; set; }
        public string Tooltip { get; set; }
    }

    internal static void DrawQuickLinks()
    {
        int numColumns = 2; // Number of columns in the table
        var tableWidth = ImGui.GetContentRegionAvail().X; // Get the available width in the window

        // Calculate the width for each column based on the available width and the number of columns
        var columnWidth = tableWidth / numColumns;

        // Begin a new ImGui table with specified flags (borders, fixed sizing, reorderable)
        ImGui.BeginTable(AetherBox.Name, numColumns, ImGuiTableFlags.Borders | ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.Reorderable);

        // Set up columns within the table based on the calculated column width
        for (int columnIndex = 0; columnIndex < numColumns; columnIndex++)
        {
            ImGui.TableSetupColumn($"{AetherBox.Name}Column{columnIndex}", ImGuiTableColumnFlags.WidthFixed, columnWidth);
        }

        int columnCount = 0; // Keep track of the current column count

        // Loop through the list of quickLinks and display them in the table
        foreach (var link in quickLinks)
        {
            if (columnCount == 0)
            {
                ImGui.TableNextRow(); // Start a new row in the table after every 'numColumns' columns
            }

            if (IconSet.GetTexture(link.ImageUrl, out var icon))
            {
                // Display the image in a new column with specified width and height
                ImGuiHelper.ImageInNewColumn(icon, tableWidth, tableWidth / numColumns, icon.Height);
                if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                {
                    Util.OpenLink(link.Url); // Open the link when the image is clicked
                }
            }

            ImGuiHelper.Tooltip(link.Tooltip); // Display a tooltip for the link
            columnCount++;

            if (columnCount == numColumns)
            {
                columnCount = 0; // Reset the column count when reaching the specified number of columns
            }
        }

        ImGui.EndTable(); // End the ImGui table
    }

    // Define an array or list of LinkInfo objects
    private static List<LinkInfo> quickLinks = new List<LinkInfo>
    {
        new LinkInfo
        {
            Url = "https://eu.finalfantasyxiv.com/lodestone/topics/detail/8e7360878c6a7c3209614b36a801a783f74ff21d",
            ImageUrl = "https://img.finalfantasyxiv.com/t/1dcbf39c97285ba9a42012eecf2c031f0ffbceb1.png?1696927293",
            Tooltip = "Patch 6.55 Notes"
        },
        new LinkInfo
        {
            Url = "https://i.imgur.com/MXnsoOf.png",
            ImageUrl = "https://i.imgur.com/MXnsoOf.png",
            Tooltip = "FFXIV website"
        },
        new LinkInfo
        {
            Url = "https://eu.finalfantasyxiv.com/lodestone/",
            ImageUrl = "https://i.imgur.com/m8UItW8.jpeg",
            Tooltip = "Eorzea Database"
        },
        new LinkInfo
        {
            Url = "https://eu.finalfantasyxiv.com/jobguide/battle/?utm_source=lodestone&utm_medium=pc_banner&utm_campaign=eu_jobguide",
            ImageUrl = "https://lds-img.finalfantasyxiv.com/promo/h/1/q0Aq7Pl3uvaFbj7rEswEa1kAHw.png",
            Tooltip = "Job Guides (FFXIV)"
        },
        new LinkInfo
        {
            Url = "https://www.icy-veins.com/ffxiv/",
            ImageUrl = "https://i.imgur.com/Qk2buNL.jpeg",
            Tooltip = "Job Guides (Icy Veins)"
        },
        new LinkInfo
        {
            Url = "https://etro.gg/",
            ImageUrl = "https://i.imgur.com/dfEeA3a.png",
            Tooltip = "Etro.GG"
        },
        // Add as many links as needed
    };

}
