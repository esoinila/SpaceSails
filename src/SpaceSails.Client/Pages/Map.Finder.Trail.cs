using System;
using System.Collections.Generic;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #251 · THE ANSWER AND THE TRAIL (#417) — taking her case or not, her leaving, the witness who may have
/// seen it, the papers' subjects, and the case read against a hull.
///
/// <para>Split out of <c>Map.Finder.cs</c> under #251 as a pure move: one contiguous run, no member
/// renamed, re-scoped or re-ordered, and no field — every field of the family stays in the opening
/// file.</para>
/// </summary>
public sealed partial class Map
{
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
    ///
    /// <para><b>#417 slice 2a · AND THE WALK-UP NO LONGER FILES ANYTHING.</b> It used to: reaching him WAS
    /// the lead. What he does now is say he does not work for you (once a watch —
    /// <see cref="FinderCase.TheGreetingIsDue"/>) and become somebody the bar will let you buy a glass for.
    /// The lead itself is behind that glass, in <see cref="TheWitnessHearsTheOffer"/>.</para>
    /// </summary>
    private void TheWitnessMayHaveSeenIt(string giver)
    {
        if (!ThisFaceIsTheCasesWitness(giver))
        {
            return;
        }

        _finderWitnessWatch = _finderWitnessWatch.On(PatronRota.WatchIndex(SimTime));
        if (!FinderCase.TheGreetingIsDue(_finderProgress, _finderWitnessWatch))
        {
            return;
        }

        _finderWitnessWatch = _finderWitnessWatch.Greeting();
        TheWitnessSays(FinderCase.WitnessBeforeTheGlass);
    }

    /// <summary>
    /// #417 slice 2a · <b>THE GLASS, ANSWERED — AND THE LEAD THAT IS BEHIND IT.</b> Called from the bar's
    /// own <c>BuyContactDrink</c> at the moment <see cref="ContactDrink.OfferDrink"/> has settled and before
    /// a credit has moved, on BOTH arms. Nothing is rolled here: the verdict is the one the bar reached, on
    /// the one seam every drink in the game goes through.
    ///
    /// <para><b>Returns the tail of a sentence, and that is deliberate.</b> The bar composes one receipt per
    /// press — the refusal line and its math, or the parley's line and its math — into <c>_barNotice</c> and
    /// one pulse, and both of those are SLOTS: a second line written into them in the same press is a line
    /// said to nobody (#693/#736). So his sentence rides out on the receipt that is already carrying the
    /// math, in the same idiom the drink's own <c>learn</c> tail uses two lines above the call.</para>
    ///
    /// <para>The lead files here, in the breath after he speaks, which is the canon pass's own order.</para>
    /// </summary>
    /// <param name="giver">Whoever the glass was for. Almost never him.</param>
    /// <param name="accepted">Whether the bar's own offer roll said he takes it.</param>
    /// <returns>What to append to the receipt, or an empty string when the glass was only a glass.</returns>
    private string TheWitnessHearsTheOffer(string giver, bool accepted)
    {
        if (!ThisFaceIsTheCasesWitness(giver) || _finderCase is not { } c)
        {
            return "";
        }

        _finderWitnessWatch = _finderWitnessWatch.On(PatronRota.WatchIndex(SimTime));
        FinderCase.WitnessAnswer answer =
            FinderCase.WhatTheGlassDoes(_finderProgress, _finderWitnessWatch, accepted);
        if (answer == FinderCase.WitnessAnswer.Nothing)
        {
            return "";
        }

        // HIS ONE ASK THIS WATCH IS SPENT, whichever way it went — the finder's own "asked once an evening",
        // on the unit a rota regular is measured in. A refusal costs the captain the watch and not the lead.
        _finderWitnessWatch = _finderWitnessWatch.Asking();

        if (answer == FinderCase.WitnessAnswer.Talks)
        {
            _finderProgress = _finderProgress with { WitnessHeard = true };

            // THE QUESTION HE WAS ASKED, under his own name as well as the case's. Not a new sentence and
            // not an answer somebody put in his mouth: the hook is what the captain came to ask about, and
            // what the book keeps is that he asked THIS person about it.
            FileNoteAbout(c.TheHook, FinderGlyph, c.SubjectsWithTheWitness(giver));
            RequestVaultSave();
        }

        return $"  “{FinderCase.LineFor(answer)}”";
    }

    /// <summary>
    /// #417 slice 2a · <b>WOULD THE BAR POUR FOR HIM AT ALL?</b> Asked by <c>PresentBarContacts</c>, which
    /// is the ONE gate every drink row in the game is behind — the counter card's, the table card's and the
    /// contract card's alike.
    ///
    /// <para>It exists because the shipped gate is <i>a contact you have HISTORY with</i>, and a rota
    /// regular the captain has never done a job for has none: the case's own witness could be sitting there
    /// with no way to stand him anything. Rather than grow a second offer path beside the bar's (two flows,
    /// two rolls, one question), the case joins him to the list the fixers and the old crew are already on
    /// — the same move #973 L5a made for a shipmate behind a customs desk. He drops back off it the moment
    /// the lead is in the book, unless the glass itself made him a contact, which it does.</para>
    /// </summary>
    private bool TheCaseWouldHaveHimLoosened(string giver) =>
        !_finderProgress.WitnessHeard && ThisFaceIsTheCasesWitness(giver);

    /// <summary>Is this face, at this berth, the witness the live case names? The same three clauses slice 1
    /// opened its lead with, in one place now that three doors ask them.</summary>
    private bool ThisFaceIsTheCasesWitness(string giver) =>
        TheTrailIsLive && _finderCase is { } c
        && string.Equals(_dockedHavenId, c.WitnessPortId, StringComparison.Ordinal)
        && giver.Contains(c.WitnessId, StringComparison.OrdinalIgnoreCase);

    /// <summary>#417 slice 2a · He says it where the captain is standing: into the bar's own notice — which
    /// is what his table card, the counter card and the contract card all print (#736) — and out as the
    /// pulse for a captain who has no card up at all. The bar's own idiom, one line lower than
    /// <c>BuyContactDrink</c>'s.</summary>
    private void TheWitnessSays(string line)
    {
        _barNotice = $"“{line}”";
        ShowPulseMessage(_barNotice);
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

            // HER OWN LEDGER OF NAMES, in the dossier's own words — the fact the captain went and looked at,
            // filed under the case as well as under her. Nothing is composed here: the line is the one the
            // dossier prints (#397), and a second phrasing of it would be two readings of one record.
            FileNoteAbout(ShipHistories.For(c.HullId).FormerNamesLine, FinderGlyph, c.SubjectsWithTheHull);
            RequestVaultSave();
            return;
        }

        if (!_finderProgress.HerringCleared && string.Equals(shipId, c.HerringHullId, StringComparison.Ordinal))
        {
            _finderProgress = _finderProgress with { HerringCleared = true };
            SayItWhereTheyAreLooking(FinderCase.HerringCleared, Telling.Floor);
            FileNoteAbout(FinderCase.HerringCleared, FinderGlyph, c.SubjectsWithTheHerring);
            RequestVaultSave();
        }
    }
}
