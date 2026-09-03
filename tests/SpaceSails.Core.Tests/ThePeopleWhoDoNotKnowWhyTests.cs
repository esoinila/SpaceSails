using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #1068 · <b>THROUGH PEOPLE WHO DO NOT KNOW WHY</b> — the watchers' third manifestation channel, under
/// #672's owner-blessed doctrine (2026-09-01). The burial (#1063) delivered this channel's third act; these
/// are the two it left standing: <b>a berth reassigned</b> and <b>a moon repriced overnight</b>.
///
/// <para>Every guard below was watched go RED against a revert of the behaviour it names — the revert is
/// listed on each one, in the shape this ground has used since #587's lesson: <b>a guard that has never
/// failed is a guard nobody has checked</b>.</para>
///
/// <para><b>THE WORLD IS THE SHIPPED ONE.</b> Both deliveries land at a HARBOUR, and a harbour is a fact
/// about a scenario: which havens exist, what their traffic is, which well they orbit. A hand-built three-
/// body sky would let every guard here pass against a world that cannot tell a great port from an outpost,
/// which is the fifth named bug class exactly. So the sweeps run over Sol's own moons and Sol's own berths,
/// and the populations they run over are DERIVED and asserted to be real rather than typed.</para>
///
/// <para><b>THE REGISTER IS AMBIENT AND IS RESTORED IN A <c>finally</c>.</b> It only ever changes the answer
/// for the ids in it, which is what makes that safe — <see cref="PoliteDecline.Install"/> records what this
/// ground paid to learn the rule. Nothing else in this assembly reads <see cref="QuietHands"/> or
/// <see cref="DockRoster"/>, and no guard here installs a register it does not take back.</para>
/// </summary>
public sealed class ThePeopleWhoDoNotKnowWhyTests
{
    private static CircularOrbitEphemeris Sol() =>
        CircularOrbitEphemeris.FromScenario(SimulatorTests.LoadSol());

    private static void WithHands(IReadOnlyList<QuietHands.Hand> hands, Action body)
    {
        IReadOnlyList<QuietHands.Hand> had = QuietHands.Handled;
        QuietHands.Install(hands);
        try
        {
            body();
        }
        finally
        {
            QuietHands.Install(had);
        }
    }

    /// <summary>Every moon in the shipped scenario that a harbour actually serves, paired with that harbour
    /// — derived off <see cref="QuietHands.PortFor"/> rather than typed, and asserted to be a real
    /// population. A world where no ground had a port would pass every negative law in this file for the
    /// wrong reason.</summary>
    private static List<(string Ground, string Port)> ServedGrounds(ICelestialEphemeris sky)
    {
        var served = new List<(string, string)>();
        foreach (CelestialBody body in sky.Bodies)
        {
            if (body.Kind == BodyKind.Station || body.IsHaven || body.ParentId is null)
            {
                continue;   // a ground is something you land on, not a berth and not the Sun
            }
            if (QuietHands.PortFor(sky, body.Id) is { } port)
            {
                served.Add((body.Id, port.Id));
            }
        }

        Assert.True(served.Count >= 6,
            $"only {served.Count} shipped ground(s) have a harbour — this sample proves little.");
        return served;
    }

    /// <summary>…and of those, the ones whose harbour keeps more than one berth, which is the population the
    /// reassignment can possibly act on. Asserted real for the same reason: a port with one collar cannot
    /// reassign anybody, and a sweep that found only those would be green and empty.</summary>
    private static List<(string Ground, string Port)> GroundsWithAChoiceOfBerths(ICelestialEphemeris sky)
    {
        List<(string Ground, string Port)> many =
            [.. ServedGrounds(sky).Where(g => DockRoster.BerthsAt(sky, g.Port) > 1)];

        Assert.True(many.Count >= 4,
            $"only {many.Count} shipped ground(s) are served by a port with a choice of berths.");
        return many;
    }

    private static DisclosureClock.Opening Opened(string ground, long window) => new(ground, window);

