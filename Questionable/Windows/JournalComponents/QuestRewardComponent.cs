using System;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Text;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Questionable.Controller;
using Questionable.Data;
using Questionable.Model;
using Questionable.Windows.QuestComponents;
namespace Questionable.Windows.JournalComponents;

internal sealed class QuestRewardComponent
(
    QuestRegistry questRegistry,
    QuestData questData,
    QuestTooltipComponent questTooltipComponent,
    UiUtils uiUtils)
{
    private readonly QuestData _questData = questData;
    private readonly QuestRegistry _questRegistry = questRegistry;
    private readonly QuestTooltipComponent _questTooltipComponent = questTooltipComponent;
    private readonly UiUtils _uiUtils = uiUtils;

    private bool _showEventRewards;

    public void DrawItemRewards()
    {
        using ImRaii.TabItemDisposable tab = ImRaii.TabItem("物品奖励");
        if (!tab)
            return;

        ImGui.Checkbox("显示季节活动任务奖励", ref _showEventRewards);
        ImGui.Spacing();

        ImGui.BulletText(
            "仅列出不可交易物品（例如飞艇模型可在市场交易，因此不会列出）。");

        DrawGroup("坐骑", EItemRewardType.Mount);
        DrawGroup("宠物", EItemRewardType.Minion);
        DrawGroup("Orchestrion Rolls", EItemRewardType.OrchestrionRoll);
        DrawGroup("Triple Triad Cards", EItemRewardType.TripleTriadCard);
        DrawGroup("Fashion Accessories", EItemRewardType.FashionAccessory);
    }

    private void DrawGroup(string label, EItemRewardType type)
    {
        if (!ImGui.CollapsingHeader($"{label}###Reward{type}"))
            return;

        foreach (ItemReward item in _questData.RedeemableItems.Where(x => x.Type == type)
            .OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase))
        {
            if (_questData.TryGetQuestInfo(item.ElementId, out IQuestInfo? questInfo))
            {
                bool isEventQuest = questInfo is QuestInfo { IsSeasonalEvent: true };
                if (!_showEventRewards && isEventQuest)
                    continue;

                string name = item.Name;
                if (isEventQuest)
                    name += $" {SeIconChar.Clock.ToIconString()}";

                bool complete = item.IsUnlocked();
                Vector4 color = !_questRegistry.IsKnownQuest(item.ElementId)
                    ? ImGuiColors.DalamudGrey
                    : complete
                        ? ImGuiColors.ParsedGreen
                        : ImGuiColors.DalamudRed;
                FontAwesomeIcon icon = complete ? FontAwesomeIcon.Check : FontAwesomeIcon.Times;
                if (_uiUtils.ChecklistItem(name, color, icon))
                {
                    using ImRaii.TooltipDisposable tooltip = ImRaii.Tooltip();
                    ImGui.Text($"Obtained from: {questInfo.Name}");
                    using (ImRaii.PushIndent())
                    {
                        _questTooltipComponent.DrawInner(questInfo, false);
                    }
                }
            }
        }
    }
}
