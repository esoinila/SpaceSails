using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Part of Map.Surface (#870 split; the header note lives in Map.Surface.cs) — processing the loot, and the wallet that comes out all at once.
public partial class Map
{
    // ── #696 · THE DARKROOM: PROCESSING THE LOOT TAKES TIME, AND THE AIR PRICES THE WHERE ───────────────
    //
    // Owner, mid-run: "How is our detective notebook / picture taking progressing for our ability to process
    // the files etc so we don't need carry them. That is something one would do without using tanked air. It
    // is good game mechanic... we take time to process the loot."
    //
    // Read the whole of Core.Processing for the design. What this half does is three things and no more:
    // start a hold, advance it while the boots stay put, and — at the far end — call the effect the game
    // ALREADY HAD. Nothing here touches the tank. The hold passes sim time; StepSuitAir prices sim time on
    // whatever ground the captain chose to stand on; the decision "read it here or haul it to the shelter"
    // falls out of two systems that have never heard of each other. That is the whole ruling.
    //
    // THE SATCHEL SHUTS ON THE WAY IN, and it is the one judgement call in this lane worth arguing about.
    // #691's leave verb kept the dialog open on purpose (you put a thing down in order to pick a thing up)
    // and that was right while the drop was instantaneous. It is wrong the moment the drop has a body: the
    // teeth of this mechanic are twenty seconds of being STATIONARY AND VISIBLE, and a captain cannot watch
    // a motion tracker through a backdrop blur. So the pocket closes, the fan comes back, and the bar fills
    // over the captain's own mark. #680's law is not "say it in the dialog" — it is "say it where the player
    // is looking", which is what SayItWhereTheyAreLooking asks every time.

    /// <summary>#696 · How long one document takes, right now. The Core constant, unless the QA cheat has
    /// switched the clock off — and it is a PROPERTY rather than four call sites, so the sim, the bar, the
    /// keybar hint and the satchel's own leave hint can never be running different numbers.</summary>
    private double ProcessingSeconds => _processCheatSeconds ?? Core.Processing.SecondsPerDocument;

    /// <summary>
    /// #696 → #1016 · <b>THE ONE HOLD, AND IT IS THE PAGE'S NOW.</b>
    ///
    /// <para>It rode the <c>SurfaceExcursion</c> from #696 until the owner's ruling of 2026-08-30:
    /// <i>"Maybe it might be good idea to refactor the working the case etc table options to not be tied to
    /// any location? Kind of clean separation from the arriving random encounters that are more place tied
    /// events."</i> The eighth seat (#973 L5b — a top in a docked station's bar) has no excursion, so a hold
    /// that lived on one could not exist there: <c>OpenTheSpread</c> returned on its first line, the button
    /// was live and dead, and the owner pressed <b>Work the case</b> in The Stormwatch Bar and got nothing.
    /// </para>
    ///
    /// <para><b>Still exactly one clock.</b> Moving the field is the whole of the change — every reader and
    /// every writer in the game asks this member, the arithmetic is still <see cref="Core.Processing"/>'s,
    /// the bar is still the one bar, and the excursion-only costs stay excursion-only because they were never
    /// in here to begin with (see <c>TheDarkroomHasNeverHeardOfTheTank</c>: the air prices sim time out on
    /// the ground and prices nothing in a pressurised berth, without one line of this file knowing it).</para>
    ///
    /// <para>Never saved. A half-photographed sheet is not a possession — what IS durable is the register at
    /// the far end (<c>_workedUp</c>), which is the thing this issue made a fact about the case.</para>
    /// </summary>
    private ProcessingHold? _processing;

    /// <summary>#1016 · IS A BAR ALREADY FILLING UNDER THE CAPTAIN'S HANDS? The ground's channels
    /// (<see cref="SurfaceExcursion.AnyChannel"/> — a dig, a door-force, a drill) OR the one darkroom hold,
    /// which is no longer one of them because it is no longer the ground's. Every [E] that starts a slow
    /// thing asks THIS, so the mutual exclusion #562 wrote is still one question with one answer on every
    /// ground the captain can stand on — including the ones with no ground at all.</summary>
    private bool AnySlowThingUnderYourHands =>
        _processing is not null || _surface is { AnyChannel: true };

    /// <summary>#1016 · Which floor the boots are on, for the one comparison a running hold makes. Under a
    /// moon it is the excursion's; a berth deck and the captain's own boat are not floors of a building, and
    /// they answer zero exactly as the docked bar's own walkers do. Stated once, so the hold's anchor and
    /// the check against it cannot be measured off two different ideas of a floor.
    ///
    /// <para>#1253 · …and a BERTH answers its own floor now, which it could not before one station grew a
    /// level under its concourse. The hold's whole claim is <i>you have not moved off the ground you started
    /// this on</i>, and a captain who rode a car with a document half-processed has moved off it as
    /// completely as one who rode a lift under a moon. It reads <c>_havenFloor</c> — the same field the
    /// docked bar's own walkers are keyed on — and it is still zero everywhere it has always been zero, so
    /// no hold anywhere else in the game learns a thing.</para></summary>
    private int TheFloorUnderfoot => _surface?.Floor ?? _havenFloor;

    /// <summary>#696 · Take the document out and start the clock. The item is NOT removed and nothing is
    /// filed: everything happens at the far end, so an interruption has nothing to undo.</summary>
    private void BeginProcessing(
        Core.Processing.Work work,
        Core.Satchel.Item item,
        string standing,
        (SatchelTry.Target Target, string? Context, string Label)? at)
    {
        string label = SatchelLabel(item);

        // One pair of hands. The satchel shuts on the way in, but the captain can open it again with I and
        // press the row a second time — and a control that does nothing and says nothing is indistinguishable
        // from a bug (#603), so the refusal is a sentence.
        if (_processing is { } already)
        {
            SayItWhereTheyAreLooking(Core.Processing.AlreadyBusyLine(already.Work, already.Label));
            return;
        }

        // And a shovel already in the ground is the same objection wearing a different glyph: every channel
        // on this surface draws the ONE progress bar (#562), so two at once is a captain watching a clock
        // that belongs to something else. The dig, the door-force and the drill all ask this same property
        // before they start. (#1016 · the property counts the hold from one level up now — the hold is the
        // page's, so a ground that has none still answers for the ground's own channels.)
        if (AnySlowThingUnderYourHands)
        {
            return;
        }

        var hold = new ProcessingHold
        {
            Work = work,
            Item = item,
            Label = label,
            AnchorX = _avatarX,
            AnchorY = _avatarY,
            Floor = TheFloorUnderfoot,
            Standing = standing,
            At = at,
        };
        _processing = hold;

        // The pocket goes away so the fan comes back. See the note above — this is the one place the #691
        // "satchel stays open" call is deliberately reversed, and the reason is that the vulnerability IS
        // the mechanic.
        CloseSatchel();

        // ?process=0 · the QA switch. A story test must not have to wait out a clock designed to be felt,
        // and a zero-length hold is finished the instant it starts rather than one frame later — a frame is
        // a tick of air on the regolith, and a cheat that quietly charged for it would make the very guard
        // this lane ships flap. It also skips the "hold position" line, because a build that tells the
        // captain to stand still and then does not is a build whose prose has stopped being true.
        if (ProcessingSeconds <= 0)
        {
            CompleteProcessing(hold);
            return;
        }

        ShowPulseMessage(Core.Processing.StartLine(work, label, ProcessingSeconds));
        RendererInterop.PlayCue("blip");
    }

    /// <summary>#696 · Advance the hold. Stepping off the spot — or riding the lift to another floor —
    /// abandons it; filling the clock fires the effect.
    ///
    /// <para>There is no air arithmetic in this method and there must never be any. StepSurface calls
    /// StepSuitAir on the same tick with the same dt, whatever this returns, so the hold is priced by where
    /// the captain is standing without one line here knowing that a tank exists.</para>
    ///
    /// <para>#1016 · It is stepped from TWO frames now, and never from both on one tick: the surface tick
    /// (after the tank, see StepSurface) whenever there IS an excursion, and the walked frame's
    /// no-excursion branch (Map.Sim.Tick, beside the sit beat, which was split ashore for the same reason in
    /// #973 L5b) when there is not. A berth has no suit to charge, which is why the ordering law that governs
    /// the first call site has nothing to say about the second.</para></summary>
    private void StepProcessing(double dtRealSeconds)
    {
        if (_processing is not { } hold)
        {
            return;
        }

        if (TheFloorUnderfoot != hold.Floor
            || Core.Processing.Wandered(hold.AnchorX, hold.AnchorY, _avatarX, _avatarY))
        {
            AbandonProcessing(Core.Processing.Interruption.Walked);
            return;
        }

        hold.Elapsed += dtRealSeconds;
        if (Core.Processing.Done(hold.Elapsed, ProcessingSeconds))
        {
            CompleteProcessing(hold);
        }
    }

    /// <summary>#696 · The far end. The hold clears FIRST, so the effect it fires runs in a world with no
    /// hold in it — a leave that re-entered <see cref="LeaveItem"/> would otherwise meet its own clock and
    /// refuse itself.</summary>
    private void CompleteProcessing(ProcessingHold hold)
    {
        _processing = null;
        RendererInterop.PlayCue("board");

        // #784 · THE SEATED REGISTER'S FAR END. Same hold, same bar, same twenty seconds — a different
        // ending, because what a table buys is not a better fact: it is the sheet still being in your pocket
        // afterwards. So this returns WITHOUT calling SetItDown, which is the entire difference between
        // digging a paper out at a table and photographing it to leave it on the ground.
        //
        // #1016 · …and it is the ONE arm of this far end that asks for no ground. A write-up is a captain, a
        // chair and a sheet; the three below all name something that is only out there (a bucket, a bulkhead
        // with a reader on it, a square of regolith to put a thing down on), so they keep the excursion they
        // have always needed and simply do not fire in a berth.
        if (hold.Work == Core.Processing.Work.Write)
        {
            TheWriteUpLands(hold.Item, hold.Standing);
            return;
        }

        // #828 · THE SECURE RUNG'S FAR END. Same hold, same bar, same seconds — and an ending that consumes
        // the sheet, because the whole of what the top rung sells is that the captain STOOD THERE while it
        // went. It returns without SetItDown for the same reason the seated register does: nothing is being
        // put on the ground here. Nothing about the destruction is re-implemented at this end either — the
        // one act (Map.Bin.cs → TheSheetIsGone) is called, exactly as the three unwatched rungs call it.
        if (hold.Work == Core.Processing.Work.Shred)
        {
            TheDestructionWasWatched(hold.Item);
            return;
        }

        if (hold.Work == Core.Processing.Work.Read && hold.At is { } at)
        {
            // #603/#697 · Exactly the ending a press has always had, with exactly the arguments the press
            // would have passed. Not a copy of it — the same method — because a hand-written second ending
            // is this repo's first named bug class aimed at a state transition.
            TheOfferIsAnswered(SatchelTry.Offer(hold.Item, at.Target, at.Context), hold.Item, at);
            return;
        }

        if (_surface is { } ex)
        {
            SetItDown(ex, hold.Item, hold.Standing);
        }
    }

    /// <summary>#696 · Cancel honestly. Nothing filed, nothing consumed, the paper still in the sleeve — and
    /// ONE line saying so, because a twenty-second investment that evaporates in silence reads as a lost
    /// press rather than as a decision the world took away from you.</summary>
    /// <summary>#1016 · End the hold <b>only if it is this kind of work</b>, out loud. The seat family's
    /// three privacy seams — standing up, somebody taking the chair opposite, somebody taking the far end of a
    /// plank — end a DIG and nothing else: a leave or a shredding is not privacy being revoked.
    ///
    /// <para>They used to make that discrimination by reading <c>Surface.Processing.Work</c>, which is a
    /// reach through a GROUND into a clock, and it was a reach that answered "no dig" at the one seat with no
    /// ground under it (#973 L5b's bar top) — so a captain who stood up mid-sheet in a berth lost their
    /// twenty seconds in silence. One overload instead, so the seat asks for an ANSWER rather than for the
    /// machinery, and the family's ask on <see cref="ISeatHost"/> stays exactly the size it was.</para></summary>
    private void AbandonProcessing(Core.Processing.Work only, Core.Processing.Interruption why)
    {
        if (_processing is { Work: { } running } && running == only)
        {
            AbandonProcessing(why);
        }
    }

    /// <inheritdoc cref="AbandonProcessing(Core.Processing.Work, Core.Processing.Interruption)"/>
    private void AbandonProcessing(Core.Processing.Interruption why)
    {
        if (_processing is not { } hold)
        {
            return;
        }

        _processing = null;
        SayItWhereTheyAreLooking(Core.Processing.AbandonedLine(hold.Work, hold.Label, why));
    }

    /// <summary>#696 · What the satchel says while a hold runs, or null when nothing is under the captain's
    /// hands. Composed in Core so the dialog and the standing prompt cannot grow two vocabularies for one
    /// clock.</summary>
    private string? ProcessingUnderway() => _processing is { } hold
        ? Core.Processing.HoldLine(hold.Work, hold.Label,
            Core.Processing.SecondsLeft(hold.Elapsed, ProcessingSeconds))
        : null;

    /// <summary>#784 · HOW FAR THE DIG HAS GOT, 0..1, or null when nothing is under the captain's hands.
    ///
    /// <para>Owner, live on the phase-2 build: <i>"the progress bar is kind of small there… it might be good
    /// to have it on the dialog… took me a while to notice it."</i> The rectangle on the deck rides the
    /// DeckView idiom and is honest at a glance from across the hall — but a seated captain is looking at the
    /// DOCKED STRIP, and a clock drawn where nobody is looking is #782's readability law failing at time
    /// rather than at type.</para>
    ///
    /// <para>It is <see cref="Core.Processing.Fraction"/> — the SAME call the deck rectangle is fed by (see
    /// <c>DigProgress</c> in the surface HUD) — so the strip's bar and the deck's rectangle cannot come to
    /// disagree about how far along one dig is. Two arithmetics for one clock is this repo's two-clocks
    /// class, and it is cheaper to not have than to guard.</para>
    ///
    /// <para>#1016 · AND IT IS THE ONLY BAR A DOCKED BERTH HAS. There is no <c>SurfaceHud</c> off an
    /// excursion — <c>BuildSurfaceHud</c> returns null on its first line — so the deck rectangle the surface
    /// dig also wears simply is not drawn in a station bar, and manufacturing a hud to carry it would light
    /// the moon's instruments (the motion fan, the nerve gauge, the regolith keybar) inside The Stormwatch
    /// Bar. What the seated captain gets is the read the owner asked for when he asked for this bar at all —
    /// <i>"it might be good to have it on the dialog… took me a while to notice it"</i> — at the strip's own
    /// width, fed by the same <see cref="Core.Processing.Fraction"/>. Same clock, same fraction, same
    /// markup.</para></summary>
    private double? ProcessingFraction() => _processing is { } dug
        ? Core.Processing.Fraction(dug.Elapsed, ProcessingSeconds)
        : null;

    /// <summary>#696 · What the 🫳 control promises before it is pressed. A DOCUMENT costs seconds, because
    /// leaving one means photographing it first (#691); anything else is set down and that is all. The
    /// question "is this a document" is <see cref="LeftBehind.GistOf"/>'s — the SAME call
    /// <see cref="LeaveItem"/> branches on — so the hint and the press can never disagree about which rows
    /// have a clock behind them.</summary>
    private string LeaveHintFor(Core.Satchel.Item item) =>
        _surface is not null && LeftBehind.GistOf(item, WhereYouAreStanding()) is { Length: > 0 }
            ? Core.Processing.LeaveHint(ProcessingSeconds)
            : "Leave it here — it stays where you put it";

    /// <summary>#696 · The interruptions the hold cannot see coming, routed from the systems that CAN.
    ///
    /// <para>An air alarm breaks it on purpose. #564's founding rule is that air must never be a silent timer
    /// that kills you, and a warning that fires while the captain is watching a progress bar fill is exactly
    /// that timer wearing a costume — so the bar goes away and the alarm has the screen to itself. It fires
    /// once per walk per threshold (the warnings are one-shot), so it is a beat and never a lockout: the
    /// captain may start the same hold again on the next press and finish it on the reserve if that is the
    /// decision they want to take.</para></summary>
    private void ProcessingIsInterrupted(Core.Processing.Interruption why) => AbandonProcessing(why);

    // ── #697 · THE WALLET IS ONE THING, AND IT COMES OUT ALL AT ONCE ────────────────────────────────────
    //
    // Owner: "Let's also add option to try all ID cards ... by grouping them into a folder in the inventory."
    // And, on the register the answer is written in: "It is a little throw at the movie ... where he had this
    // wallet with zillion different contradictory IDs :-D"
    //
    // A captain who has worked three sites is carrying several authorities that disagree about who they work
    // for, and holding them up used to be four presses producing four sentences of which one was worth
    // reading. The fold is Core's (SatchelTry.OfferWallet, #683's ladder); everything here is the gesture.

    /// <summary>#697 · Whether the wallet is open on the CARRIED page. Folded shut on every open
    /// (<see cref="TheSatchelOpensOnThePocket"/>) — the cards are one row until the captain asks for them.</summary>
    private bool _walletOpen;
}
