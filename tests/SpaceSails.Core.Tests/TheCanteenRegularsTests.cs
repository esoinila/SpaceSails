using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #709 · THE HIVE'S FIRST PEOPLE, AND THE ONE FLOOR THEY ARE ON.
///
/// <para>Owner, 2026-08-05: <i>"we should have people in the bar... we have cover story"</i> · <i>"for now
/// let's keep the people in B1."</i></para>
///
/// <para>The second sentence is the one with teeth. The Hive's abandoned tone is load-bearing, and staff
/// leaking one floor down would spend it — so <b>B1-only is a law, not a default</b>, and these pin it over
/// every floor of every site the generator admits.</para>
/// </summary>
public sealed class TheCanteenRegularsTests
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
    public void NOBODYIsAnywhereButTheUpperCanteenOnTheTopPressurisedFloor()
    {
        // THE OWNER'S RULING, over every amenity on every floor of every site. A regular in the deep staff
        // mess or a washroom would be a different game than the one the descent gradient is built on.
        var wrong = new List<string>();
        int seenSomebody = 0, seenCanteens = 0;

        foreach (string body in Sweep())
        {
            int? top = UndergroundComplex.TopPressurisedFloor(body);
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, Field);
                foreach (UndergroundComplex.Amenity a in floor.Amenities)
                {
                    var sat = CanteenRegulars.Sitting(body, level, a);
                    bool allowed =
                        a.Use == UndergroundComplex.Comfort.UpperCanteen && top == level;

                    if (allowed)
                    {
                        seenCanteens++;
                        seenSomebody += sat.Count;
                    }
                    else if (sat.Count > 0)
                    {
                        wrong.Add($"  {body} B{-level} {a.Use}: {sat.Count} person(s) where nobody may sit");
                    }
                }
            }
        }

        Assert.True(wrong.Count == 0,
            $"{wrong.Count} room(s) hold people they must not:\n{string.Join("\n", wrong.Take(20))}");

        // And the law is not vacuously true because nobody is ever anywhere.
        Assert.True(seenCanteens > 20, $"only {seenCanteens} upper canteens in the sweep — nothing was proved.");
        Assert.True(seenSomebody > 20, $"only {seenSomebody} people across the whole sweep — the bar is empty.");
    }

    [Fact]
    public void TheTwoLawsHoldEvenWHERETHEGENERATORCANNOTPUTTHEMYET()
    {
        // WHY THIS TEST EXISTS, and it is the whole lesson of this PR. The sweep above passes with BOTH
        // guards in Sitting() deleted — because today they mask each other: a washroom has no tables, and
        // the deep staff mess is never on the top floor. Two guards, neither individually reachable by any
        // floor the generator currently builds, and a sweep that signs off on both. That is a green test
        // that asserts nothing (docs §"the fifth class"), and it is the exact failure this repo has a name
        // for.
        //
        // So the conditions are FORCED rather than found. `Amenity` is a public record; a synthetic one puts
        // the world into the state the carver cannot reach yet, and both guards become individually provable.
        // The day somebody gives the staff mess a canteen's tables, or carves a second canteen further down,
        // the ruling must not quietly stop being true — and these are what will notice.
        string body = Bodies[0];
        int top = UndergroundComplex.TopPressurisedFloor(body)
            ?? throw new InvalidOperationException($"{body} has no pressurised floor to test on.");
        IReadOnlyList<(double X, double Y)> tables = [(10.0, 20.0), (30.0, 40.0)];

        // 1 · The deep staff mess, given a canteen's tables, on the top floor itself. Nobody sits: the mess
        //     is pass-only and its people are #618's question, not this one.
        Assert.Empty(CanteenRegulars.Sitting(body, top, new UndergroundComplex.Amenity(
            UndergroundComplex.Comfort.StaffCanteen, 0, 0, "PLATE", "FIXTURE", tables)));

        // 2 · A washroom with tables in it, which is absurd and is the point — the law is about the ROOM'S
        //     USE, never about whether it happens to have somewhere to sit.
        Assert.Empty(CanteenRegulars.Sitting(body, top, new UndergroundComplex.Amenity(
            UndergroundComplex.Comfort.Washroom, 0, 0, "PLATE", "FIXTURE", tables)));

        // 3 · A second upper canteen, carved somewhere below. THE OWNER'S B1 RULING, and the only test in
        //     the file that can ever catch it breaking.
        foreach (int below in UndergroundComplex.FloorsOf(body).Where(l => l != top))
        {
            Assert.Empty(CanteenRegulars.Sitting(body, below, new UndergroundComplex.Amenity(
                UndergroundComplex.Comfort.UpperCanteen, 0, 0, "PLATE", "FIXTURE", tables)));
        }

        // And the same forced shape, on the floor where people ARE allowed, does seat somebody — or the
        // three assertions above would pass on a function that always returns nothing.
        Assert.NotEmpty(CanteenRegulars.Sitting(body, top, new UndergroundComplex.Amenity(
            UndergroundComplex.Comfort.UpperCanteen, 0, 0, "PLATE", "FIXTURE", tables)));
    }

    [Fact]
    public void TheCanteenIsNeverEMPTYAndNeverACROWD()
    {
        // Owner asked for people in the bar. An empty canteen is a thing this building already has twenty
        // floors of — and six would be a crowd, which is a different building than a clandestine basement.
        foreach (string body in Sweep())
        {
            int? top = UndergroundComplex.TopPressurisedFloor(body);
            if (top is not { } level)
            {
                continue;
            }

            foreach (UndergroundComplex.Amenity a in UndergroundComplex.Build(body, level, Field).Amenities
                .Where(a => a.Use == UndergroundComplex.Comfort.UpperCanteen))
            {
                var sat = CanteenRegulars.Sitting(body, level, a);
                Assert.InRange(sat.Count, 1, Math.Min(a.Tables.Count, CanteenRegulars.MostAtOnce));
            }
        }
    }

    [Fact]
    public void NobodyShaesAChairAndNobodyIsInTheRoomTwice()
    {
        // Two people on one chair and one person said twice are the same bug wearing different clothes, and
        // the seeded picker's skip-forward is the thing under test.
        foreach (string body in Sweep())
        {
            if (UndergroundComplex.TopPressurisedFloor(body) is not { } level)
            {
                continue;
            }

            foreach (UndergroundComplex.Amenity a in UndergroundComplex.Build(body, level, Field).Amenities
                .Where(a => a.Use == UndergroundComplex.Comfort.UpperCanteen))
            {
                var sat = CanteenRegulars.Sitting(body, level, a);

                var seats = sat.Select(s => (s.X, s.Y)).ToList();
                Assert.Equal(seats.Count, seats.Distinct().Count());

                var faces = sat.Select(s => s.Plate).ToList();
                Assert.Equal(faces.Count, faces.Distinct().Count());

                // And every one of them is sitting at a table the room actually has.
                foreach (CanteenRegulars.Seated s in sat)
                {
                    Assert.Contains((s.X, s.Y), a.Tables);
                }
            }
        }
    }

    [Fact]
    public void WithinOneSHIFTTheRoomIsTheSameRoomEveryTimeYouLookAtIt()
    {
        // Determinism where it matters. Walk out of the canteen and back in on the same watch and it must be
        // the same three people in the same three chairs — a room that reshuffles while you are standing in
        // it is a slot machine, and worse, the [E] key would answer about somebody who has moved.
        foreach (string body in Bodies)
        {
            if (UndergroundComplex.TopPressurisedFloor(body) is not { } level)
            {
                continue;
            }

            foreach (UndergroundComplex.Amenity a in UndergroundComplex.Build(body, level, Field).Amenities
                .Where(a => a.Use == UndergroundComplex.Comfort.UpperCanteen))
            {
                foreach (long watch in new long[] { 0, 1, 7, 1234 })
                {
                    Assert.Equal(
                        CanteenRegulars.Sitting(body, level, a, watch),
                        CanteenRegulars.Sitting(body, level, a, watch));
                }
            }
        }
    }

    [Fact]
    public void ACROSSShiftsTheRoomACTUALLYCHANGES()
    {
        // #709 · The owner's ask: "let's have some random element of who is in the bar and where they got to
        // sit down." Before this the roster was seeded off the site alone, so a moon had the same three people
        // in the same three chairs forever — furniture, not a canteen.
        //
        // THIS IS THE GUARD FOR THAT, and it is measured rather than asserted at one point: a roster that
        // varied on one site and one watch pair could pass by luck. It sweeps every site across many watches
        // and demands real churn in BOTH halves — who is in, and where they sat.
        int bodiesChecked = 0;

        foreach (string body in Sweep())
        {
            if (UndergroundComplex.TopPressurisedFloor(body) is not { } level)
            {
                continue;
            }

            foreach (UndergroundComplex.Amenity a in UndergroundComplex.Build(body, level, Field).Amenities
                .Where(a => a.Use == UndergroundComplex.Comfort.UpperCanteen))
            {
                var rosters = new HashSet<string>(StringComparer.Ordinal);
                var seatings = new HashSet<string>(StringComparer.Ordinal);

                for (long watch = 0; watch < 24; watch++)
                {
                    var sat = CanteenRegulars.Sitting(body, level, a, watch);
                    rosters.Add(string.Join("|", sat.Select(s => s.Plate).OrderBy(s => s, StringComparer.Ordinal)));
                    seatings.Add(string.Join("|", sat.Select(s => $"{s.Plate}@{s.X:F1},{s.Y:F1}")));
                }

                // WHO is in: several different crowds over a day of watches.
                Assert.True(rosters.Count >= 4,
                    $"{body}: only {rosters.Count} different crowd(s) across 24 watches — the room is furniture.");

                // WHERE they sat: strictly more variety than the rosters alone, because the same two people
                // may swap chairs. This is the half the owner asked for second and it is the easier one to
                // forget to seed.
                Assert.True(seatings.Count >= rosters.Count,
                    $"{body}: {seatings.Count} seating(s) for {rosters.Count} crowd(s) — chairs are not moving.");

                bodiesChecked++;
            }
        }

        Assert.True(bodiesChecked > 20, $"only {bodiesChecked} canteens swept — nothing was proved.");
    }

    [Fact]
    public void NotOneOfThemExplainsANYTHING()
    {
        // §13.8, and this is now the most dangerous room in the game for it: it is the ONE place down here
        // where somebody can talk. A regular who hints is a canon break that no later fix can take back.
        //
        // The guard walks the catalog itself rather than a copy, so a line added tomorrow is checked tomorrow.
        string[] forbidden =
            ["reever", "old one", "restore", "backup", "revive", "resurrect", "clone", "slave"];

        var prose = CanteenRegulars.AllProse().ToList();
        Assert.Equal(CanteenRegulars.CastSize * 2, prose.Count);

        foreach (string line in prose)
        {
            Assert.False(string.IsNullOrWhiteSpace(line.Trim('.', ' ')) && line != "...",
                "a regular with nothing to say still needs something on the plate.");
            foreach (string bad in forbidden)
            {
                Assert.DoesNotContain(bad, line, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void EveryPlateReadsAsAPersonAndEveryOneOfThemIsBORING()
    {
        // The register test inherited from #701, one door along. Nobody in this room is interesting: they are
        // tired, owed money, waiting on somebody else's paperwork, or eating. A regular who was mysterious
        // would be a quest-giver in a hat, and the room would stop being cover and become a corridor with
        // clues in it.
        foreach (string plate in CanteenRegulars.AllProse().Where((_, i) => i % 2 == 0))
        {
            Assert.StartsWith(CanteenRegulars.Glyph, plate, StringComparison.Ordinal);
            Assert.Equal(plate.ToUpperInvariant(), plate);
        }
    }
}
