using System;
using System.IO;
using System.Linq;
using System.Reflection;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #316 law 1's SECOND HALF, in the page · <b>SOMEBODY ELSE HAS BEEN HERE.</b>
///
/// <para>Core's guards (<c>TheBattlefieldIsALedgerTests</c>) prove the arithmetic: a resolved discovery
/// yields husks, a hole and — in the dire case — an abandoned bot, all dated by the day the roll landed, all
/// surviving a vault. None of that reaches a player unless the CLIENT is wired, and every way the wiring can
/// be half-done here is a silent forget rather than a crash:</para>
/// <list type="bullet">
///   <item>the watch deletes the chest and files nothing — the shipped behaviour this lane is replacing;</item>
///   <item>the marks are filed AFTER the chest comes off the books, when the ✗ they are supposed to sit in
///   no longer exists;</item>
///   <item>they are filed and never seeded on arrival, so the field is clean anyway;</item>
///   <item>they are seeded and never drawn;</item>
///   <item>the ledger is written and the file never asked to carry it, so the one place this feature can be
///   met — a LATER trip — never happens.</item>
/// </list>
///
/// <para>Read off the shipping method bodies, the way this file's siblings read routing claims: the page
/// cannot be stood up in a test, and half a wiring is exactly the mistake that would ship. What CAN be
/// driven for real is driven for real — the projection that decides where the hole goes, and the whole
/// bury→leave→robbed→return→read chain through the shipping calls.</para>
///
/// <para><b>Proven RED</b> (watched, quoted in the pull request): by filing the marks after
/// <c>_caches.Remove</c>, by deleting the arrival seeding, by dropping the husk term from the watch's
/// threshold, and by pointing the hole at <c>MoonSurface.CachePosition</c> instead of the recorded dig spot.</para>
/// </summary>
public class TheGroundKeepsSomebodyElsesFootprintsTests
{
    private const BindingFlags Members =
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    private static Type MapType =>
        typeof(DeckPlan).Assembly.GetType("SpaceSails.Client.Pages.Map")
        ?? throw new InvalidOperationException("the page is gone");

    private static Type ExcursionType =>
        MapType.GetNestedType("SurfaceExcursion", Members)
        ?? throw new InvalidOperationException("the excursion record is gone");

    /// <summary>
    /// THE WATCH FILES THE SCENE, AND FILES IT WHILE THERE IS STILL A CHEST TO DERIVE IT FROM.
    ///
    /// <para>Ordering is the whole guard. Every mark is built out of the cache — its id seeds the pack the
    /// rivals met, its recorded dig spot IS the hole — so a writer called after <c>_caches.Remove</c> is
    /// writing about a chest that no longer exists. It would not throw; it would file a hole at the wrong
    /// place or at none, silently, on the one path a player can never re-run.</para>
    /// </summary>
    [Fact]
    public void TheDiscoveryWatch_FilesTheSceneBeforeItDeletesTheChest()
    {
        string watch = Pages("Map.Quests.Caches.cs");

        int filed = watch.IndexOf("TheRivalsLeftTheirMarks(c, found)", StringComparison.Ordinal);
        int deleted = watch.IndexOf("_caches.Remove(c.Id)", StringComparison.Ordinal);

        Assert.True(filed >= 0, "the discovery watch files no evidence at all");
        Assert.True(deleted >= 0, "the discovery watch no longer removes the found chest");
        Assert.True(filed < deleted,
            "the scene is filed AFTER the chest is off the books — the marks are derived from the chest");

        // …and the day it happened is what the watch hands over, not SimTime: the pattern-match binding is
        // the whole reason DiscoveredWithin returns a period rather than a bool.
        Assert.Contains("DiscoveredWithin(c, from, SimTime, TheFightThisGroundCarries(c)) is { } found",
            watch, StringComparison.Ordinal);
    }

