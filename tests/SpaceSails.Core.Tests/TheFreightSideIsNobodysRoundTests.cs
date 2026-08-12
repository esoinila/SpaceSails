using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #824 · THE FREIGHT SIDE IS NOBODY'S ROUND, AND THAT IS THE FEATURE.
///
/// <para>The fork was filed honestly and could have gone either way. #804's roster walks the spine and every
/// cross corridor; #801's goods car stands at the blind end of that spine with its alcove cut into the lower
/// face — and no beat has ever gone near it. Feature, or a hole in the rota nobody noticed?</para>
///
/// <para><b>Owner ruling, 2026-08-11:</b> <i>"blind spot by design. The unwatched freight side is deliberate
/// — canon already says deliveries run 04:00-06:00 crew-side, and the gumshoe who studies the rounds earns
/// the safe corridor. Work item is now clear: a guard that PROVES the hole is stable (no beat leg enters the
/// goods-car alcove band on ordinary floors), so a future patrol change cannot close it silently."</i></para>
///
/// <para>So this file asserts an ABSENCE, which is the hardest kind of law to write honestly, because an
/// absence is what a broken sweep reports too. Three things are therefore said, and the second and third
/// exist only to stop the first from being about nothing:</para>
///
/// <list type="number">
/// <item><b>No leg and no stop of any round enters the band</b>, on every ordinary patrolled floor of every
/// site — and "ordinary" is <see cref="PatrolBeat.IsPatrolled"/>'s own answer, which already excludes the
/// regolith, the bar floor, the head office, the band nobody listed and the halls nobody dug.</item>
/// <item><b>The band is the alcove the generator actually pours</b>, read back off <c>FloorPlan.Walls</c> on
/// every one of those floors. A band computed from a number that had drifted off the car would be an empty
/// patch of rock that no round could enter however the rota changed — the fifth bug class exactly.</item>
/// <item><b>The round comes close enough for the hole to be a CHOICE.</b> Somewhere in the sweep a leg passes
/// within a guard's own marker sight of the band, and on EVERY floor the round's closest approach is nearer
/// than the distance the building itself calls "too far apart to watch at once"
/// (<see cref="UndergroundComplex.MinShaftSeparationOn"/>). The freight side is a short walk off a corridor
/// somebody walks all night, and still nobody turns down it.</item>
/// </list>
///
/// <para><b>What this file does NOT claim.</b> It is a law about the ROSTER — the published stops and the
/// straight legs between them — and not about the pathfinder's realised steps, which are the client's A* over
/// the deck's own collision field and are audited there (<c>TheRoundIsWalkableTests</c>). That is the honest
/// boundary: Core owns where the round is SENT, the client owns how a man gets there. It is also a fair proxy
/// here, because the alcove is a three-walled dead end whose only mouth is on the spine — a route that
/// entered it would have to be aimed at it.</para>
///
/// <para><b>The sweep found no violation.</b> 470 floors carry both a round and a goods car, and the nearest
/// any leg comes to the band anywhere in the building is <b>23.412 du</b>, on <c>secret-lab-site-unlisted
/// B17</c>. The hole is real, it is on every floor, and it is now nailed down.</para>
/// </summary>
public sealed class TheFreightSideIsNobodysRoundTests
{
    /// <summary>A du of slack a computed coordinate is allowed to miss a poured wall by. The generator and
    /// this file both work in exact arithmetic off the same shared constants, so this is a float's epsilon
    /// and not a tolerance for a near miss.</summary>
    private const double Eps = 0.001;

    /// <summary>
    /// #824 · HOW DEEP THE ALCOVE IS CUT — the box the goods car stands in, measured from the spine's lower
    /// face to the back wall.
    ///
    /// <para>It is a number here because it is a literal in <c>UndergroundComplex.Build</c>: the three walls
    /// of either car's box are poured at <c>shaftY ± (CorridorHalf + 5)</c> and the 5 has never been given a
    /// name. Rather than publish a constant for a test's convenience, the copy is held to the original by
    /// <see cref="TheBandIsTheAlcoveTheGeneratorPours"/>, which reads the back wall off every floor of every
    /// site and fails the moment the two disagree by more than <see cref="Eps"/>.</para>
    /// </summary>
    private const double AlcoveDepthDu = 5.0;

