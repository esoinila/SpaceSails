using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #801 · THE BUILDING HAS TWO CARS, AND THEY ARE NOT BESIDE EACH OTHER.
///
/// <para>Owner, 2026-08-09: <i>"that elevator would be so busy it would be packed and never available… it
/// is a choke point, and the whole lab would be too easily guarded by just having the guard posted in front
/// of the one elevator. I want to remove that too-easy plot-to-catch-us plot hole."</i></para>
///
/// <para>The law under test is <b>no floor of a clandestine site has exactly one way off it, wherever the
/// corridor has room for two</b> — and the second half of that sentence is what makes it a law rather than
/// a coincidence: the guard binds where the generator admits a second car and is silent where it does not,
/// so a field whose corridors run to their own end caps is not quietly reported as compliant.</para>
///
/// <para>The card law (§13.5) is the thing most at risk from a second shaft, so it is asserted from the
/// other side too: the goods car runs its band and cannot be a way to buy depth without the paper.</para>
/// </summary>
public sealed class TheOtherCarTests
{
    private static SurfaceLayout.Field Field => SurfaceLayout.DefaultField;

    /// <summary>The shipped sites, both cheat rocks, the head office, and forty rolled ones.</summary>
    private static IEnumerable<string> Sweep() =>
        new[]
        {
            "luna", "phobos", "europa", "ganymede", "callisto",
            "titan", "enceladus", "miranda", "triton", "the-clinker",
            "secret-lab-site", "secret-lab-site-unlisted", UndergroundComplex.FoundBandCheatSiteId,
        }.Concat(Enumerable.Range(0, 40).Select(i => $"probe-moon-{i}"));

    // ── (a) THE CHOKE LAW ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// EVERY FLOOR OF EVERY SITE HAS TWO CARS ON IT, and they are at opposite ends of the corridor and on
    /// opposite faces of it.
    ///
    /// <para><b>Proven RED</b> by making <see cref="UndergroundComplex.ServiceShaftAt"/> return null — i.e.
    /// the game as it shipped before this PR:</para>
    /// <code>
    /// 594 floor(s) can be sealed by one person standing on one square:
    ///   luna B1: 1 car on this floor — a guard posted at (34, -154) has the building.
    ///   luna B2: 1 car on this floor — a guard posted at (34, -154) has the building.
    ///   …
    /// </code>
    /// </summary>
    [Fact]
    public void NoFloorOfAClandestineSiteHasOnlyOneWayOff()
    {
        // The premise, first: this ground DOES admit a second car, or everything below passes vacuously.
        IReadOnlyList<UndergroundComplex.Shaft> cars = UndergroundComplex.ShaftsOn(Field);
        Assert.True(cars.Count >= 2,
            "the shipped field takes only one car — every law in this file would be about nothing.");

        var wrong = new List<string>();
        int floors = 0;

        foreach (string body in Sweep())
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                floors++;
                UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, Field);

