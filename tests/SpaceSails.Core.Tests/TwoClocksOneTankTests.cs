using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #740 · TWO CLOCKS, ONE TANK — the DEAD AIR card and the gauge on the suit are quoting the same tank in
/// the same second, so they say the same characters.
///
/// <para>Owner's smoke run, <c>/map?secretlab=deep&amp;land=1&amp;floor=11</c>: the card said <i>"your tank
/// is now the clock. You have <b>21 min 01 s</b>"</i> and the HUD, seconds later on the same floor, read
/// <b>AIR 8h09</b>. Neither number was invented — the suit genuinely runs on two clocks, the play budget
/// (<see cref="SuitAir.TankSeconds"/>) and the fiction the captain is told about
/// (<see cref="SuitAir.PrimaryHours"/>) — and the card was the one surface in the game that had never gone
/// through <see cref="SuitAir.SuitClock"/>. It printed the designer's stopwatch at a captain who was being
/// asked to spend it.</para>
///
/// <para>That is the drawn-shape-vs-sim class in prose form, and #612's own law is the one it breaks: the
/// instruments may never disagree about air. So this recomputes BOTH figures from the same
/// <see cref="SuitAir"/> facts and requires them to be the same string — and requires the card's sentence to
/// name the instrument it is quoting, because a figure that owns its source cannot be silently re-derived.
/// </para>
///
/// <para><b>Proven RED</b> against today's copy — restoring <c>$"{(int)(airSeconds / 60)} min
/// {(int)(airSeconds % 60):00} s"</c> and <i>"You have {tank}."</i> fails every case in the sweep, on the
/// agreement assertion and on the old-units assertion both.</para>
/// </summary>
public sealed class TwoClocksOneTankTests
{
    /// <summary>The floors the card can be raised on, and a body for each — a dead floor in the ordinary
    /// building, one deep in it, and the site with a band nobody dug, so the sweep is not a single screenshot
    /// wearing five hats.</summary>
    private static IEnumerable<(string Body, int Level)> DeadFloors()
    {
        foreach (string body in new[] { "miranda", "luna", "titan", UndergroundComplex.FoundBandCheatSiteId })
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                if (!UndergroundComplex.HoldsPressure(body, level))
                {
                    yield return (body, level);
                }
            }
        }
    }

    /// <summary>Tanks across every band the readout has: brimful, working, low, the last of the primary, the
    /// secondary pack, and empty. A guard handed one air value would pass on a card that printed a constant.
    /// </summary>
    private static readonly double[] Tanks =
        [SuitAir.FullSeconds, SuitAir.TankSeconds, 754.0, 420.0, 96.0, 40.0, 0.0];

    /// <summary>The shape of the old sentence's figure: a raw count of play-seconds dressed as wall time.
    /// Nothing on a suit reads in this shape, which is the whole complaint.</summary>
    private static readonly Regex PlayBudgetMinutes = new(@"\d+\s*min\s*\d\d\s*s", RegexOptions.Compiled);

    [Fact]
    public void TheCardAndTheGaugeQuoteTheSameTank()
    {
        var bad = new List<string>();
        var quoted = new HashSet<string>(StringComparer.Ordinal);

        foreach ((string body, int level) in DeadFloors())
        {
            foreach (double air in Tanks)
            {
                string card = UndergroundComplex.VacuumCard(body, level, air);

                // What the HUD will be reading in the same second, off the same facts. The distance home is
                // the card's own situation — a captain who has just stepped out of the lift — and it only
                // decides which SENTENCE the gauge picks, never which clock it prints.
                string hud = SuitAir.Readout(air, distanceHomeDu: 0, SuitAir.Supply.Tanks);

                if (air <= 0)
                {
                    // An empty tank is the one case with no figure to agree about. The card must still not
                    // invent one, and must say the word the gauge says.
                    if (PlayBudgetMinutes.IsMatch(card))
                    {
                        bad.Add($"  {body} B{-level} @ empty: the card quotes a figure at a captain with no air.");
                    }
                    if (!card.Contains("empty", StringComparison.OrdinalIgnoreCase))
                    {
                        bad.Add($"  {body} B{-level} @ empty: the gauge says EMPTY and the card does not.");
                    }
                    continue;
                }

                string clock = SuitAir.Clock(air);
                quoted.Add(clock);

                // (b) THE TWO NUMBERS AGREE — the same characters, not merely the same quantity rounded two
                // ways, because a captain compares glyphs and not quantities.
                if (!card.Contains(clock, StringComparison.Ordinal))
                {
                    bad.Add($"  {body} B{-level} @ {air:F0}s: the gauge reads \"{clock}\" and the card does not say it.");
                }
                if (!hud.Contains(clock, StringComparison.Ordinal))
                {
                    bad.Add($"  {body} B{-level} @ {air:F0}s: the HUD line \"{hud}\" does not carry its own clock.");
                }

                // …and no SECOND figure beside it. Agreement is worthless if the card also prints the old
                // number underneath — two clocks on one card is the bug with a witness attached.
                if (PlayBudgetMinutes.IsMatch(card))
                {
                    Match m = PlayBudgetMinutes.Match(card);
                    bad.Add($"  {body} B{-level} @ {air:F0}s: the card still quotes the play budget (\"{m.Value}\").");
                }

                // (a) THE SENTENCE OWNS IT — it says which instrument the figure came off, so the next
                // person to edit this copy cannot quietly re-derive the number from something else.
                if (!card.Contains($"gauge reads {clock}", StringComparison.Ordinal))
                {
                    bad.Add($"  {body} B{-level} @ {air:F0}s: the card prints \"{clock}\" without naming the gauge.");
                }
            }
        }

        // THE WORLD CAN TELL PASS FROM FAIL. If every tank in the sweep read the same, a card printing one
        // frozen string would sail through everything above.
        Assert.True(quoted.Count >= 4,
            $"the sweep only ever asked for {quoted.Count} distinct clock(s) — it cannot tell a live figure " +
            "from a constant.");
        Assert.NotEmpty(DeadFloors());

        Report(bad, "dead floors whose card and whose gauge disagree about the tank");
    }

    [Fact]
    public void TheCardSpeaksInTheUNITSTheSuitReadsIn()
    {
        // The units are the whole of it. Eight hours is what the fiction says a suit like this holds (#573,
        // off the NASA EMU), and twenty minutes is what the game spends against its compressed surface
        // clock. The card is read by a captain, so it reads in the captain's units — and on the secondary
        // pack it says RESERVE, exactly as the gauge does, because those are different promises.
        string full = UndergroundComplex.VacuumCard("miranda", -2, SuitAir.TankSeconds);
        Assert.Contains($"{(int)SuitAir.PrimaryHours}h", full, StringComparison.Ordinal);
        Assert.False(PlayBudgetMinutes.IsMatch(full),
            "a full tank is eight hours on the gauge; the card is still quoting the play budget.");

        string reserve = UndergroundComplex.VacuumCard("miranda", -2, SuitAir.ReserveSeconds * 0.5);
        Assert.True(SuitAir.OnTheReserve(SuitAir.ReserveSeconds * 0.5),
            "the reserve sample is not actually on the reserve — this case would prove nothing.");
        Assert.Contains("RESERVE", reserve, StringComparison.Ordinal);
        Assert.Contains(SuitAir.Clock(SuitAir.ReserveSeconds * 0.5), reserve, StringComparison.Ordinal);
    }

    [Fact]
    public void ThereIsONEPlaceThatTurnsAirIntoAClock()
    {
        // The fix that lasts is not the card's new sentence, it is that the sentence had to ask somebody
        // else for the number. SuitAir.Clock is that somebody, and the gauge's own branches go through it
        // too — so the card and the HUD cannot drift apart again without both moving at once.
        foreach (double air in Tanks.Where(a => a > 0))
        {
            string clock = SuitAir.Clock(air);
            Assert.Contains(clock, SuitAir.Readout(air, 0, SuitAir.Supply.Tanks), StringComparison.Ordinal);
            Assert.Contains(clock, SuitAir.Readout(air, 0, SuitAir.Supply.Room), StringComparison.Ordinal);
            Assert.Contains(clock, SuitAir.Readout(air, 0, SuitAir.Supply.Ship), StringComparison.Ordinal);
            Assert.Contains(clock, UndergroundComplex.VacuumCard("miranda", -2, air), StringComparison.Ordinal);
        }
    }

    private static void Report(List<string> bad, string what)
    {
        if (bad.Count == 0)
        {
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"{bad.Count} {what}:");
        foreach (string line in bad.Take(40))
        {
            sb.AppendLine(line);
        }
        Assert.Fail(sb.ToString());
    }
}
