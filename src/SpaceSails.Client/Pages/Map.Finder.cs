using System;
using System.Collections.Generic;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #417 slice 1 · <b>ILSE VARGA WORKS THE BARS.</b> A second finder at a bar table, the trail she sets, and
/// the berth it ends at.
///
/// <h3>Built out of five things that already existed, and one new sentence apiece</h3>
///
/// <para><b>The legs</b> are #973 L0's <c>ApproachTheTable</c> — the same hook the walk-in crosses the floor
/// on, and its own summary says it is <i>"the one verb any NPC in a docked station's bar uses to walk up to
/// the captain"</i>. <b>The chair</b> is the eighth seat. <b>The graph</b> is Core's
/// <see cref="FinderCase"/>, handed the world this page is running. <b>The trail's three seams</b> are the
/// bar's own patron flow (#414's rota), the ruins' own papers (#563/#603), and a hull's own ledger of names
/// (#397/#426) — no lead has a verb of its own, because a detective's work is asking the world questions it
/// already answers. <b>The words</b> are Fable's, in Core, and not one of them is typed twice.</para>
///
/// <h3>Why the leads say almost nothing</h3>
///
/// <para>Because the FIELD BOOK is the detective. Each lead answered files the case's own line under the
/// case's own subjects (#741/#934), so the THREADS page stacks four entries under <i>👤 Ilse Varga</i> and
/// the captain watches the case assemble itself out of things he went and looked at. The two moments that DO
/// speak are the two the canon pass wrote sentences for: the red herring clearing, and the reveal.</para>
///
/// <h3>Once per port, and never twice</h3>
///
/// <para>She keeps a VISIT FOLD, exactly as the salesman and the walk-in do: a different berth is a different
/// evening, and she asks once an evening whatever the answer was. Taking the case is what stops her asking at
/// the NEXT port — the case is one case, and a finder with two of them running is a job board.</para>
/// </summary>
public sealed partial class Map
{
    /// <summary>The graph, once the captain has TAKEN it. Null for a captain who has never sat down with her
    /// — which is almost every captain almost always.
    ///
    /// <para>Deliberately not written until the case is taken. A graph the captain looked at and walked away
    /// from is not a graph he owns, and a field that held one would quietly stop her ever coming back: the
    /// reason-to-cross-the-floor test below reads this field to decide she already has work out.</para></summary>
    private FinderCase.Case? _finderCase;

    /// <summary>…and the one she is offering at THIS table, which is a fact about this evening and is
    /// forgotten with it. Null when she has nothing to hand over.</summary>
    private FinderCase.Case? _finderOffer;

    /// <summary>…and how far down it he has got.</summary>
    private FinderCase.Progress _finderProgress = FinderCase.Progress.Fresh;

    /// <summary>Which berth this file is remembering. Null off a berth; a different berth is a different
    /// evening — the same fold the salesman and the walk-in keep.</summary>
    private string? _finderVisitBerth;

    /// <summary>Whether she has already crossed this floor this visit. Once an evening, whatever was
    /// said.</summary>
    private bool _finderAskedThisVisit;

    /// <summary>#417 dev cheat (<c>/map?finder=1</c>, <c>/map?finder=0</c>): force her on or off this berth.
    /// Null is the shipped rota. It forces WHETHER and never WHAT — the case, the hulls, the berth and the
    /// pay are the ones a captain gets.</summary>
    private bool? _finderCheat;

    /// <summary>Her card, up only while she is standing at the table, and it cannot outlive her body — the
    /// state #731's escort branch was written to refuse. The flag says whether she is here to ask or here to
    /// settle up.</summary>
    private (FinderCase.Case Case, bool Paying)? _finderCard;

    /// <summary>The reveal, up at the confrontation berth. Its own card because it is its own moment: the
    /// captain is not sitting with anybody, and the two verbs on it are the whole of the scene.</summary>
    private FinderCase.Case? _finderReveal;

    /// <summary>What the settling did, written onto the reveal card itself (#736's law) rather than pulsed
    /// under its own backdrop. Null until the captain has chosen.</summary>
    private string? _finderOutcome;

    /// <summary>Whether she has been answered, which is the same thing as her not being wanted any more.</summary>
    private bool _finderAnswered;

    /// <summary>The glyph the case wears in the field book. Named once, so the entry, the guard and any
    /// future row cannot come to three views of one mark.</summary>
    private const string FinderGlyph = "🕵";