                // Every car's alcove is CUT on this floor — the shafts are the same holes on every floor of
                // the building, so a plan that drew one of them and not the other would be a car a captain
                // could ride to a floor that has no doors on it.
                foreach (UndergroundComplex.Shaft car in cars)
                {
                    if (!AlcoveIsCut(floor, car))
                    {
                        wrong.Add($"  {body} B{-level}: the {car.Kind} car's alcove is not cut on this floor.");
                    }
                }
            }
        }

        Assert.True(floors > 100, $"only {floors} floors walked — this proved little.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} floor(s) can be sealed by one person standing on one square:\n"
            + string.Join("\n", wrong.Take(20)));
    }

    /// <summary>Is this car's alcove standing on this plan? Asked of the walls the generator actually laid,
    /// at the two verticals that make the box, so a car published in a list and never poured goes red.</summary>
    private static bool AlcoveIsCut(in UndergroundComplex.FloorPlan floor, in UndergroundComplex.Shaft car)
    {
        bool left = false, right = false;
        foreach (SurfaceLayout.Wall w in floor.Walls)
        {
            if (Math.Abs(w.X1 - w.X2) > 0.001)
            {
                continue;
            }
            bool spansTheMouth =
                Math.Abs(Math.Min(w.Y1, w.Y2) - Math.Min(car.Y, car.Landing.Y)) < 6.0
                && Math.Abs(w.Y1 - w.Y2) > 4.0;
            if (!spansTheMouth)
            {
                continue;
            }
            left |= Math.Abs(w.X1 - (car.X - UndergroundComplex.ShaftHalf)) < 0.001;
            right |= Math.Abs(w.X1 - (car.X + UndergroundComplex.ShaftHalf)) < 0.001;
        }
        return left && right;
    }

    /// <summary>
    /// THE TWO CARS CANNOT BOTH BE WATCHED — they are a third of the main corridor apart and they open off
    /// opposite faces of it.
    ///
    /// <para><b>Proven RED</b> by placing the goods car in the first empty stretch the search finds instead
    /// of the one furthest from the cage (the blind end nearest the lift, x = 55.0):</para>
    /// <code>
    /// the two cars are 21.0 du apart and the law wants 92.7 — one person standing between them has both.
    /// </code>
    /// </summary>
    [Fact]
    public void TheTwoCarsCannotBothBeWatched()
    {
        IReadOnlyList<UndergroundComplex.Shaft> cars = UndergroundComplex.ShaftsOn(Field);
        UndergroundComplex.Shaft cage = Assert.Single(
            cars, c => c.Kind == UndergroundComplex.ShaftKind.Cage);
        UndergroundComplex.Shaft goods = Assert.Single(
            cars, c => c.Kind == UndergroundComplex.ShaftKind.Service);

        double apart = Math.Abs(cage.X - goods.X);
        double law = UndergroundComplex.MinShaftSeparationOn(Field);
        Assert.True(apart >= law,
            $"the two cars are {apart:F1} du apart and the law wants {law:F1} — one person standing "
            + "between them has both.");

        // …and on OPPOSITE faces, which is the half of it a distance cannot say: two mouths in the same
        // wall are one machine room however far apart they are, and the map has to read as two ways out.
        Assert.True(cage.Landing.Y > cage.Y, "the cage no longer opens off the spine's upper face.");
        Assert.True(goods.Landing.Y < goods.Y, "the goods car no longer opens off the spine's lower face.");

        // Same spine, so "the other end of the corridor" is a walk and not a different floor.
        Assert.Equal(cage.Y, goods.Y, 3);

        // The plate is not the same plate. A car nobody can tell from the other is one car drawn twice.
        Assert.NotEqual(cage.Sign, goods.Sign);
        Assert.True(cage.ReachesTheSurface && cage.RunsTheGate, "the cage stopped being the cage.");
        Assert.False(goods.ReachesTheSurface || goods.RunsTheGate,
            "the goods car climbs out or runs a gate — §13.5 is a law about the building, not about a car.");
    }

    /// <summary>
    /// THE GROUND DECIDES WHETHER THERE ARE TWO, and says NO out loud rather than putting a car through a
    /// chamber.
    ///
    /// <para>This is what stops the choke law being a tautology. A field whose cross corridors run out to
    /// its own end caps has no blind end to stand a car in, and the placer must return null there — which
    /// is also the anti-vacuous half of every other test in this file: the placement is being computed, not
    /// asserted.</para>
    /// </summary>
    [Fact]
    public void TheGroundDecidesWhetherThereAreTwo()
    {
        // A field so short that the outermost corridor's own chambers reach both end caps. Same shape as
        // the shipped one, a third of the width.
        var cramped = new SurfaceLayout.Field(
            LeftX: -55, RightX: 45, TopY: -20, BottomY: -280,
            LandingBandY: -27, AnchorX: -6, AnchorY: -232, HomeX: 0);

        Assert.Null(UndergroundComplex.ServiceShaftAt(cramped));
        Assert.Single(UndergroundComplex.ShaftsOn(cramped));

        // …and the shipped field is not that field, or the law above would be about nothing.
        Assert.NotNull(UndergroundComplex.ServiceShaftAt(Field));
    }

    /// <summary>
    /// NEITHER CAR HAS A CORRIDOR OR A CHAMBER STANDING ON IT.
    ///
    /// <para>#585's own law (<c>NoRibIsRunThroughTheLiftShaft</c>) said about both cars, and the reason the
    /// goods car is placed past the outermost corridor's chambers rather than in the biggest gap between
    /// two of them: the widest empty x on a spine is very often one a room column is about to be laid in
    /// four floors further down, where the module is a third bigger (<see cref="UndergroundComplex.DeepestRoomScale"/>).</para>
    /// </summary>
    [Fact]
    public void NeitherCarHasACorridorOrAChamberStandingOnIt()
    {
        var wrong = new List<string>();
        int floors = 0, rooms = 0;

        foreach (string body in Sweep())
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                floors++;
                UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, Field);
                foreach (UndergroundComplex.Shaft car in UndergroundComplex.ShaftsOn(Field))
                {
                    foreach (UndergroundComplex.Rib rib in floor.Ribs)
                    {
                        if (Math.Abs(rib.X - car.X) < UndergroundComplex.ShaftHalf
                            + UndergroundComplex.CorridorHalf)
                        {
                            wrong.Add($"  {body} B{-level}: a corridor runs through the {car.Kind} car.");
                        }
                    }
                    foreach ((double rx, double ry) in floor.RoomCentres)
                    {
                        rooms++;
                        if (Math.Abs(rx - car.X) < UndergroundComplex.ShaftHalf
                            && Math.Abs(ry - car.Y) < UndergroundComplex.CorridorHalf + 7.0)
                        {
                            wrong.Add($"  {body} B{-level}: a room sits on top of the {car.Kind} car.");
                        }
                    }
                }
            }
        }

        Assert.True(floors > 100 && rooms > 1000, $"{floors} floors, {rooms} rooms — this proved little.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} car(s) have something standing on them:\n" + string.Join("\n", wrong.Take(20)));
    }

    // ── (b) WHAT THE GOODS CAR'S PANEL OFFERS ─────────────────────────────────────────────────────────

    /// <summary>
    /// THE GOODS CAR RUNS ITS BAND AND NOTHING ELSE — no SURFACE row, no gate row, no refusing row, and
    /// exactly the floors of this band that the site actually has.
    ///
    /// <para><b>Proven RED</b> by giving the service car the cage's panel (deleting the early return in
    /// <c>LiftPanel</c>):</para>
    /// <code>
    /// 594 goods-car panel(s) offer something the shaft does not have:
    ///   luna B1: the goods car offers SURFACE — there is no hut on the regolith over this hole.
    ///   secret-lab-site-unlisted B4: the goods car offers B5, which is another band's floor.
    ///   secret-lab-site-unlisted B4: the goods car has a sealed row — this shaft has no gate in it.
    ///   …
    /// </code>
    ///
    /// <para>Anti-vacuous on the other side too: the same floors are counted where the CAGE does offer a
    /// surface row and a gated row, so a panel that had quietly stopped offering anything at all could not
    /// pass this by being empty.</para>
    /// </summary>
    [Fact]
    public void TheGoodsCarRunsItsBandAndNothingElse()
    {
        var wrong = new List<string>();
        int panels = 0, gatesOnTheCage = 0, surfaceRowsOnTheCage = 0;

        foreach (string body in Sweep())
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                // The card for every band this site has, so the goods car is asked the hardest question
                // there is: a wallet that would open anything.
                var wallet = new List<string>();
                for (int b = 0; b <= 12; b++)
                {
                    if (UndergroundComplex.SiteHasBand(body, b))
                    {
                        wallet.Add(new UndergroundComplex.AuthorityCard(body, b).Id);
                    }
                }

                IReadOnlyList<UndergroundComplex.LiftStop> cage =
                    UndergroundComplex.LiftPanel(body, level, wallet);
                IReadOnlyList<UndergroundComplex.LiftStop> goods = UndergroundComplex.LiftPanel(
                    body, level, UndergroundComplex.ShaftKind.Service, wallet);
                panels++;

                int band = UndergroundComplex.BandOf(level);
                surfaceRowsOnTheCage += cage.Count(s => s.Level == 0);
                gatesOnTheCage += cage.Count(s => s.Level < 0 && UndergroundComplex.BandOf(s.Level) != band);

                foreach (UndergroundComplex.LiftStop stop in goods)
                {
                    if (stop.Level >= 0)
                    {
                        wrong.Add($"  {body} B{-level}: the goods car offers SURFACE — there is no hut on "
                            + "the regolith over this hole.");
                    }
                    else if (UndergroundComplex.BandOf(stop.Level) != band)
                    {
                        wrong.Add($"  {body} B{-level}: the goods car offers B{-stop.Level}, which is "
                            + "another band's floor.");
                    }
                    if (stop.Refusal is not null)
                    {
                        wrong.Add($"  {body} B{-level}: the goods car has a sealed row — this shaft has no "
                            + "gate in it.");
                    }
                    if (stop.OpenedBy is not null || stop.OpenedByChit)
                    {
                        wrong.Add($"  {body} B{-level}: the goods car names a card. Nothing reads one here.");
                    }
                }

                // And it offers ALL of them — a second car that skipped a floor would put the choke back on
                // whichever floor it skipped.
                var offered = goods.Select(s => s.Level).ToHashSet();
                var expected = cage
                    .Where(s => s.Level < 0 && UndergroundComplex.BandOf(s.Level) == band)
                    .Select(s => s.Level)
                    .ToHashSet();
                if (!offered.SetEquals(expected))
                {
                    wrong.Add($"  {body} B{-level}: the goods car serves "
                        + $"[{string.Join(",", offered.OrderByDescending(v => v))}] and the cage's own band "
                        + $"is [{string.Join(",", expected.OrderByDescending(v => v))}].");
                }

                // Exactly one row is the floor it is standing on, on both cars.
                Assert.Single(goods, s => s.IsCurrent);
            }
        }

        Assert.True(panels > 100, $"only {panels} panels read — this proved little.");
        Assert.True(surfaceRowsOnTheCage == panels,
            $"{surfaceRowsOnTheCage} of {panels} cage panels offer the surface — #600's law has moved.");
        Assert.True(gatesOnTheCage > 20,
            $"only {gatesOnTheCage} gated rows on the cage across the sweep — the goods car's silence "
            + "proves nothing if nothing is being kept quiet about.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} goods-car panel(s) offer something the shaft does not have:\n"
            + string.Join("\n", wrong.Take(20)));
    }

    /// <summary>THE CAGE'S PANEL IS THE PANEL IT HAS ALWAYS BEEN. The overload every older caller reaches
    /// is the cage's, to the last row — said here so that "nothing about the cage moved" is a fact a diff
    /// cannot quietly change.</summary>
    [Fact]
    public void TheOlderOverloadIsStillTheCagesOwnPanel()
    {
        int compared = 0;
        foreach (string body in Sweep().Take(20))
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                var card = new List<string>
                {
                    new UndergroundComplex.AuthorityCard(
                        body, UndergroundComplex.NextShaftBelow(body, level) ?? 0).Id,
                };
                Assert.Equal(
                    UndergroundComplex.LiftPanel(body, level, card),
                    UndergroundComplex.LiftPanel(body, level, UndergroundComplex.ShaftKind.Cage, card));
                compared++;
            }
        }
        Assert.True(compared > 80, $"only {compared} panels compared — this proved little.");
    }

    /// <summary>#801 · THE PANEL'S OWN SENTENCE SAYS WHERE THE OTHER CAR IS, and explains nothing about the
    /// building (§13.8). The whole anti-boredom half of the owner's note — <i>"the come-back-here point"</i>
    /// — is that a captain who finds one car has been told there is another.</summary>
    [Fact]
    public void TheGoodsCarsPlateSaysWhatItIsAndLeaksNothing()
    {
        string[] prose =
        [
            UndergroundComplex.ServiceCarSign,
            UndergroundComplex.ServiceCarPanelLine,
            UndergroundComplex.CageSign,
        ];

        Assert.Contains("corridor", UndergroundComplex.ServiceCarPanelLine, StringComparison.Ordinal);
        Assert.Contains("cage", UndergroundComplex.ServiceCarPanelLine, StringComparison.Ordinal);

        string[] forbidden =
        [
            "old one", "old ones", "reever", "restore", "backup", "revive", "resurrect", "clone",
            "slave", "brain", "kaamos", "minister", "ancient", "alien", "experiment", "specimen",
        ];
        foreach (string line in prose)
        {
            foreach (string bad in forbidden)
            {
                Assert.DoesNotContain(bad, line, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
