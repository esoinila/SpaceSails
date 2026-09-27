using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// ─────────────────────────────────────────────────────────────────────────────────────────────────
// #973 L5a · THE OLD CREW, WIRED. The law is Core's (`OldCrew`, `OldCrewScene`, `HeldMemory`,
// `CaptainCrossings`); this is the six things the client owes it:
//
//   THE SEEDING     four shipmates per game thread, bonds rolled before postings, each of them a row
//                   in the contacts book with the small warmth their role starts with and the flag
//                   that says they knew the face. Lazy off the thread id, so an old save seeds on
//                   first load exactly as a new one does — the roll is deterministic and costs nothing.
//   THE ROOM        a shipmate posted at the berth the captain is tied up at is IN THE ROOM: they
//                   join the bar's contact list, and the drink they take reads the three named
//                   modifiers the bible adds.
//   THE FACE        the first meeting after a rebirth is the scene. Their line, the captain's three
//                   buttons, their answer — and the answer goes into the crossings ledger.
//   THE PHOTOGRAPH  handed over by a person rather than surfaced from the book: a held-memory sheet
//                   marked HIS, four faces, four threads, and the bleached plate.
//   THE PAGE        the summer party, seeded into the Captain's ledger dated before anything else,
//                   carrying the mark that says the SERVICE filed it — so no rebirth can grey it.
//   THE PRICES      the signer files a report (one unit of #715 heat, once per visit); the knock on a
//                   door with two people behind it costs a nerve pip, once per visit.
//
// NOTHING HERE DECIDES ANYTHING. Who is cast, who is bound to whom, where they work, what anybody
// says, which theory a sheet serves and what a report costs are all Core's. The client's opinions in
// this lane are about surfaces: which card, which room, and when a visit has ended.
// ─────────────────────────────────────────────────────────────────────────────────────────────────
public partial class Map
{
    // ── THE SEEDING ──────────────────────────────────────────────────────────────────────────────────

    private IReadOnlyList<OldCrew.Seeded> _oldCrew = [];
    private string _oldCrewSeededFor = "\0";      // never a thread id, so the first read always seeds

    /// <summary>The captain's crossings (the-captains-character.md §3), oldest first.</summary>
    private IReadOnlyList<CaptainCrossings.Crossing> _crossings = [];

    /// <summary>The held-memory sheets: the summer-party page, the photograph, and what a good glass
    /// shakes loose. L3 builds their surfaces in the satchel; this lane fills the book.</summary>
    private IReadOnlyList<HeldMemory.Sheet> _heldMemories = [];

    /// <summary>Which shipmates this LIFE has already had the face scene with. Per life, because the scene
    /// is about a face that is new — a captain who has already explained himself to Teo does not explain
    /// himself to Teo again, and the next captain has his own explaining to do.</summary>
    private readonly HashSet<string> _facesExplained = new(StringComparer.Ordinal);

    /// <summary>The berth the once-per-visit prices below are counted against. When the captain leaves and
    /// ties up somewhere else, the visit is over and both sets empty — which is what "once per visit" means
    /// without a single hook in the docking code.</summary>
    private string _crewVisitBerth = "";
    private readonly HashSet<string> _signerReportedFor = new(StringComparer.Ordinal);
    private readonly HashSet<string> _knockedThisVisit = new(StringComparer.Ordinal);

    /// <summary>
    /// The four shipmates this universe cast, bound and posted. Seeded lazily off the thread id: the roll is
    /// deterministic, so a save written before this lane existed comes back with exactly the crew it would
    /// have had, and a universe switched to mid-session re-seeds without a hook in the switch.
    /// </summary>
    private IReadOnlyList<OldCrew.Seeded> TheOldCrew
    {
        get
        {
            string thread = _activeThreadId ?? "";
            if (!string.Equals(_oldCrewSeededFor, thread, StringComparison.Ordinal))
            {
                SeedTheOldCrew(thread);
            }

            return _oldCrew;
        }
    }

