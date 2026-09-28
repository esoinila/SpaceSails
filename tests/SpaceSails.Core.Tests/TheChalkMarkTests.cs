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
/// #794 · THE CHALK MARK — the faceless trade's return leg, as pure arithmetic.
///
/// <para>Fable's design on #794 (2026-09-27), moved by the owner's ruling of 2026-09-28 from a park bench to
/// the gallery at the end of Selene Gate's observation walk: a paid delivery earns one return, left under
/// one of the gallery's two tables, on a schedule — every third watch, the mark up for the window, the goods
/// exposed one watch after the wipe. The room's geometry is the client's (<c>HavenInterior.GalleryTops</c>);
/// what is driven here is everything Core decides once it is told how many tables stand there, and
/// <c>TheChalkMarkIsWiredTests</c> in the client suite holds the real room to the same count. Each guard was
/// watched go RED against a revert of the behaviour it names (quoted in the PR body for #794 slice 2).</para>
/// </summary>
public sealed class TheChalkMarkTests
{
    /// <summary>The gallery's two steel tables — the count <c>HavenInterior.GalleryTops</c> publishes, held to
    /// this number by the client suite.</summary>
    private const int Tables = 2;

    private const string Haven = ChalkMark.Haven;

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

    private static IEnumerable<string> Parcels(int n) =>
        Enumerable.Range(0, n).Select(i => UnlistedParcel.FromTheDesk("selene-gate", 1000 + i).Id);

    private static double At(long watch) => (watch * PatronRota.WatchSeconds) + 60.0;

    private static ChalkMark Mark(string parcel, long paid) => ChalkMark.For(parcel, Haven, paid, Tables)!.Value;

