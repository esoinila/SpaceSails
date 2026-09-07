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

// Subject: BUSTED — the catch, the three options, the dice, and the brain-backup that wakes a new captain. Part of Map.Combat (#870 split; the header note lives in Map.Combat.cs).

/// <summary>
/// THE CATCH, AND EVERY OTHER WAY THE PANEL OPENS — the grapples landing, a writ that waited coming due
/// on its own filed terms (#1151), a surface impact, an overdraw death, the auto-land that stages one,
/// and the dev door that stages any of them by name.
///
/// <para>#251 · This is the opening file of a four-part family. The other three are named for the stage
/// they own: <c>.Options</c> (the dice and the three answers — submit, bribe, resist, and Bolivia),
/// <c>.Wake</c> (the freeze-frame, the clinic, the successor captain and the haven he wakes at), and
/// <c>.Encounter</c> (the record the card is drawn from, and the two presses that are not one of the
/// three).</para>
///
/// <para>The family's one static field, <c>NebulaGlitchFlashes</c>, is four literal strings and reads
/// nothing; it stays beside its only reader in <c>.Wake</c>. See
/// <c>NoPartialClassSpreadsItsStaticFieldsTests</c> for why the chain is the hazard and the spread is
/// not. No member is renamed, re-scoped or re-ordered by the cut.</para>
/// </summary>
public partial class Map
{
    // PR-BUSTED (owner ruling §5): the catch is no longer an instant tax that leaves the collector
    // inert — it OPENS the boarding pop-up. Warp yanks to 1×, the ship is grappled, and the collector
    // hails with a demand and three options (SUBMIT / BRIBE / RESIST). The seed is folded from the
    // hunter's identity and the sim moment, so every roll in this encounter is reproducible.
    private void ApplyHunterCatch(HunterState hunter) => TheDemandGoesUp(hunter, onTheTermsOf: null);

    /// <summary>
    /// The body of the catch, and the one place a boarding demand is built. <see cref="ApplyHunterCatch"/> is
    /// still the door every ordinary catch comes through, unchanged and one-armed — a second parameter with a
    /// default on it looks free and is not: three other files reach this method by reflection, where an
    /// optional argument is a missing argument.
    /// </summary>
    /// <param name="onTheTermsOf">#1151 slice 4 · A WRIT THAT WAITED, or null on every ordinary catch. The
    /// demand's two numbers — the heat it is worth and the moment its seed is cut from — are the FILE's and
    /// not today's when a writ is being served, so a captain who kept a collector waiting is charged neither
    /// less nor more for the wait. Everything else about the beat is identical, because it is the same beat:
    /// the same panel, the same three options, the same dice.</param>
    private void TheDemandGoesUp(HunterState hunter, PendingWritRecord? onTheTermsOf)
    {
        Warp = 1;
        _effectiveWarp = 1;
        RendererInterop.PlayCue("board");

        int heat = Math.Max(1, onTheTermsOf?.HeatWhenFiled ?? _heat.Level);
        double moment = onTheTermsOf?.FiledAtSimTime ?? SimTime;

        ulong seed = DiceRule.Seed("busted", HunterSeqOf(hunter.Id), (long)moment);
        _busted = new BustedEncounter
        {
            HunterId = hunter.Id,
            HunterCallsign = hunter.Callsign,
            Heat = heat,
            Seed = seed,
            Bribe = BustedRule.BribeDemand(heat, seed),
            Cause = DeathCause.Collector,          // #380: a catch that ends in the volley is a collector death
            DeathBodyName = _nearestBody?.Name,    // the place the last stand happened, for the wake card
        };

        // #422 arc 2 — THE COLLECTOR'S WRIT. A heat/collector catch is the moment you get a look at what the
        // repo men are really sent to recover: not the cargo, a PATTERN. The glimpse assembles the shard the
        // first time; the writ line then rides the demand card that once, so the recontextualization lands
        // without cluttering every later stop. They work Nebula's collateral.
        if (AssembleNebulaSilently("collector-writ"))
        {
            _busted.CollectorWrit = NebulaLore.ById("collector-writ")!.Lore;
        }

        // #777 · AND THE BEAT IS FINALLY COUNTED AS TOLD. The demand panel opening IS StoryBeats' collector
        // hail — it has rendered that beat's painting at the top of itself since #528 — so the beat is raised
        // through the one door, HOSTED: the seam spends the cadence and writes the hail's words into the log,
        // and raises no card, because the card is the thing we just opened. Reading a beat's ArtFile out of
        // markup was never the same as raising it; that is what left this one an orphan through #663.
        RaiseStoryBeat(StoryBeats.Beat.CollectorHail, hunter.Callsign);

        // …and the bird reads the SAME number the card does — #1151 slice 4. It was `_heat.Level` written out
        // a third time, which on a served writ would have had the parrot quoting an exposure the demand it is
        // squawking about is not priced on.
        SquawkNow(Parrot.Squawk.Busted, _lastTimestampMs ?? 0, BustedRule.ExposurePhrase(heat), force: true);
        StateHasChanged();
    }

