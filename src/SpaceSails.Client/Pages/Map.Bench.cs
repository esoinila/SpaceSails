using System;
using System.Collections.Generic;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #793 · THE PARK BENCH IS A GUMSHOE MOVE — sit, see who else stops, and work the case if the bench is all
/// yours.
///
/// <para>Owner, from play in the new park (<i>"I love the park"</i>), in three parts:</para>
///
/// <list type="number">
/// <item><b>The benches take the sit verb.</b> <i>"E at a steel bench seats you… a park bench under a
/// painted sky is the best breather in the base."</i> #790 shipped them as labelled fixtures; this wires
/// them to the seat machinery #778/#788/#799 already built, short rest and all.</item>
/// <item><b>The bench is a COUNTER-SURVEILLANCE instrument.</b> <i>"it is a good gumshoe move to see if
/// anyone is following us by foot, as they would need to stop moving also."</i></item>
/// <item><b>The bench processes the case — IF it is all yours.</b> <i>"if we get the whole bench to
/// ourselves to have some privacy."</i></item>
/// </list>
///
/// <h3>The same panel, on purpose</h3>
///
/// <para>A bench does not get a seated frame of its own. Sitting down is one posture with one answer
/// (<c>Map.Seated.CaptainIsSeated</c>), one docked strip, one WAIT beat and one short-rest ledger, and a
/// second seated panel beside those would be this repo's first named bug class aimed at a chair. What is
/// genuinely different about a bench is three facts, and they are three flags on the sitting rather than
/// three copies of it: which rung of the exposure ladder it is, what the room says when nobody comes, and
/// what somebody ARRIVING does — because on a plank they sit down beside you and start no conversation
/// at all.</para>
///
/// <h3>What is deliberately NOT here</h3>
///
/// <para><b>A tailing NPC.</b> Nothing in this game follows the captain yet, and inventing a watcher to
/// demonstrate a bench would be shipping a feature to justify a fixture. What ships is the SEAM: Core's
/// <see cref="FootTail"/> question, asked for real on every beat spent on a bench, answered <i>no</i> today
/// by every mover that exists (#804's rounds are laid before the captain arrives and cannot be tails), with
/// the hold law and its drawing written and guarded from both directions.</para>
/// </summary>
public partial class Map
{
    // ── SITTING DOWN ON ONE ───────────────────────────────────────────────────────────────────────────

    /// <summary>What a bench's watch-scoped state is keyed on. Its own prefix, so a bench and a canteen top
    /// with the same ordinal on the same floor can never share a wait counter or an approach latch — they
    /// are two seats in two rooms, and one key for both would be one source consumed as if it were two.</summary>
    private static string BenchKey(SurfaceExcursion ex, int benchIndex) =>
        $"{ex.CanteenWatch}:{ex.Floor}:bench:{benchIndex}";

    /// <summary>
    /// #793 · TAKE A BENCH — [E] at a steel bench in the park, and the captain sits down.
    ///
    /// <para>#757's own posture and geometry, one room along: the seat is the spot you walked to in order to
    /// press the key, and nothing teleports anybody onto the furniture (the plank is a solid segment in the
    /// collision field and always was). WHICH bench it is comes off Core's own list
    /// (<see cref="ParkBenches.On"/>), matched to the console the press landed on — a lookup rather than a
    /// decision, and never a coordinate this file measured for itself (§13.15).</para>
    ///
    /// <para>A bench that already has somebody on it STILL TAKES THE PRESS. That is the feature and not an
    /// oversight: half a bench is a rest, and it is the rung of the exposure ladder that teaches the privacy
    /// law by refusing the spread out loud (#603) rather than by having no control at all.</para>
    /// </summary>
    private bool TryTakeBench()
    {
        if (_surface is not { } ex || ex.Floor >= 0)
        {
            return false;
        }

        // Already sitting. The press is CONSUMED — [E] is not how you stand up, "Stand up" is.
        if (_seating.Table is not null)
        {
            return true;
        }

        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not
            { Kind: DeckPlan.ConsoleKind.HiveBench } spot)
        {
            return false;
        }

