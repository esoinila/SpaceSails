using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using SpaceSails.Core;
using SpaceSails.Core.Interior;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #794 slice 1 · THE CHALK MARK — the faceless trade's return leg, as pure arithmetic.
///
/// <para>Fable's design on #794 (2026-09-27): a paid delivery earns one return, left under one bench of the
/// park under the ground that was dug, on a schedule — every third watch, the mark up for the window, the
/// goods exposed one watch after the wipe. Every guard here walks the REAL park the generator carves on the
/// real field, and each was watched go RED against a revert of the behaviour it names (quoted in the PR
/// body for #794).</para>
/// </summary>
public sealed class TheChalkMarkTests
{
    private static SurfaceLayout.Field Field => SurfaceLayout.DefaultField;

    /// <summary>Every moon sol.json ships, read off the file rather than typed — the same pool the desk's
    /// parcels are addressed from (<see cref="ShuttleExcursion.IsLandableSurface"/> is "a moon").</summary>
    private static IReadOnlyList<string> SolMoons()
    {
        string path = Path.Combine(TestTree.RepoRoot(), "scenarios", "sol.json");
        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
        var moons = new List<string>();
        foreach (JsonElement b in doc.RootElement.GetProperty("bodies").EnumerateArray())
        {
            if (b.TryGetProperty("kind", out JsonElement k) && k.GetString() == "moon")
            {
                moons.Add(b.GetProperty("id").GetString()!);
            }
        }
        return moons;
    }

    /// <summary>Every park ground in sol.json — each moon whose complex, were it there, keeps the green —
    /// with its park, built by the real generator.</summary>
    private static IEnumerable<(string Body, UndergroundComplex.Park Park)> EveryParkGround()
    {
        foreach (string body in SolMoons())
        {
            if (ChalkMark.TheParkUnder(body, Field, forcePresent: true) is { } park)
            {
                yield return (body, park);
            }
        }
    }

    private static IEnumerable<string> Parcels(int n) =>
        Enumerable.Range(0, n).Select(i => UnlistedParcel.FromTheDesk("selene-gate", 1000 + i).Id);

    private static double At(long watch) => (watch * PatronRota.WatchSeconds) + 60.0;

    // ── THE CLOCK ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE MARK IS NEVER UP OUTSIDE A WINDOW — and a window is every third watch from the payment, never
    /// before it.
    /// </summary>
    [Fact]
    public void TheMarkIsNeverUpOutsideAWindow()
    {
        (string body, UndergroundComplex.Park park) = EveryParkGround().First();
        var wrong = new List<string>();
        int ups = 0;
        foreach (string parcel in Parcels(60))
        {
            const long paid = 500;
            ChalkMark mark = ChalkMark.For(parcel, body, paid, in park)!.Value;
            var up = new List<long>();
            for (long w = paid - 6; w < paid + 30; w++)
            {
                if (mark.MarkIsUpAt(At(w)))
                {
                    up.Add(w);
                }
            }
            ups += up.Count;

            // The schedule, said independently of the implementation: the first window is the payment's
            // own watch or one of the next two, and every window after it is exactly three watches on.
            if (up.Count != 10)
            {
                wrong.Add($"{parcel}: {up.Count} windows in 30 watches, not 10");
            }
            if (up.Count > 0 && (up[0] < paid || up[0] - paid >= ChalkMark.WatchesBetweenWindows))
            {
                wrong.Add($"{parcel}: first chalk on watch {up[0]}, payment on {paid}");
            }
            for (int i = 1; i < up.Count; i++)
            {
                if (up[i] - up[i - 1] != ChalkMark.WatchesBetweenWindows)
                {
                    wrong.Add($"{parcel}: chalk on {up[i - 1]} and again on {up[i]}");
                }
            }
        }

        Assert.True(ups > 0, "no mark was ever up — this proves nothing.");
        Assert.True(wrong.Count == 0, string.Join("\n", wrong.Take(20)));
    }

