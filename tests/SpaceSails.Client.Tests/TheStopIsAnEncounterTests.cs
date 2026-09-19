using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #746 · <b>THE CLIENT HALF: ONE CARD, FOUR MOVES, AND EVERY ROAD OUT OF IT IS A DECISION.</b>
///
/// <para>The same split every patrol guard in this suite keeps
/// (<see cref="TheParcelIsFoundFirstTests"/>, <c>TheGuardsCatchYouTests</c>) and for the same reason: the
/// round lives in a partial class on a razor page no test can instantiate. The JUDGEMENT is Core's and is
/// driven end to end in <c>TheGuardStopIsAnEncounterTests</c>; what is pinned here is the wiring the page
/// owns and Core cannot see — WHERE the moves are drawn, WHEN the wallet is read, what the ✕ does, and the
/// one line that makes a helpful walk free.</para>
///
/// <para>Every guard below was reverted in the page and watched go red; the revert is named on it.</para>
/// </summary>
public sealed class TheStopIsAnEncounterTests
{
    private static string Read(params string[] parts) =>
        File.ReadAllText(Path.Combine([TestTree.RepoRoot(), .. parts]));

    /// <summary>The CODE, with the design record taken out of it. These files are half comment by weight and
    /// every name these guards count is discussed in prose beside the line that uses it — so a count over the
    /// raw text would be a count of the explanation, and would go green the day somebody deleted the call and
    /// left the paragraph. <see cref="TheParcelIsFoundFirstTests"/>'s own helper, one file along.</summary>
    private static string Code(string source)
    {
        string noBlock = Regex.Replace(source, @"/\*.*?\*/", " ", RegexOptions.Singleline);
        return Regex.Replace(noBlock, "//[^\n]*", " ");
    }

    private static string Challenge() =>
        Code(Read("src", "SpaceSails.Client", "Pages", "Patrol", "Patrol.Challenge.cs"));

    private static string Stop() =>
        Code(Read("src", "SpaceSails.Client", "Pages", "Patrol", "Patrol.Stop.cs"));

    // ── (1) THE ARRIVAL RAISES THE SCENE, NOT THE VERDICT ───────────────────────────────────────────────

    /// <summary>
    /// <b>THE WALLET IS READ AT THE MOVE AND NOWHERE ELSE.</b>
    ///
    /// <para>This is the whole shape of the lane, as a fact about the source: the frame that puts a man at
    /// arm's length raises a CARD WITH FOUR THINGS ON IT, and the ladder is not walked until one of them is
    /// pressed. If the read stayed on the arrival, the moves would be four buttons on a card whose answer
    /// was already decided — which is the shape of every fake choice ever shipped.</para>
    ///
    /// <para>The two seams that judge a paper (<c>WalletChoice.WhatHappens</c> and
    /// <c>PatrolBeat.TheGuardReads</c>) therefore appear in the round's code exactly where the move is
    /// answered, and the arrival carries the scene instead.</para>
    ///
    /// <para><b>RED</b> by putting the read back on the arrival (<c>TheWalletIsRead(ex, stop, false)</c> at
    /// the end of <c>TheRoundStopsAtYou</c>): <i>the arrival walks the ladder — the moves decide
    /// nothing</i>. <b>RED</b> by raising the scene with no <c>StopUnderway</c>: <i>the arrival raises no
    /// scene</i>.</para>
    /// </summary>
    [Fact]
    public void TheArrivalRaisesTheSceneAndTheLadderWaitsForAMove()
    {
        string challenge = Challenge();
        string stop = Stop();

        Assert.True(
            challenge.Contains("StopUnderway = new Stop", StringComparison.Ordinal)
            && challenge.Contains("GuardStop.SceneFor(g.Plate)", StringComparison.Ordinal),
            "the arrival raises no scene — a checkpoint with no moves on it is #804's card again.");

        Assert.False(
            challenge.Contains("WalletChoice.WhatHappens", StringComparison.Ordinal)
            || challenge.Contains("PatrolBeat.TheGuardReads", StringComparison.Ordinal),
            "the arrival walks the ladder — the moves decide nothing.");

        // …and the ladder IS walked, once, where a move is answered.
        Assert.Contains("WalletChoice.WhatHappens", stop, StringComparison.Ordinal);
        Assert.Contains("PatrolBeat.TheGuardReads", stop, StringComparison.Ordinal);

        // The opening the card carries is the SCENE's, off Core, and the round composes no sentence of its
        // own about a checkpoint.
        Assert.Contains("GuardStop.Opening", challenge, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Pass.\"", challenge, StringComparison.Ordinal);
    }