        if (UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField()).Park
            is not { } green)
        {
            return false;
        }

        if (ParkBenches.At(in green, spot.X, spot.Y) is not { } bench)
        {
            return false;
        }

        SitOnThisBench(in green, bench);
        return true;
    }

    /// <summary>
    /// The one place a bench sitting is opened, so a dev row and a captain's [E] cannot open two different
    /// benches.
    ///
    /// <para>#820 · …and the one place the captain is put ON one. Owner, from the bench: <i>"I would move the
    /// avatar on top of the bench when I sit… just snap it into the correct position."</i> WHICH END is
    /// Core's (<see cref="ParkBenches.Bench.EndYouTake"/>: the free one when somebody is already sitting
    /// there, else the one you walked up to) and this file measures nothing. The step off is read here too,
    /// while the park is in hand, and carried on the sitting — because standing up off a PLANK is the one
    /// stand in the game that has to move the captain: the bench is a solid segment in the collision field,
    /// and a captain left where they sat would be standing inside the furniture.</para>
    /// </summary>
    private void SitOnThisBench(in UndergroundComplex.Park green, ParkBenches.Bench bench)
    {
        if (_surface is not { } ex)
        {
            return;
        }

        bool shared = bench.Taken;
        Encounter.Scene sat = ParkBenches.TheBench(shared);

        // Read BEFORE the body moves: the end you take is the end you walked up to, and asking after the
        // snap would be asking where the captain is now sitting, which answers itself.
        (double seatX, double seatY) = bench.EndYouTake(_avatarX, _avatarY);
        (double offX, double offY) = TowardTheWalk(in green, seatX, seatY);
        SitCaptainOn(seatX, seatY);

        _seating.Table = new TableTalk
        {
            StepOff = (offX, offY),
            Key = BenchKey(ex, bench.Index),
            // THE APPROACH ORDINAL, and deliberately not the bench's own. SomebodyComes is seeded on
            // (site, floor, ordinal, watch, beat), and bench 0 sharing table 0's ordinal would deal the two
            // seats the same answer on the same shift. Core owns the offset; this only asks for it.
            Index = ParkBenches.ApproachOrdinal(bench.Index),
            BenchIndex = bench.Index,
            Bench = true,
            Who = CanteenTable.Who.None,
            Plate = shared ? ParkBenches.SharedPlate : ParkBenches.OwnBenchPlate,
            Scene = sat,
            Seats = ParkBenches.Ends,
            // Two ends. One is yours the moment you sit down; the other is free unless somebody is on it.
            Free = shared ? 0 : ParkBenches.Ends - 1,
            // A bench is not a cabinet: there is no door, and the whole instrument is that people can walk
            // past you. Saying otherwise here would switch off the approach roll the beat is built on.
            Quiet = false,
            // SOLO IS TRUE EVEN ON A SHARED BENCH, and that is the distinction this rung exists to make.
            // Solo means "this is not a conversation" — it is what docks the frame and what lets the wait
            // beat and the short rest run. Somebody on the far end of a plank is not talking to you. What
            // they cost you is privacy, which is SharedSeat's job and nothing else's.
            Solo = true,
            SharedSeat = shared,
            // #783's relaxed register is a canteen table's: an empty chair opposite, boots up on it, a
            // bought glass. A bench has neither the chair nor the table, and it draws no art at all — so
            // this stays false rather than borrowing a picture of a room the captain is not in.
            Relaxed = false,
            DrinkInHand = APourInFrontOfYou,
            // Nobody to ask. The bench is simply taken, and the taking is the scene's opening line.
            Joined = true,
            Outcome = sat.Opening,
        };

        RendererInterop.PlayCue("reveal");
        StateHasChanged();
    }

    // ── SOMEBODY SITS DOWN NEXT TO YOU ────────────────────────────────────────────────────────────────

    /// <summary>
    /// #793 · THE OTHER END GOES — and your papers go with it.
    ///
    /// <para>Owner: <i>"somebody sitting down next to your open case files is a BEAT."</i> This is #799's
    /// own <c>CompanyArrived</c> seam, wired for a bench: the hold ends BEFORE the flag flips, the way
    /// privacy ending should end it — sleeve shut, book blank, nothing filed.</para>
    ///
    /// <para>It is NOT <c>SomebodyTakesTheChair</c> and must never become it. That method flips
    /// <c>Solo</c>, which raises the full conversation card over the room — correct for a haulier who has
    /// crossed a hall to talk to you, and wrong for a stranger with a cup who has sat down on the far end of
    /// a plank and not looked up. Nobody says anything here, and the panel does not pretend otherwise.</para>
    /// </summary>
    private void SomebodyTakesTheOtherEnd(SurfaceExcursion ex, TableTalk t)
    {
        if (ex.Processing is { Work: Core.Processing.Work.Write })
        {
            AbandonProcessing(ex, Core.Processing.Interruption.CompanyArrived);
        }

        t.SharedSeat = true;
        t.Plate = ParkBenches.SharedPlate;
        t.Scene = ParkBenches.TheBench(shared: true);
        t.Free = 0;
        t.Showing = false;
        t.Math = null;

        // #761 · Told, clearly, on the surface the captain is looking at. The plank moved; the game says so
        // in words rather than leaving it to be inferred from a control that has quietly gone grey.
        t.Outcome = ParkBenches.SomebodyTookTheOtherEndLine;
        RendererInterop.PlayCue("reveal");
        RequestVaultSave();
        StateHasChanged();
    }

    // ── THE TAIL-CHECK ────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #793 · EVERYBODY ON FOOT, as the bench sees them.
    ///
    /// <para>Today that is #804's rounds and nothing else — built through
    /// <see cref="PatrolBeat.OnTheRound"/>, which stamps them as walkers on a PUBLISHED ROUND and therefore
    /// as movers that can never be tails. That is not this file being polite about guards: their beat is a
    /// pure function of (site, floor, watch) drawn before the captain stepped off the car, and a route that
    /// existed before you did is not a route that is about you.</para>
    ///
    /// <para>The list is built here, per ask, rather than kept as a field: it is one small allocation on a
    /// key press and it cannot go stale, which a cached copy of everybody's position certainly would.</para>
    /// </summary>
    private IReadOnlyList<FootTail.Mover> MoversAfoot()
    {
        var afoot = new List<FootTail.Mover>(_guards.Count);
        for (int i = 0; i < _guards.Count; i++)
        {
            afoot.Add(PatrolBeat.OnTheRound(i, _guards[i].X, _guards[i].Y));
        }
        return afoot;
    }

    /// <summary>#793 · Is anything actually following the captain right now? Core's one predicate, asked with
    /// the captain's own position and the same sight blockers the guards' markers are drawn through.</summary>
    private bool AnythingTailingYou =>
        FootTail.AnythingTailing(_avatarX, _avatarY, MoversAfoot(), SightBlockers());

    /// <summary>
    /// #793 · WHAT SITTING STILL SHOWED YOU — one sentence, and never a meter.
    ///
    /// <para>#618's law, one instrument along: <i>"a cover that can blow, not a detection meter."</i> Nothing
    /// counts, nothing fills, nothing turns red. You sat down, you looked down the walk, and the park either
    /// handed you something or it did not — and either way it is a sentence a player has to read and decide
    /// about, which is the whole of the gumshoe register.</para>
    /// </summary>
    private string TheTailReading() => FootTail.Reading(AnythingTailingYou);

    // ── THE DEMO ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #793 QA · <c>?park=1&amp;spread=1</c> — sat on a bench with the papers out, in one link.
    ///
    /// <para>The owner's own demo ask for #784, in the room #793 moved it to: a start point with things in
    /// the sleeve to process while the HUD state is <i>sitting down with enough privacy</i>. It walks to one
    /// of the room's OWN benches (asked of <see cref="ParkBenches.On"/>, never a coordinate typed here) and
    /// sits the captain down through the very method [E] reaches — a cheat that assembled its own sitting
    /// would be demonstrating a bench that does not ship.</para>
    ///
    /// <para>It takes a FREE bench, because the whole point of the row is the spread being allowed; the
    /// shared rung is one bench along and is reached by walking, which is a thing a tester should do on
    /// their feet rather than be teleported into.</para>
    /// </summary>
    private bool SitOnAFreeBenchIfAsked(in UndergroundComplex.Park green)
    {
        if (!_spreadCheat)
        {
            return false;
        }

        foreach (ParkBenches.Bench bench in ParkBenches.On(in green))
        {
            if (bench.Taken)
            {
                continue;
            }

            // Stood beside the plank rather than on it — a bench is a solid segment in the collision field,
            // and this row places a body, so WHICH SIDE has to come off the room rather than off a sign.
            //
            // §13.15, paid attention to: a standoff typed as "one and a bit du below the bench" is a guess
            // about which way a bench faces, and the carve puts them on the OUTSIDE of every bend — some
            // above the walk, some below it, none of them the same. So the direction is the park's own
            // published gravel (ParkWalkOff), and the captain lands on the walk side, on ground #790's own
            // reachability guard has already proved you can stand on.
            (double sx, double sy) = bench.YourEnd;
            (double wx, double wy) = TowardTheWalk(in green, sx, sy);
            StandCaptainAt(wx, wy, "you walk down the gravel to a free bench");
            SeedTheSpreadFinds();
            SitOnThisBench(in green, bench);
            ShowPulseMessage(
                "🧪 DEV ?park=1&spread=1: sat on a park bench with the WHOLE BENCH to yourself and three "
                + "finds in the sleeve. Press I and dig. Then walk to the bench with somebody on it and try "
                + "the same — the refusal is the feature.");
            return true;
        }

        return false;
    }

    /// <summary>How far off the plank a captain stands beside it — walking up to press [E], and stepping off
    /// again when they stand up (#820). Clear of the bench's own collision segment plus a body's radius, and
    /// comfortably inside <see cref="DeckPlan.InteractRadius"/> even measured to the bench's CENTRE — a
    /// placement that set somebody down inside the furniture, or just out of reach of the console it walked
    /// them to, is §13.15's second cause, which this project has paid for twice.</summary>
    private const double BenchStandoffDu = DeckPlan.AvatarRadius + 1.0;

    /// <summary>#793/#820 · One standoff off a bench end, ON THE WALK SIDE — the park's own gravel deciding
    /// which way that is. The nearest published walk sample gives the bearing; the standoff gives the
    /// distance. Nothing here knows which way a bench faces, because nothing here laid one down.
    ///
    /// <para>It was the dev row's alone until sitting down started snapping the captain onto the plank. It is
    /// now also where standing up puts them, which is the same question asked from the other side — and one
    /// answer to it is the reason a solid bench cannot close over the dot.</para></summary>
    private static (double X, double Y) TowardTheWalk(
        in UndergroundComplex.Park green, double fromX, double fromY) =>
        ParkBenches.TowardTheWalk(in green, fromX, fromY, BenchStandoffDu);
}
