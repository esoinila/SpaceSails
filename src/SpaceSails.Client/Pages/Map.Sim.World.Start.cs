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

// Subject: part of Map.Sim.World (#870 lane 7a; the header note lives in Map.Sim.World.cs) — the stages that hand the built world over: the start point, the cheats that need a live world under them, and the keyboard.
public partial class Map
{

    /// <summary>Where this boot ends: clamped on at a berth, at a named start point, or at the picker.</summary>
    private void ApplyTheStartPoint(BootQuery q)
    {
        // Start point: an explicit /map?start=<id> jumps straight there (the renderer is live now, so
        // a docked-&-ashore start's board cue is safe); with no param, offer the boot picker so a
        // playtester (or a player who'd rather not always cast off from Earth) can choose a locale.
        if (q.DockCheat is not null && ResolveDockStartId(q.DockCheat) is { } dockHaven)
        {
            StartDockedAtHaven(dockHaven); // #288: boot already clamped on at any dockable berth
        }
        else if (q.StartId is not null)
        {
            ApplyStart(q.StartId);
        }
        else
        {
            PeekSavedVault(); // #225: surface a "Continue — docked at <haven>" lead if a vault exists.
            _showStartPicker = true;
        }
    }

    /// <summary>The cheats that must run in THIS order, because each of them is read by the next: the
    /// ashore walk (after the clamp, before any ground), the nerve seed (before the descent prices it and
    /// before a death card asks it), the accepted expedition, the head-office route (before the shuttle
    /// board is computed off where the ship floats), and the landing — which carries the death with it,
    /// because the PLACE is read off the live excursion.</summary>
    private void StandTheCaptainWhereTheCheatsAsk(BootQuery q)
    {
        // #428 · ?ashore=1 — walk the walk for them. AFTER the clamp (the interior is welded by
        // SetDeckForDock, which the start above ran) and BEFORE any landing cheat, which brings its own
        // ground and takes the captain off this deck entirely.
        if (q.AshoreCheat)
        {
            ShowPulseMessage(StandAtTheBarThreshold()
                ? $"🍸 Test: you are ashore in {_havenName} — the ship → tube → hall walk is already behind you. [E] works the tables, the counter and the corners."
                : "🍸 Test: ?ashore=1 needs a berth with a walkable interior — this one has no bar to stand in. Try &dock=the-space-bar.");

            // #1016 · …and ?barcase=1 walks one leg further: onto a free top, sat down, with papers in the
            // sleeve. Immediately after the threshold, because it needs the deck the line above welded and
            // the coordinates it just wrote.
            SitAtABarTopIfAsked();
        }

        // #428 · ?nerve=N — seed the gauge BEFORE the landing cheat rides the shuttle down and before any
        // ?death= is staged, because both READ the live nerve: the descent's first frames price the gauge,
        // and the death card asks CaptainSuccession.OverdrawQualifies(_nerve) whether the captain was
        // already empty. Seeding after them would hand the card the default steady gauge and caption a
        // shattered captain as merely mauled — the sentence saying one thing while the sim did another.
        if (q.NerveCheat is { } seedPips)
        {
            _nerve = NervePips.FromPips(seedPips);
        }

        if (_pendingExpeditionCheat is not null)
        {
            InjectExpeditionCheat(); // #370: after the clamp — the accepted gig lands on a live, docked world
        }

        // #411 — the head-office route seat has to be applied BEFORE ?land= fires, because the shuttle
        // board is computed off where the ship floats and this cheat MOVES the ship to the ice moon. Every
        // other ?kaamos= value is state-only and rides the ordinary block further down.
        if (string.Equals(q.KaamosCheat, "hq", StringComparison.OrdinalIgnoreCase))
        {
            SeedKaamosHeadOfficeCheat();
            q.KaamosCheat = null;
        }

        if (_landCheat)
        {
            // #464: ride the shuttle down now that the berth is clamped and the ephemeris is live, so the
            // in-range board is real. Fire-and-forget: BeginSurfaceExcursion narrates its own descent
            // phases and yields between them, exactly as the hatch's own path does.
            // #621: …and ?death= waits for the boots to be on the ground, because the PLACE is read off the
            // live excursion. Killing the captain before the shuttle has landed would classify the death on
            // her deck and hand back the wrong card — which is the whole bug the cheat exists to hunt.
            _ = AutoLandThenStageDeathAsync(q.DeathCheat);
        }
        else if (q.DeathCheat is { } onHerDeck)
        {
            StageDeathCheat(onHerDeck);
        }
    }

