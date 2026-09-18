using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #1062 slice 2 · <b>LOSING YOUR OWN TAIL</b>, held to the seven things it claims.
///
/// <para>Owner, 2026-09-01: <i>"… or trying to lose a tail our selves :-D"</i>. The half is worth nothing if
/// any of the following stops being true, so each is a separate fact with a separate revert behind it:</para>
///
/// <list type="number">
/// <item>WHO puts him there is somebody else's shipped folder, read at the game's own band — and a captain
/// nobody has written anything about is never followed;</item>
/// <item>the band he keeps IS the renderer's smear rung, asserted against the renderer's own ladder rather
/// than against the number that built it;</item>
/// <item><b>there is not one die in the whole half</b> — the source says so and the arithmetic proves it;</item>
/// <item>the two-door tell is the FIRE CODE's own count and a man in one doorway never adds up;</item>
/// <item>losing him costs exactly what finding him cost, off the one look cadence, never a literal;</item>
/// <item>the chair asks the ONE look and nothing else — asserted by calling both and comparing, over a sweep
/// where the walls have to matter;</item>
/// <item>the prose is enumerated, authored verbatim, filed under a PLACE and never a face, and free of the
/// reserved word and the pattern.</item>
/// </list>
/// </summary>
public sealed class TheTailBehindYouTests
{
    // ── 1 · WHO PUTS HIM THERE ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1062 · <b>THE TRIGGER IS #715's FOLDER, AT #715's OWN BAND</b> — and the anti-vacuity clause is the
    /// middle one: a captain with a cold book and a clean evening is NEVER followed, however many times the
    /// question is asked.
    ///
    /// <para>Without that clause this guard would pass on a <c>Follows</c> that answered true for everybody,
    /// which is the fifth named bug class exactly: a threshold that selects the whole world. So the sweep
    /// walks every heat from zero to the meter's own ceiling and asserts the answer CHANGES, at the rung the
    /// game already calls "the gate wants a face" and at no other.</para>
    ///
    /// <para><b>Revert that reddened it:</b> <c>Follows</c> written as <c>heat &gt; 0 || clocked</c> —
    /// <i>one refused card at a gate, months ago, and a man is behind you for ever</i>.</para>
    /// </summary>
    [Fact]
    public void TheFolderIsTheTriggerAndItIsTheGamesOwnBand()
    {
        bool everTrue = false, everFalse = false;
        for (int heat = 0; heat <= IllegalHeat.Ceiling; heat++)
        {
            bool follows = TheTailBehindYou.Follows(heat, youWereClockedTailingSomebody: false);

            // The band is the game's own and not a second opinion about when a company gets curious.
            Assert.Equal(IllegalHeat.TheGateWantsAFace(heat), follows);
            Assert.Equal(heat >= IllegalHeat.TheGateWantsAFaceAt, follows);

            everTrue |= follows;
            everFalse |= !follows;
        }

        Assert.True(everTrue && everFalse, "a trigger that answers the same for every heat is not a trigger.");

        // A cold book and a clean evening: nobody. This is most of most voyages and it must stay silent.
        Assert.False(TheTailBehindYou.Follows(0, youWereClockedTailingSomebody: false));

        // …and slice 1's own failure is the other door in. A captain who was CLOCKED following one of this
        // bar's regulars has advertised what he does, and the evening answers — with no folder at all.
        Assert.True(TheTailBehindYou.Follows(0, youWereClockedTailingSomebody: true));
    }