    // #264 — the impact enforcer's consequence. Lab 16's "periapsis under the surface — impact coming"
    // finally arrives: a LIVE-FLOWN step reached a body's surface radius (SurfaceImpact caught the
    // crossing; the ship never flew the interior). Reuse the BUSTED freeze-frame → brain-backup
    // resurrection whole — the death machinery is not duplicated — so you wake at the nearest haven's
    // clinic in the insurance rustbucket, ship and visible cargo gone, banked/buried safe. There is no
    // collector here (the planet collected), no heat, no dice: straight to the freeze. Say-the-state:
    // the ledger logs it, the strip shouts it, the parrot squawks. Docked ships and havens on rails
    // can't reach here — the caller exempts the dock and SurfaceImpact skips zero-radius havens.
    private void TriggerImpact(SurfaceImpact.Crossing hit)
    {
        if (_busted is not null)
        {
            return; // already mid-reckoning — one death at a time
        }

        Warp = 1;
        _effectiveWarp = 1;

        // Pin the ship to the point of contact so nothing coasts on behind the modal; the resurrection
        // resets it onto the clinic haven when the captain wakes.
        _ship = _ship with { Position = hit.Position, Velocity = Vector2d.Zero, SimTime = hit.SimTime };

        RendererInterop.PlayCue("board");    // impact/volley hook (a dedicated cue is a follow-up)
        RendererInterop.PlayCue("gameover"); // game-over-music hook

        _busted = new BustedEncounter
        {
            HunterId = string.Empty,        // no collector — the surface collected
            HunterCallsign = hit.BodyName,
            Heat = 0,
            Seed = DiceRule.Seed("impact", (long)hit.SimTime),
            Bribe = default,                // unused on the impact path (no bribe to a planet)
            Phase = BustedEncounter.Stage.Impact,
            ImpactBodyName = hit.BodyName,
            Cause = DeathCause.Impact,      // #380: the surface collected the ship — a place-dependent death
            DeathBodyName = hit.BodyName,
        };

        string line = $"💥 IMPACT — the ship struck {hit.BodyName}. Periapsis went under the surface, and the surface won.";
        LogAutopilotEvent(line);
        ShowPulseMessage(line);
        _shipAlerts.Raise(AlertKind.Collision, AlertSeverity.Red, $"IMPACT — struck {hit.BodyName}", SimTime);
        SquawkNow(Parrot.Squawk.Impact, _lastTimestampMs ?? 0, hit.BodyName, force: true);
        StateHasChanged();
    }

