using System.Text.RegularExpressions;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #698 · WHAT YOU LEFT IS DRAWN ON THE DECK, AND THE KEYBAR SAYS SO. Owner, on B12 of the clinic, within
/// the hour of #691 shipping: <i>"I dropped 3 files on somebody here but there was nothing marked onto the
/// map?"</i>
///
/// <para>That is #691's own flagged call — <i>"a left thing is not drawn on the deck. The line says where;
/// no marker. Cheap to add later"</i> — collected the same afternoon. The recovery worked; the way back ran
/// on the memory of a pulsed sentence, which #615 and #600 between them say is not a way back at all.</para>
///
/// <para><b>Why these run on the real decks.</b> The store's own laws are pinned in Core
/// (<c>TheGroundKnowsWhereYouLeftItTests</c>). What is left to get wrong here is the WIRING — three deck
/// generators, one of which forgets; a mark that draws once and never clears; a prompt measured off its own
/// copy of the ring. So these build the deck a captain's boots actually collide with, count the marks on it,
/// and then read the shipped source for the four call sites that make it happen at all.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheDeckMarksWhatYouLeftTests
{
    private const int ClinicFloor = -12;

    private static Satchel.Item Paper(int i) => new(Satchel.Kind.Paper, $"hive:luna:-12:{i}");

    /// <summary>A real Hive floor, built exactly the way the excursion builds it.</summary>
    private static DeckPlan HiveFloor() =>
        HiveInterior.FloorDeck("luna", ClinicFloor, MoonSurface.ExpeditionField(), 0, (_, _) => { }, []);

    /// <summary>A real patch of regolith, built exactly the way the excursion builds it.</summary>
    private static DeckPlan Regolith()
    {
        LandingSite site = LandingSites.At("luna", 0);
        return MoonSurface.SurfaceDeck(
            "luna", "Luna", [], droidCount: 0, fillDroids: static (_, _) => { },
            siteSalt: site.LayoutSalt, siteName: site.Name);
    }

    private static DeckPlan.ConsoleSpot[] MarksOn(DeckPlan deck) =>
        [.. deck.Consoles.Where(c => c.Kind == DeckPlan.ConsoleKind.LeftBehind)];

    // ── The decks ─────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AHiveFloorWithNothingOnItCarriesNoMarks()
    {
        DeckPlan deck = HiveFloor();
        deck.AppendRegion(LeftMarks.Region(new LeftBehind(), ClinicFloor));

        Assert.Empty(MarksOn(deck));
    }

    [Fact]
    public void ThreeFilesOnOneSquareOfAHiveFloorDrawONEMarkWhereTheyLie()
    {
        // The owner's own sentence, on the owner's own floor. Three files, one square, one mark — and the
        // plate says WHAT YOU LEFT rather than how much of it there is.
        var ground = new LeftBehind();
        string spot = LeftBehind.SpotKey(ClinicFloor, 4, 7);
        ground.Leave(spot, Paper(1));
        ground.Leave(spot, Paper(2));
        ground.Leave(spot, Paper(3));

        DeckPlan deck = HiveFloor();
        deck.AppendRegion(LeftMarks.Region(ground, ClinicFloor));

        DeckPlan.ConsoleSpot mark = Assert.Single(MarksOn(deck));
        (double x, double y) = BeachComber.SquareCenter(4, 7);
        Assert.Equal(x, mark.X, 3);
        Assert.Equal(y, mark.Y, 3);
        Assert.Equal(LeftBehind.MarkerLabel, mark.Label);
    }

    [Fact]
    public void TwoSquaresDrawTwoMarks()
    {
        var ground = new LeftBehind();
        ground.Leave(LeftBehind.SpotKey(ClinicFloor, 4, 7), Paper(1));
        ground.Leave(LeftBehind.SpotKey(ClinicFloor, -3, 2), Paper(2));

        DeckPlan deck = HiveFloor();
        deck.AppendRegion(LeftMarks.Region(ground, ClinicFloor));

        Assert.Equal(2, MarksOn(deck).Length);
    }

    [Fact]
    public void TheRegolithDrawsItToo()
    {
        // Left things exist on grounds as well as floors, and the ground is where a captain sheds weight
        // before a walk out. A marker that only worked underground would be half a fix.
        var ground = new LeftBehind();
        ground.Leave(LeftBehind.SpotKey(0, 2, -5), Paper(1));

        DeckPlan deck = Regolith();
        deck.AppendRegion(LeftMarks.Region(ground, 0));

        DeckPlan.ConsoleSpot mark = Assert.Single(MarksOn(deck));
        (double x, double y) = BeachComber.SquareCenter(2, -5);
        Assert.Equal(x, mark.X, 3);
        Assert.Equal(y, mark.Y, 3);
    }

    [Fact]
    public void AFloorNeverDrawsAnotherFloorsMarks()
    {
        var ground = new LeftBehind();
        ground.Leave(LeftBehind.SpotKey(ClinicFloor, 4, 7), Paper(1));
        ground.Leave(LeftBehind.SpotKey(ClinicFloor + 1, 4, 7), Paper(2));

        DeckPlan deck = HiveFloor();
        deck.AppendRegion(LeftMarks.Region(ground, ClinicFloor));

        Assert.Single(MarksOn(deck));
    }

    [Fact]
    public void TheMarkIsGoneOnTheRebuildAfterTheSpotEmpties()
    {
        var ground = new LeftBehind();
        string spot = LeftBehind.SpotKey(ClinicFloor, 4, 7);
        ground.Leave(spot, Paper(1));

        DeckPlan before = HiveFloor();
        before.AppendRegion(LeftMarks.Region(ground, ClinicFloor));
        Assert.Single(MarksOn(before));

        ground.PickUp(spot, []);

        DeckPlan after = HiveFloor();
        after.AppendRegion(LeftMarks.Region(ground, ClinicFloor));
        Assert.Empty(MarksOn(after));
    }

    [Fact]
    public void TheMarkIsSceneryAndBlocksNobody()
    {
        // The owner asked for scenery. A mark carries no wall, so it adds no collision segment — a captain
        // pinned against their own paperwork would be a worse bug than the missing mark.
        var ground = new LeftBehind();
        ground.Leave(LeftBehind.SpotKey(ClinicFloor, 4, 7), Paper(1));

        DeckPlan deck = HiveFloor();
        int wallsBefore = deck.Walls.Length;
        deck.AppendRegion(LeftMarks.Region(ground, ClinicFloor));

        Assert.Equal(wallsBefore, deck.Walls.Length);
        (double x, double y) = BeachComber.SquareCenter(4, 7);
        Assert.Equal((x, y), deck.Move(x, y, 0, 0));
    }

    /// <summary>
    /// THE MARK CAN NEVER CLAIM A KEY THE PICKUP WILL NOT TAKE.
    ///
    /// <para><see cref="DeckView"/> draws [E] over the console <see cref="DeckPlan.NearestConsoleSpot"/>
    /// answers — while the recovery runs AHEAD of console dispatch (#691). So the two are consistent only if
    /// a mark that is inside the interact radius is always inside the recovery ring. It is, and by
    /// arithmetic rather than by luck: the nearest square OUTSIDE the ring has its centre further away than
    /// the radius reaches. Pinned here because both numbers are free to move.</para>
    /// </summary>
    [Fact]
    public void AMarkInsideTheInteractRadiusIsAlwaysInsideTheRecoveryRing()
    {
        // The worst case: the captain at the very corner of their own square, the mark at the centre of the
        // nearest square the ring does not cover.
        double gap = ((LeftBehind.ReachSquares + 1) + 0.5 - 1.0) * BeachComber.SquareSize;

        Assert.True(gap > DeckPlan.InteractRadius,
            $"a mark {gap:0.##} du away can be the answering console while the pickup ignores it — " +
            $"the interact radius is {DeckPlan.InteractRadius} du");
    }

    // ── The wiring ────────────────────────────────────────────────────────────────────────────────────

    private static string RepoRoot()
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null)
        {
            if (Directory.Exists(Path.Combine(at.FullName, "src", "SpaceSails.Client")))
            {
                return at.FullName;
            }
            at = at.Parent;
        }

        throw new DirectoryNotFoundException($"could not find the repo root above {AppContext.BaseDirectory}");
    }

    private static string SurfaceSource() =>
        File.ReadAllText(Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages", "Map.Surface.cs"));

    /// <summary>The body of one method in <c>Map.Surface.cs</c>, brace-matched from its signature.</summary>
    private static string BodyOf(string signature)
    {
        string text = SurfaceSource();
        int at = text.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(at >= 0, $"Map.Surface.cs no longer declares `{signature}` — this guard has gone blind");

        int open = text.IndexOf('{', at);
        Assert.True(open > 0, $"`{signature}` has no body");

        int depth = 0;
        for (int i = open; i < text.Length; i++)
        {
            if (text[i] == '{')
            {
                depth++;
            }
            else if (text[i] == '}' && --depth == 0)
            {
                return text[open..(i + 1)];
            }
        }

        throw new InvalidOperationException($"`{signature}` never closes");
    }

    /// <summary>
    /// EVERY BRANCH OF THE REBUILD COMPOSES THE MARKS.
    ///
    /// <para>The rebuild returns from three places — the Hive's floors, a derelict's steel and, at the end,
    /// the open regolith — and a captain can put something down on all three. This counts the returns and
    /// demands a composer call before each one, which is the shape that would have caught the ORIGINAL miss:
    /// the composer existed nowhere, so every branch was a branch that forgot.</para>
    /// </summary>
    [Fact]
    public void EveryBranchOfTheSurfaceRebuildComposesTheMarks()
    {
        string body = BodyOf("private void RebuildSurfaceDeck()");

        int returns = Regex.Matches(body, @"\breturn\s*;").Count;
        Assert.True(returns >= 3,
            $"RebuildSurfaceDeck has {returns} early return(s) — this guard expected the three deck families");

        // Every early return that is NOT the "no excursion" guard, plus the fall-through end of the method,
        // must have the composer between it and the deck it just built.
        string[] segments = Regex.Split(body, @"\breturn\s*;");
        var missing = new List<int>();
        for (int i = 0; i < segments.Length; i++)
        {
            // The first segment is the null-excursion guard: nothing has been built yet.
            if (i == 0 || !segments[i].Contains("_deckPlan =", StringComparison.Ordinal))
            {
                continue;
            }
            if (!segments[i].Contains("ComposeWhatYouLeft(", StringComparison.Ordinal))
            {
                missing.Add(i);
            }
        }

        Assert.True(missing.Count == 0,
            $"{missing.Count} branch(es) of RebuildSurfaceDeck build a deck and never compose #698's marks " +
            $"(segment index {string.Join(", ", missing)}) — a captain who leaves something on that deck " +
            "gets the owner's complaint back verbatim");
    }

    /// <summary>
    /// AND IT HAPPENS AT THE MOMENT, NOT AT THE NEXT REBUILD.
    ///
    /// <para>The marks are composed by the rebuild, so putting a thing down and picking it back up must each
    /// ask for one. Without this the mark appears whenever some unrelated event next rebuilds the deck —
    /// which is indistinguishable, at the moment the owner looks down, from no mark at all.</para>
    ///
    /// <para>#696 · The drop is <c>SetItDown</c> now — <c>LeaveItem</c> is the press, and for a document it
    /// starts a twenty-second hold before anything reaches the ground. The redraw belongs to the moment the
    /// thing actually LANDS, which is the far end; pinning it to the press would pin it to a moment when
    /// there is deliberately nothing on the floor to draw.</para>
    /// </summary>
    [Theory]
    [InlineData("private void SetItDown(SurfaceExcursion ex, Core.Satchel.Item item, string standing)")]
    [InlineData("private bool TryPickUpWhatYouLeft()")]
    public void PuttingAThingDownAndTakingItBackBothRedrawTheDeck(string signature)
    {
        Assert.Contains("RebuildSurfaceDeck()", BodyOf(signature), StringComparison.Ordinal);
    }

    /// <summary>
    /// THE KEYBAR OFFERS THE PICKUP, ON BOTH DECK FAMILIES, OFF THE RING THE KEY ITSELF OBEYS.
    ///
    /// <para>Two claims in one, and the second is the one worth having. The bar must carry
    /// <see cref="LeftBehind.ReachPrompt"/> — otherwise the recovery stays the secret it has been since
    /// #691 — and it must reach it through <c>StandingOnWhatYouLeft()</c>, which is
    /// <see cref="LeftBehind.AnythingInReach"/>. A bar that re-derived the ring here would be an offer that
    /// could disagree with the press, which is this house's fifth named bug class.</para>
    /// </summary>
    [Fact]
    public void TheKeybarOffersTheTakeBackWhereverTheCaptainCanLeaveThings()
    {
        string body = BodyOf("private string BuildSurfaceKeyHints(SurfaceExcursion ex)");

        int offers = Regex.Matches(body, @"LeftBehind\.ReachPrompt").Count;
        Assert.True(offers >= 2,
            $"the keybar offers the take-back on {offers} deck family/families — the derelict's steel and " +
            "the regolith/Hive bar are built separately and a captain can leave things on both");

        int asks = Regex.Matches(body, @"StandingOnWhatYouLeft\(\)").Count;
        Assert.Equal(offers, asks);

        // …and that helper is Core's ring and nothing else. Matched as an expression, not brace-scanned:
        // the one-liner it must stay is `=> _surface is { } ex && ex.Ground.AnythingInReach(…)`.
        Assert.Matches(
            new Regex(@"StandingOnWhatYouLeft\(\)\s*=>[^;]*\bAnythingInReach\(", RegexOptions.Singleline),
            SurfaceSource());
    }

    /// <summary>
    /// AND THE PICKUP ASKS THE SAME FUNCTION.
    ///
    /// <para>#691 wrote the ring out inline. Leaving that loop in place while the prompt asked Core would
    /// have been two rings with one name — so the verb was moved onto <see cref="LeftBehind.SpotInReach"/>
    /// and this pins that it stays there.</para>
    /// </summary>
    [Fact]
    public void ThePickUpMeasuresTheRingWithCoreAndNotWithALoopOfItsOwn()
    {
        string body = BodyOf("private bool TryPickUpWhatYouLeft()");

        Assert.Contains("SpotInReach(", body, StringComparison.Ordinal);
        Assert.DoesNotContain("SpotKey(", body, StringComparison.Ordinal);
    }
}
