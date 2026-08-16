using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #784 · SITTING DOWN IS A STATE THE GAME CAN SEE — phase one.
///
/// <para>Owner, live 2026-08-08, three rulings in the same minutes over the #778 table:</para>
///
/// <list type="bullet">
/// <item><i>"Let's make the graphics say I am sitting down at the avatar level — like different graphics
/// etc."</i> → <see cref="CaptainIsSeated"/> rides to <see cref="Rendering.DeckView.State"/> and the deck
/// draws a different figure. A glance says sitting, with no panel text involved.</item>
/// <item><i>"before moving I have to stand up… so if I try to move when sitting down it should ask with a
/// pop-up whether I want to stand up again."</i> → <see cref="Seating.StandUpAsk"/>.
/// <para><b>#847 SUPERSEDES THE FIRST HALF OF THAT.</b> Owner, 2026-08-13: <i>"Must stand up before walking
/// … the keys simply cost you the stand first, which is how chairs work."</i> A movement input no longer
/// ASKS — it pays, through <see cref="StandUpBeforeWalking"/>, at every seat kind including the counter
/// stool that was never in the table flag at all. The confirm itself stays exactly where the second ruling
/// put it: Esc on a DOCKED strip (Map.Sim's cancel chain), where "cancel" has no card to take and would
/// otherwise silently spend the watch you sat for.</para></item>
/// <item><i>"Sitting down relaxes and heals"</i> / <i>"it is like short rest in TTRPG."</i> →
/// <see cref="RestOneSeatedBeat"/>, which is <see cref="ShortRest"/>'s arithmetic spent through the nerve
/// and condition systems that already exist.</item>
/// </list>
///
/// <h3>Deliberately NOT here (phase two, and other people's lanes)</h3>
///
/// <para>The live world behind the panel, the walker who crosses the hall for real (#731), and the spread /
/// thread-drawing tabletop (#741) are all the modal frame coming off, and none of them is in this file. What
/// IS here is everything that does not need the walker machinery — which is why the seated FLAG is a
/// property other lanes can read rather than a private field: <see cref="CaptainIsRestingAtATable"/> is the
/// one question "is the captain resting" has an answer to, and #783's panel picks its state image off it
/// rather than re-deriving a second answer.</para>
///
/// <para>The write gate is a PREDICATE and the write itself is one item at a time, on purpose: the owner's
/// phase-two addendum puts a per-item timer bar in the digging idiom in front of exactly this act, and a
/// gate written as "do the whole satchel now" would have had to be taken apart first.</para>
/// </summary>
public partial class Map
{
    /// <summary>#784 QA · <c>?hurt=N</c> — how many of <see cref="CaptainCondition.MaxHits"/> the captain
    /// steps out of the boat already carrying. Set in Map.Sim's cheat parse; read once, where an excursion's
    /// blow count begins. Null off the cheat, in which case a captain lands unmarked exactly as before.
    /// <para>It never seeds the fifth blow: booting a tester straight into a death card is not a demo.</para></summary>
    private int? _hurtCheat;

    // ── THE POUR IN FRONT OF YOU ──────────────────────────────────────────────────────────────────────

    /// <summary>How long after a pour the glass is still a glass, in real seconds. The counter is a walk
    /// away from the tables (#756 put them in one room), so this has to outlast crossing the hall and
    /// choosing a top; it is not the 90 s SPREE window <see cref="PourRum"/> keeps, which is a different
    /// question about a different thing. FLAGGED for the owner's tuning.</summary>
    private const double DrinkInHandSeconds = 300.0;

