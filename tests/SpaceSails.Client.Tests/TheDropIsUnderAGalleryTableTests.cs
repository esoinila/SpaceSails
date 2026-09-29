using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;
using Xunit;
using static SpaceSails.Client.Tests.CastawayBench;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #794 slice 2 · <b>THE DROP IS UNDER A GALLERY TABLE</b> — a live page clamped on at Selene Gate, driven
/// through its own members: the room's own frame (<c>AdvanceBarWalkers</c>), the room's own [E] at a table
/// (<c>TryTakeBarTop</c>), the card's own press (<c>TableMoveClicked</c>). Nothing types a coordinate: the
/// tables, the machines, the throat and the walk are all asked of <see cref="HavenInterior"/>.
///
/// <para>What is planted is exactly the record a paid delivery writes — the owed tag, keyed on the haven — and
/// nothing else. Each guard was watched go RED against the revert its summary names (the evidence is in the
/// PR body for #794 slice 2).</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheDropIsUnderAGalleryTableTests
{
    private static Pages.Map Clamped(string canvas)
    {
        Pages.Map map = Boot(canvas);
        var sky = (ICelestialEphemeris)Read(map, "_ephemeris")!;
        CelestialBody berth = sky.Bodies.First(b => b.Id == Port);
        Invoke(map, "ClampOntoHaven", berth, sky.Position(Port, (double)Read(map, "SimTime")!), null);
        Assert.Equal(Port, (string?)Read(map, "_dockedHavenId"));
        Assert.True(HavenInterior.HasObservationWalk(Port));
        return map;
    }

    private static HashSet<string> Register(Pages.Map map) => (HashSet<string>)Read(map, "_roomsTurnedOver")!;

    /// <summary>Plant a paid delivery's return with the clock ON its window (or one watch past it).</summary>
    private static ChalkMark Owe(Pages.Map map, ChalkMark.Cheat cheat = ChalkMark.Cheat.Up)
    {
        long now = PatronRota.WatchIndex((double)Read(map, "SimTime")!);
        string parcel = UnlistedParcel.FromTheDesk("gallery-guard", now).Id;
        ChalkMark mark = ChalkMark.For(
            parcel, Port, ChalkMark.PaidWatchFor(cheat, parcel, Port, now), HavenInterior.GalleryTops(Port).Count)!.Value;
        Register(map).Add(mark.Owed);
        return mark;
    }

    private static void RoomFrame(Pages.Map map) => Invoke(map, "AdvanceBarWalkers", 1.0 / 30.0);

    private static Pages.Map.TableTalk SitAt(Pages.Map map, int table)
    {
        DeckReachability.Point top = HavenInterior.GalleryTops(Port)[table];
        Set(map, "_avatarX", top.X);
        Set(map, "_avatarY", top.Y);
        Assert.True((bool)Invoke(map, "TryTakeBarTop")!, $"gallery table {table} refused [E].");
        return (Pages.Map.TableTalk)Read(map, "SeatedTable")!;
    }

    private static void StandUp(Pages.Map map) => Invoke(map, "CloseTable");

    private static int Filed(Pages.Map map, string line) =>
        ((IEnumerable<FieldNote>)Read(map, "_fieldNotes")!).Count(n => string.Equals(n.Text, line, StringComparison.Ordinal));

    /// <summary>The gallery stands exactly as many tables as the instruction can name, and table i stands in
    /// front of wall machine i — the pairing the cross's placement and the dev line both rely on.</summary>
    [Fact]
    public void TheGallerysTablesAreTheOnesTheInstructionCanName()
    {
        IReadOnlyList<DeckReachability.Point> tops = HavenInterior.GalleryTops(Port);
        IReadOnlyList<(double X0, double Y0, double X1, double Y1)> machines = HavenInterior.TheVendingMachineBlocks(Port);
        Assert.Equal(ChalkMark.OrdinalWords.Count, tops.Count);
        for (int i = 0; i < tops.Count; i++)
        {
            int nearest = Enumerable.Range(0, tops.Count)
                .OrderBy(m => Math.Abs(((machines[m].Y0 + machines[m].Y1) / 2.0) - tops[i].Y))
                .First();
            Assert.Equal(i, nearest);
        }
    }

    /// <summary>
    /// THE MOVE IS AT THE NAMED TABLE, ALONE, AND NOWHERE ELSE: on the card at that table, off it the moment
    /// somebody shares the table, back when they go, and never at the other table. Absent, never greyed —
    /// the card at the other table is the plain table's, move for move.
    /// </summary>
    [Fact]
    public void TheMoveIsAtTheNamedTableAloneAndNowhereElse()
    {
        Pages.Map map = Clamped("chalk-gallery-move");
        ChalkMark mark = Owe(map);

        Pages.Map.TableTalk t = SitAt(map, mark.Table);
        IReadOnlyList<string> plain = [.. t.Scene.Moves.Select(m => m.Id)];
        Assert.False(ChalkMark.Offers(t.Scene));
        RoomFrame(map);
        Assert.True(ChalkMark.Offers(t.Scene), "alone at the named table in the window, and the card has no move.");
        Assert.Equal(ChalkMark.FeelUnderTheLip, t.Scene.Moves[^2].Id);

        t.SharedSeat = true;
        RoomFrame(map);
        Assert.False(ChalkMark.Offers(t.Scene), "somebody shares the table and the move is still on the card.");
        Assert.Equal(plain, t.Scene.Moves.Select(m => m.Id));

        t.SharedSeat = false;
        RoomFrame(map);
        Assert.True(ChalkMark.Offers(t.Scene));

        StandUp(map);
        Pages.Map.TableTalk other = SitAt(map, 1 - mark.Table);
        IReadOnlyList<string> otherPlain = [.. other.Scene.Moves.Select(m => m.Id)];
        for (int i = 0; i < 5; i++)
        {
            RoomFrame(map);
        }
        Assert.False(ChalkMark.Offers(other.Scene), "the move is on the card at the table nothing is under.");
        Assert.Equal(otherPlain, other.Scene.Moves.Select(m => m.Id));
    }

    /// <summary>
    /// THE TAG IS WRITTEN ONCE PER COLLECTION: the press puts one parcel in the satchel, writes one
    /// collection tag, files one line, says the felt line on the card and takes the move off it; a second
    /// press of a stale button writes nothing.
    /// </summary>
    [Fact]
    public async System.Threading.Tasks.Task FeelingUnderTheLipCollectsOnce()
    {
        Pages.Map map = Clamped("chalk-gallery-collect");
        ChalkMark mark = Owe(map);
        Pages.Map.TableTalk t = SitAt(map, mark.Table);
        RoomFrame(map);
        Assert.True(ChalkMark.Offers(t.Scene));

        int parcels = ((IEnumerable<Satchel.Item>)Read(map, "_satchel")!).Count(UnlistedParcel.IsAParcel);
        await (System.Threading.Tasks.Task)Invoke(map, "TableMoveClicked", ChalkMark.FeelUnderTheLip)!;
        await (System.Threading.Tasks.Task)Invoke(map, "TableMoveClicked", ChalkMark.FeelUnderTheLip)!;
        RoomFrame(map);

        Assert.Equal(parcels + 1, ((IEnumerable<Satchel.Item>)Read(map, "_satchel")!).Count(UnlistedParcel.IsAParcel));
        Assert.Single(Register(map), tag => tag.StartsWith($"{ChalkMark.CollectedTag}:{mark.ParcelId}@", StringComparison.Ordinal));
        Assert.Equal(1, Filed(map, ChalkMark.CollectedEntry));
        Assert.Equal(ChalkMark.FeltLine, t.Outcome);
        Assert.False(ChalkMark.Offers(t.Scene), "the move is still on the card after the goods were taken.");
        // Fable, 2026-09-29: the collection takes the goods and the move, never the chalk — the cross is still
        // on the stone until the turnover wipes it.
        Assert.NotNull(Invoke(map, "TheChalkOnTheStone"));
    }

    /// <summary>
    /// THE STONE SAYS IT ONCE, AND ONLY IN THE GALLERY: standing in the hall with the mark up files nothing;
    /// walking into the gallery files the mark's line once, however many frames the captain stands there.
    /// </summary>
    [Fact]
    public void TheStoneSaysItOnceInTheGallery()
    {
        Pages.Map map = Clamped("chalk-gallery-stone");
        Owe(map);

        DeckReachability.Point mouth = HavenInterior.TheWalksMouthAt(Port)!.Value;
        Set(map, "_avatarX", mouth.X + 6.0);
        Set(map, "_avatarY", mouth.Y);
        Assert.False(HavenInterior.InTheObservationWalk(Port, mouth.X + 6.0, mouth.Y));
        RoomFrame(map);
        Assert.Equal(0, Filed(map, ChalkMark.MarkIsUpLine));

        DeckReachability.Point island = HavenInterior.TheVendorsAt(Port)[^1];
        Assert.True(HavenInterior.InTheGallery(Port, island.X, island.Y));
        Set(map, "_avatarX", island.X);
        Set(map, "_avatarY", island.Y);
        for (int i = 0; i < 10; i++)
        {
            RoomFrame(map);
        }
        Assert.Equal(1, Filed(map, ChalkMark.MarkIsUpLine));
    }

    /// <summary>
    /// THE CROSS IS ON THE STONE — on the back wall's run between the corner and the throat's jamb, on the
    /// room's face of it, beside the named table's machine and not on any machine — drawn only while the mark
    /// is up and only on the concourse (the floor below is laid in the same coordinates).
    /// </summary>
    [Fact]
    public void TheCrossIsOnTheStoneBesideTheNamedTablesMachine()
    {
        Pages.Map map = Clamped("chalk-gallery-cross");
        ChalkMark mark = Owe(map);

        var cross = ((double X, double Y)?)Invoke(map, "TheChalkOnTheStone");
        Assert.NotNull(cross);
        (double x, double y) = cross!.Value;
        (double gx0, double gy0, double gx1, double gy1) = HavenInterior.TheGalleryBox(Port)!.Value;
        (double _, double wy0, double _, double wy1) = HavenInterior.TheWalksBox(Port)!.Value;
        Assert.Equal(gx1 - ChalkMark.OnTheFaceDu, x, 6);
        Assert.InRange(y, gy0, gy1);
        Assert.False(y >= wy0 && y <= wy1, "the cross is chalked across the throat, where there is no stone.");
        foreach ((double X0, double Y0, double X1, double Y1) m in HavenInterior.TheVendingMachineBlocks(Port))
        {
            Assert.False(x >= m.X0 && x <= m.X1 && y >= m.Y0 && y <= m.Y1, "the cross is chalked on a machine.");
        }
        IReadOnlyList<(double X0, double Y0, double X1, double Y1)> machines = HavenInterior.TheVendingMachineBlocks(Port);
        int beside = Enumerable.Range(0, HavenInterior.GalleryTops(Port).Count)
            .OrderBy(i => Math.Abs(((machines[i].Y0 + machines[i].Y1) / 2.0) - y)).First();
        Assert.Equal(mark.Table, beside);

        Set(map, "_havenFloor", HavenLevels.ServiceLevel);
        Assert.Null(Invoke(map, "TheChalkOnTheStone"));
    }

    /// <summary>
    /// NO PAID DELIVERY, NOTHING ANYWHERE: with no owed tag the stone draws nothing, the gallery files
    /// nothing, and a gallery table's card is never touched — the very same scene object, frame after frame.
    /// A return owed but out of its window draws nothing either.
    /// </summary>
    [Fact]
    public void NoPaidDeliveryNothingAnywhere()
    {
        Pages.Map map = Clamped("chalk-gallery-nothing");
        Assert.Null(Invoke(map, "TheChalkOnTheStone"));

        for (int table = 0; table < HavenInterior.GalleryTops(Port).Count; table++)
        {
            Pages.Map.TableTalk t = SitAt(map, table);
            Encounter.Scene before = t.Scene;
            for (int i = 0; i < 5; i++)
            {
                RoomFrame(map);
            }
            Assert.Same(before.Moves, t.Scene.Moves);
            Assert.Equal(before, t.Scene);
            StandUp(map);
        }
        Assert.Equal(0, ((IEnumerable<FieldNote>)Read(map, "_fieldNotes")!).Count(n => n.Glyph == ChalkMark.Glyph));
        Assert.DoesNotContain(Register(map), tag => tag.StartsWith("gallery-", StringComparison.Ordinal));

        // …and a return owed but between windows: no cross.
        long now = PatronRota.WatchIndex((double)Read(map, "SimTime")!);
        string parcel = UnlistedParcel.FromTheDesk("gallery-guard", now).Id;
        long paid = ChalkMark.PaidWatchFor(ChalkMark.Cheat.Up, parcel, Port, now) + 1;
        Register(map).Add(ChalkMark.OwedFor(Port, parcel, paid));
        Assert.Null(Invoke(map, "TheChalkOnTheStone"));
    }
}
