using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1108 · THE THREE WORLD BUILDERS, BUILT BY EVERY CORE AT ONCE, FIFTY TIMES OVER — and they must hand back
/// the same world every time.
///
/// <para><b>Why this guard exists.</b> Two of the generators memoise into a process-wide cache
/// (<c>MoonSurface</c>'s layout cache, <c>HavenInterior</c>'s deck cache) and both of them were plain
/// dictionaries once. Under xUnit's parallel classes that cost two afternoons: #585 was a shelter list that
/// did not match the ground, #649 was <c>TheOracleCanBeSeatedOnDemandTests</c> failing about one run in three
/// with an <c>InvalidOperationException</c> that has nothing to do with the oracle. Both were fixed by making
/// the dictionary concurrent — and NEITHER fix left a guard behind, so the next plain <c>Dictionary</c>
/// anybody adds to a generator would buy the same afternoon back at full price.</para>
///
/// <para><b>What it asserts.</b> A fingerprint of every mark the builder lays — walls with their flags,
/// consoles with their labels, room labels, doors, scenery and structures, at round-trip float precision —
/// taken once on a quiet thread, and then taken again by half the machine's cores for fifty rounds each,
/// while the OTHER half does nothing but insert grounds nobody has ever asked for. Every fingerprint must
/// equal the quiet one. That covers both halves of what a shared cache can get wrong: a corrupted dictionary
/// (which throws) and a cache that hands back somebody else's answer (which does not).</para>
///
/// <para><b>Proven able to fail.</b> With <c>MoonSurface._layoutCache</c> reverted from
/// <c>ConcurrentDictionary</c> to <c>Dictionary</c> — the #585 code, exactly — this class goes red with
/// <c>InvalidOperationException: Operations that change non-concurrent collections must have exclusive
/// access. A concurrent update was performed on this collection and corrupted its state.</c>, which is
/// word for word what #585 was reported as.</para>
///
/// <para><b>What it deliberately does NOT do</b> is install a process-wide register from a worker. The
/// registers are ambients the generators read on purpose (§13.15: thirty callers must not each learn what a
/// burial is), so a build during a foreign install is genuinely a different world and a test that raced one
/// would be asserting that an ambient is not an ambient. That race is closed on the writer's side instead —
/// see <c>StopRegisterCollection</c> and <c>TheProcessWideWritersAreSerialisedTests</c>.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
[SlowGate] // #251 · 15 s over 1 test, measured 2026-09-04; see TheSlowGateRosterTests.
public sealed class TheWorldBuildersAreThreadSafeTests
{
    /// <summary>How many times each reader rebuilds the whole set while the writers churn. Fifty is long
    /// enough that a plain dictionary is corrupted well before the end and short enough that the class costs
    /// fifteen seconds rather than a minute.</summary>
    private const int Rounds = 50;

    private static SurfaceLayout.Field Field => MoonSurface.ExpeditionField();

    /// <summary>The landable grounds — Miranda and Luna are authored geography and the rest are seeded, so a
    /// sample of one kind proves nothing about the other.</summary>
    private static readonly string[] Bodies = ["miranda", "luna", "phobos", "titan", "europa"];

    // ── THE FINGERPRINT ───────────────────────────────────────────────────────────────────────────────
    //
    // Every mark, in the order it was laid, at "R" precision, appended to one builder and folded to a 64-bit
    // FNV-1a. A hash rather than the transcript because fifty rounds × five workers × a dozen decks of
    // transcript is a lot of string to hold, and because the only question asked of it is "the same or not".

    private static string F(float v) => v.ToString("R", CultureInfo.InvariantCulture);

    private static string B(bool v) => v ? "1" : "0";

    private static string N(int v) => v.ToString(CultureInfo.InvariantCulture);

    private static ulong Fingerprint(DeckPlan deck)
    {
        var sb = new StringBuilder();
        foreach (DeckPlan.Wall w in deck.Walls)
        {
            sb.Append("w ").Append(F(w.X1)).Append(' ').Append(F(w.Y1)).Append(' ')
              .Append(F(w.X2)).Append(' ').Append(F(w.Y2)).Append(' ')
              .Append(B(w.IsWindow)).Append(B(w.IsHull)).Append(B(w.Unseen))
              .Append(B(w.IsStone)).Append(B(w.IsSeamless)).Append('\n');
        }
        foreach (DeckPlan.ConsoleSpot c in deck.Consoles)
        {
            sb.Append("c ").Append(c.Kind.ToString()).Append(' ').Append(F(c.X)).Append(' ').Append(F(c.Y)).Append(' ')
              .Append(c.Label).Append(' ').Append(c.ImageUrl ?? "-").Append(' ').Append(c.Caption ?? "-")
              .Append('\n');
        }
        foreach ((float X, float Y, string Text) l in deck.RoomLabels)
        {
            sb.Append("l ").Append(F(l.X)).Append(' ').Append(F(l.Y)).Append(' ').Append(l.Text).Append('\n');
        }
        foreach (DeckPlan.Door d in deck.Doors)
        {
            sb.Append("d ").Append(F(d.X1)).Append(' ').Append(F(d.Y1)).Append(' ')
              .Append(F(d.X2)).Append(' ').Append(F(d.Y2)).Append(' ').Append(B(d.Locked)).Append(' ')
              .Append(N(d.Interlock)).Append('\n');
        }
        foreach (SurfaceScenery.Mark m in deck.Scenery)
        {
            sb.Append("s ").Append(m.Of.ToString()).Append(' ').Append(F((float)m.X1)).Append(' ')
              .Append(F((float)m.Y1)).Append(' ').Append(F((float)m.X2)).Append(' ')
              .Append(F((float)m.Y2)).Append('\n');
        }
        sb.Append("n ").Append(N(deck.Structures.Length)).Append(' ')
          .Append(N(deck.Furniture.Length)).Append(' ')
          .Append(F((float)deck.SpawnX)).Append(' ').Append(F((float)deck.SpawnY)).Append('\n');

        ulong h = 14695981039346656037UL;
        string text = sb.ToString();
        foreach (char ch in text)
        {
            h = (h ^ ch) * 1099511628211UL;
        }
        return h;
    }