    /// <summary>
    /// #784 · HOW MUCH OF THE POUR WINDOW IS LEFT, in real seconds — or null when there is no bought pour in
    /// front of the captain at all. The one discoverable fact #756's counter leaves behind is the tot
    /// (<see cref="PourRum"/> is the single funnel every purchase in the game goes through), so this reads
    /// that and nothing else.
    ///
    /// <para>Two honest gaps, both filed rather than faked. FOOD does not route through <c>PourRum</c> (a
    /// fry-up does not tilt the deck, #756), so a meal buys no multiplier today. And a DRUNK captain gets
    /// none either: <see cref="NerveModel.DrunkAt"/> is the game's one drunkenness law and it already says a
    /// third tot stops helping — a rest is not the place to invent a second opinion about that.</para>
    ///
    /// <para>#784 phase two · IT IS THE FIGURE AND THE FLAG AT ONCE, and that is deliberate. The customer
    /// line on the seated strip prints the minutes left; the rest engine doubles its rate off the same
    /// window. Written as two members they would be two clocks, and a strip saying "2m of cold left" over a
    /// beat the rest engine had already decided was dry is #740's fault exactly — so there is one member and
    /// the boolean is derived from it.</para>
    /// </summary>
    private double? PourSecondsLeft
    {
        get
        {
            if (_rumTots <= 0 || NerveModel.DrunkAt(_rumTots))
            {
                return null;
            }
            double leftMs = (DrinkInHandSeconds * 1000.0) - ((_lastTimestampMs ?? 0) - _lastRumMs);
            return leftMs > 0 ? leftMs / 1000.0 : null;
        }
    }

    /// <summary>#784 · Is there a bought pour in front of the captain? Derived, never re-measured.</summary>
    private bool APourInFrontOfYou => PourSecondsLeft is not null;

    // ── THE SHORT REST ────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #784 · ONE SEATED WATCH-BEAT OF SHORT REST.
    ///
    /// <para>Called from the WAIT beat and from nowhere else, because the wait IS the beat: #757 already
    /// decided that holding a table advances a counter the room keeps, and hanging the rest off a second
    /// clock would make "how long have you been sitting there" two different numbers.</para>
    ///
    /// <para>The ledger is per WATCH and not per table — <see cref="ShortRest.NervePipCapPerWatch"/> is a
    /// ceiling on the SHIFT, so a captain who hops from top to top to reset it would be re-pressing their
    /// way out of a cap, which is the same abuse #757 closed on the approach roll.</para>
    ///
    /// <para>Both halves are spent through the systems that already own them: the nerve through
    /// <see cref="ApplyNerveRelief"/> (whole pips, named in the ledger — #480 forbids moving the gauge
    /// anonymously), the blow through the excursion's own <c>HitsTaken</c>, which is the number the
    /// condition marker, the block roll and the breathing rate all read.</para>
    /// </summary>
    /// <returns>The body's footnote to this beat, or null when it had nothing to say.</returns>
    private string? RestOneSeatedBeat(SurfaceExcursion ex, int beatsAlreadySat)
    {
        long watch = ex.CanteenWatch;
        ex.RestPipsEased.TryGetValue(watch, out int pipsSoFar);
        ex.RestHitsKnit.TryGetValue(watch, out int hitsSoFar);

        bool drink = APourInFrontOfYou;
        ShortRest.Eased eased = ShortRest.Beat(
            beatsAlreadySat, drink, pipsSoFar, hitsSoFar, ex.HitsTaken);

        if (eased.NervePips > 0)
        {
            ex.RestPipsEased[watch] = pipsSoFar + eased.NervePips;
            ApplyNerveRelief(eased.NervePips * NervePips.PipUnit);
        }
        if (eased.Hits > 0)
        {
            ex.RestHitsKnit[watch] = hitsSoFar + eased.Hits;
            ex.HitsTaken = Math.Max(0, ex.HitsTaken - eased.Hits);
        }

        return ShortRest.Line(in eased, drink);
    }

