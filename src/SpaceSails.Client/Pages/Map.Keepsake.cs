using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #620 · THE KEEPSAKE SHELF, THE PAGE'S HALF — the wiring for the shelf under the satchel's pockets and the
/// quiet minute the pendant gives in the captain's own cabin. Core owns every law and every sentence
/// (<see cref="Keepsake"/>); this file owns the three things only the page knows: where the captain is
/// standing, when the shelf was last looked at, and whether the pendant has ever been opened this run.
///
/// <para>Slice 1 of the issue: the pendant only. Nothing here is a second relief seam — the restore goes
/// through <see cref="NerveModel.DrinkRestore"/> (inside <see cref="Keepsake.Open"/>) and the page applies the
/// answer exactly the way the bunk and the head do.</para>
/// </summary>
public partial class Map
{
    // The shelf's last quiet minute, in sim time — ONE stamp for the whole shelf (the sleep idiom, like
    // _lastSleepSimTime: not persisted, and "never" reads as NOT sated so the first minute always lands).
    private double _lastQuietMinuteSimTime = double.NegativeInfinity;

    // What the card shows: which piece is folded open, which piece the last press was on, and the line.
    private string? _keepsakeOpenId;
    private string? _keepsakeAskedId;
    private string? _keepsakeSaid;

    /// <summary>What is on the shelf — Core's list; the pendant from the first minute of every run.</summary>
    private IReadOnlyList<Keepsake.Piece> KeepsakePieces() => Keepsake.Shelf();

    /// <summary>Is the captain in his own cabin — CABIN 1, the tidy berth with the bunk — and nowhere else?
    /// The ship's deck is one continuous plane that the docked complex welds onto, so a haven floor or a
    /// surface excursion is ruled out first and the berth's own bounds do the rest
    /// (<see cref="DeckPlan.InCaptainsCabin"/>, derived beside the wall segments that make the room).
    /// Alone: the cabin has no walker of its own, and the ship's only patrol keeps to the corridor.</summary>
    private bool InTheCaptainsCabin() =>
        _surface is null && _havenFloor == HavenLevels.Concourse && DeckPlan.InCaptainsCabin(_avatarX, _avatarY);

    // Has this run opened the pendant in the cabin yet? A DEDICATED latch, persisted on the vault's nerve
    // section (the monolith's idiom) and reset with a new game. The field-book line is only the RECORD of the
    // first opening: the book is capped and trims its front, so it can never be the latch.
    private bool _pendantFirstOpened;

    /// <summary>The press on a keepsake's card. Core decides (<see cref="Keepsake.Open"/>); the page applies.</summary>
    private void PressKeepsake(Keepsake.Piece piece)
    {
        Keepsake.QuietMinute minute = Keepsake.Open(
            piece, InTheCaptainsCabin(), firstOpening: !_pendantFirstOpened, _nerve,
            SimTime - _lastQuietMinuteSimTime, SimTime);

        _keepsakeAskedId = piece.Id;
        _keepsakeSaid = minute.Line;
        _keepsakeOpenId = null;

        if (!minute.Opened)
        {
            return; // a refusal changes nothing and stamps nothing — the card stays shut, the line under it
        }

        _lastQuietMinuteSimTime = SimTime;
        if (minute.Outcome == Keepsake.Outcome.Stung)
        {
            ApplyNerveShock(-minute.Delta, "the face read differently");
        }
        else
        {
            ApplyNerveRelief(minute.Delta);
        }

        RendererInterop.PlayCue("rum"); // the bunk's soft chime — a minute spent
        RequestVaultSave();

        if (minute.RaisesFlashback)
        {
            // THE FIRST OPENING raises the existing Flashback plate with the pendant's subject (its caption is
            // the canon first-opening text) and files the field-book line once. The satchel goes back in the
            // pocket so the plate is SEEN — the pocket's layer sits above every card the world raises
            // (#1027) — and the plate's caption IS the line, so the card has nothing to repeat.
            _pendantFirstOpened = true;
            FileNote(Keepsake.FieldBookLine, Keepsake.FieldBookGlyph);
            LogAutopilotEvent($"{Keepsake.FieldBookGlyph} {Keepsake.FieldBookLine}");
            _keepsakeSaid = null;
            CloseSatchel();
            RaiseStoryBeat(StoryBeats.Beat.Flashback, piece.FlashbackSubject);
            return;
        }

        _keepsakeOpenId = piece.Id; // a minute actually spent folds the card open
    }

    /// <summary>Close the folded card — nothing opens here that cannot be closed.</summary>
    private void FoldKeepsake()
    {
        _keepsakeOpenId = null;
        _keepsakeSaid = null;
        _keepsakeAskedId = null;
    }
}
