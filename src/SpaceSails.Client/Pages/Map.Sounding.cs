using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #537 · THE SEARCH, ON THE DECK. The arithmetic is <see cref="HullSounding"/> and Lab 44; this is the standing
/// still, the racket, and the plate coming off.
///
/// <para><b>The loop, in the owner's own order.</b> He asked for <i>"a combi of detect tool that scans on timer
/// like 5 seconds at a spot snd a tool to get at it"</i>, then that it <i>"could cost time… like pumping vacuum
/// does… idea is to know where to do it"</i>, then <i>"it might be noisy to say knock on walls etc."</i> So:
/// <b>read her manifest</b> (a document that books a compartment longer than the deck plan draws it) → <b>go to
/// that bulkhead</b> → <b>knock</b> (a clock that dies if you walk away, and a noise the hull hears) → <b>force
/// the plate</b>.</para>
///
/// <para><b>And the gear choice lives on the captain's remote</b>, beside WEAPONS TIGHT and the cold boat, because
/// it is the same decision those two are: the sounder is quick and announces you, knuckles are slow and do not.
/// Three switches, one idea — everything that makes you hard to find makes you slow.</para>
/// </summary>
public sealed partial class Map
{
    /// <summary>What this hull is hiding, if anything. Resolved once when the away team lands, off her id, so a
    /// captain who comes back finds the same ship.</summary>
    private HullSounding.HiddenVoid? _hullVoid;

    /// <summary>Whether the manifest has been read. The clue is a DOCUMENT — until somebody opens it, there is
    /// nothing to compare the wall to, and the search is the blind one Lab 44 priced at 22 rackets.</summary>
    private bool _manifestRead;

    /// <summary>Whether the plate has come off. Once found it stays found — the ship does not re-hide it.</summary>
    private bool _voidOpened;

    /// <summary>Where the captain started sounding, and how long they have held it. Null when not sounding.</summary>
    private (double X, double Y, double Held)? _sounding;

    /// <summary>The owner's own quiet/loud switch, on the remote. Defaults to the LOUD tool: a captain who has
    /// not thought about it should be given the fast one and then find out why that was a choice.</summary>
    private bool _soundQuietly;

    private HullSounding.Method SoundingGear =>
        _soundQuietly ? HullSounding.Method.Knuckles : HullSounding.Method.Sounder;

    /// <summary>How far a captain may drift and still be on the same spot. A hair over nothing — this is the
    /// pump's rule, and the whole cost of a sounding is that you are not moving while it runs.</summary>
    private const double SoundingDriftAllowed = 0.6;

    private bool IsSounding => _sounding is not null;

    /// <summary>Whether her manifest is worth reading yet — i.e. whether this hull actually lies in her books.
    /// Used only by the manifest console, never shown, or it would answer the question the search exists to ask.</summary>
    private bool ThisHullLies => _hullVoid is not null;

    // ── Landing on her ────────────────────────────────────────────────────────────────────────────────

    /// <summary>Resolve what she is hiding, once, at boarding.</summary>
    private void ResolveHullVoid()
    {
        _sounding = null;
        _manifestRead = false;
        _voidOpened = false;
        _hullVoid = _wreck is { } w
            ? HullSounding.VoidFor(w.Id, WreckLayout.Compartments, WreckLayout.SpineHalfHeight,
                                   WreckLayout.TopY, WreckLayout.BottomY)
            : null;
    }

    // ── The knock ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Start (or abandon) a sounding where the captain is standing. Bound to K, and it costs exactly what
    /// the owner said it should: a clock, and a noise.</summary>
    private void ToggleSounding()
    {
        if (!OnWreck || _surface is null)
        {
            return;
        }

        if (IsSounding)
        {
            _sounding = null;
            ShowPulseMessage(HullSounding.AbandonedLine);
            return;
        }

        _sounding = (_avatarX, _avatarY, 0);
        ShowPulseMessage(HullSounding.WorkingLine(SoundingGear));

        // THE NOISE IS MADE AT THE START, not on completion. Lab 44's finding is that the noise is the teeth of
        // this mechanic, and a captain who begins a sounding and thinks better of it has still hit the plating.
        MakeNoiseAboard(_avatarX, _avatarY, HullSounding.Earshot(SoundingGear));
        RendererInterop.PlayCue("board");
        StateHasChanged();
    }

    /// <summary>Run the clock. Called once a frame from the sim, and it dies the moment the captain drifts.</summary>
    private void AdvanceSounding(double dtRealSeconds)
    {
        if (_sounding is not { } spot || _surface is null)
        {
            return;
        }

        double dx = _avatarX - spot.X, dy = _avatarY - spot.Y;
        if ((dx * dx) + (dy * dy) > SoundingDriftAllowed * SoundingDriftAllowed)
        {
            _sounding = null;
            ShowPulseMessage(HullSounding.AbandonedLine);
            StateHasChanged();
            return;
        }

        double held = spot.Held + System.Math.Min(dtRealSeconds, 0.1);
        _sounding = (spot.X, spot.Y, held);

        if (!HullSounding.Completed(held, SoundingGear))
        {
            return;
        }

        _sounding = null;
        FinishSounding(spot.X, spot.Y);
    }

