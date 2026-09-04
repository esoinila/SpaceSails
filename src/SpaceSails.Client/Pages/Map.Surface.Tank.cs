using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Part of Map.Surface (#870 split; the header note lives in Map.Surface.cs) — the suit tank, where the air is coming from, the tube rearm, and the one rack law both buildings obey.
public partial class Map
{
    // ── #564 · THE TANK. ────────────────────────────────────────────────────────────────────────────────
    //
    // GroundLesson has told every new captain "The walk back is half the tank" since #440, about a resource
    // that did not exist. This is the resource.
    //
    // The rule it is built under: AIR MUST NEVER BE A SILENT TIMER THAT KILLS YOU. So there are three
    // things and not one — a readout that says how much FURTHER you may go (not merely how much is left), a
    // one-time line on the step where you cross the point of no return, and a death that says plainly what
    // happened. A countdown that quietly runs out is the same design failure as an invisible wall.
    private void StepSuitAir(double dtRealSeconds)
    {
        if (_surface is not { } ex)
        {
            return;
        }

        // #573 · INSIDE THE SHELTER, NOTHING IS SPENT. Owner, twice and unambiguously: "it should not be
        // possible to run out of air inside the emergency shelter" / "air should not be expended while in it
        // at all". Its sign has read PRESSURISED since the day it was built, and a suit standing in an
        // atmosphere is not drawing on its tank.
        //
        // Checked BEFORE the drain and returning outright, so there is no ordering by which a captain
        // sitting in a refuge can suffocate in it. The tank does not tick up either — the rack does that,
        // deliberately and with a ceiling; simply standing here is safety, not resupply.
        // #585 · UNDERGROUND, THE FLOOR DECIDES. Owner's biggest open question, answered with a beat in it:
        // B1 still holds pressure, so it is a refuge exactly like a shelter - the tank stops and the nerve
        // steadies. Everything below is dead, so depth is paid for in air and every stair down is a decision
        // about getting back up. Checked before the drain, like the shelter branch, so no ordering can
        // suffocate a captain standing in a pressurised corridor.
        //
        // #612 · AND THE WHOLE QUESTION IS NOW ASKED IN CORE. These were three conditions in a row here, and
        // the readout that reported them was a fourth condition somewhere else — which is the exact shape of
        // every expensive bug this project has filed: two places working the same answer out separately, and
        // one of them edited. SuitAir.SourceOf is the one predicate. The drain branches on ITS answer, the
        // gauge is handed the same answer, and the plate by the lift asks it the same way — so nothing on
        // screen can report a rule the sim is not running.
        //
        // The order inside SourceOf is this method's own order and must stay so: floor, then refuge, then
        // shelter, then ship.
        ShelterSpot inside = ShelterUnderfoot(ex);
        SuitAir.Supply supply = AirSupplyOf(ex);
        AnnounceAirSupply(supply, roomSpeaksForItself: inside.Found || RefugeUnderfoot(ex) >= 0);

        if (ex.Floor < 0)
        {
            // What the FLOOR provides on its own — the identical question HiveInterior's plate asks of the
            // same level, which is why the sign on the wall and the tank on your back cannot come apart.
            if (SuitAir.SourceOf(ex.Stop.Body.Id, ex.Floor, insideShelter: false, aboard: false)
                == SuitAir.Supply.Room)
            {
                ex.RefugeBreathNoted = false;
                return;
            }

            // ── #608 · THE REFUGE ON A DEAD FLOOR ────────────────────────────────────────────────────────
            //
            // Owner: "there should be like at least one air replenish station in each of the airless labs
            // underground... for pure safety" — and, the reason, "otherwise the elevator being busy could
            // kill employees".
            //
            // The SAME two things a shelter does, in the same order and by the same functions: the drain
            // stops because you are standing in an atmosphere, and the rack pumps on its own for as long as
            // you care to stand there. Checked BEFORE the drain and returning outright, exactly like the
            // shelter branch below, so there is no ordering by which a captain sitting in a refuge can
            // suffocate in it.
            //
            // What it does NOT do is make the floor free. It is one room, never beside the lift, and its
            // regulator stops at the same two thirds somebody set on the surface for the next person
            // through the door — so depth still costs air (#585), and the refuge buys RANGE.
            //
            // ── #608 · …AND ON MOST FLOORS IT BUYS LESS THAN THAT ───────────────────────────────────────
            //
            // The room is a fact and the SEAL is a story (StateOfTheRefugeOn). Three states and the branch
            // reads all three off the one Core answer, never off a second opinion:
            //
            //   HOLDING · what this always did: the drain stops and the rack pumps.
            //   EMPTY   · the drain stops and NOTHING pumps. Still exactly the thing the owner asked for —
            //             "otherwise the elevator being busy could kill employees" is answered by a room you
            //             can wait in — so it buys time to think and never a metre of range.
            //   FAILED  · nothing at all. The line is said, once, at the door, and then this falls straight
            //             through to the drain below: standing in a room whose seal went is standing on a
            //             dead floor, and the tank knows it even if the plan does not.
            int refuge = RefugeUnderfoot(ex);
            UndergroundComplex.RefugeState? seal = refuge >= 0
                ? UndergroundComplex.StateOfTheRefugeOn(ex.Stop.Body.Id, ex.Floor)
                : null;
            if (refuge >= 0)
            {
                bool holds = seal is { } s && UndergroundComplex.RefugeStillHolds(s);
                if (!ex.RefugeBreathNoted)
                {
                    ex.RefugeBreathNoted = true;
                    ShowPulseMessage(
                        UndergroundComplex.RefugeEntryLine(seal ?? UndergroundComplex.RefugeState.Failed));

                    // #573's idiom, and only where there is a rack to have been drawn on. On an empty or a
                    // failed one "somebody was here before you" would be a sentence about a reservoir that
                    // does not exist — the game telling a story off a number it is not running.
                    string found = seal == UndergroundComplex.RefugeState.Holding
                        ? SurfaceShelter.PartialLine(
                            RefugeReservoirNow(ex, refuge) / SurfaceShelter.ReservoirSeconds)
                        : "";
                    if (found.Length > 0)
                    {
                        // The same fact told by state rather than by a card, and down here it is a colder
                        // one: a rack in a sealed room a hundred and fifty metres under a moon has been
                        // drawn on, and the building has been shut for decades.
                        ShowAndFile(found, "🫁");
                    }
                }

                if (seal == UndergroundComplex.RefugeState.Holding)
                {
                    ex.RefugeReservoir[RefugeKey(ex.Floor, refuge)] = DrawFromRack(
                        ex, RefugeReservoirNow(ex, refuge), dtRealSeconds, out double intoTheTank);
                    if (intoTheTank > 0)
                    {
                        if (ex.RefugePumpNoted.Add(refuge))
                        {
                            ShowPulseMessage(SurfaceShelter.PumpingLine);
                        }
                    }
                    else if (ex.RefugePumpNoted.Contains(refuge) && ex.RefugePumpNoted.Add(-refuge - 1))
                    {
                        ShowPulseMessage(SurfaceShelter.PumpDoneLine);
                    }
                }

                if (holds)
                {
                    return;   // the room holds: the tank stops, with or without anything to fill it from
                }
            }
            else
            {
                ex.RefugeBreathNoted = false;
            }

            // Anywhere else on a dead floor drains exactly like open regolith: this is the price of going
            // deeper, and it is the only thing stopping the facility from being somewhere to live.
        }

        if (inside.Found)
        {
            if (!ex.ShelterBreathNoted)
            {
                ex.ShelterBreathNoted = true;
                ShowPulseMessage(SurfaceShelter.BreathingLine);
                string story = SurfaceShelter.PartialLine(
                    ShelterReservoirNow(ex, inside) / SurfaceShelter.ReservoirSeconds);
                if (story.Length > 0)
                {
                    // "Somebody was here" is a fact about the world told by state rather than by a card —
                    // exactly the kind of thing that was being lost eight seconds after it was earned.
                    ShowAndFile(story, "⛺");
                }
            }

            // #573 · THE RACK ALWAYS GIVES, and the PUMPING TIME is the cost. Owner: "it should always give
            // some more air... like a steady production rate... The time it takes to pump air is good
            // incentive to not take too much." It replaced a one-shot draw that could be SPENT, which had a
            // nasty failure he walked into: stranded beside an empty rack with nothing to do but die. A
            // cracker that always produces cannot strand anybody, and standing in a shed while the Old Ones
            // keep walking prices the top-up far better than an empty state ever did.
            //
            // #563 slice 3 · Keyed on the RACK, which is a tile and an index rather than an index. A bare
            // index was one site's list; a captain who crossed a tile boundary re-pointed every one of these
            // at a rack somewhere else, and the one they were standing in front of would have reported the
            // charge of the fourth shelter beside the tube.
            string rack = ShelterRackKey(inside);
            ex.ShelterReservoir[rack] = DrawFromRack(ex, ShelterReservoirNow(ex, inside),
                dtRealSeconds, out double pumped);
            if (pumped > 0)
            {
                if (ex.ShelterPumpNoted.Add(rack))
                {
                    ShowPulseMessage(SurfaceShelter.PumpingLine);
                }
            }
            else if (ex.ShelterPumpNoted.Contains(rack) && ex.ShelterPumpNoted.Add($"{rack}:done"))
            {
                ShowPulseMessage(SurfaceShelter.PumpDoneLine);
            }
            return;
        }
        ex.ShelterBreathNoted = false;

        // Inside the ship or in her tube you are breathing hers, and the tank tops up. This is the ONLY
        // place it refills (bar a cache found out in the world), which is what makes the tube the anchor
        // the whole supply line hangs from (#562).
        if (supply == SuitAir.Supply.Ship)
        {
            ex.AirSeconds = SuitAir.Refill(ex.AirSeconds, dtRealSeconds * TubeRefillRate);
            ex.AirWarned = false;   // re-arm the warnings: the next walk out gets told again
            ex.AirLowWarned = false;
            ex.ReserveNoted = false;
            return;
        }

        // #612 + #608 · THE DRAIN IS GATED ON THE SAME PREDICATE THE GAUGE READS.
        //
        // Every branch above has already returned for its own reason — it had a rack to run or a tank to top
        // up, which this cannot express. What it CAN do is make the two answers impossible to disagree: the
        // suit does not spend anything the hud has just told the captain it is not spending. Nothing reaches
        // here that the predicate calls not-drawing, so this line does nothing today; the day somebody adds a
        // fifth way to breathe and forgets one of the branches above, it is the difference between a wrong
        // colour and a death. It reads the VALUE the gauge was handed, not a fresh call — a second call is a
        // second chance to answer differently.
        if (!SuitAir.Drawing(supply))
        {
            return;
        }

        // #573 · BREATHING RATE. What you are doing, how frightened you are, and how hurt — the owner's
        // diving rule ("keep calm so the O2 does not run out"), which makes holding your nerve an actual
        // move rather than a mood.
        double moved = Math.Sqrt(((_avatarX - _airLastX) * (_avatarX - _airLastX))
            + ((_avatarY - _airLastY) * (_avatarY - _airLastY)));
        (_airLastX, _airLastY) = (_avatarX, _avatarY);

        double speed = dtRealSeconds > 0 ? moved / dtRealSeconds : 0;
        double exertion = speed < 0.5 ? SuitAir.Breathing.Still
            : speed > 7.0 ? SuitAir.Breathing.Running
            : SuitAir.Breathing.Walking;

        double rate = SuitAir.Breathing.Rate(exertion, _nerve, ex.HitsTaken, CaptainCondition.MaxHits);
        ex.AirSeconds = SuitAir.Drain(ex.AirSeconds, dtRealSeconds * rate);

        // Say it once when the breathing itself becomes the problem. Not a nag — a diagnosis, and a hint
        // that standing still is a move.
        if (!ex.HardBreathingNoted && rate >= SuitAir.Breathing.WorthMentioning)
        {
            ex.HardBreathingNoted = true;
            ShowPulseMessage(SuitAir.Breathing.HardBreathingLine);
        }
        else if (rate < SuitAir.Breathing.WorthMentioning * 0.8)
        {
            ex.HardBreathingNoted = false;   // re-arm once they have calmed down
        }

        double home = DistanceToTheTube();

        // #696 · Did an alarm go off on this tick? A captain must never suffocate inside a silent hold, and
        // a warning that plays while they are watching a progress bar fill is #564's forbidden silent timer
        // wearing a costume. Collected across the three thresholds and acted on once, below.
        bool alarmed = false;

        // THE LINE. Once, on the step it is crossed, while there is still a decision in it.
        if (!ex.AirWarned && SuitAir.PastPointOfNoReturn(ex.AirSeconds, home))
        {
            ex.AirWarned = true;
            alarmed = true;
            RendererInterop.PlayCue("alarm");
            ShowPulseMessage(SuitAir.CrossingWarning);
        }

        // #573 · THE SECONDARY PACK CUTS IN. The EMU's real half-hour reserve, and unlike everything else
        // here it is NOT distance-gated: the primary being gone is worth saying wherever you are standing.
        if (!ex.ReserveNoted && SuitAir.OnTheReserve(ex.AirSeconds))
        {
            ex.ReserveNoted = true;
            alarmed = true;
            RendererInterop.PlayCue("alarm");
            ShowPulseMessage(SuitAir.ReserveEngagedLine);
        }

        // #573 · AND the absolute low mark, which is the one that can actually fire in a field this size.
        // Without it a captain dies flat, having been warned about nothing — the silent timer the whole
        // mechanic forbids. It also raises the CARD, once per captain, because running out of air ends the
        // run and the owner is right that it deserves more than a toast that scrolls past.
        if (!ex.AirLowWarned && SuitAir.RunningLow(ex.AirSeconds, home))
        {
            ex.AirLowWarned = true;
            alarmed = true;
            RendererInterop.PlayCue("alarm");
            if (!ShowAirCardOnce())
            {
                ShowPulseMessage(SuitAir.LowAirWarning(ex.AirSeconds, home));
            }
        }

        // #696 · THE ALARM TAKES YOUR HANDS OFF THE PAPER. Note which way this dependency points: the suit
        // interrupts the darkroom, the darkroom knows nothing about the suit. Each threshold is one-shot per
        // walk, so this is a BEAT and never a lockout — the next press starts the same hold again, and a
        // captain who wants to finish reading a manifest on the reserve is allowed to make that decision.
        if (alarmed)
        {
            ProcessingIsInterrupted(Core.Processing.Interruption.Alarm);
        }

        if (ex.AirSeconds <= 0)
        {
            ShowPulseMessage(SuitAir.SuffocationLine);
            // The cause is PASSED, not rolled — see TriggerSurfaceOverdrawDeath. A suffocation narrated as
            // an Old One's hand would be the sim doing one thing and a sentence reporting another.
            TriggerSurfaceOverdrawDeath(ex, nerveRanOut: false, known: DeathCause.Suffocated);
        }
    }

    /// <summary>How far the captain is from the tube mouth — the way home, and the only distance the suit
    /// has any opinion about. A DISTANCE and never a coordinate, so a captain 400 du sideways and one 400 du
    /// deep are priced identically (#453: depth is not a danger gradient).</summary>
    private double DistanceToTheTube()
    {
        double dx = _avatarX - MoonSurface.SpawnX;
        double dy = _avatarY - MoonSurface.SpawnY;
        return Math.Sqrt((dx * dx) + (dy * dy));
    }

    // #573 · Last frame's position, for working out whether the captain is standing, walking or running.
    // Speed is not otherwise tracked on the surface, and the difference between a stroll and a sprint is the
    // whole of the owner's "keep calm" rule.
    private double _airLastX;
    private double _airLastY;

    // ── #612 · WHERE THE AIR IS COMING FROM. ────────────────────────────────────────────────────────────
    //
    // The last answer the predicate gave, kept ONLY so the crossing can be said once. It is deliberately not
    // what anything DISPLAYS — a cached copy of a fact is a second source of that fact, and this is the one
    // fact in the game that must not have two. Null before an excursion has ticked, which is also what stops
    // the crossing line firing on the frame a captain lands.
    private SuitAir.Supply? _airSupplyNoted;

    /// <summary>#612 · THE CROSSING, SAID ONCE. Owner: <i>"maybe pop-up about you have air or you are in
    /// vacuum type ... it is vital info :-D"</i>.
    ///
    /// <para>It fires only where the tank STARTS or STOPS, never on Room→Ship (both are free, and a line
    /// about a change that costs nothing is the nag that turns a vital fact into wallpaper), and never on
    /// the first tick of an excursion. A room with a DOOR is left to say it in its own voice —
    /// <c>SurfaceShelter.BreathingLine</c> and <c>UndergroundComplex.RefugeEntryLine</c> are already the
    /// better sentences for those thresholds, and two lines for one door is exactly the noise the tank
    /// mechanic was told not to become. What is left is the crossing nothing else narrates: stepping out of
    /// the car onto a floor that holds or does not, and leaving her tube for the regolith.</para>
    ///
    /// <para>#608 · A refuge speaks for itself in all THREE of its states — the room that holds, the room
    /// that holds and has nothing in it, and the door that will not cycle each have their own sentence — so
    /// <paramref name="roomSpeaksForItself"/> is still simply <i>is the captain in one</i>. The failed one
    /// suppresses no crossing in any case: standing in it does not change what the tank is doing.</para></summary>
    private void AnnounceAirSupply(SuitAir.Supply supply, bool roomSpeaksForItself)
    {
        SuitAir.Supply? was = _airSupplyNoted;
        _airSupplyNoted = supply;

        if (was is null || SuitAir.Drawing(was.Value) == SuitAir.Drawing(supply) || roomSpeaksForItself)
        {
            return;
        }

        RendererInterop.PlayCue("blip");
        ShowPulseMessage(SuitAir.SupplyChangedLine(supply));
    }

    /// <summary>How fast her tube refills a suit — several times real time, because standing in an airlock
    /// watching a gauge is not the game. Getting home is the achievement; the top-up is a formality.</summary>
    private const double TubeRefillRate = 12.0;

    // ── #562 · THE TUBE REARMS YOU. ────────────────────────────────────────────────────────────────────
    //
    // Owner, playtesting Miranda with both sentries shouldered and dry: "The gun reload at airlock is not
    // working here now… I carry both guns but they are not being reloaded." He was right twice over.
    //
    // The bug: boarding the shuttle REMOVES the bots from _shipBots and puts them in ex.Bots, and they only
    // come back on liftoff. So for the whole excursion the roster is empty, and every rearm affordance — all
    // of which read _shipBots — reported "No bots aboard… they're deployed on a surface, or written off."
    // That is false in the one state it matters: the captain is carrying both of them, shouldered, in his own
    // airlock. Worse, it was a trap. A dry bot could not be fed until liftoff, and the reason you walked back
    // was that it went dry.
    //
    // The fix he asked for: "I expect them to be reloaded at that tube I was at." So the down-tube feeds
    // them — automatically, cheaply, one magazine at a time, with a bar you can watch and a receipt that
    // says what it cost.
    //
    // WHY A PLACE AND NOT A BUTTON — this is the design, in his words: "the reload forces the player to plan
    // their routes … and keep their supply line safe for retreat to reload", and the tube is therefore "the
    // invisible tether to players distance". Every excursion becomes a loop with a known anchor, and the
    // interesting question is how far out you dare go before the walk back costs more than the rounds would.
    // The retreat is the price; the credits deliberately are not (SentryBot.RestockPricePerRound, halved).
    private void StepTubeRearm(double dtRealSeconds)
    {
        if (_surface is not { } ex)
        {
            return;
        }

        // Standing anywhere but inside the tube ends it. No penalty and nothing lost: rounds already racked
        // are already in the magazine, and the bar simply starts over next time you come back.
        if (!MoonSurface.IsInDownTube(_avatarX, _avatarY))
        {
            ex.RearmBotIndex = null;
            ex.RearmProgress = 0;
            return;
        }

        // Nothing to feed, or nothing to feed it with. Both are quiet — a captain walks through this tube on
        // every single trip, and a tube that nags on the way out would be worse than one that never spoke.
        if (ex.RearmBotIndex is not { } idx)
        {
            idx = NextBotWantingRounds(ex);
            if (idx < 0 || _credits < SentryBot.RestockPricePerRound)
            {
                return;
            }
            ex.RearmBotIndex = idx;
            ex.RearmProgress = 0;
        }

        // The bot may have been planted (or the list rebuilt) since the clock started.
        if (idx >= ex.Bots.Count || ex.Bots[idx].Deployed)
        {
            ex.RearmBotIndex = null;
            ex.RearmProgress = 0;
            return;
        }

        ex.RearmProgress += dtRealSeconds / SentryBot.RearmSecondsPerMagazine;
        if (ex.RearmProgress < 1.0)
        {
            return;
        }

        RackOneMagazine(ex, idx);
        ex.RearmBotIndex = null;
        ex.RearmProgress = 0;
    }

    /// <summary>The first SHOULDERED bot that is short of a full magazine, or -1. Deployed bots are skipped
    /// on purpose: one standing out on the regolith is not in the tube being handed rounds, and pretending
    /// otherwise would be exactly the sim-says-one-thing-sentence-says-another bug this whole lane fixes.
    /// Fills in roster order, one at a time — a magazine is a timer, and one whole timer beats two short.</summary>
    private static int NextBotWantingRounds(SurfaceExcursion ex)
    {
        for (int i = 0; i < ex.Bots.Count; i++)
        {
            if (!ex.Bots[i].Deployed && ex.Bots[i].Rounds < SentryBot.MaxMagazine)
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>Rack one magazine as full as the purse allows, spend the credits, and say so. The quote is
    /// the same pure Core law the haven armory uses (<see cref="SentryBot.QuoteRestock"/>) over a one-bot
    /// list — the same price seen from another door, never a second economy.</summary>
    private void RackOneMagazine(SurfaceExcursion ex, int idx)
    {
        SurfaceBot bot = ex.Bots[idx];
        SentryBot.RestockQuote quote = SentryBot.QuoteRestock([bot.Rounds], _credits);
        if (quote.RoundsBought <= 0)
        {
            return; // the purse ran dry between starting the clock and finishing it
        }

        bot.Rounds = quote.Magazines[0];
        _credits -= quote.Cost;
        RendererInterop.PlayCue("board");
        RequestVaultSave();   // rounds and purse both moved — durable before the next thing happens

        // The first time this ever happens to a captain, the card explains the tether. After that the
        // receipt is the right register: you know where the ammo comes from now.
        if (!ShowTubeRearmCardOnce())
        {
            ShowPulseMessage(
                $"🔫 {bot.Unit} racked to {SentryBot.Readout(bot.Rounds)} — {quote.Cost:N0} cr. " +
                (NextBotWantingRounds(ex) >= 0 ? "Feeding the next one." : "Both full. Back out you go."));
        }
    }

    // #563 · The world grew and the captain has read why. Same seam as CloseGroundLesson — Dismiss() hands
    // the keyboard back to the map div, which matters doubly here: this card can open mid-excursion with a
    // pack already walking toward you, and a swallowed keypress would be a death.
    private void CloseGroundGrew()
    {
        _groundGrewOpen = false;
    }

    /// <summary>
    /// #584 · <b>THE GROUND GREW, AND HERE IS WHERE — the one writer every reveal goes through.</b>
    ///
    /// <para>Owner, mid-tour: <i>"I got like one 'you expanded the map' notification in one map but I was
    /// left totally un-aware about what that did and where?"</i> Three call sites appended real ground to the
    /// live plan and every one of them raised the same card, which said WHAT at length and WHERE not at all.
    /// They go through here now, and they hand over the one fact they each already had in hand: the mouth of
    /// the ground that just joined — the forced door, the hatch, the seal that cracked.</para>
    ///
    /// <para>Two things happen with it, and they are deliberately different in lifetime. The CARD is told
    /// once, in the plate idiom the game uses for a place (<see cref="GroundGrows.Where"/>); the FAN is told
    /// for the rest of the excursion (<c>ex.NewGround</c> → <c>BuildBeacons</c>), because a card is gone in
    /// four seconds and the walk is not. That split is the whole of this fix: the sentence answers the
    /// question and the instrument keeps answering it.</para>
    ///
    /// <para>Returns whatever <see cref="ShowGroundGrewCardOnce"/> returned, so a caller that keeps a toast
    /// for every later time goes on keeping it.</para>
    /// </summary>
    /// <param name="ex">The live excursion — the fan's mark is filed on it.</param>
    /// <param name="mouthX">Where the ground joined: the door/hatch that gave, in field coordinates.</param>
    /// <param name="mouthY">The same.</param>
    private bool TheGroundJustGrew(SurfaceExcursion ex, double mouthX, double mouthY)
    {
        ArgumentNullException.ThrowIfNull(ex);

        // The instrument first, so it is already pointing when the card comes down — and unconditionally,
        // because the card is once per CAPTAIN and the second door a captain ever forces is the one they are
        // most likely to walk away from without noticing.
        ex.NewGround.Add((mouthX, mouthY, ex.Floor));

        _groundGrewWhere = GroundGrows.Where(
            ex.Stop.Body.Id, ex.Floor, mouthX - _avatarX, mouthY - _avatarY);

        return ShowGroundGrewCardOnce();
    }

    /// <summary>#563 · Raise the map-just-grew card, but only ever once per captain. Reached through
    /// <see cref="TheGroundJustGrew"/> from every path that appends real ground to the live plan (a forced
    /// expedition door, an outpost hatch, Vantar's concealed lab door).
    ///
    /// <para>Returns true when the card went up, so the caller can keep its toast for every later time —
    /// the card explains the rule to someone who has never seen it, and the toast is exactly right for
    /// someone who has. Saving immediately is deliberate: the one-time bit must be durable the instant it
    /// is spent, the same habit the convergence reveal uses.</para></summary>
    private bool ShowGroundGrewCardOnce()
    {
        if (_groundGrewSeen)
        {
            return false;
        }
        _groundGrewSeen = true;
        _groundGrewOpen = true;
        RequestVaultSave();
        StateHasChanged();
        return true;
    }

    // #562 · The captain has read what the tube does. Same Dismiss() seam — the keyboard goes back to the
    // map div, which matters here because the card fires INSIDE the tube, i.e. the moment before a captain
    // means to walk back out into whatever they retreated from.
    private void CloseTubeRearm()
    {
        _tubeRearmOpen = false;
    }

    // #573 · The captain has read what the tank is doing. Dismiss() hands the keyboard back — and here that
    // matters more than anywhere: this card opens while the air is already going, so a swallowed keypress
    // is spent air.
    private void CloseAirCard()
    {
        _airCardOpen = false;
    }

    /// <summary>#585 · Walk a body out of solid mass it has ended up inside — a wall that was built around
    /// it rather than one it walked into. Tries short steps outward on a ring of bearings and takes the first
    /// that is open ground; gives up rather than loop, because a contact stuck in stone is a curiosity and a
    /// frame that never ends is a crash.</summary>
    private static (double X, double Y) ExtricateFromStone(
        double x, double y, IReadOnlyList<SurfaceCollision.Segment> walls, double radius)
    {
        if (!SurfaceCollision.Blocked(x, y, radius, walls))
        {
            return (x, y);
        }

        for (double reach = 1.5; reach <= 18.0; reach += 1.5)
        {
            for (int i = 0; i < 12; i++)
            {
                double a = i / 12.0 * Math.Tau;
                double tx = x + (Math.Cos(a) * reach), ty = y + (Math.Sin(a) * reach);
                if (!SurfaceCollision.Blocked(tx, ty, radius, walls))
                {
                    return (tx, ty);
                }
            }
        }
        return (x, y);
    }

    // ── #608 · ONE RACK LAW, TWO BUILDINGS ──────────────────────────────────────────────────────────────
    //
    // The underground refuges are the surface shelter's mechanic, underground — so they are the surface
    // shelter's CODE, not a second copy of it. Everything that decides how much air moves lives in
    // SurfaceShelter (Produce, Transfer, the two-thirds ceiling somebody set for the next person through the
    // door) and both callers step through this one function.
    //
    // The reason it is a function rather than a comment saying "keep these in sync": this project's most
    // expensive habit is two places that have to agree and only one being changed, and the two racks are a
    // perfect candidate — they will be tuned by somebody reading one of them. #573's shelter and #608's
    // refuge now cannot drift, because there is nothing to drift.

    /// <summary>#612 + #608 · IS THE TANK RUNNING? ONE ANSWER, READ BY THE SIM AND BY THE GAUGE.
    ///
    /// <para>#612 shipped the <c>AIR: TANKS / ROOM</c> source on the hud because the owner asked <i>"where
    /// here does it say if I consume tanks or have air?"</i> — and its own issue states the hard part: the
    /// gauge <b>"has to agree with the plate by the lift on every floor. Two instruments disagreeing about
    /// whether you can breathe is worse than one instrument saying nothing."</b></para>
    ///
    /// <para>It was computed as its own expression beside <c>StepSuitAir</c>'s branches, which is the exact
    /// arrangement this project keeps paying for: two places that must agree, and only one gets changed. It
    /// did not survive its first contact with a new way to breathe. A refuge (#608) stops the drain, and the
    /// hud went on reading TANKS while the sim was not spending anything — a captain sitting in air being
    /// told, in colour, that their tank was running out.</para>
    ///
    /// <para>So the drain is gated on this and the gauge is fed from this. Anything that ever becomes a new
    /// place to breathe is added HERE, once, and both follow.</para>
    ///
    /// <para><b>And "here" is now Core</b>, because this client-side version could still only be read by a
    /// client: the plate <c>HiveInterior</c> paints by the lift was calling
    /// <c>UndergroundComplex.HoldsPressure</c> for itself and spelling its own words, which made a THIRD
    /// answer to the same question. <see cref="SuitAir.SourceOf"/> is the predicate; this method is the one
    /// place that gathers the four facts to hand it, and every surface reads what it says.</para></summary>
    /// <para><b>#621 · and the third fact was answered with the wrong world's rule.</b> "Aboard" was
    /// <c>MoonSurface.IsSafeAboard(_avatarY)</c> — the regolith's top rim at y = −20 — while a derelict's
    /// whole deck runs −9 to +9, so every point aboard every wreck said YES. The gauge told a captain
    /// standing in a hull that has held vacuum for years that they were on HER AIR and their tank was
    /// FILLING, and the drain agreed with it. <see cref="AwayTeamSide.BackAtTheShuttle"/> is the one place
    /// that knows which door you are on the far side of, and both the reach rule and this one now read
    /// it.</para>
    private SuitAir.Supply AirSupplyOf(SurfaceExcursion ex) =>
        SuitAir.SourceOf(
            ex.Stop.Body.Id,                                 // #677 which building — the halls breathe
            ex.Floor,
            StandingInTheShelter(ex),                        // #573 the deep shelter
            CaptainBeyondReach,                              // her tube — or past a wreck's lock: breathing hers
            BreathingRefugeUnderfoot(ex));                   // #608 a pressure refuge that still holds

    /// <summary>Is the tank running? The one bit of <see cref="AirSupplyOf"/>, for callers that want no
    /// more than that.</summary>
    private bool TankIsDrawing(SurfaceExcursion ex) => SuitAir.Drawing(AirSupplyOf(ex));

    /// <summary>Run one rack for <paramref name="dt"/> seconds: it makes air, it moves what it can into the
    /// suit, and the warnings re-arm if anything went in. Returns the reservoir it is left holding, and
    /// reports how much reached the tank.</summary>
    private double DrawFromRack(SurfaceExcursion ex, double held, double dt, out double pumped)
    {
        double made = SurfaceShelter.Produce(held, dt);
        pumped = SurfaceShelter.Transfer(ex.AirSeconds, made, SuitAir.TankSeconds, dt);
        if (pumped > 0)
        {
            ex.AirSeconds = SuitAir.Refill(ex.AirSeconds, pumped);
            ex.AirLowWarned = false;
            ex.AirWarned = false;
            ex.ReserveNoted = false;
        }
        return made - pumped;
    }

    /// <summary>#608 · The refuges on the floor the captain is standing on, read off the deck the renderer
    /// actually drew.
    ///
    /// <para><b>Not rebuilt from Core.</b> <c>UndergroundComplex.Build</c> is pure but not free, and this is
    /// asked every frame by the suit; more importantly, a second call would be a second answer. The consoles
    /// on <c>_deckPlan</c> ARE the refuges — <see cref="HiveInterior.FloorDeck"/> put them there off the
    /// floor plan — so the room the captain can see and the room that holds their air are the same object by
    /// construction rather than by two functions agreeing.</para></summary>
    private List<(double X, double Y)> RefugesOn()
    {
        var found = new List<(double, double)>();
        foreach (DeckPlan.ConsoleSpot spot in _deckPlan.Consoles)
        {
            if (spot.Kind == DeckPlan.ConsoleKind.HiveRefuge)
            {
                found.Add((spot.X, spot.Y));
            }
        }
        return found;
    }

    /// <summary>#608 · What the seal on this floor's refuge has done with the decades, or null off a floor
    /// that has one. Asked of Core rather than carried, because the state is a fact about the FLOOR and
    /// there is one refuge on it: <see cref="UndergroundComplex.StateOfTheRefugeOn"/> is the one answer the
    /// suit, the plate, the tracker and the panel all read.</summary>
    private UndergroundComplex.RefugeState? RefugeSealHere(SurfaceExcursion ex) =>
        ex.Floor >= 0 ? null : UndergroundComplex.StateOfTheRefugeOn(ex.Stop.Body.Id, ex.Floor);

    /// <summary>#608 · Is the captain standing in air that a refuge is providing? Both halves — inside the
    /// box AND the box still holds — because a failed refuge is a room on a dead floor and nothing else, and
    /// the gauge saying ROOM in one would be the instrument lying at the one door on the floor a captain
    /// walked a tank to reach.</summary>
    private bool BreathingRefugeUnderfoot(SurfaceExcursion ex) =>
        ex.Floor < 0
        && RefugeUnderfoot(ex) >= 0
        && RefugeSealHere(ex) is { } seal
        && UndergroundComplex.RefugeStillHolds(seal);

    /// <summary>Which refuge the captain is standing inside, or -1. Never anything but -1 above ground.
    /// GEOMETRY ONLY — whether that room has anything in it is <see cref="RefugeSealHere"/>'s business, and
    /// keeping the two apart is what lets a failed refuge still say its line at the door.</summary>
    private int RefugeUnderfoot(SurfaceExcursion ex)
    {
        if (ex.Floor >= 0)
        {
            return -1;
        }
        List<(double X, double Y)> all = RefugesOn();
        for (int i = 0; i < all.Count; i++)
        {
            if (UndergroundComplex.RefugeHolds(all[i].X, all[i].Y, _avatarX, _avatarY))
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>A refuge rack's reservoir, in suit-seconds — the shelter's own story, told about a room
    /// under a moon. Keyed per FLOOR, so walking back into B7's refuge finds it as you left it and B3's is
    /// somebody else's problem.</summary>
    private double RefugeReservoirNow(SurfaceExcursion ex, int index)
    {
        if (index < 0)
        {
            return 0;
        }

        // #608 · A rack only exists where the maintenance line did. On an empty or a failed refuge this is
        // zero at the source rather than zero by the caller remembering to ask — a reservoir that could be
        // read out of a room with no bottles in it is one refactor away from filling a tank from one.
        if (RefugeSealHere(ex) != UndergroundComplex.RefugeState.Holding)
        {
            return 0;
        }
        int key = RefugeKey(ex.Floor, index);
        if (ex.RefugeReservoir.TryGetValue(key, out double held))
        {
            return held;
        }

        // #573's idiom, underground: a rack that is not full means SOMEBODY WAS HERE. Down here that is a
        // colder sentence than it is on the regolith — the building has been shut for decades and the seals
        // on this room have not — and it costs nothing but a seeded roll.
        double start = SurfaceShelter.SomebodyWasHere(
                ex.Stop.Body.Id, $"{ex.Site.LayoutSalt}:hive{ex.Floor}", index)
            ? SurfaceShelter.ReservoirSeconds * 0.42
            : SurfaceShelter.ReservoirSeconds;
        ex.RefugeReservoir[key] = start;
        return start;
    }

    /// <summary>One key per refuge per floor, so B2's rack is not B3's. Same shape as
    /// <see cref="HiveInterior.RoomKey"/>, and deliberately a different dictionary.</summary>
    private static int RefugeKey(int level, int index) => (level * 1000) - index;

    /// <summary>#608 · What a rack — either rack — says when it is asked how it is doing. One reading of one
    /// machine, so the shed on the regolith and the refuge eleven floors down can never describe the same
    /// state in two different ways.</summary>
    private static string RackGaugeLine(SurfaceExcursion ex, double held) =>
        ex.AirSeconds >= SuitAir.TankSeconds * SurfaceShelter.FillToFraction
            ? SurfaceShelter.PumpDoneLine
            : held > SurfaceShelter.ReservoirSeconds * 0.1
                ? SurfaceShelter.PumpingLine
                : SurfaceShelter.TrickleLine;

    /// <summary>#573 · Raise the tank-is-low card, once per captain ever. Returns true when it went up, so
    /// the caller keeps its pulse line for every later trip.</summary>
    private bool ShowAirCardOnce()
    {
        if (_airCardSeen)
        {
            return false;
        }
        _airCardSeen = true;
        _airCardOpen = true;
        RequestVaultSave();
        StateHasChanged();
        return true;
    }

    /// <summary>#562 · Raise the tube-feeds-you card, once per captain ever. Returns true when it went up,
    /// so the caller keeps its receipt line for every later racking. The card teaches the shape of an
    /// excursion — one anchor, plan the route home — and the receipt is right for a captain who knows.</summary>
    private bool ShowTubeRearmCardOnce()
    {
        if (_tubeRearmSeen)
        {
            return false;
        }
        _tubeRearmSeen = true;
        _tubeRearmOpen = true;
        RequestVaultSave();
        StateHasChanged();
        return true;
    }
}
