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

// Subject: part of Map.Sim (#870 split; the header note lives in Map.Sim.cs) — every key the page answers, and the mouse's way back to the keyboard.
public partial class Map
{

    private void OnKeyUp(KeyboardEventArgs e)
    {
        if (_deckMode)
        {
            _deckKeys.Remove(Canonical(e.Key));
        }
    }

    private void OnFocusOut(FocusEventArgs e) => _deckKeys.Clear();

    // 2026-07-18 playtest: after a mouse affordance — closing the treasure-map card ("Into the ledger"),
    // clicking a desk tab — DOM focus stayed on the button, so the map div went deaf to the 0–7 desk keys
    // and E until the captain clicked the page again. The one idiom: every click that should hand the
    // keyboard back to the helm routes its state change through here, then pulls focus home to the map div.
    // Keyboard paths already own focus, so they never call this — this is the mouse's way back to the keys.
    private async Task RefocusMap() => await _focusableDiv.FocusAsync();

    // #470 · THE MOUSE'S WAY BACK, MADE GENERAL. The idiom above was right and was applied exactly four
    // times — to the four cards whose deafness the owner happened to hit and report. Nine others still took
    // the keyboard away and never gave it back, and every new card inherited the same trap (the first-ground
    // tutorial ended up switching off the three keys it had just taught).
    //
    // The fix is a seam rather than nine more copies. The Close*/Dismiss* methods stay plain synchronous
    // state changes — they are also called by the Esc handler and by the one-card-at-a-time chaining, and
    // BOTH of those are keyboard paths that already own focus. Only the mouse needs the way home, so only
    // the mouse routes through here: @onclick="() => Dismiss(CloseDossier)".
    private async Task Dismiss(Action close)
    {
        close();
        await RefocusMap();
    }

    // #735 · The same seam, under the name the other kind of caller means. A card button that ADVANCES a
    // card rather than closing it (the freeze beat's "…wake up" walks the death card to its next stage)
    // needs the keyboard handed back just as badly: the button it was clicked on leaves the DOM with the
    // stage, focus falls to <body>, and the next stage's Enter then has nowhere to land. One line, no
    // second copy of the idiom — Dismiss is not renamed because fifty call sites do mean "dismiss".
    private Task PressAndRefocus(Action act) => Dismiss(act);

    private static string Canonical(string key) => key switch
    {
        "W" or "ArrowUp" => "w",
        "A" or "ArrowLeft" => "a",
        "S" or "ArrowDown" => "s",
        "D" or "ArrowRight" => "d",
        _ => key,
    };

    // #338 addendum · THE GAME'S FIRST SOUND — the master audio switch (default ON, remembered browser-side
    // in JS). _audioArmed also does double duty as item-4's gesture unlock: the first keypress of the
    // session both arms the WebAudio context (so a chirp fired later from the rAF loop can sound) and syncs
    // our on/off label from the remembered pref.
    private bool _audioEnabled = true;
    private bool _audioArmed;

    private void ToggleAudio()
    {
        _audioEnabled = !_audioEnabled;
        RendererInterop.SetAudioEnabled(_audioEnabled);
        ShowPulseMessage(_audioEnabled
            ? "🔊 Sound on — the tracker will chirp on first contact."
            : "🔇 Sound muted. (Press M to bring it back.)");
    }