    // ── THE CLOCK ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE MARK IS NEVER UP OUTSIDE A WINDOW — and a window is every third watch from the payment, never
    /// before it.
    /// </summary>
    [Fact]
    public void TheMarkIsNeverUpOutsideAWindow()
    {
        var wrong = new List<string>();
        int ups = 0;
        foreach (string parcel in Parcels(60))
        {
            const long paid = 500;
            ChalkMark mark = Mark(parcel, paid);
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
        var wrong = new List<string>();
        foreach (string parcel in Parcels(40))
        {
            ChalkMark mark = Mark(parcel, 900);
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
    /// THE TABLE IS ONE OF THE GALLERY'S TWO, and the instruction names it in words — first or second, by
    /// the room's own order. Both tables are used across parcels, so the seed is spreading the drop.
    /// </summary>
    [Fact]
    public void TheTableIsOneOfTheGallerysTwo()
    {
        var wrong = new List<string>();
        var used = new HashSet<int>();
        foreach (string parcel in Parcels(40))
        {
            if (ChalkMark.For(parcel, Haven, 77, Tables) is not { } mark)
            {
                wrong.Add($"{parcel}: no table at all");
                continue;
            }
            used.Add(mark.Table);
            if (mark.Table < 0 || mark.Table >= Tables)
            {
                wrong.Add($"{parcel}: table {mark.Table} of {Tables}");
                continue;
            }
            if (mark.Ordinal != mark.Table + 1
                || !mark.ThePaymentLine().Contains(
                    $"the {ChalkMark.OrdinalWords[mark.Table]} table.", StringComparison.Ordinal))
            {
                wrong.Add($"{parcel}: the line does not name table {mark.Table}");
            }
            if (mark.HavenId != ObservationWalk.HavenId)
            {
                wrong.Add($"{parcel}: left at {mark.HavenId}, not the haven with the gallery");
            }
        }

        Assert.True(wrong.Count == 0, string.Join("\n", wrong.Take(20)));
        Assert.Equal(Tables, used.Count);
        Assert.Equal(Tables, ChalkMark.OrdinalWords.Count);
    }

    /// <summary>No gallery, no table, no drop — and never a table the instruction cannot name.</summary>
    [Fact]
    public void NoTableNoDrop()
    {
        Assert.Null(ChalkMark.For("p", Haven, 3, 0));
        Assert.Equal(0, ChalkMark.For("p", Haven, 3, 1)!.Value.Table);
        foreach (string parcel in Parcels(20))
        {
            Assert.InRange(ChalkMark.For(parcel, Haven, 3, 7)!.Value.Table, 0, ChalkMark.OrdinalWords.Count - 1);
        }

        var register = new HashSet<string>(StringComparer.Ordinal) { ChalkMark.OwedFor(Haven, "p", 3) };
        Assert.Empty(ChalkMark.OwedOn(register, Haven, 0));
    }

    /// <summary>
    /// THE CROSS IS ON THE STONE BESIDE THE MACHINE, toward the throat — not on the machine, on the room's
    /// face of the wall, whichever end of the room the machine stands at.
    /// </summary>
    [Fact]
    public void TheCrossIsOnTheStoneBesideTheMachineTowardTheThroat()
    {
        // A machine flush to a wall at x = 10, the room to the west; one at each end of a run whose throat
        // is at y = 0.
        (double, double, double, double) north = (8.0, 6.0, 10.0, 8.0);
        (double, double, double, double) south = (8.0, -8.0, 10.0, -6.0);

        (double nx, double ny) = ChalkMark.WhereOnTheStone(north, 0.0);
        (double sx, double sy) = ChalkMark.WhereOnTheStone(south, 0.0);

        Assert.Equal(10.0 - ChalkMark.OnTheFaceDu, nx, 9);
        Assert.Equal(6.0 - ChalkMark.BesideTheMachineDu, ny, 9);
        Assert.Equal(10.0 - ChalkMark.OnTheFaceDu, sx, 9);
        Assert.Equal(-6.0 + ChalkMark.BesideTheMachineDu, sy, 9);
    }

    // ── THE REGISTER ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// NO PAID DELIVERY, NOTHING OWED: a register with every other kind of tag in it — the desk's own quiet
    /// watches among them — yields no return. And one that IS owed yields exactly that one until it is
    /// collected, after which it is gone for good. Owed at the gallery's haven and nowhere else.
    /// </summary>
    [Fact]
    public void OnlyAPaidDeliveryIsOwedAndACollectionEndsIt()
    {
        string parcel = UnlistedParcel.FromTheDesk("selene-gate", 4242).Id;
        var register = new HashSet<string>(StringComparer.Ordinal)
        {
            ParcelDrop.NothingForThisHullOn(12), "room:3:7@12", "fence:selene-gate@5",
        };
        Assert.Empty(ChalkMark.OwedOn(register, Haven, Tables));

        register.Add(ChalkMark.OwedFor(Haven, parcel, 30));
        Assert.StartsWith("gallery-owed:selene-gate|", ChalkMark.OwedFor(Haven, parcel, 30), StringComparison.Ordinal);
        IReadOnlyList<ChalkMark> owed = ChalkMark.OwedOn(register, Haven, Tables);
        Assert.Single(owed);
        Assert.Equal(parcel, owed[0].ParcelId);
        Assert.Equal(30, owed[0].PaidWatch);
        Assert.Empty(ChalkMark.OwedOn(register, "the-space-bar", Tables));

        string collected = owed[0].CollectedOn(34);
        Assert.Equal($"gallery-drop:{parcel}@34", collected);
        register.Add(collected);
        Assert.Empty(ChalkMark.OwedOn(register, Haven, Tables));
    }

    /// <summary>
    /// THE PARSERS STILL READ SLICE 1'S NAMES. No shipped save carries a park tag, but one that did would
    /// still be owed, still be told once and still end at its collection.
    /// </summary>
    [Fact]
    public void TheParksTagNamesAreStillRead()
    {
        Assert.Equal(["park-owed", "park-drop", "park-chalk-seen", "park-chalk-wiped"], ChalkMark.ParkTags);

        const string parcel = "old";
        var register = new HashSet<string>(StringComparer.Ordinal) { $"park-owed:{Haven}|{parcel}@30" };
        ChalkMark mark = Assert.Single(ChalkMark.OwedOn(register, Haven, Tables));
        Assert.Equal(parcel, mark.ParcelId);

        register.Add($"park-chalk-seen:{parcel}@{mark.Window}");
        Assert.Null(mark.InTheGallery(At(mark.Window), register));

        register.Add($"park-drop:{parcel}@{mark.Window}");
        Assert.Empty(ChalkMark.OwedOn(register, Haven, Tables));
    }

    /// <summary>
    /// THE STONE SAYS THE MARK ONCE PER WINDOW, THE WIPE ONCE — and only of a mark the captain SAW. A wipe
    /// nobody saw is never told (the #649 discipline: you never find out which).
    /// </summary>
    [Fact]
    public void TheStoneTellsTheMarkOnceAndOnlyASeenWipe()
    {
        ChalkMark mark = Mark(UnlistedParcel.FromTheDesk("selene-gate", 7).Id, 100);
        long win = mark.Window;
        var register = new HashSet<string>(StringComparer.Ordinal);

        // A wipe nobody saw: the watch after the first window, with nothing seen.
        Assert.Null(mark.InTheGallery(At(win + 1), register));

        // Up, told once.
        ChalkMark.StoneBeat? up = mark.InTheGallery(At(win + ChalkMark.WatchesBetweenWindows), register);
        Assert.NotNull(up);
        Assert.Equal(ChalkMark.MarkIsUpLine, up!.Value.Line);
        register.Add(up.Value.Tag);
        Assert.Null(mark.InTheGallery(At(win + ChalkMark.WatchesBetweenWindows), register));

        // Wiped after being seen: told once, then silence.
        long after = win + ChalkMark.WatchesBetweenWindows + 1;
        ChalkMark.StoneBeat? wiped = mark.InTheGallery(At(after), register);
        Assert.NotNull(wiped);
        Assert.Equal(ChalkMark.WipedLine, wiped!.Value.Line);
        register.Add(wiped.Value.Tag);
        Assert.Null(mark.InTheGallery(At(after), register));
        Assert.Null(mark.InTheGallery(At(after + 1), register));
    }

    // ── THE TABLE ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE MOVE IS ABSENT AT THE OTHER TABLE AND AT A SHARED ONE — absent, not greyed: the card is the plain
    /// table's, move for move, whenever the captain may not feel for anything. In every register the table
    /// card comes in, and idempotent both ways.
    /// </summary>
    [Fact]
    public void TheMoveIsAbsentAtTheOtherTableAndAtASharedOne()
    {
        ChalkMark mark = Mark(UnlistedParcel.FromTheDesk("selene-gate", 9).Id, 200);
        double loaded = At(mark.Window);

        foreach (Encounter.Scene plain in new[]
                 {
                     SittingAlone.TheTable(), SittingAlone.TheTable(relaxed: true),
                     SittingAlone.TheTable(relaxed: true, drinkInHand: true),
                 })
        {
            for (int table = 0; table < Tables; table++)
            {
                foreach (bool alone in new[] { true, false })
                {
                    bool offer = mark.IsOnOffer(table, alone, loaded);
                    Encounter.Scene card = ChalkMark.TheTable(plain, offer);
                    bool should = table == mark.Table && alone;
                    Assert.True(offer == should, $"table {table} alone={alone}: on offer {offer}, should be {should}");
                    Assert.Equal(should, ChalkMark.Offers(card));
                    if (!should)
                    {
                        Assert.Equal(plain.Moves.Select(m => m.Id), card.Moves.Select(m => m.Id));
                    }
                }
            }

            Encounter.Scene with = ChalkMark.TheTable(plain, goodsUnderThisLip: true);
            Assert.Equal(plain.Moves.Count + 1, with.Moves.Count);
            Encounter.Move feel = with.Moves.Single(m => m.Id == ChalkMark.FeelUnderTheLip);
            Assert.Equal(ChalkMark.FeelUnderTheLipLabel, feel.Label);
            Assert.Equal(ChalkMark.FeltLine, feel.Says);
            Assert.Equal(SittingAlone.Stand, with.Moves[^1].Id);
            Assert.Single(ChalkMark.TheTable(with, goodsUnderThisLip: true).Moves, m => m.Id == ChalkMark.FeelUnderTheLip);
            Assert.Equal(plain.Moves.Select(m => m.Id),
                ChalkMark.TheTable(with, goodsUnderThisLip: false).Moves.Select(m => m.Id));
        }

        // …and at the right table, alone, with nothing under it: absent as well.
        Assert.False(mark.IsOnOffer(mark.Table, alone: true, At(mark.Window + 2)));
    }

    /// <summary>
    /// THE PARK LOST THE DROP: its bench card is its own two moves again (SIT A WHILE, Stand up), and nothing
    /// in the chalk mark can be asked about a park any more — no overload takes one, and no member builds a
    /// bench's card. The mechanic MOVED; it is not in two places.
    /// </summary>
    [Fact]
    public void TheParksBenchCardHasExactlyTwoMovesAgain()
    {
        foreach (bool shared in new[] { false, true })
        {
            Assert.Equal([SittingAlone.Wait, SittingAlone.Stand], ParkBenches.TheBench(shared).Moves.Select(m => m.Id));
        }

        var parkish = new List<string>();
        foreach (MethodInfo m in typeof(ChalkMark).GetMethods(
                     BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance
                     | BindingFlags.DeclaredOnly))
        {
            foreach (ParameterInfo p in m.GetParameters())
            {
                Type t = p.ParameterType.IsByRef ? p.ParameterType.GetElementType()! : p.ParameterType;
                if (t == typeof(UndergroundComplex.Park) || t == typeof(ParkBenches.Bench))
                {
                    parkish.Add($"{m.Name}({p.Name})");
                }
            }
            if (!m.IsSpecialName
                && (m.Name.Contains("Bench", StringComparison.Ordinal) || m.Name.Contains("Park", StringComparison.Ordinal)))
            {
                parkish.Add(m.Name);
            }
        }
        Assert.True(parkish.Count == 0, "the chalk mark still answers about a park: " + string.Join(", ", parkish));
    }

    /// <summary>What is under the lip is a parcel on the existing rail, addressed to another ground than the
    /// one the delivery that earned it was buried at.</summary>
    [Fact]
    public void WhatIsUnderTheLipRidesTheRail()
    {
        IReadOnlyList<string> pool = SolMoons();
        Assert.True(pool.Count >= 5, $"only {pool.Count} moons in sol.json — this proves little.");
        foreach (string parcel in Parcels(30))
        {
            ChalkMark mark = Mark(parcel, 3);
            Satchel.Item back = mark.TheParcelUnderTheSlat(pool);
            Assert.True(UnlistedParcel.IsAParcel(back));
            Assert.NotEqual(parcel, back.Id);
            ParcelDrop.Destination? where = ParcelDrop.For(back, pool);
            ParcelDrop.Destination? dug = ParcelDrop.For(parcel, pool);
            Assert.NotNull(where);
            Assert.NotNull(dug);
            Assert.NotEqual(dug!.Value.BodyId, where!.Value.BodyId);
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

    /// <summary>The payment line, for both tables, is the design's sentence verbatim.</summary>
    [Fact]
    public void ThePaymentLineIsTheCanonSentence()
    {
        Assert.Equal(
            "There is something for you at Selene Gate. The gallery at the end of the walk, the first table. "
            + "Watch the stone by the machines.",
            new ChalkMark("p", Haven, 0, 0, 0).ThePaymentLine());
        Assert.Equal(
            "There is something for you at Selene Gate. The gallery at the end of the walk, the second table. "
            + "Watch the stone by the machines.",
            new ChalkMark("p", Haven, 0, 1, 0).ThePaymentLine());
    }

    /// <summary>Every other line the slice can put on a screen is the brief's, verbatim.</summary>
    [Fact]
    public void EveryLineIsTheBriefsVerbatim()
    {
        Assert.Equal(
            "Somebody has chalked the stone beside the machines. A cross, waist-high, the width of a hand. "
            + "The crew that keeps this gallery clean will file it as damage by the next watch.",
            ChalkMark.MarkIsUpLine);
        Assert.Equal("The stone is clean. Somebody wiped it, or somebody read it. The stone does not say.",
            ChalkMark.WipedLine);
        Assert.Equal("FEEL UNDER THE LIP", ChalkMark.FeelUnderTheLipLabel);
        Assert.Equal(
            "Tape, cold. A packet the size of a hand, wrapped so it does not rattle. Nobody on the walk looks round.",
            ChalkMark.FeltLine);
        Assert.Equal(
            "Collected under the gallery's table. Whoever left it keeps the walk's hours better than the walk does.",
            ChalkMark.CollectedEntry);
        Assert.Equal("🖍", ChalkMark.Glyph);
        Assert.Equal(6, ChalkMark.AllProse().Count());
    }

    /// <summary>The dev start's two clocks land where they say: a window, and the watch after one.</summary>
    [Fact]
    public void TheDevStartLandsOnTheWindowItAskedFor()
    {
        foreach (string parcel in Parcels(20))
        {
            const long now = 321;
            ChalkMark up = Mark(parcel, ChalkMark.PaidWatchFor(ChalkMark.Cheat.Up, parcel, Haven, now));
            Assert.True(up.MarkIsUpAt(At(now)));
            ChalkMark wiped = Mark(parcel, ChalkMark.PaidWatchFor(ChalkMark.Cheat.Wiped, parcel, Haven, now));
            Assert.False(wiped.MarkIsUpAt(At(now)));
            Assert.True(wiped.GoodsAreThereAt(At(now)));
            Assert.Equal(now - 1, wiped.LastWindowBefore(now));
        }

        Assert.Equal(ChalkMark.Cheat.Up, ChalkMark.CheatIn("https://x/map?dock=selene-gate&ashore=1&chalk=1"));
        Assert.Equal(ChalkMark.Cheat.Wiped, ChalkMark.CheatIn("https://x/map?dock=selene-gate&ashore=1&chalk=wiped"));
        Assert.Equal(ChalkMark.Cheat.None, ChalkMark.CheatIn("https://x/map?dock=selene-gate&ashore=1"));
        Assert.Equal(ChalkMark.Cheat.None, ChalkMark.CheatIn(null));
    }
}
