using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #488 · THE SALVAGE RUN. Owner: <i>"let's make the wreck case. A kind of salvage run and exploration …
/// the accident investigation might be something we could even get paid from … or we might want to loot it
/// all and never tell anyone."</i>
///
/// <para>The loop: a derelict hangs in shuttle range, the ship HOLDS on her (<see cref="LoiterKeeping"/> —
/// free, because Lab 40 says a co-orbital hold costs nothing), the away team boards, walks her, reads the
/// evidence off what is bolted to the deck, and then decides. File the report and take a finder's fee and
/// a contact; or strip her and say nothing.</para>
///
/// <para>Everything mechanical lives in <see cref="Derelict"/> (Core, pure, tested). This file is the
/// client's half: spawn her, let the captain look, and spend what Core decides.</para>
/// </summary>
public sealed partial class Map
{
    /// <summary>How many hold units a stripped wreck's cargo rides home as. It is the VALUE that matters
    /// (Core priced it); the units are so the hold, the collector and the fence all see something real.</summary>
    private const int SalvageCargoUnits = 3;

    /// <summary>The cargo class stripped salvage is stamped under — its own name, because "where did you
    /// get this" is exactly the question it should invite.</summary>
    private const string SalvageCargoClass = "salvage";

    /// <summary>The wreck currently in reach, if any — seeded from her id, so she is the same wreck every
    /// time anyone looks at her.</summary>
    private Derelict.Wreck? _wreck;

    /// <summary>Which evidence stations the away team has actually read. Naming the cause is only allowed
    /// from what has been LOOKED AT — the investigation is legwork, not a guess.</summary>
    private readonly HashSet<string> _wreckExamined = [];

    /// <summary>Set once she has been filed or stripped — she is finished either way.</summary>
    private bool _wreckSalvaged;

    /// <summary>The open decision card, when the captain is standing at the cargo deciding.</summary>
    private bool _showWreckChoice;

    /// <summary>What the captain believes happened — picked on the choice card from the causes their
    /// evidence actually supports. Null until they commit to a reading.</summary>
    private Derelict.WreckCause? _wreckReported;

    /// <summary>The outcome card after the decision lands.</summary>
    private Derelict.SalvageOutcome? _wreckOutcome;

    /// <summary>What the away team is standing and looking at — the wreck's own portrait of how she died,
    /// raised when the cause's station is read.
    ///
    /// <para>#533 · <paramref name="Anomaly"/> is the FOURTH line, and empty on nearly every hull: two
    /// numbers this ship's own instruments read, side by side, on the card the captain is already holding.
    /// It rides the card's record rather than a surface of its own because a second panel would announce
    /// the hull before she was read (#761's law, and the anti-tell this whole lane rests on).</para></summary>
    public readonly record struct WreckLook(string Title, string Art, string Caption, string Anomaly = "");

    private WreckLook? _wreckLook;

    private void CloseWreckLook() => _wreckLook = null;

    /// <summary>Is the away team currently inside a derelict (rather than on a moon)?</summary>
    private bool OnWreck =>
        _surface is { } ex && Derelict.TryParseWreckId(ex.Stop.Body.Id, out _);

