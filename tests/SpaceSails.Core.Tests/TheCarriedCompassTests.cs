namespace SpaceSails.Core.Tests;

/// <summary>
/// #727 · THE MISSION THAT LEAVES THE SHIP — the pure half.
///
/// <para>Owner, 2026-08-06: <i>"a mission that works outside the ship UI is something new … we should filter
/// out / minimize ship-specific stuff to appropriate ('cannot do in this UI level, but high level: go to Moon
/// X') type info in the carried mission UI."</i></para>
///
/// <para>These pin the FOLD itself: what a foot-level step becomes (nothing — it is passed through), what
/// everything else becomes (one collapsed sentence), and the two ways the fold could quietly lie — by
/// dropping a mission, or by letting an affordance ride out of the ship on a step the pane cannot honour.
/// The client half (the pane's markup, the desk's own words, the on-foot completion) is
/// <c>TheCarriedMissionsPaneTests</c> next door.</para>
/// </summary>
public class TheCarriedCompassTests
{
    private static MissionStep Foot(string text, string? place = null, string? action = null) =>
        new(text, MissionUiLevel.Foot, "Selene Gate", place, action);

    private static MissionStep Ship(string text, string destination = "Selene Gate") =>
        new(text, MissionUiLevel.Ship, destination);

    // ── The collapse ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The owner's own example, to the letter. RED by leaking the burn text through instead.</summary>
    [Fact]
    public void AShipLevelStepCollapsesToItsDestinationAndSaysNothingAboutBurns()
    {
        IReadOnlyList<CompassLine> compass = MissionProjection.OnFoot(
            [new CarriedMission("The Selene run", "A parcel for Madam Coil.",
                Ship("prograde 12 m/s, coast 40 h, clamp at Selene Gate"))],
            standingOn: "luna");

        CompassLine only = Assert.Single(compass);
        Assert.Equal("⛵ return to the ship — next: Selene Gate", only.Step);
        Assert.False(only.Actionable);
        Assert.DoesNotContain("prograde", only.Step, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("clamp", only.Step, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>A berth step is not a chair step, and it is not a boots step either — you still have to sail
    /// to the counter. It folds the same way, because from a basement the difference does not exist.</summary>
    [Fact]
    public void ABerthStepCollapsesTheSameWay()
    {
        IReadOnlyList<CompassLine> compass = MissionProjection.OnFoot(
            [new CarriedMission("The tip", "Bought over a drink.",
                new MissionStep("🕸 Tip taken", MissionUiLevel.Berth, "The Rusty Roadstead"))],
            standingOn: "luna");

        Assert.Equal("⛵ return to the ship — next: The Rusty Roadstead", Assert.Single(compass).Step);
    }

    /// <summary>A step the chair named no destination for still says the collapsed sentence — a blank after
    /// "next:" would be the pane lying by omission.</summary>
    [Fact]
    public void ACollapseWithNoDestinationStillNamesSomething()
    {
        IReadOnlyList<CompassLine> compass = MissionProjection.OnFoot(
            [new CarriedMission("A job", "", new MissionStep("▶ On the hook", MissionUiLevel.Ship, ""))],
            standingOn: null);

        Assert.Equal($"{MissionProjection.ReturnToShip}{MissionProjection.NoDestinationNamed}",
            Assert.Single(compass).Step);
    }

    // ── The pass-through ────────────────────────────────────────────────────────────────────────────────

    /// <summary>A foot step is passed through BYTE FOR BYTE. The carried view is a projection, not a rewrite:
    /// a second wording of the same step is a second mission list wearing a disguise. RED by any
    /// prettifying — a trimmed glyph, a capitalised first letter, an appended full stop.</summary>
    [Fact]
    public void AFootStepIsTheChairsOwnWordsUntouched()
    {
        const string chairSaid = "▶ Crack hatch V-06 — code 4417";
        IReadOnlyList<CompassLine> compass = MissionProjection.OnFoot(
            [new CarriedMission("The break-in", "The Fixer wants what is behind V-06.",
                Foot(chairSaid, place: "cinder-roost"))],
            standingOn: "cinder-roost");

        CompassLine only = Assert.Single(compass);
        Assert.Equal(chairSaid, only.Step);
        Assert.True(only.Actionable);
    }

    /// <summary>…and a foot step on ground you are NOT standing on is a voyage like any other. The dig on
    /// Phobos is a shovel verb; from a Luna basement it is "get back to the ship".</summary>
    [Fact]
    public void AFootStepOnSomebodyElsesGroundCollapses()
    {
        IReadOnlyList<CompassLine> compass = MissionProjection.OnFoot(
            [new CarriedMission("The cache run", "Dig up what Coil buried.",
                new MissionStep("🗺 Dig at the X on Phobos", MissionUiLevel.Foot, "Phobos", "phobos"))],
            standingOn: "luna");

        CompassLine only = Assert.Single(compass);
        Assert.Equal("⛵ return to the ship — next: Phobos", only.Step);
        Assert.False(only.Actionable);
    }

    /// <summary>A foot step that names no ground is actionable wherever you are — "hand it over when you see
    /// him" is not a place.</summary>
    [Fact]
    public void AFootStepWithNoGroundOfItsOwnIsActionableAnywhere()
    {
        Assert.True(MissionProjection.ActionableWhereYouStand(Foot("▶ do the thing"), standingOn: "luna"));
        Assert.True(MissionProjection.ActionableWhereYouStand(Foot("▶ do the thing"), standingOn: null));
    }

    /// <summary>In the chair — no ground underfoot at all — nothing is a boots step. RED by treating a null
    /// ground as a wildcard, which would spell out a hatch code to a captain in flight.</summary>
    [Fact]
    public void InTheChairAGroundedFootStepIsNotActionable()
    {
        Assert.False(MissionProjection.ActionableWhereYouStand(
            Foot("▶ Crack hatch V-06 — code 4417", place: "cinder-roost"), standingOn: null));
    }

    // ── The two ways it could lie ───────────────────────────────────────────────────────────────────────

    /// <summary>NO BUTTON IT CANNOT HONOUR. An action id may only ride a step the captain can act on where
    /// they stand; a collapsed line carries none, whatever the caller attached. RED by passing
    /// <c>m.Step.Action</c> straight through — which is exactly how a dead control gets into a satchel.</summary>
    [Fact]
    public void ACollapsedLineNeverCarriesAnAffordance()
    {
        IReadOnlyList<CompassLine> compass = MissionProjection.OnFoot(
        [
            new CarriedMission("far", "", new MissionStep(
                "🗺 Dig at the X on Phobos", MissionUiLevel.Foot, "Phobos", "phobos", "dig-here")),
            new CarriedMission("sailing", "", new MissionStep(
                "▶ On the hook", MissionUiLevel.Ship, "Selene Gate", null, "burn-prograde")),
            new CarriedMission("here", "", Foot("▶ press E", place: "luna", action: "press-e")),
        ], standingOn: "luna");

        Assert.Null(compass[0].Action);
        Assert.Null(compass[1].Action);
        Assert.Equal("press-e", compass[2].Action);
    }

    /// <summary>ONE MODEL. Every mission handed in comes out — collapsed, maybe, but PRESENT and in order. A
    /// pane that quietly dropped the ones it could not act on would be the two-lists bug with better manners.
    /// RED by filtering on <c>Actionable</c> anywhere inside the projection.</summary>
    [Fact]
    public void NothingIsEverDroppedAndTheOrderIsKept()
    {
        CarriedMission[] held =
        [
            new("one", "", Ship("a")),
            new("two", "", Foot("b", place: "luna")),
            new("three", "", new MissionStep("c", MissionUiLevel.Berth, "somewhere")),
            new("four", "", Foot("d", place: "phobos")),
        ];

        IReadOnlyList<CompassLine> compass = MissionProjection.OnFoot(held, standingOn: "luna");

        Assert.Equal(held.Length, compass.Count);
        Assert.Equal(["one", "two", "three", "four"], compass.Select(l => l.Title));
        Assert.Equal(1, compass.Count(l => l.Actionable));   // and only the one you are standing on
    }

    /// <summary>Nothing owed is an ANSWER, not an empty list — the pane says it in the house voice rather
    /// than showing a blank page to a captain who came to it asking why they are down here.</summary>
    [Fact]
    public void AnEmptyCompassIsEmptyAndTheWordsForItExist()
    {
        Assert.Empty(MissionProjection.OnFoot([], standingOn: "luna"));
        Assert.False(string.IsNullOrWhiteSpace(MissionProjection.NothingOwedOnFoot));
        Assert.False(string.IsNullOrWhiteSpace(MissionProjection.CompassBlurb));
    }
}
