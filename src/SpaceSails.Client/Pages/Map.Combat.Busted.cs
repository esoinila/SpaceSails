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
public partial class Map
{

    // PR-BUSTED (owner ruling §5): the catch is no longer an instant tax that leaves the collector
    // inert — it OPENS the boarding pop-up. Warp yanks to 1×, the ship is grappled, and the collector
    // hails with a demand and three options (SUBMIT / BRIBE / RESIST). The seed is folded from the
    // hunter's identity and the sim moment, so every roll in this encounter is reproducible.
    private void ApplyHunterCatch(HunterState hunter)
    {
        Warp = 1;
        _effectiveWarp = 1;
        RendererInterop.PlayCue("board");

        ulong seed = DiceRule.Seed("busted", HunterSeqOf(hunter.Id), (long)SimTime);
        _busted = new BustedEncounter
        {
            HunterId = hunter.Id,
            HunterCallsign = hunter.Callsign,
            Heat = Math.Max(1, _heat.Level),
            Seed = seed,
            Bribe = BustedRule.BribeDemand(Math.Max(1, _heat.Level), seed),
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

        SquawkNow(Parrot.Squawk.Busted, _lastTimestampMs ?? 0, BustedRule.ExposurePhrase(Math.Max(1, _heat.Level)), force: true);
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

    // The dice helpers a resist/Bolivia roll carries — the purchasable-modifier seam. One example is
    // shipped (the Boarding-nets jammer); the shop of helpers is a follow-up (owner §5.0).
    private List<DiceModifier> ResistModifiers()
    {
        var mods = new List<DiceModifier>();
        if (_hasNetJammer)
        {
            mods.Add(new DiceModifier("Boarding-nets jammer", +2));
        }

        return mods;
    }

    private static long HunterSeqOf(string hunterId) =>
        int.TryParse(hunterId.AsSpan(hunterId.LastIndexOf('-') + 1), out int n) ? n : 0;

    // ---- The three options ----

    // SUBMIT: confiscate all hot cargo + a heat share of the purse (with the minimum-take fallback and
    // the mercy floors), then clear heat to 0 — the debt is collected.
    private void BustedSubmit(bool harsher = false)
    {
        if (_busted is not { } b)
        {
            return;
        }

        BustedRule.Confiscation c = BustedRule.Confiscate(
            b.Heat, _credits, _hotCargo.BuildLots(_cargoByClass), b.Seed, harsher);
        ApplyConfiscation(c);
        _heat = EncounterRule.RaiseHeat(HeatState.None, 0, SimTime); // clears to 0 — debt collected
        _lastAnnouncedHeat = 0;
        _hotCargo.Launder();
        b.Confiscation = c;
        b.Phase = BustedEncounter.Stage.Confiscated;
        StateHasChanged();
    }

    private void ApplyConfiscation(BustedRule.Confiscation c)
    {
        _credits = c.CoinLeft;
        foreach (BustedRule.CargoSeizure s in c.Seizures)
        {
            int have = _cargoByClass.GetValueOrDefault(s.CargoClass);
            int taken = Math.Min(have, s.Units);
            if (taken <= 0)
            {
                continue;
            }

            int left = have - taken;
            if (left > 0)
            {
                _cargoByClass[s.CargoClass] = left;
            }
            else
            {
                _cargoByClass.Remove(s.CargoClass);
                _hotCargo.Forget(s.CargoClass);
            }

            _cargoUnits -= taken;
            _cargoValue -= taken * CargoMarket.UnitValue(s.CargoClass);
        }

        _cargoUnits = Math.Max(0, _cargoUnits);
        _cargoValue = Math.Max(0, _cargoValue);
        RendererInterop.PlayCue("board");
        RequestVaultSave(); // #225: a boarding resolution changed purse, cargo and heat
    }

    // BRIBE: pay the dice-rolled fee to keep the cargo; the hunter breaks off, heat is UNCHANGED (you
    // bought this patrol, not the law). Can't afford → the button is greyed with the number.
    private void BustedBribe()
    {
        if (_busted is not { } b || _credits < b.Bribe.Total)
        {
            return;
        }

        _credits -= b.Bribe.Total;
        RemoveHunter(b.HunterId);
        b.ResultMessage = $"{b.Bribe.Total:N0} cr changes hands. {b.HunterCallsign} logs a clean sweep and sheers off — heat unchanged. You bought this patrol, not the law.";
        b.Phase = BustedEncounter.Stage.BribedOff;
        SquawkNow(Parrot.Squawk.HunterBacksOff, _lastTimestampMs ?? 0, force: true);
        StateHasChanged();
    }

    /// <summary>
    /// #535 · <b>PRESENT THE KEY — the fifth exit, and the only one that reaches backwards.</b>
    ///
    /// <para>Every other answer on this card leaves a mark. Submit and the hold goes; bribe and the coin
    /// goes; resist and heat climbs whichever way the dice fall. This one costs the key and nothing else,
    /// and what the captain buys is <b>an encounter that never happened</b>: no heat gained, no entry on the
    /// wire, nobody who remembers, and the pursuer simply gone.</para>
    ///
    /// <h4>The scrub is written as four things this method does NOT do</h4>
    /// <list type="bullet">
    /// <item><b>No heat.</b> <c>_heat</c> is not touched at all — not raised, and not cleared either. A catch
    /// that never happened cannot clear a debt any more than it can add to one, so submitting stays the only
    /// way heat goes to zero.</item>
    /// <item><b>No wire entry, and it never forms.</b> There is no <c>PushNewsEvent</c> here — not
    /// <c>HunterBrokeOff</c>, which is the headline this seam would otherwise reach for, because breaking a
    /// pursuer off is exactly what just happened. The scrub is not a deletion after the fact: nothing is ever
    /// pushed, so there is nothing on the wire to find, and <c>TheKeyLeavesNoTraceTests</c> reads the pushed
    /// list rather than the rendered ticker.</item>
    /// <item><b>Nobody remembers.</b> No contact row, no goodwill, no hostility — <c>_contacts</c> is not
    /// opened. A memory that never forms is the whole of what the issue calls the treasure.</item>
    /// <item><b>Nothing is said.</b> <see cref="BlackOpsKey.NoContactLoggedPlate"/> IS the telling (#761's
    /// law, met by the plate this stage renders), and there is no result message under it. A sentence
    /// narrating the silence would be the game filing a report about a report it did not file.</item>
    /// </list>
    ///
    /// <para>The pursuer leaves through <see cref="RemoveHunter"/>, which is the deterrent's own break-off
    /// (#522/#1090) and the same call the bribe makes — a hand-written removal beside it would be this repo's
    /// first named bug class aimed at a state transition.</para>
    /// </summary>
    private void PresentTheBlackOpsKey()
    {
        if (_busted is not { Phase: BustedEncounter.Stage.Demand } b
            || BlackOpsKey.InThePocket(_satchel) is not { } key)
        {
            return;
        }

        _satchel = [.. BlackOpsKey.Spend(_satchel, key)];
        RemoveHunter(b.HunterId);
        b.Phase = BustedEncounter.Stage.NoContactLogged;
        RequestVaultSave();   // #225: the pocket changed, and it is the only thing that did
        StateHasChanged();
    }

    // RESIST: heat 1–2 → one opposed dice check (lose = SUBMIT + harsher cut; win = hunter broken off,
    // heat +1). Heat 3 → the full Bolivia.
    private void BustedResist()
    {
        if (_busted is not { } b)
        {
            return;
        }

        if (b.Heat >= EncounterRule.MaxHeatLevel)
        {
            b.Phase = BustedEncounter.Stage.Bolivia;
            b.BoliviaInitiative = BoliviaEncounter.RollInitiative(b.Seed, b.Heat, ResistModifiers());
            b.BoliviaNet = b.BoliviaInitiative.Value.Margin;
            b.BoliviaBeatIndex = 0;
            SquawkNow(Parrot.Squawk.FiringSolution, _lastTimestampMs ?? 0, force: true);
            StateHasChanged();
            return;
        }

        OpposedRoll roll = BustedRule.ResistCheck(b.Heat, b.Seed, ResistModifiers());
        b.ResistRoll = roll;
        if (roll.ChallengerWins)
        {
            RemoveHunter(b.HunterId);
            _heat = EncounterRule.RaiseHeat(_heat, 1, SimTime); // a win pins the wolves meaner
            b.ResultMessage = $"You break {b.HunterCallsign}'s boarding — they peel off nursing the dent. Heat climbs; the next wave is meaner.";
            b.Phase = BustedEncounter.Stage.ResistWon;
            SquawkNow(Parrot.Squawk.HunterBacksOff, _lastTimestampMs ?? 0, force: true);
        }
        else
        {
            b.Phase = BustedEncounter.Stage.ResistLost;
        }

        StateHasChanged();
    }

    // #735 · Stage-guarded for the same reason the wake is (see BustedResurrect): once Enter presses this
    // button too, a keystroke on a focused button can arrive as BOTH a key handler and the browser's own
    // click — and a confiscation applied twice is the collector taking the cut twice off one press.
    private void BustedResistLostConfirm()
    {
        if (_busted is not { Phase: BustedEncounter.Stage.ResistLost })
        {
            return;
        }

        BustedSubmit(harsher: true);
    }

    // A Bolivia beat: fold the choice, roll it, advance. After the last beat, tally and decide.
    private void BustedBoliviaChoose(string choiceId)
    {
        if (_busted is not { } b || b.Phase != BustedEncounter.Stage.Bolivia)
        {
            return;
        }

        EncounterBeat beat = BoliviaEncounter.Script[b.BoliviaBeatIndex];
        EncounterChoice choice = beat.Choices[0];
        foreach (EncounterChoice c in beat.Choices)
        {
            if (c.Id == choiceId) { choice = c; break; }
        }

        OpposedRoll roll = BoliviaEncounter.RollBeat(b.Seed, b.BoliviaBeatIndex, choice, b.Heat, ResistModifiers());
        b.BoliviaNet += roll.Margin;
        b.BoliviaOutcomes.Add(new BoliviaEncounter.BeatOutcome(b.BoliviaBeatIndex, choice.Id, roll));
        b.BoliviaBeatIndex++;

        if (b.BoliviaBeatIndex >= BoliviaEncounter.Script.Count)
        {
            b.BoliviaEnding = BoliviaEncounter.Decide(b.BoliviaNet);
            if (b.BoliviaEnding == BoliviaEncounter.Ending.Flee)
            {
                RemoveHunter(b.HunterId); // left tied up at their own ship
                _heat = new HeatState(BoliviaEncounter.FleeHeat, SimTime);
                _lastAnnouncedHeat = BoliviaEncounter.FleeHeat;
                b.ResultMessage = $"You fight clear — {b.HunterCallsign} left tied up at their own ship. You slip away carrying heat {BoliviaEncounter.FleeHeat}.";
                b.Phase = BustedEncounter.Stage.Fled;
                SquawkNow(Parrot.Squawk.HunterBacksOff, _lastTimestampMs ?? 0, force: true);
            }
            else
            {
                b.Phase = BustedEncounter.Stage.FreezeFrame;
                RendererInterop.PlayCue("board");        // volley hook (audio cue is a follow-up)
                RendererInterop.PlayCue("gameover");     // game-over-music hook
            }
        }

        StateHasChanged();
    }

    // THE FREEZE-FRAME → brain-backup resurrection: wake at the nearest haven's clinic in an insurance
    // rustbucket. Everything VISIBLE aboard is gone; the tank comes up FULL — the same fuel a new voyage
    // starts with (#477), so the wake puts you back in the game instead of into a fuel hunt. Buried/banked
    // survives (it lives off-ship — other lanes). Never a dead save.
    private void BustedResurrect()
    {
        // #735 · …and only OUT OF THE FREEZE. The guard used to be "is there a death open at all", which was
        // true right up until Enter joined the mouse on this button: a keystroke on a focused button fires
        // the keydown handler AND the browser's own click, so the wake could be asked for twice — and a
        // second wake is a second clinic bill, a second succession and a second captain, off one press. The
        // stage is the fact that decides, so the stage is what is asked. Every road in is one of these three.
        if (_busted is not { } b
            || b.Phase is not (BustedEncounter.Stage.FreezeFrame
                or BustedEncounter.Stage.Impact
                or BustedEncounter.Stage.SurfaceEnd))
        {
            return;
        }

        // #422 arc 2 — how many times this THREAD has woken before (durable), read BEFORE the succession adds
        // this death's retiree, so it counts prior lives only. Gates the clinic's second page below.
        int priorDeaths = ActiveThreadInfo?.Retired.Count ?? 0;

        RemoveHunter(b.HunterId);

        // Evening wind #20 — a surface-overdraw death happens mid-excursion, so the away gig ends here as a
        // failed/aborted trip (no payout) and the surface is folded away, before the resurrection flies the
        // brain-backup to the nearest clinic. The buried/banked hoards live off-ship and are untouched.
        bool wasOnSurface = _surface is not null;
        if (wasOnSurface)
        {
            _surface = null;
            _reevers.Clear();
            _lastNearestReeverRange = null;
            LogAutopilotEvent("🛬 The expedition is written off — the away team scrubs the gig and the body path goes to backup.");
        }

        int wakeTank = WakeTankPulses();

        // Consult the insurance policy at rebirth (issue #227 seam): None-tier returns the uninsured
        // rustbucket + full clinic bill untouched; a covered tier would ease it — the pure rule decides.
        RebirthOutcome outcome = InsuranceRule.ApplyToRebirth(
            _insurance, SimTime, InsuranceRule.DefaultRebirth(wakeTank));
        BustedRule.ResurrectionKit kit = outcome.Kit;

        // The rebirth tax (issue #227): the clinic bill is booked against the stake, floored at 0 — a
        // shortfall is a debt the WIRE lane's bank will reconcile against banked/buried funds at merge.
        int afterBill = Math.Max(0, kit.Credits - outcome.ClinicBillCr);
        LogAutopilotEvent($"🏥 Clinic bill: −{outcome.ClinicBillCr:N0} cr (brain-backup wake-up). Insurance: {_insurance.Tier}.");

        _credits = afterBill;
        _cargoUnits = 0;
        _cargoValue = 0;
        _cargoByClass.Clear();
        _hotCargo.Launder();
        _reactionMassPulses = Math.Min(kit.ReactionMassPulses, ReactionMassCapacityFor(kit.MassLevel));
        _slugAmmo = kit.SlugAmmo;
        _missileAmmo = kit.MissileAmmo;
        _massLevel = kit.MassLevel;
        _sensorLevel = kit.SensorLevel;
        _holdLevel = kit.HoldLevel;
        _telescopeLevel = kit.TelescopeLevel;
        _hasNetJammer = false;

        // A NEW CAPTAIN GETS THEIR OWN NERVE. Nothing reset this before, so the brain-backup woke a fresh
        // captain already carrying the dead one's shattered gauge — and since the commonest death IS the
        // nerve running out, the replacement routinely woke at or near the floor, one fright from dying the
        // same way. Found while wiring #480, which makes it plainly visible: without this the LEDGER would
        // hand the new captain someone else's last bad minute too. The beat clocks, the banked sub-pip
        // pressure and the ledger all go with them.
        // …but the DEAD captain's ledger is the one thing worth keeping across the seam: the card is about
        // to ask "what broke you?", and the answer is in the last few pips they spent. Snapshot it here,
        // THEN wipe the live one — the first cut cleared it before the card ever rendered, so the block
        // silently never appeared (caught in the browser, not by a test).
        _deathNerveLedger = _nerveLedger;
        _nerve = NerveModel.Steady;
        _nerveBeats = NervePips.Beats.Fresh;
        _nerveShockCarry = 0;
        _nerveLedger = [];
        _nerveFlash = null;

        RebuildSensor();
        _heat = HeatState.None;
        _lastAnnouncedHeat = 0;
        b.ClinicBillCr = outcome.ClinicBillCr;
        b.StakeCr = kit.Credits;   // #621: the receipt reads the kit that paid, not a constant beside it
        b.HullDescription = outcome.HullDescription;

        // Wake at the nearest haven's clinic: reset the ship state onto that haven, riding its rails.
        string clinicName = WakeAtNearestHaven();
        b.ClinicName = clinicName;

        // A surface death folded the excursion; fold the surface DECK away too, back to the bare ship deck
        // in flight (the clinic haven the wake placed us at), so the map view resumes under the modal.
        if (wasOnSurface)
        {
            SetDeckForDock(null);
        }

        // Evening wind #20 — THE NEW CAPTAIN, on ANY death-resurrection (this overdraw AND the collector /
        // Bolivia / impact paths): the piracy insurance rolls the thread's identity — a fresh seeded name and
        // a differing face — and the roster keeps the retiree. The corner chip and roster re-read the change.
        IssueSuccessorCaptain(b);

        // #422 arc 2 — THE GLITCH IN THE REBIRTH. You experience the Nebula thread by DYING: for one flat
        // second the wake card reads a line it should not (a seeded flash off this death), and reading it
        // assembles the shard. The oracle (#427) may already have leaked it; this is the first-hand vector,
        // delivered ON the card. The flash shows every rebirth; the shard is gathered once.
        b.RebirthGlitch = NebulaGlitchFlash(b.Seed);
        AssembleNebulaSilently("rebirth-glitch");

        // #422 — THE CLINIC'S SECOND PAGE. Surfaces only once you've woken here BEFORE (a prior death this
        // thread or this session): the fragment's own fiction — a policy number "older than you", carrying
        // entries you never made. Shown on the card the once it is first gathered.
        if ((priorDeaths >= 1 || _rebirthsSeen >= 1) && AssembleNebulaSilently("clinic-ledger"))
        {
            b.ClinicLedger = NebulaLore.ById("clinic-ledger")!.Lore;
        }

        _rebirthsSeen++;

        b.Phase = BustedEncounter.Stage.Resurrected;
        StateHasChanged();
    }

    // #422 arc 2 — the one-flat-second glitch the resurrection card flashes before the welcome loops clean.
    // Seeded off the death so it varies per rebirth yet is deterministic; the "DO NOT REVIVE ORIGINAL" tell
    // is the shard's heart. Presentation only — the canonical shard lore lives in NebulaLore.
    private static readonly string[] NebulaGlitchFlashes =
    [
        "RESTORE FROM PATTERN 40 · SUBSCRIBER LUCID · DO NOT REVIVE ORIGINAL",
        "PATTERN 40 · COPY SPUN · ORIGINAL RETAINED · DO NOT WAKE ORIGINAL",
        "SUBSCRIBER RE-INSTANCED · SEE ARCHIVE · DO NOT REVIVE ORIGINAL",
        "CONTINUITY: BELIEVED · ORIGINAL: FILED · DO NOT DISTURB THE DARK",
    ];

    private static string NebulaGlitchFlash(ulong seed) =>
        NebulaGlitchFlashes[(int)(seed % (ulong)NebulaGlitchFlashes.Length)];

    // Issue the successor captain onto the active universe (Evening wind #20). Reads the retiring identity,
    // rolls + persists the new one through the registry (the #368 fields are editable data), and refreshes
    // the cached roster so the in-play chip and the saved-voyages list show the new face at once. A run with
    // no indexed thread (a legacy save) simply keeps its current identity and the card narrates generically.
    private void IssueSuccessorCaptain(BustedEncounter b)
    {
        if (string.IsNullOrEmpty(_activeThreadId))
        {
            return;
        }

        if (Threads.Get(_activeThreadId) is { } before)
        {
            b.RetiredCaptainName = SpaceSails.Core.Captains.For(before).Name;
        }

        int simDay = (int)(SimTime / 86400);
        if (Threads.IssueSuccessor(_activeThreadId, simDay) is { } after)
        {
            b.NewCaptainName = after.CaptainName;
            b.NewCaptainAvatar = after.AvatarIndex;
            RefreshThreadList(); // the chip (ActiveThreadInfo) + the roster now read the new identity

            // #973 L1 · THE FILING LINE. The successor wakes remembering the ledger only as far as the last
            // premium reached; every page dated after that is one they don't remember writing. Read AFTER the
            // succession is on the thread, because the roll a grey page is later read on is salted with the
            // LIFE, and this is the moment the life number changes. The policy consulted is `_insurance` at
            // `SimTime` — the same two the clinic bill above was computed from, so the receipt and the amnesia
            // can never disagree about whether anybody was covered.
            b.FilingNotice = MarkTheBookAtTheFilingLine();

            // #973 L5a · …and the same moment empties the list of people this captain has explained his face
            // to. Beside the filing line rather than anywhere else because it is the same fact about the same
            // moment: the one who walks out of the clinic is not the one who answered them.
            ANewFaceHasNothingExplained();
        }
    }

    // The tank a rebirth wakes with: a FULL base tank — the same fuel a new voyage starts with
    // (Map.Sim's _reactionMassPulses seed and ReactionMassCapacityFor(0) agree on the number).
    //
    // #477: this used to be a "mercy floor" priced from FuelReachability, falling back to the flat
    // autopilot reserve — AutopilotRehearsal.ReservePulses(500) = 90 p. But the autopilot refuses any
    // journey that would eat into its reserve, so a tank set TO the reserve is a tank with no usable
    // fare: the new captain woke at Uranus with 90 p, 0 cr and an empty hold, and could not fly, buy
    // or sell. Owner's ruling — give the rebirth what a new game gives. Death costs the hull, the
    // purse, the hold and every upgrade; it does not also cost you the ability to leave.
    private int WakeTankPulses() => ReactionMassCapacityFor(0);

    private int ReactionMassCapacityFor(int massLevel) => 500 + 150 * massLevel;

    // Set the ship down at the nearest haven (the clinic), riding its velocity — a starter-grade parked
    // state. Returns the haven's name for the wake card.
    private string WakeAtNearestHaven()
    {
        if (_ephemeris is null)
        {
            return "a frontier clinic";
        }

        CelestialBody? nearest = null;
        double best = double.MaxValue;
        foreach (CelestialBody body in _ephemeris.Bodies)
        {
            if (!body.IsHaven)
            {
                continue;
            }

            double d = (_ephemeris.Position(body.Id, SimTime) - _ship.Position).Length;
            if (d < best) { best = d; nearest = body; }
        }

        if (nearest is null)
        {
            return "a frontier clinic";
        }

        Vector2d pos = _ephemeris.Position(nearest.Id, SimTime);

        // #478 · WAKE AT A BERTH, NOT INSIDE THE STATION. This used to be
        //     _ship = new ShipState(pos, vel, SimTime);
        // which parked the new captain at the haven's EXACT CENTRE — distance zero. Every projected sample
        // then sat inside the haven's body radius, ClosestApproach reported a permanent subsurface pass, and
        // the alarm channel shouted "ROCKS AHEAD! — impact with The Tilt" at a ship reading "clamped on" at
        // 0.0 km/s rel. It was never a predictor bug: the ship really was inside the station.
        //
        // A dockable haven now rides the ONE TRUE CLAMP every real arrival uses (co-moving berth offset,
        // welded interior, pinned at dock), so the wake state is byte-for-byte an ordinary docking and the
        // collision projection sees the same honest berth separation it sees after any other arrival.
        if (DockableHavens.IsDockable(nearest))
        {
            ClampOntoHaven(nearest, pos);
        }
        else
        {
            // A haven with no berth to clamp to (a bare waypoint): still never inside it — sit off it at the
            // shared berth offset, co-moving, rather than at its centre.
            _ship = BerthState.CoMoving(_ephemeris, nearest.Id, SimTime, BerthState.BerthOffsetMeters);
        }

        ReprojectTrajectory();
        _camera.CenterOn(_ship.Position);
        return nearest.Name;
    }

    // #470: dismissed through the Dismiss() seam so the keyboard comes home. This is the last card of the
    // death→rebirth chain, and the new captain takes the helm the instant it drops — without the way back
    // they would sit at the controls with every key dead.
    private void CloseBusted()
    {
        _busted = null;
        StateHasChanged();
    }

    // Buy the one shipped dice helper: a Boarding-nets jammer, a named +2 on resist/Bolivia initiative
    // (the modifier seam; the shop is a follow-up). One-time purchase, lost on resurrection.
    private void BuyNetJammer()
    {
        if (_hasNetJammer || _credits < NetJammerPriceCr)
        {
            return;
        }

        _credits -= NetJammerPriceCr;
        _hasNetJammer = true;
        ShowPulseMessage("Boarding-nets jammer fitted — +2 on any last stand. The collectors hate it.");
        StateHasChanged();
    }

    // The open BUSTED pop-up's state — grappled, at 1×, the captain choosing. The dice are rolled Core-
    // side (BustedRule / BoliviaEncounter); this holds what's been rolled and which panel to show.
    public sealed class BustedEncounter
    {
        // Impact (#264): a body's surface collected the ship — no collector, no dice, straight to the
        // freeze-frame → clinic re-birth. Reuses this whole encounter so the death machinery is shared.
        // SurfaceEnd (Evening wind #20): a nerve-overdraw death out on the regolith — no collector, no dice.
        // Its own freeze-beat (the cause art + the place-dependent line) before the shared resurrection.
        // #535 · NoContactLogged is the fifth exit: a key was presented and the encounter never happened.
        // Appended, like every stage that has joined this list before it — the markup switches on these arms
        // and several guards name one by hand, so a stage slipped into the middle would re-page all of them.
        public enum Stage { Demand, Confiscated, BribedOff, ResistWon, ResistLost, Bolivia, Fled, FreezeFrame, Resurrected, Impact, SurfaceEnd, NoContactLogged }

        public required string HunterId { get; init; }
        public required string HunterCallsign { get; init; }
        public required int Heat { get; init; }
        public required ulong Seed { get; init; }
        public required DiceRoll Bribe { get; init; }
        public Stage Phase { get; set; } = Stage.Demand;
        public OpposedRoll? ResistRoll { get; set; }
        public BustedRule.Confiscation? Confiscation { get; set; }
        public string? ResultMessage { get; set; }
        public string? ClinicName { get; set; }
        public int ClinicBillCr { get; set; }

        /// <summary>#621 · What the policy actually PAID — read off the rebirth outcome's own kit, not
        /// re-quoted from <see cref="BustedRule.InsuranceCredits"/> in the markup. The receipt was printing
        /// the uninsured constant while the purse was set from the kit; the day a policy tier hands a
        /// different stake the card would have gone on stating the old number with complete confidence, and
        /// two places computing one fact is the bug even while they agree.</summary>
        public int StakeCr { get; set; }
        public string? HullDescription { get; set; }
        public string? ImpactBodyName { get; set; } // #264: the body that collected the ship

        // #422 arc 2 (NEBULA MUTUAL) — the fragments this death surfaces, delivered ON this card. The rebirth
        // glitch flashes on every wake (assembled once); the collector writ is glimpsed at a collector catch;
        // the clinic's second page surfaces on a LATER death. Blank when the beat doesn't apply this time.
        public string? RebirthGlitch { get; set; } // the one-flat-second wake-card glitch flash
        public string? CollectorWrit { get; set; } // the writ glimpsed off a collector at the demand
        public string? ClinicLedger { get; set; }  // the clinic ledger's second page (woken here before)

        // #380 item 1: WHAT killed the captain, and WHERE — so the resurrection card explains the death
        // place-dependently (cause art + a seeded house-voice line) before the brain-backup copy. Defaults to
        // the collector (the BUSTED last stand); the impact path sets Impact. Surface causes are wired ready.
        public DeathCause Cause { get; set; } = DeathCause.Collector;

        /// <summary>#574 · WHERE it happened — her own deck, somebody else's hull, or a suit on a surface.
        /// The card reads the same cause differently depending on this, because a derelict has no regolith
        /// to be run down on and an away team is not standing on a deck.</summary>
        public DeathPlace Place { get; set; } = DeathPlace.OwnShip;
        // Which meter actually ran out (#480 follow-up): true = the nerve overdrew, false = the five blows
        // landed. Before #469 was fixed nerve was effectively the ONLY way to die out there, so the card
        // hardcoded the nerve line; now that the condition marker really decides, the caption must not
        // blame a steady captain's nerve for a mauling.
        public bool NerveRanOut { get; set; }

        public string? DeathBodyName { get; set; } // the place the death is narrated off (moon / body flown into)

        // Evening wind #20 — THE NEW CAPTAIN. On any death-resurrection the piracy insurance issues a fresh
        // name + face; these carry the hand-over for the resurrection card (blank when no thread to succeed —
        // a legacy run — so the card falls back to the plain brain-backup copy).
        /// <summary>#973 L1 · The one line the wake says about the FILING LINE — which pages of the ledger came
        /// back with this captain and which did not. Decided by the policy in force at the death
        /// (<c>FilingLine.WakeNotice</c>), never by counting greyed rows. Blank on a legacy run with no thread
        /// to succeed, exactly like the two names below it.</summary>
        public string? FilingNotice { get; set; }

        public string? RetiredCaptainName { get; set; } // the captain who just walked into the dark
        public string? NewCaptainName { get; set; }     // the name the policy put on the license
        public int NewCaptainAvatar { get; set; }       // the new face in the mirror (art/captain-N.jpg)

        // Bolivia progress
        public OpposedRoll? BoliviaInitiative { get; set; }
        public int BoliviaBeatIndex { get; set; }
        public int BoliviaNet { get; set; }
        public List<BoliviaEncounter.BeatOutcome> BoliviaOutcomes { get; } = [];
        public BoliviaEncounter.Ending? BoliviaEnding { get; set; }
    }
}
