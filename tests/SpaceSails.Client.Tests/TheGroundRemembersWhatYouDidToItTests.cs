using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #563 slice 2 · THE GROUND REMEMBERS WHAT THE CAPTAIN DID TO IT — where that memory is kept, and what is
/// deliberately not multiplied across the lattice.
///
/// <para>Slice 1 keyed everything a captain does to a hut on the TILE, which was the hard half and is
/// measured in Core (<c>TheTreadmillsRemaindersTests</c>). What it could not say is <b>where the answer
/// lives</b>, and that turned out to be the whole bug: three <c>HashSet</c>s on the excursion record, so a
/// hatch shouldered open on one trip was dogged again on the next. The Core guards prove the ledger works;
/// these prove the game is holding it in the one place that survives a shuttle.</para>
/// </summary>
public class TheGroundRemembersWhatYouDidToItTests
{
    private static Type MapType =>
        typeof(SpaceSails.Client.Rendering.DeckPlan).Assembly.GetType("SpaceSails.Client.Pages.Map")
        ?? throw new InvalidOperationException("the page is gone");

    private static Type ExcursionType =>
        MapType.GetNestedType("SurfaceExcursion", BindingFlags.NonPublic | BindingFlags.Public)
        ?? throw new InvalidOperationException("the excursion record is gone");

    private const BindingFlags Members =
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    /// <summary>THE LEDGER IS ON THE SHIP, NOT ON THE VISIT. A <see cref="GroundMemory"/> held by the page
    /// outlives an excursion; one held by the excursion is thrown away with it, which is the bug this slice
    /// exists to fix and would be invisible in every test that never lifts off.</summary>
    [Fact]
    public void WhatTheCaptainDidToAHut_IsHeldByThePageAndNotByTheVisit()
    {
        Assert.Contains(
            MapType.GetFields(Members),
            f => f.FieldType == typeof(GroundMemory));

        Assert.DoesNotContain(
            ExcursionType.GetFields(Members).Select(f => f.FieldType)
                .Concat(ExcursionType.GetProperties(Members).Select(p => p.PropertyType)),
            t => t == typeof(GroundMemory));
    }

    /// <summary>…AND THE VISIT KEEPS NO COPY OF IT EITHER. A cached copy of a fact is a second source of
    /// that fact, and this is the class of state that must not have two: the excursion carries the resolved
    /// hut PLACEMENTS (a cache of a pure function, safe to drop) and nothing about what was done to
    /// them.</summary>
    [Fact]
    public void TheExcursion_KeepsNoSecondOpinionAboutWhichHutsAreOpen()
    {
        string[] names =
        [
            .. ExcursionType.GetFields(Members).Select(f => f.Name),
            .. ExcursionType.GetProperties(Members).Select(p => p.Name),
        ];

        foreach (string what in new[] { "Forced", "Looted", "Read", "Emptied" })
        {
            Assert.DoesNotContain(names, n => n.Contains("Hut", StringComparison.Ordinal)
                && n.Contains(what, StringComparison.Ordinal));
        }
    }

    /// <summary>THE LEDGER REACHES THE FILE IN BOTH DIRECTIONS. A section that is written and never read
    /// back — or read and never written — is a save that forgets, and it forgets silently: nothing throws,
    /// the huts are simply dogged again a week later. Both halves are named here because the page cannot be
    /// stood up in a test and half a wiring is exactly the mistake that would ship.</summary>
    [Fact]
    public void TheGroundLedger_IsBothSavedAndLoaded()
    {
        string vault = Pages("Map.Vault.cs");
        Assert.Contains("new GroundSection", vault, StringComparison.Ordinal);
        Assert.Contains("GroundMemory.Restore(vault.Ground?.Changed)", vault, StringComparison.Ordinal);
    }

