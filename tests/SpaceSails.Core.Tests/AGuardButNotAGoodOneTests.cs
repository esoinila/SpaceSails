using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #618 / #582 slice 1 · <b>A GUARD, BUT NOT A GOOD ONE — THE LAW.</b> One seeded state per (ground, opening
/// window) and nothing else deciding it; the canon to the byte; the moves that exist and only those; the tide's
/// answer at the mouth; the dev latch. Each fact names the revert that turned it RED.
/// </summary>
public sealed class AGuardButNotAGoodOneTests
{
    private static readonly string[] Grounds = ["luna", "phobos", "titan", "secret-lab-site", "miranda"];

    /// <summary>
    /// <b>EXACTLY ONE STATE PER (GROUND, WINDOW).</b> Asked twice, the same answer; over a thousand windows, all
    /// three postings occur, posted is the most common, and the word works about one window in three; a
    /// different window is allowed a different man.
    ///
    /// <para><b>Proven RED</b> by returning <c>Posting.Posted</c> unconditionally from <see cref="GateGuard.For"/>:
    /// <c>Assert.True() Failure — luna never finds him absent</c>.</para>
    /// </summary>
    [Fact]
    public void ThereIsExactlyOneStatePerGroundAndWindow()
    {
        foreach (string ground in Grounds)
        {
            var seen = new Dictionary<GateGuard.Posting, int>();
            int talks = 0;
            for (long w = 0; w < 1000; w++)
            {
                GateGuard.Visit a = GateGuard.For(ground, w);
                Assert.Equal(a, GateGuard.For(ground, w));
                seen[a.Posting] = seen.GetValueOrDefault(a.Posting) + 1;
                talks += a.TalkWorks ? 1 : 0;
            }

            Assert.True(seen.GetValueOrDefault(GateGuard.Posting.Absent) > 0, $"{ground} never finds him absent");
            Assert.True(seen.GetValueOrDefault(GateGuard.Posting.OnRound) > 0, $"{ground} never finds him on his round");
            Assert.True(seen[GateGuard.Posting.Posted] > seen[GateGuard.Posting.Absent]
                        && seen[GateGuard.Posting.Posted] > seen[GateGuard.Posting.OnRound],
                $"{ground}: posted is not the most common window");
            Assert.InRange(talks, 250, 420);
        }

        Assert.NotEqual(
            Enumerable.Range(0, 50).Select(w => GateGuard.For("luna", w)),
            Enumerable.Range(0, 50).Select(w => GateGuard.For("titan", w)));
    }

    /// <summary>
    /// <b>THE CANON, TO THE BYTE</b> — Fable's lines as the brief wrote them, and the list that holds them.
    ///
    /// <para><b>Proven RED</b> by changing the apostrophe in <c>'Go on, then.'</c> to a typographic one.</para>
    /// </summary>
    [Fact]
    public void TheLinesAreFablesVerbatim()
    {
        Assert.Equal("Gate", GateGuard.Plate);
        Assert.Equal("THE MAN AT THE DOOR", GateGuard.CardTitle);
        Assert.Equal("A man in a coat that was issued to somebody bigger. He looks at your hands, then at your face, then at your hands again.", GateGuard.ApproachLine);
        Assert.Equal("He reads the badge longer than a badge takes to read. 'Go on, then.'", GateGuard.BadgeLine);
        Assert.Equal("TALK YOUR WAY IN", GateGuard.TalkLabel);
        Assert.Equal("'Contractor,' you say, and he nods the way a man nods at a word he has been told to expect.", GateGuard.TalkWorkedLine);
        Assert.Equal("'Nobody said.' He does not move, and he does not forget your face.", GateGuard.TalkFailedLine);
        Assert.Equal("MAKE A NOISE", GateGuard.NoiseLabel);
        Assert.Equal("You put something metal against something hollow. He comes off the wall like a man who has been waiting all shift for a reason.", GateGuard.NoiseLine);
        Assert.Equal("He follows you into the light, which is the one thing he was told not to do. The tide does the rest. You do not watch.", GateGuard.TideTakesHimLine);
        Assert.Equal("He stops at the mouth of the tube, where the coat says he is allowed to stop, and goes back the way he came.", GateGuard.TideDownLine);
        Assert.Equal("The chair by the door has a coat on it. The coat has no man in it.", GateGuard.AbsentLine);
        Assert.Equal("He walks his round the way a man walks to the canteen. Six minutes, if the canteen is where you think it is.", GateGuard.RoundLine);
        Assert.Equal("Past the man at the door. He is not the reason nobody comes down here.", GateGuard.BookLine);
        Assert.Equal("📍", GateGuard.BookGlyph);

        Assert.Equal(14, GateGuard.AllProse().Count());
        Assert.Equal(14, GateGuard.AllProse().Distinct(StringComparer.Ordinal).Count());
    }