    // ── THE VISIT ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A DIFFERENT BERTH IS A DIFFERENT EVENING. The one place forgetting happens.</summary>
    private void EnsureFinderVisit(string? berth)
    {
        if (_finderVisitBerth == berth)
        {
            return;
        }

        _finderVisitBerth = berth;
        _finderOffer = null;
        _finderCard = null;
        _finderAnswered = false;
        _finderAskedThisVisit = false;
    }

    // ── ONE FRAME OF HER EVENING ───────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #417 · Called once a frame from the docked bar's own metabolism, beside the salesman's and the
    /// walk-in's. Does nothing at all unless the captain is in the room and sitting alone.
    /// </summary>
    private void AdvanceTheFinder(in HavenInterior.BarFloor bar)
    {
        EnsureFinderVisit(bar.BodyId);
        if (!InTheBar(in bar))
        {
            return;
        }

        // Her card cannot outlive her body. The walker list is the truth about who is at the table.
        if (_finderCard is not null && TheFinderAfoot() is null)
        {
            CloseTheFindersCard();
            return;
        }

        if (_finderAskedThisVisit || TheFinderAfoot() is not null || _barAfoot.Count >= WalkerBand
            || !TheCaptainIsSittingAloneInTheBar())
        {
            return;
        }

        if (!TheresAReasonForHerToCrossThisFloor(bar.BodyId))
        {
            return;
        }

        if (!ApproachTheTable(FinderCase.Plate, TheFinderIsStillWanted, SheReachesTheFindersTable))
        {
            _finderAskedThisVisit = true;
        }
    }

    /// <summary>
    /// #417 · <b>IS THERE ANYTHING FOR HER TO SAY AT THIS TABLE?</b> Three answers, and the order is the
    /// scene: she comes to be PAID first (a settled case is a debt she owes, and she pays it at the next bar
    /// she finds you in), she comes with a CASE when the captain has none, and otherwise she does not come —
    /// a finder with a job already running does not sit down to talk about it.
    ///
    /// <para>The cheat forces WHETHER she has anything to say and not WHAT: with no case, no settled case and
    /// no world to build one out of, there is still nothing for her to cross a floor about.</para>
    /// </summary>
    private bool TheresAReasonForHerToCrossThisFloor(string berth)
    {
        if (_finderCase is { } running)
        {
            _finderOffer =
                _finderProgress.Settled != FinderCase.Outcome.Open && !_finderProgress.PaidOff ? running : null;
            return _finderOffer is not null;
        }

        if (_finderCheat is false)
        {
            return false;
        }

        _finderOffer = TheCaseThisPortDeals(berth);
        return _finderOffer is not null;
    }

    /// <summary>
    /// #417 · <b>THE WORLD, HANDED TO CORE.</b> Every list the case is built from is read off the running
    /// game here and nowhere else: the scenario's berths and their tiers, the traffic actually flying, the
    /// moons a shuttle can put the captain on. Core does the choosing; this method does the looking.
    /// </summary>
    private FinderCase.Case? TheCaseThisPortDeals(string berth)
    {
        if (_ephemeris is null)
        {
            return null;
        }

        var names = new Dictionary<string, string>(StringComparer.Ordinal);
        var sites = new List<FinderCase.Site>();
        foreach (CelestialBody body in _ephemeris.Bodies)
        {
            names[body.Id] = body.Name;
            if (ShuttleExcursion.IsLandableSurface(body.Kind))
            {
                sites.Add(new FinderCase.Site(body.Id, body.Name));
            }
        }

        var hulls = new List<FinderCase.Hull>(_npcStates.Length);
        foreach (NpcState npc in _npcStates)
        {
            hulls.Add(new FinderCase.Hull(
                npc.Ship.Id, npc.Ship.Callsign, ShipHistories.For(npc.Ship.Id)));
        }

        return FinderCase.Build(
            _activeThreadId ?? "", berth, OldCrew.BerthsOf(_ephemeris), names, hulls, sites);
    }

    /// <summary>The walker that is her, if she is on the floor. By plate, exactly as the walk-in is told
    /// apart: her errand is the room's ordinary <see cref="Errand.Approaching"/>.</summary>
    private Walker? TheFinderAfoot()
    {
        foreach (Walker w in _barAfoot)
        {
            if (w.For == Errand.Approaching
                && string.Equals(w.Walk.Plate, FinderCase.Plate, StringComparison.Ordinal))
            {
                return w;
            }
        }

        return null;
    }

