using System.Collections.ObjectModel;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using ECommons.ImGuiMethods;
using Questionable.Model.Common;
using Questionable.Windows.Common.Ui;
namespace Questionable.Windows.ConfigComponents;

internal sealed class PluginConfigComponent
(
    IDalamudPluginInterface pluginInterface,
    Configuration configuration,
    CombatController combatController,
    UiUtils uiUtils,
    ICommandManager commandManager,
    AutomatonIpc automatonIpc,
    PandorasBoxIpc pandorasBoxIpc) : ConfigComponent(pluginInterface, configuration)
{
    private readonly IDalamudPluginInterface _pluginInterface = pluginInterface;
    private readonly Configuration _configuration = configuration;
    private readonly CombatController _combatController = combatController;
    private readonly UiUtils _uiUtils = uiUtils;
    private readonly ICommandManager _commandManager = commandManager;

    private static readonly ReadOnlyDictionary<ECombatModule, PluginInfo> CombatPlugins =
        new Dictionary<ECombatModule, PluginInfo>
        {
            {
                ECombatModule.BossMod,
                new("Boss Mod (VBM)",
                    "BossMod",
                    "Automates all kinds of combat and interaction in overworld and duty content",
                    new("https://github.com/awgil/ffxiv_bossmod"),
                    new("https://puni.sh/api/repository/veyn"),
                    "/vbm")
            },
            {
                ECombatModule.WrathCombo,
                new("Wrath Combo",
                    "WrathCombo",
                    string.Empty,
                    new("https://github.com/PunishXIV/WrathCombo"),
                    new("https://puni.sh/api/plugins"),
                    "/wrath")
            },
            {
                ECombatModule.RotationSolverReborn,
                new("Rotation Solver Reborn",
                    "RotationSolver",
                    string.Empty,
                    new("https://github.com/FFXIV-CombatReborn/RotationSolverReborn"),
                    new(
                        "https://raw.githubusercontent.com/FFXIV-CombatReborn/CombatRebornRepo/main/pluginmaster.json"),
                    "/rsr")
            },
            {
                ECombatModule.AEAssist,
                new("AEAssist",
                    "AEAssistV3",
                    "如果你希望使用 AEAssist，请自行寻找安装方式",
                    new("https://github.com/FFXIV-CombatReborn/AEAssist"),
                    null)
            }

        }.AsReadOnly();

    private static readonly IReadOnlyList<PluginInfo> RequiredPlugins =
    [
        new("vnavmesh",
            "vnavmesh",
            _L("vnavmesh 处理寻路、导航，负责将你的角色移动到下一个与任务相关的目的地。"),
            new("https://github.com/awgil/ffxiv_navmesh/"),
            new("https://puni.sh/api/repository/veyn"),
            "/vnav"),
        new("Lifestream",
            "Lifestream",
            _L("用于在城市小型以太之光间传送。"),
            new("https://github.com/NightmareXIV/Lifestream"),
            new("https://github.com/NightmareXIV/MyDalamudPlugins/raw/main/pluginmaster.json"),
            "/lifestream"),
        new("TextAdvance",
            "TextAdvance",
            _L("自动接受并交付任务，跳过过场动画和对话。"),
            new("https://github.com/NightmareXIV/TextAdvance"),
            new("https://github.com/NightmareXIV/MyDalamudPlugins/raw/main/pluginmaster.json"),
            "/at c"),
        //CombatPlugins[ECombatModule.BossMod]
    ];

    private readonly IReadOnlyList<PluginInfo> _recommendedPlugins =
    [
        new("Artisan",
            "Artisan",
            _L("全自动生产插件（自动制作）。"),
            new("https://github.com/PunishXIV/Artisan"),
            new("https://puni.sh/api/plugins"),
            "/artisan"),
        new("AutoDuty",
            "AutoDuty",
            _L("自动完成副本"),
            new("https://github.com/erdelf/AutoDuty"),
            new("https://puni.sh/api/repository/erdelf"),
            "/ad"),
        new("AutoHook",
            "AutoHook",
            _L("Automates fishing"),
            new("https://github.com/PunishXIV/AutoHook"),
            new("https://puni.sh/api/plugins"),
            "/autohook"),
        new("CBT (formerly known as Automaton)",
            "Automaton",
            _L("""
            Automaton 是一组自动化相关的功能合集。
            """),
            new("https://github.com/Jaksuhn/Automaton"),
            new("https://puni.sh/api/repository/croizat"),
            "/cbt",
            [
                new(_L("已启用 'Sniper no sniping'"),
                    _L("自动完成红莲版本加入的狙击小游戏任务"),
                    () => automatonIpc.IsAutoSnipeEnabled)
            ]),
        new("MogMail",
            "Mogmail",
            _L("Claim mailed items during QST operation"),
            new("https://github.com/Nexaii/Mogmail"),
            new("https://puni.sh/api/plugins/nexai"),
            "/mogmail"),
        new("NotificationMaster",
            "NotificationMaster",
            _L("Sends a configurable out-of-game notification if a quest requires manual actions."),
            new Uri("https://github.com/NightmareXIV/NotificationMaster"),
            new("https://github.com/NightmareXIV/MyDalamudPlugins/raw/main/pluginmaster.json"),
            "/pnotify"),
        new("Pandora's Box",
            "PandorasBox",
            _L("""
            Pandora's Box 是一组便捷功能合集。
            """),
            new("https://github.com/PunishXIV/PandorasBox"),
            new("https://puni.sh/api/plugins"),
            "/pandora",
            [
                new(_L("已启用 'Auto Active Time Maneuver'"),
                    _L("""
                    自动完成单人任务、副本和大型任务中的 Active Time Maneuver。
                    """),
                    () => pandorasBoxIpc.IsAutoActiveTimeManeuverEnabled)
            ]),
        new("Stylist",
            "Stylist",
            _L("""
            装备管理器
            """),
            new("https://github.com/NightmareXIV/Stylist"),
            new("https://github.com/NightmareXIV/MyDalamudPlugins/raw/main/pluginmaster.json"),
            "/stylist c"),
    ];

    public override void DrawTab()
    {
        using ImRaii.TabItemDisposable tab = ImRaii.TabItem(_L("插件依赖") + "###Plugins");
        if (!tab)
            return;

        Draw(out bool allRequiredInstalled);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (allRequiredInstalled)
            ImGui.TextColored(QstTheme.Success, _L("所有需要的插件都已安装"));
        else
        {
            ImGui.TextColored(QstTheme.Danger,
                _L("缺少必需的插件，Questionable 可能无法正常工作。"));
        }
    }

    public void Draw(out bool allRequiredInstalled)
    {
        float checklistPadding;
        using (_pluginInterface.UiBuilder.IconFontFixedWidthHandle.Push())
        {
            checklistPadding = ImGui.CalcTextSize(FontAwesomeIcon.Check.ToIconString()).X +
                               ImGui.GetStyle().ItemSpacing.X;
        }

        allRequiredInstalled = true;
        ImGui.SetNextItemOpen(isOpen: true, ImGuiCond.Once);
        if (QstWidgets.SectionHeader(_L("必需的插件:"), "RequiredPlugins", defaultOpen: false))
        {
            using (ImRaii.PushIndent())
            {
                foreach (PluginInfo plugin in RequiredPlugins)
                    allRequiredInstalled &= DrawPlugin(plugin, checklistPadding);
            }
        }

        if (QstWidgets.SectionHeader(_L("自动输出/循环插件:"), "RotationPlugins", defaultOpen: false))
        {
            using (ImRaii.Disabled(_combatController.IsRunning))
            {
                using (ImRaii.PushIndent())
                {
                    if (ImGui.RadioButton(_L("不使用自动输出/循环插件（战斗必须手动进行）"),
                        _configuration.General.CombatModule == ECombatModule.None))
                    {
                        _configuration.General.CombatModule = ECombatModule.None;
                        _pluginInterface.SavePluginConfig(_configuration);
                    }

                    allRequiredInstalled &= DrawCombatPlugin(ECombatModule.BossMod, checklistPadding);
                }

                ImGui.TextWrapped(_L("以下自动输出/循环插件仅用于兼容性和测试："));
                using (ImRaii.PushIndent())
                {
                    allRequiredInstalled &= DrawCombatPlugin(ECombatModule.WrathCombo, checklistPadding);
                    allRequiredInstalled &=
                        DrawCombatPlugin(ECombatModule.RotationSolverReborn, checklistPadding);
                    allRequiredInstalled &=
                        DrawCombatPlugin(ECombatModule.AEAssist, checklistPadding);
                }
            }
        }

        if (QstWidgets.SectionHeader(_L("推荐/小众插件:"), "NichePlugins", defaultOpen: false))
        {
            using (ImRaii.PushIndent())
            {
                foreach (PluginInfo plugin in _recommendedPlugins)
                    DrawPlugin(plugin, checklistPadding);
            }
        }
    }

    private void AddConfigClickable(IExposedPlugin? installedPlugin, PluginInfo plugin)
    {
        if (installedPlugin != null && plugin.ConfigCommand != null && plugin.ConfigCommand.StartsWith('/'))
        {
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(_L("打开设置"));
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }
            if (ImGui.IsItemClicked())
                _commandManager.ProcessCommand(plugin.ConfigCommand);

        }
    }

    private bool DrawPlugin(PluginInfo plugin, float checklistPadding)
    {
        using (ImRaii.PushId("plugin_" + plugin.DisplayName))
        {
            IExposedPlugin? installedPlugin = FindInstalledPlugin(plugin);
            bool isInstalled = installedPlugin != null;
            string label = plugin.DisplayName;
            if (installedPlugin != null)
                label += $" v{installedPlugin.Version}";

            ImGui.BeginGroup();
            if (installedPlugin != null && installedPlugin.InternalName.Equals("vnavmesh", StringComparison.Ordinal) && (installedPlugin.Manifest.Author.Contains("AtmoOmen")))
                plugin = new(
                    plugin.DisplayName,
                    plugin.InternalName,
                    plugin.Details,
                    new("https://github.com/AtmoOmen/ffxiv_navmesh-cn"),
                    new("https://gh.atmoomen.top/DalamudPlugins/main/pluginmaster.json"),
                    plugin.ConfigCommand
                );
            _uiUtils.ChecklistItem(label, isInstalled);

            DrawPluginDetails(plugin, checklistPadding, isInstalled);
            ImGui.EndGroup();
            AddConfigClickable(installedPlugin, plugin);
            return isInstalled;
        }
    }

    private bool DrawCombatPlugin(ECombatModule combatModule, float checklistPadding)
    {
        ImGui.Spacing();

        PluginInfo plugin = CombatPlugins[combatModule];
        using (ImRaii.PushId("plugin_" + plugin.DisplayName))
        {
            IExposedPlugin? installedPlugin = FindInstalledPlugin(plugin);
            bool isInstalled = installedPlugin != null;
            string label = plugin.DisplayName;
            if (installedPlugin != null)
                label += $" v{installedPlugin.Version}";

            if (ImGui.RadioButton(label, _configuration.General.CombatModule == combatModule))
            {
                _configuration.General.CombatModule = combatModule;
                _pluginInterface.SavePluginConfig(_configuration);
            }

            ImGui.SameLine(0);
            using (_pluginInterface.UiBuilder.IconFontFixedWidthHandle.Push())
            {
                Vector4 iconColor = isInstalled ? QstTheme.Success : QstTheme.Danger;
                FontAwesomeIcon icon = isInstalled ? FontAwesomeIcon.Check : FontAwesomeIcon.Times;

                ImGui.AlignTextToFramePadding();
                ImGui.TextColored(iconColor, icon.ToIconString());
            }
            AddConfigClickable(installedPlugin, plugin);

            DrawPluginDetails(plugin, checklistPadding, isInstalled, blurb: false);
            return isInstalled || _configuration.General.CombatModule != combatModule;
        }
    }

    private void DrawPluginDetails(PluginInfo plugin, float checklistPadding, bool isInstalled, bool blurb = true)
    {
        using (ImRaii.PushIndent(checklistPadding))
        {
            if (!string.IsNullOrEmpty(plugin.Details) && blurb)
                ImGui.TextWrapped(plugin.Details);

            bool allDetailsOk = true;
            if (plugin.DetailsToCheck != null)
            {
                foreach (PluginDetailInfo detail in plugin.DetailsToCheck)
                {
                    bool detailOk = detail.Predicate();
                    allDetailsOk &= detailOk;

                    _uiUtils.ChecklistItem(detail.DisplayName, isInstalled && detailOk);
                    if (!string.IsNullOrEmpty(detail.Details))
                    {
                        using (ImRaii.PushIndent(checklistPadding))
                        {
                            ImGui.TextWrapped(detail.Details);
                        }
                    }
                }
            }

            ImGui.Spacing();

            if (isInstalled)
            {
                if (!allDetailsOk && plugin.ConfigCommand != null && plugin.ConfigCommand.StartsWith('/'))
                {
                    ImRaii.ColorDisposable? color = null;
                    if (!allDetailsOk)
                        color = ImRaii.PushColor(ImGuiCol.Text, QstTheme.Accent);
                    if (ImGuiComponentsLocal.IconButton(FontAwesomeIcon.Cog))
                        _commandManager.ProcessCommand(plugin.ConfigCommand);
                    color?.Dispose();
                }
            }
            else
            {
                if (ImGuiComponentsLocal.IconButtonWithText(FontAwesomeIcon.Globe, _L("打开网站")))
                    Util.OpenLink(plugin.WebsiteUri.ToString());

                ImGui.SameLine();
                if (plugin.DalamudRepositoryUri != null)
                {
                    if (ImGuiComponentsLocal.IconButtonWithText(FontAwesomeIcon.Code, _L("打开仓库")))
                        Util.OpenLink(plugin.DalamudRepositoryUri.ToString());
                }
                else
                {
                    ImGui.AlignTextToFramePadding();
                    ImGuiComponents.HelpMarker(_L("可在官方 Dalamud 插件库中找到"));
                }
            }
        }
    }

    private static bool PluginImageButton(PluginInfo plugin, float size, bool isInstalled, bool isActive)
    {
        string url = $"https://qstxiv.github.io/icons/{plugin.InternalName}.png";
        if (ThreadLoadImageHandler.TryGetTextureWrap(url, out IDalamudTextureWrap? logo))
        {
            return ImGui.ImageButton(
                logo.Handle,
                new(size.Scale(), size.Scale()),
                2,
                isInstalled ? QstTheme.Success : QstTheme.Danger,
                isActive ? Vector4.One : new(0.5f, 0.5f, 0.5f, 1f)
            );
        }

        return false;
    }

    private IExposedPlugin? FindInstalledPlugin(PluginInfo pluginInfo)
    {
        return _pluginInterface.InstalledPlugins.FirstOrDefault(x =>
            x.InternalName == pluginInfo.InternalName && x.IsLoaded);
    }

    private sealed record PluginInfo
    (
        string DisplayName,
        string InternalName,
        string Details,
        Uri WebsiteUri,
        Uri? DalamudRepositoryUri,
        string? ConfigCommand = null,
        List<PluginDetailInfo>? DetailsToCheck = null);

    private sealed record PluginDetailInfo(string DisplayName, string Details, Func<bool> Predicate);
}