    // ── Reading her ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>Press E at an evidence station: read what is actually there. Each station can only tell you
    /// what it tells you — the cause's own damage says the most, the log and the manifest are the
    /// corroboration that lets a careful captain catch a wreck that lies.</summary>
    private void ExamineWreckEvidence()
    {
        if (_wreck is not { } w
            || _deckPlan.NearestConsoleSpot(_avatarX, _avatarY)
                is not { Kind: DeckPlan.ConsoleKind.WreckEvidence } spot)
        {
            return;
        }

        string id = EvidenceIdFor(spot.Label);
        bool fresh = _wreckExamined.Add(id);

        ShowPulseMessage(id switch
        {
            "cause" => $"🔎 {Derelict.Evidence(w.Cause)}",
            "log" => Derelict.LogFinding(w),
            "manifest" => ManifestFindingWithTheClue(w),
            _ => "🔎 Nothing here but cold deck plate.",
        });

        // The cause's own station is the one you STAND AND LOOK at, so it gets the wreck's portrait —
        // eight ships that died eight different ways should not all read the same. The card shows the
        // EVIDENCE, never the conclusion: naming what it means is still the captain's job.
        if (id == "cause")
        {
            // #488: THE SAME ROOM, AFTER THE VACUUM HAD IT. Owner: "should we have a different pic after
            // vent cycle… one with claw marks :-D" — the proof the soak could never offer, because the
            // counter only ever gave a number and the instrument never would say what was in there. Keeps
            // the rule: what you find is claw marks and a collapsed nest, never the thing that made them.
            bool cleared = CauseRoomIsFinished()
                           && Derelict.ArtFileCleared(w.Cause) is { Length: > 0 };

            string art = cleared ? Derelict.ArtFileCleared(w.Cause) : Derelict.ArtFile(w.Cause);
            string caption = cleared ? Derelict.EvidenceCleared(w.Cause) : Derelict.Evidence(w.Cause);

            // #533 · AND THE FOURTH LINE, WHEN THIS HULL HAS ONE. The survey is where it belongs: the
            // captain is already standing at the one station they walked here to read, and what they get is
            // two numbers side by side and no finding. The book keeps it at the same moment, because a
            // sentence that fades in eight seconds is a sentence you paid for and cannot read twice (#587).
            string fourth = TheFourthLine();
            if (art.Length > 0)
            {
                _wreckLook = new WreckLook(spot.Label.Replace("✔ ", ""), art, caption, fourth);
            }

            FileTheAnomalyOnce();
        }

        // #654 · AND THE OTHER TWO STATIONS, ON EVERY HULL. The log and the manifest are not colour: they
        // are the CORROBORATION that lets a careful captain catch a wreck that lies, and reading both is the
        // one thing that takes the decoy off the choice card. They were a pulse line that faded in a second
        // and a half while the damage two rooms away got a full plate.
        //
        // ONE generic painting each, and the same one on all ten hulls — see Derelict.LogArtFile for why
        // that is not laziness. The caption does every bit of the differentiating.
        else if (id is "log" or "manifest")
        {
            _wreckLook = new WreckLook(
                spot.Label.Replace("✔ ", ""),
                id == "log" ? Derelict.LogArtFile : Derelict.ManifestArtFile,
                id == "log" ? Derelict.LogCaption(w) : Derelict.ManifestCaption(w));
        }

        if (fresh)
        {
            RendererInterop.PlayCue("reveal");
            RebuildWreckDeck();   // the station now reads ✔
        }
    }

    // ── #533 · THE ANOMALY: TWO INSTRUMENTS THAT DISAGREE ─────────────────────────────────────────────
    //
    // Owner: "The story ones are exceptional in some sense that we are left to wonder. Like what was such a
    // rich ship doing there-kind of things 😎" — and the whole of the client's part in it is three lines:
    // ask the hull, print the answer at the station the captain is already at, and write it in the book.
    //
    // NOTHING ELSE IN THE GAME MAY READ IT. That is the issue's second discipline — no note found later, no
    // contact with an answer, no arc card three lanes on — and it is guarded as a source law
    // (TwoInstrumentsDisagreeTests.NothingOutsideTheReadingAndTheBookReadsAnAnomaly): this file and Core's
    // own are the only two in the tree allowed to name WreckAnomaly at all.

    /// <summary>
    /// What this hull's two instruments say, or null for the ordinary ship — which is nearly all of them.
    ///
    /// <para><b>A road we cannot ask about is not a road with no traffic.</b> Without a sky, or on a body
    /// with no parent to name the berth she was found off, this answers null rather than handing Core a
    /// zero: "nobody lists this road" is a FACT the captain can check, and a fact nobody asked for is the
    /// one thing an anomaly may never be built out of.</para>
    /// </summary>
    private WreckAnomaly.Reading? TheAnomalyOnThisHull()
    {
        if (_wreck is not { } w || !OnWreck
            || _ephemeris is not { } sky
            || _surface?.Stop.Body.ParentId is not { } berth)
        {
            return null;
        }

        // The road she hangs on is the road that serves the berth she was found off — #541's own rule,
        // which counts what is ON A BOARD across the whole system that berth is in. The Tilt and The Deep
        // carry real tonnage and none of it is listed anywhere, which is exactly the sentence.
        return WreckAnomaly.For(w, new WreckAnomaly.Facts(ArrivalTube.ScheduledTonnage(sky, berth)));
    }

