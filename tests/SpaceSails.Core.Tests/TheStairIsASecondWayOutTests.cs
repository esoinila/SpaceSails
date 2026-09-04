using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #719 · A SECOND WAY OUT, AND IT SHIPS BEFORE ANYTHING IS ALLOWED TO STOP THE CAR.
///
/// <para>Owner, 2026-08-05: <i>"just stopping the elevator by remote of radio message would stop all escape
/// way too easy :-d"</i> / <i>"going up would use more air"</i>. Until this issue the cage was the single
/// way home — <c>ShaftKind</c> had two members and <c>ReachesTheSurface</c> was <c>Kind == Cage</c> — so a
/// maintenance break would have been a softlock with atmosphere rather than a horror beat.</para>
///
/// <para>The laws proved here, each one an ordering the issue states as non-negotiable:</para>
/// <list type="number">
/// <item>every listed floor of every clandestine site has a stair door, with the plate beside it;</item>
/// <item>it is never beside the cage — it stands beyond the outermost cross corridor, so every room in the
/// building lies between the two;</item>
/// <item>it climbs out and it runs no gate;</item>
/// <item>the climb is DERIVED — <c>MetresDown</c> through <c>SurfaceScale</c>, priced by
/// <c>SuitAir.WalkHomeSeconds</c> at <c>HeavyLabour</c> — and it costs more than the walk it is, which costs
/// more than the car, which charges nothing;</item>
/// <item>nothing below the listed bottom has one, and the stair never opens into a sealed working or a
/// gated band;</item>
/// <item>the plate is the only new string, and it does not wear the cars' vocabulary.</item>
/// </list>
/// </summary>
public sealed class TheStairIsASecondWayOutTests
{
    private static SurfaceLayout.Field Field => SurfaceLayout.DefaultField;

    /// <summary>The scenario's own moons, a wide net of generated ids, and the head office — the same net
    /// <c>TheHiveAmenitiesTests</c> states its laws over, because "every clandestine site" is a claim about
    /// the GENERATOR and not about the ten a player is likely to visit.</summary>
    private static IEnumerable<string> ManySites()
    {
        foreach (string body in new[]
        {
            "luna", "phobos", "europa", "ganymede", "callisto",
            "titan", "enceladus", "miranda", "triton", "the-clinker",
            "secret-lab-site", "secret-lab-site-unlisted",
        })
        {
            yield return body;
        }
        for (int i = 0; i < 90; i++)
        {
            yield return $"generated-moon-{i}";
        }
        yield return KaamosLore.IceMoonBodyId;
    }

    private static void Report(List<string> bad, int seen, string law, int atLeast)
    {
        // ANTI-VACUITY. Every sweep in this file could pass by walking nothing at all, which is the fifth
        // named bug class in this repository: a guard handed a world that cannot tell pass from fail.
        Assert.True(seen >= atLeast, $"only {seen} case(s) were walked — this proved nothing about {law}.");
        if (bad.Count == 0)
        {
            return;
        }
        var sb = new StringBuilder();
        sb.AppendLine($"{bad.Count} of {seen} case(s) break the law: {law}");
        foreach (string line in bad.Take(20))
        {
            sb.AppendLine(line);
        }
        Assert.Fail(sb.ToString());
    }

    // ── (1) THERE IS ONE, ON EVERY FLOOR THE BUILDING ADMITS TO ──────────────────────────────────────────