    // ── 2 · THE BAND HE KEEPS ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1062 · <b>BOTH ENDS OF HIS BAND ARE THE GAME'S OWN NUMBERS</b> — the range at which one person
    /// registers another, and the range at which a body on a deck is legible at all. Neither was invented
    /// here, and the guard says which is which so that a retune of #832's stack carries this feature with it
    /// instead of leaving it behind.
    ///
    /// <para>Anti-vacuity: the band must have ROOM in it (a band of zero width would satisfy every
    /// containment claim below and mean nothing), both reaches he tries must be INSIDE it (a target outside
    /// the band would be a man walking to a spot he immediately has to leave), and a range either side of it
    /// must be refused.</para>
    ///
    /// <para><b>Revert that reddened it:</b> <c>StandsOffDu</c> written as <c>0</c> — <i>a "tail" who walks
    /// up and stands on the captain's foot</i>.</para>
    /// </summary>
    [Fact]
    public void BothEndsOfHisBandAreTheGamesOwnNumbers()
    {
        Assert.Equal(PatrolBeat.NoticeDu, TheTailBehindYou.StandsOffDu, 9);
        Assert.Equal(FootTail.LegibleDu, TheTailBehindYou.LosesYouBeyondDu, 9);
        Assert.Equal(PatrolBeat.MarkerSightDu, TheTailBehindYou.LosesYouBeyondDu, 9);
        Assert.True(TheTailBehindYou.StandsOffDu < TheTailBehindYou.LosesYouBeyondDu,
            "a band with no room in it is not a band.");

        Assert.False(TheTailBehindYou.HoldsHisBand(TheTailBehindYou.StandsOffDu - 0.001));
        Assert.True(TheTailBehindYou.HoldsHisBand(TheTailBehindYou.StandsOffDu));
        Assert.True(TheTailBehindYou.HoldsHisBand(TheTailBehindYou.LosesYouBeyondDu));
        Assert.False(TheTailBehindYou.HoldsHisBand(TheTailBehindYou.LosesYouBeyondDu + 0.001));

        // Every reach he tries is a reach he may stand at, and he tries the FAR one first — as far back as
        // the room will let him.
        Assert.Equal(2, TheTailBehindYou.TheRangesHeTries.Count);
        Assert.All(TheTailBehindYou.TheRangesHeTries, r => Assert.True(TheTailBehindYou.HoldsHisBand(r)));
        Assert.True(TheTailBehindYou.TheRangesHeTries[0] > TheTailBehindYou.TheRangesHeTries[^1]);

        // The sides he sounds are a LIST and not a roll — behind first, the captain's own front last.
        Assert.Equal(Math.PI, TheTailBehindYou.TheSidesHeSounds[0], 9);
        Assert.Equal(0.0, TheTailBehindYou.TheSidesHeSounds[^1], 9);
        Assert.Equal(TheTailBehindYou.TheSidesHeSounds.Count, TheTailBehindYou.TheSidesHeSounds.Distinct().Count());
    }

    // ── 3 · NOT ONE DIE ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1062 · <b>THE NOTICE LAW IS DETERMINISTIC, AND THE SOURCE IS PART OF THE CLAIM.</b>
    ///
    /// <para>#1062's law for this half is <i>"deterministic notice (a pure function of distance/exposure
    /// ticks, no dice)"</i>, and that cannot be tested by value alone: a seeded roll is deterministic too,
    /// and would pass every repetition check ever written while quietly making gumshoe craft a lottery. So
    /// the guard reads BOTH of the lane's own files and fails if either ever names a die, and then proves the
    /// arithmetic is a plain threshold on the way through.</para>
    ///
    /// <para>This is also the one assertion that tells slice 1 and slice 2 apart, and it is deliberate: the
    /// captain tailing somebody asks #436's eye, which rolls, because the question there is whether a man
    /// happened to glance over his shoulder. Nobody is glancing here.</para>
    ///
    /// <para><b>Revert that reddened it:</b> <c>NoticedFromTheChair</c> written as
    /// <c>DiceRule.Roll(DiceRule.Seed($"coat:{exposureSeconds}"), DiceRule.D20).Face &gt;= 18</c> — the
    /// source sweep went RED on <c>DiceRule</c> before the arithmetic half was even reached.</para>
    /// </summary>
    [Fact]
    public void TheNoticeLawIsDeterministicAndHasNoDiceInIt()
    {
        string[] dice = ["DiceRule", "Random", "NextDouble", ".Roll(", "Seed("];
        foreach (string file in LaneSource())
        {
            string code = CodeOnly(File.ReadAllText(file));
            foreach (string die in dice)
            {
                Assert.False(code.Contains(die, StringComparison.Ordinal),
                    $"{Path.GetFileName(file)} names `{die}` — #1062's law for this half is NO DICE.");
            }
        }

        // …and the arithmetic is a plain threshold, monotone, on the one cadence.
        Assert.False(TheTailBehindYou.NoticedFromTheChair(TheTailBehindYou.NoticeSeconds - 0.001));
        Assert.True(TheTailBehindYou.NoticedFromTheChair(TheTailBehindYou.NoticeSeconds));
        Assert.True(TheTailBehindYou.NoticedFromTheChair(TheTailBehindYou.NoticeSeconds * 10));
        Assert.False(TheTailBehindYou.NoticedFromTheChair(0));

        // The same inputs, a thousand times, and never a different answer.
        for (int i = 0; i < 1000; i++)
        {
            Assert.True(TheTailBehindYou.NoticedFromTheChair(TheTailBehindYou.NoticeSeconds));
            Assert.False(TheTailBehindYou.HeIsLost(TheTailBehindYou.LostSeconds - 0.001));
        }
    }

