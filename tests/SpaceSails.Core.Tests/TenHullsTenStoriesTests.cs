namespace SpaceSails.Core.Tests;

using System;
using System.Collections.Generic;
using System.Text;
using SpaceSails.Core;

/// <summary>
/// #533 · TEN NAMED CAUSES, TEN HULLS, TEN STORIES — and the two places the tenth one was never written.
///
/// <para>Every wreck narrates itself from three stations and a card. The taxonomy is the content
/// (<see cref="Derelict.WreckCause"/>), so a cause with no arm in a switch does not error and does not look
/// empty: it quietly borrows the fallback and reads as an ordinary ship. Both of tonight's findings are that
/// shape, and both were on <see cref="Derelict.WreckCause.VentedByOneOfTheirOwn"/> — the hull
/// <c>?archive=1</c> boots into.</para>
///
/// <list type="number">
/// <item><b>Her log said the opposite of her evidence.</b> The log switch had no arm for her, so the bridge
/// station printed <i>"The log ends N years ago. The last entries are ordinary ship's business, and then
/// there are no more"</i> — while <see cref="Derelict.Evidence"/>, on the same screen, said <i>"the log runs
/// on for months after that, in one immaculate hand, signing forty names on and off watch."</i> And the
/// words the log was supposed to speak already existed and were already tested, in
/// <see cref="HullVenting.VentedShipLogLine"/>, read by nothing.</item>
/// <item><b>Her evidence station had no name.</b> No arm in <c>CauseStation</c>, <c>CauseStationName</c> or
/// the client's <c>CauseLabel</c>, so she got the fallback point (0, 0) and a console labelled THE WRECK.</item>
/// </list>
///
/// <para>So these tests are per-CAUSE loops rather than per-symptom assertions: the failure mode is a cause
/// nobody wrote an arm for, and only walking all ten can find the eleventh.</para>
/// </summary>
public class TenHullsTenStoriesTests
{
    private static Derelict.Wreck HullThatDied(Derelict.WreckCause cause) =>
        Derelict.SeededWithCause(cause) ?? throw new InvalidOperationException($"no seeded hull for {cause}");

    // ── Every cause has a place of its own to be read at ──────────────────────────────────────────────

    [Fact]
    public void EveryCauseNamesItsOwnEvidenceStation()
    {
        var unnamed = new List<string>();
        foreach (Derelict.WreckCause cause in Enum.GetValues<Derelict.WreckCause>())
        {
            string name = WreckLayout.CauseStationName(cause);
            if (name == "the wreck")
            {
                unnamed.Add($"  {cause} — its evidence station is called \"the wreck\", which is the fallback "
                    + "for a cause nobody wrote an arm for. The audit walks to it; the captain reads a "
                    + "console with no name on it.");
            }
        }

        Report(unnamed, "causes whose own evidence station is unnamed");
    }

    [Fact]
    public void EveryCauseStationStandsSomewhereOnHerDeck()
    {
        // A station is either in a named compartment or on the spine. Not a reachability audit — WreckLayout
        // already walks those with A* — but the sanity check that a fallback point was never a PLACE.
        foreach (Derelict.WreckCause cause in Enum.GetValues<Derelict.WreckCause>())
        {
            DeckReachability.Point at = WreckLayout.CauseStation(cause);
            bool inARoom = WreckLayout.CompartmentAt(at.X, at.Y) is not null;
            bool onTheSpine = Math.Abs(at.Y) <= WreckLayout.SpineHalfHeight
                              && at.X > WreckLayout.AftX && at.X < WreckLayout.BowX;
            Assert.True(inARoom || onTheSpine,
                $"{cause}'s evidence station at ({at.X}, {at.Y}) is neither in a compartment nor in the spine");
        }
    }

    // ── The log, against the evidence it has to agree with ────────────────────────────────────────────

