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

// Map.Vault.FrontDoor — THE DOOR THE GAME OPENS ON: what the boot peek found to continue, the ten-berth
// rack the logbook draws, and the two ways that surface is opened (the front door, the captain's drawer).
//
// #251 · MOVED HERE BY PURE MOTION out of `Map.Vault.cs` — see the note at the head of
// `Map.Vault.Threads.cs` for why the file was cut and what "pure motion" is holding here.
public partial class Map
{
    // Peeked at boot so the front-door load view can lead with "Continue — <where>".
    private bool _resumeAvailable;
    private string? _resumeHavenName;
    private bool _resumeTampered;
    private Vault? _pendingResumeVault;

    // The labels of every occupied slot, projected for the front-door and the captain's-desk drawer.
    private IReadOnlyList<SaveSlotMeta> _slotList = [];

    // The in-game save/load drawer (#310, #292 quiet-drawer law): the SAME surface as the boot front
    // door, opened from the captain's desk. One surface, two doors.
    private bool _showSaveDrawer;

    private void OpenSaveDrawer()
    {
        RefreshSlotList();
        _resumeAvailable = Slots.Newest() is not null;
        _resumeHavenName = Slots.Newest()?.Where;
        _showSaveDrawer = true;
    }

    private void CloseSaveDrawer() => _showSaveDrawer = false;

    // ─────────────────────────────────────────────────────────────────────────────────────────────
    // #951 · THE LOGBOOK IS REACHABLE FROM THE DEATH CARD TOO. Owner, having undocked and flown a periapsis
    // under Selene Gate's own surface: "I undocked from station and died. I want an option to load previous
    // game here like in the beginning."
    //
    // The death cards offered exactly one verb — "…wake up" — which is the brain-backup beat and stays
    // exactly where it is (it is canon: the captain IS revived, and the succession/clinic pages hang off it).
    // Beside it now stands the other true answer to a death: that was not the run I meant to keep, take me
    // back to one I banked. It opens the SAME logbook the boot door opens — Continue on the newest autosave,
    // every banked berth listed — because there is only one save surface in this game and this is it.
    //
    // The death card is drawn in the modal band ABOVE the logbook's own backdrop, so while the logbook is
    // open the busted card yields (Map.razor guards its render on !_showSaveDrawer). Closing the logbook
    // brings the death card back untouched — nobody is trapped, and the beat is not skipped by looking.
    // ─────────────────────────────────────────────────────────────────────────────────────────────
    private void OpenLogbookFromDeath()
    {
        OpenSaveDrawer();
        StateHasChanged();
    }

    // The nine manual slot ids (1..9), for the drawer/front-door to render a bank-to row per slot.
    private static readonly string[] ManualSlotIds =
        [.. Enumerable.Range(1, SaveSlotBook.ManualSlotCount).Select(SaveSlotBook.ManualSlotId)];

    // The WHOLE rack (#312), in fixed slot order: the rolling autosave first, then the nine manual berths.
    // The front door and the captain's-desk drawer both render all ten — occupied ones (newest-first from
    // the label list) then empty berths — so the logbook is a shelf, never just the occupied handful.
    private static readonly string[] AllSlotIds =
        [SaveSlotBook.AutoSlotId, .. ManualSlotIds];

    // Every dockable station haven (id + name), inner → outer (scenario body order), straight from the one
    // registry (#297/#288) — the front door's primary "pick a berth to begin" list AND the ?dock menu.
    private IReadOnlyList<(string Id, string Name)> BerthStarts()
        => _ephemeris is null
            ? []
            : [.. DockableHavens.All(_ephemeris).Select(b => (b.Id, b.Name))];

    /// <summary>
    /// #161 · IS THE FRONT DOOR LIVE? — which is a different question from "is the world ready", and
    /// asking the wrong one is what left the boot picker dead for fourteen seconds.
    ///
    /// <para>The door needs two things and only two: the berth roster, which is
    /// <see cref="BerthStarts"/> off the ephemeris, and the vault, which <c>PeekSavedVault</c> reads.
    /// The boot does both in one breath (<c>OpenTheFrontDoorAsync</c>, no await between the ephemeris
    /// build and the peek, deliberately) — so the ephemeris standing there IS the whole answer, and this
    /// page needs no flag of its own to say so. That matters beyond tidiness: every instance field here
    /// is swept by the 793-field roster and by the boot's own fingerprint, and a boolean that only ever
    /// says what <c>_ephemeris is not null</c> already says would have joined both ledgers for nothing.</para>
    ///
    /// <para>In game it is simply always true, which is right: the logbook drawer opens over a running
    /// world.</para>
    ///
    /// <para><b>What it does NOT say</b>, said out loud so nobody reads more into it than is there: this is
    /// "the world the door offers exists", not "the vault has been read". The two coincide only because the
    /// boot does them in one breath, and that coincidence is the boot's to keep — move
    /// <c>OpenTheFrontDoorAsync</c> away from the ephemeris and the berths would go live with no Continue
    /// beside them. The UiGate's front-door budget measures the berths (a fresh browser context has no
    /// saved voyage to continue), so the ordering is guarded by the conductor's own shape and by this
    /// note, not by a browser.</para>
    /// </summary>
    private bool FrontDoorReady => _ephemeris is not null;

    /// <summary>
    /// #161 · THE DOOR IS OPEN BEFORE THE WORLD IS. So every verb ON that door — Continue, a berth, a
    /// captain's saved row, an imported file — waits here for the rest of the boot before it acts.
    ///
    /// <para>The wait is honest rather than hidden: the caller has already shut the picker, so what shows
    /// through underneath is the boot's OWN loading door, gear turning, narrating the phase it is on
    /// ("plotting the traffic lanes — freighter 5 of 8…"). A captain who chooses at second two watches
    /// the same thing they used to watch before the door opened at all; a captain who takes ten seconds
    /// reading the berths waits for nothing.</para>
    ///
    /// <para>Awaiting <c>Boot</c> is the whole mechanism. It is the one handle on the running boot
    /// (#737), it swallows its own abandonment, and it completes AFTER <c>ApplyTheStartPoint</c> — which
    /// is why that method no longer re-raises the picker over the voyage this click is starting.</para>
    /// </summary>
    private Task TheRestOfTheBootAsync()
    {
        if (_worldReady || Boot is not { } booting)
        {
            return Task.CompletedTask;
        }

        StateHasChanged(); // the picker is shut; let the boot's own door and its phase line show through
        return booting;
    }

    // Boot a brand-new voyage already clamped on at a chosen berth — the front door's primary start action
    // (docked-starts rework, 2026-07-18). Dismisses the front door and hands to the shared docked-start
    // path, then lands on the deck (walkable haven) or the Nav map (pumps-only berth).
    private async Task ChooseBerthStart(string havenId)
    {
        _showStartPicker = false;
        await TheRestOfTheBootAsync(); // #161: the door opens before the world behind it is finished
        EnterNewGameThread(); // a berth start is a NEW voyage — fresh universe, fresh thread (feat/game-threads)
        StartDockedAtHaven(havenId);
        MaybeGreetTutorialHome(havenId); // a fresh new captain picking Selene Gate gets the soft-catch lesson, seeded here
        if (!_deckMode && _activeDesk != ShipDesk.Nav)
        {
            SwitchDesk(ShipDesk.Nav);
        }

        StateHasChanged();
        await _focusableDiv.FocusAsync();
    }
}