    private void OnKeyDown(KeyboardEventArgs e)
    {
        // #338 addendum item 4: unlock audio on the first keypress and adopt the remembered mute pref, so a
        // chirp fired later from the render loop isn't silently blocked.
        if (!_audioArmed)
        {
            _audioArmed = true;
            RendererInterop.ArmAudio();
            _audioEnabled = RendererInterop.GetAudioEnabled();
        }

        if (_shuttleRun is not null)
        {
            switch (e.Key)
            {
                case "w" or "W" or "ArrowUp" or "a" or "A" or "ArrowLeft"
                    or "s" or "S" or "ArrowDown" or "d" or "D" or "ArrowRight":
                    _deckKeys.Add(Canonical(e.Key));
                    return;
                case "q" or "Q":
                    EndShuttleRun(boarded: false, "Boarding run aborted — shuttle back in the cradle");
                    return;
                default:
                    return;
            }
        }

        // Desk switching (StationDesks.md rule 3): number keys 1-7 always win, even mid-deck-walk
        // (7 re-enters/toggles deck, 1-6 leave it) — checked before HandleDeckKey so WASD/E/Q
        // never shadow them, and before the pulse switch below so digits never fire a burn.
        // Inputs/sliders already stop propagation on their own keydown (see the plot panel's
        // range/number fields), so typing into them never reaches this handler at all.
        if (e.Key.Length == 1 && e.Key[0] is >= '1' and <= '7')
        {
            var deskKey = (ShipDesk)(e.Key[0] - '0');
            // #330: ashore, a desk shortcut can't silently yank the captain off the regolith — the desks
            // are a tube ride up. Deck (7) is where they already stand, so it stays a no-op switch.
            if (_surface is not null && deskKey != ShipDesk.Deck)
            {
                ShowPulseMessage("🧭 The nav desk is a tube ride away, captain — board the shuttle to get back to it.");
                return;
            }
            SwitchDesk(deskKey);
            return;
        }

        // PR-15: the captain's position is key `0` — same digit-key rules as 1-7 above (wins
        // mid-deck-walk, checked before HandleDeckKey/the pulse switch).
        if (e.Key == "0")
        {
            if (_surface is not null)
            {
                ShowPulseMessage("🧭 The captain's desk is a tube ride away — board the shuttle first.");
                return;
            }
            SwitchDesk(ShipDesk.Captain);
            return;
        }

        // #735 · Enter presses the visible primary action of an open card — the keyboard YES, next to the
        // keyboard CANCEL below. Checked BEFORE the flight keys for the same reason Esc is: while a card
        // has the screen, the keys belong to the card. Nothing open to confirm and Enter falls through to
        // the helm, which does not bind it either — so this is a key the game had spare.
        if (e.Key is "Enter" or "NumpadEnter")
        {
            if (TryConfirmTopOverlay())
            {
                StateHasChanged();
            }
            return;
        }

        if (e.Key == "Escape")
        {
            // #351 (owner 2026-07-18: "No way to close this dialog? Where is cancel?") — Escape is the
            // keyboard CANCEL for the deck/flight cards: close the top-most open overlay first (reusing
            // each card's own house closer), and only fall through to the helm when nothing's open to
            // dismiss. Without this, Escape over an open offer card yanked the captain off the deck to Nav.
            if (TryDismissTopOverlay())
            {
                StateHasChanged();
                return;
            }
            // Ashore, Escape doesn't switch desks (that would leave the surface silently) — let it fall
            // through to nothing rather than yanking the captain up the tube.
            if (_surface is null)
            {
                SwitchDesk(ShipDesk.Nav);
            }
            return;
        }

        // Owner request: ` peeks at the map — hide every panel to read the sky, tap again to restore. Works
        // on any desk. #1038 · this key TOGGLES and is the only one that does; the 👁 button (still lit on
        // the tab bar, .peek-keep) and Escape (top of the cancel chain above) are the two ways OUT, so no
        // key that means "stop" can ever start a peek.
        if (e.Key is "`" or "~")
        {
            TogglePeekMap();
            return;
        }

        // #338 addendum: M mutes/unmutes all sound (the first-contact chirp and every cue). Global — the
        // audio switch is not a surface-only affordance.
        if (e.Key is "m" or "M")
        {
            ToggleAudio();
            return;
        }

        // #406: `/` opens the Nav search box and hands it the keyboard — type a name to find & jump to a
        // target instead of zoom-hunting. Only on the desks that render the solar map (where the box
        // lives — the same Nav/Sensors/WarRoom gate). The box's own keydown stops propagation, so once
        // it has focus the typed keys never reach this handler to drive the ship.
        if (e.Key == "/" && _surface is null && _activeDesk is ShipDesk.Nav or ShipDesk.Sensors or ShipDesk.WarRoom)
        {
            _ = FocusNavSearch();
            return;
        }

        if (_deckMode && HandleDeckKey(e.Key))
        {
            return;
        }

        bool pulse = false;
        double factor = 1.0;

        if (e.Key is "o" or "O")
        {
            EnterOrbit();
            return;
        }


        // Shift = fine trim (±1%) for orbital finesse near planets; plain = the full ±10%.
        bool fine = e.ShiftKey;
        switch(e.Key)
        {
            case "+":
            case "=":
            case "ArrowUp":
                factor = fine ? 1.01 : ManeuverPlan.AccelerateFactor;
                pulse = true;
                break;
            case "-":
            case "_":
            case "ArrowDown":
                factor = fine ? 0.99 : ManeuverPlan.DecelerateFactor;
                pulse = true;
                break;
            case "p":
            case "P":
                TogglePlotMode();
                return;
            case "v":
            case "V":
                VentCharge();
                return;
        }

        if (pulse)
        {
            // PR-I: a holed sail can't thrust — the crew is still sewing (fires until the repair window closes).
            if (_sailHoled)
            {
                double daysLeft = Math.Max(0, (_sailRepairedAtSimTime - _ship.SimTime) / 86400.0);
                ShowPulseMessage($"Sail holed — no drive while the crew sews (~{daysLeft:F1} d)");
                return;
            }

            // Firing the drive breaks the clamps — you can't burn while bolted to a dock.
            if (_dockedHavenId is not null)
            {
                Undock();
            }

            if (_reactionMassPulses <= 0)
            {
                ShowPulseMessage("Out of reaction mass");
                return;
            }
            if (_ship.SimTime < _lastPulseSimTime + PulseCooldownSeconds)
            {
                ShowPulseMessage("Pulse drive cooling down…");
                return;
            }

            _ship = _ship with { Velocity = _ship.Velocity * factor };
            _reactionMassPulses--;
            _lastPulseSimTime = _ship.SimTime;
            ShowPulseMessage(factor > 1
                ? (fine ? "Trim: +1%" : "Pulse: accelerate +10%")
                : (fine ? "Trim: −1%" : "Pulse: decelerate −10%"));
            RendererInterop.PlayCue("pulse");

            // A live override invalidates every still-pending node (plan §4).
            bool anyStaled = false;
            foreach (PlanNode node in _planNodes)
            {
                if (!node.Stale && !node.Executed && node.SimTime > _ship.SimTime)
                {
                    node.Stale = true;
                    anyStaled = true;
                }
            }

            if (anyStaled)
            {
                RebuildPlan();
                ShowPulseMessage("Plan invalidated downstream");
            }

            ReprojectTrajectory();
        }
    }
}
