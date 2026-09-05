using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #326 · <b>NOTHING COMES BETWEEN THE CAPTAIN AND THE SHUTTLE.</b> Owner, live 2026-07-18: <i>"I think the
/// securibots most important job is not let anything come between me and the shuttle :-D"</i>, and the
/// bodyguard half of it the same day: <i>"Protect the path to the ship at about half way so there is always a
/// way to retreat back to safety (until bullets run out) :-D"</i>
///
/// <h3>The world these guards are built in, and why it is built that way</h3>
/// <para>Every case here stands on a field where <b>the two rules give different answers</b> — a Reever
/// standing 15 du off the gun and NOT in the corridor, and one standing 18 du off the gun that IS. If the
/// corridor test were loose enough to catch both, or the priority never fired, these would agree and the file
/// would be green about nothing at all. That is this repository's fifth named bug class, and it is the one a
/// proximity threshold invites, so the distinguishing facts are asserted <b>out loud</b> in
/// <see cref="TheCorridorCanTellTheTwoRulesApart"/> before any case leans on them.</para>
///
/// <h3>Proven able to fail</h3>
/// <list type="bullet">
/// <item>Reverting <c>SentryBot.Step</c>'s pick to the nearest-visible loop it shipped with reddens
/// <see cref="TheOneInTheCorridorIsShotBeforeTheOneStandingUnderTheGun"/> and
/// <see cref="AVolleyPutsItsRoundIntoTheCorridorThreat"/>.</item>
/// <item>Widening <see cref="SentryDoctrine.CorridorHalfWidthDu"/> to the whole arc reddens
/// <see cref="TheCorridorCanTellTheTwoRulesApart"/> — the threshold that selects everything, caught by the
/// guard written to catch it.</item>
/// <item>Returning the raw midpoint from <see cref="SentryDoctrine.HoldingPoint"/> reddens
/// <see cref="APostInsideAWallSlidesToTheNearestLegalSpotOnTheLine"/>.</item>
/// <item>Pinning the post to the deploy-time captain instead of the live one reddens
/// <see cref="ThePostIsRecomputedEveryTimeTheCaptainMoves"/>.</item>
/// </list>
/// </summary>
public sealed class NothingComesBetweenTheCaptainAndTheShuttleTests
{
    /// <summary>The captain, out in the field with his back to the deep.</summary>
    private const double CaptainX = 0.0, CaptainY = 0.0;

    /// <summary>The way home, 40 du up-field — a walk long enough to be cut, which is the whole scenario.</summary>
    private const double HomeX = 0.0, HomeY = -40.0;

    private static SentryDoctrine.RetreatLine TheLine =>
        new(CaptainX, CaptainY, HomeX, HomeY);

    /// <summary>The bot on its post: the middle of that line, which is where the escort stance puts it.</summary>
    private static SentryBot.Deployed TheGuard =>
        new("K-77", 0.0, -20.0, SentryBot.MaxMagazine);

    /// <summary><b>Off the road and close to the gun.</b> 15 du from the bot — well inside the 22 du arc —
    /// and 15 du off the corridor, which is outside the 11 du half-width. Under the old rule this is what
    /// every round went into.</summary>
    private static SentryBot.Target UnderTheGun => new(15.0, -20.0, 0);

    /// <summary><b>On the road and further off.</b> 2 du from the line, so squarely in the corridor, and
    /// 18.1 du from the bot — further away than <see cref="UnderTheGun"/>, and still inside the arc. This is
    /// the one cutting the captain off, and the one the doctrine exists to shoot.</summary>
    private static SentryBot.Target InTheCorridor => new(2.0, -2.0, 0);

    private static double From(SentryBot.Deployed bot, SentryBot.Target t) =>
        Math.Sqrt(((t.X - bot.X) * (t.X - bot.X)) + ((t.Y - bot.Y) * (t.Y - bot.Y)));

    // ── (a) THE WORLD CAN TELL PASS FROM FAIL ─────────────────────────────────────────────────────────

