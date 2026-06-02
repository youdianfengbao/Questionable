using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ECommons.ExcelServices;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Questionable.Controller;
using Questionable.Data;
using Questionable.Functions;
using Questionable.Model;
using Questionable.Model.Questing;
using Questionable.Windows.Common;
using Questionable.Windows.QuestComponents;
using Questionable.Windows.Utils;
namespace Questionable.Windows;

internal sealed class PriorityWindow : LWindow
{
    private const string ClipboardPrefix = "qst:priority:";
    private const string LegacyClipboardPrefix = "qst:v1:";
    private const char ClipboardSeparator = ';';
    private const string JobQuestsPresetName = "职业/特职任务";
    private readonly IChatGui _chatGui;

    private readonly Configuration _configuration;
    private readonly IDalamudPluginInterface _pluginInterface;

    private readonly QuestController _questController;
    private readonly QuestData _questData;
    private readonly QuestFunctions _questFunctions;
    private readonly QuestRegistry _questRegistry;
    private readonly QuestSelector _questSelector;
    private readonly QuestTooltipComponent _questTooltipComponent;
    private readonly UiUtils _uiUtils;
    private Dictionary<string, List<ElementId>>? _builtInPresets;
    private ElementId? _draggedItem;
    private Job? _lastKnownJob;
    private string _presetName = string.Empty;
    private string? _selectedPresetName;