    // ══ THE TRIGGER ═════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// NOTHING MOVES ON THE VISIT THAT OPENED THE GROUND, AND EVERYTHING MOVES ONE WHOLE WINDOW LATER.
    ///
    /// <para>#672's instrument law, said about a desk: a roster retyped before the captain had climbed back
    /// out of the seam he had just crossed would be a decision taken about him, inside the hour, by an
    /// office that watched him take it. The positive twin ships beside it because "nothing happened" is this
    /// feature's ordinary answer and a world that never acted would pass the negative alone.</para>
    ///
    /// <para><b>RED against:</b> <c>WindowsBeforeTheHandMoves = 0</c> —
    /// <i>"the harbour had already retyped the roster in the window he crossed the seam in"</i>.</para>
    /// </summary>
    [Fact]
    public void NothingMovesUntilOneWholeWindowHasPassed()
    {
        const string ground = "hand-ground-a";
        IReadOnlyList<DisclosureClock.Opening> opened = [Opened(ground, 0)];

        IReadOnlyList<QuietHands.Hand> none = QuietHands.Note(opened, [], null, 0.0);   // the crossing's own window
        Assert.Empty(none);

        double aWindowLater = DisclosureClock.WindowSeconds * QuietHands.WindowsBeforeTheHandMoves;
        IReadOnlyList<QuietHands.Hand> moved = QuietHands.Note(opened, [], null, aWindowLater);
        Assert.Single(moved);
        Assert.Equal(ground, moved[0].BodyId);
        Assert.False(moved[0].BerthGiven, "a filed reassignment has not been handed over yet");
    }

    /// <summary>
    /// NOTHING MOVES ON A GROUND NOBODY HAS BEEN PAST THE SEAM OF — and the register is handed BACK BY
    /// REFERENCE, which is what lets the caller ask for a save only when something happened.
    ///
    /// <para><b>RED against:</b> <see cref="QuietHands.Note"/> returning <c>[.. had]</c> instead of
    /// <c>had</c> on the empty-register path — <i>"a voyage where nobody has opened anything asked for a
    /// save on every descent"</i>. The by-reference return IS this law as the caller meets it: the client
    /// only writes a vault when the register it got back is not the one it handed in.</para>
    /// </summary>
    [Fact]
    public void NothingMovesOnAGroundNobodyHasOpened()
    {
        IReadOnlyList<QuietHands.Hand> had = [];
        double late = DisclosureClock.WindowSeconds * 40;

        Assert.Same(had, QuietHands.Note(null, had, null, late));
        Assert.Same(had, QuietHands.Note([], had, null, late));

        // …and the twin, so the sweep above is not green because nothing can ever be filed.
        Assert.Single(QuietHands.Note([Opened("hand-ground-b", 0)], had, null, late));
    }

    /// <summary>
    /// NOTHING MOVES WHILE HE IS STANDING ON IT. Both deliveries are things he comes back to FIND — a slot
    /// that is not the one he had, a price that is not the one he paid — and an act with a witness is a
    /// thing he could describe.
    ///
    /// <para><b>RED against:</b> dropping the <c>standingOn</c> clause in <see cref="QuietHands.Note"/> —
    /// <i>"the harbour filed his ground while he was walking about on it"</i>.</para>
    /// </summary>
    [Fact]
    public void NothingMovesWhileHeIsStandingOnIt()
    {
        const string ground = "hand-ground-c";
        IReadOnlyList<DisclosureClock.Opening> opened = [Opened(ground, 0)];
        IReadOnlyList<QuietHands.Hand> had = [];
        double late = DisclosureClock.WindowSeconds * 9;

        Assert.Same(had, QuietHands.Note(opened, had, ground, late));
        Assert.Single(QuietHands.Note(opened, had, "somewhere-else", late));
        Assert.Single(QuietHands.Note(opened, had, null, late));
    }