    // ── 4 · THE FIRE CODE BECOMES GAMEPLAY ──────────────────────────────────────────────────────────────

    /// <summary>
    /// #1062 · <b>THE TELL IS THE FIRE CODE'S OWN NUMBER</b> — <i>"double back through a two-door room (the
    /// FIRE CODE's ≥2 doors becomes gameplay)"</i>, quoted from <see cref="UndergroundComplex.FireCodeMinExits"/>
    /// rather than typed here.
    ///
    /// <para>Anti-vacuity is the ZERO and ONE cases: a count that made a tell out of one doorway would fire on
    /// every man who ever stood in one, which is a reading that selects everybody and therefore nobody.</para>
    ///
    /// <para><b>Revert that reddened it:</b> <c>DoorsThatMakeTheTell</c> written as <c>1</c> — <i>a man
    /// leaning on the door he came in by is now a tell, every evening, in every bar in the game</i>.</para>
    /// </summary>
    [Fact]
    public void TheTellIsTheFireCodesOwnDoorCount()
    {
        Assert.Equal(UndergroundComplex.FireCodeMinExits, TheTailBehindYou.DoorsThatMakeTheTell);
        Assert.Equal(2, TheTailBehindYou.DoorsThatMakeTheTell);

        Assert.False(TheTailBehindYou.TwoDoorsRunning(0));
        Assert.False(TheTailBehindYou.TwoDoorsRunning(1));
        Assert.True(TheTailBehindYou.TwoDoorsRunning(2));
        Assert.True(TheTailBehindYou.TwoDoorsRunning(7));
    }

    // ── 4b · #1229 · THE OTHER BEHAVIOUR: THE POST ──────────────────────────────────────────────────────