    /// <summary>
    /// #824 · HOW FAR OUTSIDE ITS OWN WALLS THE BLIND SPOT REACHES — the approach margin that turns the
    /// alcove box into the BAND the ruling is about.
    ///
    /// <para>Not a number invented for this guard. <see cref="UndergroundComplex.ShaftClearDu"/> is the clear
    /// corridor the building already demands either side of a car before the ground counts as taking one, and
    /// <c>Build</c> claims exactly that much around the goods car's box in the placement ledger
    /// (<c>carX ± ShaftHalf ± 1.5</c>, down to <c>shaftY - CorridorHalf - 6.5</c>). The band is therefore the
    /// ground the BUILDING reserved for the freight side, read rather than guessed — and it is applied on the
    /// corridor side too, so a beat that came to stand in the alcove's mouth is red before it steps in.</para>
    /// </summary>
    private const double BandApproachDu = UndergroundComplex.ShaftClearDu;

    private static SurfaceLayout.Field Field => SurfaceLayout.DefaultField;

    /// <summary>The shipped sites, both cheat rocks, the head office, and forty rolled ones — the same sweep
    /// the second car's own laws walk (<c>TheOtherCarTests</c>), because a rota is a fact about every floor of
    /// every building and a rule that only holds on luna has not met the generator.</summary>
    private static IEnumerable<string> Sweep() =>
        new[]
        {
            "luna", "phobos", "europa", "ganymede", "callisto",
            "titan", "enceladus", "miranda", "triton", "the-clinker",
            "secret-lab-site", "secret-lab-site-unlisted", UndergroundComplex.FoundBandCheatSiteId,
        }.Concat(Enumerable.Range(0, 40).Select(i => $"probe-moon-{i}"));

    /// <summary>A rectangle in deck units. Written down rather than passed as four doubles because every
    /// complaint in this file prints it, and a band printed in a different order from the one it was tested
    /// in is how a red audit lies to the person reading it.</summary>
    private readonly record struct Band(double X0, double Y0, double X1, double Y1)
    {
        public override string ToString() => $"x[{X0:0.0}, {X1:0.0}] y[{Y0:0.0}, {Y1:0.0}]";
    }

    /// <summary>THE BAND, off the published car geometry and nothing else: the alcove box (the car's own
    /// width, cut from the spine's lower face down to the back wall) grown by <see cref="BandApproachDu"/>
    /// on all four sides.</summary>
    private static Band BandFor(in UndergroundComplex.Shaft car)
    {
        // Which face this car opens off is derived from its own doorstep and never typed — the cage hangs off
        // the spine's upper face and the goods car off the lower one, so a sign written here would be right
        // for one of them and silently vacuous for the other.
        int outward = car.Landing.Y > car.Y ? +1 : -1;
        double face = car.Y + (outward * UndergroundComplex.CorridorHalf);
        double back = face + (outward * AlcoveDepthDu);

        return new Band(
            car.X - UndergroundComplex.ShaftHalf - BandApproachDu,
            Math.Min(face, back) - BandApproachDu,
            car.X + UndergroundComplex.ShaftHalf + BandApproachDu,
            Math.Max(face, back) + BandApproachDu);
    }

    /// <summary>The goods car on this field, or null where the ground would not take a second one.</summary>
    private static UndergroundComplex.Shaft? GoodsCarOn(in SurfaceLayout.Field field) =>
        UndergroundComplex.ShaftsOn(field)
            .Cast<UndergroundComplex.Shaft?>()
            .FirstOrDefault(s => s!.Value.Kind == UndergroundComplex.ShaftKind.Service);

