using Dalamud.Bindings.ImGui;
using Dalamud.Game.Text;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using Lumina.Excel.Sheets;
using Questionable.Model.Common;
using Questionable.Model.Questing;
using Questionable.Windows.Common.Ui;
namespace Questionable.Windows.JournalComponents;

internal sealed class QuestRewardComponent
(
    QuestRegistry questRegistry,
    QuestData questData,
    QuestTooltipComponent questTooltipComponent,
    QuestFunctions questFunctions,
    QuestJournalUtils questJournalUtils,
    UiUtils uiUtils)
{
    private bool _showEventRewards;

    public void DrawItemRewards()
    {
        using ImRaii.TabItemDisposable tab = ImRaii.TabItem(_L("物品奖励"));
        if (!tab)
            return;

        ImGui.Checkbox(_L("显示季节活动任务奖励"), ref _showEventRewards);
        ImGui.Spacing();

        ImGui.BulletText(
            _L("仅列出不可交易物品（例如飞艇模型可在市场交易，因此不会列出）。"));

        DrawGroup(_L("坐骑"), EItemRewardType.Mount);
        DrawGroup(_L("宠物"), EItemRewardType.Minion);
        DrawGroup(_L("管弦乐琴乐谱"), EItemRewardType.OrchestrionRoll);
        DrawGroup(_L("幻卡"), EItemRewardType.TripleTriadCard);
        DrawGroup(_L("时尚配饰"), EItemRewardType.FashionAccessory);
        DrawGroup(_L("副本"), EItemRewardType.Duty);
    }

    private void DrawGroup(string label, EItemRewardType type)
    {
        if (!ImGui.CollapsingHeader($"{label}###Reward{type}"))
            return;

        if (type is EItemRewardType.Duty)
        {
            var resultsDuties = questRegistry.AllQuests
                    .Where(x => x.Id is QuestId &&
                           ((QuestInfo)x.Info).CfcUnlock != null &&
                           !questFunctions.IsQuestUnobtainable(x.Id))
                    .OrderBy(x => x.Id.Value).ToList();
            if (resultsDuties.Count == 0)
                ImGui.Text(_L("No results"));
            foreach (QuestInfo q in resultsDuties.Select(x => (QuestInfo)x.Info))
            {
                ContentFinderCondition cfc = q.CfcUnlock!.Value;
                if (cfc.Name.ByteLength == 0)
                    continue;
                string name = $"{cfc.Name.ToDalamudString()} ({cfc.RowId})";
                bool complete = questFunctions.IsQuestComplete(q.QuestId);
                Vector4 color = !questRegistry.IsKnownQuest(q.QuestId)
                    ? QstTheme.TextMuted
                    : complete
                        ? QstTheme.Success
                        : QstTheme.Danger;
                FontAwesomeIcon icon = complete ? FontAwesomeIcon.Check : FontAwesomeIcon.Times;
                if (uiUtils.ChecklistItem(name, color, icon, iconOverride: questJournalUtils.GetIconOverride(q, icon)))
                {
                    using ImRaii.TooltipDisposable tooltip = ImRaii.Tooltip();
                    ImGui.Text(_LF("Obtained from: {0}", q.Name));
                    using (ImRaii.PushIndent())
                    {
                        questTooltipComponent.DrawInner(q, showItemRewards: false);
                    }
                }
                questRegistry.TryGetQuest(q.QuestId, out Domain.Quest? quest);
                questJournalUtils.ShowContextMenu(q, quest, nameof(QuestRewardComponent));
            }
            return;
        }

        var results = questData.RedeemableItems.Where(x => x.Type == type)
            .OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
        if (results.Count == 0)
            ImGui.Text(_L("No results"));

        foreach (ItemReward item in results)
        {
            if (questData.TryGetQuestInfo(item.ElementId, out IQuestInfo? questInfo))
            {
                bool isEventQuest = questInfo is QuestInfo { IsSeasonalEvent: true };
                if (!_showEventRewards && isEventQuest)
                    continue;

                string name = item.Name;
                if (isEventQuest)
                    name += $" {SeIconChar.Clock.ToIconString()}";

                bool complete = item.IsUnlocked();
                Vector4 color = !questRegistry.IsKnownQuest(item.ElementId)
                    ? QstTheme.TextMuted
                    : complete
                        ? QstTheme.Success
                        : QstTheme.Danger;
                FontAwesomeIcon icon = complete ? FontAwesomeIcon.Check : FontAwesomeIcon.Times;
                if (uiUtils.ChecklistItem(name, color, icon, iconOverride: questJournalUtils.GetIconOverride((QuestInfo)questInfo, icon)))
                {
                    using ImRaii.TooltipDisposable tooltip = ImRaii.Tooltip();
                    if (item.Type is not EItemRewardType.TripleTriadCard)
                        ImGui.Text(_LF("Obtained from: {0}", questInfo.Name));
                    using (ImRaii.PushIndent())
                    {
                        questTooltipComponent.DrawInner(questInfo, showItemRewards: false);
                    }
                }
                questRegistry.TryGetQuest(questInfo.QuestId, out Domain.Quest? quest);
                questJournalUtils.ShowContextMenu(questInfo, quest, nameof(QuestRewardComponent));
            }
        }
    }
}
