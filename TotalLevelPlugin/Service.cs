using Dalamud.IoC;
using Dalamud.Plugin.Services;

namespace TotalLevelPlugin;

public class Service
{
#pragma warning disable CS8618
  [PluginService] public static IFramework Framework { get; private set; }
  [PluginService] public static IPluginLog PluginLog { get; private set; }
  [PluginService] public static IGameGui GameGui { get; private set; }
  [PluginService] public static IClientState ClientState { get; private set; }
  [PluginService] public static IDataManager DataManager { get; private set; }
  [PluginService] public static INamePlateGui NamePlateGui { get; private set; }
  [PluginService] public static IPlayerState PlayerState { get; private set; }
  [PluginService] public static IAddonLifecycle AddonLifecycle { get; private set; }
#pragma warning restore CS8618
}
