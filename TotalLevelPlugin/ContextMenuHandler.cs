using System;
using System.Runtime.CompilerServices;
using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Game.Text;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Info;

namespace TotalLevelPlugin;

public class ContextMenuHandler : IDisposable
{
    private readonly Configuration configuration;
    private readonly MenuItem searchInfoMenuItem;

    public ContextMenuHandler(Configuration configuration)
    {
        this.configuration = configuration;

        // Create the menu item that will appear in context menus
        searchInfoMenuItem = new MenuItem
        {
            Name = "TotalLevel: Search Info",
            Prefix = SeIconChar.BoxedLetterT, // You can change this icon
            OnClicked = OnSearchInfoClicked,
            IsEnabled = true
        };

        Service.ContextMenu.OnMenuOpened += OnMenuOpened;
    }

    public void Dispose()
    {
        Service.ContextMenu.OnMenuOpened -= OnMenuOpened;
    }

    private unsafe void OnMenuOpened(IMenuOpenedArgs args)
    {
        if (!configuration.MasterEnable || !configuration.ShowContextMenuEntry)
            return;

        // Only add to context menus for game objects (characters in the world)
        if (args.Target is MenuTargetDefault target)
        {
            // Check if this is a player character
            if (target.TargetContentId != 0)
            {
                args.AddMenuItem(searchInfoMenuItem);
            }
        }
    }

    private unsafe void OnSearchInfoClicked(IMenuItemClickedArgs args)
    {
        if (args.Target is not MenuTargetDefault target)
            return;

        var ad = AgentDetail.Instance();
        InfoProxyCommonList.CharacterData* cdata = null;

        if (target.TargetCharacter != null)
        {
            // Use the CharacterData directly from the target, if available
            cdata = (InfoProxyCommonList.CharacterData*)target.TargetCharacter.Address;
        }
        else
        {
            var searchProxy = InfoProxySearch.Instance();
            // Search for the a CharacterData entry in the InfoProxySearch list
            var entry = searchProxy->GetEntryByContentId(target.TargetContentId);
            if (entry != null)
            {
                // Use the found entry.
                cdata = entry;
            }
            else
            {
                // Forge a CharacterData struct with what info we have available.
                // This will be missing Grand Company, Free Company, and other details.
                var data = new InfoProxyCommonList.CharacterData();
                data.ContentId = target.TargetContentId;
                data.NameString = target.TargetName;
                data.State = InfoProxyCommonList.CharacterData.OnlineStatus.NotFound;
                data.ClientLanguage = InfoProxyCommonList.CharacterData.Language.None;
                data.HomeWorld = (ushort)target.TargetHomeWorld.RowId;
                cdata = &data;
            }
        }

        if (cdata == null)
            return;

        ad->OpenForCharacterData(cdata);
        return;
    }
}