    /// <summary>#784 · The room's answer, with the body's footnote after it when the beat gave something
    /// back. One sentence and then one clause: the silence at your table is the EVENT (#757) and it keeps
    /// the lead, because a rest that pushed in front of the room's own answer would be the mechanic talking
    /// over the scene.</summary>
    private static string WithTheBodysFootnote(string saidByTheRoom, string? saidByTheBody) =>
        saidByTheBody is { Length: > 0 } ? $"{saidByTheRoom} {saidByTheBody}" : saidByTheRoom;

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
    private void AskWhetherToStandUp()
    {
        if (!_seating.StandUpAsk)
        {
            _seating.StandUpAsk = true;
            RendererInterop.PlayCue("reveal");
            StateHasChanged();
        }
    }

    /// <summary>Esc, the ✕, and the "stay where you are" button all come through here — keeping your seat is
    /// one act however you do it, and it is free.</summary>
    private void KeepYourSeat()
    {
        _seating.StandUpAsk = false;
        StateHasChanged();
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
    private void StandUpFromTable()
    {
        _seating.StandUpAsk = false;
        if (_seating.Table is null)
        {
            return;
        }
        CloseTable();
        ShowPulseMessage(SeatedPosture.StoodUpToWalkLine);
        StateHasChanged();
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
    private bool StandUpBeforeWalking()
    {
        bool stood = false;

        if (_seating.Stool is not null)
        {
            GetDownFromStool();
            stood = true;
        }

        if (_seating.Table is not null)
        {
            StandUpFromTable();
            stood = true;
        }

        return stood;
    }

    /// <summary>Whether the confirm should say what standing up COSTS. Only when there is something left to
    /// lose — a captain who has taken everything a short rest has is not being warned about anything.</summary>
    private bool StandingUpCostsARest =>
        _surface is { } ex
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
    private void OweTheSitBeat() => _seating.SitBeatOwedSeconds = SeatedPosture.SitBeatSeconds;

    /// <summary>#865 · Spend the beat. Called once from the surface tick with the frame's own dt, which is
    /// the same clock everything else down here is paid out of.</summary>
    private void SpendTheSitBeat(double dtRealSeconds)
    {
        if (_seating.SitBeatOwedSeconds > 0.0)
        {
            _seating.SitBeatOwedSeconds = Math.Max(0.0, _seating.SitBeatOwedSeconds - Math.Max(0.0, dtRealSeconds));
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
    private SeatedHud.Seat? SeatedIn =>
        _seating.Table is { } t
            ? t.CubicleKey is { Length: > 0 } wc && CubicleIsShut(wc) ? SeatedHud.Seat.LockedCubicle
                : t.Bench ? SeatedHud.Seat.ParkBench
                : t.Quiet ? SeatedHud.Seat.Cabinet : SeatedHud.Seat.HallTable
        : _seating.Stool is not null ? SeatedHud.Seat.BarStool
        : null;

    /// <summary>#784 · Has the captain got this seat to themselves? At a table that is the chair opposite
    /// being empty (#757's own Solo); at the counter it is
    /// <see cref="Core.Interior.TheStools.HasNeighbour"/> — the exposure fact #789 already shipped, asked
    /// rather than re-derived.</summary>
    private bool SeatedAlone
    {
        get
        {
            if (_seating.Table is { } t)
            {
                // #793 · TWO WAYS TO LOSE THE SEAT TO YOURSELF, and they are not the same fact. At a table
                // it is the chair opposite (#757's Solo, which is also whether this is a CONVERSATION). On a
                // bench it is the far end of the plank — somebody who has started no conversation at all
                // and is still close enough to read what is on your knee. Solo alone would have called a
                // shared bench private; SharedSeat alone would have called a table with a face across it
                // private. Both, asked together, because the ladder's question is "can anybody read this".
                return t.Solo && !t.SharedSeat;
            }
            if (_surface is { } ex && _seating.Stool is { } s)
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
    private string? SeatedCustomerLine()
    {
        if (_surface is not { } ex || SeatedIn is not { } seat)
        {
            return null;
        }
        ex.RestPipsEased.TryGetValue(ex.CanteenWatch, out int pips);
        return SeatedHud.CustomerLine(
            seat, PourSecondsLeft, pips, ShortRest.NervePipCapPerWatch,
            SittingAlone.Fill(ex.CanteenWatch),
            // #865 · …and one clause knows about the company. "YOUR OWN TABLE" printed while the weighbridge
            // clerk eats opposite would be the strip and the room disagreeing about whose top this is.
            SeatedWithCompany);
    }

    // ── PRIVACY GATES THE SPREAD ──────────────────────────────────────────────────────────────────────

    /// <summary>#784/#793 · May the case be spread out at this seat? Core's one predicate
    /// (<see cref="SeatedSpread.CanSpreadTheCase"/>) asked with the two facts this file owns.</summary>
    private bool CanSpreadTheCaseHere =>
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
    private bool CaptainIsSeatedAnywhere => SeatedIn is not null;
    /// <summary>#784 · Why not, said out loud — the posture refusal on your feet, the privacy refusal in the
    /// wrong seat, null when the papers may come out. One ladder, asked top-down: standing beats seating,
    /// because a captain who is not in a seat at all has not yet reached the question about WHICH seat.</summary>
    private string? SpreadRefusal =>
        SeatedPosture.RefusalIfStanding(CaptainIsSeatedAnywhere)
        ?? (SeatedIn is { } seat ? SeatedSpread.RefusalAt(seat, SeatedAlone) : null);

    // ── THE FIELD BOOK'S SECOND REGISTER ──────────────────────────────────────────────────────────────

    /// <summary>
    /// #784 · WRITE IT UP PROPERLY — the deliberate entry, and the posture is the gate.
    ///
    /// <para>Owner's law: <i>"writing things down requires sitting down to be properly done."</i> Standing,
    /// the book keeps taking exactly what it has always taken — the automatic gist-once jot #696 files when
    /// a document is photographed and left behind, which is untouched by this method and by this issue. What
    /// a table buys is not a better fact: it is the SHEET STILL BEING IN YOUR POCKET afterwards, which is
    /// the difference between a scrawl on a moving knee and a copy made in your own hand.</para>
    ///
    /// <para>The refusal is SAID (#603/#680): a control that does nothing and says nothing is
    /// indistinguishable from a bug, and this one is drawn everywhere so that the law can be learned by
    /// pressing it once.</para>
    /// </summary>
    /// <summary>
    /// #784 · THE SPREAD ROW, PRESSED WITH THE MOUSE — and the way home when the press shuts the dialog.
    ///
    /// <para>The seam <c>TableMoveClicked</c> documents one file over: <i>"only the mouse needs the way
    /// home."</i> A keyboard path already owns focus; a click leaves it on the button that has just stopped
    /// existing, and the deck goes deaf — the captain presses W, or I, and nothing happens. Starting a dig
    /// CLOSES the satchel by design (#696: the vulnerability is the mechanic), so every successful press on
    /// this page is exactly that case.</para>
    ///
    /// <para><b>Found by playing it</b>, in the browser, on this build: dug a manifest out of the spread and
    /// the map took no keys at all afterwards. Same bug #746 found after "Take your leave", same fix.</para>
    /// </summary>
    private async Task SpreadDigClicked(Core.Satchel.Item item)
    {
        WriteItUp(item);

        // Only when the dialog actually went away. A press that keeps it up must NOT steal focus back, or
        // tabbing through the rows would fight the map for every key.
        if (!_showSatchel)
        {
            await RefocusMap();
        }
    }

    private void WriteItUp(Core.Satchel.Item item)
    {
        if (_surface is not { } ex)
        {
            return;
        }

        // The gates, asked of Core, in the order a captain meets them: are you sitting, and is this a seat
        // you would lay evidence out on. The client decides only what "seated" and "alone" mean; it never
        // decides what a seat is FOR.
        if (SpreadRefusal is { } refusal)
        {
            SayItWhereTheyAreLooking(refusal);
            return;
        }

        // Whether there is anything to write is the SAME question the standing register asks (#696's own
        // "is there a gist" is asked once, by Core), so the two registers can never disagree about which
        // things are documents.
        string standing = WhereYouAreStanding();
        if (LeftBehind.GistOf(item, standing) is not { Length: > 0 })
        {
            return;
        }

        if (ex.WrittenUpProperly.Contains(WrittenUpKey(item)))
        {
            SayItWhereTheyAreLooking(SeatedPosture.AlreadyWrittenLine);
            return;
        }

        // #784 phase two · AND NOW IT TAKES TIME, ON THE BAR THE GAME ALREADY DRAWS. Owner: "The inventory
        // processing should have like a timer progress bar like the digging has… we are digging for info and
        // understanding." So this does not open a second clock or a second bar — it starts the one hold
        // #696 already owns (Processing.Work.Write), which means the one channel, the one precedence ladder,
        // and the one progress rectangle over the captain's own mark. Nothing about the drawing is new.
        //
        // The entry lands at the far end, in TheWriteUpLands, and NOTHING is filed here: an interruption has
        // to have nothing to undo, which is #696's founding discipline and the reason the set is added to
        // there rather than up here where it would survive a stand-up.
        BeginProcessing(ex, Core.Processing.Work.Write, item, standing, null);
    }

    /// <summary>
    /// #784 · THE FAR END OF A SEATED WRITE-UP — the dig finishes and the book gains the entry.
    ///
    /// <para>Called only from <see cref="CompleteProcessing"/>, which clears the hold first, so this runs in
    /// a world with no clock in it. The gist is re-asked rather than carried on the hold: a document's gist
    /// is a pure function of the paper and the ground (<see cref="LeftBehind.GistOf"/>), and asking it twice
    /// is cheaper than a second copy of it that could go stale across twenty seconds of somebody else's
    /// sim.</para>
    ///
    /// <para>The line lands on the STRIP when the strip is up (<c>t.Outcome</c> is what the docked frame
    /// draws as its latest line), and pulses otherwise. That is #680's law read correctly for a frame with no
    /// backdrop in it: the rule was never "never pulse", it was "never pulse under a blur".</para>
    /// </summary>
    private void TheWriteUpLands(SurfaceExcursion ex, Core.Satchel.Item item, string standing)
    {
        // kept: true — the SEATED disposition. Same fact, one different clause. A book entry reading "read
        // and left on the floor of B1" directly under a sentence saying "the sheet goes back in the sleeve"
        // is the game reporting one thing while the sim does another; the docked strip prints both on one
        // line, where it is impossible to miss. FOUND BY LOOKING AT IT — it shipped in #788's instant write
        // and was invisible there, filed once into a book nobody had open while the outcome pulsed and went.
        if (LeftBehind.GistOf(item, standing, kept: true) is not { Length: > 0 } gist)
        {
            return;
        }
        if (!ex.WrittenUpProperly.Add(WrittenUpKey(item)))
        {
            return;
        }

        FileNote(gist, SeatedPosture.WriteGlyph);
        string said = SeatedPosture.WrittenUpLine(SatchelLabel(item), gist);
        if (_seating.Table is { } t)
        {
            t.Outcome = said;
        }
        else
        {
            SayItWhereTheyAreLooking(said);
        }
        RequestVaultSave();
    }

    /// <summary>Is the pen live on this row? A thing with no gist is not a document, and a document already
    /// in the book in your own hand is done — but a captain on their FEET, or in the wrong seat, still gets
    /// the control, because the refusal is how the law is taught.</summary>
    private bool CanWriteUp(Core.Satchel.Item item) =>
        _surface is { } ex
        && LeftBehind.GistOf(item, WhereYouAreStanding()) is { Length: > 0 }
        && !ex.WrittenUpProperly.Contains(WrittenUpKey(item));

    /// <summary>What the pen's tooltip says — the hint when it will work, the refusal when it will not, so
    /// the price of a press is known before the press (#696's own discipline, one control over).</summary>
    private string WriteUpHint(Core.Satchel.Item item) =>
        !CanWriteUp(item) ? SeatedPosture.AlreadyWrittenLine
        : SpreadRefusal ?? SeatedSpread.SpreadHint;

    /// <summary>#784 · HAS THE CASE BEGUN — is any sheet still in the sleeve already in the book? The one
    /// fact the spread door's own words switch on (owner: the button should say "we change the thing we
    /// look at" once a sheet is done): begun means <see cref="SeatedSpread.SpreadAgainLabel"/>, untouched
    /// means <see cref="SeatedSpread.SpreadLabel"/>. Read off <c>WrittenUpProperly</c> against the sleeve
    /// as it stands — a worked paper that was since binned no longer argues the case is open here.</summary>
    private bool CaseHasBegun
    {
        get
        {
            if (_surface is not { } ex)
            {
                return false;
            }
            foreach (Core.Satchel.Item item in _satchel)
            {
                if (ex.WrittenUpProperly.Contains(WrittenUpKey(item)))
                {
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>The spread door's label for the current state of the case — see <see cref="CaseHasBegun"/>.</summary>
    private string SpreadDoorLabel =>
        CaseHasBegun ? SeatedSpread.SpreadAgainLabel : SeatedSpread.SpreadLabel;

    /// <summary>…and its hint, unless a refusal outranks it.</summary>
    private string SpreadDoorHint =>
        SpreadRefusal ?? (CaseHasBegun ? SeatedSpread.SpreadAgainHint : SeatedSpread.SpreadHint);

    /// <summary>
    /// #784/#798 · THE ROWS THE SPREAD OFFERS — everything in the sleeve this table has business with.
    ///
    /// <para>It used to be <see cref="CanWriteUp"/>'s own answer, and that was right while the page had one
    /// verb on it. #798 gave it a second one, and the moment it did, the old list had a hole in exactly the
    /// place the owner's loop ends: <b>a paper you have just dug vanished off the page</b>, so <i>sit, dig,
    /// book, BIN</i> could not be done without shutting the spread and reopening the pocket. A page that
    /// hides the thing you have just worked on is a page that hides the next verb.</para>
    ///
    /// <para>So the row is offered for anything with a gist — a document, in the book's own sense — and the
    /// two controls on it answer for themselves. The dig refuses OUT LOUD when there is nothing left to dig
    /// (<see cref="SeatedPosture.AlreadyWrittenLine"/>), which is #603's rule and how the law is learned.</para>
    /// </summary>
    private List<Core.Satchel.Item> SpreadableFinds()
    {
        var found = new List<Core.Satchel.Item>();
        foreach (Core.Satchel.Item item in _satchel)
        {
            if (CanWriteUp(item) || Core.RipAndBin.IsEvidence(item.Kind))
            {
                found.Add(item);
            }
        }
        return found;
    }

    /// <summary>#798 · What the dig control says on a row whose paper is already in the book — the same
    /// truth <see cref="WriteUpHint"/> tells in the tooltip, said on the button itself, so a row that is
    /// still there for the SHREDDER's sake does not go on offering an evening's work that is already done.</summary>
    private string SpreadRowVerb(Core.Satchel.Item item) =>
        CanWriteUp(item) ? "dig it out →" : "already in the book";

    /// <summary>
    /// #784/#828 · HOW THE REGISTER NAMES ONE SHEET — the key, built once.
    ///
    /// <para>A satchel row has a kind and an id and neither alone is a document: two moons can hand out
    /// paper #3. This project has paid four times for a law transcribed at its call sites, and #828 was
    /// about to add a fifth copy of this one from the far side of the building, so the format lives here and
    /// every hand that writes the set or reads it goes through it.</para>
    /// </summary>
    private static string WrittenUpKey(Core.Satchel.Item item) => $"{item.Kind}:{item.Id}";

    /// <summary>#828 · Is this sheet already in the book in the captain's own hand? The bin picker's own
    /// question, asked of the seated register's set rather than of a second one — the dig is the only thing
    /// that puts a paper in here, and the picker only reads.</summary>
    private bool AlreadyWrittenUp(Core.Satchel.Item item) =>
        _surface is { } ex && ex.WrittenUpProperly.Contains(WrittenUpKey(item));

    // -- #870 lane 6b - THE FORWARDERS, AND THEY ARE TEMPORARY -----------------------------------------
    //
    // Fifteen members moved onto Seating in this lane, and every one of them was already being asked for BY
    // NAME - by the markup, by the cancel chain, by the surface tick, by the deck's own state hand-off.
    // Those call sites are not the subject of 6b, so not one of them changed: each name still answers here,
    // and each answer is one hop.
    //
    // They keep the accessibility they had, which is what proves nothing outside the family gained a reach
    // it did not have. #870's 6c is where this block is DELETED and the callers ask _seating directly - it
    // is one contiguous block for exactly that reason. Do not add a forwarder for a NEW question: add the
    // question to Seating and call it through _seating, the way this lane's own family verbs now do.

    /// <inheritdoc cref="Seating.CaptainIsSeated"/>
    public bool CaptainIsSeated => _seating.CaptainIsSeated;

    /// <inheritdoc cref="Seating.SeatedTable"/>
    private TableTalk? SeatedTable => _seating.SeatedTable;

    /// <inheritdoc cref="Seating.CaptainIsRestingAtATable"/>
    public bool CaptainIsRestingAtATable => _seating.CaptainIsRestingAtATable;

    /// <inheritdoc cref="Seating.SeatedOnABenchInTheOpen"/>
    private bool SeatedOnABenchInTheOpen => _seating.SeatedOnABenchInTheOpen;

    /// <inheritdoc cref="Seating.SeatedIsDocked"/>
    private bool SeatedIsDocked => _seating.SeatedIsDocked;

    /// <inheritdoc cref="Seating.SeatedIsAConversation"/>
    private bool SeatedIsAConversation => _seating.SeatedIsAConversation;

    /// <inheritdoc cref="Seating.SeatedWithCompany"/>
    private bool SeatedWithCompany => _seating.SeatedWithCompany;

    /// <inheritdoc cref="Seating.SeatedCompanyLine"/>
    private string? SeatedCompanyLine() => _seating.SeatedCompanyLine();

    /// <inheritdoc cref="Seating.SeatedOverheardLine"/>
    private string? SeatedOverheardLine() => _seating.SeatedOverheardLine();

    /// <inheritdoc cref="Seating.CaptainIsOnAStool"/>
    private bool CaptainIsOnAStool => _seating.CaptainIsOnAStool;

    /// <inheritdoc cref="Seating.SeatedStoolPlate"/>
    private string? SeatedStoolPlate => _seating.SeatedStoolPlate;

    /// <inheritdoc cref="Seating.TableMovesOnTheTable"/>
    private IReadOnlyList<Encounter.Move> TableMovesOnTheTable() => _seating.TableMovesOnTheTable();

    /// <inheritdoc cref="Seating.StoolMovesOnTheTable"/>
    private IReadOnlyList<Encounter.Move> StoolMovesOnTheTable() => _seating.StoolMovesOnTheTable();

    /// <inheritdoc cref="Seating.TheStandUpConfirmIsUp"/>
    private bool TheStandUpConfirmIsUp => _seating.TheStandUpConfirmIsUp;

    /// <inheritdoc cref="Seating.TheSitBeatIsSettling"/>
    private bool TheSitBeatIsSettling => _seating.TheSitBeatIsSettling;
}