    /// <summary>
    /// IT IS NOT FARMABLE: GOING BACK CHANGES NOTHING. Ten more descents on the same ground file nothing
    /// more and move no window — law four of <see cref="DisclosureClock"/>, carried into its third customer.
    ///
    /// <para><b>RED against:</b> dropping the <c>HandOn(had, …) is not null</c> guard in
    /// <see cref="QuietHands.Note"/> — <i>"eleven rows for one ground, one per visit"</i>.</para>
    /// </summary>
    [Fact]
    public void GoingBackMoreOftenFilesNothingMore()
    {
        const string ground = "hand-ground-d";
        IReadOnlyList<DisclosureClock.Opening> opened = [Opened(ground, 0)];

        IReadOnlyList<QuietHands.Hand> hands = QuietHands.Note(opened, [], null, DisclosureClock.WindowSeconds * 2);
        Assert.Single(hands);
        long filedIn = hands[0].Window;

        for (int visit = 0; visit < 10; visit++)
        {
            IReadOnlyList<QuietHands.Hand> again =
                QuietHands.Note(opened, hands, null, DisclosureClock.WindowSeconds * (3 + visit));
            Assert.Same(hands, again);
        }

        Assert.Equal(filedIn, hands[0].Window);
    }

    // ══ THE PORT THAT SERVES A GROUND ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// ONE PORT RULE, AND IT DOES NOT MOVE WITH THE CLOCK. Both deliveries land at the same harbour, and it
    /// has to be the same harbour on the outbound leg and on the way back — a port that changed with the
    /// planets would owe a berth at one station and hand it over at another.
    ///
    /// <para>The answer is also asserted to be a real berth in the scenario rather than merely non-null.</para>
    ///
    /// <para><b>RED against:</b> ranking <see cref="QuietHands.PortFor"/> by live distance instead of by the
    /// timetable — <i>"the harbour that serves Europa was Ringside on the way out and the Red Eye coming
    /// back"</i>.</para>
    /// </summary>
    [Fact]
    public void ThePortThatServesAGroundIsTheSamePortOnEveryLeg()
    {
        CircularOrbitEphemeris sky = Sol();
        var berths = new HashSet<string>(DockableHavens.AllIds(sky), StringComparer.Ordinal);

        foreach ((string ground, string port) in ServedGrounds(sky))
        {
            Assert.Contains(port, berths);
            Assert.Equal(port, QuietHands.PortFor(sky, ground)!.Id);      // asked twice
            Assert.Equal(port, QuietHands.PortFor(Sol(), ground)!.Id);    // asked of a second world
            Assert.Contains(port, ArrivalTube.Neighbourhood(sky, ground)); // and it is in the ground's system
        }
    }

    // ══ DELIVERY 1 · A BERTH REASSIGNED ═════════════════════════════════════════════════════════════════

    /// <summary>
    /// THE BERTH HE IS GIVEN IS A DIFFERENT ONE — EXACTLY ONCE, AND THEN NEVER AGAIN.
    ///
    /// <para>Three statements in one sweep, because they are one behaviour: the ordinary berth at a port
    /// never moves; the reassigned berth is not that berth; and the moment it has been handed over the port
    /// is back to the ordinary berth for ever. The last is what "never twice in a row" means, and it is the
    /// only direction this feature was ever farmable in.</para>
    ///
    /// <para><b>RED against:</b> three reverts, watched fail one at a time —
    /// <see cref="DockRoster.BerthGiven"/> ignoring its window (<i>"the reassigned berth was the berth he
    /// already had"</i>); <see cref="QuietHands.GiveTheBerth"/> returning the register unchanged
    /// (<i>"the clamp did not spend the reassignment"</i>); and <c>OwedGroundAt</c> not reading
    /// <c>BerthGiven</c> at all (<i>"the port owed him a different berth again on the next clamp, and the
    /// one after"</i>).</para>
    /// </summary>
    [Fact]
    public void TheReassignedBerthDiffersFromTheOrdinaryOneExactlyOnce()
    {
        CircularOrbitEphemeris sky = Sol();
        int checkedPorts = 0;

        foreach ((string ground, string port) in GroundsWithAChoiceOfBerths(sky))
        {
            int berths = DockRoster.BerthsAt(sky, port);
            int ordinary = DockRoster.OrdinaryBerth(port, berths);
            Assert.InRange(ordinary, 0, berths - 1);
            Assert.Equal(ordinary, DockRoster.OrdinaryBerth(port, berths));   // it never moves

            var filed = new QuietHands.Hand(ground, 7, BerthGiven: false);
            WithHands([filed], () =>
            {
                long owed = QuietHands.BerthOwedAt(sky, port)
                    ?? throw new Xunit.Sdk.XunitException($"{port} owed no reassignment for {ground}");
                Assert.Equal(filed.Window, owed);

                int given = DockRoster.BerthGiven(port, berths, owed);
                Assert.InRange(given, 0, berths - 1);
                Assert.NotEqual(ordinary, given);
                Assert.Equal(given, DockRoster.BerthGiven(port, berths, owed));   // and it is stable

                // …and it is spent by being given.
                IReadOnlyList<QuietHands.Hand> after = QuietHands.GiveTheBerth([filed], sky, port);
                Assert.True(after[0].BerthGiven, $"{port}: the clamp did not spend the reassignment");
                WithHands(after, () =>
                {
                    Assert.Null(QuietHands.BerthOwedAt(sky, port));
                    Assert.Equal(ordinary, DockRoster.BerthGiven(port, berths, QuietHands.BerthOwedAt(sky, port)));

                    // …and it stays spent however many times he ties up here again.
                    IReadOnlyList<QuietHands.Hand> again = QuietHands.GiveTheBerth(after, sky, port);
                    Assert.Same(after, again);
                });
            });
            checkedPorts++;
        }

        Assert.True(checkedPorts >= 4, $"only {checkedPorts} port(s) were swept.");
    }

