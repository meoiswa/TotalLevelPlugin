using System;
using System.Linq;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;

namespace TotalLevelPlugin;

public class SocialDetailBHandler : IDisposable
{
    private readonly Configuration configuration;

    private delegate int GetClassIndexDelegate(uint classJobId);

    [Signature("E8 ?? ?? ?? ?? 85 C0 78 ?? 45 8B C4 8D 50")]
    private GetClassIndexDelegate GetClassIndex { get; set; }

    // Start index for the class job levels in the number array
    private readonly uint baseArrayIndex = 109;

    // Node ID for the character name text node in SocialDetailB
    private const uint NameNodeId = 3;

    public SocialDetailBHandler(Configuration configuration)
    {
        this.configuration = configuration;

        Service.AddonLifecycle.RegisterListener(AddonEvent.PostUpdate, "SocialDetailB", OnSocialDetailBUpdate);
        Service.GameInteropProvider.InitializeFromAttributes(this);
    }

    public void Dispose()
    {
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PostUpdate, "SocialDetailB", OnSocialDetailBUpdate);
    }

    private void OnSocialDetailBUpdate(AddonEvent type, AddonArgs args)
    {
        if (!configuration.MasterEnable)
            return;

        UpdateSocialDetailB(args);
    }

    private unsafe void UpdateSocialDetailB(AddonArgs args)
    {
        try
        {
            nint addonPtr = args.Addon;
            if (addonPtr == nint.Zero)
                return;

            var addon = (AtkUnitBase*)addonPtr;
            if (addon == null)
                return;

            if (!addon->IsFullyLoaded())
                return;

            // Calculate total levels from AtkValues
            var (battleTotal, craftingTotal) = CalculateTotalLevelsFromAddon(addon);

            // Determine which level to display based on configuration
            var (displayLevel, levelLabel) = configuration.DisplayMode switch
            {
                LevelDisplayMode.CombatLevel => (battleTotal, "Combat"),
                LevelDisplayMode.CraftingLevel => (craftingTotal, "Crafting"),
                _ => (battleTotal + craftingTotal, "Total")
            };

            if (displayLevel <= 0)
                return;

            // Find and modify the name node
            var nameNode = addon->GetNodeById(NameNodeId);
            if (nameNode == null)
                return;

            // The name node should be a text node
            if (nameNode->Type != NodeType.Text)
                return;

            var textNode = (AtkTextNode*)nameNode;
            var originalName = textNode->NodeText.ToString();

            // Only modify if we haven't already modified it (check for any level suffix)
            if (originalName.Contains($"({levelLabel} level-{displayLevel})"))
                return;

            if (originalName.Contains($"("))
            {
                // remove the level suffix
                var parenthesisIndex = originalName.IndexOf('(');
                originalName = originalName.Substring(0, parenthesisIndex - 1);
            }

            // Append level to the name
            var newText = $"{originalName} ({levelLabel} level-{displayLevel})";
            textNode->SetText(newText);

            Service.PluginLog.Debug($"Modified SocialDetailB name: {originalName} -> {newText} (Battle: {battleTotal}, Crafting: {craftingTotal})");
        }
        catch (Exception ex)
        {
            Service.PluginLog.Error(ex, "Error updating SocialDetailB");
        }
    }

    private unsafe (int battleTotal, int craftingTotal) CalculateTotalLevelsFromAddon(AtkUnitBase* addon)
    {
        var battleTotal = 0;
        var craftingTotal = 0;

        var numberArray = AtkStage.Instance()->GetNumberArrayData(NumberArrayType.Hud2);
        var classJobSheet = Service.DataManager.Excel.GetSheet<ClassJob>();

        // Group the class jobs by the exp array index (groups Classes and Jobs together, like SMN+SCH+ACN)
        foreach (var group in classJobSheet.GroupBy(x => x.ExpArrayIndex).Where(x => x.Key >= 0))
        {
            var row = group.First();
            var classJobCategoryId = row.ClassJobCategory.RowId;
            var isBattleJobGroup = classJobCategoryId == 30 || classJobCategoryId == 31;
            var isCraftingJobGroup = classJobCategoryId == 32 || classJobCategoryId == 33;

            if (!isBattleJobGroup && !isCraftingJobGroup)
                continue;

            var classJobIndex = GetClassIndex(row.RowId);
            var classJobLevel = numberArray->IntArray[baseArrayIndex + classJobIndex];

            if (isBattleJobGroup)
                battleTotal += classJobLevel;
            else if (isCraftingJobGroup)
                craftingTotal += classJobLevel;
        }
        return (battleTotal, craftingTotal);
    }
}
