using System;
using System.Collections.Generic;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #870 lane 6c · THE PARK BENCH, as the seat's own verbs — moved off <c>Map.Bench.cs</c>, byte for byte
/// except for the receiver.
///
/// <para>#793's design, its rulings and the three facts that make a plank different from a canteen top are
/// documented where they have always been documented: <c>Map.Bench.cs</c>'s own class summary, which stayed
/// with the tail-check machinery this page still owns. What is here is the PRESS — taking a bench, being
/// joined on it, and the dev row that walks you to a free one — and what it needs from the page it is drawn
/// on is <see cref="ISeatHost"/> and nothing else.</para>
/// </summary>
public partial class Map
{
    private sealed partial class Seating
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
        public bool TryTakeBench()
        {
            if (_host.Surface is not { } ex || ex.Floor >= 0)
            {
                return false;
            }

            // Already sitting. The press is CONSUMED — [E] is not how you stand up, "Stand up" is.
            if (Table is not null)
            {
                return true;
            }

            if (_host.DeckPlan.NearestConsoleSpot(_host.AvatarX, _host.AvatarY) is not
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
            if (_host.Surface is not { } ex)
            {
                return;
            }

            bool shared = bench.Taken;
            Encounter.Scene sat = ParkBenches.TheBench(shared);

            // Read BEFORE the body moves: the end you take is the end you walked up to, and asking after the
            // snap would be asking where the captain is now sitting, which answers itself.
            (double seatX, double seatY) = bench.EndYouTake(_host.AvatarX, _host.AvatarY);
            (double offX, double offY) = TowardTheWalk(in green, seatX, seatY);
            _host.SitCaptainOn(seatX, seatY);

            TakeThisSeat(new TableTalk
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
                DrinkInHand = _host.APourInFrontOfYou,
                // Nobody to ask. The bench is simply taken, and the taking is the scene's opening line.
                Joined = true,
                Outcome = sat.Opening,
            });
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
                _host.AbandonProcessing(ex, Core.Processing.Interruption.CompanyArrived);
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
            _host.RequestVaultSave();
            _host.StateHasChanged();
        }

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
        public bool SitOnAFreeBenchIfAsked(in UndergroundComplex.Park green)
        {
            if (!_host.SpreadCheat)
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
                _host.StandCaptainAt(wx, wy, "you walk down the gravel to a free bench");
                _host.SeedTheSpreadFinds();
                SitOnThisBench(in green, bench);
                _host.ShowPulseMessage(
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
}
