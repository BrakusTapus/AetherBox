using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using AetherBox;
using AetherBox.Data;
using AetherBox.Features;
using AetherBox.FeaturesSetup;
using AetherBox.Helpers;
using ClickLib.Clicks;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Components;
using Dalamud.Logging;
using Dalamud.Memory;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using EasyCombat.UI.Helpers;
using ECommons;
using ECommons.Automation;
using ECommons.Automation.LegacyTaskManager;
using ECommons.DalamudServices;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using Newtonsoft.Json;
using static Dalamud.Interface.Utility.Raii.ImRaii;

namespace AetherBox.Features;

public abstract class BaseFeature
{
    public delegate void OnJobChangeDelegate(uint? jobId);

    protected delegate void DrawConfigDelegate(ref bool hasChanged);

    protected global::AetherBox.AetherBox P;

    protected IDalamudPluginInterface Pi;

    protected Configuration config;

    protected TaskManager TaskManager;

    private uint? jobID = Svc.ClientState.LocalPlayer?.ClassJob.Id;

    public static readonly SeString AetherBoxPayload = new SeString(new UIForegroundPayload(32)).Append($"{SeIconChar.BoxedLetterA.ToIconString()}{SeIconChar.BoxedLetterE.ToIconString()}{SeIconChar.BoxedLetterT.ToIconString()}{SeIconChar.BoxedLetterH.ToIconString()}{SeIconChar.BoxedLetterE.ToIconString()}{SeIconChar.BoxedLetterR.ToIconString()}{SeIconChar.BoxedLetterB.ToIconString()}{SeIconChar.BoxedLetterO.ToIconString()}{SeIconChar.BoxedLetterX.ToIconString()} ").Append(new UIForegroundPayload(0));

    public FeatureProvider Provider { get; private set; }

    public virtual bool Enabled { get; protected set; }

    public abstract string Name { get; }

    public virtual string Key => GetType().Name;

    public abstract string Description { get; }

    public uint? JobID
    {
        get
        {
            return jobID;
        }
        set
        {
            if (value.HasValue && jobID != value)
            {
                jobID = value;
                this.OnJobChanged?.Invoke(value);
            }
        }
    }

    public virtual bool Ready { get; protected set; }

    public virtual FeatureType FeatureType { get; }

    public virtual bool isDebug { get; }

    public virtual bool UseAutoConfig => false;

    public string LocalizedName => Name;

    protected virtual DrawConfigDelegate DrawConfigTree => null;

    internal unsafe static bool IsTargetLocked => ((byte*)TargetSystem.Instance())[309] == 1;

    internal static bool GenericThrottle => EzThrottler.Throttle("AetherBoxGenericThrottleGenericThrottle", 200);

    public event OnJobChangeDelegate OnJobChanged;

    public static float RemainingRecastTime { get; }

    public virtual void Draw()
    {
    }

    public void InterfaceSetup(global::AetherBox.AetherBox plugin, IDalamudPluginInterface pluginInterface, Configuration config, FeatureProvider fp)
    {
        P = plugin;
        Pi = pluginInterface;
        this.config = config;
        Provider = fp;
        TaskManager = new TaskManager();
    }

    public virtual void Setup()
    {
        TaskManager.TimeoutSilently = true;
        Ready = true;
    }

    public virtual void Enable()
    {
        Svc.Log.Debug("Enabling " + Name);
        Svc.Framework.Update += CheckJob;
        Enabled = true;
    }

    private void CheckJob(IFramework framework)
    {
        if ((object)Svc.ClientState.LocalPlayer != null)
        {
            JobID = Svc.ClientState.LocalPlayer.ClassJob.Id;
        }
    }

    public virtual void Disable()
    {
        Svc.Framework.Update -= CheckJob;
        Enabled = false;
    }

    public virtual void Dispose()
    {
        Ready = false;
    }

    protected T LoadConfig<T>() where T : FeatureConfig
    {
        return LoadConfig<T>(Key);
    }

