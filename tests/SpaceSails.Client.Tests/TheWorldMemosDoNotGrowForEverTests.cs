using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using SpaceSails.Client.Rendering;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1112 · THE TWO WORLD MEMOS ARE HELD TO A CAP, AND WHAT COMES BACK OUT OF THEM IS WHAT WENT IN.
///
/// <para><b>What was wrong.</b> <c>HavenInterior</c>'s deck cache keyed on the docking watch — and the watch
/// advances for ever. Nothing ever took an entry out again, so a long voyage left one whole built station in
/// memory per watch until the tab closed. Its twin <c>MoonSurface</c>'s layout cache had had a cap and a
/// flush since #371 Phase 1: the same shape of memo, the same kind of key, one of them bounded and one of
/// them not, because the rule was written into a call site instead of into a type. Both now hold a
/// <c>BoundedMemo</c>, and this class is what stops the two drifting apart a second time.</para>
///
/// <para><b>What it asserts.</b> Four things, in the order they matter:</para>
/// <list type="number">
/// <item>the policy itself, on a memo of its own: cap + 1 distinct keys go in, it never holds more than the
/// cap, and the oldest key is gone afterwards;</item>
/// <item>the haven memo, driven the way the bug was found — cap + 1 distinct docking watches, one per
/// call, exactly as a long voyage does it — never holds more than its cap;</item>
/// <item>a hit and a miss hand back the same station, mark for mark, including after an eviction has
/// thrown the first build away. A cap that changed what a caller was handed would be a far worse bug than
/// the leak it fixed;</item>
/// <item>and the moon's cap still holds under the shared policy, plus a drift law: no runtime memo in the
/// shipped client may be a bare concurrent dictionary again.</item>
/// </list>
///
/// <para><b>Proven able to fail</b> (each reverted alone, watched red, restored — see the PR):
/// with <c>HavenInterior</c>'s memo put back to the unbounded <c>ConcurrentDictionary</c> it was, (2) goes
/// red holding 65 stations against a cap of 64 and (4)'s drift law goes red on the bare dictionary; with
/// <c>BoundedMemo</c>'s <c>Clear()</c> on overflow removed, (1) and (2) and the moon's half of (4) go red.</para>
///
/// <para><b>Why the cap assertions are safe under parallel test classes</b> — both memos are process-wide and
/// other classes build decks at the same time. <c>Count &lt;= Cap</c> is an invariant of the memo, not a
/// property of this test's usage: every insert is taken under the memo's own lock, so a racing insert can
/// never carry it past the cap, and a foreign insert can only make an UNBOUNDED memo bigger — never smaller.
/// The assertions therefore cannot flake in either direction, and they are not vacuous either: cap + 1
/// distinct keys is one more than the cap by construction, so a memo without one must exceed it.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheWorldMemosDoNotGrowForEverTests
{
    /// <summary>The haven whose decks these guards build. The first listed is enough: the memo is one
    /// dictionary for every station, so it cannot be bounded for one and unbounded for another.</summary>
    private static string TheHaven => HavenInterior.InteriorBodyIds[0];

    /// <summary>A distinct docking watch per index — the memo's key carries
    /// <see cref="PatronRota.WatchIndex"/>, so one whole watch apart is the smallest step that is a new
    /// key, and it is exactly the step a voyage takes.</summary>
    private static double WatchTime(int index) => PatronRota.WatchSeconds * (index + 1);

    // ── 1 · THE POLICY, ON ITS OWN MEMO ───────────────────────────────────────────────────────────────

    /// <summary>
    /// #1112 · A memo of eight, handed nine distinct keys, holds eight — and the first key is the one that
    /// is gone. (It is gone because on overflow they ALL are: the rule is the moon's flush, not an LRU. The
    /// guard states the property that matters to the leak — the oldest does not outlive the cap — and would
    /// hold just as well if the rule ever became a recency list.)
    /// </summary>
    [Fact]
    public void AMemoNeverHoldsMoreThanItsCapAndTheOldestIsWhatLeaves()
    {
        const int cap = 8;
        var memo = new BoundedMemo<int, string>(cap);
        var counts = new List<int>();

        for (int key = 0; key <= cap; key++)
        {
            int k = key;
            string built = memo.GetOrBuild(k, () => $"deck-{k}");
            Assert.Equal($"deck-{k}", built);
            counts.Add(memo.Count);
            Assert.True(memo.Count <= cap,
                $"after {key + 1} distinct key(s) the memo holds {memo.Count}, over its cap of {cap}. " +
                $"Counts so far: {string.Join(", ", counts)}");
        }

        Assert.False(memo.Holds(0),
            $"key 0 went in first and {cap} newer keys have gone in since — a memo of {cap} that still " +
            "holds it is holding more than its cap, or is evicting the wrong end.");
        Assert.True(memo.Holds(cap), "the key that was just built is the one thing the memo must hold.");
    }

    // ── 2 · THE HAVEN MEMO, DRIVEN THE WAY THE BUG WAS FOUND ──────────────────────────────────────────

    /// <summary>
    /// #1112 · A VOYAGE OF WATCHES. Dock at the same station on cap + 1 different watches — which is what a
    /// long game does, one watch at a time, and what the leak turned into one built station per watch held
    /// for ever. The memo must never hold more than its cap.
    /// </summary>
    [Fact]
    public void TheHavenMemoIsStillCappedAfterAVoyageOfWatches()
    {
        int cap = HavenInterior.DeckCacheCap;
        Assert.InRange(cap, 1, 4096);

        var watches = new HashSet<long>();
        int worst = 0;
        for (int i = 0; i <= cap; i++)
        {
            double simTime = WatchTime(i);
            watches.Add(PatronRota.WatchIndex(simTime));
            Assert.NotNull(HavenInterior.DockedDeck(TheHaven, simTime: simTime));
            worst = Math.Max(worst, HavenInterior.DeckCacheCount);
            Assert.True(HavenInterior.DeckCacheCount <= cap,
                $"after docking on {i + 1} distinct watch(es) the haven memo holds " +
                $"{HavenInterior.DeckCacheCount} built station(s), over its cap of {cap}. A key carrying the " +
                "docking watch never repeats, so an uncapped memo grows for the life of the process.");
        }

        // Anti-vacuous: the loop must really have asked for more distinct keys than the cap can hold, or
        // "never went over" is a sentence about nothing.
        Assert.Equal(cap + 1, watches.Count);
        Assert.True(worst > 0, "the memo held nothing at any point — this proves nothing about a cap.");
    }

    // ── 3 · A HIT AND A MISS ARE THE SAME STATION ─────────────────────────────────────────────────────

    /// <summary>
    /// #1112 · THE CAP MAY NOT CHANGE WHAT A CALLER IS HANDED. The same watch asked for twice running (the
    /// second one a hit), and then asked for again after cap + 1 other watches have flushed the memo out
    /// from under it (a miss, rebuilt from scratch) — all three must fingerprint identically, every wall,
    /// door, console, seat, backdrop, ink and room name.
    /// </summary>
    [Fact]
    public void AHitAndAMissBuildTheSameStation()
    {
        double simTime = WatchTime(9_000);
        string first = Fingerprint(Docked(simTime));
        string hit = Fingerprint(Docked(simTime));
        Assert.Equal(first, hit);

        // Evict it: cap + 1 distinct watches is one more than the memo can hold, so whatever else the
        // process is doing to this cache, the deck built above is not in it any more.
        for (int i = 0; i <= HavenInterior.DeckCacheCap; i++)
        {
            HavenInterior.DockedDeck(TheHaven, simTime: WatchTime(20_000 + i));
        }

        string rebuilt = Fingerprint(Docked(simTime));
        Assert.Equal(first, rebuilt);
    }

    private static DeckPlan Docked(double simTime) =>
        HavenInterior.DockedDeck(TheHaven, simTime: simTime)
        ?? throw new InvalidOperationException($"{TheHaven} is listed as having a deck and has none.");

    // ── 4 · THE MOON'S CAP, AND THE LAW THAT KEEPS THE TWINS TOGETHER ─────────────────────────────────

    /// <summary>
    /// #1112 · The moon's memo had this cap before the shared policy existed; it still has it after. Cap + 1
    /// distinct grounds (a new layout salt is a new key, which is how a bury/lift cycle churns it).
    /// </summary>
    [Fact]
    public void TheMoonMemoIsStillCappedAfterAVoyageOfGrounds()
    {
        int cap = MoonSurface.LayoutCacheCap;
        Assert.InRange(cap, 1, 4096);

        for (int i = 0; i <= cap; i++)
        {
            MoonSurface.SurfaceDeck(
                "luna", "luna", [], 0, static (_, _) => { },
                siteSalt: $"cap-guard-{i}", siteName: "cap guard", hasSecretSite: true);
            Assert.True(MoonSurface.LayoutCacheCount <= cap,
                $"after {i + 1} distinct ground(s) the layout memo holds {MoonSurface.LayoutCacheCount}, " +
                $"over its cap of {cap}.");
        }
    }

    /// <summary>
    /// #1112 · THE DRIFT LAW. The haven memo went unbounded for a year and a half of issues beside a twin
    /// that was capped, because "cache policy" was four lines inside one method and nobody diffs two methods
    /// in two files. So: every process-wide memo in the shipped client goes through <c>BoundedMemo</c>, and
    /// the way that is checked is that no static field in the assembly is a bare concurrent dictionary —
    /// concurrency is the tell, since the only reason a static dictionary here is ever made concurrent is
    /// that it is WRITTEN at run time (the authored lookup tables are plain and read-only). Both twins are
    /// then named explicitly, so removing one memo's bound cannot be hidden behind a rename.
    /// </summary>
    [Fact]
    public void EveryRuntimeMemoInTheClientIsBounded()
    {
        Type[] types = typeof(HavenInterior).Assembly.GetTypes();
        Assert.True(types.Length > 50, $"only {types.Length} type(s) reflected — the assembly was not read.");

        var bare = new List<string>();
        foreach (Type type in types)
        {
            foreach (FieldInfo field in type.GetFields(
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                if (field.FieldType.IsGenericType
                    && field.FieldType.GetGenericTypeDefinition().FullName is
                        "System.Collections.Concurrent.ConcurrentDictionary`2")
                {
                    bare.Add($"{type.Name}.{field.Name}");
                }
            }
        }

        Assert.True(bare.Count == 0,
            "a process-wide memo is held in a bare ConcurrentDictionary, which is how the haven's deck " +
            "cache grew without bound while its twin was capped (#1112). Use BoundedMemo:\n  " +
            string.Join("\n  ", bare));

        foreach (Type twin in new[] { typeof(HavenInterior), typeof(MoonSurface) })
        {
            Assert.True(
                twin.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                    .Any(f => f.FieldType.IsGenericType
                              && f.FieldType.GetGenericTypeDefinition() == typeof(BoundedMemo<,>)),
                $"{twin.Name} memoises what it builds and must hold it in a BoundedMemo — the two world " +
                "builders are twins and a policy only one of them holds is a policy that drifts.");
        }
    }

    // ── THE FINGERPRINT ───────────────────────────────────────────────────────────────────────────────
    //
    // Every mark the plan carries, printed by the record structs themselves so a field added to a Wall or a
    // TableTop tomorrow lands in the fingerprint without anybody remembering to add it here — plus the two
    // things a record cannot print for us: a table's chairs (an array inside a record) and the room-naming
    // delegate, which is sampled over a grid because a cached plan handing back the wrong room NAME is
    // exactly the kind of quiet wrongness this guard is for.

    private static string Fingerprint(DeckPlan deck)
    {
        var sb = new StringBuilder();
        foreach (DeckPlan.Wall w in deck.Walls) { sb.Append("wall ").Append(w).Append('\n'); }
        foreach (DeckPlan.Door d in deck.Doors) { sb.Append("door ").Append(d).Append('\n'); }
        foreach (DeckPlan.ConsoleSpot c in deck.Consoles) { sb.Append("console ").Append(c).Append('\n'); }
        foreach ((float X, float Y, string Text) l in deck.RoomLabels) { sb.Append("label ").Append(l).Append('\n'); }
        foreach ((float X, float Y, string Text, float Px, int Tone) b in deck.BigLabels) { sb.Append("sign ").Append(b).Append('\n'); }
        foreach (DeckPlan.Backdrop b in deck.Backdrops) { sb.Append("backdrop ").Append(b).Append('\n'); }
        foreach (DeckPlan.Structure s in deck.Structures) { sb.Append("structure ").Append(s).Append('\n'); }
        foreach (DeckPlan.FurnitureSpot f in deck.Furniture) { sb.Append("furniture ").Append(f).Append('\n'); }
        foreach (DeckPlan.TableTop t in deck.Tables)
        {
            sb.Append("table ").Append(t).Append('\n');
            foreach (DeckPlan.TableChair chair in t.Seating) { sb.Append("  chair ").Append(chair).Append('\n'); }
        }
        foreach (DeckPlan.StoolSpot s in deck.Stools) { sb.Append("stool ").Append(s).Append('\n'); }
        foreach (DeckPlan.BenchSpot b in deck.BenchSeats) { sb.Append("bench ").Append(b).Append('\n'); }
        foreach (SpaceSails.Core.SurfaceScenery.Mark m in deck.Scenery) { sb.Append("scenery ").Append(m).Append('\n'); }
        foreach (SpaceSails.Core.SurfaceCollision.Segment s in deck.CollisionSegments) { sb.Append("segment ").Append(s).Append('\n'); }

        sb.Append("spawn ").Append(N(deck.SpawnX)).Append(' ').Append(N(deck.SpawnY)).Append('\n');
        sb.Append("droids ").Append(deck.DroidCount).Append('\n');
        sb.Append("flags ").Append(deck.ShipFixtures).Append(' ').Append(deck.FollowCam).Append(' ')
          .Append(deck.AppendedRegionCount).Append('\n');
        sb.Append("ink ").Append(deck.HullInk).Append('|').Append(deck.StoneInk).Append('|')
          .Append(deck.DoorInk).Append('\n');

        for (int x = -40; x <= 40; x += 5)
        {
            for (int y = -40; y <= 90; y += 5)
            {
                sb.Append("room ").Append(x).Append(' ').Append(y).Append(' ')
                  .Append(deck.Location(x, y)).Append('\n');
            }
        }
        return sb.ToString();
    }

    private static string N(double v) => v.ToString("R", CultureInfo.InvariantCulture);
}