    /// <summary>
    /// THE BERTH KIND IS UNTOUCHED. #1066 counts a great port as a run ashore and every other berth as a
    /// working stop, and #1078's establishing shot is the same read; a reassignment that could change the
    /// tier would break the captain's word to his crew by moving a ship thirty metres sideways.
    ///
    /// <para>Also swept: the given berth is a berth of THIS port's roster. A slot outside <c>[0, berths)</c>
    /// is a hull tied to a station that does not have that many berths.</para>
    ///
    /// <para><b>RED against:</b> dropping the <c>% berths</c> wrap in <see cref="DockRoster.BerthGiven"/> —
    /// <i>"the roster gave him berth 17 of 12"</i>.</para>
    /// </summary>
    [Fact]
    public void TheBerthKindSurvivesTheReassignment()
    {
        CircularOrbitEphemeris sky = Sol();

        foreach ((string ground, string port) in GroundsWithAChoiceOfBerths(sky))
        {
            ArrivalTube.Tier before = ArrivalTube.TierFor(sky, port);
            int berths = DockRoster.BerthsAt(sky, port);

            WithHands([new QuietHands.Hand(ground, 3, BerthGiven: false)], () =>
            {
                Assert.Equal(before, ArrivalTube.TierFor(sky, port));
                Assert.Equal(berths, DockRoster.BerthsAt(sky, port));
                Assert.Equal(ArrivalTube.BeatFor(before), ArrivalTube.BeatFor(ArrivalTube.TierFor(sky, port)));

                int given = DockRoster.BerthGiven(port, berths, QuietHands.BerthOwedAt(sky, port));
                Assert.InRange(given, 0, berths - 1);
            });
        }
    }

    /// <summary>
    /// AND THE HULL IS ACTUALLY SOMEWHERE ELSE. The delivery is not a number in a register: the clamp pins
    /// the ship on the slot's own bearing, so a reassigned captain is tied up on another side of the same
    /// station — same standoff, same rail, same envelope, different berth.
    ///
    /// <para><b>RED against:</b> <see cref="BerthState.CoMoving"/> ignoring <c>bearingRadians</c> —
    /// <i>"the reassigned berth put the hull exactly where the old one did"</i>.</para>
    /// </summary>
    [Fact]
    public void TheReassignedBerthPutsTheHullSomewhereElseOnTheSameRail()
    {
        CircularOrbitEphemeris sky = Sol();

        foreach ((string ground, string port) in GroundsWithAChoiceOfBerths(sky))
        {
            CelestialBody station = sky.Bodies.First(b => b.Id == port);
            Vector2d at = sky.Position(port, 0);
            Vector2d vel = TransferMath.BodyVelocity(sky, port, 0);

            ShipState ordinary = BerthState.CoMoving(
                sky, port, 0, BerthState.BerthOffsetMeters, 0, DockRoster.BearingAt(sky, port));

            WithHands([new QuietHands.Hand(ground, 5, BerthGiven: false)], () =>
            {
                ShipState moved = BerthState.CoMoving(
                    sky, port, 0, BerthState.BerthOffsetMeters, 0, DockRoster.BearingAt(sky, port));

                Assert.True((moved.Position - ordinary.Position).Length > 1.0,
                    $"{port}: the reassigned berth pinned the hull in the same place as the ordinary one");

                // Same rail, same reach, same clamp — only the bearing moved.
                Assert.Equal(BerthState.BerthOffsetMeters, (moved.Position - at).Length, 3);
                Assert.Equal(BerthState.BerthOffsetMeters, (ordinary.Position - at).Length, 3);
                Assert.True(DockRule.InEnvelope(moved, at, vel, station.BodyRadius),
                    $"{port}: a reassigned berth must still satisfy the envelope the clamp demands");
            });
        }
    }