    protected T LoadConfig<T>(string key) where T : FeatureConfig
    {
        try
        {
            string configFile;
            configFile = Path.Combine(Pi.AssemblyLocation.DirectoryName, key + ".json");
            if (!File.Exists(configFile))
            {
                return null;
            }
            return JsonConvert.DeserializeObject<T>(File.ReadAllText(configFile));
        }
        catch (Exception ex)
        {
            Svc.Log.Error(ex, "Failed to load config for feature " + Name);
            return null;
        }
    }

    protected void SaveConfig<T>(T config) where T : FeatureConfig
    {
        SaveConfig(config, Key);
    }

    protected void SaveConfig<T>(T config, string key) where T : FeatureConfig
    {
        try
        {
            string path;
            path = Path.Combine(Pi.AssemblyLocation.DirectoryName, key + ".json");
            string jsonString;
            jsonString = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(path, jsonString);
        }
        catch (Exception ex)
        {
            Svc.Log.Error(ex, "Feature failed to write config " + Name);
        }
    }

    private void DrawAutoConfig()
    {
        bool configChanged;
        configChanged = false;
        try
        {
            object configObj;
            configObj = GetType().GetProperties().FirstOrDefault((PropertyInfo p) => p.PropertyType.IsSubclassOf(typeof(FeatureConfig))).GetValue(this);
            IOrderedEnumerable<(FieldInfo f, FeatureConfigOptionAttribute)> orderedEnumerable;
            orderedEnumerable = from f in configObj.GetType().GetFields()
                                where f.GetCustomAttribute(typeof(FeatureConfigOptionAttribute)) != null
                                select (f: f, (FeatureConfigOptionAttribute)f.GetCustomAttribute(typeof(FeatureConfigOptionAttribute))) into a
                                orderby a.Item2.Priority, a.Item2.Name
                                select a;
            int configOptionIndex;
            configOptionIndex = 0;
            foreach (var (f2, attr) in orderedEnumerable)
            {
                if (attr.ConditionalDisplay)
                {
                    MethodInfo conditionalMethod;
                    conditionalMethod = configObj.GetType().GetMethod("ShouldShow" + f2.Name, BindingFlags.Instance | BindingFlags.Public);
                    if (conditionalMethod != null && !(bool)(conditionalMethod.Invoke(configObj, Array.Empty<object>()) ?? ((object)true)))
                    {
                        continue;
                    }
                }
                if (attr.SameLine)
                {
                    ImGui.SameLine();
                }
                if (attr.Editor != null)
                {
                    object v;
                    v = f2.GetValue(configObj);
                    object[] arr;
                    arr = new object[2]
                    {
                        $"{attr.Name}##{f2.Name}_{GetType().Name}_{configOptionIndex++}",
                        v
                    };
                    if ((bool)attr.Editor.Invoke(null, arr))
                    {
                        configChanged = true;
                        f2.SetValue(configObj, arr[1]);
                    }
                }
                else if (f2.FieldType == typeof(bool))
                {
                    bool v2;
                    v2 = (bool)f2.GetValue(configObj);
                    if (ImGui.Checkbox($"{attr.Name}##{f2.Name}_{GetType().Name}_{configOptionIndex++}", ref v2))
                    {
                        configChanged = true;
                        f2.SetValue(configObj, v2);
                    }
                }
                else if (f2.FieldType == typeof(int))
                {
                    int v3;
                    v3 = (int)f2.GetValue(configObj);
                    ImGui.SetNextItemWidth((attr.EditorSize == -1) ? (-1f) : ((float)attr.EditorSize * ImGui.GetIO().FontGlobalScale));
                    bool e;
                    e = attr.IntType switch
                    {
                        FeatureConfigOptionAttribute.NumberEditType.Slider => ImGui.SliderInt($"{attr.Name}##{f2.Name}_{GetType().Name}_{configOptionIndex++}", ref v3, attr.IntMin, attr.IntMax),
                        FeatureConfigOptionAttribute.NumberEditType.Drag => ImGui.DragInt($"{attr.Name}##{f2.Name}_{GetType().Name}_{configOptionIndex++}", ref v3, 1f, attr.IntMin, attr.IntMax),
                        _ => false,
                    };
                    if (v3 % attr.IntIncrements != 0)
                    {
                        v3 = v3.RoundOff(attr.IntIncrements);
                        if (v3 < attr.IntMin)
                        {
                            v3 = attr.IntMin;
                        }
                        if (v3 > attr.IntMax)
                        {
                            v3 = attr.IntMax;
                        }
                    }
                    if (attr.EnforcedLimit && v3 < attr.IntMin)
                    {
                        v3 = attr.IntMin;
                        e = true;
                    }
                    if (attr.EnforcedLimit && v3 > attr.IntMax)
                    {
                        v3 = attr.IntMax;
                        e = true;
                    }
                    if (e)
                    {
                        f2.SetValue(configObj, v3);
                        configChanged = true;
                    }
                }
                else if (f2.FieldType == typeof(float))
                {
                    float v4;
                    v4 = (float)f2.GetValue(configObj);
                    ImGui.SetNextItemWidth((attr.EditorSize == -1) ? (-1f) : ((float)attr.EditorSize * ImGui.GetIO().FontGlobalScale));
                    bool e2;
                    e2 = attr.IntType switch
                    {
                        FeatureConfigOptionAttribute.NumberEditType.Slider => ImGui.SliderFloat($"{attr.Name}##{f2.Name}_{GetType().Name}_{configOptionIndex++}", ref v4, attr.FloatMin, attr.FloatMax, attr.Format),
                        FeatureConfigOptionAttribute.NumberEditType.Drag => ImGui.DragFloat($"{attr.Name}##{f2.Name}_{GetType().Name}_{configOptionIndex++}", ref v4, 1f, attr.FloatMin, attr.FloatMax, attr.Format),
                        _ => false,
                    };
                    if (v4 % attr.FloatIncrements != 0f)
                    {
                        v4 = v4.RoundOff(attr.FloatIncrements);
                        if (v4 < attr.FloatMin)
                        {
                            v4 = attr.FloatMin;
                        }
                        if (v4 > attr.FloatMax)
                        {
                            v4 = attr.FloatMax;
                        }
                    }
                    if (attr.EnforcedLimit && v4 < attr.FloatMin)
                    {
                        v4 = attr.FloatMin;
                        e2 = true;
                    }
                    if (attr.EnforcedLimit && v4 > attr.FloatMax)
                    {
                        v4 = attr.FloatMax;
                        e2 = true;
                    }
                    if (e2)
                    {
                        f2.SetValue(configObj, v4);
                        configChanged = true;
                    }
                }
                else
                {
                    ImGui.Text("Invalid Auto Field Type: " + f2.Name);
                }
                if (!attr.HelpText.IsNullOrEmpty())
                {
                    ImGuiComponents.HelpMarker(attr.HelpText);
                }
            }
            if (configChanged)
            {
                SaveConfig((FeatureConfig)configObj);
            }
        }
        catch (Exception ex)
        {
            ImGui.Text("Error with AutoConfig: " + ex.Message);
            ImGui.TextWrapped(ex.StackTrace ?? "");
        }
    }