    // Evening wind #20 (owner 2026-07-18) — THE OVERDRAW DEATH. Nerves shot past empty and a Reever's hand
    // is the last straw: the captain goes crazy and dramatically exits the scene. Route the SURFACE causes
    // (#380 item 1, wired-ready) live for the first time: classify place-dependently via
    // DeathNarration.SurfaceEnd (the Old Ones TOOK you — or, rarely, you JOINED them) and hand it to the
    // SAME BUSTED freeze-beat → brain-backup resurrection the collector/impact deaths use — the death
    // machinery is shared, never duplicated. No collector, no dice: straight to the surface freeze-beat. The
    // resurrection folds the excursion away as a failed gig and issues a new captain (BustedResurrect).
    /// <param name="nerveRanOut">True when the NERVE overdrew (the gauge hit the floor with a qualifying
    /// hit); false when the FIVE BLOWS ran out and the captain was simply mauled. Drives the freeze-frame
    /// caption — see <see cref="DeathNarration.SurfaceCaption"/>.</param>
    /// <param name="known">#564 · A cause the caller already KNOWS, passed instead of rolled. The roll
    /// below only knows the ground's two answers (the Old Ones took you, or you joined them), so anything
    /// that kills a captain for another reason — running the tank dry — would have been narrated as a
    /// Reever's hand. That is the same sim-says-one-thing-sentence-says-another failure #545 fixed for the
    /// black-ops sweep, and the fix is the same: the caller passes what it knows.
    ///
    /// <para>#633 · Both branches invented this parameter independently, for the same reason, one day apart
    /// — <c>known</c> here and <c>forcedCause</c> on <c>main</c> (#538's sweep team). One name survives, and
    /// it is this one: nothing is being FORCED, the caller simply is not guessing.</para></param>
    private void TriggerSurfaceOverdrawDeath(
        SurfaceExcursion dying, bool nerveRanOut, DeathCause? known = null)
    {
        if (_busted is not null)
        {
            return; // already mid-reckoning — one death at a time
        }

        Warp = 1;
        _effectiveWarp = 1;

        string body = dying.Stop.Body.Name;
        ulong seed = DiceRule.Seed("overdraw", (long)SimTime);
        // #538 · WHO ACTUALLY KILLED YOU. SurfaceEnd only ever answers "the pack, or the rare Joined at a
        // sliver" — which was fine while the pack was the only thing aboard that could end a captain. The
        // first playtest of the sweep team ended with three men with rifles shooting a captain in a corridor
        // and a card that said "the Old Ones took you… ran you down on her regolith short of the tube". A
        // caller that KNOWS the cause now says so, and the roll is only consulted when nobody does.
        DeathCause cause = known ?? DeathNarration.SurfaceEnd(_nerve, seed); // Reevers, or the rare Joined at a sliver

        // #574 · A death away from her deck can never be a COLLECTOR — a collector is a person who came for
        // you, and there is nobody aboard a dead hull or out on an empty moon. Owner: "the debt collector
        // deaths should also only happen in those situations never in any other". Coerced rather than
        // trusted, because this method has four callers and will have more.
        // #609 · AND UNDER A MOON IS ITS OWN PLACE. Owner, having suffocated on B2 and been handed the
        // surface card: "now we have the suffocated on surface one :-D"
        //
        // He was 150 m down in a poured corridor being told about regolith, a suit and the long walk back to
        // the tube. Nothing here knew "underground" existed, so every death in the Hive fell through to the
        // away team's — the sim knowing one thing and the sentence reporting another, which is the named bug
        // class on this ground and the third card it has cost.
        //
        // The floor is the fact, and it is right here on the excursion. It just was not being asked.
        DeathPlace place = dying.Floor < 0
            ? DeathPlace.Underground
            : Derelict.TryParseWreckId(dying.Stop.Body.Id, out _)
                ? DeathPlace.Derelict
                : DeathPlace.LandingParty;
        if (!DeathNarration.CanHappen(cause, place))
        {
            cause = DeathCause.Reevers;
        }

        _busted = new BustedEncounter
        {
            HunterId = string.Empty,     // no collector — the ground and the mind collected
            HunterCallsign = body,
            Heat = 0,
            Seed = seed,
            Bribe = default,             // unused on a surface death (no bribe to your own nerves)
            Phase = BustedEncounter.Stage.SurfaceEnd,
            Cause = cause,
            // #574: a salvage run and an away team are not the same death. Derelict ids parse; a moon does
            // not — so the excursion itself says which world's words the card should use.
            Place = place,
            NerveRanOut = nerveRanOut,
            DeathBodyName = body,
        };

        RendererInterop.PlayCue("alarm");
        RendererInterop.PlayCue("gameover");
        string line = cause switch
        {
            DeathCause.Suffocated => DeathNarration.SuffocationHeadline(body),
            DeathCause.Joined =>
                $"🧠 Nerves gone past empty on {body} — the captain turns, and walks TOWARD the crowd. The insurance will need a new name.",
            // #525 · The one they chose. The pulse must not say an Old One's hand was the last straw over a
            // captain who turned both keys themselves ninety seconds ago.
            DeathCause.Scuttled =>
                $"☢ The overload ran out with the captain still aboard the {body}. She goes all at once and mostly inward. The insurance will need a new name.",
            // Nothing about this one is about nerve, so it must not narrate as if it were.
            DeathCause.Inspected =>
                $"🕶 Found aboard {body}, told to stand still, and not standing still. The sweep goes on down the corridor. The insurance will need a new name.",
            _ =>
                $"🧠 Nerves shot past empty on {body} — an Old One's hand is the last straw. The captain breaks. The insurance will need a new name.",
        };
        LogAutopilotEvent(line);
        ShowPulseMessage(line);
        _shipAlerts.Raise(AlertKind.Collision, AlertSeverity.Red, $"CAPTAIN LOST — {body}", SimTime);
        SquawkNow(Parrot.Squawk.Impact, _lastTimestampMs ?? 0, body, force: true);
        StateHasChanged();
    }

