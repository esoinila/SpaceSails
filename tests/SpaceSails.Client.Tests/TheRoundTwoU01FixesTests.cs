using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Contracts;
using SpaceSails.Core;
using Xunit;
using Pages = SpaceSails.Client.Pages;

namespace SpaceSails.Client.Tests;

/// <summary>
/// ROUND 2 · TEST USER U01 (Mira) · issue #1415. One behavioural guard per fixed finding that lives in the
/// client; the Core halves are in <c>TheRoundTwoU01CoreFixesTests</c>. Every guard here was watched RED with
/// its fix reverted (see the lane's pull request for the log).
/// </summary>
public sealed class TheRoundTwoU01FixesTests
{
    private const BindingFlags Hidden = TestTree.AnythingOnAnInstance;

    private static readonly Lazy<ICelestialEphemeris> Sol =
        new(() => CircularOrbitEphemeris.FromScenario(TestTree.Sol));

    private static Pages.Map Ashore(string standingOn = "cinder-roost")
    {
        var map = new Pages.Map();
        typeof(ComponentBase).GetField("_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(map, true);
        Set(map, "_scenarioName", "Sol");
        Set(map, "_ephemeris", Sol.Value);
        Set(map, "_ship", Invoke(map, "InitializeShipState")!);
        Set(map, "_dockedHavenId", standingOn);
        return map;
    }

    private static void Set(object o, string field, object? value) =>
        o.GetType().GetField(field, Hidden)!.SetValue(o, value);

    private static object? Field(object o, string field) =>
        o.GetType().GetField(field, Hidden)!.GetValue(o);

    private static object? Invoke(Pages.Map map, string method, params object?[] args)
    {
        MethodInfo? call = typeof(Pages.Map).GetMethod(method, Hidden);
        Assert.True(call is not null, $"the component has no `{method}`.");
        return call!.Invoke(map, args);
    }

    private static T Static<T>(string method, params object?[] args)
    {
        MethodInfo? call = typeof(Pages.Map).GetMethod(method,
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.True(call is not null, $"the component has no static `{method}`.");
        return (T)call!.Invoke(null, args)!;
    }

    // ── U01-F08 · a named contact is named in their own reply ────────────────────────────────────────────

    [Theory]
    [InlineData("MADAM COIL", "Madam Coil")]
    [InlineData("GILT-EYE", "Gilt-Eye")]
    [InlineData("THE FIXER", "The Fixer")]
    [InlineData("ONE-EYE SILAS", "One-Eye Silas")]
    [InlineData("A STRANGER AT THE END", "The stranger")]
    public void AContactAnswersToTheNameOnTheirPlate(string plate, string said) =>
        Assert.Equal(said, Static<string>("BarSpeaker", plate));

    [Fact]
    public void DecliningMadamCoilIsSaidInHerName()
    {
        Pages.Map map = Ashore();
        Set(map, "_pendingOffer", new Pages.Map.Quest("cargo-1", Pages.Map.QuestKind.CargoRun, "MADAM COIL",
            "x", "Selene Gate", "A job", "Blurb", 500, "earth"));
        Invoke(map, "DeclineOffer");
        var pulse = (PulseSlot)Field(map, "_pulse")!;
        Assert.Contains("Madam Coil turns back to their drink", pulse.Message);
        Assert.DoesNotContain("stranger", pulse.Message);
    }

    // ── U01-F16 · the mid-flight parenthetical fits the run ──────────────────────────────────────────────

    private static NpcShip Hauler(double departed, double arrives) => new(
        "npc-1", "Milk Run", "Compute cores", "luna", "venus", RoutePersonality.Economical,
        departed, departed, default, ManeuverPlan.Empty, arrives, 5, 0, true);

    [Fact]
    public void AnInnerRunIsNotSaidToTakeYears()
    {
        const double day = 86400.0;
        Assert.DoesNotContain("years", Static<string>("MidFlightNote", Hauler(0, 40 * day)));
        Assert.DoesNotContain("years", Static<string>("MidFlightNote", Hauler(0, 200 * day)));
        Assert.Contains("weeks", Static<string>("MidFlightNote", Hauler(0, 40 * day)));
        Assert.Contains("years", Static<string>("MidFlightNote", Hauler(0, 900 * day)));
    }

    // ── U01-F18 · a boot-time line gets its dwell from the first frame it can be seen on ─────────────────

    [Fact]
    public void ABootTimeLineIsNotSpentBeforeTheOverlayLifts()
    {
        // Written at t = 0 (no frame yet); the overlay lifts 20 s later.
        PulseSlot written = PulseSlot.Empty.Write("A toast naming the corner of the bar the oracle keeps.", PulseRank.Status, 0);
        Assert.Null(written.Expire(20_000).Message); // the bug: wiped by the first Expire

        PulseSlot restarted = Static<PulseSlot>("TheBootPulseStartsItsClockNow", written, 20_000.0);
        Assert.NotNull(restarted.Expire(20_500).Message);
        Assert.Null(restarted.Expire(20_000 + PulseSlot.MaxDwellMs + 1).Message);
    }

    // ── U01-F11 · a case taken at a bar seat is not "found on the ground" ───────────────────────────────

    [Fact]
    public void TheFindersCaseNamesItsGiverAndNotTheGround()
    {
        var caseLine = new FieldFinding("Selene Gate · bar",
            [new FieldNote("Three names on one hull.", 86400 * 3, "Selene Gate · bar", "🕵")], 86400 * 3);
        string prov = Static<string>("FieldProvenance", caseLine);
        Assert.Contains(FinderCase.DisplayName, prov);
        Assert.DoesNotContain("found on the ground", prov);

        var dug = new FieldFinding("Miranda · site",
            [new FieldNote("A paper.", 0, "Miranda · site", "📄")], 0);
        Assert.StartsWith("found on the ground", Static<string>("FieldProvenance", dug));
    }

    // ── U01-F04 · the away gig and the case are on the books, each under its own heading with a NEXT ────

    [Fact]
    public void AnAcceptedGigAndACaseAreFiledInTheLedger()
    {
        var plan = new ExpeditionPlan(ExpeditionFlavor.Science, ExpeditionSiteKind.CrashedHull,
            "expedition-site-wreck", "Object 2-9-1 wreck", 4, 900, 0);
        FinderCase.Progress taken = FinderCase.Progress.Fresh with { Taken = true };
        FinderCase.Case theCase = default;

        var rows = Static<IReadOnlyList<Pages.Stations.Captain.QuestItem>>("OtherWorkCards", plan, theCase, taken);

        Assert.Equal(2, rows.Count);
        Assert.All(rows, r => Assert.NotNull(r.Heading));
        Assert.All(rows, r => Assert.False(string.IsNullOrWhiteSpace(r.NextAction)));
        Assert.Contains("Object 2-9-1 wreck", rows[0].NextAction);
        Assert.NotEqual(rows[0].Heading, rows[1].Heading);

        // A case not yet taken is not on the books; nor is a gig nobody accepted.
        Assert.Single(Static<IReadOnlyList<Pages.Stations.Captain.QuestItem>>(
            "OtherWorkCards", null, theCase, taken));
        Assert.Empty(Static<IReadOnlyList<Pages.Stations.Captain.QuestItem>>(
            "OtherWorkCards", null, theCase, FinderCase.Progress.Fresh));
    }

    [Fact]
    public void TheCaptainsRowsOfAMapWithAGigIncludeTheGig()
    {
        Pages.Map map = Ashore();
        Set(map, "_expedition", new ExpeditionPlan(ExpeditionFlavor.Science, ExpeditionSiteKind.CrashedHull,
            "expedition-site-wreck", "Object 2-9-1 wreck", 4, 900, 0));
        var cards = (Pages.Stations.Captain.QuestItem[])Invoke(map, "QuestCards")!;
        Assert.Contains(cards, c => c.Heading is not null && c.Title.Contains("Object 2-9-1 wreck"));
    }

    // ── U01-F05 · I opens the satchel wherever the captain stands on foot ───────────────────────────────

    [Fact]
    public void IOpensTheSatchelOnAStationDeckWithNoGroundUnderFoot()
    {
        Pages.Map map = Ashore();
        Set(map, "_deckMode", true);
        Assert.Null(Field(map, "_surface"));
        Assert.False((bool)Field(map, "_showSatchel")!);

        Assert.True((bool)Invoke(map, "HandleDeckKey", "i", false)!);
        Assert.True((bool)Field(map, "_showSatchel")!, "I did nothing on a station deck.");

        Assert.True((bool)Invoke(map, "HandleDeckKey", "I", true)!);
        Assert.False((bool)Field(map, "_showSatchel")!, "the same key does not shut it again (#688).");
    }

    // ── U01-F20 · one name for the expedition rock ───────────────────────────────────────────────────────

    [Fact]
    public void TheOrdersNameTheRockByItsLiveName()
    {
        var order = new ShipMission(MissionKind.FlyTo, DestinationBodyId: "expedition-site-wreck");
        Assert.Contains("Object 2-9-1 wreck",
            order.Describe(id => id == "expedition-site-wreck" ? "Object 2-9-1 wreck" : id));
        // An id the caller cannot name falls back to the humanized id, never to nothing.
        Assert.Contains("Expedition Site Wreck", order.Describe(id => id));
        Assert.Contains("Expedition Site Wreck", order.Describe());
    }

    // ── U01-F03 · the crash card's Reload really reloads ────────────────────────────────────────────────

    [Fact]
    public void TheCrashCardsReloadIsARealBrowserReloadNotARouterLink()
    {
        // An <a href="."> inside the Router is swallowed as a client-side navigation to "/", which is what left
        // the error card standing under a changed URL. The control must be one the router cannot intercept.
        string app = System.IO.File.ReadAllText(System.IO.Path.Combine(TestTree.RepoRoot(), "src", "SpaceSails.Client", "App.razor"));
        int error = app.IndexOf("The voyage hit an error.", StringComparison.Ordinal);
        Assert.True(error > 0);
        string card = app[error..app.IndexOf("</ErrorContent>", error, StringComparison.Ordinal)];
        Assert.Contains("location.reload()", card, StringComparison.Ordinal);
        Assert.DoesNotContain("href=\".\"", card, StringComparison.Ordinal);
    }
}