    /// <summary>The gate <c>ApproachTheTable</c> asks when the walk is planned, again on the frame it lands,
    /// and every frame she is standing there. The walk-in's own two answers and for her reason: before she
    /// arrives it is seated-and-alone, and once she is AT the table she is the company, so the question
    /// becomes whether the captain is still in the chair.</summary>
    private bool TheFinderIsStillWanted() =>
        !_finderAnswered
        && (_finderCard is null
            ? TheCaptainIsSittingAloneInTheBar()
            : CaptainIsSeated && SeatedTable is { Bench: false, Office: false });

    // ── SHE REACHES THE TABLE ──────────────────────────────────────────────────────────────────────────

    /// <summary>She is at your elbow. Held behind the one scrim like everybody else's card (#1052).</summary>
    private void SheReachesTheFindersTable() => RaiseAScrimCard(HerCaseGoesUp, TheFinderIsStillWanted);

    /// <summary>The card itself, once the glass is hers.</summary>
    private void HerCaseGoesUp()
    {
        if (_finderOffer is not { } c || SeatedTable is not { } t)
        {
            return;
        }

        _finderAskedThisVisit = true;
        t.Solo = false;
        t.Plate = FinderCase.Plate;
        t.Free = Math.Max(0, t.Free - 1);

        _finderCard = (c, _finderProgress.Settled != FinderCase.Outcome.Open);

        // She is a relationship from the first hello — the book knows her name before she has asked for
        // anything, which is what lets the reputation this case pays land somewhere that already exists.
        _contacts.AddGoodwill(FinderCase.ContactId, FinderCase.DisplayName, 0);
        RendererInterop.PlayCue("reveal");
        StateHasChanged();
    }

    /// <summary>Take her off the card. The body stays wherever the room has it; only the panel goes.</summary>
    private void CloseTheFindersCard()
    {
        _finderCard = null;
        StateHasChanged();
    }

    // ── THE ANSWER ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #417 · THE CAPTAIN ANSWERS. Two answers when she is asking, and none at all when she is paying —
    /// being handed money is not a decision, so the paying card's only way out is the way out.
    /// </summary>
    private void AnswerTheFinder(bool take)
    {
        if (_finderCard is not { } up)
        {
            return;
        }

        _finderAnswered = true;

        if (up.Paying)
        {
            _finderProgress = _finderProgress with { PaidOff = true };
            RequestVaultSave();
        }
        else if (take)
        {
            TakeTheCase(up.Case);
        }

        // …and on a no, nothing is kept. The offer was a fact about this evening (see _finderOffer) and it
        // goes with her, so the next port deals its own.

        CloseTheFindersCard();
        SheLeavesTheFindersTable();
    }

    /// <summary>The table is the captain's own again — the walk-in's own tidy-up, because it is the same
    /// chair and the same strip.</summary>
    private void SheLeavesTheFindersTable()
    {
        if (SeatedTable is { } t)
        {
            t.Solo = true;
            t.Plate = SittingAlone.OwnTablePlate;
            t.Free = Math.Max(0, t.Free + 1);
        }

        RequestVaultSave();
        StateHasChanged();
    }

    /// <summary>
    /// #417 · <b>THE CASE IS TAKEN, AND THE LEAD CARD GOES IN THE BOOK.</b> Filed under the case's own
    /// subjects, so the finder, the port she asked at and the ground the paper is on each get a heading for
    /// the rest of the trail to stack under (#741/#934).
    /// </summary>
    private void TakeTheCase(FinderCase.Case c)
    {
        _finderCase = c;
        _finderProgress = _finderProgress with { Taken = true };
        FileNoteAbout(FinderCase.LeadBody, FinderGlyph, c.SubjectLine);
        LogAutopilotEvent($"{FinderGlyph} {FinderCase.DisplayName}: {FinderCase.LeadTitle}.");
        RequestVaultSave();
    }

    // ── THE TRAIL ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Is there a live case with this trail still to walk?</summary>
    private bool TheTrailIsLive =>
        _finderCase is not null && _finderProgress.Taken
        && _finderProgress.Settled == FinderCase.Outcome.Open;

