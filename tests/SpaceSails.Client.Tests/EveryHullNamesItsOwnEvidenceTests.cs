using System;
using System.Collections.Generic;
using System.Text;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #533 · THE CONSOLE THE CAPTAIN PRESSES E ON, held to the name the audit walks to.
///
/// <para>The deck's cause labels were a second complete list of names, typed in the renderer beside Core's
/// own. Neither had an arm for <see cref="Derelict.WreckCause.VentedByOneOfTheirOwn"/>, so the hull
/// <c>?archive=1</c> boots into stood a console amidships in her corridor labelled <b>THE WRECK</b> — and
/// the reachability audit's failure message for that station read "the wreck" too. Two places holding one
/// fact is the bug even while they agree; these two did not.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class EveryHullNamesItsOwnEvidenceTests
{
    [Fact]
    public void NoHullsEvidenceConsoleIsCalledTheWreck()
    {
        var unnamed = new List<string>();
        foreach (Derelict.WreckCause cause in Enum.GetValues<Derelict.WreckCause>())
        {
            string label = WreckInterior.CauseLabel(cause);
            if (label.Contains("THE WRECK", StringComparison.Ordinal))
            {
                unnamed.Add($"  {cause} — the console at her own evidence reads \"{label}\". That is the "
                    + "fallback for a cause nobody wrote an arm for, standing in the room that is the whole "
                    + "reason to board her.");
            }
        }

        Report(unnamed, "hulls whose evidence console has no name of its own");
    }

    [Fact]
    public void TheDeckLabelAndTheAuditsNameAreTheSameName()
    {
        foreach (Derelict.WreckCause cause in Enum.GetValues<Derelict.WreckCause>())
        {
            string label = WreckInterior.CauseLabel(cause);
            string audited = WreckLayout.CauseStationName(cause);

            Assert.EndsWith(audited.ToUpperInvariant(), label, StringComparison.Ordinal);
            Assert.True(label.Length > audited.Length + 1,
                $"{cause}'s label \"{label}\" has lost its glyph");
        }
    }

    /// <summary>
    /// AND THE LABEL IS AN ID. <c>ExamineWreckEvidence</c> recovers which station was pressed by looking for
    /// LOG or MANIFEST in the label text, so a cause whose own name contained either word would hand the
    /// captain the bridge log's paragraph while they stood at the nest. It is true today by luck, and luck
    /// is what this file is for.
    /// </summary>
    [Fact]
    public void NoCauseLabelCanBeMistakenForTheLogOrTheManifest()
    {
        var collisions = new List<string>();
        foreach (Derelict.WreckCause cause in Enum.GetValues<Derelict.WreckCause>())
        {
            string label = WreckInterior.CauseLabel(cause);
            if (label.Contains("LOG", StringComparison.OrdinalIgnoreCase)
                || label.Contains("MANIFEST", StringComparison.OrdinalIgnoreCase))
            {
                collisions.Add($"  {cause} — \"{label}\" would be read as the bridge log / the manifest by "
                    + "EvidenceIdFor, and pressing E at her damage would print somebody else's paragraph.");
            }
        }

        Report(collisions, "cause labels that collide with the station-id lookup");
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
