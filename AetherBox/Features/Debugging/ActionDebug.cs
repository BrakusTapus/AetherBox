using System;
using System.Collections.Generic;
using System.Linq;
using AetherBox.Debugging;
using AetherBox.Helpers;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;

namespace AetherBox.Features.Debugging;

public class ActionDebug : DebugHelper
{
    private unsafe readonly ActionManager* inst = ActionManager.Instance();

    private ActionType actionType;

    private uint actionID;

    private int selectedChannel;

    public override string Name => "ActionDebug".Replace("Debug", "") + " Debugging";

    public unsafe float AnimationLock => AddressHelper.ReadField<float>(inst, 8);

    public unsafe override void Draw()
    {
        ImGui.Text(Name ?? "");
        ImGui.Separator();
        ImGui.Text($"Anim lock: {AnimationLock:f3}");

        ActionManager.Instance()->UseActionLocation(actionType, actionID, 3758096384uL, null);

        List<ActionType> actionTypes = Enum.GetValues(typeof(ActionType)).Cast<ActionType>().ToList();

        int selectedTypeIndex = 0;
        string selectedTypeName = actionTypes[selectedTypeIndex].ToString();

        using (ImRaii.Combo("Action Type", selectedTypeName))
        {
            for (int i = 0; i < actionTypes.Count; i++)
            {
                if (ImGui.Selectable(actionTypes[i].ToString(), selectedTypeIndex == i))
                {
                    selectedTypeIndex = i;
                    selectedTypeName = actionTypes[selectedTypeIndex].ToString();
                }
            }
        }
    }

}