    /// <summary>
    /// #1229 · <b>A WALL OFFERS PLACES TO STAND, AND THEY ARE ARITHMETIC.</b>
    /// <see cref="TheTailBehindYou.PostsAlong"/> cuts one wall into body-wide slices and offers the middle of
    /// each, one body clear of the stone, ON THE SIDE THE CAPTAIN IS ON. Everything about it is pinned here
    /// because a client file that sounded its own wall geometry would be this repository's oldest and most
    /// reliably wrong bug class with a man leaning on it.
    ///
    /// <para><b>Reverts that reddened it:</b> the normal left un-flipped (<i>every post is on the far side of
    /// the wall, in the next room</i>); the offset dropped to zero (<i>he stands INSIDE the stone</i>); the
    /// slice count floored at zero rather than one (<i>a wall shorter than a body offers nowhere, so the
    /// corners of a room silently stop existing</i>).</para>
    /// </summary>
    [Fact]
    public void AWallOffersBodyWidePlacesOnTheSideTheCaptainIsOn()
    {
        const double r = 0.7;
        double off = TheTailBehindYou.StandsOffTheWallBy(r);
        Assert.Equal(2 * r, off);

        // A wall along the x axis, ten long, with the captain to the NORTH of it.
        IReadOnlyList<(double X, double Y)> north = TheTailBehindYou.PostsAlong(0, 0, 10, 0, r, 5, 5);
        Assert.NotEmpty(north);
        foreach ((double x, double y) in north)
        {
            Assert.Equal(off, y, 9);                 // one body clear of the stone, on the captain's side
            Assert.InRange(x, 0, 10);                // …and on the wall, not off the end of it
        }

        // …and the same wall with the captain to the SOUTH offers the mirror of it and never the far side.
        foreach ((double _, double y) in TheTailBehindYou.PostsAlong(0, 0, 10, 0, r, 5, -5))
        {
            Assert.Equal(-off, y, 9);
        }

        // The slicing is a body's width, so a longer wall offers more places and they do not bunch up.
        Assert.True(
            TheTailBehindYou.PostsAlong(0, 0, 20, 0, r, 5, 5).Count
            > TheTailBehindYou.PostsAlong(0, 0, 10, 0, r, 5, 5).Count,
            "twice the wall offers no more places to stand — the slicing is not a body's width.");

        // A wall shorter than a body still offers its own middle: a piece of a room that offered nowhere
        // would be a corner no man could ever stand in, which is where this whole beat happens.
        Assert.Single(TheTailBehindYou.PostsAlong(0, 0, 0.5, 0, r, 5, 5));

        // …and a wall with no length is not a wall.
        Assert.Empty(TheTailBehindYou.PostsAlong(3, 3, 3, 3, r, 5, 5));

        // Deterministic: the same wall and the same captain give the same list, for ever.
        Assert.Equal(
            TheTailBehindYou.PostsAlong(0, 0, 10, 0, r, 5, 5),
            TheTailBehindYou.PostsAlong(0, 0, 10, 0, r, 5, 5));
    }

    /// <summary>
    /// #1229 · <b>HE GIVES IT ONE LOOK BEFORE HE COMES AFTER YOU</b>, and the look is the cadence this game
    /// already measures looking in — never a number of its own. That beat is what makes the two-door tell a
    /// SEQUENCE rather than an accident of where he was standing beforehand.
    ///
    /// <para><b>Revert that reddened it:</b> <c>SecondsBeforeHeFollowsYouOut =&gt; 0</c> — <i>a shadow welded
    /// to the captain's heels, through the doorway on the same frame, which is not a man</i>.</para>
    /// </summary>
    [Fact]
    public void HeWaitsOneLookBeforeHeFollowsYouOutAndItIsTheGamesOwnCadence()
    {
        Assert.Equal(TheTailBehindYou.TickSeconds, TheTailBehindYou.SecondsBeforeHeFollowsYouOut);
        Assert.Equal(ReeverObservation.LookIntervalSeconds, TheTailBehindYou.SecondsBeforeHeFollowsYouOut);

        // …and it is ONE look and not the twelve the notice costs: a man who waited out the whole notice
        // clock before leaving a room would never be in the second doorway at all.
        Assert.True(
            TheTailBehindYou.SecondsBeforeHeFollowsYouOut < TheTailBehindYou.NoticeSeconds,
            "he waits longer to follow you out than it takes to notice him — the tell is unreachable.");
        Assert.True(TheTailBehindYou.SecondsBeforeHeFollowsYouOut > 0,
            "he follows on the same frame, which is not a man giving a room a beat.");
    }

