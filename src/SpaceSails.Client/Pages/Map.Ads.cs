using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// ─────────────────────────────────────────────────────────────────────────────────────────────────
// #973 L4 · ADS + MISSIONS, WIRED. The law and the words are Core's (`StationAds`, `TheOldShip`,
// `FilingLine`, `Flashback`, `NebulaRep`); this is the four things the client owes them:
//
//   THE POSTER      a captain who has died once reads the PIRATE INSURANCE poster differently. Once per
//                   life, it is a second way to the SIGNING sheet — and, if that afternoon is already in
//                   the book, it is one sentence and nothing else.
//   THE ADS         three wall plates, each worth one line of ONE memory. First look files its line, in
//                   the afternoon's order and not the captain's; the third one finishes the sheet.
//   THE PLACE       arriving somewhere a page you don't remember writing already NAMES finishes that page
//                   with no dice thrown — the only way in the game a grey page comes back for free.
//   THE OLD SHIP    the renamed HALCYON REACH is berthed once per universe, and the first time the scope
//                   holds her or the ship ties up alongside her, the strongest memory in the book.
//
// NOTHING HERE DECIDES ANYTHING. Which line belongs to which plate and in what order is `StationAds`;
// which berth she is at is `TheOldShip`; what any of it says is Fable's. The client's opinions in this
// lane are which fixture, which edge, and when a latch has already been spent.
// ─────────────────────────────────────────────────────────────────────────────────────────────────
public partial class Map
{
    // ── THE POSTER, AFTER A REBIRTH ──────────────────────────────────────────────────────────────────

    /// <summary>#973 L4 · Which life the poster has already been read at by a REBORN captain, or 0 for none.
    ///
    /// <para>The same shape as the rep's <c>_repSigningToldInLife</c>, and for the same reason: a poster is
    /// not a page, so there is no <c>FilingLine.PageState.Refused</c> latch to hang this on. The next captain
    /// gets the wall back, because it is a different man walking past it.</para></summary>
    private int _posterReadInLife;

    /// <summary>
    /// A REBORN CAPTAIN STOPS AT THE POSTER. Called from the fixture reader after the poster's own two reads
    /// (the cheerful sell, and the grey line a second look finds) have had their say — this lane adds a third
    /// thing to a wall that already did two, and it changes neither of them.
    ///
    /// <para>The gate is a REBIRTH and not a policy: the joke only lands on a man who has been through the
    /// clinic once, because until then the afternoon on the poster is simply an afternoon he was at.</para>
    /// </summary>
    private void ThePosterAfterARebirth()
    {
        if (RetiredCaptainCount == 0 || _posterReadInLife == CaptainsLife)
        {
            return;
        }

        _posterReadInLife = CaptainsLife;

        if (HeldMemory.Find(_heldMemories, NebulaRep.SigningMemoryId) is not null)
        {
            // The afternoon is already filed. The wall has nothing to give him but the sentence.
            ShowPulseMessage(StationAds.PosterAgainToast);
            LogAutopilotEvent($"{FilingLine.Mark} {StationAds.PosterAgainToast}");
            return;
        }

        // …and if it is NOT, the poster is the other door to it. Same sheet, same words, same reborn line —
        // `FileTheSigningSheet` is the one writer, so a captain who got the afternoon off a wall and a captain
        // who got it off Harlan Fess are holding the same page rather than two versions of one.
        FileTheSigningSheet();
        RaiseStoryBeat(StoryBeats.Beat.Flashback, NebulaRep.SigningMemoryId);
        LogAutopilotEvent($"{FilingLine.Mark} {Flashback.StrayToast(NebulaRep.SigningMemoryFor(RetiredCaptainCount))}");
        StateHasChanged();
    }

    // ── THE ADS ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// ONE PLATE, READ. The sheet is created on the first ad the captain stops at and grows one line per
    /// DISTINCT plate — in the afternoon's order, whichever plate came first.
    ///
    /// <para>The sheet's own text is the whole of the bookkeeping (<see cref="StationAds.LinesIn"/>): there
    /// is no second store of which plates have been read, so there is nothing that can disagree with the page
    /// and nothing to clear at a rebirth. A plate whose line is already on the sheet is a wall the captain has
    /// already read, which is exactly what "per ad per life" means once the line is in.</para>
    /// </summary>
    private void TheAdIsRead(int index)
    {
        if (index < 0 || index >= StationAds.Ads.Count)
        {
            return;
        }

        HeldMemory.Sheet? had = HeldMemory.Find(_heldMemories, StationAds.TheFilingDay);
        var lines = new List<int>(StationAds.LinesIn(had?.Text));
        if (lines.Contains(index))
        {
            return;     // this plate's line is already on the page; the wall has nothing left to give
        }

        lines.Add(index);
        string text = StationAds.TextFor(lines);
        bool whole = StationAds.IsWhole(text);

        _heldMemories = HeldMemory.Put(_heldMemories, new HeldMemory.Sheet(
            StationAds.TheFilingDay,
            HeldMemory.Mark.Mine,
            HeldMemory.Theory.Money,
            text,
            had?.Threads ?? [],
            had?.SimTime ?? SimTime,
            Filed: false,
            HandedBy: had?.HandedBy ?? "",
            // A memory the captain has assembled out of three separate walls is as corroborated as a memory
            // gets without a second witness — so a WHOLE afternoon is at full confidence. Until then it keeps
            // whatever the SPREAD has already earned it.
            Confidence: whole ? SpreadReconcile.MostConfidence : had?.Confidence ?? 0,
            Corrected: had?.Corrected ?? false));

        // The line that just arrived, said in Fable's words; and when the third one lands, the afternoon's
        // own closing sentence instead — it is the one the captain is meant to be left holding.
        string toast = whole ? StationAds.WholeToast : StationAds.Ads[index].Line;
        ShowPulseMessage(toast);
        LogAutopilotEvent($"{FilingLine.Mark} {toast}");
        RequestVaultSave();
        StateHasChanged();
    }