    // ── (a) THE HOLE ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// NO LEG AND NO STOP OF ANY ROUND ENTERS THE GOODS CAR'S ALCOVE BAND, on every ordinary patrolled floor
    /// of every site, on the bare circuit and on four watches of the round as it is actually walked.
    ///
    /// <para>The round is a CLOSED loop — <c>Map.Patrol</c> advances legs with <c>(leg + 1) % Count</c> — so
    /// the leg from the last stop back to the first is walked like any other and is checked like any other.
    /// Skipping it would leave the one leg that crosses the whole floor unexamined, which is the leg a rota
    /// change is most likely to swing through the blind end.</para>
    ///
    /// <para><b>Proven RED, twice.</b> First by giving every round a synthetic leg past the car — a stop at
    /// the goods car's own landing, appended to the circuit, which is exactly the shape "a round gains a leg
    /// past the car" would have taken as a patch. The landing itself is a pace out on the SPINE and so stands
    /// clear of the band; it is the walk to it that is red, which is the point:</para>
    /// <code>
    /// 640 round(s) walk into the freight side that nobody watches:
    ///   luna B2 watch 1: the leg (-114.4, -189.3) → (-138.0, -154.5) enters the band
    ///     x[-142.5, -133.5] y[-163.5, -155.5] — the goods car's alcove is on somebody's round now.
    ///   luna B3 watch 2: the leg (-138.0, -154.5) → (-114.4, -189.3) enters the band …
    ///   luna B4 watch 0: the leg (-138.0, -154.5) → (-16.0, -189.3) enters the band …
    ///   …
    /// </code>
    ///
    /// <para>…and again with the stop moved INTO the alcove rather than onto its doorstep (a post at
    /// <c>car.Y - CorridorHalf - 2</c>), because a leg check and a stop check are two claims and only one of
    /// them was red above:</para>
    /// <code>
    /// 1410 round(s) walk into the freight side that nobody watches:
    ///   luna B2 circuit: the leg (104.4, -117.7) → (-138.0, -159.0) enters the band
    ///     x[-142.5, -133.5] y[-163.5, -155.5] — the goods car's alcove is on somebody's round now.
    ///   luna B2 circuit: the leg (-138.0, -159.0) → (34.0, -152.5) enters the band …
    ///   luna B2 circuit: the stop 'the goods car's alcove' stands at (-138.0, -159.0), inside the band
    ///     x[-142.5, -133.5] y[-163.5, -155.5].
    ///   …
    /// </code>
    /// </summary>
    [Fact]
    public void NoRoundEntersTheGoodsCarsAlcoveBand()
    {
        UndergroundComplex.Shaft car = GoodsCarOn(Field)
            ?? throw new InvalidOperationException(
                "the shipped field takes only one car — every law in this file would be about nothing.");
        Band band = BandFor(car);

        var walkedIn = new List<string>();
        int floors = 0, legs = 0;

        foreach (string body in Sweep())
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                if (!PatrolBeat.IsPatrolled(body, level))
                {
                    continue;
                }

                UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, Field);
                IReadOnlyList<PatrolBeat.Stop> circuit = PatrolBeat.Circuit(floor, Field);
                if (circuit.Count < 2)
                {
                    continue;   // a floor with no round to walk is #804's business, not this file's
                }

                // The floor is counted only where BOTH things this law is about are actually present. A
                // building with no goods car, or a floor with no round on it, would satisfy an absence for
                // the wrong reason.
                floors++;