    public bool DrawConfig(ref bool hasChanged)
    {
        bool configTreeOpen;
        configTreeOpen = false;
        if ((UseAutoConfig || DrawConfigTree != null) && Enabled)
        {
            float x;
            x = ImGui.GetCursorPosX();
            if (ImGui.TreeNode(Name + "##treeConfig_" + GetType().Name))
            {
                configTreeOpen = true;
                ImGui.SetCursorPosX(x);
                ImGui.BeginGroup();
                if (UseAutoConfig)
                {
                    DrawAutoConfig();
                }
                else
                {
                    DrawConfigTree(ref hasChanged);
                }
                ImGui.EndGroup();
                ImGui.TreePop();
            }
        }
        else
        {
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, 0u);
            ImGui.PushStyleColor(ImGuiCol.HeaderActive, 0u);
            ImGui.TreeNodeEx(LocalizedName, ImGuiTreeNodeFlags.NoTreePushOnOpen | ImGuiTreeNodeFlags.Leaf);
            ImGui.PopStyleColor();
            ImGui.PopStyleColor();
        }
        if (hasChanged && Enabled)
        {
            ConfigChanged();
        }
        return configTreeOpen;
    }

    protected virtual void ConfigChanged()
    {
        if (this == null)
        {
            return;
        }
        PropertyInfo config;
        config = GetType().GetProperties().FirstOrDefault((PropertyInfo p) => p.PropertyType.IsSubclassOf(typeof(FeatureConfig)));
        if (config != null)
        {
            object configObj;
            configObj = config.GetValue(this);
            if (configObj != null)
            {
                SaveConfig((FeatureConfig)configObj);
            }
        }
    }

    public unsafe bool IsRpWalking()
    {
        if (Svc.ClientState.LocalPlayer == null)
        {
            return false;
        }
        if (Svc.GameGui.GetAddonByName("_DTR") == IntPtr.Zero)
        {
            return false;
        }
        AtkUnitBase* addon;
        addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("_DTR");
        if (addon->UldManager.NodeListCount < 9)
        {
            return false;
        }
        try
        {
            return addon->GetNodeById(10u)->IsVisible();
        }
        catch (Exception e)
        {
            e.Log();
            return false;
        }
    }

    internal unsafe static int GetInventoryFreeSlotCount()
    {
        InventoryType[] obj;
        obj = new InventoryType[4]
        {
            InventoryType.Inventory1,
            InventoryType.Inventory2,
            InventoryType.Inventory3,
            InventoryType.Inventory4
        };
        InventoryManager* c;
        c = InventoryManager.Instance();
        int slots;
        slots = 0;
        InventoryType[] array;
        array = obj;
        foreach (InventoryType x in array)
        {
            InventoryContainer* inv;
            inv = c->GetInventoryContainer(x);
            for (int i = 0; i < inv->Size; i++)
            {
                if (inv->Items[i].ItemId == 0)
                {
                    slots++;
                }
            }
        }
        return slots;
    }

    internal static bool IsInventoryFree()
    {
        return GetInventoryFreeSlotCount() >= 1;
    }

    public unsafe bool IsMoving()
    {
        return AgentMap.Instance()->IsPlayerMoving == 1;
    }

    public void PrintModuleMessage(string msg)
    {
        XivChatEntry message;
        message = new XivChatEntry
        {
            Message = new SeStringBuilder().AddUiForeground("[" + global::AetherBox.AetherBox.Name + "] ", 45).AddUiForeground("[" + Name + "] ", 62).AddText(msg)
                .Build()
        };
        Svc.Chat.Print(message);
    }

    public void PrintModuleMessage(SeString msg)
    {
        XivChatEntry message;
        message = new XivChatEntry
        {
            Message = new SeStringBuilder().AddUiForeground("[" + global::AetherBox.AetherBox.Name + "] ", 45).AddUiForeground("[" + Name + "] ", 62).Append(msg)
                .Build()
        };
        Svc.Chat.Print(message);
    }

    internal unsafe static AtkUnitBase* GetSpecificYesno(Predicate<string> compare)
    {
        for (int i = 1; i < 100; i++)
        {
            try
            {
                AtkUnitBase* addon;
                addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("SelectYesno", i);
                if (addon == null)
                {
                    return null;
                }
                if (GenericHelpers.IsAddonReady(addon))
                {
                    string text;
                    text = MemoryHelper.ReadSeString(&addon->UldManager.NodeList[15]->GetAsAtkTextNode()->NodeText).ExtractText();
                    if (compare(text))
                    {
                        Svc.Log.Verbose($"SelectYesno {text} addon {i} by predicate");
                        return addon;
                    }
                }
            }
            catch (Exception e)
            {
                Svc.Log.Error("", e);
                return null;
            }
        }
        return null;
    }

    internal unsafe static AtkUnitBase* GetSpecificYesno(params string[] s)
    {
        for (int i = 1; i < 100; i++)
        {
            try
            {
                AtkUnitBase* addon;
                addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("SelectYesno", i);
                if (addon == null)
                {
                    return null;
                }
                if (GenericHelpers.IsAddonReady(addon) && MemoryHelper.ReadSeString(&addon->UldManager.NodeList[15]->GetAsAtkTextNode()->NodeText).ExtractText().Replace(" ", "")
                    .EqualsAny<string>(s.Select((string x) => x.Replace(" ", ""))))
                {
                    Svc.Log.Verbose($"SelectYesno {s.Print()} addon {i}");
                    return addon;
                }
            }
            catch (Exception e)
            {
                Svc.Log.Error("", e);
                return null;
            }
        }
        return null;
    }

    internal static bool TrySelectSpecificEntry(string text, Func<bool> Throttler = null)
    {
        return TrySelectSpecificEntry(new string[1] { text }, Throttler);
    }

    internal unsafe static bool TrySelectSpecificEntry(IEnumerable<string> text, Func<bool> Throttler = null)
    {
        if (GenericHelpers.TryGetAddonByName<AddonSelectString>("SelectString", out var addon) && GenericHelpers.IsAddonReady(&addon->AtkUnitBase))
        {
            string entry;
            entry = GetEntries(addon).FirstOrDefault((string x) => x.ContainsAny(text));
            if (entry != null)
            {
                int index;
                index = GetEntries(addon).IndexOf(entry);
                if (index >= 0 && IsSelectItemEnabled(addon, index) && (Throttler?.Invoke() ?? GenericThrottle))
                {
                    ClickSelectString.Using((nint)addon).SelectItem((ushort)index);
                    Svc.Log.Debug($"TrySelectSpecificEntry: selecting {entry}/{index} as requested by {text.Print()}");
                    return true;
                }
            }
        }
        else
        {
            RethrottleGeneric();
        }
        return false;
    }

    internal unsafe static bool IsSelectItemEnabled(AddonSelectString* addon, int index)
    {
        AtkTextNode* step1;
        step1 = (AtkTextNode*)addon->AtkUnitBase.UldManager.NodeList[2]->GetComponent()->UldManager.NodeList[index + 1]->GetComponent()->UldManager.NodeList[3];
        return GenericHelpers.IsSelectItemEnabled(step1);
    }

    internal unsafe static List<string> GetEntries(AddonSelectString* addon)
    {
        List<string> list;
        list = new List<string>();
        for (int i = 0; i < addon->PopupMenu.PopupMenu.EntryCount; i++)
        {
            list.Add(MemoryHelper.ReadSeStringNullTerminated((nint)addon->PopupMenu.PopupMenu.EntryNames[i]).ExtractText());
        }
        return list;
    }

    internal static void RethrottleGeneric(int num)
    {
        EzThrottler.Throttle("AetherBoxGenericThrottle", num, rethrottle: true);
    }

    internal static void RethrottleGeneric()
    {
        EzThrottler.Throttle("AetherBoxGenericThrottle", 200, rethrottle: true);
    }

    internal unsafe static bool IsLoading()
    {
        if (!GenericHelpers.TryGetAddonByName<AtkUnitBase>("FadeBack", out var fb) || !fb->IsVisible)
        {
            if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("FadeMiddle", out var fm))
            {
                return fm->IsVisible;
            }
            return false;
        }
        return true;
    }

    public bool IsInDuty()
    {
        if (!Svc.Condition[ConditionFlag.BoundByDuty] && !Svc.Condition[ConditionFlag.BoundByDuty56] && !Svc.Condition[ConditionFlag.BoundByDuty95])
        {
            return Svc.Condition[ConditionFlag.BoundToDuty97];
        }
        return true;
    }
}
