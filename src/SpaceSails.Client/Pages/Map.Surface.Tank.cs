using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Part of Map.Surface (#870 split; the header note lives in Map.Surface.cs) — the suit tank, where the air is coming from, the tube rearm, and the one rack law both buildings obey.

/// <summary>
/// #564 · THE TANK — the resource the ground lesson has been warning every new captain about since #440,
/// stepped once a frame.
///
/// <para>#251 · This file keeps the tank and the one method that spends it. The other three are named for
/// what they answer about it: <c>.Supply</c> (#612 — where the air is coming from, said once at the
/// crossing), <c>.Rearm</c> (#562 — the tube rearms you, and the cards that say so), and <c>.Rack</c>
/// (#608 — one rack law, two buildings).</para>
///
/// <para>The family declares no static field, so the #1163 initializer hazard is absent by construction.
/// No member is renamed, re-scoped or re-ordered by the cut.</para>
/// </summary>
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
            //   EMPTY   · #1149 · the drain stops and the rack pumps from EMPTY. Somebody drew this one
            //             right down before the captain got to it (the #573 footprint), and a cracker that
            //             always produces is the whole of that idiom — so the room costs a captain TIME
            //             instead of buying them range, and standing there is a real, grim, valid decision.
            //   FAILED  · nothing at all. The line is said, once, at the door, the card goes up, and then
            //             this falls straight through to the drain below: standing in a room whose seal went
            //             is standing on a dead floor, and the tank knows it even if the plan does not.
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

                    // #573's idiom, and only where there is a rack to have been drawn on. On a FAILED one
                    // "somebody was here before you" would be a sentence about a reservoir that does not
                    // exist — the game telling a story off a number it is not running.
                    //
                    // #1149 · EMPTY is now the loudest case of it rather than an exclusion. An empty rack is
                    // not decay, it is a footprint: the reservoir reads zero, PartialLine's first rung says
                    // so and says who, and the cracker is already refilling it while the captain reads.
                    string found = holds
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

                    // #1149 · AND THE ONE ROOM IN THE BUILDING THAT IS A STORY. Owner: "If for dramatic
                    // suspense we need one that does not work, that is narrated, with a gen-AI image:
                    // something scary or weird happened to the shelter." The card is the whole of the
                    // telling (#761) — the pulse above is what a captain SEES standing in the doorway, and
                    // the card is what the room turns out to be — and it is raised from HERE rather than
                    // from a verb because arriving IS the event: there is nothing to press and nothing to
                    // decide. Once per site, because a building has at most one of these.
                    if (seal == UndergroundComplex.RefugeState.Failed)
                    {
                        RaiseStoryBeat(StoryBeats.Beat.RefugeFailed, ex.Stop.Body.Id);
                    }
                }

                if (holds)
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
    /// deep are priced identically (#453: depth is not a danger gradient).
    ///
    /// <para>#719 slice 2 · <b>UNLESS SOMEBODY HAS STOPPED THE CAR</b>, in which case the way home is the
    /// walk to the stair's door and then the climb, and the suit says so. This is #1115's one flagged
    /// judgement call, paid off: the stair's price was real from the day it shipped and the readout would
    /// not quote it, because underground the fan's HOME ring was the CAGE and
    /// <c>TheWayBackIsAlwaysOnTheFanTests</c> holds the law that the ring and the readout measure ONE
    /// journey — quoting a climb as "the walk home" with a free ride standing in the corridor would have
    /// been two instruments disagreeing about where home is.</para>
    ///
    /// <para>The break settles it the right way round. With the car stopped there is no free ride left to
    /// disagree with: the ring moves to the stair door (<c>BuildBeacons</c>) and this measures to the same
    /// door and then up, off Core's own arithmetic — so the law holds BY the break rather than being broken
    /// by it. <b>And every threshold the suit already owns moves with it</b>: the crossing line, the
    /// reserve, the low-air card and the on-grid countdown are all written against this one number, so
    /// nothing new has to warn anybody. The instruments that were already watching simply start telling the
    /// truth about a longer journey — which is the owner's <i>going up would use more air</i>, arriving as
    /// the price of a thing the captain did.</para></summary>
    private double DistanceToTheTube()
    {
        if (_surface is { } stopped && TheCarIsStopped)
        {
            return TheClimbHomeDu(stopped);
        }

        double dx = _avatarX - MoonSurface.SpawnX;
        double dy = _avatarY - MoonSurface.SpawnY;
        return Math.Sqrt((dx * dx) + (dy * dy));
    }

    // #573 · Last frame's position, for working out whether the captain is standing, walking or running.
    // Speed is not otherwise tracked on the surface, and the difference between a stroll and a sprint is the
    // whole of the owner's "keep calm" rule.
    private double _airLastX;
    private double _airLastY;
}
