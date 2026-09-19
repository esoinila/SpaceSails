using System.Collections.Generic;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Subject: part of Map.Patrol (#870 lane 6′c; the header note lives in Map.Patrol.cs) — the PAGE's half of the challenge: the name on your own papers, which is the captain thread's business and not the round's, and the four things the markup and the cancel chain still ask the round for by name.
public sealed partial class Map
{
    /// <summary>#836 · The name on your own papers. Everything the site issued you carries it, spelled the
    /// way the rota spells it — read off the thread the captain is being played on, so a renamed captain's
    /// wallet renames itself with them.</summary>
    private string NameOnYourOwnPapers =>
        ActiveThreadInfo is { } row ? Captains.For(row).Name : Captains.Name(_activeThreadId);

    // #870 lane 6′c · IT STAYED HERE, and it is on IPatrolHost. It is an IDENTITY, read off the captain
    // THREAD this game is being played on — two page members deep, and nothing whatever to do with a round.
    // The wallet asks for the ANSWER, and Map.razor still asks for it by this name to letter a fan row.

    /// <inheritdoc cref="Patrol.TheHail"/>
    private void TheHail(Guard g) => _patrol.TheHail(g);

    /// <inheritdoc cref="Patrol.TheWalletFan"/>
    private IReadOnlyList<Satchel.Item> TheWalletFan => _patrol.TheWalletFan;

    /// <inheritdoc cref="Patrol.WalletFanIsUp"/>
    private bool WalletFanIsUp => _patrol.WalletFanIsUp;

    /// <inheritdoc cref="Patrol.ChooseThePaper"/>
    private void ChooseThePaper(Satchel.Item paper) => _patrol.ChooseThePaper(paper);

    // ── #746 · THE STOP AS AN ENCOUNTER, AS THE CARD ASKS FOR IT ──────────────────────────────────────
    //
    // Four of these are the ordinary forwarders a press and the three things its button has to draw need —
    // the table scene's own shape one family along (Map.Table.cs's nine), so ViewObjectCard.razor never holds
    // a Patrol, a Stop or a SurfaceExcursion in a local.
    //
    // The fifth is the one that could not be a forwarder: the NERVE. It is the page's own gauge, and
    // IPatrolHost may only shrink — so the page reads it through Core's own rung function and hands the round
    // the ANSWER, which is the exact technique that interface's docblock names as how its number is kept
    // small.

    /// <inheritdoc cref="Patrol.TheStopIsWaitingOnAMove"/>
    private bool TheStopIsWaitingOnAMove => _patrol.TheStopIsWaitingOnAMove;

    /// <inheritdoc cref="Patrol.TheStopsMoves"/>
    private IReadOnlyList<Encounter.Move> TheStopsMoves() => _patrol.TheStopsMoves();

    /// <inheritdoc cref="Patrol.TheStopMoveOnOffer"/>
    private bool TheStopMoveOnOffer(Encounter.Move move) => _patrol.TheStopMoveOnOffer(move);

    /// <inheritdoc cref="Patrol.TheStopMoveRefusal"/>
    private string TheStopMoveRefusal(Encounter.Move move) => _patrol.TheStopMoveRefusal(move);

    /// <summary>#746 · One move at a checkpoint, with the nerve read off the page's own gauge through
    /// <see cref="Encounter.NerveReadsAcrossATable"/> — the same rungs the readout draws, so the −1 in the
    /// modifier stack and the gauge the captain is looking at are one answer.</summary>
    private void TheStopMove(string moveId) =>
        _patrol.TheStopMove(moveId, Encounter.NerveReadsAcrossATable(_nerve));

    /// <summary>
    /// #746 · <b>THE CARD CAME DOWN WITHOUT A DECISION, SO THE DECISION WAS SAYING NOTHING.</b>
    ///
    /// <para>The general closing law (#992: no pop-up that cannot be closed) and this scene meet here. The ✕
    /// closes the card — it always did and it still does — and a stop that survived it would be the one
    /// pop-up in the game that paid a captain for ignoring it, which is worse than one that cannot be closed
    /// at all. So the scene's own exit move carries <see cref="Encounter.Leave"/>'s id and every road out of
    /// the card presses it: the same answer, the same seam, the same cost, by the road #615's find card
    /// already goes down (<i>"closing IS Leave"</i>). A stop that is already answered is not touched.</para>
    /// </summary>
    private void TheStopIsAnsweredByTheClosing() =>
        _patrol.TheStopMove(GuardStop.Nothing, Encounter.NerveReadsAcrossATable(_nerve), byClosing: true);
}