    /// <summary>Cast them, book them, and lay the one page down. Called once per thread.</summary>
    private void SeedTheOldCrew(string thread)
    {
        _oldCrewSeededFor = thread;
        _oldCrew = thread.Length == 0 || _ephemeris is null
            ? []
            : OldCrew.Seed(thread, OldCrew.BerthsOf(_ephemeris));

        foreach (OldCrew.Seeded s in _oldCrew)
        {
            OldCrew.Shipmate who = OldCrew.ById(s.Id)!.Value;
            _contacts.SeedOldShipmate(OldCrew.LedgerId(s.Id), who.Name, who.Warmth);
        }

        // THE SUMMER PARTY. Dated before the captain's first anything, so it sits at the head of the book;
        // marked MINE, tagged LOVE, and carrying the stamp that is the whole joke — the one piece of the
        // decent past that was preserved perfectly is the one the service wrote up against him.
        if (_oldCrew.Count > 0 && HeldMemory.Find(_heldMemories, OldCrewScene.SummerPartyId) is null)
        {
            _heldMemories = HeldMemory.Put(_heldMemories, new HeldMemory.Sheet(
                OldCrewScene.SummerPartyId,
                HeldMemory.Mark.Mine,
                HeldMemory.Theory.Love,
                OldCrewScene.SummerPartyPage,
                [],
                SummerPartySimTime,
                Filed: true));
        }

        StandTheOldCrewWhereTheCaptainIs();   // a no-op unless /map?oldcrew=1 armed it
    }

    /// <summary>How long before the captain's own book begins the fleet-day sits. Negative, so the page is
    /// older than the first line of anything the game will ever write — which is what "before the captain's
    /// first filing" means in a world whose clock starts at zero.</summary>
    private const double SummerPartySimTime = -30 * 86400.0;

    // ── THE ROOM ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Every old shipmate posted at the berth the captain is tied up at — who he can walk in on.
    /// Empty in the sky, empty at a berth none of them works at.</summary>
    private IReadOnlyList<OldCrew.Seeded> OldCrewHere =>
        _deckMode ? OldCrew.At(TheOldCrew, _dockedHavenId) : [];

    /// <summary>The three named modifiers, read off the room the captain is actually standing in. Asked once
    /// so the offer roll and the drink roll can never be handed different rooms.</summary>
    private ContactDrink.TheRoom TheRoomFor(string giver) => new(
        SharedHistory: _contacts.For(giver).KnewTheOldFace,
        SignerPresent: OldCrew.SignerIsAt(TheOldCrew, _deckMode ? _dockedHavenId : null),
        FlingPresent: OldCrew.FlingIsAt(TheOldCrew, _deckMode ? _dockedHavenId : null));

    /// <summary>The history line the black book shows beside a shipmate — <i>the best friend · now with
    /// Ilse</i> — readable BEFORE the captain knocks, which is the whole of the Fail Forward adoption.
    /// Empty for anybody who is not one of them.</summary>
    private string OldCrewHistoryLine(string giver)
    {
        if (!OldCrew.IsAnOldShipmate(giver))
        {
            return "";
        }

        string id = giver[OldCrew.LedgerPrefix.Length..];
        return OldCrew.Find(TheOldCrew, id) is { } s ? s.History() : "";
    }

    // ── THE VISIT ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Roll the once-per-visit books over when the captain ties up somewhere new. Called at the top
    /// of every seam that spends one of them, so a visit ends by being left rather than by a hook.</summary>
    private void FreshVisitIfMoved()
    {
        string here = _dockedHavenId ?? "";
        if (string.Equals(_crewVisitBerth, here, StringComparison.Ordinal))
        {
            return;
        }

        _crewVisitBerth = here;
        _signerReportedFor.Clear();
        _knockedThisVisit.Clear();
    }

    // ── THE FACE ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The shipmate the face scene is open on, or null. One at a time: it is a conversation.</summary>
    private string? _faceScene;

    /// <summary>What they said back, once the captain has pressed one of the three. Null while the buttons
    /// are still up.</summary>
    private string? _faceSceneReply;

