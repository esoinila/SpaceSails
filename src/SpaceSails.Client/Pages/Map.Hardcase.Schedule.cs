using System;
using System.Collections.Generic;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// THE SHEET IN THE DUST, AND THE ROWS THE FILE KEEPS — what falls out of his case when he runs, what it
/// says when the captain picks it up, and the vault section that remembers which grounds he has worked.
///
/// <para>An empty list is deliberately not written; see <c>InsuranceWeatherSection.Hardcase</c> for
/// why.</para>
///
/// <para>Split out of <c>Map.Hardcase.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public sealed partial class Map
{
    // ── The sheet in the dust ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The papers go where his feet were. Once per excursion — he only ever breaks once — and the deck is
    /// rebuilt so the mark is on the ground the same frame he stops being on it.
    ///
    /// <para><b>It is kept on the EXCURSION and NOT in <see cref="LeftBehind"/>.</b> That store is the
    /// obvious home for a thing lying on a floor, and every sentence it prints says <i>"where YOU left
    /// it"</i> (<c>LeftBehind.ReachPrompt</c>, <c>FoundAgainLine</c>). Printing those over somebody else's
    /// dropped paperwork would be the game reporting one world while the sim held another — this
    /// repository's third named bug class, in the one register this whole beat is about.</para>
    ///
    /// <para>Excursion-scoped for the store's own #688 reason, though: the world does not keep a ledger of
    /// every piece of paper anybody has ever shed on a moon, and a schedule still lying in the dust three
    /// visits later would be the permanence that ruling deliberately declined. Within the walk it is exactly
    /// where it fell.</para>
    /// </summary>
    private void TheScheduleFalls(SurfaceExcursion ex, double x, double y)
    {
        if (ex.HardcaseDrop is not null || ex.HardcaseScheduleTaken)
        {
            return;
        }

        ex.HardcaseDrop = new DeckReachability.Point(x, y);
        RebuildSurfaceDeck();
    }

    /// <summary>
    /// #1061 · THE MARK ON THE REGOLITH — the sheet, drawn where it fell, as a <c>ViewObject</c> console.
    ///
    /// <para>The appended-region idiom (#698's own, and the hidden door's and the outpost hut's before it):
    /// no walls, no collision, nothing a captain can be pinned against. It carries the plate the sheet is
    /// called by, so the [E] offer over it, the head of the card it opens and the row it becomes in the
    /// sleeve are the same four words.</para>
    /// </summary>
    private void ComposeTheDroppedSchedule(SurfaceExcursion ex)
    {
        if (ex.HardcaseScheduleTaken
            || ex.HardcaseDrop is not { } spot
            || ex.Floor != HardcaseRep.SurfaceFloor)
        {
            return;
        }

        _deckPlan.AppendRegion(new DeckPlan.DeckRegion(
            [],
            [new DeckPlan.ConsoleSpot(
                DeckPlan.ConsoleKind.ViewObject, (float)spot.X, (float)spot.Y,
                HardcaseRep.ScheduleLabel)],
            [],
            []));
    }

    /// <summary>
    /// #1061 · <b>PICKING IT UP.</b> Recognised by the console's own plate, the way the head office's beats
    /// and the insurance poster already are, so this lane adds no dispatch of its own.
    ///
    /// <para>Three things happen and they happen in the order #678's law requires: the sleeve is asked
    /// FIRST, and a sleeve that will not take it leaves the sheet lying exactly where it is and says so in
    /// Core's own words. Only once it has actually gone in does the card go up and the book get its
    /// entry.</para>
    ///
    /// <para><b>The entry is the payoff.</b> It is filed with two subjects — the letterhead and the ground —
    /// so it lands on the same red-pen thread as everything else the captain has written down about Nebula
    /// Mutual (#898). The schedule prices what it never names, and the book is where that becomes
    /// legible.</para>
    /// </summary>
    /// <returns>Whether this press was the sheet's.</returns>
    private bool TryTheDroppedSchedule(SurfaceExcursion ex, string label)
    {
        if (!string.Equals(label, HardcaseRep.ScheduleLabel, StringComparison.Ordinal)
            || ex.HardcaseScheduleTaken)
        {
            return false;
        }

        var sheet = new Core.Satchel.Item(Core.Satchel.Kind.Paper, HardcaseRep.ScheduleFindId);
        if (!Core.Satchel.CanTake(_satchel, sheet))
        {
            ShowPulseMessage(UndergroundComplex.PocketFullLine.Trim());
            return true;
        }

        _satchel = [.. Core.Satchel.Add(_satchel, sheet)];
        ex.HardcaseScheduleTaken = true;
        ex.HardcaseDrop = null;

        CarriedObject.Reveal read = CarriedObject.PaperReveal(HardcaseRep.ScheduleFindId);
        _viewObject = new DeckPlan.ConsoleSpot(
            DeckPlan.ConsoleKind.ViewObject, (float)_avatarX, (float)_avatarY,
            read.Label, read.ArtUrl, read.Story, UndergroundComplex.PaperPocketLine.Trim());

        FileNoteAbout(
            HardcaseRep.ScheduleBody, HardcaseRep.ScheduleGlyph,
            HardcaseRep.ScheduleSubjects(ex.Site.Name));

        RebuildSurfaceDeck();
        RequestVaultSave();
        StateHasChanged();
        return true;
    }

    // ── The vault ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The rows the file keeps, or null when he has never been found anywhere — see
    /// <see cref="InsuranceWeatherSection.Hardcase"/> for why an empty list is not written.</summary>
    private IReadOnlyList<string>? TheGroundsKoltHasWorked() =>
        _hardcaseGroundsWorked.Count == 0 ? null : [.. _hardcaseGroundsWorked];

    /// <summary>Read them back, tolerantly: a blank row is dropped and the cap is re-applied on the way in,
    /// so an edited file cannot hand this build a third moon.</summary>
    private void RestoreTheGroundsKoltHasWorked(IReadOnlyList<string>? rows)
    {
        _hardcaseGroundsWorked.Clear();
        foreach (string row in rows ?? [])
        {
            if (string.IsNullOrWhiteSpace(row))
            {
                continue;
            }

            IReadOnlyList<string> book = HardcaseRep.WithGroundWorked(_hardcaseGroundsWorked, row);
            _hardcaseGroundsWorked.Clear();
            _hardcaseGroundsWorked.AddRange(book);
        }

        // A load is a different universe arriving; whatever he was doing on the last one is not a fact
        // about this one.
        _hardcaseGround = null;
        _hardcaseWorkingHere = false;
        _hardcaseCard = null;
        _hardcaseStandingAt = null;
    }
}