    // ── (2) THE MOVES ARE INSIDE THE CARD, AND THE CHAIN IS WIRED ALL THE WAY DOWN ──────────────────────

    /// <summary>
    /// <b>ONE CARD, AND THE MOVES ARE IN ITS OWN SUBTREE.</b>
    ///
    /// <para>#680's law, which this scene's sibling learned expensively: the pulse HUD renders UNDER the
    /// modal backdrop and its blur, so anything a captain has to act on belongs inside the panel that is up.
    /// The move row is therefore between the <c>OverlayShell</c>'s own tags on the view-object card — the
    /// card the stop has been told on since #684 — and not a surface of its own, because two cards on one
    /// screen is the stacked-card mistake #777 named.</para>
    ///
    /// <para>The parameter chain is walked WHOLE — Map.razor → AtArmsLengthRack → ViewObjectCard — because a
    /// row wired at two of three levels is a row nobody can press, and the razor generator would not say
    /// so.</para>
    ///
    /// <para><b>RED</b> by dropping <c>TheStopMove</c> from the rack's invocation in Map.razor: <i>Map.razor
    /// does not carry the stop's moves — they are wired at fewer than 3 levels</i>. <b>RED</b> by moving the
    /// move row out below <c>&lt;/OverlayShell&gt;</c>: <i>the moves are drawn outside the card's own
    /// subtree</i>.</para>
    /// </summary>
    [Fact]
    public void TheMovesAreDrawnInsideTheOneCardAndAreWiredAtEveryLevel()
    {
        string card = Read("src", "SpaceSails.Client", "Pages", "Map", "ViewObjectCard.razor");

        int shell = card.IndexOf("<OverlayShell", StringComparison.Ordinal);
        int closed = card.IndexOf("</OverlayShell>", StringComparison.Ordinal);
        int moves = card.IndexOf("TheStopIsWaitingOnAMove", StringComparison.Ordinal);
        Assert.True(shell > 0 && closed > shell, "the view-object card no longer renders an OverlayShell.");
        Assert.True(
            moves > shell && moves < closed,
            "the moves are drawn outside the card's own subtree — #680: a control under the backdrop's blur "
            + "is a control nobody can read.");

        // The row presses the page's own verb and says why when it is refused (#603).
        Assert.Contains("TheStopMove(m.Id)", card, StringComparison.Ordinal);
        Assert.Contains("TheStopMoveRefusal(m)", card, StringComparison.Ordinal);
        Assert.Contains("disabled=\"@(!open)\"", card, StringComparison.Ordinal);

        // …and it composes no label of its own: every word on every button is Core's.
        Assert.Contains("@m.Label", card, StringComparison.Ordinal);
        Assert.DoesNotContain("SHOW THE PASS", card, StringComparison.Ordinal);

        foreach ((string what, string[] path) in new (string, string[])[]
        {
            ("Map.razor", ["src", "SpaceSails.Client", "Pages", "Map.razor"]),
            ("AtArmsLengthRack.razor",
                ["src", "SpaceSails.Client", "Pages", "Map", "AtArmsLengthRack.razor"]),
            ("ViewObjectCard.razor", ["src", "SpaceSails.Client", "Pages", "Map", "ViewObjectCard.razor"]),
        })
        {
            string text = Read(path);
            Assert.True(
                text.Contains("TheStopIsWaitingOnAMove", StringComparison.Ordinal)
                && text.Contains("TheStopMove", StringComparison.Ordinal)
                && text.Contains("TheStopsMoves", StringComparison.Ordinal),
                $"{what} does not carry the stop's moves — they are wired at fewer than 3 levels and cannot "
                + "be pressed.");
        }
    }

    // ── (3) CLOSING IS DECIDING ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE ✕ IS THE SCENE'S OWN EXIT MOVE.</b>
    ///
    /// <para>#992's general ruling — <i>there should not be a pop-up that cannot be closed or minimized</i>
    /// — and this scene meet at one line. The card keeps its ✕, its backdrop and its Esc, and every one of
    /// them goes through <c>CloseViewObject</c>; a stop that survived that would be the one pop-up in the
    /// game that paid a captain for ignoring it. So the closing presses SAY NOTHING, which is #615's own
    /// idiom on the card next door (<i>"closing IS Leave"</i>) — and it is why the checkpoint needs no
    /// special case in the closing law at all.</para>
    ///
    /// <para><b>RED</b> by dropping the call from <c>CloseViewObject</c>: <i>closing the card does not answer
    /// the stop</i>.</para>
    /// </summary>
    [Fact]
    public void ClosingTheCardIsTheDecisionAndTheStopCannotSurviveIt()
    {
        string fixtures = Code(Read("src", "SpaceSails.Client", "Pages", "Map.Deck.Fixtures.cs"));
        int closing = fixtures.IndexOf("private void CloseViewObject()", StringComparison.Ordinal);
        Assert.True(closing > 0, "CloseViewObject moved — this guard is watching a method that is gone.");

        Assert.Contains(
            "TheStopIsAnsweredByTheClosing();",
            fixtures[closing..],
            StringComparison.Ordinal);

        // …and what it presses is the scene's own exit id, never a second spelling of it.
        string page = Code(Read("src", "SpaceSails.Client", "Pages", "Map.Patrol.Challenge.cs"));
        Assert.Contains("GuardStop.Nothing", page, StringComparison.Ordinal);
        Assert.Contains("byClosing: true", page, StringComparison.Ordinal);
    }

