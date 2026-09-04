using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #719 slice 2 · <b>THE MAINTENANCE BREAK, AND THE ONE THING IT MAY NEVER BE.</b>
///
/// <para>Owner, 2026-08-05: <i>"what if the elevator needs maintenance break :-)"</i> —
/// <i>"just stopping the elevator by remote of radio message would stop all escape way too easy :-d"</i> —
/// <i>"going up would use more air"</i>. The middle sentence is the whole of what this file guards. A radio
/// call that takes the car away is a horror beat only where a second way out exists; anywhere else it is a
/// softlock with atmosphere.</para>
///
/// <para><b>The law is stated as an equality rather than as a promise.</b>
/// <see cref="UndergroundComplex.ACallCanStopTheCarOn"/> is <see cref="UndergroundComplex.HasStairOn"/>
/// with the hulls taken out, so the floors a break may land on and the floors #1115 cut a stair into are ONE
/// list. That is what makes the ordering law a condition instead of a convention — and it is asked here of
/// the whole generated site list, because "the car may only be stopped where the stair is" is a claim about
/// the GENERATOR and not about the ten sites a player is likely to visit.</para>
///
/// <para>The rest is the price: the way home with the car gone, in the units the suit speaks, quoting
/// exactly what the press actually charges.</para>
/// </summary>
public sealed class TheMaintenanceBreakIsAPriceTests
{
    private static SurfaceLayout.Field Field => SurfaceLayout.DefaultField;

    /// <summary>#1115's own net, and for its reason: the scenario's moons, a wide sweep of generated ids,
    /// and the head office.</summary>
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
        // ANTI-VACUITY, this file's copy of #1115's: a sweep that walked nothing passes, and a guard handed
        // a world that cannot tell pass from fail is the fifth named bug class in this repository.
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