    /// <summary>
    /// #621 dev cheat · <c>/map?death=&lt;cause&gt;</c> — land first (if the URL asked to), THEN die.
    ///
    /// <para><see cref="AutoLandForCheatAsync"/> is fire-and-forget with several early returns, so the death
    /// cannot simply be queued after it in the boot block: it would fire while the shuttle was still coming
    /// down and the excursion's floor and body id — the two facts the place classifier reads — would not
    /// exist yet. Awaited here instead, in the one place that knows the landing is over.</para>
    /// </summary>
    private async Task AutoLandThenStageDeathAsync(DeathCause? cause)
    {
        await AutoLandForCheatAsync();
        if (cause is { } asked)
        {
            StageDeathCheat(asked);
        }
    }

    /// <summary>
    /// #621 dev cheat · KILL THE CAPTAIN NOW, through the real machinery.
    ///
    /// <para>Nothing here builds a card. It calls the same three triggers the game calls
    /// (<see cref="TriggerSurfaceOverdrawDeath"/>, <see cref="TriggerImpact"/>, <see cref="ApplyHunterCatch"/>)
    /// with the same arguments the sim would have handed them, so every downstream fact — the seeded line,
    /// the place classification, the art, the tail, the succession, the clinic bill, the rebirth glitch — is
    /// computed exactly as it is in play. A cheat that painted its own death card would prove that the cheat
    /// works and nothing else, which is the failure this project has named: a green test that asserts
    /// nothing, dressed as a quick start.</para>
    ///
    /// <para>Two arguments are read from the LIVE state rather than invented, for the same reason:
    /// <c>nerveRanOut</c> comes from <see cref="CaptainSuccession.OverdrawQualifies"/> on the real gauge
    /// (so a full-nerve captain gets the mauled caption and a shattered one gets the overdraw caption,
    /// truthfully), and the place is never passed at all — the excursion decides it.</para>
    /// </summary>
    private void StageDeathCheat(DeathCause cause)
    {
        string asked = cause.ToString().ToLowerInvariant();
        if (_busted is not null)
        {
            return; // already mid-reckoning — one death at a time, the same rule the triggers keep
        }

        // AWAY FROM HER DECK. One call, and the excursion answers WHERE by itself.
        if (_surface is { } ex)
        {
            TriggerSurfaceOverdrawDeath(
                ex, nerveRanOut: CaptainSuccession.OverdrawQualifies(_nerve), known: cause);

            // Say so when the law refused the cause — read back off the staged encounter rather than
            // re-deriving it, so the message can never disagree with the card behind it.
            if (_busted is { } staged && staged.Cause != cause)
            {
                ShowPulseMessage(
                    $"🧪 DEV ?death={asked}: a {asked} death cannot happen on a {staged.Place} "
                    + $"(DeathNarration.CanHappen) — the law substituted {staged.Cause}.");
            }
            return;
        }

        switch (cause)
        {
            case DeathCause.Impact:
                // The surface collected the ship. The crossing is synthesised at the ship's own position on
                // the nearest body, which is what SurfaceImpact would have handed over one tick later.
                UpdateNearestBody();
                if (_nearestBody is not { } rock)
                {
                    ShowPulseMessage("🧪 DEV ?death=impact: no body charted yet to fly into.");
                    return;
                }
                TriggerImpact(new SurfaceImpact.Crossing(rock.Id, rock.Name, 1.0, SimTime, _ship.Position));
                return;

            case DeathCause.Collector:
                // Not the freeze-frame — the CATCH, which is where a collector death actually begins. You
                // get the demand card and have to lose your way through SUBMIT / BRIBE / RESIST → THE
                // BOLIVIA to reach it, because that ladder is the thing worth playing and reading.
                _heat = EncounterRule.RaiseHeat(_heat, 2, SimTime);
                SpawnHunterForHeatEvent();
                if (_hunters.Count == 0)
                {
                    ShowPulseMessage(
                        "🧪 DEV ?death=collector: nothing policed within reach of this berth to send muscle "
                        + "from — try &dock=selene-gate or &dock=ringside.");
                    return;
                }
                ApplyHunterCatch(_hunters[^1]);
                return;

            case DeathCause.Void:
                // #638 · IT HAS A LANE NOW. This arm used to fall into the `default` below, whose honest
                // refusal — that this cause had no lane at all — is the sentence the whole issue was filed
                // about. The door skips the twenty days and takes the real ending: the same call the watch
                // makes when the clock runs out, so what a tester sees is what a captain sees.
                TheVoidTakesHer();
                return;

            default:
                // Reevers / Joined / Suffocated need somebody out of the ship.
                ShowPulseMessage(
                    $"🧪 DEV ?death={asked}: not a death that happens on her deck. Add &land=1 (the ground), "
                    + "&wreck=1&land=1 (a derelict) or &secretlab=1&land=1&floor=2 (the Hive).");
                return;
        }
    }
}