    // ── (4) A HELPFUL WALK IS FREE, AND IT CAN NEVER END AT THE SKY ─────────────────────────────────────

    /// <summary>
    /// <b>THE SAME LEGS, AND NONE OF THE BILL.</b>
    ///
    /// <para>A man showing you to the lift because you asked him the way walks the escort that already
    /// exists — the route he plans, the pace ahead of him (#804), the pumps, both of you on the fan — and it
    /// would be absurd for that to arrive with #715's crossing, a rung of his patience and a ticket to the
    /// regolith attached. Three lines, and each of them would be wrong in a different way if it were
    /// missing: the crossing is skipped, the escort counter is not spent, and <c>KickOutDue</c> cannot be
    /// true on a free walk.</para>
    ///
    /// <para><b>RED</b> by banking the crossing unconditionally: <i>the free walk banks a crossing</i>.
    /// <b>RED</b> by dropping <c>!EscortIsFree</c> from <c>KickOutDue</c>: <i>a captain who asked for
    /// directions could be walked to the sky</i>.</para>
    /// </summary>
    [Fact]
    public void TheHelpfulWalkSkipsTheCrossingAndCanNeverEndAtTheSky()
    {
        string floor = Code(Read("src", "SpaceSails.Client", "Pages", "Patrol", "Patrol.Floor.cs"));
        string escort = Code(Read("src", "SpaceSails.Client", "Pages", "Patrol", "Patrol.Escort.cs"));

        Assert.Contains("if (!EscortIsFree)", floor, StringComparison.Ordinal);
        Assert.Contains(
            "TheHeatOfBeingWalkedOut(ex, book, simTime, IllegalHeat.Crossing.TheEscort);",
            floor,
            StringComparison.Ordinal);

        // The YES-BUT's rung is banked by the frame loop, through the SAME seam, and waits for no card.
        Assert.Contains(
            "IllegalHeat.Crossing.YourNameInTheirBook", floor, StringComparison.Ordinal);

        // …and a free walk spends nothing of his patience and can never end at the sky.
        Assert.Contains(
            "KickOutDue = !EscortIsFree && PatrolBeat.BookedTooOften(EscortsThisWatch);",
            escort,
            StringComparison.Ordinal);
        Assert.Contains("if (!EscortIsFree)", escort, StringComparison.Ordinal);
    }

    // ── (5) THE CHEAT REACHES THE SCENE ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b><c>?roll=hi|lo</c> REACHES THE ROUND.</b> A scene nobody can reach on demand is a scene that ships
    /// broken, and until this line the cheat reached the canteen and stopped at the corridor.
    ///
    /// <para>It is a field of the ROUND rather than a twenty-second member of <c>IPatrolHost</c>, which may
    /// only shrink — so this guard also pins that the interface did not grow for it.</para>
    ///
    /// <para><b>RED</b> by dropping <c>_patrol.RollCheat = _rollCheat;</c>: <i>the roll cheat does not reach
    /// the round</i>.</para>
    /// </summary>
    [Fact]
    public void TheRollCheatReachesTheCheckpointWithoutWideningTheHost()
    {
        string cheats = Code(Read("src", "SpaceSails.Client", "Pages", "Map.Sim.World.QueryArcs.cs"));
        Assert.Contains("_patrol.RollCheat = _rollCheat;", cheats, StringComparison.Ordinal);

        string host = Code(Read("src", "SpaceSails.Client", "Pages", "Patrol", "IPatrolHost.cs"));
        Assert.DoesNotContain("RollCheat", host, StringComparison.Ordinal);
        Assert.DoesNotContain("Nerve {", host, StringComparison.Ordinal);

        // The nerve reaches the round as an ANSWER, off Core's own rungs, from the page.
        string page = Code(Read("src", "SpaceSails.Client", "Pages", "Map.Patrol.Challenge.cs"));
        Assert.Contains("Encounter.NerveReadsAcrossATable(_nerve)", page, StringComparison.Ordinal);
    }
}