    /// <summary>
    /// THE GOODS OUTLIVE THE MARK BY EXACTLY ONE WATCH: up and loaded on the window; wiped and exposed the
    /// watch after; gone the watch after that.
    /// </summary>
    [Fact]
    public void TheGoodsOutliveTheMarkByExactlyOneWatch()
    {
        (string body, UndergroundComplex.Park park) = EveryParkGround().First();
        var wrong = new List<string>();
        foreach (string parcel in Parcels(40))
        {
            ChalkMark mark = ChalkMark.For(parcel, body, 900, in park)!.Value;
            for (long win = mark.Window; win < mark.Window + 12; win += ChalkMark.WatchesBetweenWindows)
            {
                int markWatches = 0, goodsWatches = 0;
                for (long w = win; w < win + ChalkMark.WatchesBetweenWindows; w++)
                {
                    markWatches += mark.MarkIsUpAt(At(w)) ? 1 : 0;
                    goodsWatches += mark.GoodsAreThereAt(At(w)) ? 1 : 0;
                    if (mark.MarkIsUpAt(At(w)) && !mark.GoodsAreThereAt(At(w)))
                    {
                        wrong.Add($"{parcel} watch {w}: a mark over nothing");
                    }
                }
                if (goodsWatches - markWatches != 1 || markWatches != 1)
                {
                    wrong.Add($"{parcel} window {win}: mark {markWatches} watch(es), goods {goodsWatches}");
                }
                if (!mark.GoodsAreThereAt(At(win + 1)) || mark.MarkIsUpAt(At(win + 1)))
                {
                    wrong.Add($"{parcel} window {win}: the watch after is not 'wiped, goods exposed'");
                }
            }
            if (mark.GoodsAreThereAt(At(mark.Window - 1)))
            {
                wrong.Add($"{parcel}: goods before the first window");
            }
        }
        Assert.True(wrong.Count == 0, string.Join("\n", wrong.Take(20)));
    }

    // ── WHERE ───────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE BENCH ORDINAL IS ONE OF THE PARK'S BENCHES ON EVERY PARK GROUND IN SOL.JSON — a free one (never
    /// the lone figure's), counted from the notice's gate, and the instruction names it in words.
    /// </summary>
    [Fact]
    public void TheBenchIsOneOfTheParksBenchesOnEveryParkGround()
    {
        var grounds = EveryParkGround().ToList();
        Assert.True(grounds.Count >= 5, $"only {grounds.Count} park grounds in sol.json — this proves little.");

        var wrong = new List<string>();
        var benchesUsed = new HashSet<(string, int)>();
        foreach ((string body, UndergroundComplex.Park park) in grounds)
        {
            IReadOnlyList<ParkBenches.Bench> benches = ParkBenches.On(in park);
            foreach (string parcel in Parcels(40))
            {
                if (ChalkMark.For(parcel, body, 77, in park) is not { } mark)
                {
                    wrong.Add($"{body} {parcel}: no bench at all");
                    continue;
                }
                benchesUsed.Add((body, mark.Bench));
                if (mark.Bench < 0 || mark.Bench >= benches.Count)
                {
                    wrong.Add($"{body} {parcel}: bench {mark.Bench} of {benches.Count}");
                    continue;
                }
                if (benches[mark.Bench].Taken)
                {
                    wrong.Add($"{body} {parcel}: under the lone figure's bench, which nobody sits on alone");
                }
                if (mark.Ordinal < 1 || mark.Ordinal > ChalkMark.OrdinalWords.Count
                    || mark.Ordinal != ChalkMark.OrdinalFromTheGate(in park, mark.Bench))
                {
                    wrong.Add($"{body} {parcel}: ordinal {mark.Ordinal} does not name bench {mark.Bench}");
                }
                if (!mark.ThePaymentLine().Contains($"the {ChalkMark.OrdinalWords[mark.Ordinal - 1]} bench from the gate",
                        StringComparison.Ordinal))
                {
                    wrong.Add($"{body} {parcel}: the line does not name the bench");
                }
            }
        }

        Assert.True(wrong.Count == 0, string.Join("\n", wrong.Take(20)));
        Assert.True(benchesUsed.Count > grounds.Count,
            "every parcel landed under the same bench — the seed is not spreading the drop.");
    }

