using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// MONEY TAKEN FOR A REASON NOBODY CAN READ IS NOT A MYSTERY, IT IS A BUG (#938 D4 · #553).
///
/// <para><b>What was wrong.</b> <see cref="ComplianceSurcharge"/> shaves <see cref="ComplianceSurcharge.Rate"/>
/// off both halves of a filed wreck report — the finder's fee and the correct-cause bonus
/// (<c>Derelict.cs</c>). It has shipped that way since #553. Its five explanatory members —
/// <c>Clause</c>, <c>LineItem</c>, <c>WhenAsked</c>, <c>AskAbout</c>, <c>WhatItIsLine</c> — had ZERO
/// consumers anywhere in <c>src</c>, and <c>git grep "14(b)" -- src/SpaceSails.Client</c> was empty. So
/// the captain who did it properly was paid four per cent less than the arithmetic on the button, with
/// no line item, no clause number and no sentence anywhere in the game.</para>
///
/// <para><b>Why that is not "referenced but not described".</b> #553's design is that the incident is
/// never explained — <i>"Love that referenced but not described"</i> — and a reference nobody can see is
/// not a reference. Undescribed is the point; invisible is the failure. The delivery mechanism was
/// written and never wired.</para>
///
/// <para><b>The law.</b> The surface that shows the payment shows the deduction, and the deduction is
/// shown with its clause. Three facts hold it: the receipt really is the surface (the wreck-outcome card
/// in <c>Map.razor</c> cites <c>ComplianceSurcharge.LineItem</c> in the same block that prints
/// <c>CreditsNow</c>), the line item really carries the clause number, and the number on it really is
/// the money that went missing — checked against <c>Rate</c> alone, so a drifting deduction and a
/// drifting label cannot agree with each other.</para>
/// </summary>
public class TheDeductionReadsWithItsClauseTests
{
    /// <summary>
    /// THE PREMISE. A filed report really does lose money to the clause — if it ever stops, the law below
    /// is about a line item that never appears and proves nothing.
    /// </summary>
    [Fact]
    public void ThePremise_FilingReallyPaysTheSurchargeAndStrippingNeverDoes()
    {
        Assert.True(ComplianceSurcharge.Rate > 0, "the surcharge rate is zero — nothing is being deducted");

        var filedWithNothingTaken = new List<string>();
        var strippedButCharged = new List<string>();

        foreach (Derelict.Wreck wreck in SomeHulls())
        {
            Derelict.SalvageOutcome filed =
                Derelict.Resolve(wreck, Derelict.SalvageChoice.FileTheReport, wreck.Cause);
            Derelict.SalvageOutcome stripped =
                Derelict.Resolve(wreck, Derelict.SalvageChoice.StripAndSayNothing, null);

            if (filed.SurchargeCr <= 0)
            {
                filedWithNothingTaken.Add($"  {wreck.Id} ({wreck.ShipName}, {wreck.AssessedValueCr:N0} cr assessed)");
            }
            if (stripped.SurchargeCr != 0)
            {
                strippedButCharged.Add($"  {wreck.Id} — {stripped.SurchargeCr} cr taken off a road that files nothing");
            }
        }

        Assert.True(filedWithNothingTaken.Count == 0,
                    "filing these hulls costs nothing at all, so there is no deduction to read:\n"
                    + string.Join("\n", filedWithNothingTaken));
        Assert.True(strippedButCharged.Count == 0,
                    "the quiet road was charged a compliance surcharge — #553's whole joke is that it is not:\n"
                    + string.Join("\n", strippedButCharged));
    }

    /// <summary>
    /// THE LAW, half one: the number the receipt prints is the money that actually went missing. Checked
    /// against <see cref="ComplianceSurcharge.Rate"/> and the payment itself, never against the code that
    /// produced it — a second copy of the same arithmetic would agree with a wrong answer.
    /// </summary>
    [Fact]
    public void TheDeductionPrintedIsTheMoneyThatWentMissing()
    {
        var wrong = new List<string>();

        foreach (Derelict.Wreck wreck in SomeHulls())
        {
            foreach (bool readRight in new[] { true, false })
            {
                Derelict.WreckCause reported = readRight
                    ? wreck.Cause
                    : (Derelict.WreckCause)(((int)wreck.Cause + 1) % Enum.GetValues<Derelict.WreckCause>().Length);
                if (!readRight && reported == wreck.Cause)
                {
                    continue;
                }

                Derelict.SalvageOutcome o =
                    Derelict.Resolve(wreck, Derelict.SalvageChoice.FileTheReport, reported);

                // What the captain would have been paid had the clause not existed, reconstructed from the
                // two numbers on the receipt — the payment plus the deduction.
                int gross = o.CreditsNow + o.SurchargeCr;
                double expected = gross * ComplianceSurcharge.Rate;

                // The deduction is taken per line and rounded per line, so allow one credit of rounding
                // per line (fee, and bonus when the finding stood).
                double slack = readRight ? 1.5 : 1.0;
                if (Math.Abs(o.SurchargeCr - expected) > slack)
                {
                    wrong.Add(
                        $"  {wreck.Id} ({wreck.ShipName}, cause read {(readRight ? "right" : "wrong")}) — "
                        + $"the card would print a {o.SurchargeCr:N0} cr surcharge on a {gross:N0} cr gross, "
                        + $"but {ComplianceSurcharge.Rate:P0} of that gross is {expected:N1} cr");
                }
            }
        }

        Assert.True(wrong.Count == 0,
                    $"{wrong.Count} filing(s) would print a deduction that is not the money taken:\n"
                    + string.Join("\n", wrong));
    }

