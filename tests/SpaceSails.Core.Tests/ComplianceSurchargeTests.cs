using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #553 · REFERENCED, NEVER DESCRIBED. Owner, on the incident behind the whole Reever arc: <i>"there was some
/// prior incident that made legal research super slow and expensive and that incentivized moving to hidden
/// places"</i> — and then, ruling on how to deliver it: <i>"Love that referenced but not described … it adds
/// depth 👌"</i>
///
/// <para>These laws exist because that discipline is fragile in exactly one direction: somebody, some day, will
/// want to be helpful and explain the clause. The whole effect dies the moment anything does.</para>
/// </summary>
public sealed class ComplianceSurchargeTests
{
    /// <summary>
    /// NOTHING IN THIS FILE EXPLAINS THE INCIDENT — the load-bearing law. The clause is cited everywhere and
    /// described nowhere, so the words that would describe it must never appear: no accident, no disaster, no
    /// deaths, no year, no name. The captain learns what it COSTS and never what it was.
    /// </summary>
    [Fact]
    public void NothingAnywhereSaysWhatHappened()
    {
        var everything = new List<string>
        {
            ComplianceSurcharge.Clause,
            ComplianceSurcharge.WhatItIsLine,
            ComplianceSurcharge.LineItem(240),
        };
        everything.AddRange(ComplianceSurcharge.WhenAsked);

        foreach (string forbidden in new[]
                 {
                     "incident", "accident", "disaster", "catastrophe", "outbreak",
                     "died", "deaths", "killed", "victims", "inquiry", "scandal",
                 })
        {
            foreach (string line in everything)
            {
                Assert.DoesNotContain(forbidden, line, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    /// <summary>
    /// AND NOBODY WILL ANSWER, WITHOUT ANYBODY LYING. Four different non-answers, none of them evasive — they
    /// genuinely do not know, because nobody working today was working then. That is more unsettling than a
    /// cover-up and far cheaper: a conspiracy needs conspirators, and this needs only a form.
    /// </summary>
    [Fact]
    public void EveryAnswerIsAnHonestNonAnswer()
    {
        Assert.True(ComplianceSurcharge.WhenAsked.Count >= 3, "one stock answer reads as a scripted brush-off");
        Assert.Equal(ComplianceSurcharge.WhenAsked.Count,
                     ComplianceSurcharge.WhenAsked.Distinct(StringComparer.Ordinal).Count());

        foreach (string answer in ComplianceSurcharge.WhenAsked)
        {
            Assert.False(string.IsNullOrWhiteSpace(answer));
            // Nobody refuses. Refusal implies knowledge, and knowledge is the thing that must not exist.
            Assert.DoesNotContain("cannot tell you", answer, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("not permitted", answer, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("classified", answer, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>A given office always answers the same way. An office that changed its story would be a
    /// conspiracy; one that has simply always said the same unhelpful thing is worse.</summary>
    [Fact]
    public void AnOfficeNeverChangesItsStory()
    {
        for (int again = 0; again < 5; again++)
        {
            Assert.Equal(ComplianceSurcharge.AskAbout("selene-gate"), ComplianceSurcharge.AskAbout("selene-gate"));
        }

        // …and different offices are not all reading from one card.
        var voices = new[] { "selene-gate", "the-tilt", "red-eye", "cinder-roost", "ringside-exchange", "the-deep" }
            .Select(ComplianceSurcharge.AskAbout)
            .Distinct(StringComparer.Ordinal)
            .Count();
        Assert.True(voices > 1);
    }

    /// <summary>The clause reference is ONE stable string. A number that recurs across unrelated paperwork is how
    /// a player works out that one event is behind all of it; a number that drifted would read as set dressing.</summary>
    [Fact]
    public void TheClauseNumberIsTheOneFactAnybodyHas()
    {
        Assert.False(string.IsNullOrWhiteSpace(ComplianceSurcharge.Clause));
        Assert.Contains(ComplianceSurcharge.Clause, ComplianceSurcharge.LineItem(100), StringComparison.Ordinal);
    }

    // ── What it costs, and who pays it ────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE HONEST ROAD PAYS IT AND THE QUIET ROAD DOES NOT. This is the whole reason the surcharge is a mechanic
    /// rather than flavour: filing is legal work, stripping her and saying nothing is not. The captain FEELS the
    /// regulation instead of reading about it — and the game's oldest salvage rule (filing never out-earns
    /// stripping) gets the in-fiction reason it never had.
    /// </summary>
    [Fact]
    public void OnlyDoingItProperlyCostsAnything()
    {
        Assert.True(ComplianceSurcharge.AppliesTo(Derelict.SalvageChoice.FileTheReport));
        Assert.False(ComplianceSurcharge.AppliesTo(Derelict.SalvageChoice.StripAndSayNothing));
    }

    /// <summary>…and it really does come off the money, on both halves of a filing.</summary>
    [Fact]
    public void ItComesOffTheFeeAndTheBonusAlike()
    {
        Derelict.Wreck w = new("audit-hull", "Audited Hull", Derelict.WreckCause.HullBreach, 100_000, 40);

        Derelict.SalvageOutcome filed = Derelict.Resolve(
            w, Derelict.SalvageChoice.FileTheReport, Derelict.WreckCause.HullBreach);
        Derelict.SalvageOutcome stripped = Derelict.Resolve(
            w, Derelict.SalvageChoice.StripAndSayNothing, null);

        int grossFee = (int)Math.Round(100_000 * Derelict.ReportFeeFraction);
        int grossBonus = (int)Math.Round(100_000 * Derelict.CorrectCauseBonusFraction);

        Assert.Equal(ComplianceSurcharge.Deduct(grossFee) + ComplianceSurcharge.Deduct(grossBonus), filed.CreditsNow);
        Assert.True(filed.CreditsNow < grossFee + grossBonus);

        // The oldest salvage rule still holds, and now for a reason somebody could name.
        Assert.True(stripped.CreditsNow > filed.CreditsNow);
    }

    /// <summary>An irritation, not a punishment. A tax you notice every time is worth more here than one that
    /// hurts once — and if it ever became painful, captains would stop filing and the whole honest road would
    /// die rather than merely cost.</summary>
    [Fact]
    public void ItIsATaxAndNotAPenalty()
    {
        Assert.InRange(ComplianceSurcharge.Rate, 0.01, 0.10);
        Assert.Equal(0, ComplianceSurcharge.AmountOn(0));
        Assert.Equal(0, ComplianceSurcharge.AmountOn(-500));
        Assert.Equal(400, ComplianceSurcharge.AmountOn(10_000));
    }

    /// <summary>The line item states a number and a clause and stops — the entire delivery of the incident is
    /// this one sentence, repeated for years.</summary>
    [Fact]
    public void TheLineItemIsANumberAndAClauseAndNothingElse()
    {
        string line = ComplianceSurcharge.LineItem(2_400);

        Assert.Contains("2,400", line, StringComparison.Ordinal);
        Assert.Contains(ComplianceSurcharge.Clause, line, StringComparison.Ordinal);
        Assert.True(line.Length < 70, "a receipt line that starts explaining itself is not a receipt line");
    }
}