    /// <summary>
    /// <b>NOBODY SAYS SECURITY, THE AUTHORITY IS NEVER MENTIONED, AND NOT ONE WORD SPENDS A RESERVED WORD.</b>
    ///
    /// <para><b>Proven RED</b> by appending <c>" Security."</c> to the noise line.</para>
    /// </summary>
    [Fact]
    public void NotOneWordSaysWhatHeIsFor()
    {
        string[] forbidden =
        [
            "security", "authority", "monolith", "reever", "old one", "ancient", "alien", "not ours",
            "restore", "backup", "kaamos", "minister", "donor",
        ];
        foreach (string line in GateGuard.AllProse())
        {
            foreach (string word in forbidden)
            {
                Assert.DoesNotContain(word, line, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    /// <summary>
    /// <b>ONLY THE MOVES THAT EXIST.</b> A spent TALK is ABSENT from the list (the card draws the list and nothing
    /// else — there is no refused control to grey); the badge move exists only with a pass that passes, and wears
    /// #605's own label; MAKE A NOISE is always there.
    ///
    /// <para><b>Proven RED</b> by always adding the talk move: <c>Assert.DoesNotContain() Failure — gate:talk</c>.</para>
    /// </summary>
    [Fact]
    public void ASpentWordIsAbsentAndABadgeIsOfferedOnlyWhenItPasses()
    {
        IReadOnlyList<Encounter.Move> fresh = GateGuard.MovesOnTheCard(aBadgePasses: false, faceRemembered: false);
        Assert.Equal([GateGuard.TalkMove, GateGuard.NoiseMove], fresh.Select(m => m.Id));

        IReadOnlyList<Encounter.Move> spent = GateGuard.MovesOnTheCard(aBadgePasses: false, faceRemembered: true);
        Assert.DoesNotContain(spent, m => m.Id == GateGuard.TalkMove);
        Assert.Equal([GateGuard.NoiseMove], spent.Select(m => m.Id));

        IReadOnlyList<Encounter.Move> carded = GateGuard.MovesOnTheCard(aBadgePasses: true, faceRemembered: true);
        Assert.Equal([GateGuard.ShowMove, GateGuard.NoiseMove], carded.Select(m => m.Id));
        Assert.Equal(GuardStop.ShowLabel, carded[0].Label);
    }

    /// <summary>
    /// <b>THE PASS THAT PASSES IS THE LADDER'S, NOT A SECOND JUDGE.</b> This site's pass on its top floor passes;
    /// another site's does not; an empty wallet has nothing.
    ///
    /// <para><b>Proven RED</b> by accepting any <c>Satchel.Kind.Badge</c>: the other site's pass is returned.</para>
    /// </summary>
    [Fact]
    public void ThePassThatPassesIsTheBuildingsOwnLadder()
    {
        const string body = "luna";
        int floor = UndergroundComplex.TopPressurisedFloor(body)!.Value;
        Satchel.Item ours = PatrolBeat.Badge(body);
        Satchel.Item theirs = PatrolBeat.Badge("titan");

        Assert.Equal(WalletChoice.Outcome.Worked, WalletChoice.WhatHappens(body, floor, 7, ours));
        Assert.Equal(ours, GateGuard.ThePassThatPasses(body, floor, 7, [theirs, ours]));
        Assert.Null(GateGuard.ThePassThatPasses(body, floor, 7, [theirs]));
        Assert.Null(GateGuard.ThePassThatPasses(body, floor, 7, []));
    }

    /// <summary>
    /// <b>THE TIDE DECIDES AT THE MOUTH.</b> A tide Old One on the field takes him on the spot; a quiet field sends
    /// him back once one of the tide's own mean gaps has passed; before that he is standing.
    ///
    /// <para><b>Proven RED</b> by swapping the two answers.</para>
    /// </summary>
    [Fact]
    public void TheTideDecidesAtTheMouth()
    {
        Assert.Equal(ReeverTide.MeanGapSeconds, GateGuard.AtTheMouthSeconds);
        Assert.Equal(GateGuard.AtTheMouth.Taken, GateGuard.TheTideAnswers(0.0, aTideOneIsUp: true));
        Assert.Equal(GateGuard.AtTheMouth.Standing, GateGuard.TheTideAnswers(1.0, aTideOneIsUp: false));
        Assert.Equal(GateGuard.AtTheMouth.GoesBack,
            GateGuard.TheTideAnswers(GateGuard.AtTheMouthSeconds, aTideOneIsUp: false));
    }

    /// <summary>
    /// <b>SIX MINUTES IS SIX MINUTES.</b> The walk back sets off early by exactly the walk out, so the door is
    /// empty for the six minutes the line says.
    ///
    /// <para><b>Proven RED</b> by ignoring the walk out: he heads back at 360 s, not 330.</para>
    /// </summary>
    [Fact]
    public void TheRoundIsSixMinutesAway()
    {
        Assert.Equal(360.0, GateGuard.AwaySeconds);
        Assert.False(GateGuard.HeHeadsBack(329.0, walkOutSeconds: 30.0));
        Assert.True(GateGuard.HeHeadsBack(330.0, walkOutSeconds: 30.0));
    }

    /// <summary><b>THE DEV LATCH</b> reads <c>guard=</c> off an address and nothing else.</summary>
    [Fact]
    public void TheDevLatchReadsTheAddress()
    {
        Assert.Equal(GateGuard.Posting.Posted, GateGuard.CheatIn("http://x/map?secretlab=1&land=1&guard=posted"));
        Assert.Equal(GateGuard.Posting.Absent, GateGuard.CheatIn("http://x/map?guard=absent"));
        Assert.Equal(GateGuard.Posting.OnRound, GateGuard.CheatIn("http://x/map?guard=round&land=1"));
        Assert.Null(GateGuard.CheatIn("http://x/map?secretlab=1&land=1"));
        Assert.Null(GateGuard.CheatIn("http://x/map?guard=nonsense"));
        Assert.Null(GateGuard.CheatIn(null));
    }
}