    /// <summary>
    /// #326 · <b>THE TWO RULES DISAGREE ON THIS FIELD</b>, and here is the arithmetic that says so, asserted
    /// rather than assumed.
    ///
    /// <para>Both Old Ones are inside the arc, so both are shootable and the choice is real. One is nearer.
    /// The other one — and only the other one — is in the corridor. A half-width that caught both, or a range
    /// that caught neither, would leave every case in this file passing on a world with nothing to decide.
    /// </para>
    ///
    /// <para>The last assertion is the derivation itself: the corridor must be strictly INSIDE the arc. Set
    /// them equal and "threatens the line" becomes a synonym for "is in range", the priority rule selects
    /// everything, and the feature is a no-op wearing a comment.</para>
    /// </summary>
    [Fact]
    public void TheCorridorCanTellTheTwoRulesApart()
    {
        SentryBot.Deployed bot = TheGuard;

        Assert.True(
            From(bot, UnderTheGun) < From(bot, InTheCorridor),
            "the near one is not nearer: this world cannot tell a corridor pick from a nearest pick.");
        Assert.True(SentryBot.InRange(bot.X, bot.Y, UnderTheGun.X, UnderTheGun.Y));
        Assert.True(SentryBot.InRange(bot.X, bot.Y, InTheCorridor.X, InTheCorridor.Y));

        Assert.False(
            SentryDoctrine.ThreatensTheLine(TheLine, UnderTheGun.X, UnderTheGun.Y),
            $"the near one is being counted as a corridor threat at a half-width of "
            + $"{SentryDoctrine.CorridorHalfWidthDu} du — every shootable target now 'threatens the line' "
            + "and the priority selects everything.");
        Assert.True(SentryDoctrine.ThreatensTheLine(TheLine, InTheCorridor.X, InTheCorridor.Y));

        // The derivation, in one line: half an arc, and strictly inside it.
        Assert.True(
            SentryDoctrine.CorridorHalfWidthDu < SentryBot.RangeDeckUnits,
            "a corridor as wide as the arc is a corridor that means nothing.");
        Assert.Equal(SentryBot.RangeDeckUnits / 2.0, SentryDoctrine.CorridorHalfWidthDu, 9);
    }

    // ── (b) TARGET PRIORITY IS THE LINE, NOT THE NEAREST ──────────────────────────────────────────────

    /// <summary>
    /// #326 clause 1 · <b>THE ONE IN THE CORRIDOR IS SHOT FIRST</b> — at any range, over whatever shambles
    /// closest to the gun.
    ///
    /// <para>And the anchor half, which is the half that keeps this from being a mute: hand the same field to
    /// <see cref="SentryDoctrine.Pick"/> with no line and the answer is the near one again, exactly as it was
    /// before #326 existed. The rule fires on the corridor and nothing else.</para>
    /// </summary>
    [Fact]
    public void TheOneInTheCorridorIsShotBeforeTheOneStandingUnderTheGun()
    {
        SentryBot.Deployed bot = TheGuard;
        SentryBot.Target[] field = [UnderTheGun, InTheCorridor];

        Assert.Equal(1, SentryDoctrine.Pick(bot, field, null, TheLine, null));
        Assert.Equal(0, SentryDoctrine.Pick(bot, field, null, null, null));

        // …and the listing order is not what decided it. Same field, other way round.
        SentryBot.Target[] swapped = [InTheCorridor, UnderTheGun];
        Assert.Equal(0, SentryDoctrine.Pick(bot, swapped, null, TheLine, null));
        Assert.Equal(1, SentryDoctrine.Pick(bot, swapped, null, null, null));
    }

    /// <summary>#326 · Within one class, nearest still decides and ties still fall to the lower index — the
    /// clause the issue is explicit about. Two Old Ones in the corridor, one nearer; then two at the same
    /// range.</summary>
    [Fact]
    public void InsideTheCorridorNearestStillDecidesAndTiesFallToTheFirst()
    {
        SentryBot.Deployed bot = TheGuard;

        var near = new SentryBot.Target(0.0, -14.0, 0);   //  6 du off the gun, on the line
        var far = new SentryBot.Target(0.0, -2.0, 0);     // 18 du off the gun, on the line
        Assert.Equal(1, SentryDoctrine.Pick(bot, [far, near], null, TheLine, null));

        var left = new SentryBot.Target(-5.0, -20.0, 0);
        var right = new SentryBot.Target(5.0, -20.0, 0);
        Assert.Equal(0, SentryDoctrine.Pick(bot, [left, right], null, TheLine, null));
        Assert.Equal(0, SentryDoctrine.Pick(bot, [right, left], null, TheLine, null));
    }