    /// <summary>Everything that needs a live, docked world under it: the inbound rock, the fetch, the
    /// crack, the back room, the tip, the hoard, the two long arcs and their convergence, the oracle’s
    /// whereabouts, and every body a <c>?reveal=</c> charts.</summary>
    private void SeedTheArcsAndTheJobs(BootQuery q)
    {
        if (_pendingDeflectionCheat is not null)
        {
            InjectDeflectionCheat(); // #394: after the clamp — rock inbound, ship docked at the threatened port
        }

        if (q.FetchCheat is not null)
        {
            InjectFetchCheat(q.FetchCheat); // after the start, so the dest can be the station we docked at
        }

        if (q.CrackCheat is not null)
        {
            InjectCrackCheat(q.CrackCheat); // needs the docked station's deck built (a locked hatch to target)
        }

        if (q.BackroomCheat is not null)
        {
            InjectBackroomCheat(q.BackroomCheat); // PR-F: weld the wing open, or stage the crack that opens it
        }

        if (q.TipCheat is not null)
        {
            InjectTipCheat(); // seed a representative route tip so the ledger's Tips & intel is reachable
        }

        if (q.HoardCheat is not null)
        {
            InjectHoardCheat(q.HoardCheat); // #223: seed a buried chest and/or a bought rumour map
        }

        if (q.KaamosCheat is not null)
        {
            SeedKaamosCheat(q.KaamosCheat); // #411: assemble N KAAMOS fragments (readout + reach notice), or seat the pod/holder so the find itself can be played
        }

        if (q.NebulaCheat is not null)
        {
            SeedNebulaCheat(q.NebulaCheat); // #422: assemble N NEBULA fragments (readout + truth notice), or seat the adjuster so the bar scene itself can be played
        }

        if (q.OldCrewCheat)
        {
            SeedOldCrewCheat(); // #973 L5a: the four shipmates at THIS berth, and a captain already buried
        }

        if (q.ConvergeCheat)
        {
            SeedConvergeCheat(); // #422: seed both arcs' joint threshold and fire THE CONVERGENCE reveal
        }

        if (_oracleForce)
        {
            // #428: say WHERE she is, not just that she's here — the corner is deliberately clear of every
            // other console, and a captain who can't find her reads the cheat as broken.
            ShowPulseMessage("🌀 Test: Static Marsh has the port-back corner of this bar, whatever the watch. Walk in, head aft along the left wall, and press E on ◈ “STATIC” MARSH.");
        }

        // Tuesday plan PR-A: ?start=wreck drops you 2 km off the roadster — you're on top of her, so
        // chart her quietly (no "found it!" fanfare when you were parked alongside all along). This
        // also keeps ?start=wreck&fetch=active green.
        if (q.StartId == "wreck")
        {
            RevealBody("derelict-roadster", "", announce: false);
        }

        // ?reveal=<bodyId> (repeatable): chart any hidden body at boot for testing every downstream leg.
        foreach (string id in q.RevealCheats)
        {
            RevealBody(id, $"🧪 Test: {BodyName(id)} charted.");
        }

        // #997 wave 10 · ?target=<contact-id> — point the tactical UI at a contact so her dossier is on the
        // glass at boot. LAST in this stage on purpose: `?target=collector` sends the muscle, and the
        // muscle fits out at the nearest policed body to where the ship is NOW — which is only settled once
        // every start and every arc above has finished moving her.
        if (q.TargetCheat is not null)
        {
            SeedTargetCheat(q.TargetCheat);
        }
    }

    /// <summary>The two seeded approaches, which suppress the picker because picking a berth would
    /// overwrite them — and then the purse, the tank and the clock, LAST, so a start’s own defaults are
    /// already down before these overwrite them.</summary>
    private void SeedTheApproachesAndThePurse(BootQuery q)
    {
        // ?sling=<bodyId>: boot onto an inbound arc with a close pass by that body (PR-G test hook).
        // Suppress the start picker — picking a berth would overwrite the seeded approach state.
        if (q.SlingCheat is not null)
        {
            _showStartPicker = false;
            SeedSlingCheat(q.SlingCheat);
        }

        // ?skim=<bodyId>: boot onto a hyperbolic inbound grazing that body's atmosphere (PR-I test hook).
        if (q.SkimCheat is not null)
        {
            _showStartPicker = false;
            SeedSkimCheat(q.SkimCheat);
        }

        // ?credits=N / ?fuel=N (#288): seed the purse and tank last, after any start has laid down the
        // defaults, so an in-situ situation (afford a fill-up, reach a pump) is set up straight from boot.
        if (q.CreditsCheat is { } seedCredits)
        {
            _credits = seedCredits;
        }

        if (q.FuelCheat is { } seedPulses)
        {
            _reactionMassPulses = Math.Clamp(seedPulses, 0, ReactionMassCapacity);
        }

        // ?simhours=N: jump the sim clock at boot so the roaming Magpie's rota can be sampled (PR-F).
        // While docked, HoldAtDock re-pins the ship to the berth at the new time on the next tick.
        if (q.SimHoursCheat is { } jumpHours)
        {
            _ship = _ship with { SimTime = jumpHours * 3600 };
            SimTime = _ship.SimTime;
        }
    }

    /// <summary>Paint it, hand the keyboard over, and warm the cold surface draw path in the player’s own
    /// idle time. The focus is behind #737’s second gate: a captured @ref is only a live element while
    /// the page is mounted.</summary>
    private async Task HandThePageToThePlayerAsync(CancellationToken abandoned)
    {
        StateHasChanged();

        // The captured @ref is only a live element while the page is mounted; focusing a reference whose
        // element has left the DOM throws out of here (#737).
        abandoned.ThrowIfCancellationRequested();
        await _focusableDiv.FocusAsync();

        // #371 Phase 1 (perf) · warm the cold surface DRAW path once, idle-time, now that the map is
        // interactive. Fire-and-forget and yield-fronted so it never lengthens the perceived boot stall;
        // see WarmSurfaceDrawPathAtBootAsync for the never-flash guard.
        _ = WarmSurfaceDrawPathAtBootAsync();
    }
}
