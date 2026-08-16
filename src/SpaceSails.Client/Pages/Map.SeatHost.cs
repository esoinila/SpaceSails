using System.Collections.Generic;
using System.Threading.Tasks;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #870 lane 6c — THE PAGE, AS A CHAIR SEES IT.
///
/// <para>Twenty-eight one-line answers, and that is the whole file. Each one hands <c>Map.Seating</c> the one
/// thing a seat verb genuinely needs from the page it is drawn on — a coordinate, a purse, a sentence, a
/// re-render — and nothing else on this page is reachable from in there. <see cref="ISeatHost"/> carries the
/// argument for each; this is only the wiring.</para>
///
/// <h3>Why every one of them is EXPLICIT</h3>
///
/// <para>Because the page's own surface must not grow by one member for this. <c>Map</c> already has a
/// <c>ShowPulseMessage</c>, a <c>SitCaptainOn</c>, a <c>FileNote</c> — the whole point is that those are the
/// same ones, called by the same names, from the same places they always were. Implemented explicitly they
/// answer the interface and nobody else: no new public API, no second spelling of a verb, no collision with
/// the private it forwards to, and a reader who greps for <c>ShowPulseMessage</c> still finds exactly one
/// implementation of it in this repository.</para>
///
/// <para>It is also what makes the count honest. An implicit member could be picked up by any caller in the
/// assembly and quietly become load-bearing somewhere else; an explicit one is reachable through
/// <see cref="ISeatHost"/> and therefore only from the seat family, so the number in the ratchet is a real
/// measure of one collaboration rather than an aspiration.</para>
///
/// <para><c>Map.ISeatHost</c> and not <c>ISeatHost</c> because the interface is declared INSIDE this page —
/// see its own summary for why (a landing is a private nested type, and three members name one).</para>
/// </summary>
public partial class Map : Map.ISeatHost
{
    // ── WHERE THE CAPTAIN IS, AND WHAT THEY ARE STANDING ON ───────────────────────────────────────────

    /// <inheritdoc/>
    SurfaceExcursion? ISeatHost.Surface => _surface;

    /// <inheritdoc/>
    double ISeatHost.AvatarX => _avatarX;

    /// <inheritdoc/>
    double ISeatHost.AvatarY => _avatarY;

    /// <inheritdoc/>
    DeckPlan ISeatHost.DeckPlan => _deckPlan;

    // ── WHAT THE CAPTAIN HAS ON THEM ──────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    int ISeatHost.Credits
    {
        get => _credits;
        set => _credits = value;
    }

    /// <inheritdoc/>
    List<Core.Satchel.Item> ISeatHost.Satchel
    {
        get => _satchel;
        set => _satchel = value;
    }

    /// <inheritdoc/>
    double ISeatHost.Nerve => _nerve;

    // ── THE COUNTER'S OWN CARD, WHICH THE STOOL IS A POSTURE OF ───────────────────────────────────────

    /// <inheritdoc/>
    Core.Interior.Barkeep? ISeatHost.BarMenu => _barMenu;

    /// <inheritdoc/>
    string? ISeatHost.BarNotice
    {
        set => _barNotice = value;
    }

    // ── PUTTING THE BODY SOMEWHERE ────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    void ISeatHost.SitCaptainOn(double x, double y) => SitCaptainOn(x, y);

    /// <inheritdoc/>
    void ISeatHost.StandCaptainAt(double x, double y, string where) => StandCaptainAt(x, y, where);

    // ── SAYING SOMETHING, AND DRAWING IT ──────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    void ISeatHost.ShowPulseMessage(string message) => ShowPulseMessage(message);

    /// <inheritdoc/>
    void ISeatHost.StateHasChanged() => StateHasChanged();

    /// <inheritdoc/>
    Task ISeatHost.RefocusMap() => RefocusMap();

    /// <inheritdoc/>
    void ISeatHost.FileNote(string text, string glyph) => FileNote(text, glyph);

    /// <inheritdoc/>
    void ISeatHost.RequestVaultSave() => RequestVaultSave();

    // ── THE SYSTEMS A SEAT SPENDS THROUGH, ASKED FOR THEIR ANSWER ─────────────────────────────────────

    /// <inheritdoc/>
    void ISeatHost.AbandonProcessing(SurfaceExcursion ex, Core.Processing.Interruption why) =>
        AbandonProcessing(ex, why);

    /// <inheritdoc/>
    void ISeatHost.ApplyNerveShock(double rawAmount, string label) => ApplyNerveShock(rawAmount, label);

    /// <inheritdoc/>
    bool ISeatHost.APourInFrontOfYou => APourInFrontOfYou;

    /// <inheritdoc/>
    double? ISeatHost.PourSecondsLeft => PourSecondsLeft;

    /// <inheritdoc/>
    string? ISeatHost.RestOneSeatedBeat(SurfaceExcursion ex, int beatsAlreadySat) =>
        RestOneSeatedBeat(ex, beatsAlreadySat);

    /// <inheritdoc/>
    string ISeatHost.TheTailReading() => TheTailReading();

    /// <inheritdoc/>
    bool ISeatHost.CubicleIsShut(string key) => CubicleIsShut(key);

    // ── THE DEV ROWS ──────────────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    void ISeatHost.SeedTheSpreadFinds() => SeedTheSpreadFinds();

    /// <inheritdoc/>
    Encounter.Band? ISeatHost.RollCheat => _rollCheat;

    /// <inheritdoc/>
    bool? ISeatHost.ApproachCheat => _approachCheat;

    /// <inheritdoc/>
    bool? ISeatHost.NeighbourCheat => _neighbourCheat;

    /// <inheritdoc/>
    bool ISeatHost.SpreadCheat => _spreadCheat;
}
