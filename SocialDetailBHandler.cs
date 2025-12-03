using System;
using System.Collections.Generic;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace TotalLevelPlugin;

public class SocialDetailBHandler : IDisposable
{
    private readonly Configuration configuration;

    // Node ID for the character name text node in SocialDetailB
    // This may need adjustment based on addon structure inspection
    private const uint NameNodeId = 3;

    private readonly Dictionary<string, string> jobToClass = new()
    {
        { "PLD", "GLA" },
        { "WAR", "MRD" },
        { "WMH", "CNJ" },
        { "SCH", "ACN" },
        { "MNK", "PUG" },
        { "DRG", "LNC" },
        { "NIN", "ROG" },
        { "BRD", "ARC" },
        { "BLM", "THM" },
        { "SUM", "ACN" },
    };

    private readonly Dictionary<string, uint> classNodeIds = new()
    {
        { "GLA", 44 },
        { "MRD", 45 },

        { "CJN", 53 },

        { "PUG", 63 },
        { "LNC", 64 },
        { "ROG", 65 },

        { "ARC", 72 },

        { "THM", 81 },
        { "ACN", 82 },
    };

    private readonly Dictionary<string, uint> jobNodeIds = new()
    {
        { "PLD", 39 },
        { "WAR", 40 },
        { "DRK", 41 },
        { "GNB", 42 },

        { "WHM", 48 },
        { "SCH", 49 },
        { "AST", 50 },
        { "SGE", 51 },

        { "MNK", 57 },
        { "DRG", 58 },
        { "NIN", 59 },
        { "SAM", 60 },
        { "RPR", 61 },
        { "VPR", 62 },

        { "BRD", 67 },
        { "MCH", 68 },
        { "DNC", 69 },

        { "BLM", 76 },
        { "SMN", 77 },
        { "RDM", 78 },
        { "PCT", 79 },
        { "BLU", 80 },
    };

    private readonly Dictionary<string, uint> dohNodeIds = new()
    {
        { "CRP", 85 },
        { "BSM", 86 },
        { "ARM", 87 },
        { "GSM", 88 },
        { "LTW", 89 },
        { "WVR", 90 },
        { "ALC", 91 },
        { "CUL", 92 },

        { "MIN", 94 },
        { "BTN", 95 },
        { "FSH", 96 },
    };

    public SocialDetailBHandler(Configuration configuration)
    {
        this.configuration = configuration;

        Service.AddonLifecycle.RegisterListener(AddonEvent.PostUpdate, "SocialDetailB", OnSocialDetailBUpdate);
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

        // Read battle job levels from AtkValues
        foreach (var (acronym, nodeId) in jobNodeIds)
        {
            var jobNode = addon->GetNodeById(nodeId);
            if (jobNode == null)
                continue;
            var textNode = jobNode->GetComponent()->GetNodeById(3)->GetAsAtkTextNode();
            if (textNode == null)
                continue;
            var text = textNode->NodeText.ToString();
            if (text.Contains("-") && jobToClass.TryGetValue(acronym, out var classAcronym) && classNodeIds.TryGetValue(classAcronym, out var classNodeId))
            {
                jobNode = addon->GetNodeById(classNodeId);
                if (jobNode == null)
                    continue;
                textNode = jobNode->GetComponent()->GetNodeById(3)->GetAsAtkTextNode();
                if (textNode == null)
                    continue;
                text = textNode->NodeText.ToString();
            }

            if (int.TryParse(text.Split(' ')[0], out var level))
            {
                battleTotal += level;
            }
        }

        // Read crafting job levels from AtkValues
        foreach (var (acronym, nodeId) in dohNodeIds)
        {
            var classNode = addon->GetNodeById(nodeId);
            var textNode = classNode->GetComponent()->GetNodeById(3)->GetAsAtkTextNode();
            if (textNode == null)
                continue;
            var text = textNode->NodeText.ToString();
            if (int.TryParse(text.Split(' ')[0], out var level))
            {
                craftingTotal += level;
            }
        }

        return (battleTotal, craftingTotal);
    }
}