    /// <summary>One pass over every builder this suite hammers, fingerprinted in a fixed order: the surface
    /// deck (with the memoised layout behind it), the underground floors, and the docked haven decks (with
    /// the other memo behind them).</summary>
    private static List<ulong> BuildTheWorld()
    {
        var marks = new List<ulong>();

        foreach (string body in Bodies)
        {
            foreach (LandingSite site in LandingSites.For(body))
            {
                marks.Add(Fingerprint(MoonSurface.SurfaceDeck(
                    body, body, [], 0, static (_, _) => { },
                    siteSalt: site.LayoutSalt, siteName: site.Name, hasSecretSite: true)));
            }

            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                marks.Add(Fingerprint(
                    HiveInterior.FloorDeck(body, level, Field, 0, static (_, _) => { }, [])));
            }
        }

        foreach (string havenId in HavenInterior.InteriorBodyIds)
        {
            DeckPlan haven = HavenInterior.DockedDeck(havenId)
                ?? throw new InvalidOperationException($"{havenId} is listed as having a deck and has none.");
            marks.Add(Fingerprint(haven));
        }

        return marks;
    }

    /// <summary>
    /// #1108 · THE CHURN — a ground nobody has ever built, over and over, for as long as the comparison
    /// runs. Every call is a cache MISS and therefore an <b>insert</b>, and because
    /// <c>MoonSurface</c>'s memo is capped at 64 entries the cap's <c>Clear()</c> fires every sixty-odd
    /// iterations, with every other worker reading the same dictionary at the time.
    ///
    /// <para><b>Without this the guard proves nothing, and it was written without it twice.</b> The quiet
    /// baseline warms every key the comparison uses, so the concurrent phase was pure <c>TryGetValue</c> —
    /// and a plain <c>Dictionary</c> under nothing but readers is perfectly safe. Reverting
    /// <c>_layoutCache</c> to the #585 code left this class GREEN. One churn build per round did not fix it
    /// either: 600 inserts spread over eleven seconds is one write every eighteen milliseconds, and threads
    /// simply do not meet that rarely. It took a DEDICATED writer per core, looping as fast as it can, to
    /// make the window the bug actually lives in. That is the "a green test that asserts nothing" bug class
    /// twice over: the assertion was right both times and the world could not tell pass from fail.</para>
    ///
    /// <para>The decks it builds are thrown away; only the writes they cause matter. The haven memo is
    /// churned once per comparison round rather than in this loop, because it has no cap — a tight loop of
    /// distinct watches would grow it without bound, which is a real property of that cache and not
    /// something a guard should discover by exhausting the runner's memory.</para></summary>
    private static void ChurnUntil(CancellationToken stop, int worker)
    {
        for (long n = 0; !stop.IsCancellationRequested; n++)
        {
            MoonSurface.SurfaceDeck(
                "luna", "luna", [], 0, static (_, _) => { },
                siteSalt: $"churn-{worker}-{n}", siteName: "churn", hasSecretSite: true);
        }
    }

    /// <summary>
    /// #1108 · THE SAME WORLD, WHOEVER IS ASKING AND HOWEVER MANY OF THEM ARE ASKING AT ONCE.
    /// </summary>
    [Fact]
    public async Task EveryBuilderHandsBackTheSameWorldUnderEveryCoreAtOnce()
    {
        List<ulong> quiet = BuildTheWorld();
        Assert.True(quiet.Count > 20,
            $"only {quiet.Count} deck(s) were built — this proves little about a shared cache.");

        int workers = Math.Max(2, Environment.ProcessorCount / 2);
        var wrong = new System.Collections.Concurrent.ConcurrentBag<string>();

        using var stop = new CancellationTokenSource();
        Task[] churners = [.. Enumerable.Range(0, workers)
            .Select(w => Task.Run(() => ChurnUntil(stop.Token, w), CancellationToken.None))];

        Task[] readers = [.. Enumerable.Range(0, workers).Select(worker => Task.Run(() =>
        {
            for (int round = 0; round < Rounds; round++)
            {
                // …and the other memo, keyed on the docking watch among other things: a watch nobody has
                // docked at is a key nobody has built, so this round inserts into it too.
                HavenInterior.DockedDeck(
                    HavenInterior.InteriorBodyIds[0],
                    simTime: 100_000.0 * ((worker * Rounds) + round + 1));

                List<ulong> mine = BuildTheWorld();
                if (mine.Count != quiet.Count)
                {
                    wrong.Add($"worker {worker} round {round}: built {mine.Count} decks, not {quiet.Count}");
                    return;
                }
                for (int i = 0; i < mine.Count; i++)
                {
                    if (mine[i] != quiet[i])
                    {
                        wrong.Add($"worker {worker} round {round}: deck #{i} fingerprints " +
                                  $"{mine[i]:x16}, not {quiet[i]:x16}");
                        return;
                    }
                }
            }
        }))];

        await Task.WhenAll(readers);
        await stop.CancelAsync();
        // Awaited, not abandoned: a churner that died on a corrupted dictionary must be the thing this
        // test reports, and an unobserved faulted task reports nothing at all.
        await Task.WhenAll(churners);

        Assert.True(wrong.IsEmpty,
            $"{wrong.Count} worker(s) were handed a different world by a shared generator:\n  " +
            string.Join("\n  ", wrong.Take(10)));
    }
}
