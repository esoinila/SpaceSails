using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #709 · THE CORK BOARD, AND THE PERSON WHOSE NOTICE IT IS.
///
/// <para>Owner: <i>"let's add a bulletin board to the bar"</i> · <i>"maybe spot the person notifying in the
/// bar."</i></para>
///
/// <para>The second sentence is the feature. Every notice belongs to somebody sitting in that room, and
/// <b>nothing in the game ever says so</b> — so the only thing holding it together is that the authoring is
/// actually consistent. That is what these are for: the connection the player is invited to make must really
/// be there to be made.</para>
/// </summary>
public sealed class TheBoardInTheBarTests
{
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
        "secret-lab-site", "secret-lab-site-unlisted",
    ];

    private static IEnumerable<string> Sweep() =>
        Bodies.Concat(Enumerable.Range(0, 60).Select(i => $"probe-moon-{i}"));

    private static SurfaceLayout.Field Field => SurfaceLayout.DefaultField;

    [Fact]
    public void EveryNoticeBelongsToSomebodyWhoCouldHavePINNEDIt()
    {
        // THE WHOLE FEATURE, and the only guard that can protect it. The pairing is never rendered, never
        // spoken and never filed — so if an author adds a notice about a thing nobody in the room does, the
        // room quietly stops being consistent and no player will ever be able to tell us why the board feels
        // like set dressing.
        var cast = CanteenRegulars.AllProse().Where((_, i) => i % 2 == 0).ToList();

        foreach (string pairing in CanteenBoard.AllPairings())
        {
            Assert.Contains(pairing, cast);
        }

        // One notice each, and every one of the cast accounted for: a regular with no notice is a person the
        // board forgot, and two notices for one person is a board that repeats itself.
        var pairings = CanteenBoard.AllPairings().ToList();
        Assert.Equal(pairings.Count, pairings.Distinct().Count());
        Assert.Equal(CanteenRegulars.CastSize, CanteenBoard.CatalogSize);
        Assert.Equal(cast.OrderBy(s => s, StringComparer.Ordinal),
                     pairings.OrderBy(s => s, StringComparer.Ordinal));
    }

    [Fact]
    public void NoNoticeEverNAMESThePersonWhoPinnedIt()
    {
        // The other half of the same law. Consistency is offered; the answer is not. A notice that named its
        // author would spend the only thing this object has, and it would do it silently — the text would
        // still read fine.
        foreach (string line in CanteenBoard.AllProse())
        {
            foreach (string plate in CanteenBoard.AllPairings())
            {
                // The plates are shouted role descriptions ("A FITTER, OFF A MAINTENANCE CONTRACT"); no notice
                // may quote one, in either case.
                string role = plate.TrimStart('◈', ' ');
                Assert.DoesNotContain(role, line, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void TheBoardHangsOnlyWhereThePeopleAre()
    {
        // Same B1 law as the cast, and forced rather than found — the lesson this PR's sibling learned the
        // hard way (two guards that masked each other and a sweep that could not tell pass from fail).
        string body = Bodies[0];
        int top = UndergroundComplex.TopPressurisedFloor(body)
            ?? throw new InvalidOperationException($"{body} has no pressurised floor.");
        IReadOnlyList<(double X, double Y)> tables = [(10.0, 20.0), (30.0, 40.0)];

        UndergroundComplex.Amenity Make(UndergroundComplex.Comfort use) =>
            new(use, 0, 0, "PLATE", "FIXTURE", tables);

        Assert.Empty(CanteenBoard.Pinned(body, top, Make(UndergroundComplex.Comfort.StaffCanteen)));
        Assert.Empty(CanteenBoard.Pinned(body, top, Make(UndergroundComplex.Comfort.Washroom)));
        Assert.Null(CanteenBoard.At(body, top, Make(UndergroundComplex.Comfort.StaffCanteen)));

        foreach (int below in UndergroundComplex.FloorsOf(body).Where(l => l != top))
        {
            Assert.Empty(CanteenBoard.Pinned(body, below, Make(UndergroundComplex.Comfort.UpperCanteen)));
            Assert.Null(CanteenBoard.At(body, below, Make(UndergroundComplex.Comfort.UpperCanteen)));
        }

        // And it DOES hang where it should, or every assertion above passes on a function returning nothing.
        Assert.NotEmpty(CanteenBoard.Pinned(body, top, Make(UndergroundComplex.Comfort.UpperCanteen)));
        Assert.NotNull(CanteenBoard.At(body, top, Make(UndergroundComplex.Comfort.UpperCanteen)));
    }

    [Fact]
    public void FourDifferentNoticesEverySiteAndTheSameFourEveryVisit()
    {
        int boards = 0;
        foreach (string body in Sweep())
        {
            if (UndergroundComplex.TopPressurisedFloor(body) is not { } level)
            {
                continue;
            }

            foreach (UndergroundComplex.Amenity a in UndergroundComplex.Build(body, level, Field).Amenities
                .Where(a => a.Use == UndergroundComplex.Comfort.UpperCanteen))
            {
                var up = CanteenBoard.Pinned(body, level, a);
                boards++;

                Assert.Equal(CanteenBoard.PinnedAtOnce, up.Count);
                Assert.Equal(up.Count, up.Select(n => n.Head).Distinct().Count());
                Assert.Equal(up, CanteenBoard.Pinned(body, level, a));
            }
        }

        Assert.True(boards > 20, $"only {boards} boards in the sweep — nothing was proved.");
    }

    [Fact]
    public void TheBoardOwnsItsOwnPatchOfWallAndCrowdsNOTHING()
    {
        // ConsoleCrowdingTests wants 2 du between consoles, and this board shares a small room with the
        // amenity console and up to three seated people. The offsets are chosen, not guessed — so they get
        // checked, here, where the arithmetic lives rather than in a client sweep that does not reach B1.
        foreach (string body in Bodies)
        {
            if (UndergroundComplex.TopPressurisedFloor(body) is not { } level)
            {
                continue;
            }

            foreach (UndergroundComplex.Amenity a in UndergroundComplex.Build(body, level, Field).Amenities
                .Where(a => a.Use == UndergroundComplex.Comfort.UpperCanteen))
            {
                (double bx, double by) = CanteenBoard.At(body, level, a)!.Value;

                // EVERY table, not merely the occupied ones. Since the rota (#709) the roster and the seating
                // turn over with the shift, so any table may hold somebody on some watch — checking only the
                // watch-0 occupants would be a guard that passes today and ships a crowded console tomorrow.
                var others = new List<(double X, double Y, string What)> { (a.X, a.Y, "the fixture console") };
                others.AddRange(a.Tables.Select(t => (t.X, t.Y, "a table")));

                foreach ((double ox, double oy, string what) in others)
                {
                    double gap = Math.Sqrt(((bx - ox) * (bx - ox)) + ((by - oy) * (by - oy)));
                    Assert.True(gap >= 2.0,
                        $"{body} B{-level}: the board is {gap:F2} du from {what} — under the 2 du clearance.");
                }
            }
        }
    }

    [Fact]
    public void NotOneNoticeExplainsANYTHING()
    {
        // §13.8. The board is the second-most dangerous surface in the building after the people, because it
        // is WRITING, and writing invites being read closely.
        string[] forbidden =
            ["reever", "old one", "restore", "backup", "revive", "resurrect", "clone", "slave"];

        var prose = CanteenBoard.AllProse().ToList();
        Assert.Equal(CanteenBoard.CatalogSize * 2, prose.Count);

        foreach (string line in prose)
        {
            Assert.False(string.IsNullOrWhiteSpace(line));
            foreach (string bad in forbidden)
            {
                Assert.DoesNotContain(bad, line, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