    /// <summary>The anomaly's line for the survey card, or empty — the fourth line, and nothing else.</summary>
    private string TheFourthLine() => TheAnomalyOnThisHull()?.Line ?? "";

    /// <summary>
    /// Put it in the field book, once: the same two facts in the book's own voice, under the hull it is
    /// about (#741 — the subject is declared by the author of the sentence, never read back out of it). No
    /// verdict travels with it, because there is none to travel.
    ///
    /// <para><b>THE BOOK IS ITS OWN LATCH.</b> "Have I written this down?" is a question the book can
    /// answer, so it is asked of the book rather than of a flag beside it — and the flag would have been the
    /// worse answer twice over: it resets on a reload, so a captain who saved aboard and came back would
    /// file the same sentence a second time, and a fresh page field moves the boot sweep's roster and
    /// therefore every frame fingerprint in the ledger, for a boolean the vault already knows.</para>
    /// </summary>
    private void FileTheAnomalyOnce()
    {
        if (TheAnomalyOnThisHull() is not { } reading)
        {
            return;
        }

        foreach (Core.FieldNote already in _fieldNotes)
        {
            if (string.Equals(already.Text, reading.Gist, StringComparison.Ordinal))
            {
                return;
            }
        }

        FileNoteAbout(reading.Gist, WreckAnomaly.Glyph, reading.Subjects);
    }

    // The station's id, recovered from its label (the label carries a ✔ once read).
    private static string EvidenceIdFor(string label) =>
        label.Contains("LOG", StringComparison.OrdinalIgnoreCase) ? "log"
        : label.Contains("MANIFEST", StringComparison.OrdinalIgnoreCase) ? "manifest"
        : "cause";

    // The bridge log and the manifest USED TO BE TWO PRIVATE SWITCHES HERE, out of reach of any test and out
    // of sight of Derelict.Evidence — which narrates the same ship. They disagreed: the vented hull had no
    // arm in the log switch, so the station printed "the log ends N years ago … and then there are no more"
    // while her evidence, on the same screen, said the log runs on for months in one immaculate hand. The
    // words the log should have spoken were already written, and Core-tested, in HullVenting.VentedShipLogLine
    // — read by nothing at all. Both switches now live in Core beside the evidence they must agree with.
    //
    // #633 · AND THE SWITCHES CAME BACK ON `main`, WITH ONE THING IN THEM THAT IS NOT PROSE. While this
    // branch was moving the words into Core, `main` was adding #537's search clue to the client's copy: a
    // hull that hides a void books one compartment longer than her deck plan draws it, and reading the
    // manifest properly is what hands you that discrepancy. The prose stays in Core, where the tests can see
    // it agree with the evidence; the CLUE stays here, because it is about THIS boarding's hidden void and
    // nothing in Core knows that. One fact, one owner, each.

    /// <summary>
    /// #537 · THE MANIFEST IS WHERE THE LIE IS. Core writes the document; this adds what reading it properly
    /// tells you about the hull you are standing in. On a clean hull the frame numbers match down the page
    /// and it is an honest dead end, which it has to be: a document that only speaks up when there is
    /// something to find is a pointer, not a clue.
    /// </summary>
    private string ManifestFindingWithTheClue(in Derelict.Wreck w) =>
        Derelict.ManifestFinding(w) + " " + ReadTheseFramesAgainst(HullSounding.ClueKind.Manifest);