    /// <summary>
    /// #316 law 1 · THE HUSKS RIDE THE SAME LEDGER, AND ONE HAND WRITES THEM. The Core guards
    /// (<c>TheTreadmillsRemaindersTests</c>) prove the rows survive a vault; what only the client can get
    /// wrong is the WIRING, and every way it could be half-done is a silent forget:
    ///
    /// <list type="bullet">
    ///   <item>a kill path that appends to the visit's list and never writes the ledger — the exact bug this
    ///   lane is fixing, re-shipped through the other of the two guns;</item>
    ///   <item>a ledger written and never read back on arrival, so the field is clean anyway;</item>
    ///   <item>the age reading never polled, so a row with a moment in it is a moment nobody is told.</item>
    /// </list>
    ///
    /// <para>Read off the shipping method bodies, the way this file's siblings read routing claims: the page
    /// cannot be stood up in a test, and half a wiring is exactly the mistake that would ship.</para>
    ///
    /// <para><b>Proven RED</b> three ways: by putting the sweep team's kill back to
    /// <c>_surface?.Husks.Add(...)</c>, by removing the arrival seeding, and by removing the poll from
    /// <c>StepSurface</c>.</para>
    /// </summary>
    [Fact]
    public void TheHusks_AreWrittenByOneHandSeededOnArrivalAndRead()
    {
        // ONE WRITER. Both guns come through AHuskFallsAt, and nothing else in the client builds a husk row:
        // a second writer is a second place to forget the ledger, which is this bug exactly.
        foreach (string file in new[] { "Map.Surface.Reevers.cs", "Map.SweepTeam.cs" })
        {
            Assert.Contains("AHuskFallsAt(ex,", Pages(file), StringComparison.Ordinal);
            Assert.DoesNotContain(".Husks.Add(", Pages(file), StringComparison.Ordinal);
        }

        string tiles = Pages("Map.Surface.Tiles.cs");
        Assert.Contains("private void AHuskFallsAt(SurfaceExcursion ex, double x, double y)", tiles,
            StringComparison.Ordinal);
        Assert.Contains("_groundMemory.Remember(GroundMemory.HuskKey(", tiles, StringComparison.Ordinal);
        Assert.Contains("RequestVaultSave()", tiles, StringComparison.Ordinal);

        // …and the row is only written where the GROUND can hold one. A poured floor hundreds of metres down
        // and somebody else's steel deck are real places to be shot on and neither is a tile of a landing
        // site's lattice, so a husk filed there would be measured in a frame it was never in.
        Assert.Contains("TheGroundKeepsHusksHere(ex)", tiles, StringComparison.Ordinal);
        Assert.Contains("ex.Floor == 0 && !OnWreck", tiles, StringComparison.Ordinal);

        // SEEDED ON ARRIVAL, at the one place the excursion is built, beside the rooms it already seeds —
        // and out of Core's own reader, so the client never learns the key format.
        Assert.Contains("SeedTheHusksLeftHere(excursion);", Pages("Map.Surface.cs"), StringComparison.Ordinal);
        Assert.Contains("_groundMemory.HusksAt(ex.Stop.Body.Id, ex.Site.LayoutSalt)", tiles,
            StringComparison.Ordinal);

        // READ: the age band is polled as the captain walks, in the underfoot family, and the words are
        // Core's own rather than a second copy of them here.
        Assert.Contains("CheckHusksUnderfoot();", Pages("Map.Surface.Frame.cs"), StringComparison.Ordinal);
        Assert.Contains("GroundMemory.AgeLine(read, SimTime)", tiles, StringComparison.Ordinal);
        foreach (string prose in new[] { "Still smoking", "Dusted over", "Regolith-dusted" })
        {
            Assert.DoesNotContain(prose, tiles, StringComparison.Ordinal);
        }
    }

    /// <summary>THE GROUND OUT THERE IS FURNISHED. Slice 1 welded a tile's WALLS and nothing else, so a
    /// building a hundred du from the tube was a thick-walled room with an open gap where its door belongs
    /// and nothing at all inside — word for word the report #573 was filed about, re-shipped one tile out.
    ///
    /// <para>Asked of the composer itself rather than of its source, and rather than of Core: what a captain
    /// meets is the REGION this builds, and a call that is present in the text while the region comes back
    /// empty is exactly the guard failure this project has a name for.</para></summary>
    [Fact]
    public void AChunkOfGroundFarFromTheTube_ComesBackWithDoorsAndThingsToPress()
    {
        int chunksWithDoors = 0, chunksWithFinds = 0, chunks = 0;

        foreach (string body in new[] { "luna", "phobos", "titan", "enceladus" })
        {
            foreach (LandingSite site in LandingSites.For(body))
            {
                chunks++;
                object region = ComposeChunk(body, site.LayoutSalt, new SurfaceTiles.Address(-2, -3));

                chunksWithDoors += Count(region, "Doors") > 0 ? 1 : 0;
                chunksWithFinds += Salvage(region) > 0 ? 1 : 0;

                // Ground, and only ground: no filled masses out there, whatever the moon's own signature is.
                Assert.Equal(0, Count(region, "Structures"));
                Assert.True(Count(region, "Walls") > 0,
                    $"{body}/{site.LayoutSalt}: a chunk three tiles out came back with no ground at all.");
            }
        }

        Assert.True(chunks > 8, $"only {chunks} chunks were built — that is not a sweep.");
        Assert.True(chunksWithDoors > chunks / 2,
            $"{chunksWithDoors} of {chunks} chunks out in the world hang a single door.");
        Assert.True(chunksWithFinds > chunks / 2,
            $"{chunksWithFinds} of {chunks} chunks out in the world hold anything worth pressing [E] on.");
    }

    /// <summary>WHAT THE LATTICE MUST NOT MULTIPLY. #1058 made the landmarks a fact about a BODY — one
    /// monolith standing on one ground — and an unbounded lattice is precisely the machine that would turn
    /// that into a wallpaper of monoliths, one per tile, out to the backstop. Miranda's is the case that
    /// matters: its canon ground has one, and its ninth tile out must not.</summary>
    [Fact]
    public void TheMonolithsPlate_NeverArrivesOnAWeldedTile()
    {
        foreach (string body in new[] { "phobos", "luna", "miranda" })
        {
            foreach (LandingSite site in LandingSites.For(body))
            {
                foreach ((int dx, int dy) in new[] { (0, -1), (-2, -3), (3, -2), (1, 0) })
                {
                    object region = ComposeChunk(body, site.LayoutSalt, new SurfaceTiles.Address(dx, dy));
                    foreach ((float _, float _, string text) in Labels(region))
                    {
                        Assert.NotEqual(Monolith.ConsoleLabel, text);
                        Assert.NotEqual(FalseSlab.ConsoleLabel, text);
                    }
                    Assert.Equal(0, Count(region, "Structures"));
                }
            }
        }
    }