    // ══ DELIVERY 2 · A MOON REPRICED OVERNIGHT ══════════════════════════════════════════════════════════

    /// <summary>
    /// THE PRICE MOVES BY THE MARKET'S OWN MOVE AND NEVER FURTHER — at the ground's own port, and at no
    /// other pump in the system.
    ///
    /// <para>The band is the belt markup, which is the only price move this market has ever published
    /// anywhere; a Scully reads it as a pump putting its prices up, because that is what it is. The sweep
    /// files EVERY served ground at once, so a planet whose moons were all opened cannot add up past the
    /// band — which is the clamp, and the clamp is the volatility law itself.</para>
    ///
    /// <para><b>RED against:</b> dropping the <c>Math.Clamp</c> in
    /// <see cref="QuietHands.PulsePriceMoveAt"/> — <i>"the pump at the Red Eye had moved three credits"</i>
    /// — and, for the twin, dropping its served-port filter so every pump takes every ground's move,
    /// <i>"the price at Selene Gate moved because somebody opened a hall at Jupiter"</i>.</para>
    /// </summary>
    [Fact]
    public void TheRepricingStaysInsideTheMarketsOwnBand()
    {
        CircularOrbitEphemeris sky = Sol();
        List<(string Ground, string Port)> served = ServedGrounds(sky);
        int band = QuietHands.PulsePriceBandCr;

        Assert.True(band > 0, "the market publishes no spread at all — nothing could move inside it.");
        Assert.Equal(FuelMarket.OuterPricePerPulse - FuelMarket.InnerPricePerPulse, band);

        // THE WORLD HAS TO BE ABLE TO TELL PASS FROM FAIL. A clamp is only guarded by a register that would
        // BREACH the band without it, so the sweep needs a port serving more than one ground AND a window
        // for each of those grounds that moves the price the SAME way. Both are searched, never typed: the
        // grouping is the scenario's own (Jupiter's moons all fuel at the Red Eye), and the windows are
        // found by asking the rule itself. A register whose directions happened to cancel would pass an
        // unclamped sum perfectly — which is exactly what the first draft of this guard did.
        (string Port, List<string> Grounds) crowded = served
            .GroupBy(g => g.Port, StringComparer.Ordinal)
            .Select(g => (Port: g.Key, Grounds: g.Select(x => x.Ground).ToList()))
            .OrderByDescending(g => g.Grounds.Count)
            .ThenBy(g => g.Port, StringComparer.Ordinal)
            .First();

        Assert.True(crowded.Grounds.Count >= 2,
            $"no shipped port serves two grounds ({crowded.Port} serves {crowded.Grounds.Count}) — "
            + "nothing here could ever breach the band, so nothing here is guarded.");

        var sameWay = new List<QuietHands.Hand>();
        foreach (string ground in crowded.Grounds)
        {
            long? found = null;
            for (long window = 0; window < 64 && found is null; window++)
            {
                long w = window;
                WithHands([new QuietHands.Hand(ground, w, BerthGiven: false)], () =>
                {
                    if (QuietHands.PulsePriceMoveAt(sky, crowded.Port) == band)
                    {
                        found = w;
                    }
                });
            }
            Assert.NotNull(found);
            sameWay.Add(new QuietHands.Hand(ground, found!.Value, BerthGiven: false));
        }

        // …and the other direction exists too, or "one direction" would be a coin with one face.
        bool everDown = false;
        for (long window = 0; window < 64 && !everDown; window++)
        {
            WithHands([new QuietHands.Hand(crowded.Grounds[0], window, BerthGiven: false)], () =>
                everDown |= QuietHands.PulsePriceMoveAt(sky, crowded.Port) == -band);
        }
        Assert.True(everDown, $"{crowded.Grounds[0]} never moved its port's price DOWN in 64 windows.");

        var ports = new HashSet<string>(served.Select(s => s.Port), StringComparer.Ordinal);
        WithHands(sameWay, () =>
        {
            // THE CLAMP: three grounds all pushing the same way is still one credit a pulse.
            Assert.Equal(band, QuietHands.PulsePriceMoveAt(sky, crowded.Port));

            foreach (CelestialBody body in sky.Bodies)
            {
                int move = QuietHands.PulsePriceMoveAt(sky, body.Id);
                Assert.InRange(move, -band, band);
                if (!string.Equals(body.Id, crowded.Port, StringComparison.Ordinal))
                {
                    Assert.Equal(0, move);   // no pump but the served ground's own has moved at all
                }
            }
        });

        // …and the whole world's grounds at once still moves no pump past the band.
        WithHands([.. served.Select((g, i) => new QuietHands.Hand(g.Ground, i, BerthGiven: false))], () =>
        {
            int moved = 0;
            foreach (CelestialBody body in sky.Bodies)
            {
                int move = QuietHands.PulsePriceMoveAt(sky, body.Id);
                Assert.InRange(move, -band, band);
                if (!ports.Contains(body.Id))
                {
                    Assert.Equal(0, move);
                }
                else if (move != 0)
                {
                    moved++;
                }
            }
            Assert.True(moved >= 2, $"only {moved} port(s) repriced — this sweep proves little.");
        });

        // …and with nothing filed, nothing anywhere has moved.
        WithHands([], () =>
        {
            foreach (CelestialBody body in sky.Bodies)
            {
                Assert.Equal(0, QuietHands.PulsePriceMoveAt(sky, body.Id));
            }
        });
    }

