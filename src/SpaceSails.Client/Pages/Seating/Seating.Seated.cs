using System;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #870 lane 6c · SITTING DOWN AND GETTING UP AGAIN, and the exposure ladder — moved off
/// <c>Map.Seated.cs</c>, byte for byte except for the receiver.
///
/// <para>#784's three rulings and #847's supersession of the first of them are documented where they have
/// always been documented: <c>Map.Seated.cs</c>'s own class summary. What is here is the half of that file
/// which is genuinely ABOUT THE SEAT — the stand-up confirm and its one raiser, the one stand-up path every
/// movement input pays, the sit beat armed and spent, which rung of the exposure ladder the captain is on,
/// whether they have the seat to themselves, and the two questions the spread is gated on.</para>
///
/// <para>What stayed on the page is everything a seat is a GATE ON rather than a part of: the pour window,
/// the short rest's arithmetic and its per-watch ceiling, and the whole field-book register — the dig, the
/// entry, the rows, the key. Those spend the nerve, the condition and the processing systems, and this object
/// asks them for an answer (<see cref="ISeatHost.APourInFrontOfYou"/>,
/// <see cref="ISeatHost.PourSecondsLeft"/>) rather than for the machinery. The register goes the other way:
/// it is on <see cref="Map"/> and it asks the seat for <c>SpreadRefusal</c>, which is exactly the shape a
/// gate should have.</para>
/// </summary>
public partial class Map
{
    private sealed partial class Seating
    {
        // ── STANDING UP IS A DECISION ─────────────────────────────────────────────────────────────────────

        /// <summary>#784 · THE STAND-UP CONFIRM — <i>are you sure you want to give the seat up?</i>
        ///
        /// <para>The whole point is the investment underneath: a stray press must not throw away the watch you
        /// spent being findable (#757) or the breath you have got back sitting there (#784).</para>
        ///
        /// <para>#847 · ITS ONE RAISER IS NOW ESC ON A DOCKED STRIP. It was WASD as well until the owner ruled
        /// that a movement key is not a stray press at all — a captain pressing W has said where they are going,
        /// and the answer to that is a stand, not a question (<see cref="StandUpBeforeWalking"/>). What is left
        /// here is the press that says nothing about what the captain wants NEXT: cancel, on a frame with no card
        /// to take, where a silent teardown would spend the sitting for nothing.</para></summary>
        public void AskWhetherToStandUp()
        {
            if (!StandUpAsk)
            {
                StandUpAsk = true;
                RendererInterop.PlayCue("reveal");
                _host.StateHasChanged();
            }
        }

        /// <summary>Esc, the ✕, and the "stay where you are" button all come through here — keeping your seat is
        /// one act however you do it, and it is free.</summary>
        public void KeepYourSeat()
        {
            StandUpAsk = false;
            _host.StateHasChanged();
        }

        /// <summary>The one press that stands you up. Through <see cref="CloseTable"/>, which is #757's own
        /// single way out of a table — a hand-written copy of standing up is this repo's first named bug class,
        /// and the day leaving a table costs something, it must cost it here too.
        ///
        /// <para>#847 · …and this is the path a MOVEMENT INPUT takes as well, for every seat that is a table:
        /// the confirm's Stand up button, W at a park bench and a clicked route from a cubicle are one act with
        /// one teardown under them (<see cref="StandUpBeforeWalking"/>). The one line it says —
        /// <see cref="SeatedPosture.StoodUpToWalkLine"/>, "off you go" — was written for exactly the press that
        /// now reaches it most often.</para></summary>
        public void StandUpFromTable()
        {
            StandUpAsk = false;
            if (Table is null)
            {
                return;
            }
            CloseTable();
            _host.ShowPulseMessage(SeatedPosture.StoodUpToWalkLine);
            _host.StateHasChanged();
        }

