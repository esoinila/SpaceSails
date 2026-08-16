using System.Collections.Generic;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #798 · RIP IT AND BIN IT — the third answer, beside take and leave.
///
/// <para>Owner, live in play (2026-08-09): <i>"Now the option is to photograph the papers and leave them
/// there… I would not like to leave them at table in canteen… we need option to destroy them by ripping and
/// binning etc?"</i> And, filing it as the next thing to build: <i>"those trash cans are needed so we get
/// rid of the processed materials without connecting them to us too clearly, like leaving them to the
/// table."</i></para>
///
/// <para>The phase-two loop (#784) ends by producing a liability. You sit, you dig, the book takes the gist
/// — and then you are holding an original that is worth nothing to you and a great deal to anybody who finds
/// it in your coat or on your table. <b>Sit, dig, book, BIN</b> is the whole shape, and until this file ran
/// the last rung did not exist.</para>
///
/// <h3>What this file decides, and what it does not</h3>
/// <para>Core (<see cref="RipAndBin"/>) owns the ladder, the reach and every word. The generator owns where
/// the bins are. What is left for a client is the three facts only a running world has: <b>which bin the
/// captain is standing at</b>, <b>whether anybody was looking</b>, and the act itself.</para>
///
/// <h3>The one law</h3>
/// <para><b>THE BOOK NEVER UNLEARNS.</b> Destroying an original touches the sleeve and nothing else — not a
/// filed note, not a red thread between two of them (#741), not the seated register's own written-up set.
/// What the captain dug out of a sheet is in the book in their own hand, and a hand-written page does not
/// come apart because the paper it was copied from did. Every line below that could break that law is
/// commented with the reason it does not.</para>
/// </summary>
public sealed partial class Map
{
    /// <summary>Which (body, floor) <see cref="_binsOnThisFloor"/> was built for — the memo's key, because
    /// two sites number their floors the same way and B1 of one is not B1 of the other.</summary>
    private string? _binsBuiltFor;

    /// <summary>#798 · The bins on the floor under the captain's boots.
    ///
    /// <para>Memoised, and it has to be: the hint on every satchel row asks for it on every render, and a
    /// floor plan is a whole building's worth of arithmetic. The memo is keyed on the thing that changes it
    /// and nothing else — a floor's bins are a pure function of (body, level), exactly like its walls.</para></summary>
    private IReadOnlyList<RipAndBin.Bin> _binsOnThisFloor = [];

    /// <summary>#798 · Somewhere to put a paper, here. Empty everywhere but underground: the ship has a
    /// recycler nobody has modelled and the regolith has nothing at all, and a verb that quietly worked in
    /// places with no fixture would be the sim doing one thing while the world showed another.</summary>
    private IReadOnlyList<RipAndBin.Bin> BinsHere()
    {
        if (_surface is not { Floor: < 0 } ex)
        {
            (_binsBuiltFor, _binsOnThisFloor) = (null, []);
            return [];
        }

        string key = $"{ex.Stop.Body.Id}:{ex.Floor}";
        if (_binsBuiltFor != key)
        {
            _binsBuiltFor = key;
            _binsOnThisFloor = UndergroundComplex
                .Build(ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField()).TheBins;
        }
        return _binsOnThisFloor;
    }

    /// <summary>#798 · The bin the captain is actually standing at, or null. Core's one answer
    /// (<see cref="RipAndBin.NearestWithinReach"/>), asked with the client's one fact — where the boots
    /// are — so the control that is drawn, the hint that explains it and the act that fires can never come
    /// to three different views of which bucket this is.</summary>
    private RipAndBin.Bin? BinWithinReach() =>
        RipAndBin.NearestWithinReach(_avatarX, _avatarY, BinsHere());

    /// <summary>#798 · Is the shredder offered on this row at all? Only for EVIDENCE — a card, a handful of
    /// rounds and the paperwork for something too big to lift are not things anybody reads over your
    /// shoulder, and the control follows the verb the way the spread tab follows the posture: on a stack of
    /// rounds this is not a refusal, it is a page that does not apply.
    ///
    /// <para>Live wherever it is drawn, and never disabled (#212/#603): standing in a corridor with no bin
    /// it answers with the sentence that says what would fix it, which is how the law is learned.</para></summary>
    private static bool RipIsOffered(Core.Satchel.Item item) => RipAndBin.IsEvidence(item.Kind);

    /// <summary>#798 · What the control says before it is pressed — the bucket and its bet when it will
    /// work, the refusal when it will not. The price of a press is known before the press (#696's own
    /// discipline, one control over).</summary>
    private string RipHint(Core.Satchel.Item item) =>
        !RipAndBin.IsEvidence(item.Kind) ? RipAndBin.NotEvidenceLine
        : BinWithinReach() is { } bin ? $"{RipAndBin.Hint(bin.Tier)} {RipAndBin.TierBet(bin.Tier)}"
        : RipAndBin.NoBinLine;