    /// <summary>
    /// COUNTED FROM THE GATE, ALONG ITS OWN SIDE OF THE WALK: walking away from the notice either way, the
    /// benches go first, second, third — measured on the real park, where the gate stands in the middle.
    /// </summary>
    [Fact]
    public void TheBenchesAreCountedOutwardFromTheGate()
    {
        foreach ((string body, UndergroundComplex.Park park) in EveryParkGround())
        {
            var sides = ParkBenches.On(in park)
                .GroupBy(b => Math.Sign(b.X - park.X))
                .ToList();
            foreach (var side in sides)
            {
                var outward = side.OrderBy(b => Math.Abs(b.X - park.X)).ToList();
                for (int i = 0; i < outward.Count; i++)
                {
                    Assert.True(ChalkMark.OrdinalFromTheGate(in park, outward[i].Index) == i + 1,
                        $"{body}: bench {outward[i].Index} is #{i + 1} walking out from the gate, " +
                        $"and the ordinal says {ChalkMark.OrdinalFromTheGate(in park, outward[i].Index)}");
                }
            }
        }
    }

    /// <summary>No building under the ground, no park, no drop — and the head office never has one.</summary>
    [Fact]
    public void NoParkNoDrop()
    {
        Assert.False(ChalkMark.TheGroundKeepsAPark(KaamosLore.IceMoonBodyId, forcePresent: true));
        Assert.Null(ChalkMark.TheParkUnder(KaamosLore.IceMoonBodyId, Field, forcePresent: true));
        Assert.False(ChalkMark.TheGroundKeepsAPark(null));
        foreach (string moon in SolMoons())
        {
            Assert.Equal(SecretLab.Present(moon) && ChalkMark.TheGroundKeepsAPark(moon, forcePresent: true),
                ChalkMark.TheGroundKeepsAPark(moon));
        }
    }

    // ── THE REGISTER ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// NO PAID DELIVERY, NOTHING OWED: a register with every other kind of tag in it — the desk's own quiet
    /// watches among them — yields no return. And one that IS owed yields exactly that one until it is
    /// collected, after which it is gone for good.
    /// </summary>
    [Fact]
    public void OnlyAPaidDeliveryIsOwedAndACollectionEndsIt()
    {
        (string body, UndergroundComplex.Park park) = EveryParkGround().First();
        string parcel = UnlistedParcel.FromTheDesk("selene-gate", 4242).Id;
        var register = new HashSet<string>(StringComparer.Ordinal)
        {
            ParcelDrop.NothingForThisHullOn(12), "room:3:7@12", "fence:selene-gate@5",
        };
        Assert.Empty(ChalkMark.OwedOn(register, body, in park));

        register.Add(ChalkMark.OwedFor(body, parcel, 30));
        IReadOnlyList<ChalkMark> owed = ChalkMark.OwedOn(register, body, in park);
        Assert.Single(owed);
        Assert.Equal(parcel, owed[0].ParcelId);
        Assert.Equal(30, owed[0].PaidWatch);
        Assert.Empty(ChalkMark.OwedOn(register, body + "-elsewhere", in park));

        string collected = owed[0].CollectedOn(34);
        Assert.Equal($"{ChalkMark.CollectedTag}:{parcel}@34", collected);
        register.Add(collected);
        Assert.Empty(ChalkMark.OwedOn(register, body, in park));
    }

