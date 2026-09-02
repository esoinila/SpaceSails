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
    /// they answer zero exactly as the docked bar's own walkers do (<c>BarIsNotAFloor</c>). Stated once, so
    /// the hold's anchor and the check against it cannot be measured off two different ideas of a floor.</summary>
    private int TheFloorUnderfoot => _surface?.Floor ?? 0;

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

    /// <summary>#697 · What is in the wallet, which is Core's own grouping of the pocket and never a filter
    /// written in the dialog: <see cref="Core.Satchel.CompartmentOf"/> is the law about which things are flat,
    /// and a second answer here would drift the first time a kind changes compartment (#688).</summary>
    private IReadOnlyList<Core.Satchel.Item> Wallet() =>
        Core.Satchel.OfKind(_satchel, Core.Satchel.Kind.Authority);

    /// <summary>#697 · Where the whole wallet can be offered at once: whatever the satchel is open AT, when
    /// that thing reads authorities. The question is Core's — the same <see cref="SatchelTry.CanOffer"/> the
    /// rows ask (#688) — so the folder can never carry a live offer at something the rows have gone inert
    /// for.</summary>
    private (SatchelTry.Target Target, string? Context, string Label)? WalletTarget() =>
        _satchelTarget is { } at && SatchelTry.CanOffer(Core.Satchel.Kind.Authority, at.Target) ? at : null;

    /// <summary>#697 · FAN THE WALLET AT THE READER. One press, every card, one line.
    ///
    /// <para>It performs no state transition of its own. A success ends through exactly the resolution a
    /// single successful try ends through (<see cref="TheOfferIsAnswered"/>), because a hand-written copy of
    /// that ending is this repo's first named bug class aimed at a state transition — and the day one of them
    /// learns to spend something, the other will not.</para></summary>
    private void TryTheWholeWallet()
    {
        if (WalletTarget() is not { } at)
        {
            return;
        }

        IReadOnlyList<Core.Satchel.Item> wallet = Wallet();
        if (wallet.Count == 0)
        {
            return;
        }

        // No item is charged to the fan: a wallet's success has no single row to spend, and an authority is
        // never consumed by being read anyway. Core has already decided which card answered.
        TheOfferIsAnswered(SatchelTry.OfferWallet(wallet, at.Target, at.Context), null, at);
    }

    /// <summary>#603 · Offer one carried thing to whatever the satchel is open at. The outcome is always
    /// SAID — a control that does nothing and says nothing is indistinguishable from a bug.</summary>
    private void TryItem(Core.Satchel.Item item)
    {
        if (TargetFor(item) is not { } at)
        {
            return;
        }

        // ── #696 · DECIDING A PAPER IS A MAP TAKES THE SAME SECONDS AS PHOTOGRAPHING ONE ──
        //
        // Owner's ruling covers both halves of the detective loop in one sentence, and it has to: a game
        // that charged for filing a document and handed the clue reading away free would be teaching the
        // captain to read everything on the spot and file nothing, which is the exact behaviour the cost
        // model exists to make a decision.
        //
        // The hold is started here and not inside TheOfferIsAnswered, because that method is the ENDING both
        // presses share (#697) — putting a clock inside it would put a clock in front of a wallet fan too.
        // Nothing else about the ending moves: the far end calls it with the same three arguments this line
        // would have.
        if (_surface is not null && item.Kind == Core.Satchel.Kind.Paper && at.Target == SatchelTry.Target.Tracker)
        {
            BeginProcessing(Core.Processing.Work.Read, item, WhereYouAreStanding(), at);
            return;
        }

        TheOfferIsAnswered(SatchelTry.Offer(item, at.Target, at.Context), item, at);
    }

    /// <summary>#603 · What an offer DOES once it has an answer — the one ending both presses share.
    ///
    /// <para><paramref name="item"/> is the thing that was held up, or null when the whole wallet was fanned
    /// (#697): a fan has no single row to charge, and the two consuming branches below can only ever fire for
    /// a paper or a handful of rounds, neither of which is ever in a wallet. That is what makes "no double
    /// effects" structural rather than a promise.</para></summary>
    private void TheOfferIsAnswered(
        SatchelTry.Outcome outcome,
        Core.Satchel.Item? item,
        (SatchelTry.Target Target, string? Context, string Label) at)
    {
        // ── #680 · THE ANSWER IS SAID WHERE THE PLAYER IS LOOKING ──
        //
        // Owner, live, in caps: "pressing Try IT on item produces a text that is IMPOSSIBLE to read" /
        // "it is behind the blurring effect... so we don't tell the story."
        //
        // A refusal keeps the satchel open (#614 — a captain comparing three cards should not have to
        // reopen their pockets), and this method used to pulse the line FIRST and branch after — so every
        // refusal, the exact sentences #603's law exists for, played to the HUD under the backdrop's blur.
        // The sim told the story; the z-order ate it. In the DOM is not on the screen (the owner's own
        // formulation): the one layer the backdrop cannot blur is the dialog's own subtree, so a failed
        // offer is stored for the dialog to say, and only a success — which closes the modal — pulses.
        // ── #803 · THE PUT VERB ANSWERS FOR ITSELF, BEFORE ANYTHING IS SAID ──
        //
        // Core's yes at a sentry is a yes about the KIND of thing being offered ("it takes rounds"). Whether
        // THESE rounds go into THAT drum is a question about a ceiling, a kind already loaded and whether the
        // machine is on your back or on the ground — three facts SatchelTry cannot see. So the hand-load
        // decides, here, and its answer REPLACES the generic one rather than following it: two sentences for
        // one act is how a captain ends up reading "rounds into the hopper" immediately above "there is
        // nowhere for them to go".
        if (outcome.Worked && item is { Kind: Core.Satchel.Kind.Rounds } fed
            && at.Target == SatchelTry.Target.Sentry && _surface is { } loadEx)
        {
            outcome = TheRoundsGoInByHand(loadEx, fed, at.Context, fed.Count);
        }

        if (!outcome.Worked)
        {
            _satchelOutcome = outcome.Line;
            return;
        }

        ShowPulseMessage(outcome.Line);

        // ── #603 · READING A PAPER NEVER SPENDS IT ──
        //
        // Owner: "press I ... inventory opens... select paper and see what the clue is." / "it should be
        // viewable many times."
        //
        // The first cut consumed it, which was wrong twice over. It conflated LOOKING with DECIDING — one
        // click both read the document and burned it — and it broke the field book's own law (#587: "a find
        // that is shown once is a find that is lost"). A paper is a thing you own; you can take it out and
        // read it again in a year.
        //
        // So the document is always shown, in full, every time. The tracker gets plotted on the first read
        // and GrantLabLead no-ops on every one after, which is the honest shape: the knowledge is what is
        // one-shot, not the paper.
        if (item is { Kind: Core.Satchel.Kind.Paper } read && at.Target == SatchelTry.Target.Tracker)
        {
            _viewObject = new DeckPlan.ConsoleSpot(
                DeckPlan.ConsoleKind.ViewObject, (float)_avatarX, (float)_avatarY,
                $"📋 {Core.FieldClue.Label(Core.FieldClue.CertaintyOf(read.Id)).ToUpperInvariant()}",
                "",
                Core.FieldClue.Document(read.Id) + "\n\n" + outcome.Line);

            GrantLabLead(DiceRule.Seed($"clue:{read.Id}"));
        }

        CloseSatchel();
    }

    /// <summary>
    /// #603 case 2, grown up into #803's PUT VERB · THE ROUNDS GO IN BY HAND, AND THE GUN REMEMBERS WHAT
    /// THEY WERE.
    ///
    /// <para>Six rounds is not a resupply, it is a decision — which gun, and with what. A sentry loaded with
    /// the lab round clears a line with one shot and refuses anything already on top of it; one loaded with
    /// issue ball does neither. Both facts live on the magazine, so the bot carries the kind.</para>
    ///
    /// <para><b>What #803 changed.</b> The old cut took any handful into any dry bot and wrote
    /// <c>gun.Rounds += fed.Count</c> — no ceiling, because a drum at 00 plus a hut's twenty-two could not
    /// reach ninety-nine and the bug was unreachable rather than absent. The put verb loads bots that are
    /// already part full, so the ceiling is now the whole point: a magazine the two-digit readout cannot
    /// report is the sim and the #797 instrument disagreeing about one number, which is this repo's third
    /// named bug class with a gun in its hand. <see cref="SentryHandLoad.Offer"/> owns all of it; what is
    /// left here is applying the answer.</para>
    ///
    /// <para><b>#837 · AND IT IS NOW THE WHOLE OF BOTH GRIPS.</b> The [I]-over-the-bot press and the satchel
    /// row's chooser end HERE, in this method, with a unit and a count — which is what makes the issue's law
    /// ("both grips end in the identical magazine mutation and pocket remainder for the same rounds and
    /// target") structural rather than a promise. <paramref name="rounds"/> arrived as <c>fed.Count</c> when
    /// there was only one grip and no way to load less than everything; the stepper made it a decision, so
    /// it is a parameter. Nothing else about the act moved.</para>
    /// </summary>
    private SatchelTry.Outcome TheRoundsGoInByHand(
        SurfaceExcursion ex, Core.Satchel.Item fed, string? unit, int rounds)
    {
        SurfaceBot? gun = ex.Bots.FirstOrDefault(b => b.Unit == unit);
        if (gun is null)
        {
            return new(false, "🔫 That gun is not down here any more.");
        }

        // The pocket as it stands, not as the row remembered it: a chooser stays open across a press, and a
        // stack whose Count was read two presses ago is a figure about a pocket that has since changed.
        int pocket = Core.Satchel.CountOf(_satchel, fed.Kind, fed.Id);
        int offering = Math.Clamp(rounds, 0, pocket);

        SentryHandLoad.Load load = SentryHandLoad.Offer(
            gun.Unit, gun.Rounds, gun.AmmoId, WithinHandsOf(gun), offering, fed.Id);
        if (!load.Worked)
        {
            return new(false, load.Line);
        }

        // Only what the drum TOOK leaves the pocket. The rest is still yours — the arithmetic is Core's and
        // is asserted there, so nothing here can quietly round a captain out of four rounds.
        gun.Rounds = load.Magazine;
        gun.AmmoId = load.AmmoId;
        _satchel = [.. Core.Satchel.Remove(_satchel, fed.Kind, fed.Id, load.Accepted)];
        RendererInterop.PlayCue("board");
        RequestVaultSave();

        Core.Ammunition.Kind kind = Core.Ammunition.ById(load.AmmoId);
        string line = load.Line;
        if (kind.MinimumRangeDu > 0)
        {
            line += $" It will not fire these at anything closer than {kind.MinimumRangeDu:F0} du — they " +
                "arm after travel, and that is the whole point of them.";
        }

        // #837 · …and the rounds the captain CHOSE to keep are named separately from the rounds the drum
        // would not take. Core writes both sentences; conflating them would blame a willing magazine for a
        // decision the captain made, and leave the seam between pocket and drum unaccounted for either way.
        int kept = pocket - load.Accepted;
        if (kept > load.LeftOver)
        {
            line += $" {SentryHandLoad.KeptBackLine(kept - load.LeftOver)}";
        }
        return new(true, line);
    }

    /// <summary>#837 · Can the captain's hands reach this gun? Core's amended law
    /// (<see cref="SentryHandLoad.WithinHands"/>) asked with the one fact only a running world has — how far
    /// off it is standing — and at the same arm's length the positional grip has always used. Asked in one
    /// place so the chooser's list, the chooser's hint and the act itself cannot come to three views of a
    /// reach.</summary>
    private bool WithinHandsOf(SurfaceBot gun)
    {
        double dx = gun.X - _avatarX, dy = gun.Y - _avatarY;
        return SentryHandLoad.WithinHands(
            gun.Deployed, Math.Sqrt((dx * dx) + (dy * dy)), DeckPlan.InteractRadius);
    }

    /// <summary>
    /// #803 · THE ROUNDS THE GUNS COULD NOT HOLD GO IN THE POCKET.
    ///
    /// <para>Every auto-route on this ground fills magazines in order and then stops, and until now whatever
    /// was left simply stopped existing. The receipts were not lying — they name the rounds that went IN —
    /// but a captain who watched a drawer produce twenty-two rounds into two drums that could take fourteen
    /// has been shown a thing and then had it taken away, which is the one move an object in this game is
    /// never allowed to make (#587: <i>a find that is shown once is a find that is lost</i>).</para>
    ///
    /// <para>It is also where the found-rounds item comes from at all. #603 wrote the law for rounds in a
    /// pocket, hung the door on a dry sentry and shipped — and nothing in the game ever put one there, so
    /// the whole verb was reachable only by editing a save. The overflow is the supply, and the put verb is
    /// what it is for.</para>
    ///
    /// <para>Silent when there is nothing left over, which is the ordinary case and must stay exactly as
    /// quiet as it is today.</para></summary>
    private void WhatTheDrumsCouldNotHold(int leftOver, string? ammoId = null)
    {
        if (SentryHandLoad.IntoThePocket(leftOver, ammoId) is not { } loose)
        {
            return;
        }
        _satchel = [.. Core.Satchel.Add(_satchel, loose)];
        ShowPulseMessage(SentryHandLoad.PocketedLine(loose.Count));
        RequestVaultSave();
    }

    /// <summary>#603 · What this item can be offered to right now.
    ///
    /// <para>Opened AT something — a door, a gate — everything goes to that. Opened from nowhere with the I
    /// key, most things are just a look, but a DOCUMENT can always be read as a clue, because the tracker is
    /// on the captain's arm and deciding a paper is a map is something they can do standing anywhere. That
    /// is the owner's own framing: the lead is not granted on pickup, it is granted when the player decides
    /// the paper means something.</para>
    ///
    /// <para>#688 · AT A DOOR, ONLY A KEY. Owner: <i>"Let's make a bigger story point about finding any kind
    /// of key or keycard and only suggest those at doors. Or tools, but not just like some papers."</i> The
    /// law is Core's (<see cref="SatchelTry.CanOffer"/>) and this is the one place that can route around it,
    /// so it asks. Nothing about the REFUSALS changed — a captain holding three authorities still gets every
    /// wrong-shaft and wrong-site reading #679 wrote. What stopped is the game dangling forty live offers at
    /// a bulkhead to hide the one that mattered.</para></summary>
    private (SatchelTry.Target Target, string? Context, string Label)? TargetFor(Core.Satchel.Item item)
    {
        if (_satchelTarget is { } at)
        {
            return SatchelTry.CanOffer(item.Kind, at.Target) ? at : null;
        }

        // A document can always be read — the tracker is on the captain's arm.
        if (item.Kind == Core.Satchel.Kind.Paper)
        {
            return (SatchelTry.Target.Tracker, null, "the motion tracker");
        }

        // #603 case 2 · And rounds go into a gun you are STANDING AT. Owner: "if we run empty on our
        // autoguns and have those on our inventory then from there we should be able to load them into the
        // guns." The interesting case is precisely a sentry that has run dry out in the field, away from the
        // tube — a handful is never a resupply, but it might be enough to get you home.
        if (item.Kind == Core.Satchel.Kind.Rounds && SentryUnderfoot() is { } unit)
        {
            return (SatchelTry.Target.Sentry, unit, unit);
        }

        return null;
    }

    /// <summary>
    /// #603 → #803 · The sentry within reach that would take rounds, if there is one.
    ///
    /// <para>It asked for a bot reading exactly 00, which was right while the only reason to hand a machine
    /// six rounds was that it had none. Owner, 2026-08-09: <i>"we might want to hand-load them into the bots
    /// for some special purposes, like shooting a mechanical lock."</i> A gun with eleven rounds in it and a
    /// hasp to take off wants six more, and a captain standing over it holding them was being told there was
    /// nothing to do.</para>
    ///
    /// <para>Still DEPLOYED only, and that is the division of labour rather than an oversight: the world's
    /// fixtures — the tube's belts, the shelter's press, the hut's locker — reach into the sling and fill
    /// what you carry. What you have SET DOWN is out there, and the only thing that walks rounds to it is
    /// you. The driest one wins when two are underfoot, because that is the one the captain came over
    /// for.</para></summary>
    private string? SentryUnderfoot()
    {
        if (_surface is not { } ex)
        {
            return null;
        }

        double radiusSq = DeckPlan.InteractRadius * DeckPlan.InteractRadius;
        string? best = null;
        int fewest = int.MaxValue;
        foreach (SurfaceBot b in ex.Bots)
        {
            if (!b.Deployed || b.Rounds >= SentryBot.MaxMagazine || b.Rounds >= fewest)
            {
                continue;
            }
            double dx = b.X - _avatarX, dy = b.Y - _avatarY;
            if ((dx * dx) + (dy * dy) <= radiusSq)
            {
                best = b.Unit;
                fewest = b.Rounds;
            }
        }
        return best;
    }

    /// <summary>What to write on one row of the pocket. The prose is rebuilt from the world here rather than
    /// stored, so a save can never go stale against the words.</summary>
    private static string SatchelLabel(Core.Satchel.Item item) => item.Kind switch
    {
        Core.Satchel.Kind.Authority =>
            UndergroundComplex.AuthorityCard.TryParse(item.Id, out UndergroundComplex.AuthorityCard c)
                ? UndergroundComplex.CardTitle(c)
                : "🎫 an authority card",
        // #613 · Each paper by its own name. Owner: "the operational papers could have individual short
        // titles… now they look identical in inventory." The certainty stays on the end, because that is the
        // one thing about a paper worth comparing across a pocketful of them.
        Core.Satchel.Kind.Paper =>
            $"📋 {Core.FieldClue.Title(item.Id)} — {Core.FieldClue.Label(Core.FieldClue.CertaintyOf(item.Id))}",
        Core.Satchel.Kind.Rounds => item.Id == Ammunition.LabTwoStage.Id
            ? $"🔫 {item.Count} × {Ammunition.LabTwoStage.Name}"
            : $"🔫 {item.Count} loose round{(item.Count == 1 ? "" : "s")}",

        // #614 · Named for what you actually have, which is paperwork about a thing you left in a room.
        // #677 · …and there are two of those now, told apart by the find's own id and named by the same
        // authored fragment the look-card is titled with — the odd book's idiom, and it means no row prose
        // was invented for a hall.
        Core.Satchel.Kind.Relic => UndergroundComplex.IsHallRecord(item.Id)
            ? UndergroundComplex.FoundRecordCardLabel
            : "⭕ measurements of the thing on the pallet",

        // #746 · The day-labour chit, printed as it is printed. Both ways of getting it are the same piece
        // of paper — the name in the book downstairs is not on the card, which is the whole horror of it.
        Core.Satchel.Kind.Chit => $"{CanteenTable.ChitGlyph} {CanteenTable.ChitTitle}",

        // #804 · The site's own pass, printed as it is printed. It names the SITE, which is what makes a
        // wallet of them worth carrying and what a guard on another rock reads out loud when he refuses it.
        Core.Satchel.Kind.Badge => PatrolBeat.SiteOfBadge(item.Id) is { Length: > 0 } badgeSite
            ? $"{PatrolBeat.BadgeGlyph} {PatrolBeat.BadgeTitle(badgeSite)}"
            : $"{PatrolBeat.BadgeGlyph} a site pass",

        // #763 · The kit, named as it is named. A tool this build does not know is still a tool and says so
        // rather than falling through to the default arm, which would print a receiver as a file on
        // somebody — the third named bug class, in a row of a list.
        // #537 · …and the cutting rig, which is the one tool whose COUNT is its state: the cell IS the item,
        // so the row prints what is left in it or a captain cannot tell a full rig from a last cut.
        Core.Satchel.Kind.Tool => Core.SdrScanner.IsTheKit(item) ? Core.SdrScanner.ItemName
            : Core.HullCutter.IsTheCutter(item) ? Core.HullCutter.RowLabel(item.Count)
            : "🧰 a piece of kit",

        _ => "🗃 a file on somebody",
    };
}