    /// <summary>What the plating said.</summary>
    private void FinishSounding(double x, double y)
    {
        HullSounding.Reading reading = _hullVoid is { } hidden && !_voidOpened
            ? HullSounding.Read(SoundingGear, x, y, hidden.PlateX, hidden.PlateY)
            : HullSounding.Reading.Solid;

        ShowPulseMessage(HullSounding.ReadingLine(reading));
        RendererInterop.PlayCue(reading == HullSounding.Reading.Hollow ? "reveal" : "board");

        if (reading != HullSounding.Reading.Hollow || _hullVoid is not { } found)
        {
            StateHasChanged();
            return;
        }

        // FOUND — and finding is not opening. Owner: "a combi of detect tool that scans on timer … and a tool to
        // get at it." The plate becomes a thing on the deck that a captain then has to work on, which is the
        // machinery #371 already built for forcing a sealed door.
        LogAutopilotEvent(HullSounding.ReadingLine(reading));
        AddVoidPlateConsole(found);
        StateHasChanged();
    }

    /// <summary>Put the false plate on the deck as something to press. It appears only once it has been FOUND —
    /// a console standing there from boarding would give the whole thing away to anyone who walked past.</summary>
    private void AddVoidPlateConsole(in HullSounding.HiddenVoid found)
    {
        _deckPlan.AppendRegion(new DeckPlan.DeckRegion(
            Walls: [],
            Consoles: [new DeckPlan.ConsoleSpot(
                DeckPlan.ConsoleKind.SecretDoor, (float)found.PlateX, (float)found.PlateY,
                "🕳 THE FALSE PLATE")],
            Labels: [],
            Backdrops: []));
    }

    /// <summary>Force it. It is a SecretDoor rather than a SealedDoor because the expedition lane's sealed doors
    /// carry region ids and belong to its own bookkeeping — borrowing the kind would have put a wreck's plate into
    /// an excursion's opened-door set. Same verb to the player, different ledger.</summary>
    private void OpenTheFalsePlate()
    {
        if (_hullVoid is not { } found || _voidOpened)
        {
            return;
        }

        _voidOpened = true;
        ShowPulseMessage(HullSounding.FoundItLine(found));
        LogAutopilotEvent(HullSounding.FoundItLine(found));
        RendererInterop.PlayCue("reveal");

        // Loud: a plate coming off a bulkhead is not a quiet act, whichever gear found it.
        MakeNoiseAboard(found.PlateX, found.PlateY, LoudEarshot);
        RequestVaultSave();
        StateHasChanged();
    }

    /// <summary>
    /// THE QUIET SWITCH, on the captain's remote beside WEAPONS TIGHT and the cold boat — because it is the same
    /// decision those two are. Owner named the family himself when he asked for the remote; this is its third
    /// member, and the panel's own line already covers it: everything that makes you hard to find makes you slow.
    /// </summary>
    private void ToggleQuietSearch()
    {
        _soundQuietly = !_soundQuietly;
        _sounding = null;   // the gear changed under it; the reading it was buying no longer means anything

        ShowPulseMessage(HullSounding.OfferLine(SoundingGear));
        LogAutopilotEvent(_soundQuietly
            ? "✊ Quiet search: knuckles only. Slower, shorter, and nobody hears it."
            : "📡 Sounder: quick and wide, and audible the length of her.");
        RendererInterop.PlayCue("board");
        StateHasChanged();
    }

    /// <summary>
    /// THE BAND TO SEARCH, once her manifest has been read — carried on the clock strip so a captain does not have
    /// to walk back to a document to remember four frame numbers. It states the measurement and nothing else, so
    /// it is a reminder of the QUESTION rather than an arrow pointing at the answer.
    /// </summary>
    private string? SearchBandLine
    {
        get
        {
            if (!_manifestRead || _voidOpened || _hullVoid is not { } hidden)
            {
                return null;
            }

            return $"outboard of {hidden.NearRoom} — {hidden.X1 - hidden.X0:0.#} frames of it";
        }
    }

    /// <summary>The clock strip's line while a sounding runs — the same column the pumps and the boat use, because
    /// it is the same kind of number: seconds you are committed to.</summary>
    private double? SoundingSecondsLeft =>
        _sounding is { } spot ? System.Math.Max(0, HullSounding.Seconds(SoundingGear) - spot.Held) : null;
}
