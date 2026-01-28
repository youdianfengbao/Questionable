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
        ImGui.TextColored(ImGuiColors.DPSRed, "未来提示信息");  
        ImGui.TextWrapped("从 xxxx 版本开始，QST 新增了一个功能：你可以点击任务进度按钮旁边的“!”按钮，" +  
                          "来反馈当前任务的问题。" +  
                          "这条信息是为了告知你：如果你选择使用这个新功能并提交 Bug 报告，" +  
                          "QST 将会自动收集并上传以下信息：");  
        ImGui.BulletText("所有已启用插件的列表及其版本号");  
        ImGui.BulletText("QST 最近执行的十个操作");  
        ImGui.BulletText("你在点击按钮时所处的任务 / 阶段 / 步骤");  
        ImGui.BulletText("你的优先任务列表");  
        ImGui.BulletText("一条你在设置中的填写的说明（如果填写了的话）");  
        ImGui.TextWrapped("除非你主动点击下方红色高亮的“!”按钮，" +  
                          "否则此功能绝不会向 Bug 报告服务上传任何信息。" +  
                          "如果你以后不希望看到这个按钮，可以点击下方的橙色“退出”按钮。" +  
                          "否则，点击绿色的“忽略”按钮即可暂时隐藏此提示。");  
        if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.ExclamationTriangle, "退出", ImGuiColors.DalamudOrange))  
        {  
            _configuration.General.DismissedReportWarning = true;  
            _configuration.General.ReportsDisabled = true;  
        }  
        ImGui.SameLine();  
        if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.ExclamationTriangle, "忽略", ImGuiColors.ParsedGreen))  
        {  
            _configuration.General.DismissedReportWarning = true;  
        }
    }
}
