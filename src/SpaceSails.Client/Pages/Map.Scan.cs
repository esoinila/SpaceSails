using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #763 · THE KIT THAT HEARS THE BUTTONS, wired to a pocket and a screen.
///
/// <para>Core decides everything (<see cref="SdrScanner"/>): what is on the air, how far away it is, which
/// way, whether anybody answers for it, and what every one of those facts reads as. This file is the two
/// presses and the surface they are answered on, and it holds NO state of its own — the sweep is recomputed
/// from where the captain is standing every time it is drawn, because a stored copy of it would be a second
/// answer to "what can you hear from here" and this repo keeps a table of what that costs.</para>
///
/// <h3>Where the answers go</h3>
/// <para><b>The kit's own card.</b> Both verbs are pressed from inside a dialog, so both answer through
/// <see cref="SayItWhereTheyAreLooking"/> — which, with the object card up, APPENDS to the card the captain
/// is looking at (#736/#774). Nothing here raises a second modal over the first, which is #777's law said
/// about a beat that has no painting of its own: the surface already exists, so the beat is written onto it
/// and the book keeps the record.</para>
///
/// <para><b>Once per floor.</b> The first time the kit hears anything on a floor is a story beat (#761), and
/// the latch is the captain's own book rather than a new field on this component: the beat is filed with the
/// floor named in it (<see cref="WhereYouAreStanding"/>), and a beat already in the book for this floor is a
/// beat that has been told. That is deliberate on two counts — #905's frame ledger sweeps every field of this
/// page, and a "have I said this" flag that lives anywhere other than where the saying was recorded is the
/// pair of latches <c>TheHallCardsAreRaisedOnceTests</c> already forbids.</para>
/// </summary>
public sealed partial class Map
{
    /// <summary>#763 · Is this row the kit, and is the captain somewhere it could be worked? Ashore in the
    /// ship there is nothing to sweep, and the switch is not drawn.</summary>
    private bool ScanIsOffered(Core.Satchel.Item item) =>
        _surface is not null && SdrScanner.IsTheKit(item);

    /// <summary>#763 · What the switch says it will do, on its own face. Core's words, never the razor's.</summary>
    private static string ScanHint(Core.Satchel.Item item) =>
        SdrScanner.IsTheKit(item) ? SdrScanner.ScanBlurb : "";

    /// <summary>#763 · WHAT THE KIT CAN HEAR FROM WHERE THE CAPTAIN IS STANDING, asked fresh every time.
    ///
    /// <para>Recomputed rather than remembered: the card is drawn from the same square the sweep was taken
    /// on, so there is nothing to go stale — and there is no field for a later frame to disagree with.</para></summary>
    private IReadOnlyList<SdrScanner.Hit> TheKitSweeps() =>
        _surface is { } ex
            ? SdrScanner.Hits(
                ex.Stop.Body.Id, ex.Floor, _avatarX, _avatarY, MoonSurface.ExpeditionField())
            : [];

    /// <summary>#763 · Is the kit's own card the thing in front of the captain? Asked of the card that is
    /// actually up rather than of a flag beside it, for the reason <c>ACardStopsTheWorld</c> is.</summary>
    private bool TheKitsCardIsUp =>
        _viewObject is { } shown
        && string.Equals(shown.Label, SdrScanner.CardLabel, StringComparison.Ordinal);