    /// <summary>#326 · Nothing in the corridor, so the corridor decides nothing: the bot shoots the nearest
    /// thing it can see, which is the #314 behaviour and still the right one. A doctrine that changed the
    /// answer on a field it has no opinion about would be a regression wearing a feature's name.</summary>
    [Fact]
    public void WithNothingOnTheRoadTheNearestIsStillTheAnswer()
    {
        SentryBot.Deployed bot = TheGuard;
        var nearer = new SentryBot.Target(14.0, -20.0, 0);
        var further = new SentryBot.Target(20.0, -20.0, 0);

        Assert.False(SentryDoctrine.ThreatensTheLine(TheLine, nearer.X, nearer.Y));
        Assert.False(SentryDoctrine.ThreatensTheLine(TheLine, further.X, further.Y));
        Assert.Equal(0, SentryDoctrine.Pick(bot, [nearer, further], null, TheLine, null));
    }

    /// <summary>#326 · …and a corridor threat it cannot SEE is not a target. #437's maze law outranks the
    /// doctrine: a slab between the gun and the road breaks the shot, and the bot takes the one it can hit
    /// instead of holding out for one it cannot.</summary>
    [Fact]
    public void AThreatBehindStoneIsStillBehindStone()
    {
        SentryBot.Deployed bot = TheGuard;
        SentryBot.Target[] field = [UnderTheGun, InTheCorridor];

        // A slab across the bot's line to the corridor threat, and nowhere near its line to the near one.
        SurfaceCollision.Segment[] wall = [new(-6.0, -10.0, 6.0, -10.0)];

        Assert.False(
            SurfaceCollision.HasLineOfSight(bot.X, bot.Y, InTheCorridor.X, InTheCorridor.Y, wall),
            "this bench's slab does not actually stand between the gun and the road.");
        Assert.True(SurfaceCollision.HasLineOfSight(bot.X, bot.Y, UnderTheGun.X, UnderTheGun.Y, wall));
        Assert.Equal(0, SentryDoctrine.Pick(bot, field, null, TheLine, wall));
    }

    /// <summary>#326 · A target downed earlier in the same volley is off the board, corridor or not — the
    /// no-shot-on-a-corpse law the volley already ran under.</summary>
    [Fact]
    public void ACorpseInTheCorridorIsNotATarget()
    {
        SentryBot.Deployed bot = TheGuard;
        SentryBot.Target[] field = [UnderTheGun, InTheCorridor];

        Assert.Equal(0, SentryDoctrine.Pick(bot, field, [true, false], TheLine, null));
    }

    // ── (c) THE VOLLEY ITSELF, AND THE MAGAZINE LAW UNDER IT ──────────────────────────────────────────

    /// <summary>
    /// #326 · <b>THE ROUND ACTUALLY GOES INTO THE CORRIDOR THREAT</b>, driven through the shipping volley
    /// rather than through the picker on its own — a priority that never reaches a trigger is a priority
    /// nobody has.
    /// </summary>
    [Fact]
    public void AVolleyPutsItsRoundIntoTheCorridorThreat()
    {
        SentryBot.Deployed[] bots = [TheGuard];
        SentryBot.Target[] field = [UnderTheGun, InTheCorridor];

        SentryBot.Volley held = SentryBot.Step(bots, field, null, null, TheLine);
        Assert.Equal(0, held.Reevers[0].HitsTaken);   // the near one was left alone
        Assert.Equal(1, held.Reevers[1].HitsTaken);   // …and the one cutting the road took it

        SentryBot.Volley old = SentryBot.Step(bots, field);
        Assert.Equal(1, old.Reevers[0].HitsTaken);
        Assert.Equal(0, old.Reevers[1].HitsTaken);
    }