    /// <summary>#1229 · The reach a band is actually kept at is the first of the two Core publishes, named so
    /// that the question <i>can this room hold his band?</i> and the sounding that places him are asking about
    /// one number rather than two.</summary>
    [Fact]
    public void TheReachABandIsKeptAtIsTheFirstOfTheTwoHeTries()
    {
        Assert.Equal(TheTailBehindYou.TheRangesHeTries[0], TheTailBehindYou.TheReachHeKeeps);
        Assert.True(TheTailBehindYou.HoldsHisBand(TheTailBehindYou.TheReachHeKeeps),
            "the reach a band is kept at is outside the band — the two have come apart.");
    }

    // ── 5 · WHAT IT COSTS TO BE RID OF HIM ──────────────────────────────────────────────────────────────

    /// <summary>
    /// #1062 · <b>LOSING HIM COSTS WHAT FINDING HIM COST</b>, both off
    /// <see cref="ReeverObservation.LookIntervalSeconds"/> — one instrument, read from both ends.
    ///
    /// <para><b>And the guard asserts the DERIVATION and not only the value</b>, which is #1199's most
    /// expensive lesson repeated here: a wait written as a literal is the same number as a derived one on the
    /// day it is written, so a value-only assertion stays GREEN on exactly the revert it exists to catch.
    /// The source is read for a literal seconds count beside either constant.</para>
    ///
    /// <para><b>Revert that reddened it:</b> <c>NoticeSeconds =&gt; 9.0</c> — the value assertions all stayed
    /// green and the source clause went RED.</para>
    /// </summary>
    [Fact]
    public void LosingHimCostsWhatFindingHimCostAndNeitherIsALiteral()
    {
        Assert.Equal(ReeverObservation.LookIntervalSeconds, TheTailBehindYou.TickSeconds, 9);
        Assert.Equal(TheTailBehindYou.LooksToNotice, TheTailBehindYou.LooksToLoseHim);
        Assert.Equal(TheTailBehindYou.NoticeSeconds, TheTailBehindYou.LostSeconds, 9);
        Assert.Equal(TheTailBehindYou.LooksToNotice * ReeverObservation.LookIntervalSeconds,
            TheTailBehindYou.NoticeSeconds, 9);
        Assert.True(TheTailBehindYou.LooksToNotice > 1, "one look is not an exposure, it is a glance.");

        string code = CodeOnly(File.ReadAllText(CoreFile));
        Assert.Contains("LooksToNotice * TickSeconds", code, StringComparison.Ordinal);
        Assert.Contains("ReeverObservation.LookIntervalSeconds", code, StringComparison.Ordinal);
        Assert.DoesNotContain("NoticeSeconds => 9", code, StringComparison.Ordinal);

        Assert.False(TheTailBehindYou.HeIsLost(0));
        Assert.False(TheTailBehindYou.HeIsLost(TheTailBehindYou.LostSeconds - 0.001));
        Assert.True(TheTailBehindYou.HeIsLost(TheTailBehindYou.LostSeconds));
    }

