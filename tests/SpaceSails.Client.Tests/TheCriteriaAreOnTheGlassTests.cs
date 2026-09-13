using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #243 / #242 · <b>THE TWO INSTRUMENTS, WIRED.</b>
///
/// <para>The laws live next door in Core (<c>TheGateIsAlwaysTheSameDuoTests</c>,
/// <c>TheFrameIsEatingTheCruiseTests</c>): what these gates hold is the WIRING, because an instrument
/// composed perfectly in <see cref="ConditionsGate"/> and never drawn is exactly the shape of a shipped bug
/// this repo has filed before — and so is one drawn in two places that could come to disagree.</para>
///
/// <para><b>What is proved.</b> The strip is on the glass and is raised by the gate, not by a hand-rolled
/// "is something nearby"; it quotes no threshold of its own; the piracy pop-up the owner asked us to
/// generalize renders the SAME chip component the strip does; the Scope corner mirrors the strip's verdict
/// and is empty when nothing is live; and #242's line appears only at the moment the owner named, with the
/// frame body's name in it, behind a chip that goes through the page's one frame verb.</para>
///
/// <para>Each gate carries an anti-vacuous half — something unrelated must be found in the same file first,
/// or the absent thing must be shown present in the paired case — so a rename or a move reddens these
/// instead of quietly passing over nothing.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheCriteriaAreOnTheGlassTests
{
    private const int SizePx = 280;

    private static string Source(params string[] parts)
    {
        string path = Path.Combine(
            new[] { TestTree.RepoRoot(), "src", "SpaceSails.Client" }.Concat(parts).ToArray());
        Assert.True(File.Exists(path), $"{string.Join('/', parts)} is not where this guard reads it ({path}).");
        string text = File.ReadAllText(path);
        Assert.True(text.Length > 300, $"{string.Join('/', parts)} is suspiciously empty — this guard would read nothing.");
        return text;
    }

    // ══ #243 · THE STRIP IS DRAWN, AND IT IS THE PIRACY BOX GENERALIZED ══════════════════════════════

    /// <summary>
    /// <b>THE POP-UP AND THE STRIP ARE THE SAME COMPONENT.</b> Owner: <i>"The piracy pop-up has the criteria
    /// nicely shown… I guess it could be a general meet-the-conditions [display] in the navigation
    /// screen."</i> A generalization that left the original behind would put two boxes in the game each
    /// claiming to be the instrument, so both surfaces are held to rendering <c>&lt;CriteriaChip</c> — and
    /// the chip's own markup is checked for the colours the criterion box has always worn, because "the same
    /// component" is only worth anything if the component is the one the owner liked.
    /// </summary>
    [Fact]
    public void ThePiracyPopUpAndTheNavStripRenderTheOneChip()
    {
        string strip = Source("Pages", "Map", "NavHud", "ConditionsStrip.razor");
        string dossier = Source("Pages", "Map", "DossierCard.razor");
        string chip = Source("Components", "CriteriaChip.razor");

        Assert.Contains("<CriteriaChip Criterion=", strip, StringComparison.Ordinal);
        Assert.Contains("<CriteriaChip Criterion=", dossier, StringComparison.Ordinal);

        // The pop-up is still the pop-up the owner was reading — its heading and its verdict lines are the
        // anti-vacuous half: if this box were ever replaced wholesale, this fails rather than passes over a
        // card that no longer exists.
        Assert.Contains("🎯 Autosteal needs BOTH:", dossier, StringComparison.Ordinal);
        Assert.Contains("→ BOARDABLE NOW", dossier, StringComparison.Ordinal);

        // …and the chip wears the criterion box's own colours and marks, adding none.
        Assert.Contains("text-success", chip, StringComparison.Ordinal);
        Assert.Contains("text-warning", chip, StringComparison.Ordinal);
        Assert.Contains("✅", chip, StringComparison.Ordinal);
        Assert.Contains("❌", chip, StringComparison.Ordinal);
        foreach (string mintedColour in new[] { "text-danger", "text-primary", "rgb(", "style=" })
        {
            Assert.DoesNotContain(mintedColour, chip, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// <b>ONE SOURCE — the strip quotes no threshold and makes no comparison.</b> The named bug class: a
    /// number typed twice, once where it is enforced and once where it is displayed, drifting apart at
    /// leisure. The strip's whole job is to say what the machinery will do, so the moment one of these
    /// appears in its markup the display has started arguing with the gate.
    /// </summary>
    [Fact]
    public void TheStripReTypesNoThresholdAndJudgesNothing()
    {
        string strip = Source("Pages", "Map", "NavHud", "ConditionsStrip.razor");
        string body = strip[strip.IndexOf("@if (ActiveConditions()", StringComparison.Ordinal)..];

        foreach (string typedTwice in new[]
                 {
                     "CaptureRadiusMeters", "MaxRelativeSpeed", "EnvelopeMeters", "MatchSpeed", "CaptureRange",
                     "DockReachMeters", "DockMatchSpeedMps", "500,000", "5e8", "8000", "/ 1000",
                     "<=", ">=", "? \"✅\"",
                 })
        {
            Assert.DoesNotContain(typedTwice, body, StringComparison.Ordinal);
        }

        // …and the file DOES name the composer, which is what makes the absences above meaningful rather
        // than a slice that happened to catch an empty string.
        Assert.Contains("ConditionsGate.Title(conditions)", body, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>THE HUD DRAWS IT, in the readouts flow, above #200's clamp panel.</b> The summary is read before
    /// the detail, and both are in-FLOW sections of the same <c>.map-readouts</c> block — the housing #1043
    /// settled for this family, so nothing here can fight the nav lines or the Plotting panel for a pixel.
    /// </summary>
    [Fact]
    public void TheHudHostsTheStripAboveTheClampPanel()
    {
        string hud = Source("Pages", "Map", "NavHud.razor");
        int stripAt = hud.IndexOf("<ConditionsStrip ActiveConditions=", StringComparison.Ordinal);
        int clampAt = hud.IndexOf("<DockFocusBox ", StringComparison.Ordinal);
        int readoutsAt = hud.IndexOf("class=\"map-readouts", StringComparison.Ordinal);

        Assert.True(stripAt > 0, "the HUD does not host <ConditionsStrip> at all — the instrument is not drawn.");
        Assert.True(clampAt > stripAt, "the clamp's detail panel is drawn BEFORE the strip that summarises it.");
        Assert.True(readoutsAt > 0 && readoutsAt < stripAt,
            "the strip is outside the .map-readouts flow — it has become a floating card, which is the "
            + "geometry #1043 ruled against for exactly this family of boxes.");
    }

    // ══ #243 · WHICH GATE IS LIVE, ASKED OF A REAL PAGE ══════════════════════════════════════════════

    /// <summary>
    /// <b>NO LIVE GATE, NO STRIP.</b> Owner's complaint was a screen that has to be hunted across; a
    /// permanently reserved rectangle that is blank most of the time is one more thing to hunt past. Paired
    /// with the clamp case below, so "absent" cannot mean "absent always".
    /// </summary>
    [Fact]
    public void ADriftingSkyRaisesNoStripAtAll()
    {
        Pages.Map map = APageInTheSky();
        Assert.Null(TheStrip(map));
    }

    /// <summary>
    /// <b>THE CLAMP RAISES IT, and its chips are the clamp's own rows.</b> The gate is the affordance's
    /// phase — the same one-truth (#212) the ⚓ button reads — so a haven merely drifting past cannot raise
    /// it, which is the half of #200 the owner filed as "it toggles me meaningless nearby targets here".
    /// </summary>
    [Fact]
    public void TheClampRaisesTheStripAndTheChipsAreTheClampsOwnRows()
    {
        Pages.Map map = APageInTheSky();
        CelestialBody haven = AHavenIsBeingApproached(map);

        ConditionsReading strip = TheStrip(map) ?? throw new InvalidOperationException(
            "#243 · a live dock affordance raised NO strip — the clamp's gate is not reaching the instrument.");

        Assert.Equal(ConditionGateKind.Dock, strip.Kind);
        Assert.Equal(haven.Name, strip.TargetName);

        IReadOnlyList<DockGateRow> rows = DockFocus.Rows((DockAffordance)Get(map, "_dockAffordance")!, Tank(map));
        Assert.Equal(rows.Count, strip.Criteria.Count);
        for (int i = 0; i < rows.Count; i++)
        {
            Assert.Equal(rows[i].Label, strip.Criteria[i].Label);
            Assert.Equal(rows[i].Reading, strip.Criteria[i].Reading);
            Assert.Equal(rows[i].Gate, strip.Criteria[i].Gate);
            Assert.Equal(rows[i].Inside, strip.Criteria[i].Inside);
        }
    }

    /// <summary>
    /// <b>AN ARMED ARRIVAL RAISES IT, with the capture gate's own two numbers.</b> The one state in which
    /// "am I going to make this orbit" is a question about NOW rather than about a plan. The body is named,
    /// which is #950's lesson: a row read against the wrong world is worse than no row.
    /// </summary>
    [Fact]
    public void AnArmedInsertionRaisesTheStripAgainstTheBodyItIsArmedAt()
    {
        Pages.Map map = APageInTheSky();
        CelestialBody moon = Ephemeris(map).Bodies.First(b => b.Kind == BodyKind.Moon && b.ParentId is not null);
        Set(map, "_armedOrbitBodyId", moon.Id);

        ConditionsReading strip = TheStrip(map) ?? throw new InvalidOperationException(
            "#243 · an armed insertion raised NO strip — the capture gate is not reaching the instrument.");

        Assert.Equal(ConditionGateKind.Orbit, strip.Kind);
        Assert.Equal(moon.Name, strip.TargetName);
        Assert.Equal(2, strip.Criteria.Count);

        // …and the two chips are ArrivalStepRule's judgement of the very same geometry — the #955 ARRIVE
        // row's own ✓/✗ bit, not a second opinion about the same approach.
        CelestialBody parent = Ephemeris(map).Bodies.First(b => b.Id == moon.ParentId);
        double hill = OrbitRule.HillRadius(moon, parent.Mu);
        Vector2d at = Ephemeris(map).Position(moon.Id, SimTime(map));
        ShipState ship = (ShipState)Get(map, "_ship")!;
        ArrivalStepRule.ArrivalCheck row = ArrivalStepRule.Check(
            ArrivalStepRule.ArrivalKind.Orbit, moon.Name,
            (at - ship.Position).Length,
            (VelocityOf(map, moon.Id) - ship.Velocity).Length, hill);

        Assert.Equal(row.DistanceOk, strip.Criteria[0].Inside);
        Assert.Equal(row.SpeedOk, strip.Criteria[1].Inside);
    }

    /// <summary>
    /// <b>THE BOARDING WINDOW OUTRANKS THEM BOTH.</b> Two gates can be live at once, and the one that closes
    /// in seconds is the one the strip speaks. Paired: with the prey deselected the SAME page falls back to
    /// the clamp, so this cannot pass on a strip that only ever says "Board".
    /// </summary>
    [Fact]
    public void ASelectedPreyOutranksAClampThatIsAlsoLive()
    {
        Pages.Map map = APageInTheSky();
        AHavenIsBeingApproached(map);

        APreyIsSelected(map);

        ConditionsReading withPrey = TheStrip(map) ?? throw new InvalidOperationException(
            "#243 · a selected prey raised NO strip.");
        Assert.Equal(ConditionGateKind.Board, withPrey.Kind);

        Set(map, "_selectedTargetId", null);
        ConditionsReading withoutPrey = TheStrip(map) ?? throw new InvalidOperationException(
            "#243 · with the prey deselected the clamp's gate should still be live.");
        Assert.Equal(ConditionGateKind.Dock, withoutPrey.Kind);
    }

    // ══ #243 · THE SCOPE CORNER MIRRORS IT ═══════════════════════════════════════════════════════════

    /// <summary>
    /// <b>THE GLASS SAYS THE SAME THING THE COLUMN DOES.</b> Owner: <i>"Mirror the strip's summary in the
    /// Scope corner (where the eyes are during an approach)."</i> The corner is asserted against the strip's
    /// OWN summary rather than a typed string, so a mirror that had drifted is red; and the unmirrored case
    /// is asserted too, because a corner that always prints something is furniture.
    /// </summary>
    [Fact]
    public void TheScopeCornerCarriesTheStripsVerdictAndNothingWhenNoGateIsLive()
    {
        var lit = new RecordingPen();
        var scope = new ScopeView(lit)
        {
            GateSummary = ConditionsGate.ScopeSummary(ConditionsGate.Board("Kestrel", 1e8, 40_000, 0)),
            GateMet = false,
        };
        scope.Draw(SizePx, 0, Vector2d.Zero, Vector2d.Zero, AFreighterAt(1e8));
        Assert.Contains("🎯 ✓✗", lit.Texts);

        // …and with nothing live the eyepiece is exactly what it always was.
        var dark = new RecordingPen();
        new ScopeView(dark).Draw(SizePx, 0, Vector2d.Zero, Vector2d.Zero, AFreighterAt(1e8));
        Assert.DoesNotContain(dark.Texts, t => t.Contains('✓', StringComparison.Ordinal)
                                            || t.Contains('✗', StringComparison.Ordinal));

        // The anti-vacuous half: the scope really did draw its usual readouts in both runs, so "nothing"
        // above means "no gate line", not "no scope".
        Assert.Contains("FREIGHTER", dark.Texts);
        Assert.Contains("FREIGHTER", lit.Texts);
    }

    /// <summary>The page hands the glass that summary out of ONE call to the same question the strip asks —
    /// so the corner cannot say ✓✓ over a strip showing a red chip.</summary>
    [Fact]
    public void TheCornerAndTheColumnAskOneQuestion()
    {
        string views = Source("Pages", "Map.Sim.Tick.Views.cs");
        Assert.Contains("_scopeView.GateSummary = ConditionsScopeSummary();", views, StringComparison.Ordinal);
        Assert.Contains("_scopeView.GateMet = ConditionsScopeMet();", views, StringComparison.Ordinal);

        string conditions = Source("Pages", "Map.Conditions.cs");
        Assert.Contains("ActiveConditions() is { } conditions ? ConditionsGate.ScopeSummary(conditions)",
            conditions, StringComparison.Ordinal);

        Pages.Map map = APageInTheSky();
        Assert.Equal(string.Empty, (string)Invoke(map, "ConditionsScopeSummary")!);
        AHavenIsBeingApproached(map);
        Assert.Equal(
            ConditionsGate.ScopeSummary(TheStrip(map)!.Value),
            (string)Invoke(map, "ConditionsScopeSummary")!);
    }

    // ══ #242 · THE LINE THAT CONNECTS TWO NUMBERS ════════════════════════════════════════════════════

    /// <summary>
    /// <b>IT SPEAKS WHILE A BURN IS BEING PLANNED IN A FRAME THAT IS EATING THE CRUISE, AND NOT OTHERWISE.</b>
    /// Owner: <i>"the origin was set to Mars... after setting to Sun we got going. We should have some tip
    /// about that in burn planning."</i> The page is put in exactly that state — riding a body's own motion
    /// in that body's frame, panel open — and then taken out of it one condition at a time.
    /// </summary>
    [Fact]
    public void TheLineAppearsOnlyWhilePlanningInAFrameThatIsHidingTheCruise()
    {
        Pages.Map map = APageInTheSky();
        CelestialBody host = Ephemeris(map).Bodies.First(b => b.Id == "mars");

        // Riding Mars: the ship carries Mars's own heliocentric velocity, so the Mars frame shows ~nothing
        // while the Sun frame shows the whole cruise. This is the owner's post-undock situation exactly.
        Vector2d marsVel = VelocityOf(map, host.Id);
        Set(map, "_ship", new ShipState(
            Ephemeris(map).Position(host.Id, SimTime(map)) + new Vector2d(2e7, 0), marsVel, SimTime(map)));
        Set(map, "_plotFrameBodyId", host.Id);
        Set(map, "PlotMode", true);

        (string Line, string Readout) tip = TheTip(map) ?? throw new InvalidOperationException(
            "#242 · planning a burn in the Mars frame while riding Mars raised NO line — the tip is not wired.");
        Assert.Equal(FrameMotionTip.Line(host.Name), tip.Line);
        Assert.Contains("Mars", tip.Line, StringComparison.Ordinal);
        Assert.Contains("km/s is showing", tip.Readout, StringComparison.Ordinal);

        // Close the panel and stop the engine: nothing is being decided, so nothing is said.
        Set(map, "PlotMode", false);
        Assert.Null(TheTip(map));

        // Re-open it, then read the plan in the Sun's frame — the cure the owner found by hand.
        Set(map, "PlotMode", true);
        Assert.NotNull(TheTip(map));
        Invoke(map, "SetPlotFrame", [null]);
        Assert.Null(TheTip(map));
        Assert.Null(Get(map, "_plotFrameBodyId"));
    }

    /// <summary>
    /// <b>THE CHIP GOES THROUGH THE PAGE'S ONE FRAME VERB.</b> #144's law is that a single selection rules the
    /// map, the ribbon and the readouts; a chip that wrote <c>_plotFrameBodyId</c> itself would switch the
    /// plan into a frame the rest of the HUD was not reading. It calls <c>SetPlotFrame</c>, like the chips
    /// and the picker.
    /// </summary>
    [Fact]
    public void TheSwitchChipUsesThePagesOwnFrameVerb()
    {
        string row = Source("Pages", "Map", "NavHud", "FrameMotionTipRow.razor");
        Assert.Contains("SetPlotFrame(null)", row, StringComparison.Ordinal);
        Assert.Contains("FrameMotionTip.SwitchChip", row, StringComparison.Ordinal);

        // No sentence and no threshold of its own — both are Core's, so the razor cannot drift from the law.
        foreach (string typedTwice in new[] { "Barely moving", "0.15", "15%", "v helio", "helio" })
        {
            Assert.DoesNotContain(typedTwice, row[row.IndexOf("@if (FrameMotionTipLine()", StringComparison.Ordinal)..],
                StringComparison.Ordinal);
        }

        // It is a row in the flow with a button on it, never a surface that has to be dismissed — the
        // 2026-08-24 pop-up ruling, which this one obeys by having nothing to close.
        Assert.DoesNotContain("view-object", row, StringComparison.Ordinal);
        Assert.DoesNotContain("OverlayShell", row, StringComparison.Ordinal);
    }

    /// <summary>The plotting card carries the standing explanation (#119's inventory idiom), asked of Core
    /// rather than typed — the card's own rule 2, which is what keeps a help page from describing a game
    /// that has moved on.</summary>
    [Fact]
    public void ThePlottingCardCarriesTheStandingExplanation()
    {
        string card = Source("Components", "PlottingHelp.razor");
        Assert.Contains("@FrameMotionTip.StandingExplanation", card, StringComparison.Ordinal);
        Assert.DoesNotContain("Frames hide shared motion —", card[card.IndexOf("<div class=\"plotting-help\">",
            StringComparison.Ordinal)..], StringComparison.Ordinal);
    }

    // ── THE BENCH ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A page with the shipping sky under it and the shipping ship in it, and nothing else set —
    /// the state every guard above starts from. No renderer is attached because nothing here renders: these
    /// are all pure questions the page answers about the frame it is in.</summary>
    private static Pages.Map APageInTheSky()
    {
        var map = new Pages.Map();
        ICelestialEphemeris ephemeris = CircularOrbitEphemeris.FromScenario(TestTree.Sol);
        Set(map, "_scenarioName", TestTree.Sol.Name);
        Set(map, "_ephemeris", ephemeris);
        Set(map, "_ship", Invoke(map, "InitializeShipState")!);
        return map;
    }

    private static ICelestialEphemeris Ephemeris(Pages.Map map) => (ICelestialEphemeris)Get(map, "_ephemeris")!;

    /// <summary>A REAL dockable haven out of the shipping sky, coasting toward it and still outside the
    /// door — the affordance's Approach phase, which is the one #200's panel and this strip both speak on.
    /// The haven is found through <see cref="DockableHavens.IsDockable"/>, the very predicate the ⚓ button
    /// obeys, so this bench cannot be approaching something the game would not let anybody clamp onto.</summary>
    private static CelestialBody AHavenIsBeingApproached(Pages.Map map)
    {
        CelestialBody haven = Ephemeris(map).Bodies.First(DockableHavens.IsDockable);
        Set(map, "_dockAffordance",
            new DockAffordance(DockPhase.Approach, haven.Id, haven.Name, 9e8, 3_000, 12, false));
        return haven;
    }

    private static double SimTime(Pages.Map map) => (double)Get(map, "SimTime")!;

    /// <summary>A body's velocity the way every caller in the client takes it: the one-second central
    /// difference of its analytic ephemeris position.</summary>
    private static Vector2d VelocityOf(Pages.Map map, string bodyId)
    {
        const double h = 1.0;
        double t = SimTime(map);
        return (Ephemeris(map).Position(bodyId, t + h) - Ephemeris(map).Position(bodyId, t - h)) / (2 * h);
    }

    private static ConditionsReading? TheStrip(Pages.Map map) =>
        (ConditionsReading?)Invoke(map, "ActiveConditions");

    private static (string Line, string Readout)? TheTip(Pages.Map map) =>
        ((string Line, string Readout)?)Invoke(map, "FrameMotionTipLine");

    private static int Tank(Pages.Map map) =>
        (int)typeof(Pages.Map)
            .GetProperty("EffectiveDockTankPulses", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(map)!;

    /// <summary>
    /// The shipping traffic, wrapped in the component's own private <c>NpcState</c>, with the first hull put
    /// a hundred thousand kilometres off the bow and selected — the state the scope selection leaves behind.
    /// Generated from the SHIPPING seeds (42/43, the boot's own), so this is a hull the game really flies.
    /// Returns her id.
    /// </summary>
    private static string APreyIsSelected(Pages.Map map)
    {
        ICelestialEphemeris eph = Ephemeris(map);
        IReadOnlyList<NpcShip> fleet = TrafficSchedule.Generate(eph, seed: 42, count: 8);
        Assert.NotEmpty(fleet);

        Type stateType = typeof(Pages.Map).GetNestedType("NpcState", Hidden)!;
        Array states = Array.CreateInstance(stateType, fleet.Count);
        ShipState ship = (ShipState)Get(map, "_ship")!;
        for (int i = 0; i < fleet.Count; i++)
        {
            object one = Activator.CreateInstance(stateType, nonPublic: true)!;
            stateType.GetField("Ship", Hidden)!.SetValue(one, fleet[i]);
            stateType.GetField("Active", Hidden)!.SetValue(one, true);
            stateType.GetField("CurrentlyObserved", Hidden)!.SetValue(one, true);
            // Beside the bow and matched: the boarding window is genuinely open on her, which is what makes
            // "the strip speaks about HER" a claim about the gate rather than about a list index.
            stateType.GetField("State", Hidden)!.SetValue(one, new ShipState(
                ship.Position + new Vector2d(1e8, 0), ship.Velocity, SimTime(map)));
            states.SetValue(one, i);
        }

        Set(map, "_npcStates", states);
        Set(map, "_selectedTargetId", fleet[0].Id);
        return fleet[0].Id;
    }

    /// <summary>Records rather than renders — the idiom every drawn-thing guard in this suite uses (the pen
    /// is private per class here by house habit, so this is that pen, cut to the one thing this file
    /// reads off the glass).</summary>
    private sealed class RecordingPen : IRenderer
    {
        public List<string> Texts { get; } = [];

        public void BeginFrame(int widthPx, int heightPx, RgbaColor background) => Texts.Clear();

        public void EndFrame() { }

        public int RegisterImage(string url) => 1;

        public void DrawCircle(float x, float y, float r, RgbaColor? fill, RgbaColor stroke, float w = 1f) { }

        public void DrawPolyline(ReadOnlySpan<float> pts, RgbaColor stroke, float w = 1f) { }

        public void DrawPolygon(ReadOnlySpan<float> pts, RgbaColor? fill, RgbaColor stroke, float w = 1f) { }

        public void DrawText(float x, float y, string text, RgbaColor c, string font = "12px monospace",
                             TextAlign align = TextAlign.Left) => Texts.Add(text ?? "");

        public void DrawImage(int id, float x, float y, float w, float h, float a = 1f) { }

        public void DrawImageSlice(int id, float sx, float sy, float sw, float sh,
                                   float x, float y, float w, float h, float a = 1f) { }
    }

    private static ScopeView.Target AFreighterAt(double range) =>
        new(ScopeView.TargetKind.Freighter, "KESTREL", null,
            new Vector2d(range, 0), new Vector2d(0, 1_000), 0, new RgbaColor(200, 200, 200), false);

    // ── The reflection, named once ───────────────────────────────────────────────────────────────────

    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private static void Set(Pages.Map map, string member, object? value)
    {
        if (typeof(Pages.Map).GetField(member, Hidden) is { } field)
        {
            field.SetValue(map, value);
            return;
        }
        if (typeof(Pages.Map).GetProperty(member, Hidden) is { CanWrite: true } property)
        {
            property.SetValue(map, value);
            return;
        }
        throw new InvalidOperationException($"Map has no writable member '{member}' — this bench sets nothing.");
    }

    private static object? Get(Pages.Map map, string member) =>
        typeof(Pages.Map).GetField(member, Hidden) is { } field
            ? field.GetValue(map)
            : typeof(Pages.Map).GetProperty(member, Hidden) is { } property
                ? property.GetValue(map)
                : throw new InvalidOperationException($"Map has no member '{member}' — this bench reads nothing.");

    private static object? Invoke(Pages.Map map, string method, object?[]? args = null) =>
        (typeof(Pages.Map).GetMethod(method, Hidden)
         ?? throw new InvalidOperationException($"Map has no method '{method}' — this bench calls nothing."))
        .Invoke(map, args ?? []);
}