    // ── #828 · THE OTHER DOOR: THE BIN TAKES [E] ─────────────────────────────────────────────────────────
    //
    // Owner, evening playtest at his own table in the upper canteen (2026-08-11): "I think the trash could
    // be an e-use ... where we select from inventory the processed items we rip and deposit into trash."
    //
    // #798 above is the verb PAPER-FIRST: it lives on a satchel row and the bin is a range check. This is
    // the mirror, and it is a WAY IN and nothing else. Every method below opens a page, orders a list or
    // writes a word on a row; the press at the end of it is <see cref="RipItUp"/>, the same act, with the
    // same reach check, the same filed fact and the same sleeve. Two grips, one verb — the law the issue
    // states and the guard proves by driving both doors and comparing the books.

    /// <summary>
    /// #828 · WHICH BIN [E] BELONGS TO where the captain is standing, or null.
    ///
    /// <para>A bin is not a console — the carve puts a solid box on the plan and plates it, and deliberately
    /// adds no console spot (#757: a console that answers nothing is worse than no console). So the press has
    /// to be claimed here rather than in <c>DeckPlan.ConsoleKind</c>, and the rule for claiming it is the one
    /// <see cref="Rendering.DeckPlan.NearestConsoleSpot"/> itself had to learn the hard way: <b>the NEAREST
    /// fixture wins</b>. Walk to a thing and the thing answers. A bin standing near a counter never takes the
    /// counter's key, and a captain on the bin's own published standing spot — 2.2 du from a box the carve
    /// keeps 4 du clear of every piece of furniture on the floor — is nearer to it than to anything else.</para>
    /// </summary>
    private RipAndBin.Bin? TheBinTakingYourPress()
    {
        if (BinWithinReach() is not { } bin)
        {
            return null;
        }
        double toBin = bin.DistanceTo(_avatarX, _avatarY);
        return _deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is { } spot
            && spot.DistanceFrom(_avatarX, _avatarY) <= toBin
                ? null
                : bin;
    }

    /// <summary>
    /// #828 · THE PRESS. Stand at the bin, open the sleeve over it.
    ///
    /// <para>Returns true when the bin took the key, so the console dispatch behind it is untouched by a
    /// press it never saw. It opens the satchel on the bin's own page rather than raising a fourth kind of
    /// card: the pocket is where the papers are, the rows already know how to say what they are, and the
    /// refusals already land inside the dialog (#680, and the reason the act says its line into
    /// <c>_satchelOutcome</c> rather than under the backdrop's blur).</para>
    ///
    /// <para>It does NOT freeze which bin this is. Nothing here stops a captain walking off mid-picker — the
    /// deck keys are live under an open satchel — so the page asks <see cref="BinWithinReach"/> every draw
    /// and the act asks it again at the press. A stored bin would be the sim doing one thing while the page
    /// said another, three du down the corridor.</para>
    /// </summary>
    private bool TryOpenTheBinOverTheSleeve()
    {
        if (TheBinTakingYourPress() is null)
        {
            return false;
        }

        _satchelTarget = null;      // this is not an offer to a lock — nothing here is being TRIED
        _satchelOutcome = null;
        _walletOpen = false;
        _satchelPage = SatchelPage.Bin;
        _showSatchel = true;
        return true;
    }

    /// <summary>
    /// #828 · WHAT THE PICKER OFFERS, in the order it offers it.
    ///
    /// <para>Papers only — <see cref="RipAndBin.IsEvidence"/>, the same question the row control asks, so a
    /// page of things the verb does not apply to is impossible rather than merely unlikely.</para>
    ///
    /// <para>Worked sheets LEAD (<see cref="RipAndBin.WorkedLeading"/>), which is the owner's two-tier
    /// reading law made into a list: only the dig guarantees the book kept what the sheet had, so the
    /// finished ones are the natural feed and go under the captain's thumb. The rest are still here, still
    /// pressable, wearing a flag — a refusal would be the game deciding what a captain may do with their own
    /// paper.</para>
    /// </summary>
    private List<Core.Satchel.Item> BinnableFinds()
    {
        var papers = new List<Core.Satchel.Item>();
        foreach (Core.Satchel.Item item in _satchel)
        {
            if (RipAndBin.IsEvidence(item.Kind))
            {
                papers.Add(item);
            }
        }
        return RipAndBin.WorkedLeading(papers, AlreadyWrittenUp);
    }

    /// <summary>#828 · The quiet word on a picker row: what the book already has, or what it never got. Core
    /// owns both, so the flag on the row and the warning in the hint cannot come to two views of the same
    /// sheet.</summary>
    private string BinRowFlag(Core.Satchel.Item item) =>
        AlreadyWrittenUp(item) ? RipAndBin.AlreadyInTheBookFlag : RipAndBin.NotYetWorkedFlag;