    /// <summary>#763 · SCAN. Raises the kit's card with the sweep written onto it, and tells the beat where
    /// the captain is already looking if this floor has not been heard before.</summary>
    private void ScanWithTheKit(Core.Satchel.Item item)
    {
        if (_surface is not { } ex || !SdrScanner.IsTheKit(item))
        {
            return;
        }

        IReadOnlyList<SdrScanner.Hit> heard = TheKitSweeps();

        var screen = new StringBuilder(SdrScanner.CardStory);
        screen.Append("\n\n").Append(SdrScanner.SweepHeading).Append('\n');

        if (SdrScanner.Quiet(ex.Stop.Body.Id))
        {
            // #649/#672 · The one line that says less than it knows. Asked of Core, printed unchanged.
            screen.Append(SdrScanner.QuietLine);
        }
        else if (heard.Count == 0)
        {
            screen.Append(SdrScanner.NothingHeardLine);
        }
        else
        {
            for (int i = 0; i < heard.Count; i++)
            {
                screen.Append(i == 0 ? "" : "\n").Append(SdrScanner.HitLine(heard[i]));
            }
        }

        _viewObject = new DeckPlan.ConsoleSpot(
            DeckPlan.ConsoleKind.ViewObject, (float)_avatarX, (float)_avatarY,
            SdrScanner.CardLabel, SdrScanner.ArtUrl, screen.ToString());

        // #761/#777 · The beat, told ONCE per floor, on the card that is now up. It goes through the same
        // seam every other saying does, which with this card raised means it is appended to the card rather
        // than pulsed behind its backdrop — and the book keeps it, which is also what makes it once.
        if (heard.Count > 0 && !TheBeatIsAlreadyInTheBook())
        {
            SayWhereTheyAreLookingAndFile(TheBeatHere(), SdrScanner.Glyph);
        }

        RendererInterop.PlayCue("board");
        StateHasChanged();
    }

    /// <summary>#763 · PRESS. The first active act — asked of Core, answered in Core's own words, filed.
    ///
    /// <para>#715 · The charge Core publishes is written into the book with the sentence that earned it. The
    /// meter itself is still #715's to land, exactly as #929 left it: one number to wire, and no second
    /// spelling of it anywhere to go looking for.</para></summary>
    private void PressTheHit(SdrScanner.Hit hit)
    {
        if (_surface is not { } ex)
        {
            return;
        }

        SdrScanner.Pressed pressed = SdrScanner.Press(ex.Stop.Body.Id, ex.Floor, hit);
        SayWhereTheyAreLookingAndFile(pressed.Line, pressed.Worked ? SdrScanner.Glyph : "🔒");

        RendererInterop.PlayCue("board");
        StateHasChanged();
    }

    /// <summary>#763 · The beat as it is written down — the line, and the floor it was heard on, because
    /// "there is something on the air here" is a fact about a floor.</summary>
    private string TheBeatHere() => $"{SdrScanner.BeatLine} — {WhereYouAreStanding()}";

    /// <summary>#763 · Has this floor already been told? Asked of the book, which is where the telling was
    /// recorded — one fact, in one place.</summary>
    private bool TheBeatIsAlreadyInTheBook()
    {
        string beat = TheBeatHere();
        foreach (Core.FieldNote note in Core.FieldNotes.Here(_fieldNotes, PlaceUnderfoot()))
        {
            if (string.Equals(note.Text, beat, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    // ── #763 · THE BACK COUNTER ─────────────────────────────────────────────────────────────────────────

    /// <summary>#763 · Does the bar the captain is leaning on keep one under the counter? A fact about the
    /// PLACE (<c>Barkeep.BackCounter</c>), so a captain who has been there knows and one who has not does
    /// not.</summary>
    private bool TheBackCounterIsOpen => CurrentKeep is { BackCounter: true };

    /// <summary>#763 · Buy one for coin. Pure Core decides; this debits the purse Core handed back and files
    /// the receipt, exactly as the drinks beside it do.</summary>
    private void BuyTheKit()
    {
        if (!TheBackCounterIsOpen)
        {
            return;
        }

        SdrScanner.Bought bought = SdrScanner.Buy(_credits, _satchel);
        if (bought.Taken)
        {
            _credits = bought.RemainingCredits;
            _satchel = [.. Core.Satchel.Add(_satchel, SdrScanner.TheKit)];
        }

        SayItWhereTheyAreLooking(bought.Line);
        RendererInterop.PlayCue("board");
        RequestVaultSave();
        StateHasChanged();
    }
}