    /// <summary>
    /// #537 slice 3 · ONE PAPER, ASKED THE SAME QUESTION. Three stations can carry the lie now — the
    /// manifest, the builder's frame plate at the lock, and the one warm breaker on a dead board — and
    /// <b>a hull tells exactly one of them</b>.
    ///
    /// <para>So this is the whole of the reading, for all three: does THIS document not add up? If it does,
    /// the captain gets the measurement and nothing else (#533: the question, never the answer). If it does
    /// not — either because she is honest or because she lies somewhere else — he gets an honest dead end,
    /// which is the law the manifest already shipped under. A document that only speaks up when there is
    /// something to find is a pointer, and a captain learns in two boardings to read the silence.</para>
    /// </summary>
    private string ReadTheseFramesAgainst(HullSounding.ClueKind kind)
    {
        if (_hullVoid is not { } hidden || hidden.Says != kind)
        {
            return HullSounding.HonestLine(kind);
        }

        HullSounding.Discrepancy clue = HullSounding.AsDiscrepancy(hidden);
        _clueRead = true;

        LogAutopilotEvent(HullSounding.ClueLine(clue));
        LogAutopilotEvent(HullSounding.BlindSearchLine(
            SoundingGear,
            HullSounding.HullArea(WreckLayout.AftX, WreckLayout.BowX, WreckLayout.TopY, WreckLayout.BottomY)));

        return HullSounding.ClueLine(clue);
    }

    /// <summary>The causes the captain may put their name to: the ones their evidence supports. Reading
    /// only the damage lets you name the obvious answer — and a wreck that LIES will hand you the wrong
    /// one. The log and the manifest are what let you tell the difference.</summary>
    private IReadOnlyList<Derelict.WreckCause> WreckCandidateCauses()
    {
        if (_wreck is not { } w)
        {
            return [];
        }

        var options = new List<Derelict.WreckCause> { w.Cause };
        if (Derelict.MisreadsAs(w.Cause) is { } decoy)
        {
            options.Add(decoy);
        }

        // Corroboration narrows it: once BOTH the log and the manifest have been read, the decoy is off
        // the table — the captain has done the work and can see through the dressing.
        if (_wreckExamined.Contains("log") && _wreckExamined.Contains("manifest"))
        {
            options.RemoveAll(c => c != w.Cause);
        }

        options.Sort();
        return options;
    }

    /// <summary>Has the away team looked at enough to file at all? One station is a glance; the cause plus
    /// one corroboration is a finding.</summary>
    private bool CanFileWreckReport => _wreckExamined.Count >= 2;

    // ── The decision ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>Press E at the cargo: open the two roads.</summary>
    private void OpenWreckChoice()
    {
        if (_wreck is null || _wreckSalvaged)
        {
            return;
        }
        _showWreckChoice = true;
        _wreckReported = WreckCandidateCauses().Count == 1 ? WreckCandidateCauses()[0] : null;
    }

    private void CloseWreckChoice() => _showWreckChoice = false;