    // ── 6 · ONE SIGHTLINE ORACLE ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1062 · <b>THE CHAIR ASKS THE ONE LOOK, AND THE WHOLE GAME HAS EXACTLY ONE.</b>
    ///
    /// <para>Three claims. First, the sweep: <see cref="TheTailBehindYou.ThisChairSeesTheDoor"/> is compared
    /// against <see cref="PatrolBeat.EyesOn"/> over a grid with a wall across it, and must agree everywhere —
    /// the same way slice 1 proves its notice question IS #436's eye, by calling both rather than by reading
    /// the source and believing it.</para>
    ///
    /// <para>Second, the ANTI-VACUITY clause, and it is the one that matters: the sweep must contain
    /// positions where the wall CHANGES the answer. A grid with nothing in the way would let a chair that
    /// measured a plain distance pass this test with the oracle uncalled.</para>
    ///
    /// <para>Third, the source sweep the one-oracle law actually asks for: there is exactly ONE
    /// <c>HasLineOfSight</c> declared in the whole of <c>src/</c>, and the lane's two files reach it only
    /// through names that bottom out in it.</para>
    ///
    /// <para><b>Revert that reddened it:</b> <c>ThisChairSeesTheDoor</c> written as a plain range test
    /// (<c>dx*dx + dy*dy &lt;= LosesYouBeyondDu²</c>) — <i>a chair that can see through a wall</i>.</para>
    /// </summary>
    [Fact]
    public void TheChairAsksTheOneLookAndTheGameHasOnlyOne()
    {
        // A wall down the middle, so half the grid is asking through stone.
        IReadOnlyList<SurfaceCollision.Segment> walls = [new SurfaceCollision.Segment(0, -40, 0, 40)];
        int agreed = 0, wallMattered = 0, sawSomething = 0;

        for (double cx = -20; cx <= 20; cx += 2.5)
        {
            for (double cy = -20; cy <= 20; cy += 2.5)
            {
                for (double dx = -20; dx <= 20; dx += 5)
                {
                    bool mine = TheTailBehindYou.ThisChairSeesTheDoor(cx, cy, dx, 0, walls);
                    bool theirs = PatrolBeat.EyesOn(cx, cy, dx, 0, TheTailBehindYou.LosesYouBeyondDu, walls);
                    Assert.Equal(theirs, mine);
                    agreed++;
                    sawSomething += mine ? 1 : 0;

                    bool openAir = PatrolBeat.EyesOn(cx, cy, dx, 0, TheTailBehindYou.LosesYouBeyondDu, null);
                    wallMattered += openAir != mine ? 1 : 0;
                }
            }
        }

        Assert.True(agreed > 1000, "the sweep did not sweep.");
        Assert.True(sawSomething > 0, "nothing in the sweep could see anything — the guard proves nothing.");
        Assert.True(wallMattered > 0,
            "no position in the sweep was changed by the wall, so a chair with no oracle in it would pass.");

        // …and there is exactly ONE of these in the whole game.
        string[] declared = Directory
            .EnumerateFiles(Path.Combine(TestTree.RepoRoot(), "src"), "*.cs", SearchOption.AllDirectories)
            .Where(f => CodeOnly(File.ReadAllText(f)).Contains("bool HasLineOfSight(", StringComparison.Ordinal))
            .ToArray();
        Assert.Single(declared);
        Assert.EndsWith("SurfaceCollision.cs", declared[0], StringComparison.Ordinal);

        // …and the lane names no sight arithmetic of its own: every question it asks is one of these three.
        string[] allowed = ["HasLineOfSight", "EyesOn", "InPlainSight"];
        foreach (string file in LaneSource())
        {
            string code = CodeOnly(File.ReadAllText(file));
            Assert.False(code.Contains("Segment(", StringComparison.Ordinal),
                $"{Path.GetFileName(file)} builds its own walls — the oracle takes the room's.");
            Assert.False(code.Contains("CastRay", StringComparison.Ordinal),
                $"{Path.GetFileName(file)} casts its own ray.");
        }

        Assert.True(
            LaneSource().Any(f => allowed.Any(a => CodeOnly(File.ReadAllText(f)).Contains(a, StringComparison.Ordinal))),
            "the lane asks no sightline question at all — this guard cannot fail.");
    }