    // ── (1) THE ORDERING LAW, AS ARITHMETIC ─────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE CAR MAY BE STOPPED ON EXACTLY THE FLOORS THE STAIR IS CUT INTO — and nowhere else.</b>
    ///
    /// <para>Every listed floor of every site, both directions: a floor with a stair must admit the break
    /// (or the beat cannot happen at all) and a floor without one must refuse it (or #719's ordering law is
    /// broken and a radio call is a death sentence). The band nobody listed (#592) and the halls nobody dug
    /// (#677) are on the refusing side, which is where they belong: nobody files a means-of-escape drawing
    /// for a working they never declared, and a break down there would leave one exit and then take it.</para>
    ///
    /// <para><b>Proven RED</b> by relaxing <c>ACallCanStopTheCarOn</c> to <c>level &lt; 0</c>:</para>
    /// <code>
    /// 108 of 1182 case(s) break the law: the car stops only where a second way out is cut
    ///   callisto B10: no stair on this floor and the car may be stopped on it — one radio call and the
    ///   captain is in a building with no way out of it.
    /// </code>
    /// </summary>
    [Fact]
    public void TheCarMayOnlyBeStoppedWhereTheStairIs()
    {
        var bad = new List<string>();
        int seen = 0;

        foreach (string body in ManySites())
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                seen++;
                bool stair = UndergroundComplex.HasStairOn(body, level);
                bool mayStop = UndergroundComplex.ACallCanStopTheCarOn(body, level);

                if (stair && !mayStop)
                {
                    bad.Add($"  {body} B{-level}: a stair is cut on this floor and the car cannot be "
                        + "stopped on it — the beat the stair was shipped to make survivable never happens.");
                }
                else if (!stair && mayStop)
                {
                    bad.Add($"  {body} B{-level}: no stair on this floor and the car may be stopped on it "
                        + "— one radio call and the captain is in a building with no way out of it.");
                }
            }
        }

        Report(bad, seen, "the car stops only where a second way out is cut", 1000);
    }

    /// <summary>
    /// <b>NEVER ON THE SURFACE, AND NEVER ON A WRECK.</b> Two grounds where the whole apparatus is absent:
    /// the regolith, which has no car to stop and one tube that is the ship's own, and a hull, which has no
    /// complex, no cage, no rota and nobody to radio.
    ///
    /// <para>The wreck arm is not covered by the floor sweep above, and that is exactly why it is here:
    /// <see cref="UndergroundComplex.DepthOf"/> rolls a depth for ANY id it is handed, wreck ids included,
    /// so <c>HasStairOn</c> alone would have said yes to a hull. A guard that only walked the site list
    /// would never have found out.</para>
    ///
    /// <para><b>Proven RED</b> by dropping the <c>TryParseWreckId</c> arm out of
    /// <c>ACallCanStopTheCarOn</c>:</para>
    /// <code>
    /// wreck-1 B1 admits a maintenance break: a hull has no cage to stop and nobody on it to make the call.
    /// </code>
    /// </summary>
    [Fact]
    public void NoBreakOnTheSurfaceAndNoneOnAHull()
    {
        foreach (string body in new[] { "luna", "titan", "secret-lab-site" })
        {
            foreach (int level in new[] { 0, 1, 4 })
            {
                Assert.False(UndergroundComplex.ACallCanStopTheCarOn(body, level),
                    $"{body} at level {level} admits a maintenance break — above the lid there is no car to "
                    + "stop and the way home is the captain's own tube.");
            }
        }

        // The wreck ids the game actually makes, asked of the FLOOR the break would land on if anything
        // ever put a captain there.
        foreach (string wreck in new[] { "wreck-1", "wreck-42", Derelict.BodyIdFor("abandoned-hauler-3") })
        {
            Assert.True(Derelict.TryParseWreckId(wreck, out _),
                $"\"{wreck}\" is not a wreck id any more, so this case is about nothing.");

            for (int level = -1; level >= -4; level--)
            {
                Assert.False(UndergroundComplex.ACallCanStopTheCarOn(wreck, level),
                    $"{wreck} B{-level} admits a maintenance break: a hull has no cage to stop and nobody "
                    + "on it to make the call.");
            }
        }
    }

    // ── (2) THE PRICE ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE WAY HOME WITH THE CAR STOPPED IS THE WALK TO THE DOOR PLUS THE CLIMB — priced in the seconds
    /// the press actually takes out of the tank.</b>
    ///
    /// <para>Read the way a player reads it: stand somewhere, ask the suit what the way home costs, and
    /// subtract what the walk to the door costs. What is left must be
    /// <see cref="UndergroundComplex.ClimbAirSeconds"/> to the last decimal — not approximately, not "in the
    /// same ballpark", because it is the SAME function run backwards and a difference of any size would mean
    /// the suit is quoting one journey while <c>ClimbTheStairOut</c> charges for another. That is this
    /// project's oldest bug class on the one resource the game kills people with.</para>
    ///
    /// <para>Swept over every listed floor of the shipped sites and from four standing spots, because the
    /// walk leg is the part that varies and a single reading at the stair's own door would prove nothing
    /// about it.</para>
    ///
    /// <para><b>Proven RED</b> by pricing the climb at <c>ClimbDu</c> instead of
    /// <c>ClimbAirSeconds * WalkSpeedDu</c> — dropping the heavy-labour band, which is exactly the mistake a
    /// second author of this arithmetic would make:</para>
    /// <code>
    /// 224 of 224 case(s) break the law: the readout prices the climb the press charges
    ///   luna B1 from (-140.0, -57.0): the suit quotes 34.1 s for the climb and the press takes 74.9 s.
    /// </code>
    /// </summary>
    [Fact]
    public void TheReadoutPricesTheClimbThePressCharges()
    {
        var bad = new List<string>();
        int seen = 0;

        (double X, double Y)[] spots =
        [
            (Field.LeftX + 20.0, Field.LandingBandY - 30.0),
            (0.0, 0.0),
            (Field.RightX - 20.0, Field.LandingBandY - 30.0),
            (60.0, 40.0),
        ];

        foreach (string body in new[]
        {
            "luna", "phobos", "europa", "titan", "miranda", "triton", "secret-lab-site",
        })
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                if (!UndergroundComplex.ACallCanStopTheCarOn(body, level))
                {
                    continue;
                }
                if (UndergroundComplex.StairRingAt(Field) is not { } door)
                {
                    bad.Add($"  {body} B{-level}: there is no stair door to walk to on this ground.");
                    continue;
                }

                foreach ((double x, double y) in spots)
                {
                    seen++;

                    double home = UndergroundComplex.WayOutByStairDu(Field, level, x, y);
                    double walk = Math.Sqrt(
                        ((door.X - x) * (door.X - x)) + ((door.Y - y) * (door.Y - y)));

                    double quoted = SuitAir.WalkHomeSeconds(home) - SuitAir.WalkHomeSeconds(walk);
                    double charged = UndergroundComplex.ClimbAirSeconds(Field, level);

                    if (Math.Abs(quoted - charged) > 1e-6)
                    {
                        bad.Add($"  {body} B{-level} from ({x:F1}, {y:F1}): the suit quotes {quoted:F1} s "
                            + $"for the climb and the press takes {charged:F1} s.");
                        continue;
                    }

                    if (charged <= 0)
                    {
                        bad.Add($"  {body} B{-level}: the climb is free, which is what the CAR is.");
                    }
                }
            }
        }

        Report(bad, seen, "the readout prices the climb the press charges", 200);
    }

    /// <summary>
    /// <b>AND THE BREAK IS A PRICE RATHER THAN A DEATH.</b> The climb out of the deepest floor in the game
    /// costs more than a full reserve and less than a full tank — which is what makes it a decision made with
    /// the gauge in hand rather than a sentence read off a card.
    ///
    /// <para>Both bounds matter and they are asked of Core's own numbers rather than of typed ones. A climb
    /// cheaper than the reserve would be a break nobody notices; a climb no full tank could pay would be the
    /// softlock #719 was opened to prevent, wearing an air gauge.</para>
    ///
    /// <para><b>Proven RED</b> by returning <c>0.0</c> from <c>ClimbAirSeconds</c>:</para>
    /// <code>
    /// the deepest climb in the game (enceladus B24) costs 0.0 s and the reserve alone is 75.0 s — a
    /// break nobody has to think about is not a price.
    /// </code>
    /// </summary>
    [Fact]
    public void TheDeepestClimbCostsMoreThanTheReserveAndLessThanATank()
    {
        int deepest = 0;
        string where = "";

        foreach (string body in ManySites())
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                if (!UndergroundComplex.ACallCanStopTheCarOn(body, level))
                {
                    continue;
                }
                // Chosen by DEPTH and then measured, never by the measurement itself: a version of this
                // that picked the largest climb would go quiet rather than red the day the climb was free,
                // and "nothing was walked" is a weaker finding than "the deepest climb costs nothing".
                if (level < deepest)
                {
                    deepest = level;
                    where = body;
                }
            }
        }

        Assert.True(deepest < 0, "no floor in the whole generated list admits a break — nothing was walked.");

        double worst = UndergroundComplex.ClimbAirSeconds(Field, deepest);
        double reserve = SuitAir.ReserveSeconds;
        Assert.True(worst > reserve,
            $"the deepest climb in the game ({where} B{-deepest}) costs {worst:F1} s and the reserve alone "
            + $"is {reserve:F1} s — a break nobody has to think about is not a price.");
        Assert.True(worst < SuitAir.TankSeconds,
            $"the deepest climb in the game ({where} B{-deepest}) costs {worst:F1} s out of a "
            + $"{SuitAir.TankSeconds:F1} s tank — that is not a price, it is the softlock #719 was opened "
            + "to prevent wearing an air gauge.");
    }

    // ── (3) ONE NEW STRING ──────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE PLATE IS THE WHOLE OF WHAT THIS SLICE SAYS OUT LOUD</b> — swept for by reflection over
    /// everything <see cref="UndergroundComplex"/> publishes, because "we added no sentences" is a claim
    /// that goes stale the first afternoon somebody adds one.
    ///
    /// <para>Verbatim from the canon pass, and nothing on it explains who called it: no name, no rota, no
    /// SECURITY. It may wear the cars' vocabulary — unlike <see cref="UndergroundComplex.StairSign"/>,
    /// which may not — because the machine it names is the machine that has stopped. What it may never do is
    /// borrow a word canon reserves (§13.8, and §8's monolith): a maintenance break explains nothing about
    /// the Old Ones and never tries.</para>
    ///
    /// <para><b>Proven RED</b> by adding a second <c>public const string CarStoppedLine</c> beside the
    /// plate:</para>
    /// <code>
    /// the break speaks with 2 string(s): CarStoppedLine, CarStoppedPlate. The plate is meant to be the
    /// only thing this slice says out loud.
    /// </code>
    /// </summary>
    [Fact]
    public void ThePlateIsTheOnlyStringTheBreakEverSays()
    {
        // NOT "Maintenance": UndergroundComplex.MaintenanceLedgerLine is #537's burial ledger and predates
        // this slice by a mile. A sweep that dragged an older feature's sentence in would fail on day one and
        // teach the next crew to widen the net rather than to keep it honest.
        static bool Break(string name) =>
            name.Contains("Stopped", StringComparison.OrdinalIgnoreCase)
            || name.Contains("CarStop", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Break", StringComparison.OrdinalIgnoreCase);

        List<string> named = typeof(UndergroundComplex)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string) && Break(f.Name))
            .Select(f => f.Name)
            .Concat(typeof(UndergroundComplex)
                .GetProperties(BindingFlags.Public | BindingFlags.Static)
                .Where(p => p.PropertyType == typeof(string) && Break(p.Name))
                .Select(p => p.Name))
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        Assert.True(named.Count == 1 && named[0] == "CarStoppedPlate",
            $"the break speaks with {named.Count} string(s): {string.Join(", ", named)}. The plate is meant "
            + "to be the only thing this slice says out loud — a beat that wants a sentence gets a "
            + "// FABLE: marker, not a const.");

        // Verbatim from the 2026-09-04 canon pass. Retyped here on purpose: a guard that read the constant
        // into itself would agree with any edit at all.
        Assert.Equal("CAR STOPPED · MAINTENANCE", UndergroundComplex.CarStoppedPlate);

        // NOTHING EXPLAINS WHO CALLED IT. The register is the whole beat (§13.8, #603's inference horror):
        // a captain who has just been read and refused walks back to a car with no floors on it and is left
        // to draw the one inference the game will never confirm.
        foreach (string tell in new[] { "security", "guard", "patrol", "radio", "called", "you" })
        {
            Assert.False(
                UndergroundComplex.CarStoppedPlate.Contains(tell, StringComparison.OrdinalIgnoreCase),
                $"the plate says \"{tell}\" — the building does not explain its own consequence to the "
                + "person it is happening to.");
        }

        foreach (string canon in new[] { "monolith", "reever", "old one", "kaamos", "restore" })
        {
            Assert.False(
                UndergroundComplex.CarStoppedPlate.Contains(canon, StringComparison.OrdinalIgnoreCase),
                $"the plate says \"{canon}\" — a lift out of service explains nothing about the Old Ones, "
                + "and §8's word is not lent to a maintenance ticket.");
        }
    }
}