    /// <summary>Commit. Core prices it; this spends it.</summary>
    private void ResolveWreck(Derelict.SalvageChoice choice)
    {
        if (_wreck is not { } w || _wreckSalvaged)
        {
            return;
        }

        // #524 · SHE IS WORTH WHAT IS LEFT OF HER. A fire eats an equal share per compartment while the
        // captain decides what to do about it, so the payout resolves against the burnt-down hull rather than
        // the one the manifest remembers. Derelict.Resolve stays pure; the wreck is a record struct, so what
        // it is handed is simply a smaller ship.
        Derelict.Wreck burnt = w with { AssessedValueCr = SalvageValueNow(w) };
        Derelict.SalvageOutcome outcome = Derelict.Resolve(burnt, choice, _wreckReported);
        if (burnt.AssessedValueCr < w.AssessedValueCr)
        {
            LogAutopilotEvent(HullFire.CostLine(w.AssessedValueCr, burnt.AssessedValueCr));
        }

        _credits += outcome.CreditsNow;
        if (outcome.HeatGained > 0)
        {
            _heat = EncounterRule.RaiseHeat(_heat, outcome.HeatGained, SimTime);
        }
        if (outcome.CargoIsHot)
        {
            // It is somebody's INSURED cargo and it is aboard us now — so it rides in the hold as hot,
            // through the same ledger a plundered pod does. A collector who stops us will know it on sight.
            int room = Math.Max(0, CargoCapacity - _cargoUnits);
            int taken = Math.Min(SalvageCargoUnits, room);
            if (taken > 0)
            {
                _cargoUnits += taken;
                _cargoValue += outcome.CreditsNow;
                _hotCargo.Stamp(SalvageCargoClass, taken, _heat.Level);
            }
        }

        _wreckSalvaged = true;
        _showWreckChoice = false;
        _wreckOutcome = outcome;

        // THE CREW WERE WATCHING. Owner: "too honest a captain takes their winnings." This is the exact
        // moment that opinion is formed — the one decision on the ship where doing the right thing is
        // visibly worse paid — so the crew's sheet reads it here rather than keeping a ledger of its own.
        //
        // Honest means: filed, AND filed as what she actually was. Filing her under a cause you know is
        // wrong is the profitable road wearing the paperwork's clothes, and the crew are not fooled by it
        // even when the depot is.
        if (choice == Derelict.SalvageChoice.FileTheReport && _wreckReported == w.Cause)
        {
            NoteHonestFiling();
        }
        else
        {
            NoteProfitableLie();
        }

        LogAutopilotEvent(choice == Derelict.SalvageChoice.FileTheReport
            ? $"📋 Filed on the {w.ShipName} — {outcome.CreditsNow:N0} cr."
            : $"🏴 Stripped the {w.ShipName} — {outcome.CreditsNow:N0} cr, and she stays lost.");

        // #652 · AND THE HALF THAT OUTLIVES THE PAYOUT, WHICH USED TO OUTLIVE NOTHING. The honest road's one
        // durable advantage was a boolean: the card said "somebody now owes you a straight answer" and there
        // was no somebody anywhere in the tree. A promise the game cannot name is a promise it has not made,
        // and it is the whole reason to file rather than strip — the numbers alone (10–15% against 100%) are
        // not a decision, they are an answer.
        //
        // The contact goes on the book the game already keeps, through the seam standing a round at the bar
        // already uses (ContactLedger.AddGoodwill). Core names them, seeded from the hull, so the same wreck
        // always produces the same assessor; the ledger round-trips through the vault for free.
        BookTheSalvageContact(w, outcome);

        RendererInterop.PlayCue(outcome.ContactEarned ? "reveal" : "board");
        RebuildWreckDeck();
        RequestVaultSave();
    }

    /// <summary>#652 · Put a name to the straight answer the card promises, and say whose it is. Nothing
    /// happens on the quiet road, and nothing happens on a filing that named the wrong cause — Core has
    /// already decided both (<c>ContactEarned</c>), and a bad report earning a friend would be the lane's
    /// whole point backwards.</summary>
    private void BookTheSalvageContact(in Derelict.Wreck wreck, in Derelict.SalvageOutcome outcome)
    {
        if (!outcome.ContactEarned)
        {
            _wreckContact = null;
            return;
        }

        (string id, string name) = Derelict.ContactFor(wreck);
        _contacts.AddGoodwill(id, name, Derelict.ContactGoodwill);

        // #761/#769 · Said on the pop-up that is about to open, not into a banner behind it. The outcome
        // card IS this moment's surface, so the sentence rides the card's own record and cannot outlive it;
        // the log keeps the words after the card is gone.
        _wreckContact = name;
        LogAutopilotEvent(Derelict.ContactLine(name));
    }

    /// <summary>Who countersigned the finding that just cleared, for the outcome card — null on any road
    /// that earned nobody.</summary>
    private string? _wreckContact;

    /// <summary>#938 D4 · What the office says when a captain asks about cl. 14(b), for the receipt's own
    /// tooltip. <see cref="ComplianceSurcharge.AskAbout"/> is seeded off the hull the filing is about, so
    /// the same wreck always gets the same non-answer — an office that changed its story would be a
    /// conspiracy, and #553's whole point is that this is worse than a conspiracy: there is nobody to ask.
    /// Falls back to the accountant's summary when the hull is already out of scope.</summary>
    private string TheClauseNonAnswer =>
        _wreck is { } filed ? ComplianceSurcharge.AskAbout(filed.Id) : ComplianceSurcharge.WhatItIsLine;

    private void DismissWreckOutcome() => _wreckOutcome = null;

    /// <summary>Whether anything aboard has woken yet — the first one gets the line that matters.</summary>
    private bool _anythingHasWokenAboard;

    /// <summary>Whether the motion fan has come up on this hull. It appears the first time anything moves
    /// and then stays — an ear does not un-hear — so the appearing itself is the warning.</summary>
    private bool _wreckTrackerLive;
}
