using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.JSInterop;
using SpaceSails.Client;
using SpaceSails.Client.Layout;
using SpaceSails.Client.Rendering;
using SpaceSails.Contracts;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Map.Vault.Import — THE ONE QUESTION BEFORE A FILE BECOMES THE LIVE GAME, and the bank-first escape
// offered beside it.
//
// #251 · MOVED HERE BY PURE MOTION out of `Map.Vault.cs` — see the note at the head of
// `Map.Vault.Threads.cs` for why the file was cut and what "pure motion" is holding here.
public partial class Map
{
    // IMPORT = immediately the live game (owner, #310). Asks ONCE before replacing the current voyage,
    // offering a one-click "bank current to a slot first" escape. After import the rolling autosave
    // updates to the imported state, so Continue matches what the player sees.
    private bool _importConfirming;
    private string? _importPendingText;

    // The label the consent screen shows, read FROM THE FILE'S CONTENTS (#312) — never the filename, so a
    // renamed spacesails-vault (5).json still says "The Tilt · day 34" before the captain commits to it.
    private SaveSlotMeta? _importPreview;

    private async Task ImportVault()
    {
        string text;
        try
        {
            text = await RendererInterop.VaultImport();
        }
        catch
        {
            return; // a cancelled or failed picker is a no-op
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        // Read the label from the file itself so the ask can say WHAT it is about to board.
        try
        {
            _importPreview = SaveSlotLabels.PreviewMeta(VaultSerializer.Load(text));
        }
        catch
        {
            _importPreview = null; // an unreadable file: the ask still warns, just without a label
        }

        // Hold the picked file and ask before it replaces the running voyage.
        _importPendingText = text;
        _importConfirming = true;
        StateHasChanged();
    }

    // "Bank current first, then import": the safety escape — the running voyage is banked to a free
    // manual slot before the imported file becomes live, so nothing in-flight is lost.
    private async Task ConfirmImportBankingFirst()
    {
        SaveVaultManually();
        await ApplyPendingImport();
    }

    // "Replace now": the imported file becomes the live game at once (autosave adopts it).
    private Task ConfirmImportReplace() => ApplyPendingImport();

    private void CancelImport()
    {
        _importConfirming = false;
        _importPendingText = null;
        _importPreview = null;
    }

    private async Task ApplyPendingImport()
    {
        string? text = _importPendingText;
        _importConfirming = false;
        _importPendingText = null;
        _importPreview = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        await TheRestOfTheBootAsync(); // #161: the door's ⬆ Import is live before the world behind it is

        Vault vault = VaultSerializer.Load(text);
        // An imported file is a whole universe arriving — give it its OWN thread (feat/game-threads) so it
        // never overwrites the autosave of the run that was live; that run stays intact under its own thread
        // and remains resumable. ApplyVault clears the slate first, so the imported life boards clean.
        BeginNewGameThread();
        ApplyVault(vault);
        RequestVaultSave(); // the new thread's autosave adopts the imported state → Continue matches the screen
        RefreshThreadList();
        RefreshSlotList();
        ShowPulseMessage(vault.Tampered
            ? "📛 Imported — the file was edited outside the game; the ledger is marked tampered."
            : $"⬆ Imported — now flying {SaveSlotLabels.Where(vault)}.");
        if (!_deckMode && _activeDesk != ShipDesk.Nav)
        {
            SwitchDesk(ShipDesk.Nav);
        }

        StateHasChanged();
        await _focusableDiv.FocusAsync();
    }
}
