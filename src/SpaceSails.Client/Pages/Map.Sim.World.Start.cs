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
        // #1213 · THE CLOCK IS SET BEFORE SHE ARRIVES, and it has to be. See JumpTheClockBeforeSheArrives.
        JumpTheClockBeforeSheArrives(q);

        // Start point: an explicit /map?start=<id> jumps straight there (the renderer is live now, so
        // a docked-&-ashore start's board cue is safe); with no param, offer the boot picker so a
        // playtester (or a player who'd rather not always cast off from Earth) can choose a locale.
        if (q.DockCheat is not null && ResolveDockStartId(q.DockCheat) is { } dockHaven)
        {
            StartDockedAtHaven(dockHaven); // #288: boot already clamped on at any dockable berth
        }
        else if (q.StartId is { } asked && ThisSkyCanHonourTheStart(asked))
        {
            ApplyStart(asked);
        }
        else if (q.StartId is not null || q.DockCheat is not null)
        {
            // #1216 case 2 · THE SKY THAT WAS LOADED CANNOT HONOUR IT, so the boot refuses the way Appendix A
            // has always said an unknown start is refused: THE PICKER SHOWS. `/map?scenario=sol-eu&start=wreck`
            // used to reach CircularOrbitEphemeris.Position with a body id that sky has never held and throw
            // KeyNotFoundException onto the red error page — a legal pair of whitelisted cheats ending in a
            // stack trace. A start an id names and a start a WORLD can honour are two questions, and until now
            // only the first was asked (Map.Sim.World.Query checks the registry, which is scenario-blind).
            //
            // The door was never raised at the top of the boot, precisely because a cheat WAS asked for
            // (#323's civilian rule in RaiseTheFrontDoorWhileTheReactorWarms), so raising it here is the
            // refusal: the captain gets the berth list this scenario actually has, which is the same answer
            // a mistyped start gets.
            // Nothing is said — a dev cheat that cannot be honoured is a shortcut that did not work, never a
            // new failure mode, which is the rule `?dest=` is already written to (Map.Sim.World.Start).
            _showStartPicker = true;
            StateHasChanged();
        }
        else
        {
            // #161 · NOTHING HAPPENS HERE ANY MORE, AND THAT IS THE POINT. This branch used to peek the
            // vault and raise the front door; both now happen the moment the ephemeris exists
            // (OpenTheFrontDoorAsync), thirteen seconds earlier, which is the whole of this lane.
            //
            // Neither line is missed. A CIVILIAN URL — nothing but a scenario, which is what the home
            // page's Launch button and a returning captain's bookmark carry — had its door raised at the
            // top of the boot (#323), so the raise would be a no-op. And a URL that is NOT civilian asked
            // for a bench situation and must not be handed a menu: this branch is reached by a cheat that
            // seeds state without naming a start (?fuel=, ?ellipse=, ?kaamos=all, ?converge=1, …), and
            // those boot straight into the world the cheat built, exactly as they did before #311.
            //
            // Re-raising here would be worse than a no-op anyway: the captain can CHOOSE from this door
            // while the traffic is still being plotted, and a choice made at second two is waiting on this
            // very boot to return (see TheRestOfTheBootAsync). Putting the menu back over the voyage they
            // just started is exactly the bug that would be.
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

            // #1253 · …and ?havenfloor=-1 rides one leg further DOWN, before anybody sits anywhere: the ride
            // rebuilds the deck and moves the captain, so a sitting taken first would be a sitting at a top
            // on a floor he is no longer standing on. It goes through RideTheHavenLiftTo and never through a
            // coordinate of its own — the cheat rides the car exactly as the captain would, which is what
            // stops it drifting from the landing the doors actually open at.
            if (q.HavenFloorCheat is { } floor)
            {
                ShowPulseMessage(RideTheHavenLiftTo(floor, 0)
                    ? $"🛗 Test: you are on the {HavenLevels.NameOf(floor)} at {_havenName}, standing where the first cage's doors open. Three cars up, five cabins that do not open."
                    : "🛗 Test: ?havenfloor= needs a berth with a floor under its concourse. Try &dock=selene-gate.");
            }

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

        // #640 · ?nopattern=1 — the policy is already closed. HERE, beside the nerve seed and for the same
        // reason: the death staged a few lines below READS this. BustedResurrect asks the holder whether
        // there is a pattern on file as its very first question, so a flag set after the death was staged
        // would hand the captain the ordinary clinic, and the cheat would prove the opposite of the scene
        // it exists to reach. Set on the LIVE holder rather than through a vault round-trip, because the
        // round-trip is a separate claim with its own guard, and a cheat should stand up the state rather
        // than the plumbing.
        if (q.NoPatternCheat)
        {
            _nebula.MarkPolicyClosed();
            ShowPulseMessage("🧪 Test: NO PATTERN ON FILE — this captain's policy is closed. The next death is the last one, and nothing in the world will mention it again.");
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

        // #711 slice 2 — the box goes in the pocket BEFORE ?land= fires, because this cheat WRITES the
        // landing: it mints real parcels until one names ground this berth can reach, then points the
        // descent at it. Nothing happens here without ?parcel=1.
        TakeAParcelForCheat();

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

        if (q.CrewCheat is not null)
        {
            // #663 / #1066: the voyage the crew send a deputation over — bodies, and no money to show for
            // them — or, one landing further down, that same voyage with nobody ashore in five berths,
            // which is what convenes the meeting.
            SeedCrewCheat(q.CrewCheat);
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
            RevealBody(Derelict.RoadsterBodyId, "", announce: false);
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

        // #956 · ?dest=<body-id> — the navigation destination, laid down through the page's OWN door rather
        // than the field, so what boots is the state a captain's click would have produced (the Fly to order
        // it writes, the pass it dirties) and not a lookalike. An id the scenario does not carry is ignored
        // in silence, the way ?reveal= treats one: a cheat is a shortcut, never a new failure mode.
        if (q.DestCheat is { } destId && _ephemeris?.Bodies.Any(b => b.Id == destId) == true)
        {
            SetDestination(destId);
        }
    }

    /// <summary>The two seeded approaches, which suppress the picker because picking a berth would
    /// overwrite them — and then the purse and the tank, LAST, so a start’s own defaults are already down
    /// before these overwrite them. <b>The CLOCK is no longer one of them (#1213)</b>: what hour it is is not
    /// a default a start lays down, it is when the world is, and it now moves before she arrives — see
    /// <see cref="JumpTheClockBeforeSheArrives"/>.</summary>
    private void SeedTheApproachesAndThePurse(BootQuery q)
    {
        // ?sling=<bodyId>: boot onto an inbound arc with a close pass by that body (PR-G test hook).
        // ?skim=<bodyId>: boot onto a hyperbolic inbound grazing that body's atmosphere (PR-I test hook).
        //
        // #323 · BOTH USED TO SHUT THE START PICKER HERE, and neither does any more — because neither can
        // now find it open. Each of these lines existed to undo the old door rule, which raised the picker
        // for every URL that named no ?dock=/?start= and therefore raised it over a seeded approach that
        // picking a berth would have overwritten. The door's rule is the civilian one now (#323): a URL
        // carrying ?sling= or ?skim= is a bench incantation, so the door was never raised. A line that
        // lowers a door nobody opened is a guard that cannot fail, and it is gone rather than kept for
        // comfort — TheOneCivilianFrontDoorTests boots both of these URLs and asserts the direct boot.
        if (q.SlingCheat is not null)
        {
            SeedSlingCheat(q.SlingCheat);
        }

        if (q.SkimCheat is not null)
        {
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
    }

    /// <summary>
    /// #1213 · <b>?simhours=N JUMPS THE CLOCK BEFORE THE CAPTAIN ARRIVES, NOT AFTER.</b>
    ///
    /// <para><b>The law this cheat now keeps:</b> <c>?dock=X&amp;simhours=N</c> must build the world of a
    /// captain who really tied up at X when the station clock read N hours. Same berth, same frozen watch,
    /// same rota, same schedule of who finishes and goes. Anything else is a cheat handing over one world
    /// while the instruments describe another, which is this repository's third named bug class.</para>
    ///
    /// <para><b>What it was doing instead.</b> This jump used to be the last line of
    /// <see cref="SeedTheApproachesAndThePurse"/> — four stages AFTER <see cref="ApplyTheStartPoint"/>, which
    /// is where <c>?dock=</c> clamps on and where <c>SetDeckForDock</c> freezes <c>_dockVisitSimTime</c>, the
    /// watch the whole bar is resolved at (#410). So the cheat produced a room seated on <b>watch 0</b> with
    /// the clock reading N: <c>BarWatch</c> stayed 0 whatever <c>simhours</c> said, and
    /// <c>IntoTheBarsWatch = SimTime − BarWatch × WatchSeconds</c> came out as the whole of N — past every
    /// departure <see cref="Egress"/> had scheduled inside the first <see cref="Egress.LastCallFraction"/> of
    /// that watch. <b>A room emptied its entire evening of leavers on frame one</b>, and #1199's observation
    /// walk was dealt to a chair its person had just been walked out of: the rota (unchurned) still said he
    /// was there, the room (churned) said he was not, and the beat was spent in silence on nobody. Played
    /// headless at the documented link, GILT-EYE was gone from his chair 1.5 s after boot and 105 s of warp
    /// produced no walker, no card and no note.</para>
    ///
    /// <para><b>Why here and not a fifth stage.</b> Because the clock is not a cheat that seeds state on top
    /// of a world — it is <i>when</i> the world is. Every line below this one reads it: the berth's position
    /// and velocity are taken at <c>SimTime</c> (<c>ResolveDockHaven</c>, <c>BerthState.CoMoving</c>), the
    /// deck is welded at <c>SimTime</c>, the rota is resolved at <c>SimTime</c> and the Magpie's own post —
    /// which is the rota this cheat was written for in the first place — is a function of it. Setting it
    /// afterwards was never "the clock, later"; it was a different berth, a different room and a different
    /// evening, reported as the same one.</para>
    ///
    /// <para>It stays the ship's own clock and the page's, written together, because they are one fact. While
    /// docked, <c>HoldAtDock</c> re-pins the hull to the berth on the next tick exactly as before.</para>
    /// </summary>
    private void JumpTheClockBeforeSheArrives(BootQuery q)
    {
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