    /// <summary>
    /// #326 clause 5 · <b>ONE MAGAZINE LAW, WHICHEVER WAY THE PICK WENT.</b> The doctrine decides WHO is
    /// shot and never how much it costs: one trigger pull, one round off the drum, the same
    /// <see cref="SentryBot.RoundsPerReever"/> to put one down, and a bot at 00 firing nothing either way.
    ///
    /// <para>Asserted by driving the same bot to empty twice — once with a corridor to hold and once without
    /// — and comparing the drum tick for tick. The countdown IS the retreat timer, so a stance that spent
    /// rounds faster or slower than the other would quietly be a different promise.</para>
    /// </summary>
    [Fact]
    public void TheDrumTicksTheSameWayWithACorridorAndWithout()
    {
        static List<int> Drain(SentryDoctrine.RetreatLine? line)
        {
            SentryBot.Deployed[] bots = [TheGuard];
            SentryBot.Target[] field = [UnderTheGun, InTheCorridor];
            var drum = new List<int>();
            for (int tick = 0; tick < 40; tick++)
            {
                SentryBot.Volley v = SentryBot.Step(bots, field, null, null, line);
                bots = [.. v.Bots];
                field = [.. v.Reevers];
                drum.Add(bots[0].Rounds);
            }
            return drum;
        }

        List<int> holdingTheLine = Drain(TheLine);
        List<int> holdingItsArc = Drain(null);

        Assert.Equal(holdingItsArc, holdingTheLine);

        // …and it really drained, one round a pull, so the agreement above is not two nothings agreeing. Two
        // Old Ones at RoundsPerReever apiece is the whole bill, and the drum stops there because the field
        // is empty and a bot with nothing to see holds fire (#437's no-shot/no-drain law).
        Assert.Equal(SentryBot.MaxMagazine - 1, holdingTheLine[0]);
        Assert.Equal(SentryBot.MaxMagazine - (2 * SentryBot.RoundsPerReever), holdingTheLine[^1]);

        // …and a dry bot is dry under either doctrine: no pull, no drain, no husk.
        SentryBot.Volley spent = SentryBot.Step(
            [TheGuard with { Rounds = 0 }], [UnderTheGun, InTheCorridor], null, null, TheLine);
        Assert.Equal(0, spent.Shots);
        Assert.Empty(spent.Husks);
        Assert.Equal(0, spent.Bots[0].Rounds);
    }

    /// <summary>#326 · A downed Old One in the corridor still leaves the husk it always left, at the spot it
    /// fell. The doctrine changed the aim and nothing else — the forensic mark #316 reads is untouched, and
    /// no second kind of mark was minted for the retreat line.</summary>
    [Fact]
    public void TheCorridorThreatStillFallsToAnOrdinaryHusk()
    {
        SentryBot.Deployed[] bots = [TheGuard];
        SentryBot.Target[] field = [UnderTheGun, InTheCorridor with { HitsTaken = SentryBot.RoundsPerReever - 1 }];

        SentryBot.Volley v = SentryBot.Step(bots, field, null, null, TheLine);

        SentryBot.Husk fell = Assert.Single(v.Husks);
        Assert.Equal(InTheCorridor.X, fell.X, 9);
        Assert.Equal(InTheCorridor.Y, fell.Y, 9);
        Assert.Single(v.Reevers);                       // the near one is still standing
        Assert.Equal(UnderTheGun.X, v.Reevers[0].X, 9);
    }

    // ── (d) THE BODYGUARD'S POST ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #326 clause 2 · <b>HALFWAY, AND RECOMPUTED AS THE CAPTAIN MOVES.</b> Owner: <i>"Protect the path to
    /// the ship at about half way"</i> — and the recomputation is the half of it that makes it a bodyguard
    /// rather than a second kind of post.
    ///
    /// <para>The captain walks a leg of a real excursion and the post follows him every step: three
    /// positions, three different middles, each exactly half of the live line. A post pinned to where he
    /// stood at deploy time would sit still through all three.</para>
    /// </summary>
    [Fact]
    public void ThePostIsRecomputedEveryTimeTheCaptainMoves()
    {
        var walk = new (double X, double Y)[] { (0, 0), (30, 0), (30, 24) };
        var posts = new List<(double X, double Y)>();

        foreach ((double x, double y) in walk)
        {
            var line = new SentryDoctrine.RetreatLine(x, y, HomeX, HomeY);
            (double X, double Y) post = SentryDoctrine.HoldingPoint(line, DeckRadius, null);
            posts.Add(post);

            Assert.Equal((x + HomeX) / 2.0, post.X, 9);
            Assert.Equal((y + HomeY) / 2.0, post.Y, 9);
        }

        // Three steps, three DIFFERENT posts — the guard against a "recomputed" post that never moves.
        Assert.Equal(3, posts.Distinct().Count());
    }

