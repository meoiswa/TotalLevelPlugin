using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Command;
using Dalamud.Game.Gui.NamePlate;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TotalLevelPlugin;

public sealed class TotalLevelPlugin : IDalamudPlugin
{
    public string Name => "Total Level";

    private const string commandName = "/totallevel";

    private readonly Configuration configuration;
    private readonly SocialDetailBHandler socialDetailBHandler;

    public IDalamudPluginInterface PluginInterface { get; init; }
    public ICommandManager CommandManager { get; init; }
    public WindowSystem WindowSystem { get; init; }
    public TotalLevelUI Window { get; init; }

    private readonly Dictionary<ulong, List<(string, uint)>> playerSearchCache = new();

    public TotalLevelPlugin(
        IDalamudPluginInterface pluginInterface,
        ICommandManager commandManager)
    {
        pluginInterface.Create<Service>();

        PluginInterface = pluginInterface;
        CommandManager = commandManager;

        WindowSystem = new("TotalLevelPlugin");

        configuration = LoadConfiguration();
        configuration.Initialize(SaveConfiguration);

        Window = new TotalLevelUI(configuration)
        {
            IsOpen = configuration.IsVisible
        };

        // instantiate the SocialDetailB handler
        socialDetailBHandler = new SocialDetailBHandler(configuration);

        WindowSystem.AddWindow(Window);

        CommandManager.AddHandler(commandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "opens the configuration window"
        });

        PluginInterface.UiBuilder.Draw += DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
        PluginInterface.UiBuilder.OpenMainUi += DrawConfigUI;
    }

    public void Dispose()
    {
        socialDetailBHandler.Dispose();

        PluginInterface.UiBuilder.Draw -= DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;
        PluginInterface.UiBuilder.OpenMainUi -= DrawConfigUI;

        CommandManager.RemoveHandler(commandName);

        WindowSystem.RemoveAllWindows();
    }

    private Configuration LoadConfiguration()
    {
        JObject? baseConfig = null;
        if (File.Exists(PluginInterface.ConfigFile.FullName))
        {
            var configJson = File.ReadAllText(PluginInterface.ConfigFile.FullName);
            baseConfig = JObject.Parse(configJson);
        }

        if (baseConfig != null)
        {
            return baseConfig.ToObject<Configuration>() ?? new Configuration();
        }

        return new Configuration();
    }

    public void SaveConfiguration()
    {
        var configJson = JsonConvert.SerializeObject(this.configuration, Formatting.Indented);
        File.WriteAllText(PluginInterface.ConfigFile.FullName, configJson);
    }

    private void SetVisible(bool isVisible)
    {
        configuration.IsVisible = isVisible;
        configuration.Save();

        Window.IsOpen = configuration.IsVisible;
    }

    private void OnCommand(string command, string args)
    {
        SetVisible(!configuration.IsVisible);
    }

    private void DrawUI()
    {
        WindowSystem.Draw();
    }

    private void DrawConfigUI()
    {
        SetVisible(!configuration.IsVisible);
    }
}