    // ── 7 · HE DECLARES HIMSELF ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1062 · <b>#793's SEAM IS FINALLY FILLED, AND BY SOMETHING THAT DECLARES ITSELF.</b>
    ///
    /// <para><see cref="FootTail"/> shipped as a seam and said so: <i>"Nothing in the game tails the captain
    /// yet … the day a watcher is built it has one place to declare itself rather than a bench growing a
    /// second opinion about who is behind you."</i> This asserts that this lane used that place and not a
    /// second one — the mover is minted with the clause set, never inferred from two positions.</para>
    ///
    /// <para>Anti-vacuity: the bench's DISQUALIFYING clause must still disqualify. A published round is not a
    /// tail however close it walks, and if this figure ever became one, every guard in the Hive would be one
    /// too.</para>
    ///
    /// <para><b>Revert that reddened it:</b> <c>AsAMover</c> written with
    /// <c>OnAPublishedRound: true</c> — <i>the man keeping station on the captain stops being a tail, the
    /// bench stops seeing him and the held bar is never drawn</i>.</para>
    /// </summary>
    [Fact]
    public void HeIsTheFirstThingInTheGameThatDeclaresItselfATail()
    {
        FootTail.Mover him = TheTailBehindYou.AsAMover(6, 0);
        Assert.True(FootTail.IsTailing(in him));
        Assert.False(him.OnAPublishedRound);
        Assert.Equal(TheTailBehindYou.Plate, him.Name);

        IReadOnlyList<SurfaceCollision.Segment> open = [];
        Assert.True(FootTail.InPlainSight(0, 0, in him, open));
        Assert.True(FootTail.MustHold(true, 0, 0, in him, open));
        Assert.True(FootTail.AnythingTailing(0, 0, [him], open));

        // On their feet, the sit buys nothing — the exchange the bench is named after.
        Assert.False(FootTail.MustHold(false, 0, 0, in him, open));

        // A wall, and there is nothing to be revealed.
        IReadOnlyList<SurfaceCollision.Segment> wall = [new SurfaceCollision.Segment(3, -40, 3, 40)];
        Assert.False(FootTail.MustHold(true, 0, 0, in him, wall));

        // …and a published round is still not a tail, which is what stops every guard in the game being one.
        FootTail.Mover round = FootTail.OnARound(TheTailBehindYou.Plate, 6, 0);
        Assert.False(FootTail.IsTailing(in round));
        Assert.False(FootTail.MustHold(true, 0, 0, in round, open));
    }