    public PriorityWindow(QuestController questController, QuestFunctions questFunctions, QuestSelector questSelector,
        QuestTooltipComponent questTooltipComponent, UiUtils uiUtils, IChatGui chatGui, QuestRegistry questRegistry,
        IDalamudPluginInterface pluginInterface, Configuration configuration, QuestData questData)
        : base("任务优先级###QuestionableQuestPriority")
    {
        _questController = questController;
        _questFunctions = questFunctions;
        _questSelector = questSelector;
        _questTooltipComponent = questTooltipComponent;
        _uiUtils = uiUtils;
        _chatGui = chatGui;
        _questRegistry = questRegistry;
        _pluginInterface = pluginInterface;
        _configuration = configuration;
        _questData = questData;

        _questSelector.SuggestionPredicate = quest =>
            !quest.Info.IsMainScenarioQuest &&
            !questFunctions.IsQuestUnobtainable(quest.Id) &&
            questController.PriorityManager.Quests.All(x => x.Id != quest.Id);
        _questSelector.DefaultPredicate = quest => questFunctions.IsQuestAccepted(quest.Id);
        _questSelector.QuestSelected = quest => _questController.PriorityManager.Add(quest);

        Size = new Vector2(400, 400);
        SizeCondition = ImGuiCond.Once;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new(400, 30),
            MaximumSize = default
        };
    }

    public override unsafe void DrawContent()
    {
        Job currentJob = (Job)(PlayerState.Instance()->CurrentClassJobId);
        if (_lastKnownJob != null && currentJob != _lastKnownJob && _selectedPresetName == JobQuestsPresetName)
            LoadPreset(JobQuestsPresetName);
        _lastKnownJob = currentJob;

        if (ImGui.CollapsingHeader("说明"))
        {
            ImGui.TextWrapped(
                "Questionable 通常会按以下顺序尝试执行：");
            ImGui.BulletText("下面手动添加的优先任务（按顺序）");
            ImGui.BulletText(""优先"任务：职业任务、2.0 极神、水晶塔任务");
            ImGui.BulletText(
                "待办列表中已支持的任务\n（任务日志中始终显示在屏幕上的任务）");
            ImGui.BulletText("最近的可接任务\n" +
                              "（Example: Last quest accepted is 'Y'shtola's Special Mission', try to accept other quests" +
                              " before completing that one）");
            ImGui.BulletText("未完成的可接任务");
            ImGui.BulletText("剩余的支线任务（包括季节活动和绝枪/舞者/镰刀起始任务）");
            ImGui.BulletText("未完成的季节任务");
        }

        using (ImRaii.Disabled(_questController.IsRunning))
        {
            _questSelector.DrawSelection("quests");
        }

        ImGui.Separator();

        DrawPresetSelector();
        DrawPriorityQuestList();

        ImGui.Separator();

        DrawImportExportButtons();
    }

    private void DrawPresetSelector()
    {
        _builtInPresets ??= _questController.PriorityManager.BuiltInPresets;
        using (ImRaii.Disabled(_questController.IsRunning))
        {
            string[] presetNames = _configuration.Priority.Presets.Keys
                .Prepend("(none)")
                .Concat(
                    _builtInPresets.Keys.Where(x => !_configuration.Priority.Presets.ContainsKey(x))
                        .Select(x => $"(built-in) {x}"))
                .ToArray();

            int selectedIndex;
            if (_selectedPresetName != null)
            {
                if (!_configuration.Priority.Presets.ContainsKey(_selectedPresetName)
                    && !_builtInPresets.ContainsKey(_selectedPresetName))
                    _selectedPresetName = null;

                selectedIndex = Array.IndexOf(presetNames, _selectedPresetName);
                if (selectedIndex == -1)
                {
                    // built-in presetname without prefix
                    selectedIndex = Array.IndexOf(presetNames, $"(built-in) {_selectedPresetName}");
                }
            }
            else
                selectedIndex = 0;

            if (ImGui.Combo("预设", ref selectedIndex, presetNames, presetNames.Length))
            {
                string presetName = presetNames[selectedIndex];
                if (presetName.StartsWith("(built-in) "))
                {
                    presetName = presetName["(built-in) ".Length..];
                    LoadPreset(presetName);
                }
                else if (presetName == "(none)")
                {
                    _selectedPresetName = null;
                }
                else
                {
                    LoadPreset(presetName);
                }
            }

            ImGui.SameLine();
            using (ImRaii.Disabled(selectedIndex == 0))
            {
                if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Trash, "删除"))
                {
                    string presetName = presetNames[selectedIndex];
                    if (presetName.StartsWith("(built-in) "))
                        presetName = presetName["(built-in) ".Length..];
                    _configuration.Priority.Presets.Remove(presetName);
                    if (_selectedPresetName == presetName)
                        _selectedPresetName = null;
                    Save();
                }
            }
        }

        ImGui.SameLine();
        ImGui.SetNextItemWidth(100);
        ImGui.InputTextWithHint("##PresetName", "预设名称", ref _presetName, 64);
        ImGui.SameLine();
        using (ImRaii.Disabled(string.IsNullOrWhiteSpace(_presetName) ||
                               _configuration.Priority.Presets.ContainsKey(_presetName)))
        {
            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Save, "保存预设"))
            {
                _configuration.Priority.Presets[_presetName] =
                    _questController.PriorityManager.Quests.Select(x => x.Id).ToList();
                _selectedPresetName = _presetName;
                _presetName = string.Empty;
                Save();
            }
        }
    }

    private void LoadPreset(string presetName)
    {
        if (_configuration.Priority.Presets.TryGetValue(presetName, out List<string>? questIds))
        {
            _questController.ImportQuestPriority(
                questIds.Select(ElementId.FromString).ToList());
            _selectedPresetName = presetName;
        }
        else if (_builtInPresets?.TryGetValue(presetName, out List<ElementId>? elementIds) == true)
        {
            _questController.ImportQuestPriority(elementIds);
            _selectedPresetName = presetName;
        }
    }

    private void DrawPriorityQuestList()
    {
        List<Quest> quests = _questController.PriorityManager.Quests;
        using (ImRaii.Disabled(_questController.IsRunning))
        {
            if (quests.Count > 0)
            {
                if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Copy, "复制全部"))
                    CopyAllToClipboard();

                ImGui.SameLine();
                if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Paste, "粘贴全部"))
                    PasteAllFromClipboard();
            }

            if (ImGui.BeginChild("PriorityQuestList", default, false,
                ImGuiWindowFlags.HorizontalScrollbar))
            {
                for (int i = 0; i < quests.Count; i++)
                {
                    Quest quest = quests[i];
                    using ImRaii.Id id = ImRaii.PushId(i);

                    Vector2 startPos = ImGui.GetCursorPos();

                    // Drag source
                    using (ImRaii.Disabled(i == 0))
                    {
                        if (ImGuiComponents.IconButton($"##Up{i}", FontAwesomeIcon.ArrowUp) && i > 0)
                            _questController.PriorityManager.MoveUp(quest.Id);
                    }

                    // Drag/drop
                    if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                    {
                        HandleDragDrop(i, quest);
                    }

                    ImGui.SameLine();
                    using (ImRaii.Disabled(i == quests.Count - 1))
                    {
                        if (ImGuiComponents.IconButton($"##Down{i}", FontAwesomeIcon.ArrowDown) && i < quests.Count - 1)
                            _questController.PriorityManager.MoveDown(quest.Id);
                    }

                    ImGui.SameLine();
                    ImGui.AlignTextToFramePadding();
                    (Vector4 color, FontAwesomeIcon icon, string status) = _uiUtils.GetQuestStyle(quest.Id);
                    using (_pluginInterface.UiBuilder.IconFontFixedWidthHandle.Push())
                    {
                        ImGui.TextColored(color, icon.ToIconString());
                    }

                    ImGui.SameLine();
                    ImGui.AlignTextToFramePadding();
                    ImGui.TextWrapped($"{i + 1}. {quest.Info.Name}");
                    bool hovered = ImGui.IsItemHovered();
                    if (hovered)
                        _questTooltipComponent.Draw(quest.Info);

                    ImGui.SameLine();
                    using (_pluginInterface.UiBuilder.IconFontFixedWidthHandle.Push())
                    {
                        ImGui.SetCursorPosX(ImGui.GetContentRegionAvail().X - 20);
                        if (ImGuiComponents.IconButton($"##Remove{i}", FontAwesomeIcon.Times))
                            _questController.PriorityManager.Remove(quest.Id);
                    }
                }
                ImGui.EndChild();
            }
        }
    }

    private void HandleDragDrop(int i, Quest quest)
    {
        if (ImGui.BeginDragDropSource())
        {
            _draggedItem = quest.Id;
            unsafe
            {
                ImGui.SetDragDropPayload("PRIORITY_QUEST", (void*)0, IntPtr.Zero);
            }

            ImGui.Text($"{i + 1}. {quest.Info.Name}");
            ImGui.EndDragDropSource();
        }

        if (ImGui.BeginDragDropTarget())
        {
            if (_draggedItem != null)
            {
                var payload = ImGui.AcceptDragDropPayload("PRIORITY_QUEST");
                if (payload.NativePtr != null)
                {
                    _questController.PriorityManager.Reorder(_draggedItem.Value, i);
                }
            }

            ImGui.EndDragDropTarget();
        }
    }

    private void DrawImportExportButtons()
    {
        ImGui.Separator();
        if (ImGui.Button("复制当前优先任务列表到剪贴板"))
        {
            List<Quest> quests = _questController.PriorityManager.Quests;
            if (quests.Count > 0)
            {
                string data = string.Join(ClipboardSeparator.ToString(), quests.Select(x => x.Id.ToString()));
                byte[] compressed = Compress(Encoding.UTF8.GetBytes(data));
                string clipboard = ClipboardPrefix + Convert.ToBase64String(compressed);
                ImGui.SetClipboardText(clipboard);
                _chatGui.Print("优先任务列表已复制到剪贴板。", CommandHandler.MessageTag,
                    CommandHandler.TagColor);
            }
        }

        ImGui.SameLine();
        if (ImGui.Button("从剪贴板导入优先任务列表"))
        {
            string? clipboard = ImGui.GetClipboardText();
            if (!string.IsNullOrEmpty(clipboard))
            {
                if (clipboard.StartsWith(ClipboardPrefix) || clipboard.StartsWith(LegacyClipboardPrefix))
                {
                    string prefix = clipboard.StartsWith(LegacyClipboardPrefix)
                        ? LegacyClipboardPrefix
                        : ClipboardPrefix;
                    string encoded = clipboard[prefix.Length..];
                    try
                    {
                        byte[] compressed = Convert.FromBase64String(encoded);
                        byte[] decompressed = Decompress(compressed);
                        string data = Encoding.UTF8.GetString(decompressed);
                        List<ElementId> elementIds =
                            data.Split(ClipboardSeparator).Select(ElementId.FromString).ToList();

                        _questController.ImportQuestPriority(elementIds);
                        _chatGui.Print("优先任务列表已从剪贴板导入。", CommandHandler.MessageTag,
                            CommandHandler.TagColor);
                    }
                    catch (Exception e)
                    {
                        _chatGui.PrintError(
                            $"无法解析剪贴板中的优先任务列表: {e.Message}", CommandHandler.MessageTag, CommandHandler.TagColor);
                    }
                }
                else
                {
                    _chatGui.PrintError("剪贴板内容无效。", CommandHandler.MessageTag, CommandHandler.TagColor);
                }
            }
            else
            {
                _chatGui.PrintError("剪贴板内容为空。", CommandHandler.MessageTag, CommandHandler.TagColor);
            }
        }
    }

    private static byte[] Compress(byte[] data)
    {
        using var output = new System.IO.MemoryStream();
        using (var compressor = new System.IO.Compression.DeflateStream(output, System.IO.Compression.CompressionLevel.Optimal))
        {
            compressor.Write(data, 0, data.Length);
        }
        return output.ToArray();
    }

    private static byte[] Decompress(byte[] data)
    {
        using var input = new System.IO.MemoryStream(data);
        using var decompressor = new System.IO.Compression.DeflateStream(input, System.IO.Compression.CompressionMode.Decompress);
        using var output = new System.IO.MemoryStream();
        decompressor.CopyTo(output);
        return output.ToArray();
    }

    private void CopyAllToClipboard()
    {
        List<Quest> quests = _questController.PriorityManager.Quests;
        if (quests.Count > 0)
        {
            string data = string.Join(ClipboardSeparator.ToString(), quests.Select(x => x.Id.ToString()));
            byte[] compressed = Compress(Encoding.UTF8.GetBytes(data));
            string clipboard = ClipboardPrefix + Convert.ToBase64String(compressed);
            ImGui.SetClipboardText(clipboard);
            _chatGui.Print("优先任务列表已复制到剪贴板。", CommandHandler.MessageTag, CommandHandler.TagColor);
        }
    }

    private void PasteAllFromClipboard()
    {
        string? clipboard = ImGui.GetClipboardText();
        if (!string.IsNullOrEmpty(clipboard))
        {
            if (clipboard.StartsWith(ClipboardPrefix) || clipboard.StartsWith(LegacyClipboardPrefix))
            {
                string prefix = clipboard.StartsWith(LegacyClipboardPrefix)
                    ? LegacyClipboardPrefix
                    : ClipboardPrefix;
                string encoded = clipboard[prefix.Length..];
                try
                {
                    byte[] compressed = Convert.FromBase64String(encoded);
                    byte[] decompressed = Decompress(compressed);
                    string data = Encoding.UTF8.GetString(decompressed);
                    List<ElementId> elementIds =
                        data.Split(ClipboardSeparator).Select(ElementId.FromString).ToList();

                    _questController.ImportQuestPriority(elementIds);
                    _chatGui.Print("优先任务列表已从剪贴板导入。", CommandHandler.MessageTag, CommandHandler.TagColor);
                }
                catch (Exception e)
                {
                    _chatGui.PrintError(
                        $"无法解析剪贴板中的优先任务列表: {e.Message}", CommandHandler.MessageTag, CommandHandler.TagColor);
                }
            }
            else
            {
                _chatGui.PrintError("剪贴板内容无效。", CommandHandler.MessageTag, CommandHandler.TagColor);
            }
        }
        else
        {
            _chatGui.PrintError("剪贴板内容为空。", CommandHandler.MessageTag, CommandHandler.TagColor);
        }
    }

    private void Save() => _pluginInterface.SavePluginConfig(_configuration);
}