    /// <summary>
    /// #417 · <b>LEAD ONE — THE WITNESS, AT HER OWN PORT.</b> Asked at the top of the bar's walk-up, and it
    /// answers false for every other face at every other berth, which is almost every press.
    ///
    /// <para>It does not TAKE the press: the regular still opens their own table card, still gets stood a
    /// glass, still hands over whatever work they had. What the case adds is the entry in the book — the
    /// same one line, under this person's own name as well as the case's, which is how the THREADS page
    /// comes to have four rows under one heading.</para>
    /// </summary>
    private void TheWitnessMayHaveSeenIt(string giver)
    {
        if (!TheTrailIsLive || _finderProgress.WitnessHeard || _finderCase is not { } c
            || !string.Equals(_dockedHavenId, c.WitnessPortId, StringComparison.Ordinal)
            || !giver.Contains(c.WitnessId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _finderProgress = _finderProgress with { WitnessHeard = true };
        FileNoteAbout(FinderCase.LeadBody, FinderGlyph,
                      CaseSubjects.Line([.. c.Subjects, CaseSubjects.Person(giver)]));
        RequestVaultSave();
    }

    /// <summary>
    /// #417 · <b>LEAD TWO — THE PAPER, CLIPPED UNDER THE CASE'S SUBJECTS.</b> The ruins' own papers line is
    /// unchanged and is still what the captain reads; what this adds is the HEADINGS the book files it
    /// under, and only on the one ground the case names.
    ///
    /// <para>Returns an empty subject line everywhere else, which is what <c>ShowAndFileAbout</c> already
    /// files for every other paper in the game — so the ordinary find is byte for byte what it was.</para>
    /// </summary>
    private string ThePapersSubjectsAt(string bodyId)
    {
        if (!TheTrailIsLive || _finderCase is not { } c
            || !string.Equals(bodyId, c.PaperSiteBodyId, StringComparison.Ordinal))
        {
            return "";
        }

        if (!_finderProgress.PaperFound)
        {
            _finderProgress = _finderProgress with { PaperFound = true };
            RequestVaultSave();
        }

        return c.SubjectLine;
    }

    /// <summary>
    /// #417 · <b>LEAD THREE, AND THE RED HERRING — BOTH READ OFF A DOSSIER.</b> Called wherever the captain
    /// deliberately looks a hull up: the interest target and the comms selection, which are the two presses
    /// that put a ledger of names in front of him.
    ///
    /// <para><b>The herring is the only one that speaks</b>, and it speaks the canon pass's own sentence:
    /// her chain of custody is older than the story, so she is not the hull. It is ranked at
    /// <see cref="Telling.Floor"/> and told where the captain is looking, because the dossier is a panel and
    /// a line pulsed under a panel is a line said to nobody (#736) — and what changed is something the
    /// captain now KNOWS about where not to go.</para>
    /// </summary>
    private void TheCaseReadsThisHull(string? shipId)
    {
        if (!TheTrailIsLive || shipId is null || _finderCase is not { } c)
        {
            return;
        }

        if (!_finderProgress.HullRead && string.Equals(shipId, c.HullId, StringComparison.Ordinal))
        {
            _finderProgress = _finderProgress with { HullRead = true };
            FileNoteAbout(FinderCase.LeadBody, FinderGlyph,
                          CaseSubjects.Line([.. c.Subjects, CaseSubjects.Place(c.HullCallsign)]));
            RequestVaultSave();
            return;
        }

        if (!_finderProgress.HerringCleared && string.Equals(shipId, c.HerringHullId, StringComparison.Ordinal))
        {
            _finderProgress = _finderProgress with { HerringCleared = true };
            SayItWhereTheyAreLooking(FinderCase.HerringCleared, Telling.Floor);
            FileNoteAbout(FinderCase.HerringCleared, FinderGlyph,
                          CaseSubjects.Line([.. c.Subjects, CaseSubjects.Place(c.HerringCallsign)]));
            RequestVaultSave();
        }
    }

    // ── THE CONFRONTATION ──────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #417 · <b>THE REVEAL, AT THE BERTH.</b> Raised the moment a captain who has walked the whole trail is
    /// tied up at the port the case ends at — which is the one place in this feature where the world comes to
    /// him rather than the other way round.
    ///
    /// <para>Called from the walked frame rather than from the bar's metabolism, because a working berth is
    /// not always a room with a bar in it and the fourth name is tied up outside either way.</para>
    /// </summary>
    private void TheRevealAtTheBerth()
    {
        if (!_deckMode || _surface is not null || _dockedHavenId is not { } berth
            || _finderReveal is not null || !TheTrailIsLive || !_finderProgress.TrailWalked
            || _finderProgress.Revealed || _finderCase is not { } c
            || !string.Equals(berth, c.BerthPortId, StringComparison.Ordinal))
        {
            return;
        }

        RaiseAScrimCard(TheFourthNameGoesUp, () => _deckMode && _dockedHavenId == c.BerthPortId);
    }

    /// <summary>The card itself, once the glass is its. THE CARD IS THE TELLING (#761): it changes what the
    /// captain knows and what he can do, and there is a row in <c>ThePlayerIsToldTests</c> that says so.</summary>
    private void TheFourthNameGoesUp()
    {
        if (_finderCase is not { } c)
        {
            return;
        }

        _finderProgress = _finderProgress with { Revealed = true };
        _finderOutcome = null;
        _finderReveal = c;
        LogAutopilotEvent($"{FinderGlyph} {FinderCase.Reveal}");
        RendererInterop.PlayCue("reveal");
        RequestVaultSave();
        StateHasChanged();
    }

    /// <summary>
    /// #417 · <b>THE TWO VERBS.</b> Both are the finding done, so both pay Varga's fee and Varga's standing;
    /// the fork is what happens on top, and the arithmetic is Core's (<see cref="FinderCase.PayFor"/>) so the
    /// sentence on the card and the numbers in the purse cannot come to two views of one evening.
    ///
    /// <para>The outcome rides the CARD (#736), not the HUD under its own backdrop. The heat, when there is
    /// any, is banked against whoever runs this port through the one call every crossing in the game goes
    /// through — never a number written into the ledger here.</para>
    /// </summary>
    private void SettleTheCase(FinderCase.Outcome outcome)
    {
        if (_finderReveal is not { } c || _finderProgress.Settled != FinderCase.Outcome.Open
            || outcome == FinderCase.Outcome.Open)
        {
            return;
        }

        FinderCase.Payment paid = FinderCase.PayFor(c, outcome);
        _credits += paid.Credits;
        _contacts.AddGoodwill(FinderCase.ContactId, FinderCase.DisplayName, paid.Reputation);
        _finderProgress = _finderProgress with { Settled = outcome };

        if (paid.HeatPoints > 0)
        {
            BankTheCrossing(new UndergroundComplex.HeatCharge(
                SiteOperator.Of(c.BerthPortId).Id, paid.HeatPoints));
        }

        _finderOutcome = FinderCase.OutcomeLine(outcome);
        LogAutopilotEvent($"{FinderGlyph} {_finderOutcome} (+{paid.Credits:N0} cr)");
        RequestVaultSave();
        StateHasChanged();
    }

    /// <summary>Shut the reveal. The choice, once made, is made — closing it again settles nothing, which is
    /// why the two verbs are gone off the card the moment one of them is pressed.</summary>
    private void CloseTheReveal()
    {
        _finderReveal = null;
        _finderOutcome = null;
        StateHasChanged();
    }

    // ── THE KEEPING ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>#417 · The case and the trail, as the vault stores them — two opaque rows, the house idiom.
    /// Null for a captain who has never taken one, so a file written by somebody who never met her carries no
    /// section at all.</summary>
    private FinderSection? BuildFinderSection() =>
        _finderCase is { } c && _finderProgress.HasHistory
            ? new FinderSection
            {
                Case = FinderCase.Stored(c),
                Progress = FinderCase.Stored(_finderProgress),
            }
            : null;

    /// <summary>Read them back. A pre-#417 file simply has none and wakes with no case, which is exactly what
    /// it had — and a row this build cannot parse is dropped rather than thrown over, the same tolerance the
    /// filing line and the satchel get.</summary>
    private void RestoreFinderSection(FinderSection? section)
    {
        _finderCase = null;
        _finderProgress = FinderCase.Progress.Fresh;
        _finderCard = null;
        _finderReveal = null;
        _finderOutcome = null;

        if (section is null || !FinderCase.TryRead(section.Case, out FinderCase.Case c))
        {
            return;
        }

        _finderCase = c;
        if (FinderCase.TryRead(section.Progress, out FinderCase.Progress p))
        {
            _finderProgress = p;
        }
    }
}