    /// <summary>
    /// THE MOVE IS THE SAME MOVE ON THE SECOND VISIT AND THE TENTH, AND IT REACHES THE RECEIPT.
    ///
    /// <para>A price that walked about between two fills would not be weather, it would be an event — and an
    /// event is a fact about somebody deciding, which is the Scully law spent on a pump. The second half of
    /// the guard is the seam: the quote the trade desk prints is the belt price with the move on it, and it
    /// can never go free however far down the market went.</para>
    ///
    /// <para><b>RED against:</b> taking the direction off an unseeded <c>Random</c> rather than off the
    /// filed window — <i>"the pump was a credit dearer when he asked and a credit cheaper when he
    /// paid"</i> — and against dropping the <c>Math.Max</c> floor in
    /// <see cref="FuelMarket.PricePerPulse(double, int)"/>, <i>"the pump was giving reaction mass away"</i>.
    /// </para>
    /// </summary>
    [Fact]
    public void TheRepricedPumpQuotesTheSamePriceEveryTime()
    {
        CircularOrbitEphemeris sky = Sol();
        (string ground, string port) = GroundsWithAChoiceOfBerths(sky)[0];
        var filed = new QuietHands.Hand(ground, 11, BerthGiven: false);

        int move = 0;
        WithHands([filed], () =>
        {
            move = QuietHands.PulsePriceMoveAt(sky, port);
            Assert.Equal(move, QuietHands.PulsePriceMoveAt(sky, port));
            Assert.Equal(move, QuietHands.PulsePriceMoveAt(Sol(), port));
        });

        // …and re-installing an EQUAL register (a reload) answers the same.
        WithHands([new QuietHands.Hand(filed.BodyId, filed.Window, filed.BerthGiven)], () =>
            Assert.Equal(move, QuietHands.PulsePriceMoveAt(sky, port)));

        // The receipt: the belt price with the move on it, floored so a fill is never a gift.
        double inner = FuelMarket.OuterMarkupThresholdMeters / 2;
        double outer = FuelMarket.OuterMarkupThresholdMeters * 2;
        Assert.Equal(FuelMarket.InnerPricePerPulse, FuelMarket.PricePerPulse(inner, 0));
        Assert.Equal(FuelMarket.InnerPricePerPulse + 1, FuelMarket.PricePerPulse(inner, +1));
        Assert.Equal(FuelMarket.OuterPricePerPulse - 1, FuelMarket.PricePerPulse(outer, -1));
        Assert.Equal(FuelMarket.MinimumPricePerPulse, FuelMarket.PricePerPulse(inner, -1000));
        Assert.True(FuelMarket.PricePerPulse(inner, -1000) > 0,
            "a non-positive price is read as free by QuoteFill");
    }

