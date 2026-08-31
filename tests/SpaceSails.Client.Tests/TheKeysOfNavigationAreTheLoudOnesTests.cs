using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #963 · #962 · #956 · #960 — THE NAV SCREEN, AS A PLACE AN EYE LANDS.
///
/// <para>Four of the owner's playtest complaints are one complaint. <i>"Here the smallest button is the
/// most important one… Plot. Plot should stand out among the buttons here. It is the heart of the
/// navigation process."</i> <i>"Why have the scope enable/disable button competing for attention at the
/// top of the screen — that functionality should stay at the scope position… This should help us highlight
/// the NAV button and the Add Burn buttons as the keys to navigation."</i> The toolbar was a flat row of
/// identically-dressed outline buttons in which the two that matter were indistinguishable from the six
/// that do not — and one of the six was a switch belonging to a window three inches away.</para>
///
/// <para>Half of that is a LOOK, so half of these guards read the shipping markup: which class a button
/// wears IS the behaviour under test, and a test that re-implemented the razor would prove nothing about
/// the screen the owner is looking at. The other half drive a real <see cref="Pages.Map"/> — the tooltip
/// that must name the live destination, the camera that must follow it, the stacking that must happen
/// only when two windows really do collide.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheKeysOfNavigationAreTheLoudOnesTests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    private const double AU = 1.495978707e11;

    private static readonly Lazy<SpaceSails.Contracts.ScenarioDefinition> Sol =
        new(() => ScenarioLoader.LoadFile(Path.Combine(RepoRoot(), "scenarios", "sol.json")));

    // ─────────────────────────── #963 · the scope's switch lives on the scope ───────────────────────────

    /// <summary>THE BUTTON THE OWNER SENT AWAY. "Why have the scope enable/disable button competing for
    /// attention at the top of the screen — that functionality should stay at the scope position."</summary>
    [Fact]
    public void TheNavToolbarNoLongerCarriesAScopeToggle()
    {
        string toolbar = Between(Razor("Map.razor"), "aria-label=\"Time warp controls\"", "SATURDAY-ANCHOR: toolbar");

        Assert.DoesNotContain("ToggleScope", toolbar);

        // …and the two buttons it was crowding are the loud ones now.
        Assert.Contains("map-key-action", toolbar);
        Assert.Contains("PlotButtonTip()", toolbar);
    }

    /// <summary>MINIMISE, NOT CLOSE — a closed window needs a switch somewhere else to reopen it, which is
    /// exactly the toolbar button that just left. The tile IS that switch, in the scope's own corner.</summary>
    [Fact]
    public void TheScopeMinimisesIntoATileInItsOwnCorner()
    {
        string razor = Razor("Map.razor");

        // #997 · the toggle is the OverlayShell's now — ONE minimise mechanism for this window and the
        // dossier, where there were two booleans and two markup shapes. What the page still owns is the
        // BINDING (its own tick must know whether the eyepiece is on the screen) and the tile's name. The
        // round trip itself — tuck, tile, restore, with what was in the window still in it — is pressed for
        // real through the renderer's event channel in TheOverlayShellIsOneMechanismTests.
        Assert.Contains("map-scope-tile", razor);
        Assert.Contains("@bind-Minimized=\"_scopeMinimized\"", razor);
        Assert.Contains("ScopeTileTargetName()", razor);

        // The tile sits where the window it replaces sat — same corner, or the switch has moved again.
        //
        // #997 · `right` stopped being a literal `0.75rem` — it now clears the desk-chip-strip's own
        // column (--desk-chip-strip-clearance), clamped so a phone-width screen never pushes the card off
        // its own left edge. The literal is gone, but the invariant this test is actually about — the
        // window and its tile share ONE right edge, so tucking never moves the corner — still holds, and
        // is asserted directly rather than by re-copying whatever expression happens to compute it today.
        string css = Css();
        string scopeRule = Between(css, ".map-scope {", "}");
        string tileRule = Between(css, ".map-scope-tile {", "}");

        Assert.Contains("bottom: 0.75rem;", scopeRule);
        Assert.Contains("bottom: 0.75rem;", tileRule);

        string scopeRight = Between(scopeRule, "right:", ";");
        string tileRight = Between(tileRule, "right:", ";");
        Assert.Equal(scopeRight, tileRight);
        Assert.Contains("desk-chip-strip-clearance", scopeRight);
    }

    /// <summary>The scope opens WIDE, as it always did — minimising is something the captain does, never
    /// the state he is handed.</summary>
    [Fact]
    public void TheScopeStartsExpanded() => Assert.False(Get<bool>(ParkedOffMars(), "_scopeMinimized"));

    /// <summary>A minimised scope does not paint. Its canvas survives (it must — a destroyed one leaves
    /// renderer.js holding a stale context, the M26 bug); only the drawing stops.</summary>
    [Fact]
    public void AMinimisedScopeIsNotDrawn()
    {
        string phase = Between(Client("Pages", "Map.Sim.Tick.cs"),
            "private void DrawTheScopeInsetIfItIsUp()", "_scopeView.Draw(");

        Assert.Contains("!_scopeMinimized", phase);
    }

    /// <summary>A tile reading only "🔭" is a mystery button. It names what is in the eyepiece, by the same
    /// priority the scope itself locks on: a manual pick, a selected contact, the destination, then
    /// whatever is nearest.</summary>
    [Fact]
    public void TheTileNamesWhatIsInTheEyepiece()
    {
        Pages.Map map = ParkedOffMars();

        // Nothing chosen: it falls back to the nearest body the frame's own sweep found.
        Invoke(map, "UpdateNearestBody");
        Assert.Equal(Get<CelestialBody?>(map, "_nearestBody")!.Name, Invoke(map, "ScopeTileTargetName"));

        // A destination outranks the nearest…
        Set(map, "_destinationBodyId", "jupiter");
        Assert.Equal("Jupiter", Invoke(map, "ScopeTileTargetName"));

        // …and a manual pick outranks the destination.
        Set(map, "_scopeManualId", "saturn");
        Assert.Equal("Saturn", Invoke(map, "ScopeTileTargetName"));

        // A name that resolves to nothing out there never reaches the tile.
        Set(map, "_scopeManualId", "no-such-rock");
        Assert.Equal("Jupiter", Invoke(map, "ScopeTileTargetName"));
    }

    // ─────────────────────────── #962 · Plot is the heart of navigation ───────────────────────────

    /// <summary>THE LOOK IS THE FIX. Plot and + Add burn wear a SOLID fill and the key-action dress; their
    /// neighbours stay outlines. A row where everything shouts is a row where nothing does — which is
    /// precisely the row the owner photographed.</summary>
    [Fact]
    public void PlotAndAddBurnAreDressedAsTheKeysAndTheirNeighboursAreNot()
    {
        string razor = Razor("Map.razor");

        string plot = ButtonAround(razor, "@onclick=\"TogglePlotMode\"");
        Assert.Contains("map-key-action", plot);
        Assert.Contains("btn-warning", plot);
        Assert.DoesNotContain("btn-outline-light", plot);

        string addBurn = ButtonAround(razor, "@onclick=\"AddBurnAtScrub\"");
        Assert.Contains("map-key-action", addBurn);
        Assert.Contains("btn-warning", addBurn);
        Assert.DoesNotContain("btn-outline-light", addBurn);

        // The control group: Follow Ship is an ordinary toolbar button and must stay one.
        string follow = ButtonAround(razor, "@onclick=\"ToggleFollow\"");
        Assert.DoesNotContain("map-key-action", follow);
        Assert.Contains("btn-outline-light", follow);
    }

    /// <summary>#963 — "We should have hover-on explanation of what we are plotting, when we press plot."
    /// WHAT we are plotting: so when there is a destination the tooltip names it, read off live state
    /// rather than being a fixed sentence that is true of nothing in particular.</summary>
    [Fact]
    public void ThePlotHoverSaysWhatPressingItWouldPlotRightNow()
    {
        Pages.Map map = ParkedOffMars();

        string noTarget = (string)Invoke(map, "PlotButtonTip")!;
        Assert.Contains("Plot a course", noTarget);
        Assert.Contains("destination", noTarget);   // it says how to aim it
        Assert.DoesNotContain("Jupiter", noTarget);

        Set(map, "_destinationBodyId", "jupiter");
        string aimed = (string)Invoke(map, "PlotButtonTip")!;
        Assert.Contains("Jupiter", aimed);
        Assert.Contains("scrub", aimed);
        Assert.Contains("burn", aimed);

        // In plot mode the button IS the way back, and says so instead of repeating the sales pitch.
        Set(map, "PlotMode", true);
        string flying = (string)Invoke(map, "PlotButtonTip")!;
        Assert.Contains("live", flying);
        Assert.DoesNotContain("Jupiter", flying);
    }

    // ─────────────────────────── #956 · follow the destination ───────────────────────────

    /// <summary>The camera rides the nav target. Owner: "Let's have a follow nav destination option here in
    /// addition to follow ship."</summary>
    [Fact]
    public void FollowDestPutsTheCameraOnTheNavigationTarget()
    {
        Pages.Map map = ParkedOffMars();
        Set(map, "_destinationBodyId", "jupiter");
        Set(map, "SimTime", 12_345.0);
        Invoke(map, "ToggleFollowDest");

        var followed = (Vector2d?)Invoke(map, "FollowedDestinationPosition");
        Vector2d jupiter = Get<ICelestialEphemeris>(map, "_ephemeris").Position("jupiter", 12_345.0);

        Assert.NotNull(followed);
        Assert.Equal(jupiter.X, followed!.Value.X, 3);
        Assert.Equal(jupiter.Y, followed.Value.Y, 3);
    }

    /// <summary>THE FOLLOW READS THE PLAN, IT DOES NOT REMEMBER IT. This repo's named bug class is two
    /// answers to one question: a second copy of a fact, taken once and then quietly wrong. A follow-dest
    /// that latched the body id (or worse, the POSITION) at the moment the button went down would satisfy
    /// every other guard on this feature and still strand the camera over the place we used to be going.
    ///
    /// <para>So both halves are pressed here, through the plan's own door rather than the field: change the
    /// destination while the follow is engaged and the camera must already be over the new body with no
    /// second press; let sim time run and the followed point must have travelled with that body's orbit.
    /// One source — <c>_destinationBodyId</c> read live, positioned live off the ephemeris — or this
    /// fails.</para></summary>
    [Fact]
    public void FollowDestRidesWhereverThePlanSaysWeAreGoingNow()
    {
        Pages.Map map = ParkedOffMars();
        var ephemeris = Get<ICelestialEphemeris>(map, "_ephemeris");
        Set(map, "SimTime", 12_345.0);

        Invoke(map, "SetDestination", "jupiter");
        Invoke(map, "ToggleFollowDest");
        Assert.True(Get<bool>(map, "_followDest"));

        // The plan changes its mind. Nobody presses Follow dest again.
        Invoke(map, "SetDestination", "saturn");

        var moved = (Vector2d?)Invoke(map, "FollowedDestinationPosition");
        Vector2d saturn = ephemeris.Position("saturn", 12_345.0);
        Vector2d jupiter = ephemeris.Position("jupiter", 12_345.0);

        Assert.NotNull(moved);
        Assert.Equal(saturn.X, moved!.Value.X, 3);
        Assert.Equal(saturn.Y, moved.Value.Y, 3);
        // …and it really is somewhere else, or the assertion above could not tell the two apart.
        Assert.True((saturn - jupiter).Length > AU, "the bench picked two bodies the camera cannot distinguish");

        // The other half of one-source: the body it rides is MOVING. A position captured once passes
        // everything above and freezes here.
        Set(map, "SimTime", 12_345.0 + (400.0 * 86_400.0));
        var later = (Vector2d?)Invoke(map, "FollowedDestinationPosition");
        Vector2d saturnLater = ephemeris.Position("saturn", 12_345.0 + (400.0 * 86_400.0));

        Assert.NotNull(later);
        Assert.Equal(saturnLater.X, later!.Value.X, 3);
        Assert.Equal(saturnLater.Y, later.Value.Y, 3);
        Assert.True((saturnLater - saturn).Length > AU / 10, "Saturn did not move far enough to prove the point");
    }

    /// <summary>ONE CAMERA, ONE THING TO FOLLOW. Two live follows are a fight over the same camera, and the
    /// frame would be told to centre on two places at once.</summary>
    [Fact]
    public void FollowShipAndFollowDestAreMutuallyExclusive()
    {
        Pages.Map map = ParkedOffMars();
        Set(map, "_destinationBodyId", "jupiter");

        Invoke(map, "ToggleFollowDest");
        Assert.True(Get<bool>(map, "_followDest"));
        Assert.False(Get<bool>(map, "FollowShip"));

        Invoke(map, "ToggleFollow");
        Assert.True(Get<bool>(map, "FollowShip"));
        Assert.False(Get<bool>(map, "_followDest"));
        Assert.Null(Invoke(map, "FollowedDestinationPosition"));
    }

    /// <summary>With no destination there is nothing to ride, so the button is offered greyed WITH the
    /// reason (#212 — a control that vanishes teaches nothing) and refuses to engage.</summary>
    [Fact]
    public void FollowDestIsRefusedAndExplainedWhenThereIsNoDestination()
    {
        Pages.Map map = ParkedOffMars();

        Assert.False(Prop<bool>(map, "CanFollowDestination"));
        Invoke(map, "ToggleFollowDest");
        Assert.False(Get<bool>(map, "_followDest"));
        Assert.Contains("No navigation target", (string)Invoke(map, "FollowDestTip")!);

        Set(map, "_destinationBodyId", "jupiter");
        Assert.True(Prop<bool>(map, "CanFollowDestination"));
        Assert.Contains("Jupiter", (string)Invoke(map, "FollowDestTip")!);
    }

    /// <summary>A hand on the map outranks both follows — the rule a manual pan has always had for Follow
    /// Ship, or the camera snaps back the next frame and the drag reads as broken.</summary>
    [Fact]
    public void AHandOnTheMapStandsBothFollowsDown()
    {
        string pan = Between(Client("Pages", "Map.Sim.Controls.cs"), "manual pan disengages follow-ship", "\n\n");

        Assert.Contains("_followDest = false", pan);
    }

    // ─────────────────────────── #960 · two windows, one bottom centre ───────────────────────────

    /// <summary>The dossier only moves when there is something to move OFF: it rides above the
    /// navigation-target panel exactly when that panel is up, and keeps its usual place otherwise.</summary>
    [Fact]
    public void TheDossierOnlyStacksWhenTheNavPanelIsActuallyThere()
    {
        Pages.Map map = ParkedOffMars();

        Assert.False(Prop<bool>(map, "DossierIsStacked"));

        Set(map, "PlotMode", true);
        Assert.False(Prop<bool>(map, "DossierIsStacked"));      // a plot with no destination has no panel

        Set(map, "_destinationBodyId", "jupiter");
        Assert.True(Prop<bool>(map, "DossierIsStacked"));

        // The nav panel is a Nav-desk box; on the Sensors desk the dossier has the corner to itself again.
        Set(map, "_activeDesk", Pages.ShipDesk.Sensors);
        Assert.False(Prop<bool>(map, "DossierIsStacked"));
    }

    /// <summary>THE STACK MUST ACTUALLY CLEAR. The raised dossier sits above the nav panel's CAPPED height
    /// — the cap is what makes the offset a known number rather than a measurement CSS cannot take. Let
    /// either number drift and the two cards overlap again exactly as they do in the screenshot.</summary>
    [Fact]
    public void TheRaisedDossierClearsTheCappedNavPanel()
    {
        string css = Css();
        string panel = Between(css, ".map-dest-panel {", "}");

        double panelBottom = Rem(panel, "bottom:");
        double panelCap = Rem(panel, "max-height:");
        double raised = Rem(Between(css, ".map-dossier-raised {", "}"), "bottom:");

        Assert.True(raised >= panelBottom + panelCap,
            $"the raised dossier starts at {raised}rem but the nav panel reaches {panelBottom + panelCap}rem — " +
            "they overlap, which is the bug in the owner's screenshot.");

        // The cap only works if the panel is allowed to scroll inside it; otherwise it just clips its own
        // buttons off, trading one hidden control for another.
        Assert.Contains("overflow-y: auto", panel);
    }

    /// <summary>…and either card can be tucked away. Owner: "option to minimize a window into a sugarcube
    /// tile and back would avoid the moving-windows can of worms." No dragging — that WAS the can of
    /// worms, and the fix must not quietly become it.</summary>
    [Fact]
    public void TheDossierCanBeMinimisedIntoATileAndBroughtBack()
    {
        // #997 · this card and the scope now share ONE mechanism instead of resembling each other: the
        // toggle that used to sit beside this field is the OverlayShell's, and the round trip is pressed
        // for real — through the renderer's own event channel, with something alive inside the shell to
        // prove nothing was destroyed on the way to the tile — in
        // TheOverlayShellIsOneMechanismTests.TheTileRoundTripKeepsWhatWasInTheWindow. What stays this
        // card's own business, and is asserted here, is that it STARTS open and that it is wired to that
        // mechanism rather than to a second one somebody wrote for it.
        Assert.False(Get<bool>(ParkedOffMars(), "_dossierMinimized"));

        string razor = Razor("Map.razor");
        Assert.Contains("map-dossier-tile", razor);
        Assert.Contains("@bind-Minimized=\"_dossierMinimized\"", razor);
        Assert.Contains("Dismiss=\"OverlayDismiss.Minimize\"", razor);
        Assert.DoesNotContain("draggable", Between(razor, "map-dossier bg-dark", "war room"));
    }

    // ─────────────────────────── #963 · the small symbol next to Ganymede ───────────────────────────

    /// <summary>"Is there ground visitable at these places… what is the small symbol next to ganymede… it
    /// should have some kind of text pop-up?" It does now, on the row where he met it.</summary>
    [Fact]
    public void ALandableRowSpellsOutWhatTheMarkMeans()
    {
        MethodInfo tip = typeof(Pages.Map).GetMethod("NavSearchRowTip", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("no NavSearchRowTip on Map — this bench has drifted");

        Type rowType = typeof(Pages.Map).GetNestedType("NavSearchRow", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("no NavSearchRow on Map — this bench has drifted");

        object landable = Activator.CreateInstance(rowType, 'B', "ganymede", "Ganymede",
            "body · 🛬 landable — out of shuttle reach", "🪐", false)!;
        object plain = Activator.CreateInstance(rowType, 'B', "mars", "Mars", "body", "🪐", false)!;

        string landableTip = (string)tip.Invoke(null, [landable])!;
        Assert.Contains("Ganymede", landableTip);
        Assert.Contains("surface you can go down to", landableTip);
        Assert.Contains("shuttle", landableTip);

        Assert.DoesNotContain("surface you can go down to", (string)tip.Invoke(null, [plain])!);
    }

    /// <summary>The other place the glyph is drawn is a CANVAS, where no tooltip can live — so the layer
    /// that draws it carries the sentence, and the panel actually renders that sentence as a title.</summary>
    [Fact]
    public void TheLandableLayerRowHoversItsOwnExplanation()
    {
        string leafRow = Between(Razor("Map.razor"), "@foreach (MapLayerTree.Leaf leaf in g.Leaves)", "</label>");

        Assert.Contains("l.Hint", leafRow);
        Assert.Contains("title=", leafRow);
    }

    // ─────────────────────────── the bench ───────────────────────────

    /// <summary>A real Map parked off Mars in the sol scenario — the same stand the nearest-body audit
    /// takes, because the scope tile and the follow-dest camera both read live world state.</summary>
    private static Pages.Map ParkedOffMars()
    {
        var map = new Pages.Map();
        typeof(ComponentBase).GetField("_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(map, true);

        ICelestialEphemeris ephemeris = CircularOrbitEphemeris.FromScenario(Sol.Value);
        Set(map, "_scenarioName", Sol.Value.Name);
        Set(map, "_ephemeris", ephemeris);
        Set(map, "_simulator", new Simulator(ephemeris, timeStepSeconds: 1.0));

        Vector2d mars = ephemeris.Position("mars", 0);
        Set(map, "_ship", new ShipState(mars + mars / mars.Length * (0.16 * AU), Vector2d.Zero, 0.0));
        return map;
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "scenarios")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("no repo root above the test binary");
    }

    /// <summary>Source, with the line endings normalised. Git hands these files out as CRLF on Windows and
    /// LF on the CI runner, so a guard that matches across a blank line passed on one machine and failed on
    /// the other — a bench that cannot tell pass from fail rather than a finding.</summary>
    private static string Client(params string[] parts) =>
        File.ReadAllText(Path.Combine([RepoRoot(), "src", "SpaceSails.Client", .. parts]))
            .Replace("\r\n", "\n");

    private static string Razor(string file) => Client("Pages", file);

    private static string Css() => Client("Pages", "Map.razor.css");

    private static void Set(object o, string field, object? value) =>
        (o.GetType().GetField(field, Hidden)
         ?? throw new InvalidOperationException($"no field {field} on Map — this bench has drifted"))
        .SetValue(o, value);

    private static T Get<T>(object o, string field) =>
        (T)(o.GetType().GetField(field, Hidden)
            ?? throw new InvalidOperationException($"no field {field} on Map — this bench has drifted"))
        .GetValue(o)!;

    private static T Prop<T>(object o, string name) =>
        (T)(o.GetType().GetProperty(name, Hidden)
            ?? throw new InvalidOperationException($"no property {name} on Map — this bench has drifted"))
        .GetValue(o)!;

    private static object? Invoke(object o, string method, params object?[] args) =>
        (o.GetType().GetMethod(method, Hidden)
         ?? throw new InvalidOperationException($"no method {method} on Map — this bench has drifted"))
        .Invoke(o, args);

    private static string Between(string text, string start, string end)
    {
        int i = text.IndexOf(start, StringComparison.Ordinal);
        Assert.True(i >= 0, $"\"{start}\" is not in the source any more — this bench has drifted.");
        int j = text.IndexOf(end, i + start.Length, StringComparison.Ordinal);
        Assert.True(j >= 0, $"\"{end}\" no longer follows \"{start}\" — this bench has drifted.");
        return text[i..j];
    }

    /// <summary>The whole opening tag of the button carrying a given attribute — so a guard about a
    /// button's dress reads THAT button's classes and not a neighbour's.</summary>
    private static string ButtonAround(string razor, string marker)
    {
        int at = razor.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(at >= 0, $"\"{marker}\" is not in Map.razor any more — this bench has drifted.");
        int open = razor.LastIndexOf("<button", at, StringComparison.Ordinal);
        Assert.True(open >= 0, $"\"{marker}\" is not inside a button any more — this bench has drifted.");
        return razor[open..(at + marker.Length)];
    }

    /// <summary>The first rem value of a named property inside a CSS rule.</summary>
    private static double Rem(string rule, string property)
    {
        int i = rule.IndexOf(property, StringComparison.Ordinal);
        Assert.True(i >= 0, $"no {property} in this rule — this bench has drifted.");
        string value = new(rule[(i + property.Length)..].TakeWhile(c => c != ';').ToArray());
        string number = new(value.Trim().TakeWhile(c => char.IsDigit(c) || c == '.').ToArray());
        Assert.True(number.Length > 0,
            $"{property} is not a plain rem value (\"{value.Trim()}\") — this bench has drifted.");
        return double.Parse(number, CultureInfo.InvariantCulture);
    }
}