    /// <summary>
    /// MAY THIS PERSON SAY <i>YOU LOOK DIFFERENT</i>? Three things have to be true and all three are facts
    /// rather than opinions: they knew the old face, the thread has buried at least one captain (so there IS
    /// a different face to notice), and this life has not had the scene with them yet.
    /// </summary>
    private bool TheyWouldNoticeTheFace(string giver) =>
        _contacts.For(giver).KnewTheOldFace
        && (ActiveThreadInfo?.Retired.Count ?? 0) > 0
        && !_facesExplained.Contains(giver);

    /// <summary>Open the scene. Also the seam the knock's nerve pip is spent at, because standing outside
    /// the registrar's door is the moment before you go in, not a separate button.</summary>
    private void OpenTheFaceScene(string giver)
    {
        if (!TheyWouldNoticeTheFace(giver))
        {
            return;
        }

        PayForTheKnock(giver);
        _faceScene = giver;
        _faceSceneReply = null;
        StateHasChanged();
    }

    /// <summary>
    /// THE ANSWER, AND THE CROSSING. The captain says one of three things; they answer; the choice goes into
    /// the crossings ledger with who heard it. A lie is additionally marked on that person's own page —
    /// <i>the book marks the lie</i> — because a story running with somebody is a fact about them and not a
    /// dent in the relationship, which is why it is neither goodwill nor heat.
    /// </summary>
    private void AnswerTheFace(OldCrewScene.Answer answer)
    {
        if (_faceScene is not { } giver)
        {
            return;
        }

        string id = giver[OldCrew.LedgerPrefix.Length..];
        string display = _contacts.For(giver).DisplayName;

        _facesExplained.Add(giver);
        _crossings = CaptainCrossings.Add(_crossings, CaptainCrossings.OwnFace(answer, display, SimTime));

        if (answer == OldCrewScene.Answer.ALie)
        {
            _contacts.RecordLie(giver, display);
        }

        _faceSceneReply = OldCrewScene.Reply(id, answer);
        LogAutopilotEvent($"⚖ {CaptainCrossings.Row(_crossings[^1])}");
        HandOverThePhotograph(id, display);
        RequestVaultSave();
        StateHasChanged();
    }

    /// <summary>Close it. The scene is over whether or not the photograph came out.</summary>
    private void CloseTheFaceScene()
    {
        _faceScene = null;
        _faceSceneReply = null;
    }

    /// <summary>
    /// A REBIRTH IS A NEW FACE, AND A NEW FACE HAS NOTHING EXPLAINED. Called at the succession seam beside
    /// the filing line's own marking, because both are the same fact about the same moment: the captain who
    /// walks out of the clinic is not the one who told Teo about the reactor seal, and the people who knew
    /// the old face have to be surprised all over again.
    /// </summary>
    private void ANewFaceHasNothingExplained()
    {
        _facesExplained.Clear();
        CloseTheFaceScene();
    }

    // ── THE PRICES ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE SIGNER FILES. When the man who signed is at this berth, a meeting with anybody here is a meeting
    /// he can see, and what he does about it is what he did last time: he writes it down. One unit of #715
    /// heat, owed to the authority of THIS port and to nobody else, once per visit.
    /// </summary>
    private void TheSignerReports()
    {
        FreshVisitIfMoved();
        if (!_deckMode || _dockedHavenId is not { } here
            || !OldCrew.SignerIsAt(TheOldCrew, here)
            || !_signerReportedFor.Add(here))
        {
            return;
        }

        // FABLE: line needed — the signer has filed on you at this berth, and there is nothing in the game
        // that says so. #761's sweep found this by asking one question of all five callers of the crossing
        // seam ("where does the player read it?") and getting four answers: the Hive's gate raises a card,
        // the scanner's press says it where the captain is looking, the agent's remote writes it onto his
        // panel, and the walk-in's setup was fixed in this lane. This one banks a real unit of #715 heat —
        // a debt owed to this port's authority, which will price a berth and read a gate later — and says
        // nothing at all: no card, no pulse, no note in the book, not even a log line.
        //
        // It is NOT fixed here, and the reason is the one thing a scanner cannot judge. The captain does
        // not know he was seen; that is what a man who writes things down IS, and Rusty Meg's counter has
        // the same shape one file over with an explicit comment saying so ("NOTHING REACTS on this beat").
        // If the silence is deliberate it wants that comment, and if it is not it wants a sentence, and
        // either way it wants the OWNER's word rather than a guard's guess. Until then it is filed as a
        // known silence in ThePlayerIsToldTests, which is where a debt of this shape is kept.
        BankTheCrossing(OldCrew.SignerReport(here));
    }