    /// <summary>#828 · …and the hint behind it: the warning FIRST on a sheet nothing has been dug out of,
    /// then the ordinary price of the press. Warn, then allow — the owner's ruling, and the reason this is
    /// two sentences rather than a disabled control.</summary>
    private string BinRowHint(Core.Satchel.Item item) =>
        AlreadyWrittenUp(item) ? RipHint(item)
        : $"{RipAndBin.NotYetWorkedWarning} {RipHint(item)}";

    /// <summary>
    /// #798 · THE ACT.
    ///
    /// <para>Both gates are asked out loud, in the order a captain meets them — is this a thing you tear up,
    /// and is there anything here to put it in — because a control that does nothing and says nothing is
    /// indistinguishable from a bug, and this ground has shipped that mistake twice in a week.</para>
    ///
    /// <para>It is deliberately NOT <see cref="LeaveItem"/> with a different sentence. Leaving is #615's law
    /// — <i>leaving never destroys</i>, the thing lies on the square you are standing on and
    /// <c>TryPickUpWhatYouLeft</c> hands it straight back. This is the opposite verb, and the difference is
    /// the whole feature: nothing is written to the ground, so nothing can be picked up again, by you or by
    /// anybody else.</para>
    /// </summary>
    private void RipItUp(Core.Satchel.Item item)
    {
        if (_surface is not { } atTheBin)
        {
            return;
        }

        if (!RipAndBin.IsEvidence(item.Kind))
        {
            SayItWhereTheyAreLooking(RipAndBin.NotEvidenceLine);
            return;
        }

        if (BinWithinReach() is not { } bin)
        {
            SayItWhereTheyAreLooking(RipAndBin.NoBinLine);
            return;
        }

        // ── #828 · THE TOP RUNG IS WATCHED, AND THAT IS THE WHOLE OF WHAT IT SELLS ──────────────────────
        //
        // Owner: "a safe paper disposal … that visually destroy the notes as we watch." Three rungs of this
        // ladder are a BET about somebody else's hands and the game never says which was enough; this one is
        // the only disposal a captain can CHECK, and the checking is a beat they stand through rather than a
        // sentence they are handed. So the press opens the one hold this surface has
        // (Processing.Work.Shred) and the destruction happens at the far end of it — which also means
        // walking off mid-sheet gives the paper back, creased and still yours, because a disposal nobody
        // watched has not happened.
        //
        // It is a DOORWAY in front of the act and never a second copy of one: <see cref="TheSheetIsGone"/>
        // below is the only place in the game a document is destroyed, and every rung ends in it.
        if (bin.Tier == RipAndBin.Tier.SecureDisposal)
        {
            BeginProcessing(
                atTheBin, Core.Processing.Work.Shred, item, WhereYouAreStanding(), at: null);
            return;
        }

        TheSheetIsGone(item, bin);
    }

    /// <summary>
    /// #828 · THE FAR END OF A WATCHED DESTRUCTION — the rollers stop and the sheet is gone.
    ///
    /// <para>Called only from <c>CompleteProcessing</c>, which clears the hold first, so this runs in a
    /// world with no clock in it. The bucket is re-asked rather than carried on the hold, for the reason the
    /// picker re-asks it every draw: a stored bin would be the sim doing one thing while the page said
    /// another, three du down the corridor. The hold's own tolerance
    /// (<see cref="Core.Processing.StandingToleranceDu"/>, 1.5 du) is smaller than the reach
    /// (<see cref="RipAndBin.ReachDu"/>, 4 du), so a captain who held still is still standing at the machine
    /// they fed — and one who is somehow not gets the refusal and keeps their paper.</para>
    /// </summary>
    private void TheDestructionWasWatched(Core.Satchel.Item item)
    {
        if (BinWithinReach() is not { } bin)
        {
            SayItWhereTheyAreLooking(RipAndBin.NoBinLine);
            return;
        }
        TheSheetIsGone(item, bin);
    }