    // ── THE PLACE FINISHES THE PAGE ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// YOU HAVE BEEN HERE. Called on every real arrival — a berth clamped on, an orbit entered, a shuttle
    /// set down — and a no-op for every captain who has never died, because a captain who has never died has
    /// no grey pages to be met by a place.
    ///
    /// <para>Three fences, and each one closes a different way of getting this wrong:</para>
    /// <list type="bullet">
    ///   <item>Only a GREY page. A page the captain already remembers is not one the place can give back.</item>
    ///   <item>Never a page that came back WRONG. The lie stays until the SPREAD lays it beside a document
    ///   and catches it; a place that quietly straightened a moved detail would take the one piece of
    ///   detective work the black book exists for and do it in a corridor.</item>
    ///   <item>Once per page, and the page's own state is the latch — it is no longer grey afterwards, so a
    ///   captain who docks here twice a day is finishing nothing the second time.</item>
    /// </list>
    ///
    /// <para>ONE BEAT for however many pages the place finishes, subject the first of them: the plate is
    /// about the arrival, and three plates in a row over one gangway would be the cadence law broken by
    /// arithmetic.</para>
    /// </summary>
    private void ThePlaceFinishesThePages(string? bodyId)
    {
        if (string.IsNullOrEmpty(bodyId) || _filingBook.Count == 0 || _ephemeris is null)
        {
            return;
        }

        string place = BodyName(bodyId);
        string? firstFinished = null;
        int finished = 0;

        foreach (LedgerPage page in LedgerPagesForFiling())
        {
            FilingLine.Page standing = FilingLine.Standing(_filingBook, page.Id);
            if (!standing.IsGrey || standing.WasAltered || !StationAds.NamesThePlace(page, place))
            {
                continue;
            }

            _filingBook = FilingLine.Put(
                _filingBook, standing with { State = FilingLine.PageState.CameBack });
            firstFinished ??= page.Id;
            finished++;
        }

        if (firstFinished is null)
        {
            return;
        }

        // Both sentences are said, in the order Fable wrote them — and the SECOND is the one left standing on
        // the HUD, because the pulse holds one line at a time and the outcome is what the captain needs to be
        // looking at. The ledger keeps them both, in order, so the moment reads whole afterwards.
        ShowPulseMessage(StationAds.BeenHereToast);
        LogAutopilotEvent($"{FilingLine.Mark} {StationAds.BeenHereToast}");
        ShowPulseMessage(StationAds.PlaceFinishesToast);
        LogAutopilotEvent($"{FilingLine.Mark} {StationAds.PlaceFinishesToast}"
            + (finished > 1 ? $"  ({finished.ToString(System.Globalization.CultureInfo.InvariantCulture)} pages)" : ""));

        RaiseStoryBeat(StoryBeats.Beat.Flashback, firstFinished);
        RequestVaultSave();
        StateHasChanged();
    }

    // ── THE OLD SHIP ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>The universe her berth was picked for; never a thread id, so the first read always seeds.</summary>
    private string _reachSeededFor = " ";

    /// <summary>Where this universe tied her up, or null in a world with no dockable berth at all.</summary>
    private string? _reachBerthId;

    /// <summary>
    /// #973 L4 · WHERE THE REACH IS, for whoever needs to send somebody to her — L5b's FIND ends here.
    /// Null before the world is built and in a scenario with no berths.
    /// </summary>
    private string? TheReachBodyId(string? threadId)
    {
        EnsureTheOldShipIsBerthed();
        return string.Equals(threadId ?? "", _reachSeededFor, StringComparison.Ordinal) ? _reachBerthId : null;
    }

    /// <summary>Her id in this world: the hull a shipped scenario already gave the old name to, if any, and
    /// otherwise the one this lane berthed. Asked rather than assumed, so a scenario that grows its own REACH
    /// later does not end up with two of her.</summary>
    private string? TheReachShipId()
    {
        foreach (NpcState npc in _npcStates)
        {
            if (CarriesTheOldName(npc.Ship.Id))
            {
                return npc.Ship.Id;
            }
        }

        return null;
    }

