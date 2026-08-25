using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SpaceSails.Client.Pages;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #953 · <b>THE SHIP LANES ARE ARCHIVED.</b> Owner's ruling, 2026-08-25: <i>"we have never used them to find
/// anything."</i> It closes a thread #971 had only half-pulled — that pass hid the overlay by default, after
/// the owner opened his sensors desk onto a sky <i>"covered in faint lines with no intersection"</i>.
///
/// <para><b>What archived means, and what it deliberately does not.</b> The DISPLAY is gone: no Trade lanes
/// row in the Layers panel, no corridor quads, no lane name labels, and none of it left commented out. The
/// GEOMETRY stays compiled, because it turned out not to be decoration —
/// <see cref="TheLaneSweepsAreKeptBecauseTheyPutRealContactsInTheLedger"/> carries that verdict and its
/// evidence. The decision itself is written on one flag, <see cref="ShipLanes.Archived"/>, so a redesign has
/// one thing to read and one thing to flip.</para>
///
/// <h3>Why the drawing guard paints instead of grepping</h3>
///
/// <para>A guard that searched the client for the word <c>DrawTradeCorridors</c> would pass the day somebody
/// wrote the same quads under another name. So <see cref="NoWorldInTheMatrixDrawsALane"/> boots each of the
/// five worlds in <see cref="EveryDeskBootsTests"/>'s own matrix, hands the shipping page a pen, asks it to
/// paint the map frame, and reads the ink: not one of the lane names <see cref="TradeCorridors"/> would
/// generate for that world may appear on the glass. It also asserts the pen was USED — a frame that drew
/// nothing at all would otherwise satisfy "no lane was drawn" perfectly, which is this repository's fifth
/// named bug class.</para>
///
/// <para><b>Red proof</b> (quoted in the pull request): put <c>DrawTradeCorridors</c> back on the layer stack
/// and this fails naming the lanes it found in the ink, world by world.</para>
///
/// <h3>The regression the archive uncovered</h3>
///
/// <para>The lane cache used to be filled INSIDE the corridor draw. Once #971 hid that layer by default, the
/// list <c>NearLaneFor</c> searches was empty on every desk — so the open-sky menu's two lane sweeps, which
/// #971 had just moved there, could not appear at all unless a captain first ticked a decoration layer he was
/// never shown. <see cref="ALaneIsFoundWithoutAnythingHavingBeenDrawn"/> is that fix's guard, and it fails on
/// the code as it shipped.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheShipLanesAreArchivedTests
{
    private const int WidthPx = 1200, HeightPx = 700;

    // ─────────────────────────── (a) nothing on the glass ───────────────────────────

    /// <summary>
    /// NOT ONE LANE IS PAINTED, IN ANY WORLD THE #986 MATRIX KNOWS.
    ///
    /// <para>Every layer is switched ON first. That matters: if the lanes were merely defaulted off, this
    /// would be a guard that could not tell an archived feature from a hidden one — and hidden is precisely
    /// the state #971 left and this ruling replaced.</para>
    /// </summary>
    [Fact]
    public async Task NoWorldInTheMatrixDrawsALane()
    {
        var wrong = new List<string>();

        foreach (string url in EveryDeskBootsTests.EveryWorldUrl())
        {
            using DeskBench bench = await DeskBench.BootAsync(url);

            // The page says this layer is NOT hidden — asked of the page's own resolution, so nothing here
            // can pass by the lanes merely being switched off, which is the state #971 left and this ruling
            // replaced.
            Assert.True((bool)bench.Call("LayerVisible", "routes.lanes")!,
                $"{url}: the page still hides a lane layer, so a frame with no lanes in it proves nothing");

            (IReadOnlyList<string> words, int marks) = PaintTheSky(bench);

            if (words.Count == 0 || marks == 0)
            {
                wrong.Add($"{url}: the map frame laid {words.Count} label(s) and {marks} mark(s) — this guard "
                          + "cannot tell a sky with no lanes on it from a frame that was never painted");
                continue;
            }

            var ephemeris = (ICelestialEphemeris)bench.Field("_ephemeris")!;
            double simTime = (double)bench.Field("SimTime")!;
            foreach (CorridorRegion lane in TradeCorridors.Regions(ephemeris, simTime))
            {
                if (words.Contains(lane.Name, StringComparer.Ordinal))
                {
                    wrong.Add($"{url}: \"{lane.Name}\" is written on the sky. #953 archived the ship-lane "
                              + "display — \"we have never used them to find anything.\"");
                }
            }
        }

        Assert.True(wrong.Count == 0, "#953 · " + string.Join("\n  - ", wrong));
    }

    /// <summary>
    /// Paint the solar map through the SHIPPING renderer and read the ink back out of it.
    ///
    /// <para><see cref="CanvasRenderer"/> buffers a whole frame in memory and only touches JS in
    /// <c>EndFrame</c>, so off-browser the frame is drawn for real and the flush at the end is the same
    /// documented browser gate <see cref="DeskBench"/> already lives with. Every label and every shape the
    /// map laid is sitting in the renderer when it throws — which is why this reads the real encoder rather
    /// than a stand-in pen that could quietly disagree with it.</para>
    ///
    /// <para>Returns the text drawn, and how many floats of shape data were encoded.</para>
    /// </summary>
    private static (IReadOnlyList<string> Words, int Marks) PaintTheSky(DeskBench bench)
    {
        var pen = new CanvasRenderer("lane-archive-bench");
        bench.Poke("_renderer", pen);
        bench.Poke("_viewportWidth", WidthPx);
        bench.Poke("_viewportHeight", HeightPx);

        try
        {
            bench.Call("PaintTheMapFrame");
        }
        catch (TargetInvocationException ex)
            when (ex.InnerException is PlatformNotSupportedException gate
                  && gate.Message.Contains("System.Runtime.InteropServices.JavaScript", StringComparison.Ordinal))
        {
            // EndFrame's flush — the horizon every off-browser bench in this project shares.
        }

        var texts = (List<TextCommand>)typeof(CanvasRenderer)
            .GetField("_texts", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(pen)!;
        int marks = (int)typeof(CanvasRenderer)
            .GetField("_length", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(pen)!;

        return ([.. texts.Select(t => t.Text)], marks);
    }

    // ─────────────────────────── (b) nothing in the panel ───────────────────────────

    /// <summary>The Layers panel does not offer a row that turns nothing on. The rest of the Routes family is
    /// asserted present in the same breath, so a tree that lost its whole Routes group would not pass this by
    /// being empty.</summary>
    [Fact]
    public void TheLayerTreeNoLongerOffersTradeLanes()
    {
        Assert.True(ShipLanes.Archived);

        MapLayerTree.Group routes = MapLayerTree.Groups.Single(g => g.Key == "routes");
        Assert.Equal(["routes.plan", "routes.rails"], routes.Leaves.Select(l => l.Key));
        Assert.DoesNotContain("routes.lanes", MapLayerTree.AllLeafKeys);
        Assert.DoesNotContain(
            MapLayerTree.Groups.SelectMany(g => g.Leaves),
            l => l.Label.Contains("lane", StringComparison.OrdinalIgnoreCase));

        // Nothing is hidden by default any more: there is no lane key left to hide, and the desks that used
        // to open with one missing layer now open on the whole tree.
        Assert.Empty(MapLayerTree.DefaultHidden(isSensorsDesk: true));
        Assert.Empty(MapLayerTree.DefaultHidden(isSensorsDesk: false));
    }

    /// <summary>THE CLIENT CARRIES NO SLEEPING LANE DRAW. Archive means deleted here, not commented out: a
    /// commented-out block is neither running code nor a decision, and the decision has a flag of its own.</summary>
    [Fact]
    public void NoClientFileStillDrawsOrGatesALane()
    {
        var leftovers = new List<string>();

        string client = Path.Combine(RepoRoot(), "src", "SpaceSails.Client");
        int scanned = 0;

        foreach (string file in Directory.EnumerateFiles(client, "*.*", SearchOption.AllDirectories)
                 .Where(f => f.EndsWith(".cs", StringComparison.Ordinal)
                             || f.EndsWith(".razor", StringComparison.Ordinal))
                 // Written source only. A build artefact under obj/ is generated FROM the files below, so
                 // reading it says nothing new — and a stale one from an older build would redden this guard
                 // for a reason that has nothing to do with the code.
                 .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                                         StringComparison.Ordinal)
                             && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
                                            StringComparison.Ordinal)))
        {
            scanned++;
            string text = File.ReadAllText(file);
            foreach (string gone in new[] { "DrawTradeCorridors", "routes.lanes", "CorridorFillColor" })
            {
                if (text.Contains(gone, StringComparison.Ordinal))
                {
                    leftovers.Add($"{Path.GetFileName(file)} still mentions {gone}");
                }
            }
        }

        // …and the sweep really walked the client. A filter that matched nothing would report no leftovers
        // just as cheerfully as a clean tree.
        Assert.True(scanned > 100, $"only {scanned} client source files were read — this sweep has shrunk");

        Assert.True(leftovers.Count == 0,
            "#953 · the lane display was archived, so nothing in the client should still draw it or gate it:\n  - "
            + string.Join("\n  - ", leftovers));
    }

    // ─────────────────────────── (c) the verdict on the sweeps ───────────────────────────

    /// <summary>
    /// THE TWO LANE SWEEPS ARE KEPT, AND THIS IS WHY — investigated rather than assumed.
    ///
    /// <para>The open-sky menu's <c>📡 Sweep the … lane</c> and <c>🔁 Standing watch</c> are not decoration
    /// wearing a lane's name. They book a <see cref="SensorTaskKind.CorridorSweep"/>; the tracking post aims
    /// the telescope with <see cref="TradeCorridors.SweepJobFor"/> when the pass completes, and runs a real
    /// detection sweep whose hits go into the contact ledger. So the lane geometry is live gameplay data and
    /// stays compiled, while the pixels it used to be drawn as do not.</para>
    ///
    /// <para>Asserted by DOING it, not by reading the wiring: a ship is put in the middle of the Earth–Mars
    /// lane, the lane's own sweep job is aimed from a vantage well off it, and the sweep comes back holding
    /// her. Move the ship a lane-length sideways and the same sweep misses — otherwise this would be a wedge
    /// that selects the whole sky and could not tell a hit from a miss.</para>
    /// </summary>
    [Fact]
    public void TheLaneSweepsAreKeptBecauseTheyPutRealContactsInTheLedger()
    {
        ICelestialEphemeris sol = CircularOrbitEphemeris.FromScenario(
            ScenarioLoader.LoadFile(Path.Combine(RepoRoot(), "scenarios", "sol.json")));

        CorridorRegion lane = TradeCorridors.Regions(sol, 0)
            .Single(r => r is { AId: "earth", BId: "mars" } or { AId: "mars", BId: "earth" });

        // A vantage just off the lane's midpoint, close enough that the telescope reaches along it.
        Vector2d axis = (lane.B - lane.A).Normalized();
        Vector2d perp = new(-axis.Y, axis.X);
        Vector2d vantage = lane.Midpoint + (perp * lane.Radius * 3);

        var telescope = new TelescopeModel();
        ScanJob job = TradeCorridors.SweepJobFor(lane, vantage);

        var inTheLane = new ShipState(lane.Midpoint, Vector2d.Zero, 0.0);
        var wellOff = new ShipState(lane.Midpoint + (perp * lane.Length), Vector2d.Zero, 0.0);

        IReadOnlyList<Observation> found = TrackingStation.Sweep(
            telescope, job, vantage, [("hauler", inTheLane), ("elsewhere", wellOff)], 1234.0);

        Assert.Equal("hauler", Assert.Single(found).TargetId);

        // The task the menu books really is the one the post routes to that sweep.
        SensorTask task = SensorTask.CorridorSweep(lane.AId, lane.BId, "lane sweep", recurring: false);
        Assert.Equal(SensorTaskKind.CorridorSweep, task.Kind);
        Assert.Equal(lane.AId, task.CorridorAId);
        Assert.Equal(lane.BId, task.CorridorBId);
    }

    /// <summary>
    /// A LANE IS FOUND WITHOUT ANYTHING HAVING BEEN DRAWN — the regression the archive uncovered.
    ///
    /// <para>The geometry the sweep actions need used to be built inside the corridor DRAW, so it existed only
    /// while a decoration layer was ticked on. This asks the page for the lane by the same road the open-sky
    /// menu does, on a page that has painted nothing, and it must answer.</para>
    ///
    /// <para>RED PROOF: fill the cache from a draw again (or hide the lane layer, as shipped) and this fails
    /// with "the page cannot name any lane".</para>
    /// </summary>
    [Fact]
    public async Task ALaneIsFoundWithoutAnythingHavingBeenDrawn()
    {
        using DeskBench bench = await DeskBench.BootAsync("/map?start=wreck");

        var ephemeris = (ICelestialEphemeris)bench.Field("_ephemeris")!;
        double simTime = (double)bench.Field("SimTime")!;
        CorridorRegion lane = TradeCorridors.Regions(ephemeris, simTime).First();

        object? found = bench.Call("NearLaneFor", lane.Midpoint);

        Assert.True(found is CorridorRegion,
            "#953 · the page cannot name any lane, so the open-sky menu can never offer its two sweeps. "
            + "That is what happens when the geometry the ACTIONS need is only built by a DRAW.");
        Assert.Equal(lane.Name, ((CorridorRegion)found!).Name);
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
}