        /// <summary>
        /// #847 · MUST STAND UP BEFORE WALKING — the one act a movement input owes, whatever the captain is
        /// sitting on.
        ///
        /// <para>Owner's ruling, 2026-08-13, with the receipt: <i>"Must stand up before walking (not standing up
        /// before doing that was my Pyramid climbing exercise in Giza last January … legs were sore :-D)"</i> —
        /// and the shape of it: <i>"WASD (and click-to-walk) while seated at ANY seat first routes through the
        /// seat's own stand-up path (the #846 step-off, scene teardown and all), THEN walks. No refusal line
        /// needed — the keys simply cost you the stand first, which is how chairs work."</i></para>
        ///
        /// <para><b>It ROUTES and it decides nothing.</b> There is no teardown written here: a stool is got down
        /// from the way the counter's own verb gets down from one, and every seat that is a <see cref="Seating.Table"/>
        /// — a canteen top, a cabinet, a park bench, a ring-office chair, a laboratory stool, a cubicle — stands
        /// up the way the Stand up button stands up, through <see cref="StandUpFromTable"/> and so through
        /// <c>CloseTable</c>: the abandoned write-up, the step-off square, the nudge, the line. A second author on
        /// one teardown is this repo's first named bug class, and the day standing up costs something it must
        /// cost it once.</para>
        ///
        /// <para>Both seats are asked, and neither is an <c>else</c>. One of you can only be on one thing at a
        /// time and the game is built that way — but a movement key whose job is "leave every seat you are in"
        /// must not be the place that assumes it, or the first seat that ever survives another would ride the
        /// walk out of the room.</para>
        /// </summary>
        /// <returns>Whether the captain had to get up at all — false when they were already on their feet, which
        /// is the ordinary case and costs nothing.</returns>
        public bool StandUpBeforeWalking()
        {
            bool stood = false;

            if (Stool is not null)
            {
                GetDownFromStool();
                stood = true;
            }

            if (Table is not null)
            {
                StandUpFromTable();
                stood = true;
            }

            return stood;
        }

        /// <summary>Whether the confirm should say what standing up COSTS. Only when there is something left to
        /// lose — a captain who has taken everything a short rest has is not being warned about anything.</summary>
        public bool StandingUpCostsARest =>
            _host.Surface is { } ex
            && CaptainIsRestingAtATable
            && (!ex.RestPipsEased.TryGetValue(ex.CanteenWatch, out int pips)
                || pips < ShortRest.NervePipCapPerWatch);

        // ── #865 · NOTHING MODAL COVERS THE SIT BEAT ──────────────────────────────────────────────────────
        //
        // Owner, 2026-08-13, at a canteen table on B1: "I sat down the table but the pop ups blocked my view of
        // my avatar sitting down" — and on the retry, when nothing covered it: "Oh now it picked the chair and
        // it worked." Then, of the two attempts together: "That was different than last time."
        //
        // WHAT RACED. The first-entry cards down here are POSITION POLLS — CheckCantinaHallUnderfoot and its
        // siblings run every surface tick and raise a centred _viewObject the moment the captain's coordinates
        // fall inside a room that still owes its card. #820's snap MOVES THOSE COORDINATES: sitting down puts
        // the dot on the chair, and the chair is inside the hall's box and inside a cabinet's box. So the tick
        // after [E] raised a full-screen card over the exact half-second the snap exists to be watched. And
        // because every one of those latches is one-shot per excursion, it could only ever happen ONCE — the
        // first sit of a session wore a card, the second wore the strip, and the owner read the difference as
        // nondeterminism because from the chair that is precisely what it is.
        //
        // THE BEAT IS OWED, NOT SPENT. Nothing is dropped: the polls are latched and the story seam has a queue,
        // so holding them for SeatedPosture.SitBeatSeconds delays a card and never swallows one. That is what
        // makes the rule safe to apply without asking which card it is.

        /// <summary>#870 lane 6a · ARM IT. The one caller is <c>SitCaptainOn</c> in <c>Map.Surface.Frame.cs</c> —
        /// the single placement every seat kind in the game goes through — and it used to write the field
        /// directly from outside this family. The full beat, every time: re-arming a beat already running is what
        /// a second sit-down IS, and the owed time is a hold on presentation, never a budget anybody has to
        /// balance.</summary>
        public void OweTheSitBeat() => SitBeatOwedSeconds = SeatedPosture.SitBeatSeconds;

        /// <summary>#865 · Spend the beat. Called once from the surface tick with the frame's own dt, which is
        /// the same clock everything else down here is paid out of.</summary>
        public void SpendTheSitBeat(double dtRealSeconds)
        {
            if (SitBeatOwedSeconds > 0.0)
            {
                SitBeatOwedSeconds = Math.Max(0.0, SitBeatOwedSeconds - Math.Max(0.0, dtRealSeconds));
            }
        }