    // ══ BOTH DELIVERIES, ACROSS A SAVE ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// BOTH DELIVERIES SURVIVE A VAULT ROUND-TRIP — the berth, the window, and the fact that the berth has
    /// already been handed over.
    ///
    /// <para>The spent flag is the load-bearing half: a reassignment that came back every time the captain
    /// closed the tab would be the one farmable shape this whole channel is written to avoid, and nothing on
    /// screen would ever say so.</para>
    ///
    /// <para><b>RED against:</b> <c>[JsonIgnore]</c> on <see cref="ProgressSection.HallsHandled"/>, so the
    /// register never reaches the file — <i>"the reload owed him a different berth again, and the pump was
    /// back to its old price"</i>.</para>
    /// </summary>
    [Fact]
    public void BothDeliveriesSurviveAVaultRoundTrip()
    {
        CircularOrbitEphemeris sky = Sol();
        (string ground, string port) = GroundsWithAChoiceOfBerths(sky)[0];
        int berths = DockRoster.BerthsAt(sky, port);

        var live = new QuietHands.Hand(ground, 23, BerthGiven: false);
        int movedPrice = 0;
        int movedBerth = 0;
        WithHands([live], () =>
        {
            movedPrice = QuietHands.PulsePriceMoveAt(sky, port);
            movedBerth = DockRoster.BerthGiven(port, berths, QuietHands.BerthOwedAt(sky, port));
        });

        var vault = new Vault
        {
            Version = Vault.CurrentVersion,
            SavedSimTime = 4242.0,
            Progress = new ProgressSection
            {
                HallsOpened = [new HallOpeningRecord(ground, 0)],
                HallsHandled = [new QuietHandRecord(live.BodyId, live.Window, live.BerthGiven)],
            },
        };

        Vault loaded = VaultSerializer.Load(VaultSerializer.Save(vault));
        Assert.NotNull(loaded.Progress!.HallsHandled);
        IReadOnlyList<QuietHandRecord> rows = loaded.Progress.HallsHandled!;
        Assert.Equal([new QuietHandRecord(live.BodyId, live.Window, live.BerthGiven)], rows);

        WithHands([.. rows.Select(r => new QuietHands.Hand(r.BodyId, r.Window, r.BerthGiven))], () =>
        {
            Assert.Equal(movedPrice, QuietHands.PulsePriceMoveAt(sky, port));
            Assert.Equal(movedBerth, DockRoster.BerthGiven(port, berths, QuietHands.BerthOwedAt(sky, port)));
        });

        // …and once it has been handed over, the reload keeps it handed over.
        var spent = new Vault
        {
            Version = Vault.CurrentVersion,
            SavedSimTime = 4242.0,
            Progress = new ProgressSection
            {
                HallsHandled = [new QuietHandRecord(live.BodyId, live.Window, BerthGiven: true)],
            },
        };
        Vault reloaded = VaultSerializer.Load(VaultSerializer.Save(spent));
        WithHands(
            [.. reloaded.Progress!.HallsHandled!.Select(r => new QuietHands.Hand(r.BodyId, r.Window, r.BerthGiven))],
            () =>
            {
                Assert.Null(QuietHands.BerthOwedAt(sky, port));
                Assert.Equal(
                    DockRoster.OrdinaryBerth(port, berths),
                    DockRoster.BerthGiven(port, berths, QuietHands.BerthOwedAt(sky, port)));

                // The price does NOT revert with the berth: a price that walked back the moment it had been
                // paid once would be a price watching the captain's wallet.
                Assert.Equal(movedPrice, QuietHands.PulsePriceMoveAt(sky, port));
            });
    }