    /// <summary>
    /// #326 clause 2 · <b>WALLS RESPECTED — the nearest legal point ALONG THE LINE.</b>
    ///
    /// <para>A slab is laid across the exact middle of the corridor. Three things are asserted, and the third
    /// is the one that makes it "nearest" rather than "somewhere": the post is still ON the line, a body fits
    /// where it stands, and <b>everything between it and the middle is stone</b> — walked out one probe at a
    /// time, so a lazy implementation that jumped to a comfortable spot further down the road goes red.</para>
    ///
    /// <para>And it slides to the CAPTAIN'S side of the slab, which is the side a bodyguard forced off its
    /// mark belongs on.</para>
    /// </summary>
    [Fact]
    public void APostInsideAWallSlidesToTheNearestLegalSpotOnTheLine()
    {
        SurfaceCollision.Segment[] slab = [new(-5.0, -20.0, 5.0, -20.0)];
        (double X, double Y) middle = (0.0, -20.0);

        Assert.True(
            SurfaceCollision.Blocked(middle.X, middle.Y, DeckRadius, slab),
            "this bench's slab is not actually on the midpoint; there is nothing here to slide off.");

        (double X, double Y) post = SentryDoctrine.HoldingPoint(TheLine, DeckRadius, slab);

        Assert.Equal(0.0, post.X, 9);                                          // still on the line
        Assert.False(SurfaceCollision.Blocked(post.X, post.Y, DeckRadius, slab));
        Assert.True(post.Y > middle.Y, "it slid away from the captain rather than toward him.");

        // NEAREST, proven at the resolution the search actually sounds at: one probe nearer the middle — a
        // quarter of a body — and there is stone. A search that jumped to a comfortable spot further down
        // the road, or that walked past the first opening, fails here.
        double oneProbe = DeckRadius * SentryDoctrine.PostProbeInRadii;
        Assert.True(
            SurfaceCollision.Blocked(0.0, post.Y - oneProbe, DeckRadius, slab),
            $"there was a legal spot {oneProbe:0.###} du nearer the middle than the post at "
            + $"y={post.Y:0.###} — the search is not returning the nearest one.");

        // …and every point it walked PAST on the way out really was stone.
        for (double y = middle.Y; y < post.Y - oneProbe; y += 0.05)
        {
            Assert.True(SurfaceCollision.Blocked(0.0, y, DeckRadius, slab));
        }
    }

    /// <summary>#326 · A corridor walled off end to end has no post, and the doctrine says so by handing back
    /// the middle unchanged rather than inventing a route round the outside. Clause 4 rules out the formation
    /// AI that would be, and a captain sealed off from the tube is a scene, not a pathfinding problem.</summary>
    [Fact]
    public void ACorridorMadeEntirelyOfStoneHasNoPostToOffer()
    {
        SurfaceCollision.Segment[] sealedIn = [new(-5.0, 1.0, 5.0, 1.0), new(-5.0, -41.0, 5.0, -41.0),
            new(-0.2, 1.0, -0.2, -41.0), new(0.2, 1.0, 0.2, -41.0)];

        (double X, double Y) post = SentryDoctrine.HoldingPoint(TheLine, DeckRadius, sealedIn);

        Assert.Equal(0.0, post.X, 9);
        Assert.Equal(-20.0, post.Y, 9);
    }

    /// <summary>
    /// #326 · The walk itself: a step toward the post never overshoots it, and a frame's worth of walking
    /// never crosses a wall.
    ///
    /// <para>Overshoot matters because the post moves every frame — a bot that stepped past its mark would
    /// oscillate across the corridor for the whole excursion instead of holding it.</para>
    ///
    /// <para>The wall case is walked A FRAME AT A TIME, at the pace the page actually hands out, because
    /// that is #324's law rather than this file's: <c>SurfaceCollision.Slide</c> tests the destination, not
    /// the sweep, so every body in this game — the captain, the Old Ones, and now a bodyguard — is kept
    /// honest by the step being small. Two hundred frames of pressing into a slab and it is still on its own
    /// side of it.</para>
    /// </summary>
    [Fact]
    public void TheWalkToThePostNeitherOvershootsNorCrossesAWallAFrameAtATime()
    {
        (double X, double Y) post = (0.0, -20.0);

        (double X, double Y) arrived = SentryDoctrine.StepToward(0.0, -20.4, post, 10.0, DeckRadius, null);
        Assert.Equal(post.X, arrived.X, 9);
        Assert.Equal(post.Y, arrived.Y, 9);

        // A frame at the captain's own pace: 9 du/s at a clamped 0.1 s frame.
        const double AFrame = 0.9;
        SurfaceCollision.Segment[] slab = [new(-5.0, -10.0, 5.0, -10.0)];
        (double X, double Y) at = (0.0, 0.0);
        for (int frame = 0; frame < 200; frame++)
        {
            at = SentryDoctrine.StepToward(at.X, at.Y, post, AFrame, DeckRadius, slab);
        }

        Assert.False(SurfaceCollision.Blocked(at.X, at.Y, DeckRadius, slab));
        Assert.True(at.Y > -10.0, $"it walked through the slab to reach its post, ending at y={at.Y:0.###}.");
        Assert.True(at.Y < -8.0, "it never actually set off toward its post.");
    }