    /// <summary>
    /// THE WALL SAYS THE MARK ONCE PER WINDOW, THE WIPE ONCE — and only of a mark the captain SAW. A wipe
    /// nobody saw is never told (the #649 discipline: you never find out which).
    /// </summary>
    [Fact]
    public void TheWallTellsTheMarkOnceAndOnlyASeenWipe()
    {
        (string body, UndergroundComplex.Park park) = EveryParkGround().First();
        ChalkMark mark = ChalkMark.For(UnlistedParcel.FromTheDesk("selene-gate", 7).Id, body, 100, in park)!.Value;
        long win = mark.Window;
        var register = new HashSet<string>(StringComparer.Ordinal);

        // A wipe nobody saw: the watch after the first window, with nothing seen.
        Assert.Null(mark.AtTheGate(At(win + 1), register));

        // Up, told once.
        ChalkMark.GateBeat? up = mark.AtTheGate(At(win + ChalkMark.WatchesBetweenWindows), register);
        Assert.NotNull(up);
        Assert.Equal(ChalkMark.MarkIsUpLine, up!.Value.Line);
        register.Add(up.Value.Tag);
        Assert.Null(mark.AtTheGate(At(win + ChalkMark.WatchesBetweenWindows), register));

        // Wiped after being seen: told once, then silence.
        long after = win + ChalkMark.WatchesBetweenWindows + 1;
        ChalkMark.GateBeat? wiped = mark.AtTheGate(At(after), register);
        Assert.NotNull(wiped);
        Assert.Equal(ChalkMark.WipedLine, wiped!.Value.Line);
        register.Add(wiped.Value.Tag);
        Assert.Null(mark.AtTheGate(At(after), register));
        Assert.Null(mark.AtTheGate(At(after + 1), register));
    }

    // ── THE BENCH ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE MOVE IS ABSENT ON THE WRONG BENCH AND ON A SHARED ONE — absent, not greyed: the card is the plain
    /// bench's, move for move, whenever the captain may not feel for anything.
    /// </summary>
    [Fact]
    public void TheMoveIsAbsentOnTheWrongBenchAndOnASharedOne()
    {
        (string body, UndergroundComplex.Park park) = EveryParkGround().First();
        ChalkMark mark = ChalkMark.For(UnlistedParcel.FromTheDesk("selene-gate", 9).Id, body, 200, in park)!.Value;
        double loaded = At(mark.Window);

        foreach (ParkBenches.Bench b in ParkBenches.On(in park))
        {
            foreach (bool shared in new[] { false, true })
            {
                bool offer = mark.IsOnOffer(b.Index, shared, loaded);
                Encounter.Scene card = ChalkMark.TheBench(shared, offer);
                bool should = b.Index == mark.Bench && !shared;
                Assert.True(offer == should,
                    $"bench {b.Index} shared={shared}: on offer {offer}, should be {should}");
                Assert.Equal(should, ChalkMark.Offers(card));
                if (!should)
                {
                    Assert.Equal(
                        ParkBenches.TheBench(shared).Moves.Select(m => m.Id),
                        card.Moves.Select(m => m.Id));
                }
            }
        }

        // …and on the right bench, alone, with nothing under it: absent as well.
        long empty = mark.Window + 2;
        Assert.False(mark.IsOnOffer(mark.Bench, shared: false, At(empty)));
        Assert.False(ChalkMark.Offers(ChalkMark.TheBench(shared: true, goodsUnderThisSlat: true)));

        Encounter.Scene with = ChalkMark.TheBench(shared: false, goodsUnderThisSlat: true);
        Encounter.Move feel = with.Moves.Single(m => m.Id == ChalkMark.FeelUnderTheSlat);
        Assert.Equal(ChalkMark.FeelUnderTheSlatLabel, feel.Label);
        Assert.Equal(ChalkMark.FeltLine, feel.Says);
        Assert.Equal(SittingAlone.Stand, with.Moves[^1].Id);
    }

    /// <summary>What is under the slat is a parcel on the existing rail, addressed to another ground.</summary>
    [Fact]
    public void WhatIsUnderTheSlatRidesTheRail()
    {
        IReadOnlyList<string> pool = SolMoons();
        foreach ((string body, UndergroundComplex.Park park) in EveryParkGround())
        {
            foreach (string parcel in Parcels(10))
            {
                ChalkMark mark = ChalkMark.For(parcel, body, 3, in park)!.Value;
                Satchel.Item back = mark.TheParcelUnderTheSlat(pool);
                Assert.True(UnlistedParcel.IsAParcel(back));
                Assert.NotEqual(parcel, back.Id);
                ParcelDrop.Destination? where = ParcelDrop.For(back, pool);
                Assert.NotNull(where);
                Assert.NotEqual(body, where!.Value.BodyId);
            }
        }
    }

