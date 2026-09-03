using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #928 · THE TENTH IS QUOTED, AND IT IS ONLY THE AUTOPILOT'S.
///
/// <para>The owner, playing 2026-08-17: <i>"Now the autopilot often refuses when it calculates the cost
/// to be too big. If we divide the cost by 10 that we calculate, it should fix it nicely."</i> The rule
/// that came out of it charges an autopilot-flown approach one tenth — the rehearsal's estimate AND the
/// burns the live loop fires. Core proves the arithmetic (<c>TheAutopilotFliesAtATenthTests</c>). These
/// are the two claims that can only be made about the CLIENT, and both are asked of the source, because
/// a number is quoted or spent at a particular line of a particular file.</para>
///
/// <list type="number">
/// <item><b>THE NUMBERS ON SCREEN ARE THE CHARGED ONES.</b> The refusal line ("autopilot declines X:
/// needs ≈N p … It won't strand you"), the arm line ("budgeted ≈N p at the autopilot's tenth") and the
/// flight-plan panel all quote <c>PulsesCharged</c>; the raw Δv count is never interpolated into a
/// sentence. A refusal that quotes a number the flight will not spend is the #928 bug wearing the
/// fix's clothes. RED by quoting <c>r.Pulses</c> in the refusal.</item>
/// <item><b>EVERY AUTOPILOT BURN CHARGES THROUGH THE ACCUMULATOR, AND NOTHING ELSE DOES.</b> The three
/// autopilot burn sites (the #146 transfer impulse, the approach burn, the insertion) take what they
/// subtract from <c>AutopilotRehearsal.ChargeForBurn</c>; the two hand-spent sites in the same file (the
/// captain's ⏎ orbital insertion, and a station-keeping trim, which is quoted from Lab 25 and paid in
/// full) do not. RED by charging a hand-pressed insertion at the tenth.</item>
/// <item><b>A MANUAL PULSE COSTS ONE.</b> The captain's own reflex burn is still <c>_reactionMassPulses--</c>,
/// and no file that spends the tank by hand — the pulse key, the plotted burns, the skim and the sling,
/// the docking match, the long-haul departure, weapons, the charge board — so much as NAMES the economy.
/// The tenth is the autopilot's, not the tank's. RED by applying the factor to the hand pulse.</item>
/// </list>
///
/// <para>Every one of the three was proven able to fail on this branch; the verbatim output is in the
/// PR body for #928.</para>
/// </summary>
public sealed class TheTenthIsQuotedAndOnlyTheAutopilotsTests
{
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "SpaceSails.slnx")))
        {
            dir = dir.Parent;
        }
        return dir?.FullName ?? throw new InvalidOperationException("no repo root above the test binary.");
    }

    private static string PageSource(string file)
    {
        string path = Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages", file);
        Assert.True(File.Exists(path), $"the sweep names a file that is not there: {path}");
        return MapMarkup.Read(path);
    }

    // ---- 1. the numbers on screen are the charged ones ---------------------------------------------

    [Fact]
    public void TheRefusalTheArmLineAndThePanelAllQuoteTheChargedNumber()
    {
        string autopilot = PageSource("Map.Autopilot.cs");

        // The refusal the owner read off the screen, now in charged pulses.
        Assert.Contains(
            "$\"needs ≈{r.PulsesCharged} p (incl. insertion), tank has {_reactionMassPulses} and keeps {reserve} in reserve\"",
            autopilot, StringComparison.Ordinal);
        Assert.Contains("autopilot declines {name}: {why}. It won't strand you.", autopilot, StringComparison.Ordinal);
        // …and the verdict it is taken on is the charged one too, not the raw Δv count.
        Assert.Contains("r.BudgetExceeded || r.PulsesCharged > budget", autopilot, StringComparison.Ordinal);

        // The arm line and the panel both read _armedBudgetPulses, which is the charged quote.
        Assert.Contains("_armedBudgetPulses = r.PulsesCharged;", autopilot, StringComparison.Ordinal);
        Assert.Contains("budgeted ≈{_armedBudgetPulses} p at the autopilot's tenth", autopilot, StringComparison.Ordinal);
        Assert.Contains("· budgeted ≈@(_armedBudgetPulses) p", PageSource("Map.razor"), StringComparison.Ordinal);

        // And the economy is SAID once, where the budget is quoted (#928's visible line).
        Assert.Contains("the autopilot flies at a tenth of the tank a hand would spend",
            PageSource("Map.razor"), StringComparison.Ordinal);

        // The raw count is never interpolated into a sentence anywhere on the page: `{r.Pulses}` and
        // `@(r.Pulses)` are the two spellings that would put it on screen.
        Assert.DoesNotContain("{r.Pulses}", autopilot, StringComparison.Ordinal);
        Assert.DoesNotContain("{r.Pulses ", autopilot, StringComparison.Ordinal);
    }

    // ---- 2. every autopilot burn charges through the accumulator, and nothing else does -------------

    /// <summary>What each <c>_reactionMassPulses -= …</c> in the autopilot file subtracts, and whether
    /// that quantity is allowed to be an un-economized (raw) one.</summary>
    private static readonly (string Expression, bool ChargedAtTheTenth, string Why)[] TankDebits =
    [
        ("charge", true, "#146 transfer impulse — an autopilot burn"),
        ("approachCharge", true, "the approach burn — an autopilot burn"),
        ("insertCharge", true, "the insertion — an autopilot burn"),
        ("cost", false, "a station-keeping trim: quoted from Lab 25 and paid in full, never economized"),
        ("oi.Cost", false, "the captain's own ⏎ orbital insertion: a hand pulse costs what it costs"),
    ];

    [Fact]
    public void TheThreeAutopilotBurnsChargeTheTenth_AndTheTwoHandSpentSitesDoNot()
    {
        string autopilot = PageSource("Map.Autopilot.cs");

        // Exactly the five debits, in order, spelled the way the ledger above says.
        List<string> debits = Regex.Matches(autopilot, @"_reactionMassPulses -= ([^;]+);")
            .Select(m => m.Groups[1].Value.Trim()).ToList();
        Assert.Equal(TankDebits.Select(d => d.Expression), debits);

        // Every economized debit is a ChargeForBurn result, and there are exactly three of them —
        // one per autopilot burn site, each accumulating against the raw ledger.
        List<string> charged = Regex.Matches(autopilot, @"int (\w+) = AutopilotRehearsal\.ChargeForBurn\(_armedSpentPulses, \w+\)")
            .Select(m => m.Groups[1].Value).ToList();
        Assert.Equal(
            TankDebits.Where(d => d.ChargedAtTheTenth).Select(d => d.Expression).OrderBy(x => x, StringComparer.Ordinal),
            charged.OrderBy(x => x, StringComparer.Ordinal));

        // The reserve floor and the can-I-afford-it gate are read in the SAME currency the tank is
        // debited in — a floor checked against a raw cost would stand the autopilot down on a bill it
        // is never sent.
        Assert.Contains("if (_reactionMassPulses - charge < reserveFloor)", autopilot, StringComparison.Ordinal);
        Assert.Contains("if (_reactionMassPulses - approachCharge < reserveFloor)", autopilot, StringComparison.Ordinal);
        Assert.Contains("if (insertCharge > _reactionMassPulses)", autopilot, StringComparison.Ordinal);

        // And the burns REPORT what they took, not the raw Δv price.
        Assert.Contains("({charge} p) 🛰", autopilot, StringComparison.Ordinal);
        Assert.Contains("({approachCharge} p) 🛰", autopilot, StringComparison.Ordinal);
    }

    // ---- 3. a manual pulse costs one ---------------------------------------------------------------

    /// <summary>Every client file that spends the tank BY HAND. None of them may name the autopilot's
    /// economy: the tenth is the price of a flown approach, not a discount on the tank.</summary>
    private static readonly string[] HandSpentFiles =
    [
        "Map.Sim.Keys.cs",          // the captain's reflex + / − pulse
        "Map.Plot.Nodes.cs",        // plotted burns, fired at their epoch
        "Map.Plot.Skim.cs",         // the aerobrake plan's pulses
        "Map.Plot.Sling.cs",        // the slingshot plan's pulses
        "Map.Docking.cs",           // the terminal match
        "Map.LongHaul.cs",          // a long-haul departure / mid-course
        "Map.Combat.FireControl.cs",// weapons
        "Map.ChargeBoard.cs",       // the contactor's draw
    ];

    [Fact]
    public void AHandPulseCostsOne_AndNoHandSpentFileNamesTheEconomy()
    {
        // The captain's own pulse: one press, one pulse, unchanged since the drive was written.
        Assert.Contains("_reactionMassPulses--;", PageSource("Map.Sim.Keys.cs"), StringComparison.Ordinal);

        string[] economyWords = ["EconomyFactor", "ChargeForBurn", "PulsesCharged", "AutopilotRehearsal.Charged("];
        foreach (string file in HandSpentFiles)
        {
            string src = PageSource(file);
            foreach (string word in economyWords)
            {
                Assert.False(src.Contains(word, StringComparison.Ordinal),
                    $"{file} names '{word}': the #928 tenth is the AUTOPILOT's economy — a pulse the captain " +
                    $"spends by hand costs exactly what it always cost.");
            }
        }
    }
}
