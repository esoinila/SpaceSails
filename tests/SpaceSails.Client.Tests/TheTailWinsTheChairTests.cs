using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;
using static SpaceSails.Client.Tests.CastawayBench;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1277 · <b>THE TAIL WINS THE CHAIR.</b> Two systems wanted the same man out of the same chair, and the
/// one that got there first was the one with nothing to say.
///
/// <para><b>What shipped</b> (named in #1276's bench, not fixed there): on a watch whose schedule happened to
/// name GILT-EYE, #731's hours walked him out through a cellar leaf before last call, <c>_barLeft</c> had
/// him, and #1199's tail arrived after last call to find no chair to start a route from. The whole two-leg
/// night simply did not happen that evening, silently — which means the beat was unreachable on some
/// evenings and a tester at the documented link saw an ordinary bar.</para>
///
/// <para><b>The ruling (Fable):</b> the tail wins. On a watch where the walk is dealt the hours defer to it
/// for him, and the rota's own departure of him is what the tail's first leg IS.</para>
///
/// <para>The laws below are that ruling's two halves plus its limit: the hours' roster never names the man
/// the walk has claimed, on any watch it claims him; on every watch it does NOT claim him the roster is the
/// shipped list to the byte; and a beat that has already been spent claims nobody at all, so the exemption
/// is a night's and never a name's.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheTailWinsTheChairTests
{
    private static string Berth => ObservationWalk.HavenId;

    private static string Person => TheTail.ThePersonOfInterest(Berth);

    /// <summary>How many consecutive watches the laws below are swept over. A hundred and twenty-eight
    /// four-hour shifts is three weeks of evenings, which is long enough that every arm of the rota's own
    /// seeded roll about this man — at the bar, gone, in the back — is walked many times over.</summary>
    private const int Watches = 128;

    private static HavenInterior.BarFloor TheBar =>
        HavenInterior.BarBand(Berth) ?? throw new InvalidOperationException($"{Berth} draws no bar band.");

    /// <summary>A page clamped on at the walk's own station, ashore in the bar, with the frozen docking watch
    /// wound to <paramref name="watch"/>. Nothing else is arranged: the room's hours are the room's.</summary>
    private static Pages.Map AtTheBarOnWatch(string canvasId, long watch)
    {
        Pages.Map map = Boot(canvasId);
        var sky = (ICelestialEphemeris)Read(map, "_ephemeris")!;
        CelestialBody berth = sky.Bodies.First(b => b.Id == Berth);
        Invoke(map, "ClampOntoHaven", berth, sky.Position(Berth, (double)Read(map, "SimTime")!), null);
        Assert.True((bool)Invoke(map, "StandAtTheBarThreshold")!, "the ashore boot refused this berth.");
        WindTo(map, watch, 0);
        return map;
    }

    /// <summary>Wind the page to a given watch and a given number of seconds into it. The docking watch is
    /// frozen (#709) and the hand crossing it is <c>SimTime</c>, so the two are set together or not at
    /// all.</summary>
    private static void WindTo(Pages.Map map, long watch, double secondsIn)
    {
        Set(map, "_dockVisitSimTime", watch * PatronRota.WatchSeconds);
        Set(map, "SimTime", (watch * PatronRota.WatchSeconds) + secondsIn);
    }

    /// <summary>The room's OWN departure roster for the watch the page is wound to — the page's own method,
    /// asked the way the frame asks it.</summary>
    private static IReadOnlyList<Egress.Move> TheRoster(Pages.Map map) =>
        (IReadOnlyList<Egress.Move>)Invoke(map, "TheWatchDecidesWhoGoes", TheBar)!;

    /// <summary>
    /// …AND THE ROSTER AS IT SHIPPED — <see cref="Egress.Departures"/> over every regular the rota seated,
    /// which is what the page built before this lane, spelled out here rather than read off the page.
    ///
    /// <para>A "before" taken from the page itself would move with the page, and a law stated against it
    /// would be green whatever the page did next. This is the one place in the file that restates the shipped
    /// arithmetic, and it is restated in Core's own terms.</para>
    /// </summary>
    private static IReadOnlyList<Egress.Move> TheRosterAsItShipped(long watch)
    {
        IReadOnlyList<HavenInterior.SeatedRegular> rota =
            HavenInterior.ResolveRegulars(Berth, watch * PatronRota.WatchSeconds);
        var seated = new List<Egress.Occupant>();
        for (int i = 0; i < rota.Count; i++)
        {
            if (rota[i].Present)
            {
                seated.Add(new Egress.Occupant(i, rota[i].Id));
            }
        }

        return Egress.Departures(Berth, HavenLevels.Concourse, watch, seated, TheBar.Doors);
    }

    /// <summary>Is the walk dealt on this watch? The beat's own three questions, and the third is the only one
    /// that moves: the rota has him in this room, or it does not.</summary>
    private static bool TheWalkClaimsHim(long watch) =>
        PatronRota.Resolve(Person, Berth, watch * PatronRota.WatchSeconds) == PatronState.AtBar;

    // ── (a) THE SEAM, SWEPT OVER EVERY WATCH ────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>ON EVERY WATCH THE WALK IS DEALT, THE HOURS' ROSTER DOES NOT NAME HIM — AND ON EVERY WATCH IT IS
    /// NOT, THE ROSTER IS THE SHIPPED ONE TO THE BYTE.</b>
    ///
    /// <para>The ruling's two halves, stated across three weeks of evenings rather than at one. The first is
    /// the fix: a roster that never names him is a room that cannot walk him out before last call, which is
    /// what leaves him in his chair for the tail to find. The second is its limit: the hours are #731's and
    /// are not this lane's to touch, so on every other evening the list must come back identical — same
    /// people, same moments, same leaves, same order.</para>
    ///
    /// <para><b>And the sweep proves it is asked of a world that can answer no:</b> it counts the watches on
    /// which the shipped roster DID name him and fails if there are none. A guard that selects nothing is
    /// green over a broken game (the fifth bug class), and this one would be exactly that at a berth whose
    /// rota never seated him.</para>
    ///
    /// <para><b>RED on the shipped order</b> (the roster clause reverted): on the first such watch,
    /// <c>Assert.DoesNotContain() Failure</c> — the room has him scheduled out of the chair the walk needs
    /// him in.</para>
    /// </summary>
    [Fact]
    public void TheHoursNeverScheduleTheManTheWalkHasClaimed()
    {
        Pages.Map map = AtTheBarOnWatch("the-hours-defer", 0);

        int claimed = 0, shippedTookHim = 0, freeEvenings = 0;
        for (long watch = 0; watch < Watches; watch++)
        {
            WindTo(map, watch, 0);
            IReadOnlyList<Egress.Move> shipped = TheRosterAsItShipped(watch);
            IReadOnlyList<Egress.Move> now = TheRoster(map);

            if (!TheWalkClaimsHim(watch))
            {
                // Not his night. The hours are the hours, and nothing about them moved.
                freeEvenings++;
                Assert.Equal(shipped, now);
                continue;
            }

            claimed++;
            Assert.DoesNotContain(now, m => string.Equals(m.Plate, Person, StringComparison.Ordinal));
            if (shipped.Any(m => string.Equals(m.Plate, Person, StringComparison.Ordinal)))
            {
                shippedTookHim++;
            }

            // …and NOBODY ELSE'S EVENING MOVED. Egress seeds each roll on the chair ordinal, so dropping one
            // man must not re-key the people around him — the same slip #1062 and the canteen's projection
            // both paid for, and the reason this clause can be one line in the page.
            foreach (Egress.Move was in shipped)
            {
                if (now.FirstOrDefault(m => string.Equals(m.Plate, was.Plate, StringComparison.Ordinal))
                    is { Plate.Length: > 0 } still)
                {
                    Assert.Equal(was, still);
                }
            }

            // The room does not lose a whole watch's churn over one man: at most his own move is missing.
            Assert.True(
                now.Count >= shipped.Count - 1,
                $"watch {watch}: the roster lost {shipped.Count - now.Count} departures over one man.");
        }

        Assert.True(claimed > 0, $"the walk claims nobody in {Watches} watches: this law selects nothing.");
        Assert.True(freeEvenings > 0, $"the walk claims EVERY watch: the untouched half proves nothing.");
        Assert.True(
            shippedTookHim > 0,
            $"in {Watches} watches the shipped hours never once took him: this law cannot go red.");
    }

    // ── (b) THE EVENING THAT USED TO GO SILENT, PLAYED ──────────────────────────────────────────────────

    /// <summary>The first watch on which the walk is dealt AND the shipped hours would have taken him — the
    /// evening the beat went silent, found by the sweep rather than typed in, so a rota that ever re-rolls
    /// moves the case rather than rotting it.</summary>
    private static long TheEveningBothSystemsWantedHim()
    {
        for (long watch = 0; watch < Watches; watch++)
        {
            if (TheWalkClaimsHim(watch)
                && TheRosterAsItShipped(watch)
                    .Any(m => string.Equals(m.Plate, Person, StringComparison.Ordinal)))
            {
                return watch;
            }
        }

        throw new InvalidOperationException(
            $"no watch in {Watches} has both systems wanting {Person} out of his chair.");
    }

    private static string Leg(Pages.Map map) => Read(map, "_nightLeg")!.ToString()!;

    private static IList<Pages.Map.Walker> Afoot(Pages.Map map) =>
        (IList<Pages.Map.Walker>)Read(map, "_barAfoot")!;

    private static IReadOnlySet<string> Left(Pages.Map map) =>
        (IReadOnlySet<string>)Read(map, "_barLeft")!;

    private static void Frames(Pages.Map map, double seconds, Func<bool>? until = null)
    {
        const double dt = 1.0 / 30.0;
        for (double t = 0; t < seconds; t += dt)
        {
            Set(map, "SimTime", (double)Read(map, "SimTime")! + dt);
            Invoke(map, "AdvanceBarWalkers", dt);
            if (until is not null && until())
            {
                return;
            }
        }
    }

    /// <summary>
    /// <b>HE IS IN HIS CHAIR AT LAST CALL, AND HE LEAVES BY THE TAIL'S ROUTE.</b> The ruling played on a live
    /// page, on the very evening that used to go silent: a watch whose schedule named him.
    ///
    /// <para>The room runs its hours past the moment it had him scheduled out — other regulars stand up and
    /// go, the arithmetic is untouched — and he is still sitting there: not on the room's own gone-list, not
    /// on its dealt-list, not on its feet, and still seated by the rota's own churned answer. Then last call
    /// comes and he gets up, and the legs he gets up onto are the TAIL'S: a car, not a cellar leaf.</para>
    ///
    /// <para>That is the ruling's second clause said as one fact — <i>the rota's own departure of him is what
    /// the tail's first leg IS</i>. The room still loses a man from that chair this watch; it is the walk
    /// that walks him out of it, and <c>_barLeft</c> gains him on the frame his legs start exactly as the
    /// hours would have had it.</para>
    ///
    /// <para><b>RED on the shipped order</b> (the roster clause reverted): <c>he was walked out of his chair
    /// by the hours before last call</c> — and, with that assertion removed, the night never leaves
    /// <c>NotYet</c>.</para>
    /// </summary>
    [Fact]
    public void OnTheEveningBothSystemsWantedHimTheWalkIsTheOneThatGetsHim()
    {
        long watch = TheEveningBothSystemsWantedHim();
        Pages.Map map = AtTheBarOnWatch("the-tail-wins", watch);

        // Wind past the moment the shipped hours had him scheduled out, and run the room there.
        double hisOldMoment = TheRosterAsItShipped(watch)
            .First(m => string.Equals(m.Plate, Person, StringComparison.Ordinal)).AtSecondsIntoWatch;
        WindTo(map, watch, hisOldMoment + 1);
        Frames(map, 3);

        Assert.DoesNotContain(Person, Left(map));
        Assert.DoesNotContain(Afoot(map), w => w.Who == Person);
        Assert.DoesNotContain((IEnumerable<string>)Read(map, "_barDealt")!, k => k == "out:" + Person);
        Assert.Contains(
            HavenInterior.ResolveRegulars(
                Berth, watch * PatronRota.WatchSeconds,
                (HavenInterior.RoomChurn)Read(map, "TheBarsChurn")!),
            r => r.Present && string.Equals(r.Id, Person, StringComparison.Ordinal));

        // The room's hours ran, and they ran on somebody. This is not an evening with nothing in it.
        Assert.NotEmpty((IEnumerable<string>)Read(map, "_barDealt")!);

        // Last call, and he gets up — onto the tail's first leg.
        WindTo(map, watch, PatronRota.WatchSeconds * (Egress.LastCallFraction + 0.05));
        Frames(map, 900, () => Leg(map) != "NotYet");

        Assert.Equal("ToTheCarDown", Leg(map));
        Assert.Contains(Person, Left(map));
        Assert.Contains(Afoot(map), w => w.Who == Person && w.For.ToString() == "WalkingTheRoute");

        // …and the hours never dealt him at all: he left this room once, by one road.
        Assert.DoesNotContain((IEnumerable<string>)Read(map, "_barDealt")!, k => k == "out:" + Person);
    }

    // ── (c) THE LIMIT: A SPENT BEAT CLAIMS NOBODY ───────────────────────────────────────────────────────

    /// <summary>
    /// <b>ONCE THE BEAT IS SPENT HE IS A REGULAR AGAIN, AND THE HOURS HAVE HIM BACK.</b>
    ///
    /// <para>The exemption belongs to a NIGHT and never to a name. A captain who has already followed this man
    /// onto the walk and found nobody there has had the beat; afterwards the walk is a walk and he is a man
    /// who drinks here, and a room that went on holding his chair for a beat that cannot happen again would be
    /// a permanent hole in #731's hours with one person's name on it.</para>
    ///
    /// <para>Stated on the same evening as the case above — the one where the hours DID want him — so the two
    /// answers differ in exactly one fact.</para>
    ///
    /// <para><b>RED</b> by claiming him regardless of the spend (the <c>IsSpent</c> clause dropped from
    /// <c>TheManTheWalkHasClaimed</c>): the roster comes back without him and this names it.</para>
    /// </summary>
    [Fact]
    public void ASpentBeatHandsHimBackToTheHours()
    {
        long watch = TheEveningBothSystemsWantedHim();
        Pages.Map map = AtTheBarOnWatch("the-beat-is-spent", watch);

        Assert.DoesNotContain(TheRoster(map), m => string.Equals(m.Plate, Person, StringComparison.Ordinal));

        Set(map, "_observationWalkSpentOn", ObservationWalk.Key(Berth, Person));
        Assert.Equal(TheRosterAsItShipped(watch), TheRoster(map));
        Assert.Contains(TheRoster(map), m => string.Equals(m.Plate, Person, StringComparison.Ordinal));
    }

    /// <summary>
    /// <b>AND NO OTHER ROOM IN THE GAME HOLDS A CHAIR.</b> The walk is at one station; every other bar's
    /// hours must be untouched by a lane about this one, on every watch.
    ///
    /// <para>It is the clause that stops the seam becoming a law about a NAME. GILT-EYE drinks at more than
    /// one port, and a claim that followed him around the system would quietly take a third of the turnover
    /// out of rooms that have no observation walk in them and nothing to spend it on.</para>
    /// </summary>
    [Fact]
    public void EveryOtherBarsHoursAreExactlyWhatTheyWere()
    {
        foreach (string port in HavenInterior.InteriorBodyIds)
        {
            if (string.Equals(port, Berth, StringComparison.Ordinal)
                || HavenInterior.BarBand(port) is not { } bar)
            {
                continue;
            }

            Assert.False(
                HavenInterior.HasObservationWalk(port),
                $"{port} grew an observation walk: this law needs a port that has none.");

            Pages.Map map = AtTheBarOnWatch($"no-claim-{port}", 0);
            Invoke(map, "ClampOntoHaven",
                ((ICelestialEphemeris)Read(map, "_ephemeris")!).Bodies.First(b => b.Id == port),
                ((ICelestialEphemeris)Read(map, "_ephemeris")!).Position(port, 0.0), null);

            for (long watch = 0; watch < 16; watch++)
            {
                WindTo(map, watch, 0);
                IReadOnlyList<HavenInterior.SeatedRegular> rota =
                    HavenInterior.ResolveRegulars(port, watch * PatronRota.WatchSeconds);
                var seated = new List<Egress.Occupant>();
                for (int i = 0; i < rota.Count; i++)
                {
                    if (rota[i].Present)
                    {
                        seated.Add(new Egress.Occupant(i, rota[i].Id));
                    }
                }

                Assert.Equal(
                    Egress.Departures(port, HavenLevels.Concourse, watch, seated, bar.Doors),
                    (IReadOnlyList<Egress.Move>)Invoke(map, "TheWatchDecidesWhoGoes", bar)!);
            }
        }
    }
}