    /// <summary>
    /// AN HONEST VAULT IS UNCHANGED BY THIS FEATURE EXISTING. The field is written only once something has
    /// been filed — the #1057/#1072/#1066/#677/#1063 law — because the checksum is taken over the payload
    /// and an eager empty list would change the digest of every vault ever written and hang the 📛 tampered
    /// marker on honest voyages.
    ///
    /// <para><b>RED against:</b> dropping the <c>JsonIgnoreCondition.WhenWritingNull</c> on
    /// <see cref="ProgressSection.HallsHandled"/> — <i>"hallsHandled: null appeared in every save"</i>.</para>
    /// </summary>
    [Fact]
    public void AVaultWithNothingFiledSaysNothingAboutIt()
    {
        var plain = new Vault
        {
            Version = Vault.CurrentVersion,
            SavedSimTime = 1.0,
            Progress = new ProgressSection { HallsOpened = [new HallOpeningRecord("phobos", 2)] },
        };

        string json = VaultSerializer.Save(plain);
        Assert.DoesNotContain("hallsHandled", json, StringComparison.OrdinalIgnoreCase);
        Assert.Null(VaultSerializer.Load(json).Progress!.HallsHandled);
    }

    // ══ #672's LAWS, SWEPT ══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// NEITHER DELIVERY PUBLISHES ONE STRING — no label, no line, no title, no caption, not one word. That
    /// is #672's <i>"no dialog explaining"</i> in its strongest available form, and it settles §8's reserved
    /// word for free: <b>a type with no strings in it cannot contain the reserved word.</b> A berth
    /// reassigned and a price moved are told entirely by being different from what they were.
    ///
    /// <para>Coverage floor included, exactly as <c>TheDisclosureClockTests</c> and
    /// <c>TheWorldDeclinesPolitelyTests</c> do it, so a rename that emptied a type could not turn this green
    /// by having nothing left to check.</para>
    ///
    /// <para><b>RED against:</b> adding <c>public const string Notice = "◷ BERTH REASSIGNED";</c> to
    /// <see cref="QuietHands"/> or to <see cref="DockRoster"/>.</para>
    ///
    /// <para><b>And there is no carve-out in it.</b> The one thing either type could honestly have published
    /// as a string is WHICH port serves a ground; <see cref="QuietHands.PortFor"/> hands back the body
    /// instead, because a law with one exception written into its own guard is a law nobody has to keep.</para>
    /// </summary>
    [Fact]
    public void NeitherDeliveryPublishesAnyProseAtAll()
    {
        var offenders = new List<string>();
        int surface = 0;

        const BindingFlags Public = BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance
            | BindingFlags.DeclaredOnly;

        foreach (Type channel in new[] { typeof(QuietHands), typeof(DockRoster) })
        {
            foreach (FieldInfo f in channel.GetFields(Public))
            {
                surface++;
                if (f.FieldType == typeof(string))
                {
                    offenders.Add($"{channel.Name}.{f.Name} (field)");
                }
            }
            foreach (PropertyInfo p in channel.GetProperties(Public))
            {
                surface++;
                if (p.PropertyType == typeof(string))
                {
                    offenders.Add($"{channel.Name}.{p.Name} (property)");
                }
            }
            foreach (MethodInfo m in channel.GetMethods(Public))
            {
                if (m.DeclaringType != channel)
                {
                    continue;
                }
                surface++;
                if (m.ReturnType == typeof(string) && !m.IsSpecialName)
                {
                    offenders.Add($"{channel.Name}.{m.Name} (method)");
                }
            }
        }

        Assert.True(surface >= 14, $"the two types' public surface is only {surface} member(s) — nothing swept.");
        Assert.True(offenders.Count == 0,
            "a reassigned berth and a repriced pump are never explained, so neither type publishes prose. "
            + "Found: " + string.Join(", ", offenders));
    }
}
