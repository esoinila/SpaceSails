using System.Collections.Generic;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #870 lane 6′c — THE PAGE, AS A ROUND SEES IT.
///
/// <para>Twenty-one one-line answers, and that is the whole file. Each one hands <c>Map.Patrol</c> the one
/// thing a patrol verb genuinely needs from the page it is walking on — a coordinate, a pocket, a sentence,
/// a ride — and nothing else on this page is reachable from in there. <see cref="IPatrolHost"/> carries the
/// argument for each; this is only the wiring.</para>
///
/// <h3>Why every one of them is EXPLICIT</h3>
///
/// <para>Because the page's own surface must not grow by one member for this. <c>Map</c> already has a
/// <c>ShowPulseMessage</c>, a <c>StandCaptainAt</c>, a <c>FileNote</c> — the whole point is that those are
/// the same ones, called by the same names, from the same places they always were. Implemented explicitly
/// they answer the interface and nobody else: no new public API, no second spelling of a verb, no collision
/// with the private it forwards to, and a reader who greps for <c>ShowPulseMessage</c> still finds exactly
/// one implementation of it in this repository.</para>
///
/// <para>It is also what makes the count honest. An implicit member could be picked up by any caller in the
/// assembly and quietly become load-bearing somewhere else; an explicit one is reachable through
/// <see cref="IPatrolHost"/> and therefore only from the patrol family, so the number in the ratchet is a
/// real measure of one collaboration rather than an aspiration.</para>
///
/// <para><c>Map.IPatrolHost</c> and not <c>IPatrolHost</c> because the interface is declared INSIDE this
/// page — see its own summary for why (a landing is a private nested type, and three members name one).</para>
/// </summary>
public partial class Map : Map.IPatrolHost
{
    // ── WHERE THE CAPTAIN IS, AND WHAT HE IS STANDING ON ──────────────────────────────────────────────

    /// <inheritdoc/>
    SurfaceExcursion? IPatrolHost.Surface => _surface;

    /// <inheritdoc/>
    double IPatrolHost.AvatarX
    {
        get => _avatarX;
        set => _avatarX = value;
    }

    /// <inheritdoc/>
    double IPatrolHost.AvatarY
    {
        get => _avatarY;
        set => _avatarY = value;
    }

    /// <inheritdoc/>
    double IPatrolHost.AvatarHeading
    {
        set => _avatarHeading = value;
    }

    /// <inheritdoc/>
    DeckPlan IPatrolHost.DeckPlan => _deckPlan;

    /// <inheritdoc/>
    IReadOnlyList<SurfaceCollision.Segment> IPatrolHost.SightBlockers() => SightBlockers();

    /// <inheritdoc/>
    bool IPatrolHost.SeatedOnABenchInTheOpen => SeatedOnABenchInTheOpen;

    /// <inheritdoc/>
    (RingOffice.Stall Cell, string Key)? IPatrolHost.TheCubicleTheCaptainIsShutIn(SurfaceExcursion ex) =>
        TheCubicleTheCaptainIsShutIn(ex);

    // ── WHAT THE CAPTAIN HAS ON HIM, AND WHO HE SAYS HE IS ────────────────────────────────────────────

    /// <inheritdoc/>
    List<Core.Satchel.Item> IPatrolHost.Satchel
    {
        get => _satchel;
        set => _satchel = value;
    }

    /// <inheritdoc/>
    string IPatrolHost.NameOnYourOwnPapers => NameOnYourOwnPapers;

    // ── WHAT IS IN FRONT OF THE CAPTAIN ───────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    DeckPlan.ConsoleSpot? IPatrolHost.ViewObject
    {
        get => _viewObject;
        set => _viewObject = value;
    }

    // ── SAYING IT, AND WRITING IT DOWN ────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    void IPatrolHost.ShowPulseMessage(string message, PulseRank rank) => ShowPulseMessage(message, rank);

    /// <inheritdoc/>
    void IPatrolHost.LogAutopilotEvent(string text) => LogAutopilotEvent(text);

    /// <inheritdoc/>
    void IPatrolHost.FileNote(string text, string glyph) => FileNote(text, glyph);

    /// <inheritdoc/>
    void IPatrolHost.ApplyNerveShock(double rawAmount, string label) => ApplyNerveShock(rawAmount, label);

    /// <inheritdoc/>
    void IPatrolHost.RequestVaultSave() => RequestVaultSave();

    // ── MOVING THE CAPTAIN, AND THE FLOOR UNDER HIM ───────────────────────────────────────────────────

    /// <inheritdoc/>
    void IPatrolHost.StandCaptainAt(double x, double y, string where) => StandCaptainAt(x, y, where);

    /// <inheritdoc/>
    void IPatrolHost.CancelAutoWalk(bool tellThem) => CancelAutoWalk(tellThem);

    /// <inheritdoc/>
    void IPatrolHost.RefreshAshore() => RefreshAshore();

    /// <inheritdoc/>
    void IPatrolHost.RideTheLiftTo(SurfaceExcursion ex, int level) => RideTheLiftTo(ex, level);

    /// <inheritdoc/>
    void IPatrolHost.RebuildSurfaceDeck() => RebuildSurfaceDeck();
}
