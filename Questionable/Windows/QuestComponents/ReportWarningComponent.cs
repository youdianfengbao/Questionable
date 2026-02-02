using System;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Questionable.Controller;

namespace Questionable.Windows.QuestComponents;

internal sealed class ReportWarningComponent(Configuration configuration)
{
    private readonly Configuration _configuration = configuration;

    public void Draw()
    {
        DrawReportWarning();
    }

    private void DrawReportWarning()
    {
        ImGui.TextColored(ImGuiColors.DPSRed, "预告信息");  
        ImGui.TextWrapped("从版本 xxxx 开始，QST 新增了一项功能：你可以点击任务进度按钮旁边的 " +  
                          "“!” 按钮来反馈当前任务遇到的问题。" +  
                          "特此告知：如果你选择使用这个新功能并提交 " +  
                          "Bug 报告，QST 将会自动抓取并上传以下信息：");  
        ImGui.BulletText("所有已启用插件及其版本号的列表");  
        ImGui.BulletText("QST 最近执行的 10 个操作");  
        ImGui.BulletText("点击按钮时你当前进行的 任务/序列/步骤");  
        ImGui.BulletText("你的优先任务列表");  
        ImGui.BulletText("你在设置里填写的简短留言（如有设置）");  
        ImGui.TextWrapped("除非你主动点击下方高亮的红色 “!” 按钮，否则此功能绝不会向 Bug 报告服务器发送任何信息。" +  
                          "如果你完全不想看到这个按钮，请点击下方橙色的 “拒绝加入” 按钮。" +  
                          "否则，请点击绿色的 “知道了” 按钮来隐藏此警告。");  
        if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.ExclamationTriangle, "拒绝加入", ImGuiColors.DalamudOrange))  
        {  
            _configuration.General.DismissedReportWarning = true;  
            _configuration.General.ReportsDisabled = true;  
        }  
        ImGui.SameLine();  
        if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.ExclamationTriangle, "知道了", ImGuiColors.ParsedGreen))  
        {  
            _configuration.General.DismissedReportWarning = true;  
        }
    }
}
