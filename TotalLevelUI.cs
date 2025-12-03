using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using System;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.UI.Info;

namespace TotalLevelPlugin;

public class TotalLevelUI : Window, IDisposable
{
  private readonly Configuration configuration;

  public TotalLevelUI(Configuration configuration)
  : base(
    "TotalLevel##ConfigWindow",
    ImGuiWindowFlags.AlwaysAutoResize
    | ImGuiWindowFlags.NoResize
    | ImGuiWindowFlags.NoCollapse
  )
  {
    this.configuration = configuration;

    SizeConstraints = new WindowSizeConstraints()
    {
      MinimumSize = new Vector2(468, 0),
      MaximumSize = new Vector2(468, 1000)
    };
  }

  public void Dispose()
  {
    GC.SuppressFinalize(this);
  }

  public override void OnClose()
  {
    base.OnClose();
    configuration.IsVisible = false;
    configuration.Save();
  }

  private void DrawSectionMasterEnable()
  {
    // can't ref a property, so use a local copy
    var enabled = configuration.MasterEnable;
    if (ImGui.Checkbox("Enable", ref enabled))
    {
      configuration.MasterEnable = enabled;
      configuration.Save();
    }

    if (configuration.MasterEnable)
    {
      ImGui.TextWrapped("Display Mode:");

      var displayMode = (int)configuration.DisplayMode;

      if (ImGui.RadioButton("Total Level (Combat + Crafting)", ref displayMode, (int)LevelDisplayMode.TotalLevel))
      {
        configuration.DisplayMode = LevelDisplayMode.TotalLevel;
        configuration.Save();
      }

      if (ImGui.RadioButton("Combat Level Only", ref displayMode, (int)LevelDisplayMode.CombatLevel))
      {
        configuration.DisplayMode = LevelDisplayMode.CombatLevel;
        configuration.Save();
      }

      if (ImGui.RadioButton("Crafting Level Only", ref displayMode, (int)LevelDisplayMode.CraftingLevel))
      {
        configuration.DisplayMode = LevelDisplayMode.CraftingLevel;
        configuration.Save();
      }
    }
  }

  private void DrawCheckbox(string label, string key, Func<bool> getter, Action<bool> setter)
  {
    ImGui.TextWrapped(label);
    var value = getter();
    if (ImGui.Checkbox(key, ref value))
    {
      setter(value);
    }
  }

  public override void Draw()
  {
    DrawSectionMasterEnable();
  }
}