    /// <summary>
    /// EVERY LISTED FLOOR OF EVERY SITE CARRIES A STAIR DOOR, cut in the spine's own face, with the plate
    /// beside it.
    ///
    /// <para>This is the ordering law itself. <c>StairShaftAt</c> is allowed to answer null — a field whose
    /// ribs run out to its own end caps has no blind end — and that honesty is only worth having if
    /// something asks the SHIPPED site list whether it ever happens. It does not.</para>
    ///
    /// <para><b>Proven RED</b> by returning null from <c>StairShaftAt</c> unconditionally:</para>
    /// <code>
    /// 1074 of 1074 case(s) break the law: every floor the building admits to has a stair door
    ///   luna B1: no stair on the plan at all — the cage is the single way home again.
    ///   luna B2: no stair on the plan at all — the cage is the single way home again.
    /// </code>
    /// </summary>
    [Fact]
    public void EveryListedFloorHasAStairDoorWithItsPlate()
    {
        var bad = new List<string>();
        int seen = 0;

        foreach (string body in ManySites())
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                if (!UndergroundComplex.HasStairOn(body, level))
                {
                    continue;
                }
                seen++;

                if (UndergroundComplex.StairShaftAt(Field) is not { } at)
                {
                    bad.Add($"  {body} B{-level}: no stair on the plan at all — the cage is the single way "
                        + "home again.");
                    continue;
                }

                UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, Field);
                double face = at.Y + UndergroundComplex.CorridorHalf;

                bool leaf = floor.Doorways.Any(d =>
                    Math.Abs(d.Y1 - face) < 0.001 && Math.Abs(d.Y2 - face) < 0.001
                    && Math.Abs(((d.X1 + d.X2) / 2.0) - at.X) < 0.001);
                if (!leaf)
                {
                    bad.Add($"  {body} B{-level}: the stair stands at x={at.X:F1} and no doorway is cut "
                        + "there — a way out you cannot walk into is a wall with a plate on it.");
                    continue;
                }

