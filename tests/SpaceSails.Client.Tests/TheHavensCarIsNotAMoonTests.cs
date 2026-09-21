using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using static SpaceSails.Client.Tests.CastawayBench;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1280 · <b>A MOON'S DEPTH COLUMN HAS NO BUSINESS ON A SPACE STATION.</b>
///
/// <para>Owner's QA, played at Selene Gate — a berth in ORBIT off Luna — on the haven car's own panel:</para>
///
/// <code>
///   SURFACE      CONCOURSE            🫁 air
///   −150 m       SERVICE LEVEL        ◄ you are here
/// </code>
///
/// <para>Three things a station cannot mean, all of them drawn by one surface asking a moon's questions of a
/// haven's rows. <c>SURFACE</c> against the row a captain presses to go home; <c>−150 m</c>, which is
/// <c>UndergroundComplex.MetresDown</c> — a depth in REGOLITH — quoted for a corridor inside a pressure hull,
/// and this repository's own named bug class (<i>a MOON constant governing a SHIP</i>); and the dead-air
/// column that §0 of the testing links rules out in as many words (<i>"or a dead-air tag (none of that is a
/// station)"</i>), drawn on the row the captain is NOT standing on, so the floor under his feet reads as the
/// one without air.</para>
///
/// <para><b>Core was already right and said so.</b> <see cref="HavenLevels.Panel"/> answers "pressurised" for
/// both floors and carries a paragraph explaining why a haven has no air column at all. The surface that
/// draws the rows asked <c>UndergroundComplex</c> anyway — so the one sentence Core states was contradicted
/// by the one surface a captain reads. The fix is a MODE on the one panel
/// (<c>LiftPanel.TheCarIsInAStation</c>) and never a second set of buttons.</para>
///
/// <h3>What these laws watch</h3>
///
/// <para>The markup, because that is where the bug lived and a Core-only law would have been green over it
/// all along; the PAGE, because the mode has to be ON at a berth and OFF everywhere else; and the Hive,
/// because a lane that took the depth column off a moon would have fixed a station by breaking the building
/// the column belongs to.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheHavensCarIsNotAMoonTests
{
    private static string Berth => HavenInterior.TheHavenWithFloors!;

    /// <summary>The lift panel's markup, razor comments stripped and whitespace flattened — so a law about
    /// which branch a span is INSIDE is not also a law about indentation. The component is read through
    /// <see cref="MapMarkup"/>, which splices its code-behind back on; nothing here opens half a file.</summary>
    private static string TheRowMarkup
    {
        get
        {
            string razor = MapMarkup.Read(Path.Combine(
                TestTree.RepoRoot(), "src", "SpaceSails.Client", "Pages", "Map", "LiftPanel.razor"));
            string bare = Regex.Replace(razor, @"@\*.*?\*@", " ", RegexOptions.Singleline);

            int start = bare.IndexOf("string cls = \"lift-stop\"", StringComparison.Ordinal);
            Assert.True(start >= 0, "LiftPanel.razor no longer draws a stop as a button this guard can find.");
            int end = bare.IndexOf("</button>", start, StringComparison.Ordinal);
            Assert.True(end > start, "LiftPanel.razor's stop button no longer closes where this guard expects.");

            return Regex.Replace(bare[start..end], @"\s+", " ").Trim();
        }
    }

    /// <summary>
    /// <b>THE DEPTH COLUMN AND THE AIR TAG ARE BOTH FENCED BEHIND "THIS IS A STATION".</b>
    ///
    /// <para>Read off the row's own markup, because the row is where the moon got in: Core's answer was right
    /// and the drawing did not ask it. Both spans are asserted to be the ONLY ones of their kind in the row —
    /// a second, unfenced <c>DepthPaint</c> anywhere in the button would put the regolith straight back.</para>
    ///
    /// <para><b>RED on the shipped tree:</b> there is no fence at all, so both assertions fail on the first
    /// clause.</para>
    /// </summary>
    [Fact]
    public void TheRowDrawsNoDepthAndNoAirTagInAStation()
    {
        string row = TheRowMarkup;

        Assert.Contains(
            "@if (!TheCarIsInAStation) { <span class=\"lift-stop-depth\">", row, StringComparison.Ordinal);
        Assert.Contains(
            "@if (!TheCarIsInAStation) { <text>🫁 air</text> }", row, StringComparison.Ordinal);

        Assert.Equal(1, Occurrences(row, "DepthPaint("));
        Assert.Equal(1, Occurrences(row, "🫁"));
        Assert.Equal(1, Occurrences(row, "lift-stop-depth"));

        // …and the ROW knows which shape it is wearing, so the sheet can give a station two columns rather
        // than holding a gutter open for a number that is never coming.
        Assert.Contains("TheCarIsInAStation ? \" station\" : \"\"", row, StringComparison.Ordinal);

        // The tooltip is a moon's too — "holds pressure" / "dead air — the tank runs" is a sentence about
        // regolith and a tank, and there is neither at a berth. It is the same one fence.
        Assert.Contains("TheCarIsInAStation ? null :", row, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>…AND THE HIVE'S ROW STILL CARRIES BOTH.</b> The fence is <c>!TheCarIsInAStation</c> and a moon is
    /// not a station, so every row underground keeps the depth it has always had and the air column #802's
    /// scar is about. Asserted against the shipped answers rather than against the markup, so a lane that
    /// ever tidied the column away is red here even if the fence still reads correctly.
    /// </summary>
    [Fact]
    public void TheMoonKeepsItsDepthColumnAndItsAir()
    {
        Assert.Equal("SURFACE", UndergroundComplex.DepthPaint(0));
        Assert.Contains(" m", UndergroundComplex.DepthPaint(-1), StringComparison.Ordinal);

        IReadOnlyList<UndergroundComplex.LiftStop> hive = UndergroundComplex.LiftPanel("luna", -1, []);
        Assert.True(hive.Count > 1, "the Hive's panel draws no rows, so this law is about nothing.");
        Assert.Contains(hive, s => !s.Pressurised);   // the dead-air column still has something to say
        Assert.Contains(hive, s => s.Pressurised);    // …and so does the air one
    }

    /// <summary>
    /// <b>THE HAVEN'S ROWS ARE PLATES AND NOTHING ELSE.</b> The list the panel is actually handed at a berth,
    /// asked of the page in the posture the panel is open in — no metre, no <c>SURFACE</c>, no air word in any
    /// row's name, on either floor.
    ///
    /// <para>It is the sentence <see cref="HavenLevels"/> already states in prose, asked of the strings a
    /// captain reads. A row whose NAME grew a depth would walk round the fence above without touching it.</para>
    /// </summary>
    [Fact]
    public void EveryRowAStationOffersIsJustTheFloorsName()
    {
        Pages.Map map = Ashore("haven-car-not-a-moon");
        Assert.True((bool)Read(map, "TheStationHasFloors")!, "the page says this berth has no floors.");

        foreach (int floor in HavenLevels.Levels)
        {
            Set(map, "_havenFloor", floor);
            var stops = (IReadOnlyList<UndergroundComplex.LiftStop>)Invoke(map, "LiftStops")!;
            Assert.Equal(HavenLevels.Levels.Count, stops.Count);

            foreach (UndergroundComplex.LiftStop stop in stops)
            {
                Assert.Equal(HavenLevels.NameOf(stop.Level), stop.Name);
                Assert.DoesNotContain(" m", stop.Name, StringComparison.Ordinal);
                Assert.DoesNotContain("SURFACE", stop.Name, StringComparison.Ordinal);
                Assert.DoesNotContain("🫁", stop.Name, StringComparison.Ordinal);
                Assert.DoesNotContain("air", stop.Name, StringComparison.OrdinalIgnoreCase);

                // …and the depth the moon's painter WOULD have put beside it is exactly what the owner read
                // off the screen — which is why the fence has to exist rather than the painter being trusted.
                Assert.NotEqual(
                    HavenLevels.NameOf(stop.Level), UndergroundComplex.DepthPaint(stop.Level));
            }
        }
    }

    /// <summary>
    /// <b>THE MODE IS OFF WHERE THERE IS NO STATION.</b> A captain who is not ashore at a berth with floors
    /// is never in a station's car — which is what keeps the Hive's rows the Hive's. The page's own answer,
    /// read in the two postures that are not it.
    /// </summary>
    [Fact]
    public void TheStationModeIsOffWhenTheCaptainIsNotInOne()
    {
        Pages.Map map = Ashore("haven-car-mode-off");

        Set(map, "_deckMode", false);
        Assert.False((bool)Read(map, "TheStationHasFloors")!, "off the deck there is no station car.");

        Set(map, "_deckMode", true);
        Set(map, "_dockedHavenId", null);
        Assert.False((bool)Read(map, "TheStationHasFloors")!, "with no berth there is no station car.");
    }

    // ── THE BENCH ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A live page clamped on at the one station with a floor under it — the same boot
    /// <see cref="TheRideDownIsAWayBackTests"/> uses, for the same reason.</summary>
    private static Pages.Map Ashore(string canvasId)
    {
        Pages.Map map = Boot(canvasId);
        var sky = (ICelestialEphemeris)Read(map, "_ephemeris")!;
        CelestialBody berth = sky.Bodies.First(b => b.Id == Berth);
        Invoke(map, "ClampOntoHaven", berth, sky.Position(Berth, (double)Read(map, "SimTime")!), null);
        Assert.True((bool)Invoke(map, "StandAtTheBarThreshold")!, "the ashore boot refused this berth.");
        return map;
    }

    private static int Occurrences(string text, string needle)
    {
        int n = 0;
        for (int at = text.IndexOf(needle, StringComparison.Ordinal); at >= 0;
             at = text.IndexOf(needle, at + needle.Length, StringComparison.Ordinal))
        {
            n++;
        }

        return n;
    }
}
