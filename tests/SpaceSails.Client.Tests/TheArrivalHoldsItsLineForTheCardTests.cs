using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #768 · THE ARRIVAL THAT RAISES A CARD EATS ITS OWN LINE — the wiring half.
///
/// <para>Owner's residual off #693/PR #766: <i>"on a from-the-surface ride straight through a gate, #585's
/// first-descent CARD raises on the same arrival, and the gate-accepted pulse line plays UNDER its
/// backdrop."</i> The law itself is <see cref="SpaceSails.Core.PulseHold"/> and is guarded in Core
/// (<c>TheHeldSayingOutlivesTheCardTests</c>) where a sweep can reach it. What cannot be reached from there
/// is the thing that actually broke: a saying said in the wrong PLACE, in a partial class in a razor page.</para>
///
/// <para>So these are source-shape guards, the same house shape as <c>TheCardOpensTheBottomFloorTests</c> and
/// #736's <c>TheOutcomeIsOnThePopUpTests</c>, and they pin four things: the arrival HOLDS rather than pulses;
/// it releases once its cards have been raised and not before; every road out of a card frees what it was
/// standing on; and <b>a method that holds always releases</b> — a hold with no release is a sentence lost
/// forever, which is a worse bug than the one this fixes.</para>
///
/// <para><b>Red proof.</b> Revert Map.Surface.cs / Map.Deck.cs / Map.RevealCard.cs to the shipped versions
/// and every test here goes red, naming the call site.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheArrivalHoldsItsLineForTheCardTests
{
    private static string RepoRoot()
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null)
        {
            if (Directory.Exists(Path.Combine(at.FullName, "src", "SpaceSails.Client")))
            {
                return at.FullName;
            }
            at = at.Parent;
        }
        throw new DirectoryNotFoundException($"could not find the repo root above {AppContext.BaseDirectory}");
    }

    private static string Pages(string file) =>
        File.ReadAllText(Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages", file));

    private static string Between(string text, string from, string to)
    {
        int start = text.IndexOf(from, StringComparison.Ordinal);
        Assert.True(start >= 0, $"'{from}' is no longer in the file this guard reads.");
        int end = text.IndexOf(to, start, StringComparison.Ordinal);
        Assert.True(end > start, $"'{to}' no longer follows '{from}' — this guard needs re-reading.");
        return text[start..end];
    }

    private const string Release = "ReleaseHeldSayingsUnlessACardStopsTheWorld(";

    // ── The issue's own case ─────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TheArrivalHOLDSItsSayingsInsteadOfPulsingThemUnderItsOwnCard()
    {
        // RideTheLiftTo raises up to three cards on the way through — the first descent (#585), the dead-air
        // warning (#609), the face of the gate that read the paper (#684) — and used to pulse every saying as
        // it went. Whatever the ranks decided, the winner spent its whole dwell behind a backdrop.
        string ride = Between(
            Pages("Map.Surface.Hive.cs"),
            "private void RideTheLiftTo(",
            "private (double X, double Y) SecretLabHeadSpot(");
        string loop = Between(ride, "foreach (UndergroundComplex.Saying saying in", "ApplyNerveShock(");

        Assert.True(loop.Contains("HoldAndFile(saying.Text", StringComparison.Ordinal),
            "the arrival's sayings do not go through the #768 hold — an arrival that raises a card is " +
            "pulsing under its own backdrop again.");
        Assert.True(!loop.Contains("ShowAndFile(", StringComparison.Ordinal),
            "the arrival still ShowAndFiles a saying, which pulses it on the frame a card goes up (#768).");
        Assert.True(!loop.Contains("ShowPulseMessage(", StringComparison.Ordinal),
            "the arrival still pulses directly inside the loop that raises its cards (#768).");
    }

    [Fact]
    public void TheArrivalReleasesOnlyAFTEREveryCardItCanRaiseHasBeenRaised()
    {
        // The release is the one place that can honestly ask "is something in front of the captain?", and it
        // can only ask it once the arrival has finished raising things. Released above the loop and the
        // answer is about a screen that is one frame out of date — which is the bug, with extra steps.
        string ride = Between(
            Pages("Map.Surface.Hive.cs"),
            "private void RideTheLiftTo(",
            "private (double X, double Y) SecretLabHeadSpot(");

        int released = ride.IndexOf(Release, StringComparison.Ordinal);
        Assert.True(released >= 0, "the arrival never releases what it held — every arrival saying in the " +
            "Hive is now lost, which is worse than #768's bug.");
        Assert.True(released > ride.LastIndexOf("_viewObject = new", StringComparison.Ordinal),
            "the arrival releases BEFORE the last card it can raise — the release asks whether a card is up " +
            "and is answered by a screen that has not finished happening yet (#768).");
    }

    // ── The other members of the family PR #769 left standing ────────────────────────────────────────────

    [Fact]
    public void TheRepoBoatsArrivalLineIsHeldForItsOwnPlate()
    {
        // #769's residual list, second entry: "LandTheCollectors / StepCollectors — the arrival line and the
        // shelter note are eaten by the siege plate they raise." Same shape, same family: the world acting.
        string land = Between(
            Pages("Map.Surface.RepoBoat.cs"), "private void LandTheCollectors(", "private void TheWritIsServed(");

        Assert.True(!land.Contains("ShowPulseMessage(", StringComparison.Ordinal),
            "the collectors' arrival still pulses under the plate it raises in the same breath (#768/#583).");
        Assert.True(land.Contains("HoldSaying(CollectorLanding.ArrivalLine", StringComparison.Ordinal),
            "the boat's arrival line is not held — the one sentence naming the callsign plays behind the " +
            "arrival plate's backdrop (#768).");
        Assert.True(land.Contains(Release, StringComparison.Ordinal),
            "LandTheCollectors holds and never releases, so its lines are lost whatever the card does.");
    }

    [Fact]
    public void TheShelterIsNotSanctuaryLineIsHeldForTheSiegePlate()
    {
        // The one sentence in the scene that tells a captain the shelter they are standing in will not save
        // them — said, until now, under the picture of the people who came for them.
        string step = Between(
            Pages("Map.Surface.RepoBoat.cs"), "private void StepCollectors(", "private static double Distance(");

        Assert.True(step.Contains("HoldSaying(CollectorLanding.ShelterIsNotSanctuaryLine", StringComparison.Ordinal),
            "the shelter warning is pulsed under the siege plate raised two lines below it (#768/#583).");
        Assert.True(!step.Contains("ShowPulseMessage(", StringComparison.Ordinal),
            "StepCollectors pulses something on the frame it raises a card (#768).");
        Assert.True(step.Contains(Release, StringComparison.Ordinal),
            "StepCollectors holds and never releases.");
    }

    // ── The seam itself ──────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void EveryRoadOutOfACardFreesWhatItWasStandingOn()
    {
        // Esc, Enter, E again, the backdrop and the Close button all end in these two methods — so the
        // release hangs there and nowhere else. Any code that clears the field BY HAND is a road out of the
        // card that swallows the held line, which is the shipped bug wearing a new hat.
        string deck = Pages("Map.Deck.Fixtures.cs");
        string reveal = Pages("Map.RevealCard.cs");

        Assert.True(Between(deck, "private void CloseViewObject(", "private void KnockOnHatch(")
                .Contains("ReleaseHeldSayings(", StringComparison.Ordinal),
            "CloseViewObject does not free the held sayings — the arrival's card comes down on silence (#768).");
        Assert.True(Between(reveal, "private void CloseRevealCard(", "/// <summary>Raise the card.")
                .Contains("ReleaseHeldSayings(", StringComparison.Ordinal),
            "CloseRevealCard does not free the held sayings (#768).");

        foreach (string file in Directory.EnumerateFiles(
                     Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages"), "*.cs"))
        {
            foreach (string field in new[] { "_viewObject", "_revealCard" })
            {
                int by = File.ReadAllText(file).Split($"{field} = null").Length - 1;
                string owner = field == "_viewObject" ? "Map.Deck.Fixtures.cs" : "Map.RevealCard.cs";
                int allowed = Path.GetFileName(file) == owner ? 1 : 0;   // the Close method itself
                Assert.True(by <= allowed,
                    $"{Path.GetFileName(file)} clears {field} by hand — that is a road out of the card that " +
                    "never releases what the card was standing on (#768). Call the Close method.");
            }
        }
    }

    [Fact]
    public void AMethodThatHOLDSAlwaysRELEASES()
    {
        // The anti-vacuous one, and the guard that matters most to whoever adds the next arrival: a hold with
        // no release is not "a line said late", it is a line never said at all — a worse bug than #768's, and
        // silent. So every method in the client that keeps a saying back must also decide, in its own body,
        // whether to let it go. Read out of the source rather than listed here, so a fourth holding site is
        // covered by this guard the moment it is written.
        var holders = new List<string>();
        foreach (string file in Directory.EnumerateFiles(
                     Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages"), "*.cs"))
        {
            string src = File.ReadAllText(file);

            // Method bodies, cut at the four-space declarations that begin them. Crude on purpose: if this
            // ever stops finding methods the count assertion below fails loudly rather than passing on none.
            string[] blocks = Regex.Split(src, @"\r?\n(?=    (?:private|internal|public|protected)\s)");
            foreach (string block in blocks)
            {
                if (!block.Contains("HoldSaying(", StringComparison.Ordinal)
                    && !block.Contains("HoldAndFile(", StringComparison.Ordinal))
                {
                    continue;
                }

                string name = Regex.Match(block, @"(\w+)\s*\(").Groups[1].Value;
                if (name is "HoldSaying" or "HoldAndFile")
                {
                    continue;   // the seam's own declarations
                }

                holders.Add($"{Path.GetFileName(file)}:{name}");
                Assert.True(block.Contains(Release, StringComparison.Ordinal),
                    $"{Path.GetFileName(file)}.{name} holds a saying and never releases it — the line is " +
                    "lost for good, whatever the card does (#768).");
            }
        }

        Assert.True(holders.Count >= 3,
            $"only {holders.Count} holding site(s) found ({string.Join(", ", holders)}) — this guard has " +
            "stopped being able to read the source it audits, so it is passing on nothing.");
    }
}