    /// <summary>
    /// ONE WRITER FOR SOMEBODY ELSE'S MARKS, AND IT IS THE ONLY PLACE THEY ARE MINTED. The captain's own
    /// husks have had one hand since #1105 (<c>AHuskFallsAt</c>); a rival's marks get the same law, because
    /// a second minting site is a second place to forget the vault — which is the bug both halves of this
    /// issue exist to close.
    /// </summary>
    [Fact]
    public void TheRivalMarks_HaveOneWriterAndItSavesTheFile()
    {
        string forensics = Pages("Map.Surface.Forensics.cs");

        Assert.Contains("private void TheRivalsLeftTheirMarks(TreasureCache cache, long foundPeriod)",
            forensics, StringComparison.Ordinal);
        Assert.Contains("RivalVisit.LeftBehind(", forensics, StringComparison.Ordinal);
        Assert.Contains("GroundMemory.HuskKey(cache.BodyId, salt, husk)", forensics, StringComparison.Ordinal);
        Assert.Contains("GroundMemory.ScarKey(cache.BodyId, salt, scar)", forensics, StringComparison.Ordinal);
        Assert.Contains("RequestVaultSave()", forensics, StringComparison.Ordinal);

        // Nowhere else in the client mints a rival's scene.
        foreach (string file in ClientSources())
        {
            if (Path.GetFileName(file) == "Map.Surface.Forensics.cs")
            {
                continue;
            }
            Assert.DoesNotContain("RivalVisit.LeftBehind(", File.ReadAllText(file), StringComparison.Ordinal);
            Assert.DoesNotContain("GroundMemory.ScarKey(", File.ReadAllText(file), StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// SEEDED ON ARRIVAL, AND DRAWN. A row in the file that nothing reads back is a save that forgets
    /// silently: the captain lands on the site he was robbed at and finds clean regolith, which is the
    /// shipped behaviour this lane exists to replace.
    /// </summary>
    [Fact]
    public void TheScene_IsSeededOnArrivalAndDrawn()
    {
        // Seeded where the husks are seeded, before the first frame. Matched as a STATEMENT rather than as
        // a substring: the way this wiring actually goes missing is somebody commenting the line out while
        // chasing something else, and a guard that a comment satisfies is a guard that cannot fail.
        string surface = Pages("Map.Surface.cs");
        Assert.Matches(@"(?m)^\s+SeedTheHusksLeftHere\(excursion\);", surface);
        Assert.Matches(@"(?m)^\s+SeedTheScarsLeftHere\(excursion\);", surface);
        Assert.Matches(@"(?m)^\s+foreach \(GroundMemory\.Scar scar in _groundMemory\.ScarsAt\(",
            Pages("Map.Surface.Forensics.cs"));

        // The visit carries them, and only as a copy of what the ground already holds.
        Assert.Contains(
            ExcursionType.GetProperties(Members).Select(p => p.Name), n => n == "Scars");

        // The HUD publishes both marks, and the abandoned bot rides the SENTRY list — the #314 mark for a
        // counter frozen at 00 — rather than minting a second bot glyph nobody asked for.
        string hud = Pages("Map.Surface.Hud.cs");
        Assert.Contains("_hudPits.Add((scar.X, scar.Y))", hud, StringComparison.Ordinal);
        Assert.Contains("Pits: _hudPits", hud, StringComparison.Ordinal);
        Assert.Contains("SentryBot.Readout(0), true, false", hud, StringComparison.Ordinal);

        // …and the renderer actually paints them, in the ground vocabulary that is already there.
        string frame = File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "SpaceSails.Client", "Rendering", "DeckView.Frame.cs"));
        Assert.Contains("hud.Pits is { } pits", frame, StringComparison.Ordinal);
        Assert.Contains("\"✗\", PitInk", frame, StringComparison.Ordinal);
    }

    /// <summary>
    /// THE HOLE IS AT THE ✗, NOT NEAR IT — and there is ONE function that answers where a ✗ is. A chest
    /// that recorded its dug spot (every bury since playtest bug #5) is robbed exactly there; a legacy chest
    /// that never recorded one falls back to the same hash-scatter its mark has always been drawn at. Driven
    /// for real, because this one can be.
    /// </summary>
    [Fact]
    public void TheHoleIsWhereTheMarkWas()
    {
        TreasureCache dug = CacheMint.Bury(
            "cache-you-spot", "phobos", 3, 500, [], 0, "you", playerOwned: true,
            digX: -41.5, digY: -175.25, siteIndex: 0, buried: true, padDistance: 150);
        Assert.Equal((-41.5, -175.25), MoonSurface.CacheSpot(dug));

        TreasureCache legacy = CacheMint.Bury(
            "cache-npc-legacy", "phobos", 4, 500, [], 0, "Old Vane", playerOwned: false);
        Assert.Equal(MoonSurface.CachePosition(legacy.Id), MoonSurface.CacheSpot(legacy));

        // The robbery puts its hole exactly there, whichever kind of chest it was.
        foreach (TreasureCache c in new[] { dug, legacy })
        {
            (double sx, double sy) = MoonSurface.CacheSpot(c);
            RivalVisit.Evidence left = RivalVisit.LeftBehind(c.Id, c.ReeverLevel, sx, sy, 12);
            Assert.Equal(sx, left.Pit.X, 6);
            Assert.Equal(sy, left.Pit.Y, 6);
        }
    }

    /// <summary>
    /// #316 law 3 · EVERY REPORTER OF THE ODDS READS THE GROUND, or the one that does not is a sentence
    /// promising a safe the dice stopped delivering. The bury line, the panic-drop line, the lift-off line
    /// for a chest left in the open, the ledger row and the watch's own threshold: five readers, one
    /// question.
    /// </summary>
    [Fact]
    public void EveryReporterOfTheOddsAsksWhatHappenedOnThisGround()
    {
        string dig = Pages("Map.Surface.Dig.cs");
        Assert.Contains("cache.SafetyWith(TheFightThisGroundCarries(cache))", dig, StringComparison.Ordinal);
        Assert.Contains("TheFightThisGroundCarries(ex)", dig, StringComparison.Ordinal);

        Assert.Contains("open.SafetyWith(TheFightThisGroundCarries(open))",
            Pages("Map.Surface.cs"), StringComparison.Ordinal);
        Assert.Contains("c.SafetyWith(TheFightThisGroundCarries(c))",
            Pages("Map.Quests.Ledger.cs"), StringComparison.Ordinal);
        Assert.Contains("TheFightThisGroundCarries(c)",
            Pages("Map.Quests.Caches.cs"), StringComparison.Ordinal);

        // …and NOBODY still reads the bare chest, which is the drift this guard is shaped against.
        foreach (string file in ClientSources())
        {
            string src = File.ReadAllText(file);
            Assert.DoesNotContain(".Safety.Sentence", src, StringComparison.Ordinal);
            Assert.DoesNotContain("c.Safety.Word", src, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// A BODY-WIDE CHEST FILES NOTHING, and that is the honest answer rather than a soft one. A legacy save
    /// and a rumour map never recorded WHICH of a body's landing sites they are under (#320/#650), so there
    /// is no ground to file a firefight against — and a guess would scatter husks over a site the captain
    /// has never walked, which is the exact bug #650 fixed for the ✗ itself.
    /// </summary>
    [Fact]
    public void AChestWithNoGroundUnderIt_ScattersNothingAcrossTheBody()
    {
        string forensics = Pages("Map.Surface.Forensics.cs");
        Assert.Contains("cache.SiteIndex is { } i ? LandingSites.At(cache.BodyId, i).LayoutSalt : null",
            forensics, StringComparison.Ordinal);
        Assert.Contains("if (GroundSaltFor(cache) is not { } salt)", forensics, StringComparison.Ordinal);
        Assert.Contains("return;", forensics, StringComparison.Ordinal);
    }

    /// <summary>
    /// AND THE WHOLE THING THE WAY A CAPTAIN MEETS IT, through the shipping calls: he makes a stand on a
    /// ground, buries a chest under a line that KNOWS about the mess, flies off, is beaten to it, comes back
    /// a week later and reads the scene off a reloaded file. Every step here is a call the page makes.
    /// </summary>
    [Fact]
    public void BuryLeaveAndComeBackToARobbedGround()
    {
        const string body = "phobos";
        string salt = LandingSites.At(body, 0).LayoutSalt;
        var ground = new GroundMemory();

        // 1 · The stand. Four Old Ones down where he held the line — the ground keeps it (#1105).
        for (int i = 0; i < 4; i++)
        {
            ground.Remember(GroundMemory.HuskKey(body, salt, new GroundMemory.Husk(-20 - i, -160, 0)));
        }
        int loud = ground.HusksAt(body, salt).Count;

        // 2 · The bury, and the line he is shown is priced against that mess.
        var caches = new CacheLedger();
        TreasureCache chest = caches.Bury(
            body, 800, [], simTime: 0, "you", playerOwned: true, reeverLevel: 2,
            digX: -38.0, digY: -190.0, siteIndex: 0, buried: true, padDistance: 180.0);
        Assert.True(chest.SafetyWith(loud).ChancePerMille > chest.Safety.ChancePerMille,
            "the stand he made bought a rival nothing");

        // 3 · He is away. The watch resolves the skipped span at the loud threshold and hands back the day.
        long? found = DiscoveryRule.DiscoveredWithin(
            chest, lastCheckedPeriod: 0, nowSimTime: 900 * DiscoveryRule.PeriodSeconds, huskCount: loud);
        Assert.NotNull(found);

        // 4 · The ground carries it, and the chest comes off the books.
        (double sx, double sy) = MoonSurface.CacheSpot(chest);
        RivalVisit.Evidence left = RivalVisit.LeftBehind(chest.Id, chest.ReeverLevel, sx, sy, found!.Value);
        foreach (GroundMemory.Husk h in left.Husks)
        {
            ground.Remember(GroundMemory.HuskKey(body, salt, h));
        }
        foreach (GroundMemory.Scar s in left.Scars)
        {
            ground.Remember(GroundMemory.ScarKey(body, salt, s));
        }
        caches.Remove(chest.Id);
        Assert.Empty(caches.CachesAt(body, 0));

        // 5 · Lift-off, save, load, and he walks back on a week later.
        GroundMemory back = GroundMemory.Restore(
            VaultSerializer.Load(VaultSerializer.Save(
                new Vault { Ground = new GroundSection { Changed = ground.Stored } })).Ground?.Changed);

        GroundMemory.Scar hole = Assert.Single(
            back.ScarsAt(body, salt), s => s.What == GroundMemory.ScarKind.Pit);
        Assert.Equal(sx, hole.X, 2);
        Assert.Equal(sy, hole.Y, 2);

        // The scene reads, and it reads the age the world actually has.
        double home = left.AtSimTime + (3 * GroundMemory.DaySeconds);
        Assert.Equal("Dusted over. Days old.", GroundMemory.AgeLine(hole.AtSimTime, home));
        Assert.Equal("Regolith-dusted. Weeks old.",
            GroundMemory.AgeLine(new GroundMemory.Husk(-20, -160, 0), home));   // his own stand, long ago
    }

    private static string[] ClientSources() =>
        Directory.GetFiles(Path.Combine(RepoRoot(), "src", "SpaceSails.Client"), "*.cs",
            SearchOption.AllDirectories);

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