    /// <summary>
    /// #798/#828 · THE DESTRUCTION ITSELF — the one implementation, whichever rung and whichever door.
    ///
    /// <para>Lifted out of <see cref="RipItUp"/> when the secure rung grew a beat in front of it. Nothing
    /// about it moved: the same one line touching the same one list, the same filed fact with the bucket
    /// named in it, the same witness ladder. The ONLY way to reach it is through the gates above, and there
    /// is no second copy of it anywhere — which is what makes "one verb, three rungs, two doors" structural
    /// rather than a promise.</para>
    /// </summary>
    private void TheSheetIsGone(Core.Satchel.Item item, RipAndBin.Bin bin)
    {
        // The label is read BEFORE the sleeve loses the row, because SatchelLabel rebuilds the prose from
        // the world and the item, and a sentence about a thing you are no longer carrying is a sentence
        // about nothing.
        string label = SatchelLabel(item);

        // ── THE EVIDENCE GOES, AND NOTHING ELSE DOES ────────────────────────────────────────────────────
        //
        // ONE line, touching ONE list. _fieldNotes, _caseThreads and ex.WrittenUpProperly are not mentioned
        // in this method and must never be: the book keeps what you dug out of the sheet, the red lines you
        // drew between entries stay drawn, and the seated register still knows this document was written up.
        // Nothing here goes to the ground either (#615's Leave is the other verb) — the sheet stops existing.
        _satchel = [.. Core.Satchel.Remove(_satchel, item.Kind, item.Id, item.Count)];

        // …and the ACT is filed, with the BUCKET named in it. The three tiers do the same thing today; the
        // word on this line is the entire difference a later arc has to read back when it decides whether
        // anything comes of it. It is a fact about what the captain did and never a verdict about what it
        // bought them, because nothing in this game knows that yet.
        FileNote(RipAndBin.DisposalNote(label, bin.Tier), RipAndBin.Glyph);

        string said = RipAndBin.RippedLine(label, bin.Tier);
        if (WhoIsWatchingYouRip() is { } who)
        {
            // #715's per-entity memory, arriving as one line in a book rather than as a meter — the same
            // shape #804's escort files, and for the same reason: a meter would be the announcement the
            // canon section rules out. Nothing reacts. Somebody simply knows.
            FileNote(RipAndBin.SeenNote(who), RipAndBin.SeenGlyph);
            said = $"{said} {RipAndBin.SeenLine(who)}";
        }

        // Said where the captain is actually looking (#680/#736) — the satchel stays OPEN through this, so
        // a line sent to the HUD would be in the DOM and under the backdrop's blur.
        SayItWhereTheyAreLooking(said);
        RequestVaultSave();
    }

    /// <summary>
    /// #798 · WAS ANYBODY LOOKING?
    ///
    /// <para>Owner: <i>"Ripping is a visible ACT: done at a bar desk or a watched table, it is a memory —
    /// 'the new face tore something up and binned it'."</i></para>
    ///
    /// <para><b>The LADDER is Core's</b> (<see cref="RipAndBin.WhoSaw"/>) and this method decides only what
    /// its four flags MEAN in a running world — the same division #784 draws for the spread, where the
    /// client says what "seated" and "alone" are and never what a seat is FOR. A ladder spelled out at a
    /// call site is a ladder nobody can test both directions of.</para>
    ///
    /// <para>The last flag is asked with a LINE OF SIGHT rather than a radius — the same wall law the
    /// captain, the pack, the sweep team and the rounds all obey — which is what buys a cabinet its privacy
    /// without one clause here knowing what a cabinet is.</para>
    /// </summary>
    private RipAndBin.Watcher? WhoIsWatchingYouRip()
    {
        if (_surface is not { Floor: < 0 } ex)
        {
            return null;
        }

        IReadOnlyList<SurfaceCollision.Segment> sight = SightBlockers();

        // The rota: the same predicate the challenge itself runs on (PatrolBeat.Notices), including the
        // grace off the car — a guard who has not registered you yet has not seen you do anything.
        // #870 lane 6′a · both halves are one question the round answers about itself, asked with this
        // file's own sight blockers.
        bool rota = TheRoundHasEyesOnYou(sight);

        // …and anybody at a seat who can see your hands. Core's own list of who is sitting where, off the
        // frozen watch the room was drawn with, so the people who saw it are the people on the floor.
        bool overlooked = false;
        foreach (UndergroundComplex.Amenity a in
            UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField()).Amenities)
        {
            foreach (CanteenRegulars.TableSeat top in
                CanteenRegulars.Tables(ex.Stop.Body.Id, ex.Floor, a, ex.CanteenWatch))
            {
                overlooked |= top.Taken
                    && PatrolBeat.EyesOn(top.X, top.Y, _avatarX, _avatarY, RipAndBin.OverlookedDu, sight);
            }
        }

        return RipAndBin.WhoSaw(
            rota,
            // The counter is unconditional, and it is the same ruling SeatedSpread makes about the same
            // seat: a bar top is in full view of the keep — who is security (#781) — and of everyone
            // waiting to be served. You cannot even spread the case here; tearing something up is louder.
            atTheCounter: SeatedIn == SeatedHud.Seat.BarStool,
            // #870 lane 6a · CaptainIsSeated is the seat family's own name for "there is a table under the
            // captain" — asked here rather than read out of its state. NOT SeatedWithCompany, which is the
            // chair opposite alone: this wants the privacy ladder's fuller answer, where somebody on the far
            // end of a shared bench counts as company too.
            companyAtTheTable: CaptainIsSeated && !SeatedAlone,
            overlooked);
    }
}