    /// <summary>The body a bot occupies, the same radius the captain and the Old Ones collide at. Written
    /// here rather than imported because <c>DeckPlan</c> is the client's; the number is asserted against the
    /// shipping one in the client suite's own bench, where it is reachable.</summary>
    private const double DeckRadius = 0.7;

    // ── (e) WHAT THE FEATURE IS ALLOWED TO SAY ────────────────────────────────────────────────────────

    /// <summary>
    /// #326 · <b>TWO NEW WORDS, AND THEY ARE THE OWNER'S.</b> The issue names them: <i>"[T] deploy here /
    /// hold my line home"</i>. Everything else this feature does — a priority, a post, a walk — is arithmetic,
    /// and arithmetic that starts explaining itself in prose is a feature that has quietly grown a second
    /// voice.
    ///
    /// <para>Swept off the shipping type by reflection, so a third phrase added tomorrow reddens this without
    /// anybody having to remember the rule.</para>
    /// </summary>
    [Fact]
    public void TheDoctrineSpendsExactlyTwoNewWordsAndTheyAreTheOwnersOwn()
    {
        string[] said = [.. typeof(SentryDoctrine)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .OrderBy(s => s, StringComparer.Ordinal)];

        Assert.Equal(["deploy here", "hold my line home"], said);
        Assert.Equal("deploy here", SentryDoctrine.DeployHereLabel);
        Assert.Equal("hold my line home", SentryDoctrine.HoldMyLineHomeLabel);
    }

    /// <summary>
    /// #326 · …and the source agrees with the reflection. The two files this lane ADDED are read off disk and
    /// every string literal in them counted: exactly the two labels, and nothing that turned out to be a
    /// sentence somebody slipped in behind a format string.
    ///
    /// <para>Read from disk on purpose. A sweep of the compiled type can only see constants; a lane that
    /// grew a line would grow it inline, in an interpolation, where reflection never looks.</para>
    /// </summary>
    [Fact]
    public void TheTwoFilesThisLaneAddedSayNothingElseAtAll()
    {
        string root = RepoRoot();
        string[] added =
        [
            Path.Combine(root, "src", "SpaceSails.Core", "SentryDoctrine.cs"),
            Path.Combine(root, "src", "SpaceSails.Client", "Pages", "Map.Surface.Escort.cs"),
        ];

        foreach (string file in added)
        {
            Assert.True(File.Exists(file), $"{file} is gone — this guard is about a file that no longer exists.");

            // Comments first: a docblock quoting the owner is not a string the game says.
            string code = string.Join(
                "\n",
                File.ReadLines(file).Where(l => !l.TrimStart().StartsWith("//", StringComparison.Ordinal)));

            string[] literals = [.. Regex.Matches(code, "\"([^\"\\\\]*(?:\\\\.[^\"\\\\]*)*)\"")
                .Select(m => m.Groups[1].Value)
                .Where(s => s.Length > 0)];

            foreach (string said in literals)
            {
                Assert.True(
                    said == SentryDoctrine.DeployHereLabel || said == SentryDoctrine.HoldMyLineHomeLabel,
                    $"{Path.GetFileName(file)} says \"{said}\" — this lane is allowed two phrases and they "
                    + "are the owner's own. A beat that wants a third leaves a `// FABLE: line needed` and "
                    + "the line gets written on the issue.");
            }
        }
    }

    /// <summary>§8's reserved word and the fifteen beside it. Nothing this feature says explains what any of
    /// this is FOR, which is the canon rule the whole ground is under — and here it is nearly free, because a
    /// doctrine that publishes two verbs has nothing to explain with.</summary>
    [Fact]
    public void NothingTheDoctrineSaysExplainsWhatAnyOfThisIsFor()
    {
        string[] forbidden =
        [
            "monolith", "old one", "old ones", "reever", "restore", "backup", "revive", "resurrect",
            "clone", "slave", "brain", "kaamos", "minister", "ancient", "alien", "experiment", "specimen",
        ];

        foreach (string said in new[] { SentryDoctrine.DeployHereLabel, SentryDoctrine.HoldMyLineHomeLabel })
        {
            foreach (string bad in forbidden)
            {
                Assert.DoesNotContain(bad, said, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    private static string RepoRoot()
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null)
        {
            if (Directory.Exists(Path.Combine(at.FullName, "src", "SpaceSails.Core")))
            {
                return at.FullName;
            }
            at = at.Parent;
        }
        throw new DirectoryNotFoundException($"could not find the repo root above {AppContext.BaseDirectory}");
    }
}