    [Fact]
    public void TheVentedHullsLogDoesNotEndWhereHerEvidenceSaysItRunsOn()
    {
        Derelict.Wreck w = HullThatDied(Derelict.WreckCause.VentedByOneOfTheirOwn);
        string log = Derelict.LogFinding(w);
        string evidence = Derelict.Evidence(w.Cause);

        // Her evidence promises months of entries after the ending. The station has to deliver them.
        Assert.Contains("runs on for months", evidence, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("eleven more months", log, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("forty names", log, StringComparison.OrdinalIgnoreCase);

        // And it must not do the thing it used to do, which is contradict that in its own first clause.
        Assert.DoesNotContain("The log ends", log, StringComparison.Ordinal);
        Assert.DoesNotContain("there are no more", log, StringComparison.Ordinal);

        // The words were written and tested in Core and printed nowhere. This is the seam that consumes them.
        Assert.Contains(HullVenting.VentedShipLogLine, log, StringComparison.Ordinal);
    }

    [Fact]
    public void NoHullFallsThroughToTheGenericLogLine()
    {
        // The generic ending is a real sentence and reads well — which is exactly why it hid four causes
        // that had never been written. If a future cause wants it, it has to say so in its own arm.
        const string generic = "The last entries are ordinary ship's business, and then there are no more.";
        var borrowed = new List<string>();

        foreach (Derelict.WreckCause cause in Enum.GetValues<Derelict.WreckCause>())
        {
            string log = Derelict.LogFinding(HullThatDied(cause));
            if (log.Contains(generic, StringComparison.Ordinal))
            {
                borrowed.Add($"  {cause} — her bridge log is the fallback: \"{generic}\"");
            }
        }

        Report(borrowed, "causes whose bridge log is nobody's, borrowed from the fallback arm");
    }

    [Fact]
    public void EveryHullsLogAndManifestSayWhoSheWas()
    {
        // Cheap, but it is the check that would have caught a switch returning "" on a new cause.
        foreach (Derelict.WreckCause cause in Enum.GetValues<Derelict.WreckCause>())
        {
            Derelict.Wreck w = HullThatDied(cause);
            Assert.StartsWith("🖥 ", Derelict.LogFinding(w), StringComparison.Ordinal);
            Assert.StartsWith("📦 ", Derelict.ManifestFinding(w), StringComparison.Ordinal);
            Assert.Contains(w.AssessedValueCr.ToString("N0"), Derelict.ManifestFinding(w), StringComparison.Ordinal);
            Assert.NotEqual(string.Empty, Derelict.Evidence(cause));
            Assert.NotEqual(string.Empty, Derelict.CauseHeadline(cause));
            Assert.NotEqual(string.Empty, Derelict.ArtFile(cause));
        }
    }

    // ── The card, against the ship it is describing ───────────────────────────────────────────────────

    [Fact]
    public void TheDecisionCardDoesNotTellThePiratesTheyWereNeverHere()
    {
        // The footing was typed into Map.razor and read "and nobody has been aboard since" on EVERY hull.
        // On the piracy hull the manifest station, two rooms away, says somebody boarded her and was in a
        // hurry; on the infested hull something has been aboard the entire time. What is true of all ten is
        // that nobody came BACK for her — which is also the sentence both roads are priced against.
        foreach (Derelict.WreckCause cause in Enum.GetValues<Derelict.WreckCause>())
        {
            string footing = Derelict.CardFooting(HullThatDied(cause));
            Assert.DoesNotContain("nobody has been aboard", footing, StringComparison.OrdinalIgnoreCase);
        }

        Derelict.Wreck pirated = HullThatDied(Derelict.WreckCause.Piracy);
        Assert.Contains("boarded her", Derelict.ManifestFinding(pirated), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("from outside", Derelict.Evidence(Derelict.WreckCause.Piracy), StringComparison.OrdinalIgnoreCase);
    }

    private static void Report(List<string> bad, string what)
    {
        if (bad.Count == 0)
        {
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"{bad.Count} {what}:");
        foreach (string line in bad)
        {
            sb.AppendLine(line);
        }
        Assert.Fail(sb.ToString());
    }
}