        /// <summary>
        /// #784/#793 · WHICH RUNG OF THE EXPOSURE LADDER THE CAPTAIN IS SITTING ON, or null on their feet.
        ///
        /// <para>Off the room's own facts and never a client opinion: <c>Quiet</c> is Core's cabinet flag
        /// (<see cref="CanteenRegulars.TableSeat.Quiet"/>, the same bit <see cref="SittingAlone.SomebodyComes"/>
        /// refuses on), and a stool is a stool.</para>
        ///
        /// <para>#793 · AND THE BENCH RUNG IS REACHABLE NOW. It was in the enum and answered by the predicate
        /// from the day #799 shipped, waiting for a seat to point at it; the sit verb arrived at the park's
        /// benches and this is the one line it took — a flag on the sitting (<c>TableTalk.Bench</c>) rather than
        /// a second seated state, because a bench and a cabinet are the same POSTURE and a different
        /// EXPOSURE.</para>
        /// </summary>
        /// <remarks>#821 · AND THE LADDER GAINED A TOP RUNG. A cubicle with the catch over is the one seat whose
        /// privacy does not depend on anybody else's behaviour, so it is asked FIRST — and it is asked of the one
        /// set of shut doors the deck is rebuilt from (<c>ex.CubiclesShut</c>), never of a flag captured when the
        /// captain sat down, because the catch can go over while they are already sitting on it.</remarks>
        /// <remarks>#1040 · AND THE BAR-STOOL RUNG IS REACHABLE FROM A TABLE-SHAPED SITTING NOW. The ship's
        /// own counter (owner: <i>"Our on ship bar can be upgraded to match the other bars"</i>) seats the
        /// captain through the one method every seat is opened through, so the sitting it builds is a
        /// <c>TableTalk</c> like every other — and a stool on that counter must land on the rung a stool
        /// lands on, which is where the gumshoe rule refuses the spread out loud. One flag
        /// (<c>TableTalk.Stool</c>), read here, exactly as <c>Bench</c> is; NOT a second predicate, and not a
        /// second exposure ladder. It is asked before <c>Quiet</c> because a counter is never private and a
        /// room that claimed both would have to be answered somewhere.</remarks>
        public SeatedHud.Seat? SeatedIn =>
            Table is { } t
                ? t.CubicleKey is { Length: > 0 } wc && _host.CubicleIsShut(wc) ? SeatedHud.Seat.LockedCubicle
                    : t.Bench ? SeatedHud.Seat.ParkBench
                    : t.Stool ? SeatedHud.Seat.BarStool
                    : t.Quiet ? SeatedHud.Seat.Cabinet : SeatedHud.Seat.HallTable
            : Stool is not null ? SeatedHud.Seat.BarStool
            : null;

        /// <summary>#784 · Has the captain got this seat to themselves? At a table that is the chair opposite
        /// being empty (#757's own Solo); at the counter it is
        /// <see cref="Core.Interior.TheStools.HasNeighbour"/> — the exposure fact #789 already shipped, asked
        /// rather than re-derived.</summary>
        public bool SeatedAlone
        {
            get
            {
                if (Table is { } t)
                {
                    // #793 · TWO WAYS TO LOSE THE SEAT TO YOURSELF, and they are not the same fact. At a table
                    // it is the chair opposite (#757's Solo, which is also whether this is a CONVERSATION). On a
                    // bench it is the far end of the plank — somebody who has started no conversation at all
                    // and is still close enough to read what is on your knee. Solo alone would have called a
                    // shared bench private; SharedSeat alone would have called a table with a face across it
                    // private. Both, asked together, because the ladder's question is "can anybody read this".
                    return t.Solo && !t.SharedSeat;
                }
                if (_host.Surface is { } ex && Stool is { } s)
                {
                    return !s.WithNeighbour
                        && !Core.Interior.TheStools.HasNeighbour(
                            ex.Stop.Body.Id, ex.Floor, s.Index, ex.CanteenWatch);
                }
                return false;
            }
        }