                legs += Inspect(circuit, band, $"{body} B{-level} circuit", walkedIn);
                for (long watch = 0; watch < 4; watch++)
                {
                    legs += Inspect(
                        PatrolBeat.BeatFor(body, level, watch, floor, Field),
                        band, $"{body} B{-level} watch {watch}", walkedIn);
                }
            }
        }

        Assert.True(floors > 100,
            $"only {floors} floor(s) have both a round and a goods car on them — this proved little.");
        Assert.True(legs > floors * 20, $"only {legs} legs walked over {floors} floors — this proved little.");
        Assert.True(walkedIn.Count == 0,
            $"{walkedIn.Count} round(s) walk into the freight side that nobody watches:\n"
            + string.Join("\n", walkedIn.Take(20)));
    }

    /// <summary>One round, closed into its loop, against the band. Returns how many legs it walked so the
    /// caller can prove the sweep was not empty.</summary>
    private static int Inspect(
        IReadOnlyList<PatrolBeat.Stop> round, in Band band, string where, List<string> walkedIn)
    {
        for (int i = 0; i < round.Count; i++)
        {
            PatrolBeat.Stop here = round[i];
            PatrolBeat.Stop next = round[(i + 1) % round.Count];

            if (Distance(here.X, here.Y, next.X, next.Y, band) < Eps)
            {
                walkedIn.Add(
                    $"  {where}: the leg ({here.X:0.0}, {here.Y:0.0}) → ({next.X:0.0}, {next.Y:0.0}) enters "
                    + $"the band {band} — the goods car's alcove is on somebody's round now.");
            }
            if (Inside(here.X, here.Y, band))
            {
                walkedIn.Add(
                    $"  {where}: the stop '{here.What}' stands at ({here.X:0.0}, {here.Y:0.0}), inside the "
                    + $"band {band}.");
            }
        }
        return round.Count;
    }

    // ── (b) …AND THE BAND IS THE REAL ALCOVE ──────────────────────────────────────────────────────────

    /// <summary>
    /// THE BAND IS THE ALCOVE THE GENERATOR ACTUALLY POURS. Its three walls — the two sides standing on the
    /// spine's lower face and the back wall across them — are read off <c>FloorPlan.Walls</c> on every floor
    /// of every site, and every one of them must lie inside the band with exactly
    /// <see cref="BandApproachDu"/> of margin around it.
    ///
    /// <para>This is what stops the law above being a statement about an empty patch of rock. An absence is
    /// what a band pointed at the wrong place reports too, and a guard that cannot tell the two worlds apart
    /// is the fifth bug class wearing a green tick.</para>
    ///
    /// <para><b>Proven RED</b> by taking a single du off <see cref="AlcoveDepthDu"/> — every floor in the
    /// sweep, which is what a copied literal drifting off its original looks like:</para>
    /// <code>
    /// 470 floor(s) where the band is not the alcove:
    ///   luna B2: the alcove's back wall is at y -162.000 and the band's own inner edge at -161.000 —
    ///     a 1.000 du drift between the box this guard describes and the box the generator poured.
    ///   luna B3: the alcove's back wall is at y -162.000 and the band's own inner edge at -161.000 — …
    ///   …
    /// </code>
    /// </summary>
    [Fact]
    public void TheBandIsTheAlcoveTheGeneratorPours()
    {
        UndergroundComplex.Shaft car = GoodsCarOn(Field)
            ?? throw new InvalidOperationException("the shipped field takes only one car.");
        Band band = BandFor(car);

        // The band reaches BandApproachDu outside the box on every side, so the box itself is the band shrunk
        // back by exactly that. Stated here, once, so the checks below read against the poured walls.
        double boxLo = band.X0 + BandApproachDu, boxHi = band.X1 - BandApproachDu;
        double boxBack = band.Y0 + BandApproachDu, boxFace = band.Y1 - BandApproachDu;

        var wrong = new List<string>();
        int floors = 0;

        foreach (string body in Sweep())
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                if (!PatrolBeat.IsPatrolled(body, level))
                {
                    continue;
                }

                UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, Field);
                floors++;

                double back = double.NaN;
                bool leftSide = false, rightSide = false;

                foreach (SurfaceLayout.Wall w in floor.Walls)
                {
                    bool vertical = Math.Abs(w.X1 - w.X2) < Eps;
                    bool horizontal = Math.Abs(w.Y1 - w.Y2) < Eps;

                    // The back wall: a horizontal run spanning the box's own width, on the far side of the
                    // spine from the corridor. Matched on its ENDS, not on a y typed here, so a box poured at
                    // the wrong depth is found rather than assumed.
                    if (horizontal
                        && Math.Abs(Math.Min(w.X1, w.X2) - boxLo) < Eps
                        && Math.Abs(Math.Max(w.X1, w.X2) - boxHi) < Eps
                        && w.Y1 < car.Y)
                    {
                        back = w.Y1;
                    }

                    if (!vertical || Math.Abs(Math.Max(w.Y1, w.Y2) - boxFace) > Eps)
                    {
                        continue;
                    }
                    leftSide |= Math.Abs(w.X1 - boxLo) < Eps;
                    rightSide |= Math.Abs(w.X1 - boxHi) < Eps;
                }

                if (!leftSide || !rightSide)
                {
                    wrong.Add($"  {body} B{-level}: the band's own side{(leftSide || rightSide ? "" : "s")} at "
                        + $"x {boxLo:0.000}/{boxHi:0.000} do not stand on the spine face at y {boxFace:0.000} "
                        + "— the band is not describing this floor's alcove.");
                }
                else if (double.IsNaN(back))
                {
                    wrong.Add($"  {body} B{-level}: no back wall spans [{boxLo:0.000}, {boxHi:0.000}] below "
                        + "the spine — the box this guard describes was never poured.");
                }
                else if (Math.Abs(back - boxBack) > Eps)
                {
                    wrong.Add($"  {body} B{-level}: the alcove's back wall is at y {back:0.000} and the "
                        + $"band's own inner edge at {boxBack:0.000} — a {Math.Abs(back - boxBack):0.000} du "
                        + "drift between the box this guard describes and the box the generator poured.");
                }
            }
        }

        Assert.True(floors > 100, $"only {floors} floors read — this proved little.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} floor(s) where the band is not the alcove:\n" + string.Join("\n", wrong.Take(20)));
    }

    // ── (c) …AND THE ROUND IS RIGHT THERE ─────────────────────────────────────────────────────────────

    /// <summary>
    /// THE HOLE IS A CHOICE, NOT A DIFFERENT BUILDING. Two measurements, both against numbers the game
    /// already publishes rather than against a threshold typed for a test:
    ///
    /// <list type="bullet">
    /// <item>Somewhere in the sweep a leg passes within <see cref="PatrolBeat.MarkerSightDu"/> — a guard's own
    /// marker sight — of the band. A man on his round gets close enough that the freight side's mouth is
    /// inside the circle he can be seen from, and he still walks the other way.</item>
    /// <item>On EVERY floor the round's closest approach is nearer than
    /// <see cref="UndergroundComplex.MinShaftSeparationOn"/>, the distance the building itself calls too far
    /// apart for one person to watch. The blind end is a short walk off a corridor somebody paces all night,
    /// not a wing at the other end of the site.</item>
    /// </list>
    ///
    /// <para>Without this, <see cref="NoRoundEntersTheGoodsCarsAlcoveBand"/> would pass just as green on a
    /// rota that had quietly stopped covering the floor at all — which is the whole reason the fork was filed
    /// as a fork.</para>
    ///
    /// <para><b>Proven RED</b> by pulling the round back into the east half of the building (keeping only the
    /// stops at <c>x &gt; 0</c>, i.e. a rota change that covers the cage end and gives up the rest):</para>
    /// <code>
    /// 470 floor(s) keep the round in another part of the building:
    ///   luna B2: the round never comes nearer than 144.5 du to the band, and 92.7 du is what this building
    ///     calls out of sight — the blind spot is in a wing nobody walks, which is a different feature.
    ///   luna B3: the round never comes nearer than 167.5 du to the band, and 92.7 du is what this building
    ///     calls out of sight — …
    ///   …
    /// </code>
    /// </summary>
    [Fact]
    public void TheRoundComesCloseEnoughForTheHoleToBeAChoice()
    {
        UndergroundComplex.Shaft car = GoodsCarOn(Field)
            ?? throw new InvalidOperationException("the shipped field takes only one car.");
        Band band = BandFor(car);
        double tooFarToWatch = UndergroundComplex.MinShaftSeparationOn(Field);

        double closestAnywhere = double.PositiveInfinity;
        string closestWhere = "nowhere";
        var strangers = new List<string>();
        int floors = 0;

        foreach (string body in Sweep())
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                if (!PatrolBeat.IsPatrolled(body, level))
                {
                    continue;
                }

                UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, Field);
                IReadOnlyList<PatrolBeat.Stop> circuit = PatrolBeat.Circuit(floor, Field);
                if (circuit.Count < 2)
                {
                    continue;
                }
                floors++;

                double closestHere = double.PositiveInfinity;
                for (int i = 0; i < circuit.Count; i++)
                {
                    PatrolBeat.Stop a = circuit[i], b = circuit[(i + 1) % circuit.Count];
                    closestHere = Math.Min(closestHere, Distance(a.X, a.Y, b.X, b.Y, band));
                }

                if (closestHere < closestAnywhere)
                {
                    closestAnywhere = closestHere;
                    closestWhere = $"{body} B{-level}";
                }
                if (closestHere >= tooFarToWatch)
                {
                    strangers.Add($"  {body} B{-level}: the round never comes nearer than {closestHere:0.0} du "
                        + $"to the band, and {tooFarToWatch:0.0} du is what this building calls out of sight "
                        + "— the blind spot is in a wing nobody walks, which is a different feature.");
                }
            }
        }

        Assert.True(floors > 100, $"only {floors} floors measured — this proved little.");
        Assert.True(strangers.Count == 0,
            $"{strangers.Count} floor(s) keep the round in another part of the building:\n"
            + string.Join("\n", strangers.Take(20)));
        Assert.True(closestAnywhere <= PatrolBeat.MarkerSightDu,
            $"the nearest any leg comes to the band anywhere in the sweep is {closestAnywhere:0.000} du "
            + $"({closestWhere}), and a guard's marker sight is {PatrolBeat.MarkerSightDu:0.0} — the rounds "
            + "and the freight side are no longer measuring against each other.");
    }

    // ── THE ARITHMETIC ────────────────────────────────────────────────────────────────────────────────

    private static bool Inside(double px, double py, in Band band) =>
        px >= band.X0 && px <= band.X1 && py >= band.Y0 && py <= band.Y1;

    /// <summary>How far a leg passes from the band — zero when it touches or enters it. The two questions
    /// this file asks are the same question at two thresholds, so they are answered by one function: a law
    /// that used one test for "enters" and another for "comes near" could report a leg as neither.</summary>
    private static double Distance(double ax, double ay, double bx, double by, in Band band)
    {
        if (Inside(ax, ay, band) || Inside(bx, by, band))
        {
            return 0;
        }

        double best = double.PositiveInfinity;
        (double X0, double Y0, double X1, double Y1)[] sides =
        [
            (band.X0, band.Y0, band.X1, band.Y0),
            (band.X1, band.Y0, band.X1, band.Y1),
            (band.X1, band.Y1, band.X0, band.Y1),
            (band.X0, band.Y1, band.X0, band.Y0),
        ];
        foreach ((double cx, double cy, double dx, double dy) in sides)
        {
            best = Math.Min(best, SegmentToSegment(ax, ay, bx, by, cx, cy, dx, dy));
        }
        return best;
    }

    private static double SegmentToSegment(
        double ax, double ay, double bx, double by, double cx, double cy, double dx, double dy)
    {
        if (Crosses(ax, ay, bx, by, cx, cy, dx, dy))
        {
            return 0;
        }
        return Math.Min(
            Math.Min(PointToSegment(ax, ay, cx, cy, dx, dy), PointToSegment(bx, by, cx, cy, dx, dy)),
            Math.Min(PointToSegment(cx, cy, ax, ay, bx, by), PointToSegment(dx, dy, ax, ay, bx, by)));
    }

    private static bool Crosses(
        double ax, double ay, double bx, double by, double cx, double cy, double dx, double dy)
    {
        double d1 = Turn(cx, cy, dx, dy, ax, ay), d2 = Turn(cx, cy, dx, dy, bx, by);
        double d3 = Turn(ax, ay, bx, by, cx, cy), d4 = Turn(ax, ay, bx, by, dx, dy);
        return ((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) && ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0));
    }

    private static double Turn(double ax, double ay, double bx, double by, double px, double py) =>
        ((bx - ax) * (py - ay)) - ((by - ay) * (px - ax));

    private static double PointToSegment(
        double px, double py, double ax, double ay, double bx, double by)
    {
        double vx = bx - ax, vy = by - ay;
        double len2 = (vx * vx) + (vy * vy);
        double t = len2 <= 0 ? 0 : Math.Clamp((((px - ax) * vx) + ((py - ay) * vy)) / len2, 0, 1);
        double qx = ax + (t * vx), qy = ay + (t * vy);
        return Math.Sqrt(((px - qx) * (px - qx)) + ((py - qy) * (py - qy)));
    }
}