    /// <summary>
    /// THE DOOR YOU STAND OUTSIDE OF (owner ruling §14). When the book already says the best friend is with
    /// the fling and both of them are at this berth, going in costs a nerve pip and says so — once per visit,
    /// because the reluctance is about walking in, not about being there.
    /// </summary>
    private void PayForTheKnock(string giver)
    {
        FreshVisitIfMoved();
        if (!OldCrew.IsAnOldShipmate(giver)
            || giver[OldCrew.LedgerPrefix.Length..] != OldCrew.BestFriendId
            || !OldCrew.KnockingCostsNerve(TheOldCrew, _deckMode ? _dockedHavenId : null)
            || !_knockedThisVisit.Add(giver))
        {
            return;
        }

        ApplyNerveShock(NervePips.PipUnit * OldCrewScene.KnockNervePips, OldCrewScene.KnockNerveLabel);
        ShowPulseMessage(OldCrewScene.AtTheRegistrarsDoor);
    }

    // ── THE PAGE, IN THE LEDGER ──────────────────────────────────────────────────────────────────────

    /// <summary>The seeded pages the Captain's ledger shows alongside its six books: today, the summer party
    /// and any sheet a shipmate has slipped across a table. They are dated rows like any other, and exactly
    /// one of them carries the mark that says the service filed it.</summary>
    private IEnumerable<Stations.Captain.LedgerTip> HeldMemoryTips()
    {
        foreach (HeldMemory.Sheet sheet in _heldMemories)
        {
            // #973 L3 · The heading and the byline are the BOOK's (`HeldMemory.RowTitle` / `Sheet.BookLine`),
            // so the fleet-day page is the same row at the Captain's desk and in the satchel. Two surfaces
            // spelling one sheet two ways is the bug class this house keeps a table of.
            yield return new Stations.Captain.LedgerTip(
                HeldMemory.RowTitle(sheet),
                [sheet.Text, sheet.BookLine],
                sheet.Filed ? OldCrewScene.SummerPartyProvenance : null,
                ScopeTipId: null, ShowDarkWeb: false, DossierShipId: null,
                EntryId: sheet.Id, SimTime: sheet.SimTime,
                Grey: false, Filed: sheet.Filed);
        }
    }

    /// <summary>The crossings, as the Captain's desk shows them: one row each, no total.</summary>
    private IReadOnlyList<string> CrossingRows()
    {
        var rows = new List<string>(_crossings.Count);
        foreach (CaptainCrossings.Crossing c in _crossings)
        {
            rows.Add(CaptainCrossings.Row(c));
        }

        return rows;
    }

    // ── THE DEV DOOR ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>True once <c>/map?oldcrew=1</c> has armed the dev door below.</summary>
    private bool _oldCrewCheat;

    /// <summary>Forget them — the per-thread reset, the exact inverse of the three sections above.</summary>
    private void ForgetTheOldCrew()
    {
        _oldCrew = [];
        _oldCrewSeededFor = "\0";
        _crossings = [];
        _heldMemories = [];
        _facesExplained.Clear();
        _signerReportedFor.Clear();
        _knockedThisVisit.Clear();
        _crewVisitBerth = "";
        CloseTheFaceScene();

        // #973 L3 · …and the table with them. The papers on the SPREAD are drawn out of the two books this
        // method empties, so a laid pair that survived a new voyage would be pointing at sheets that are
        // no longer in the world.
        ClearTheSpread();
        _bookTag = null;
    }
}