    /// <summary>Does this hull's service record carry the old name? The one question that decides whether a
    /// world already contains her — asked of <see cref="ShipHistories"/> rather than of an id, because the
    /// former name is the fact and the id is only bookkeeping.</summary>
    private static bool CarriesTheOldName(string shipId)
    {
        foreach (string former in ShipHistories.For(shipId).FormerNames)
        {
            if (former.Contains(TheOldShip.FormerName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// BERTH HER, ONCE PER UNIVERSE. Lazy off the thread id, exactly as the old crew is seeded: the pick is
    /// deterministic, so a save written before this lane existed comes back with her tied up where she always
    /// would have been, and a universe switched to mid-session re-berths her without a hook in the switch.
    ///
    /// <para>If the world ALREADY floats her — a scenario that grows its own hull with the old name on its
    /// record — nothing is seeded and she is simply found. Two REACHes would be worse than none.</para>
    /// </summary>
    private void EnsureTheOldShipIsBerthed()
    {
        string thread = _activeThreadId ?? "";
        if (string.Equals(_reachSeededFor, thread, StringComparison.Ordinal) || _ephemeris is null)
        {
            return;
        }

        _reachSeededFor = thread;
        _reachBerthId = null;

        // A universe switch leaves the last world's hull in the roster; she belongs to the world we left.
        _npcStates = [.. _npcStates.Where(n => !TheOldShip.IsHer(n.Ship.Id))];
        if (thread.Length == 0)
        {
            _reachSeededFor = " ";   // no universe yet — ask again when there is one
            return;
        }

        if (TheReachShipId() is { } already)
        {
            // The scenario carries her. Her berth is wherever she is parked, if she is parked at all.
            _reachBerthId = _npcStates.FirstOrDefault(n => n.Ship.Id == already)?.Ship.DepotBodyId;
            return;
        }

        if (TheOldShip.BerthFor(thread, OldCrew.BerthsOf(_ephemeris)) is not { } berth)
        {
            return;   // a world with nowhere to tie a hull up; she is not in this one
        }

        _reachBerthId = berth;
        _npcStates = [.. _npcStates, new NpcState { Ship = TheOldShip.Berthed(_ephemeris, berth, thread) }];
    }

    /// <summary>
    /// THE FIRST TIME THE CAPTAIN SEES HER AGAIN — the scope holding her fix, or his own hull tied up
    /// alongside her. One writer for both edges, because from the inside it is one moment: she is there, and
    /// he knows the sound the hatch makes.
    ///
    /// <para>Once per universe, and the SHEET is the latch — it rides the vault, so a reload cannot hand the
    /// same afternoon back twice.</para>
    /// </summary>
    private void TheOldShipIsSeen(string? shipId)
    {
        if (!TheOldShipIsHere(shipId) || HeldMemory.Find(_heldMemories, TheOldShip.SheetId) is not null)
        {
            return;
        }

        _heldMemories = HeldMemory.Put(_heldMemories, new HeldMemory.Sheet(
            TheOldShip.SheetId,
            HeldMemory.Mark.Mine,
            // LOVE, and it is the least arguable tag in the book: she is the decent past, and there is no
            // transaction anywhere on this page.
            HeldMemory.Theory.Love,
            TheOldShip.SheetText,
            [],
            SimTime));

        ShowPulseMessage(Flashback.StrayToast(TheOldShip.SheetText));
        LogAutopilotEvent($"{FilingLine.Mark} {Flashback.StrayToast(TheOldShip.SheetText)}");
        RaiseStoryBeat(StoryBeats.Beat.Flashback, TheOldShip.SheetId);
        RequestVaultSave();
        StateHasChanged();
    }

    /// <summary>Is this contact her? True for the hull this lane berthed and for a scenario's own hull
    /// carrying the old name — one question, so the two ways she can exist cannot answer differently.</summary>
    private bool TheOldShipIsHere(string? shipId) =>
        shipId is { Length: > 0 } id && (TheOldShip.IsHer(id) || string.Equals(id, TheReachShipId(), StringComparison.Ordinal));

    /// <summary>Tied up alongside her: the berth this arrival clamped onto is the berth she is impounded at.
    /// Asked on every arrival, and free for every arrival that is not hers.</summary>
    private void TheOldShipIsAlongside(string? bodyId)
    {
        if (string.IsNullOrEmpty(bodyId) || TheReachBodyId(_activeThreadId) != bodyId)
        {
            return;
        }

        TheOldShipIsSeen(TheReachShipId() ?? TheOldShip.ShipId);
    }

    /// <summary>#973 L4 · THE ONE ARRIVAL DOOR. Every edge that really is an arrival — a berth clamped onto,
    /// an orbit entered, a boat set down — comes through here, so the two things an arrival owes this lane
    /// cannot be wired at three places and forgotten at a fourth.</summary>
    private void TheArrivalIsRemembered(string? bodyId)
    {
        ThePlaceFinishesThePages(bodyId);
        TheOldShipIsAlongside(bodyId);
    }
}