    // ── 8 · THE PROSE ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1062 · <b>FOUR SENTENCES, AUTHORED, AND THE NOTE IS FILED UNDER A PLACE.</b>
    ///
    /// <para>The subject clause is the load-bearing one. The canon this half was given ends <i>never a
    /// face</i> — so a thread heading with a NAME on it would be the book claiming an identification the
    /// captain never made, which is the single thing #741 exists to refuse. The subject is minted in Core
    /// beside the sentence that prints the place, per #741's author law, and it is a
    /// <see cref="CaseSubjects.Kind.Place"/>.</para>
    ///
    /// <para><b>Revert that reddened it:</b> <c>Subjects</c> written as
    /// <c>CaseSubjects.Line(CaseSubjects.Person(place))</c> — <i>the book grew a thread headed with a face
    /// nobody ever saw</i>.</para>
    /// </summary>
    [Fact]
    public void ThePoseIsAuthoredAndTheNoteIsFiledUnderAPlaceAndNeverAFace()
    {
        const string place = "SELENE GATE";
        string[] said = [.. TheTailBehindYou.AllProse(place)];
        Assert.Equal(6, said.Length);

        Assert.Equal(
            "From this chair you can see the door. So can the man who came in after you, and he has not ordered.",
            TheTailBehindYou.FromThisChairLine);
        Assert.Equal(
            "The same grey coat, two doors running. Nobody's errand takes them through both.",
            TheTailBehindYou.TwoDoorsLine);
        Assert.Equal(
            "The corridor behind you is only a corridor. Whoever it was is asking the wrong floor about you.",
            TheTailBehindYou.LostLine);
        Assert.Equal("a tail, lost at SELENE GATE — a grey coat, never a face",
            TheTailBehindYou.NoteLine(place));
        Assert.Equal("Tidy, in the way a place is after somebody has been through it first.",
            TheTailBehindYou.TheBurnLine);
        Assert.Equal("SELENE GATE — walked before you got there, by somebody who knew where to walk",
            TheTailBehindYou.BurnNote(place));

        // #741 · the subject is the author's, and it is a PLACE.
        var note = new FieldNote(
            TheTailBehindYou.NoteLine(place), 0, place, TheTailBehindYou.Glyph,
            TheTailBehindYou.Subjects(place));
        CaseSubjects.Subject filed = Assert.Single(CaseSubjects.On(in note));
        Assert.Equal(CaseSubjects.Kind.Place, filed.Of);
        Assert.Equal(place, filed.Name);

        // …and the burn is filed under the SAME place, so THREADS stacks the evening in the order it
        // happened: he was behind me, and then this place had been gone through.
        var burn = new FieldNote(
            TheTailBehindYou.BurnNote(place), 0, place, TheTailBehindYou.Glyph,
            TheTailBehindYou.BurnSubjects(place));
        CaseSubjects.Subject onTheBurn = Assert.Single(CaseSubjects.On(in burn));
        Assert.Equal(CaseSubjects.Kind.Place, onTheBurn.Of);
        Assert.Equal(filed, onTheBurn);

        // The burn's tag is a durable key in the register the game already keeps, and it is per PORT — one
        // place burned, not a captain marked.
        Assert.NotEqual(TheTailBehindYou.BurnTag("selene-gate"), TheTailBehindYou.BurnTag("the-space-bar"));
        Assert.Contains("selene-gate", TheTailBehindYou.BurnTag("selene-gate"), StringComparison.Ordinal);
        Assert.NotEqual(
            SpaceSails.Core.BlackOpsKey.ThePortHasDealtOne("selene-gate", 0),
            TheTailBehindYou.BurnTag("selene-gate"));

        // …and the glyph is the game's own watched-from-somewhere mark, borrowed and never re-typed.
        Assert.Equal(ReeverObservation.FixedOnYouGlyph, TheTailBehindYou.Glyph);

        string[] forbidden =
        [
            // §8's reserved word and the fifteen beside it.
            "monolith", "ancient", "alien", "reever", "old one", "pre-human", "not human", "artefact",
            "artifact", "civilisation", "civilization", "millennia", "aeon", "eon",
            // …and the things this half must never name: whose man he is, and what he is called.
            "outfit", "company", "agent", "spy", "watcher", "they know", "suspicion",
        ];

        var named = new List<string>();
        foreach (string text in said.Append(TheTailBehindYou.Plate))
        {
            named.AddRange(
                forbidden.Where(w => text.Contains(w, StringComparison.OrdinalIgnoreCase))
                         .Select(w => $"\"{w}\" in \"{text}\""));
        }

        Assert.True(named.Count == 0,
            "a #1062 string settles what it must leave open:\n  " + string.Join("\n  ", named));
    }

    // ── PLUMBING ────────────────────────────────────────────────────────────────────────────────────────

    private static string CoreFile =>
        Path.Combine(TestTree.RepoRoot(), "src", "SpaceSails.Core", "TheTailBehindYou.cs");

    /// <summary>Both of the lane's own files — the law and the frame. Named by path so a guard that reads the
    /// source fails loudly if either is ever renamed out from under it.</summary>
    private static IReadOnlyList<string> LaneSource()
    {
        string[] files =
        [
            CoreFile,
            Path.Combine(TestTree.RepoRoot(), "src", "SpaceSails.Client", "Pages", "Map.TailBehindYou.cs"),
        ];

        foreach (string f in files)
        {
            Assert.True(File.Exists(f), $"this guard reads `{f}` and it is not there.");
        }

        return files;
    }

    /// <summary>The source with its DOC AND LINE COMMENTS TAKEN OUT. Every claim in this file is about what
    /// the code does, and these files argue with themselves at length in prose that names the very things the
    /// sweeps forbid — a sweep over raw text would be a sweep over an essay.</summary>
    private static string CodeOnly(string source)
    {
        var kept = new List<string>();
        foreach (string line in source.Split('\n'))
        {
            string t = line.TrimStart();
            if (t.StartsWith("//", StringComparison.Ordinal))
            {
                continue;
            }

            int slashes = line.IndexOf("//", StringComparison.Ordinal);
            kept.Add(slashes >= 0 ? line[..slashes] : line);
        }

        return string.Join('\n', kept);
    }
}
