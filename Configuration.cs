using Dalamud.Configuration;
using System;

namespace TotalLevelPlugin;

public enum LevelDisplayMode
{
  TotalLevel,
  CombatLevel,
  CraftingLevel
}

[Serializable]
public class Configuration : IPluginConfiguration
{
  public virtual int Version { get; set; } = 0;
  public bool IsVisible { get; set; } = true;
  public bool MasterEnable { get; set; } = true;
  public LevelDisplayMode DisplayMode { get; set; } = LevelDisplayMode.TotalLevel;

  [NonSerialized]
  private Action? saveAction;
  public void Initialize(Action saveAction) => this.saveAction = saveAction;
  public void Save()
  {
    saveAction?.Invoke();
  }
}