    /// <summary>
    /// THE LAW, half two: the surface that shows the payment shows the deduction, and the deduction is
    /// shown with its clause.
    /// </summary>
    [Fact]
    public void TheReceiptThatShowsThePaymentShowsTheClause()
    {
        // The clause is in the line item, not merely in a constant nobody prints.
        Assert.Contains(ComplianceSurcharge.Clause, ComplianceSurcharge.LineItem(1_234), StringComparison.Ordinal);

        string card = TheWreckOutcomeCard();

        Assert.Contains("salvageOutcome.CreditsNow", card, StringComparison.Ordinal);
        Assert.True(
            card.Contains("ComplianceSurcharge.LineItem", StringComparison.Ordinal),
            "the wreck-outcome card prints what the filing paid and never says that cl. 14(b) took a cut of "
            + "it — ComplianceSurcharge.LineItem is not cited in the card block of Map.razor:\n" + card);

        // …and the two sentences that explain the line item are on the same card, so the clause is not a
        // bare number with nothing behind it.
        Assert.Contains("ComplianceSurcharge.WhatItIsLine", card, StringComparison.Ordinal);
        Assert.Contains("TheClauseNonAnswer", card, StringComparison.Ordinal);

        // The office's non-answers are reached through AskAbout, from the client, on a real hull id.
        string page = File.ReadAllText(Path.Combine(ClientRoot, "Pages", "Map.Wreck.cs"));
        Assert.Contains("ComplianceSurcharge.AskAbout", page, StringComparison.Ordinal);
        Assert.NotEmpty(ComplianceSurcharge.AskAbout("kestrels-promise"));
        Assert.Contains(ComplianceSurcharge.AskAbout("kestrels-promise"), ComplianceSurcharge.WhenAsked);
    }

    // ── Reading the card out of the page ─────────────────────────────────────────────────────────────

    /// <summary>The wreck-outcome card's block in <c>Map.razor</c>: from its <c>@if</c> to the brace that
    /// closes it at the same indent. Structural rather than line-numbered, so ordinary edits above it do
    /// not silently move this bench onto a different card.</summary>
    private static string TheWreckOutcomeCard()
    {
        string[] lines = MapMarkup.ReadLines(Path.Combine(ClientRoot, "Pages", "Map.razor"));

        int open = Array.FindIndex(lines, l => l.Contains("@if (_wreckOutcome is { } salvageOutcome)", StringComparison.Ordinal));
        Assert.True(open >= 0, "no `@if (_wreckOutcome is { } salvageOutcome)` in Map.razor — this bench has drifted");

        string indent = lines[open][..(lines[open].Length - lines[open].TrimStart().Length)];
        int close = Array.FindIndex(lines, open + 1, l => l == indent + "}");
        Assert.True(close > open, "the wreck-outcome card's block is never closed at its own indent");

        return string.Join("\n", lines[open..(close + 1)]);
    }

    /// <summary>A spread of real hulls — the seeded generator's own, so the values, causes and ids are the
    /// ones the game deals rather than numbers invented here.</summary>
    private static IEnumerable<Derelict.Wreck> SomeHulls() =>
        Enumerable.Range(0, 24).Select(i => Derelict.Seeded($"bench-wreck-{i}"));

    private static string ClientRoot => Path.Combine(RepoRoot, "src", "SpaceSails.Client");

    private static string RepoRoot
    {
        get
        {
            string? dir = AppContext.BaseDirectory;
            while (dir is not null)
            {
                if (Directory.Exists(Path.Combine(dir, "src", "SpaceSails.Client")))
                {
                    return dir;
                }
                dir = Path.GetDirectoryName(dir);
            }
            throw new DirectoryNotFoundException("Could not find the repository root above the test assembly.");
        }
    }
}