                if (!floor.Labels.Any(m => string.Equals(m.Label, UndergroundComplex.StairSign, StringComparison.Ordinal)))
                {
                    bad.Add($"  {body} B{-level}: the leaf is cut and nothing says what it is.");
                }
            }
        }

        Report(bad, seen, "every floor the building admits to has a stair door", 1000);
    }

    // ── (2) IT IS NEVER BESIDE THE CAGE ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE STAIR STANDS BEYOND THE OUTERMOST CROSS CORRIDOR, so every room in the building is between it and
    /// the cage — which is the strong form of "a second exit next to the first is one exit", and a truer
    /// statement than any distance in deck units.
    ///
    /// <para>It also may not be shoulder to shoulder with the cage, and it may not share an end with the
    /// goods car or with the preserved/sealed pocket. Sharing an end with the pocket is the one that
    /// matters most: that pocket is where a stop order's seal stands (#1074 beat 1), it is cut into the same
    /// upper face, and a stair through it would be an escape route opening into a sealed working.</para>
    ///
    /// <para><b>Proven RED</b> by having <c>StairShaftAt</c> hand back <c>ServiceShaftAt(field)</c> — the
    /// stair put at the goods car's own end, which is what the two exclusions exist to refuse:</para>
    /// <code>
    /// 2 of 1 case(s) break the law: the stair stands clear of the cage, the goods car and the pocket
    ///   the stair is cut at x=-138.0 and the goods car stands at x=-138.0 — one end, two ways out,
    ///   which is one way out with a spare.
    ///   the stair is cut at x=-138.0 and the pocket a stop order's seal stands in is at x=-136.2 — the
    ///   escape route opens into the sealed working.
    /// </code>
    /// </summary>
    [Fact]
    public void TheStairIsBeyondEveryRibAndShoulderToShoulderWithNothing()
    {
        var bad = new List<string>();

        (double stairX, double _) = UndergroundComplex.StairShaftAt(Field)
            ?? throw new InvalidOperationException("the shipped field takes no stair — law (1) says more.");
        (double cageX, double _) = UndergroundComplex.ShaftAt(Field);

        // EVERY CROSS CORRIDOR IS ON THE CAGE'S SIDE OF THE STAIR. That is the structural form of "never
        // beside the cage": every room down here hangs off a rib, so a stair beyond the outermost rib has
        // the whole building between it and the car everybody arrives in. Said as a SIDE rather than as a
        // distance, because the corridor is not halved about the cage and a distance would rank a stair out
        // of the plan over two tenths of a deck unit.
        int side = Math.Sign(cageX - stairX);
        foreach ((int ordinal, double ribX) in UndergroundComplex.RibColumnsOn(Field))
        {
            if (Math.Sign(ribX - stairX) != side)
            {
                bad.Add($"  cross corridor {ordinal} at x={ribX:F1} is on the FAR side of the stair "
                    + $"(stair {stairX:F1}, cage {cageX:F1}) — there are rooms outside the second way out.");
            }
        }

        double shoulder = (2 * UndergroundComplex.ShaftHalf) + UndergroundComplex.ShaftClearDu;
        if (Math.Abs(stairX - cageX) < shoulder)
        {
            bad.Add($"  the stair is cut at x={stairX:F1}, {Math.Abs(stairX - cageX):F1} du from the cage "
                + $"at x={cageX:F1} — inside the {shoulder:F1} du a car wants to itself.");
        }

        if (UndergroundComplex.ServiceShaftAt(Field) is { } car
            && Math.Abs(car.X - stairX) < shoulder)
        {
            bad.Add($"  the stair is cut at x={stairX:F1} and the goods car stands at x={car.X:F1} — one "
                + "end, two ways out, which is one way out with a spare.");
        }

        if (UndergroundComplex.SpecimenRecessAt(Field) is { } pocket
            && Math.Abs(pocket.X - stairX) < shoulder)
        {
            bad.Add($"  the stair is cut at x={stairX:F1} and the pocket a stop order's seal stands in is "
                + $"at x={pocket.X:F1} — the escape route opens into the sealed working.");
        }

        Report(bad, 1, "the stair stands clear of the cage, the goods car and the pocket", 1);
    }

    // ── (3) IT CLIMBS OUT, AND IT RUNS NO GATE ───────────────────────────────────────────────────────────

    /// <summary>THE THREE WAYS OUT, EACH ANSWERING FOR ITSELF. The stair reaches the surface (the goods car
    /// still does not, which is what made the cage the single way home), and it runs no gate — §13.5 is a
    /// law about the BUILDING and neither the second car nor a flight of steps may be a way round it.</summary>
    [Fact]
    public void TheStairClimbsOutAndTheGoodsCarStillDoesNotAndNeitherRunsTheGate()
    {
        IReadOnlyList<UndergroundComplex.Shaft> exits = UndergroundComplex.ExitsOn(Field);
        Assert.Equal(3, exits.Count);

        UndergroundComplex.Shaft cage = exits.Single(s => s.Kind == UndergroundComplex.ShaftKind.Cage);
        UndergroundComplex.Shaft goods = exits.Single(s => s.Kind == UndergroundComplex.ShaftKind.Service);
        UndergroundComplex.Shaft stair = exits.Single(s => s.Kind == UndergroundComplex.ShaftKind.Stair);

        // The cage is FIRST, and everything that has ever meant "the way home" takes the first one it finds
        // (the fan's home ring, Map.Surface.Shelter). A list that reordered would move HOME silently.
        Assert.Equal(UndergroundComplex.ShaftKind.Cage, exits[0].Kind);

        Assert.True(cage.ReachesTheSurface, "the cage stopped climbing out.");
        Assert.False(goods.ReachesTheSurface, "the goods car grew a hut on the regolith (#606, #801).");
        Assert.True(stair.ReachesTheSurface,
            "the stair does not reach the surface, so the cage is still the single way home and one radio "
            + "call still ends every escape — which is the whole of #719.");

        Assert.True(cage.RunsTheGate, "the cage stopped running the gate.");
        Assert.False(goods.RunsTheGate, "the goods car runs a gate (§13.5).");
        Assert.False(stair.RunsTheGate,
            "the stair runs a gate — a flight of steps that could cross a band boundary would be depth "
            + "bought without the paper (§13.5).");

        Assert.Equal(UndergroundComplex.StairSign, stair.Sign);

        // The stair's pocket hangs off the UPPER face, like the cage's, so its doorstep is on the same side.
        // A landing that kept its own +1.0 would have put a captain inside the goods car's wall (#801); a
        // stair that inherited the goods car's -1.0 would put them in a chamber.
        Assert.Equal(stair.Y + 1.0, stair.Landing.Y, 6);
    }

    // ── (4) THE CLIMB IS DERIVED, AND IT COSTS MORE THAN THE CAR ─────────────────────────────────────────

    /// <summary>
    /// THE PRICE IS AN ARRANGEMENT OF FOUR THINGS THE GAME ALREADY MEASURES, and it is re-derived here from
    /// those four rather than compared to a number typed into a test — a pinned constant would pass just as
    /// happily if somebody replaced the derivation with the same figure typed once.
    ///
    /// <para>And the ordering the owner asked for: the climb costs MORE than the same distance walked
    /// (<c>HeavyLabour</c> against <c>Walking</c>), which costs more than the car, which charges nothing.
    /// Deeper is dearer, on every floor of every site.</para>
    ///
    /// <para><b>Proven RED</b> by pricing the climb at <c>Breathing.Walking</c>:</para>
    /// <code>
    /// Assert.Equal() Failure: Values are not within 6 decimal places
    /// Expected: 74.943174999999997 (rounded from 74.943174603174612)
    /// Actual:   34.065078999999997 (rounded from 34.065079365079363)
    /// </code>
    /// </summary>
    [Fact]
    public void TheClimbIsDerivedFromTheBuildingAndCostsMoreThanTheRide()
    {
        var bad = new List<string>();
        int seen = 0;

        double traverse = Math.Abs(
            UndergroundComplex.StairShaftAt(Field)!.Value.X - UndergroundComplex.ShaftAt(Field).X);

        foreach (string body in ManySites().Take(20))
        {
            double dearer = -1;
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                if (!UndergroundComplex.HasStairOn(body, level))
                {
                    continue;
                }
                seen++;

                // The derivation, spelled out of its four sources.
                double du = SurfaceScale.DeckUnits(UndergroundComplex.MetresDown(level)) + traverse;
                double walked = SuitAir.WalkHomeSeconds(du);
                double climbed = walked * SuitAir.Breathing.HeavyLabour;

                Assert.Equal(du, UndergroundComplex.ClimbDu(Field, level), 6);
                Assert.Equal(climbed, UndergroundComplex.ClimbAirSeconds(Field, level), 6);

                if (climbed <= walked)
                {
                    bad.Add($"  {body} B{-level}: the climb costs {climbed:F1} s and the same distance "
                        + $"walked costs {walked:F1} s — going up is not costing more air.");
                }
                if (climbed <= 0)
                {
                    bad.Add($"  {body} B{-level}: the climb is free, which is what the car is.");
                }
                if (climbed <= dearer)
                {
                    bad.Add($"  {body} B{-level}: costs {climbed:F1} s and the floor above cost "
                        + $"{dearer:F1} s — depth stopped being paid for in air.");
                }
                dearer = climbed;
            }
        }

        // The car charges nothing, which is the other half of the decision the stair puts in the player's
        // hands. Said here rather than assumed: nothing anywhere spends air on a lift ride.
        Assert.Equal(0.0, UndergroundComplex.ClimbAirSeconds(Field, 0));

        Report(bad, seen, "the climb is derived, and deeper is dearer", 100);
    }

    // ── (5) NOTHING THE BUILDING NEVER DECLARED HAS ONE ──────────────────────────────────────────────────

    /// <summary>
    /// THE STAIR STOPS AT THE LISTED BOTTOM, and that is the fiction and the safety argument at once: nobody
    /// files a means-of-escape drawing for a working they never declared, and a stair that reached the band
    /// nobody listed (#592) or the halls nobody dug (#677) would be a way into them that no gate reads.
    ///
    /// <para><b>Proven RED</b> by relaxing <c>HasStairOn</c> to <c>level &lt; 0</c>:</para>
    /// <code>
    /// 108 of 1182 case(s) break the law: nothing below the listed bottom has a stair
    ///   europa B5: the building never admitted this floor exists and HasStairOn says yes.
    ///   callisto B9: the building never admitted this floor exists and HasStairOn says yes.
    /// </code>
    /// </summary>
    [Fact]
    public void NothingBelowTheListedBottomHasAStair()
    {
        var bad = new List<string>();
        int seen = 0;

        foreach (string body in ManySites())
        {
            int listed = UndergroundComplex.DepthOf(body);
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                seen++;
                bool undeclared = level < listed
                    || UndergroundComplex.IsUnlisted(body, level)
                    || UndergroundComplex.IsFound(body, level);
                if (!undeclared)
                {
                    continue;
                }

                if (UndergroundComplex.HasStairOn(body, level))
                {
                    bad.Add($"  {body} B{-level}: the building never admitted this floor exists and "
                        + "HasStairOn says yes.");
                    continue;
                }

                UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, Field);
                if (floor.Labels.Any(m =>
                        string.Equals(m.Label, UndergroundComplex.StairSign, StringComparison.Ordinal)))
                {
                    bad.Add($"  {body} B{-level}: the building never admitted this floor exists and there "
                        + "is a stair plate on it.");
                }
            }
        }

        Report(bad, seen, "nothing below the listed bottom has a stair", 1000);
    }

    // ── (6) ONE NEW STRING, AND IT IS NOT A CAR'S ────────────────────────────────────────────────────────

    /// <summary>
    /// THE PLATE IS THE WHOLE OF WHAT THIS FEATURE SAYS OUT LOUD. One string, swept for by reflection over
    /// everything <c>UndergroundComplex</c> publishes, because "we added no sentences" is a claim that goes
    /// stale the first afternoon somebody adds one.
    ///
    /// <para>And it does not wear the cars' vocabulary. A stair whose plate said LIFT or CAR would be a sign
    /// reporting a machine that is not there — the house bug class, printed on a wall — and it may not name
    /// anything canon reserves either (§13.8: a fire regulation explains nothing about the Old Ones).</para>
    ///
    /// <para><b>Proven RED</b> by adding a second <c>public const string StairDoorLine</c> beside the
    /// plate:</para>
    /// <code>
    /// the stair speaks with 2 string(s): StairDoorLine, StairSign. The plate is meant to be the only
    /// thing this feature says out loud.
    /// </code>
    /// </summary>
    [Fact]
    public void ThePlateIsTheOnlyStringTheStairEverSays()
    {
        List<string> named = typeof(UndergroundComplex)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string)
                && f.Name.StartsWith("Stair", StringComparison.Ordinal))
            .Select(f => f.Name)
            .Concat(typeof(UndergroundComplex)
                .GetProperties(BindingFlags.Public | BindingFlags.Static)
                .Where(p => p.PropertyType == typeof(string)
                    && p.Name.StartsWith("Stair", StringComparison.Ordinal))
                .Select(p => p.Name))
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        Assert.True(named.Count == 1 && named[0] == "StairSign",
            $"the stair speaks with {named.Count} string(s): {string.Join(", ", named)}. The plate is meant "
            + "to be the only thing this feature says out loud — a beat that wants a sentence gets a "
            + "// FABLE: marker, not a const.");

        // The plate idiom: a glyph and a noun, in the voice CageSign and ServiceCarSign are stencilled in.
        Assert.EndsWith("STAIR", UndergroundComplex.StairSign, StringComparison.Ordinal);

        foreach (string reserved in new[] { "LIFT", "CAR", "CAGE", "GOODS" })
        {
            Assert.False(
                UndergroundComplex.StairSign.Contains(reserved, StringComparison.OrdinalIgnoreCase),
                $"the stair's plate says \"{reserved}\" — a flight of steps wearing the cars' vocabulary is "
                + "a sign reporting a machine that is not there.");
        }

        foreach (string canon in new[] { "reever", "old one", "kaamos", "restore" })
        {
            Assert.False(UndergroundComplex.StairSign.Contains(canon, StringComparison.OrdinalIgnoreCase),
                $"the stair's plate says \"{canon}\" — §13.8: a second means of escape is a fire "
                + "regulation, and that is all it ever needs to be.");
        }
    }
}