    /// <summary>#563 · THE ONLY INVISIBLE BOUND LEFT IS THE SHIP'S OWN LINE — which is the whole answer to
    /// the issue's last open item, the boundary fade.
    ///
    /// <para>The fade exists and still runs: <c>DeckView.DrawUnseenFalloff</c> darkens the ground for several
    /// deck units in from every <c>DeckPlan.Wall.Unseen</c>, with an irregular inner edge so the eye cannot
    /// recover a corner. What changed is that there is almost nothing left for it to hang off. The field
    /// envelope's three sides are gone (slice 1), and the backstop is a RADIUS — never a wall, never drawn,
    /// never reached — so it has no edge to fade.</para>
    ///
    /// <para>What remains is the northern rim of the top tile row: the line the landing band's own edge
    /// continues along once a captain has walked out from under the shuttle. It is a real bound, it is
    /// invisible, and it is exactly the kind of thing the fade was built for. This holds it to that: every
    /// unseen wall a welded tile brings is horizontal, and it is on the top edge of a top-row tile.</para></summary>
    [Fact]
    public void TheOnlyUnseenBoundOnAWeldedTile_IsTheTopRowsRim()
    {
        int rims = 0;

        foreach (string body in new[] { "luna", "phobos" })
        {
            LandingSite site = LandingSites.For(body)[0];

            // Up against the ship: the chunk that carries top-row tiles, where the rim lives.
            foreach ((float x1, float y1, float x2, float y2, bool unseen) in
                     Walls(ComposeChunk(body, site.LayoutSalt, new SurfaceTiles.Address(2, 0))))
            {
                if (!unseen)
                {
                    continue;
                }
                rims++;
                Assert.Equal(y1, y2, 4);
                (double _, double _, double _, double topY) =
                    SurfaceTiles.Rect(SurfaceTiles.At((x1 + x2) / 2.0, y1 - 0.5));
                Assert.Equal(topY, y1, 4);
                Assert.Equal(SurfaceTiles.TopRow, SurfaceTiles.At((x1 + x2) / 2.0, y1 - 0.5).Y);
            }

            // And out in the deep there is no invisible bound at all — nothing to fade, because nothing
            // stops you.
            foreach ((float _, float _, float _, float _, bool unseen) in
                     Walls(ComposeChunk(body, site.LayoutSalt, new SurfaceTiles.Address(-2, -3))))
            {
                Assert.False(unseen, "a tile three rows deep laid an invisible bound — the fence is back.");
            }
        }

        Assert.True(rims > 0, "no unseen rim was found at all — this guard is measuring nothing.");
    }

    private static IEnumerable<(float X1, float Y1, float X2, float Y2, bool Unseen)> Walls(object region)
    {
        Array walls = (Array)region.GetType().GetProperty("Walls")!.GetValue(region)!;
        foreach (object? w in walls)
        {
            Type t = w!.GetType();
            yield return (
                (float)t.GetProperty("X1")!.GetValue(w)!,
                (float)t.GetProperty("Y1")!.GetValue(w)!,
                (float)t.GetProperty("X2")!.GetValue(w)!,
                (float)t.GetProperty("Y2")!.GetValue(w)!,
                (bool)t.GetProperty("Unseen")!.GetValue(w)!);
        }
    }

    // ── the composer, reached the way a private static is reached ───────────────────────────────────────

    /// <summary>The tile region the live deck is grown by, built for the chunk around one address.</summary>
    private static object ComposeChunk(string body, string salt, SurfaceTiles.Address centre)
    {
        MethodInfo compose = MapType.GetMethod("TileRegion", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("TileRegion is gone — the composer moved.");
        return compose.Invoke(null, [body, salt, SurfaceTiles.Chunk(centre)])
            ?? throw new InvalidOperationException("the composer handed back nothing");
    }

    private static int Count(object region, string member) =>
        ((Array?)region.GetType().GetProperty(member)!.GetValue(region))?.Length ?? 0;

    private static int Salvage(object region)
    {
        Array consoles = (Array)region.GetType().GetProperty("Consoles")!.GetValue(region)!;
        int found = 0;
        foreach (object? spot in consoles)
        {
            object kind = spot!.GetType().GetProperty("Kind")!.GetValue(spot)!;
            found += kind.ToString() == "RuinSalvage" ? 1 : 0;
        }
        return found;
    }

    private static IEnumerable<(float X, float Y, string Text)> Labels(object region)
    {
        Array labels = (Array)region.GetType().GetProperty("Labels")!.GetValue(region)!;
        foreach (object? row in labels)
        {
            yield return ((float, float, string))row!;
        }
    }

    private static string Pages(string file) =>
        File.ReadAllText(Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages", file));

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
}