        /// <summary>
        /// #784 · THE CUSTOMER LINE — the seated strip's one instrument row, in the AIR gauge's register.
        ///
        /// <para>Owner: <i>"maybe the HUD could have place for our current state as customer of the restaurant,
        /// like we have really superb UI for the air use."</i> Every figure on it is handed to
        /// <see cref="SeatedHud.CustomerLine"/> from the SAME member the mechanic runs on —
        /// <see cref="PourSecondsLeft"/> is the window <see cref="ShortRest"/> doubles its rate off,
        /// <c>ex.RestPipsEased</c> is the ledger <see cref="RestOneSeatedBeat"/> writes, and
        /// <see cref="SittingAlone.Fill"/> is the fraction the approach roll is scaled by. There is no second
        /// copy of any of them for this line to drift away from (#740).</para>
        /// </summary>
        public string? SeatedCustomerLine()
        {
            if (_host.Surface is not { } ex || SeatedIn is not { } seat)
            {
                return null;
            }
            ex.RestPipsEased.TryGetValue(ex.CanteenWatch, out int pips);
            return SeatedHud.CustomerLine(
                seat, _host.PourSecondsLeft, pips, ShortRest.NervePipCapPerWatch,
                SittingAlone.Fill(ex.CanteenWatch),
                // #865 · …and one clause knows about the company. "YOUR OWN TABLE" printed while the weighbridge
                // clerk eats opposite would be the strip and the room disagreeing about whose top this is.
                SeatedWithCompany,
                // #758 · …and the room clause knows which stage the leaf is standing at. A strip saying "the
                // door is shut" to a captain sitting behind cloth is the same fault, one clause along.
                CabinetStage);
        }

        // ── PRIVACY GATES THE SPREAD ──────────────────────────────────────────────────────────────────────

        /// <summary>#784/#793 · May the case be spread out at this seat? Core's one predicate
        /// (<see cref="SeatedSpread.CanSpreadTheCase"/>) asked with the two facts this file owns.</summary>
        public bool CanSpreadTheCaseHere =>
            SeatedIn is { } seat && SeatedSpread.CanSpreadTheCase(seat, SeatedAlone);

        /// <summary>
        /// #784 · IS THE CAPTAIN IN A SEAT OF ANY KIND — a table, a cabinet, a stool at the counter.
        ///
        /// <para>Deliberately NOT <see cref="CaptainIsSeated"/>, which is #788's TABLE flag and is wired to the
        /// things a table decides: the drawn posture and the stand-up confirm. A stool is a posture of the
        /// COUNTER card (#789's one-E-per-fixture rule) and has never been in that flag.</para>
        ///
        /// <para>What the spread needs is the other question — <i>am I in a seat, and which one</i> — and the
        /// answer to it is <see cref="SeatedIn"/> being non-null. This member exists so that reading is named
        /// rather than spelled out at each use.</para>
        ///
        /// <para>#847 · AND IT IS THE MOVEMENT LAW'S FLAG NOW. This docblock used to say that widening the seated
        /// answer to the bar "would silently change what WASD does at the counter, which is a bigger claim than
        /// this issue is making" — and it was right to leave the claim to somebody with the authority to make it.
        /// The owner made it: a stool is a seat, and you get off a seat before you walk. So the frame-level stop
        /// in <c>MoveAvatar</c> asks THIS question rather than the table's, which is what closed the gap the
        /// #820 snap made visible — the dot walking along the counter out of a scene that still had it seated.
        /// The DRAWN posture is still the table's flag, because that is a question about a picture and nobody has
        /// ruled on the bar's.</para>
        /// </summary>
        public bool CaptainIsSeatedAnywhere => SeatedIn is not null;

        /// <summary>#784 · Why not, said out loud — the posture refusal on your feet, the privacy refusal in the
        /// wrong seat, null when the papers may come out. One ladder, asked top-down: standing beats seating,
        /// because a captain who is not in a seat at all has not yet reached the question about WHICH seat.</summary>
        public string? SpreadRefusal =>
            SeatedPosture.RefusalIfStanding(CaptainIsSeatedAnywhere)
            ?? (SeatedIn is { } seat ? SeatedSpread.RefusalAt(seat, SeatedAlone) : null);
    }
}