    // ── THE GLYPH ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE MARK IS FILED UNDER A KIND OF ITS OWN — #1063's guard, copied: no other published Core glyph, and
    /// exactly one file in src/ that wears it.
    /// </summary>
    [Fact]
    public void TheMarkIsFiledUnderAKindOfItsOwn()
    {
        var taken = new List<(string Where, string Glyph)>();
        foreach (Type type in typeof(ChalkMark).Assembly.GetTypes())
        {
            foreach (FieldInfo f in type.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (!f.IsLiteral || f.FieldType != typeof(string)
                    || !f.Name.Contains("Glyph", StringComparison.Ordinal)
                    || type == typeof(ChalkMark))
                {
                    continue;
                }
                if (f.GetRawConstantValue() is string g && g.Length > 0)
                {
                    taken.Add(($"{type.Name}.{f.Name}", g));
                }
            }
        }

        Assert.True(taken.Count >= 10, $"only {taken.Count} published glyphs were swept — this proves little.");
        List<string> clashes = [.. taken.Where(t => t.Glyph == ChalkMark.Glyph).Select(t => t.Where)];
        Assert.True(clashes.Count == 0, "the chalk mark shares its kind with: " + string.Join(", ", clashes));

        string root = TestTree.RepoRoot();
        List<string> wearers =
        [
            .. Directory
                .EnumerateFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories)
                .Concat(Directory.EnumerateFiles(Path.Combine(root, "src"), "*.razor", SearchOption.AllDirectories))
                .Select(p => Path.GetRelativePath(root, p).Replace('\\', '/'))
                .Where(rel => !rel.Contains("/obj/", StringComparison.Ordinal)
                              && !rel.Contains("/bin/", StringComparison.Ordinal))
                .Where(rel => File.ReadAllText(Path.Combine(root, rel))
                              .Contains(ChalkMark.Glyph, StringComparison.Ordinal))
                .OrderBy(rel => rel, StringComparer.Ordinal),
        ];
        Assert.Equal(["src/SpaceSails.Core/ChalkMark.cs"], wearers);
    }

    // ── THE LINES ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The payment line, with the design's own example bench, is the design's sentence verbatim.</summary>
    [Fact]
    public void ThePaymentLineIsTheCanonSentence()
    {
        ChalkMark third = new("p", "b", 0, 0, 3, 0);
        Assert.Equal(
            "There is something for you where you dug. The park, the third bench from the gate. "
            + "Watch the wall by the notice.",
            third.ThePaymentLine());
    }

    /// <summary>The dev start's two clocks land where they say: a window, and the watch after one.</summary>
    [Fact]
    public void TheDevStartLandsOnTheWindowItAskedFor()
    {
        (string body, UndergroundComplex.Park park) = EveryParkGround().First();
        foreach (string parcel in Parcels(20))
        {
            const long now = 321;
            ChalkMark up = ChalkMark.For(parcel, body, ChalkMark.PaidWatchFor(ChalkMark.Cheat.Up, parcel, body, now), in park)!.Value;
            Assert.True(up.MarkIsUpAt(At(now)));
            ChalkMark wiped = ChalkMark.For(parcel, body, ChalkMark.PaidWatchFor(ChalkMark.Cheat.Wiped, parcel, body, now), in park)!.Value;
            Assert.False(wiped.MarkIsUpAt(At(now)));
            Assert.True(wiped.GoodsAreThereAt(At(now)));
            Assert.Equal(now - 1, wiped.LastWindowBefore(now));
        }

        Assert.Equal(ChalkMark.Cheat.Up, ChalkMark.CheatIn("https://x/map?park=1&chalk=1"));
        Assert.Equal(ChalkMark.Cheat.Wiped, ChalkMark.CheatIn("https://x/map?park=1&chalk=wiped"));
        Assert.Equal(ChalkMark.Cheat.None, ChalkMark.CheatIn("https://x/map?park=1"));
        Assert.Equal(ChalkMark.Cheat.None, ChalkMark.CheatIn(null));
    }
}
