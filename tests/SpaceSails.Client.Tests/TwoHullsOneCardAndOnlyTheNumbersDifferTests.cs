using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Pages;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #534 slice 1 · <b>TWO HULLS, ONE CARD, AND THE ONLY THING THAT SEPARATES THEM IS ARITHMETIC.</b>
///
/// <para>The Core guards prove the rule. This one proves the PAGE — because the whole of #534 lives or dies
/// on what the captain is shown, and a rule that read perfectly into a card carrying a 🐺 beside her name
/// would have deleted its own mechanic before the player ever did a sum.</para>
///
/// <para>So the page is driven for real: two hulls with the same callsign, the same manifest, the same range,
/// the same closing speed and the same everything the schedule can give them, differing in ONE character of
/// their ids — and the page's own <c>DossierFor</c> is asked for both files. Every field of
/// <c>DossierInfo</c> is then compared by reflection, which is the only version of this assertion that stays
/// true when somebody adds a field: <b>the readings differ, and nothing else does.</b></para>
///
/// <para>The second half drives the one live branch: the captain closes to inside the boarding envelope on
/// both hulls, the page's own break-for-open-water runs, and the two are flown through the real simulator.
/// One is still boardable and one is not.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TwoHullsOneCardAndOnlyTheNumbersDifferTests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

    private static readonly Lazy<SpaceSails.Contracts.ScenarioDefinition> Sol =
        new(() => ScenarioLoader.LoadFile(Path.Combine(RepoRoot(), "scenarios", "sol.json")));

    /// <summary>The captain's own range and closing speed, well inside sensor reach and well outside the
    /// boarding envelope — the pre-commit distance the whole mechanic is about.</summary>
    private const double Range = 2.0e9;
    private const double Drift = 900;

    // ── the two hulls ────────────────────────────────────────────────────────────────────────────────

    /// <summary>The first two ids out of the schedule's own id space that land on opposite sides of the
    /// mask. Found by asking Core rather than typed, so a change to the hash cannot leave this bench
    /// silently testing two honest haulers against each other.</summary>
    private static (string Masked, string Honest) TheTwoIds()
    {
        string? masked = null, honest = null;
        for (int i = 0; i < 500 && (masked is null || honest is null); i++)
        {
            string id = $"npc-{i}";
            bool isMasked = QShip.IsMasked(HullNamed(id));
            if (isMasked) { masked ??= id; } else { honest ??= id; }
        }

        Assert.NotNull(masked);
        Assert.NotNull(honest);
        return (masked!, honest!);
    }

    private static NpcShip HullNamed(string id) =>
        new(id, "Meridian", "He3", "saturn", "mars", RoutePersonality.Economical,
            DepartureTime: 0, ActivationTime: 0,
            InitialState: new ShipState(Vector2d.Zero, Vector2d.Zero, 0),
            Plan: new ManeuverPlan([]), EstimatedArrivalTime: 60 * 86400,
            CargoUnits: QShip.FatHoldUnits + 3, ManeuverBudget: NpcShip.DefaultManeuverBudget, IsPod: false);

    // ── LAW 1 · THE CARD ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE TWO FILES DIFFER IN THEIR NUMBERS AND IN NOTHING ELSE. Same name, same manifest line, same status
    /// line, same range, same closing, same boarding verdict, same everything the page can say — and one of
    /// them is a warship. If a future hand ever puts a flag, a word or a colour on her, the field-by-field
    /// sweep names the field it went into.
    ///
    /// <para>The two hulls are placed MIRRORED about the captain — same range, same speed, opposite sides —
    /// so distance, relative speed and closing are equal by construction and the sweep is not measuring
    /// geometry it arranged badly.</para>
    ///
    /// <para><b>Proven RED</b> by appending a marker to the masked hull's dossier <c>Detail</c>: the sweep
    /// came back naming <c>Detail</c> as a field that differs between an honest hauler and a masked one.</para>
    /// </summary>
    [Fact]
    public void TheOnlyThingThatSeparatesTheTwoFilesIsArithmetic()
    {
        (string maskedId, string honestId) = TheTwoIds();
        Map map = TwoHullsOffMars(maskedId, honestId);

        object masked = File(map, maskedId);
        object honest = File(map, honestId);

        var differ = new List<string>();
        PropertyInfo[] rows = masked.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        Assert.True(rows.Length >= 12, $"the dossier has only {rows.Length} row(s) — nothing was swept.");

        foreach (PropertyInfo row in rows)
        {
            if (!Equals(row.GetValue(masked), row.GetValue(honest)))
            {
                differ.Add(row.Name);
            }
        }

        Assert.Equal(["Reading"], differ);
    }

    /// <summary>
    /// AND INSIDE THE READING, ONLY THE MEASURED HALF MOVES. Her papers say exactly what an honest hauler's
    /// papers say — that is what makes her papers a lie rather than a warning. So the CLAIMED column of both
    /// files is identical, and every difference is on the measured side.
    ///
    /// <para><b>Proven RED</b> by having <c>ClaimedTrimAccelMps2</c> return the measured value: the claimed
    /// column moved with her and the two hulls stopped claiming the same thing.</para>
    /// </summary>
    [Fact]
    public void HerPapersSayWhatEveryHaulersPapersSay()
    {
        (string maskedId, string honestId) = TheTwoIds();
        Map map = TwoHullsOffMars(maskedId, honestId);

        object masked = Reading(File(map, maskedId));
        object honest = Reading(File(map, honestId));

        Assert.Equal(Field<double>(honest, "ClaimedTrimAccelMps2"), Field<double>(masked, "ClaimedTrimAccelMps2"));
        Assert.Equal(Field<int>(honest, "ClaimedRadiatorPanels"), Field<int>(masked, "ClaimedRadiatorPanels"));
        Assert.Equal(Field<int>(honest, "ClaimedGuardedChannels"), Field<int>(masked, "ClaimedGuardedChannels"));

        // …and all three measurements are over the claim, which is the read the captain is being offered.
        Assert.True(Field<double>(masked, "MeasuredTrimAccelMps2") > Field<double>(masked, "ClaimedTrimAccelMps2"));
        Assert.True(Field<int>(masked, "MeasuredRadiatorPanels") > Field<int>(masked, "ClaimedRadiatorPanels"));
        Assert.True(Field<int>(masked, "MeasuredGuardedChannels") > Field<int>(masked, "ClaimedGuardedChannels"));

        // The honest hull's arithmetic closes on the same card, which is what makes the other one readable.
        Assert.Equal(Field<double>(honest, "ClaimedTrimAccelMps2"), Field<double>(honest, "MeasuredTrimAccelMps2"));
        Assert.Equal(Field<int>(honest, "ClaimedRadiatorPanels"), Field<int>(honest, "MeasuredRadiatorPanels"));
        Assert.Equal(Field<int>(honest, "ClaimedGuardedChannels"), Field<int>(honest, "MeasuredGuardedChannels"));
    }

    /// <summary>
    /// THE TWO ROWS THE GLASS OWES ARE NOT DRAWN UNTIL THE GLASS HAS HER. Without a telescope fix the card
    /// carries the burn alone — read off her observed motion — and the radiator and comms rows are absent,
    /// which is #1121's "a tell may need a completed pass" applied to a living hull.
    ///
    /// <para><b>Proven RED</b> by passing <c>true</c> for the fix in <c>DossierFor</c>: the two glass rows
    /// appeared on a contact no telescope was holding.</para>
    /// </summary>
    [Fact]
    public void TheGlassRowsWaitForTheGlass()
    {
        (string maskedId, string honestId) = TheTwoIds();
        Map map = TwoHullsOffMars(maskedId, honestId);

        // No tracking post is wired on this bench, so no fix is held on either hull.
        Assert.False(Field<bool>(Reading(File(map, maskedId)), "FixHeld"));
        Assert.False(Field<bool>(Reading(File(map, honestId)), "FixHeld"));

        // The razor draws the two glass rows behind exactly that flag and the burn row in front of it.
        string razor = System.IO.File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "SpaceSails.Client", "Pages", "Map", "DossierCard.razor")).Replace("\r\n", "\n");
        int block = razor.IndexOf("📐 Measured · claimed", StringComparison.Ordinal);
        Assert.True(block > 0, "the measured/claimed block is not on the dossier card any more.");

        int burn = razor.IndexOf("burn @read.MeasuredTrimAccelMps2", StringComparison.Ordinal);
        int gate = razor.IndexOf("if (read.FixHeld)", StringComparison.Ordinal);
        int radiator = razor.IndexOf("radiator @read.MeasuredRadiatorPanels", StringComparison.Ordinal);
        int comms = razor.IndexOf("comms @read.MeasuredGuardedChannels", StringComparison.Ordinal);

        Assert.True(burn > block && burn < gate, "the burn row must be drawn before the fix gate.");
        Assert.True(radiator > gate && comms > gate, "the radiator and comms rows must be behind the fix gate.");
    }

    /// <summary>
    /// NOTHING ON THE CARD SAYS WHAT SHE IS. The block the page draws is swept for the verdict, in every
    /// spelling a hand might reach for, plus §8's reserved word. This is the client half of the Core sweep:
    /// a rule that publishes no prose can still be betrayed by the markup that reads it.
    ///
    /// <para><b>Proven RED</b> by adding <c>&lt;div&gt;Q-SHIP&lt;/div&gt;</c> to the block: the word was
    /// quoted back.</para>
    /// </summary>
    [Fact]
    public void TheCardNeverSaysWhatSheIs()
    {
        string razor = System.IO.File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "SpaceSails.Client", "Pages", "Map", "DossierCard.razor")).Replace("\r\n", "\n");

        int block = razor.IndexOf("📐 Measured · claimed", StringComparison.Ordinal);
        Assert.True(block > 0, "the measured/claimed block is not on the dossier card any more.");
        int end = razor.IndexOf("The autosteal criterion box", StringComparison.Ordinal);
        Assert.True(end > block, "the block's far fence has moved — this guard is reading the wrong lines.");

        string drawn = razor[block..end];
        Assert.True(drawn.Length > 200, $"only {drawn.Length} characters were swept — nothing was read.");

        foreach (string word in new[] { "q-ship", "qship", "warship", "corvette", "man-of-war", "wolf", "monolith" })
        {
            Assert.DoesNotContain(word, drawn, StringComparison.OrdinalIgnoreCase);
        }
    }

    // ── LAW 2 · THE ONE LIVE BRANCH ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// SHE BREAKS AND THE HAULER BESIDE HER DOES NOT — and then the sim says what that cost the captain.
    /// Both hulls are put inside the boarding envelope, the page's own break runs, and both are flown
    /// through the real <see cref="Simulator"/> for a minute of sim time. The honest hauler is still
    /// boardable; the other one is not.
    ///
    /// <para>This is the assertion that makes the read worth doing, and it is measured through
    /// <see cref="CaptureRule.IsInWindow"/> — the game's own gate — rather than by looking at a flag.</para>
    ///
    /// <para><b>Proven RED</b> by dropping the <c>QShip.IsMasked</c> gate from <c>LetTheMaskedHullsRun</c>:
    /// the honest hauler broke too, and traffic that has never reacted to the player in this game started
    /// running from him.</para>
    /// </summary>
    [Fact]
    public void SheBreaksAndTheHaulerBesideHerDoesNot()
    {
        (string maskedId, string honestId) = TheTwoIds();
        Map map = TwoHullsInTheBoardingWindow(maskedId, honestId);

        var ship = (ShipState)Get(map, "_ship")!;
        object[] hulls = (object[])Get(map, "_npcStates")!;

        foreach (object npc in hulls)
        {
            Assert.True(CaptureRule.IsInWindow(ship, (ShipState)Field(npc, "State")!),
                "both hulls have to start boardable, or the break is not what changed anything.");
        }

        Invoke(map, "LetTheMaskedHullsRun");

        // Four hours of sim time, flown for real: the captain coasts and each hull flies her own plan.
        const int Minutes = 240;
        var sim = new Simulator(CircularOrbitEphemeris.FromScenario(Sol.Value), TrafficSchedule.NpcTimeStep);
        ShipState captain = ship;
        for (int i = 0; i < Minutes; i++)
        {
            captain = sim.Step(captain, null);
        }

        var stillBoardable = new Dictionary<string, bool>();
        foreach (object npc in hulls)
        {
            var hull = (NpcShip)Field(npc, "Ship")!;
            var state = (ShipState)Field(npc, "State")!;
            for (int i = 0; i < Minutes; i++)
            {
                state = sim.Step(state, hull.Plan);
            }

            stillBoardable[hull.Id] = CaptureRule.IsInWindow(captain, state);
        }

        Assert.True(stillBoardable[honestId], "an honest hauler has never run from the player and must not start.");
        Assert.False(stillBoardable[maskedId], "she opened the range and the boarding window should have shut.");
    }

    /// <summary>A HOLED SAIL STILL ENDS IT. The gun is what makes an evasive ship catchable — the law the
    /// tutorial's escape jink already teaches — so a hull with a slug through her sail never gets a break to
    /// fly, and neither does one already boarded.
    ///
    /// <para><b>Proven RED</b> by dropping <c>npc.Disabled</c> from the skip list: a drifting hulk laid a
    /// burn into her own plan.</para></summary>
    [Fact]
    public void AHoledSailStillEndsIt()
    {
        (string maskedId, string honestId) = TheTwoIds();
        Map map = TwoHullsInTheBoardingWindow(maskedId, honestId);
        object[] hulls = (object[])Get(map, "_npcStates")!;
        foreach (object npc in hulls)
        {
            npc.GetType().GetField("Disabled")!.SetValue(npc, true);
        }

        Invoke(map, "LetTheMaskedHullsRun");

        foreach (object npc in hulls)
        {
            Assert.Empty(((NpcShip)Field(npc, "Ship")!).Plan.Nodes);
            Assert.False((bool)Field(npc, "Broke")!);
        }
    }

    /// <summary>ONE BREAK, NOT A THRUSTER. Holding the window open does not hand her a burn every frame.</summary>
    [Fact]
    public void SheBreaksOnce()
    {
        (string maskedId, string honestId) = TheTwoIds();
        Map map = TwoHullsInTheBoardingWindow(maskedId, honestId);
        object[] hulls = (object[])Get(map, "_npcStates")!;

        for (int i = 0; i < 20; i++)
        {
            Invoke(map, "LetTheMaskedHullsRun");
        }

        object masked = hulls.First(n => ((NpcShip)Field(n, "Ship")!).Id == maskedId);
        Assert.Single(((NpcShip)Field(masked, "Ship")!).Plan.Nodes);
    }

    // ─────────────────────────── the bench ───────────────────────────

    /// <summary>Two hulls mirrored about a captain parked off Mars — same range, same speed, opposite
    /// beams — so every geometric field of the two dossiers is equal by construction.</summary>
    private static Map TwoHullsOffMars(string maskedId, string honestId) =>
        TwoHulls(maskedId, honestId, Range, Drift);

    /// <summary>The same two, inside the boarding envelope and matched to the captain's velocity.</summary>
    private static Map TwoHullsInTheBoardingWindow(string maskedId, string honestId) =>
        TwoHulls(maskedId, honestId, CaptureRule.CaptureRadiusMeters * 0.9, 0);

    private static Map TwoHulls(string maskedId, string honestId, double range, double drift)
    {
        var map = new Map();
        typeof(ComponentBase).GetField("_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(map, true);

        ICelestialEphemeris ephemeris = CircularOrbitEphemeris.FromScenario(Sol.Value);
        Set(map, "_scenarioName", Sol.Value.Name);
        Set(map, "_ephemeris", ephemeris);
        Set(map, "_simulator", new Simulator(ephemeris, timeStepSeconds: 1.0));
        Set(map, "_npcSimulator", new Simulator(ephemeris, TrafficSchedule.NpcTimeStep));

        // Well clear of Mars itself: a captain standing on the body's own coordinates divides by zero.
        Vector2d here = ephemeris.Position("mars", 0) + new Vector2d(0, 3.0e9);
        Set(map, "_ship", new ShipState(here, new Vector2d(0, 24000), 0));

        Type stateType = typeof(Map).GetNestedType("NpcState")!;
        Array hulls = Array.CreateInstance(stateType, 2);
        hulls.SetValue(Hull(stateType, maskedId, here + new Vector2d(range, 0), new Vector2d(drift, 24000)), 0);
        hulls.SetValue(Hull(stateType, honestId, here - new Vector2d(range, 0), new Vector2d(-drift, 24000)), 1);
        Set(map, "_npcStates", hulls);
        return map;
    }

    private static object Hull(Type stateType, string id, Vector2d position, Vector2d velocity)
    {
        object npc = Activator.CreateInstance(stateType)!;
        var state = new ShipState(position, velocity, 0);
        stateType.GetField("Ship")!.SetValue(npc, HullNamed(id));
        stateType.GetField("State")!.SetValue(npc, state);
        stateType.GetField("Active")!.SetValue(npc, true);
        stateType.GetField("CurrentlyObserved")!.SetValue(npc, true);
        stateType.GetField("LastObservation")!.SetValue(npc, new Observation(id, 0, position, velocity));
        return npc;
    }

    /// <summary>The page's own dossier for one hull — the shipping method, not a stand-in.</summary>
    private static object File(Map map, string id) =>
        Invoke(map, "DossierFor", id) ?? throw new InvalidOperationException($"no dossier for {id}");

    /// <summary>Unwrap the nullable <c>HullReading</c> the dossier carries.</summary>
    private static object Reading(object dossier) =>
        dossier.GetType().GetProperty("Reading")!.GetValue(dossier)
        ?? throw new InvalidOperationException("the dossier carries no reading");

    /// <summary>One member of a record struct (a property) or of the page's own NpcState (a field).</summary>
    private static object? Field(object o, string name)
    {
        Type t = o.GetType();
        return t.GetProperty(name)?.GetValue(o)
            ?? (t.GetField(name) ?? throw new InvalidOperationException($"no member {name} on {t.Name}")).GetValue(o);
    }

    private static T Field<T>(object o, string name) => (T)Field(o, name)!;

    private static object? Get(object o, string field) =>
        (o.GetType().GetField(field, Hidden)
         ?? throw new InvalidOperationException($"no field {field} on Map — this bench has drifted"))
        .GetValue(o);

    private static void Set(object o, string field, object? value) =>
        (o.GetType().GetField(field, Hidden)
         ?? throw new InvalidOperationException($"no field {field} on Map — this bench has drifted"))
        .SetValue(o, value);

    private static object? Invoke(object o, string method, params object?[] args) =>
        (o.GetType().GetMethod(method, Hidden)
         ?? throw new InvalidOperationException($"no method {method} on Map — this bench has drifted"))
        .Invoke(o, args);

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "scenarios")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("no repo root above the test binary");
    }
}
