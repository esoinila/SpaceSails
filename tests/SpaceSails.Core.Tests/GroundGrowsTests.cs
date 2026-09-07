using System.Linq;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>#563 · The map-just-grew card. Pinned the way <see cref="GroundLesson"/> is: this is a card a
/// captain sees exactly once, ever, so there is no second chance for it to be wrong.</summary>
public class GroundGrowsTests
{
    [Fact]
    public void TheCard_IsFullyDressed()
    {
        Assert.False(string.IsNullOrWhiteSpace(GroundGrows.Stamp));
        Assert.False(string.IsNullOrWhiteSpace(GroundGrows.Head));
        Assert.False(string.IsNullOrWhiteSpace(GroundGrows.Dismiss));
        Assert.False(string.IsNullOrWhiteSpace(GroundGrows.Foot));
        Assert.NotEmpty(GroundGrows.Beats);
    }

    [Fact]
    public void EveryBeat_IsRealProse_NotAStub()
    {
        // The failure this card exists to end is a one-line toast that faded before it taught anything.
        // Replacing it with three stub bullets would be the same failure wearing a bigger frame.
        foreach (string beat in GroundGrows.Beats)
        {
            Assert.True(beat.Length > 60, $"a beat barely explains itself: \"{beat}\"");
        }
    }

    [Fact]
    public void ItPromisesTheThingThatChangesHowYouPlay_ThatAWallIsNotAlwaysTheEnd()
    {
        // The entire point. A captain who believes the site has edges stops at them; one who believes a
        // sealed thing is a question goes looking. Everything #563 builds — huts, caches, breadcrumbs —
        // is worth nothing to a player who never learned to try the door.
        string all = string.Join(" ", GroundGrows.Beats).ToUpperInvariant();
        Assert.Contains("SEALED", all, System.StringComparison.Ordinal);
    }

    [Fact]
    public void ItSaysNobodyWasTeleported()
    {
        // The mechanic is genuinely unusual and easy to misread as a level load. If the card does not say
        // the ground simply CONTINUES, a player reasonably assumes they were moved somewhere else — and
        // then the walk back to the tube, which is the whole tether (#562), stops making sense to them.
        string first = GroundGrows.Beats[0].ToUpperInvariant();
        Assert.Contains("NOT MOVED", first, System.StringComparison.Ordinal);
    }

    [Fact]
    public void ItNamesTheCost_BecauseForcingSomethingOpenIsNeverFree()
    {
        // "buys time, never safety" is the law the sentries live under and the door channel obeys too: the
        // tracker keeps sweeping while you work. A card that sold expansion as pure upside would be lying.
        string all = string.Join(" ", GroundGrows.Beats).ToUpperInvariant();
        Assert.Contains("TIME", all, System.StringComparison.Ordinal);
    }

    [Fact]
    public void TheArt_IsUnderTheArtFolder_AndIsAJpeg()
    {
        // The house rule: art paths are owned by Core so the markup cannot drift from the file on disk.
        Assert.StartsWith("art/", GroundGrows.ArtFile, System.StringComparison.Ordinal);
        Assert.EndsWith(".jpg", GroundGrows.ArtFile, System.StringComparison.Ordinal);
    }

    [Fact]
    public void TheDismiss_IsAnAcknowledgement_NotAnOk()
    {
        // Same voice rule as GroundLesson.Dismiss ("Boots on, then."). A bare OK on a card about the world
        // getting bigger is a wasted line.
        Assert.DoesNotContain("OK", GroundGrows.Dismiss, System.StringComparison.OrdinalIgnoreCase);
        Assert.True(GroundGrows.Dismiss.Length > 10);
    }

    // ── #584 · AND WHERE ────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #584 · <b>THE WHERE-LINE IS THREE TOKENS THIS GAME ALREADY OWNS, AND NOTHING ELSE.</b>
    ///
    /// <para>The owner's complaint was that the card never says where. The cheap wrong answer would have
    /// been to write a sentence — <i>"New ground opens to the north-east, forty metres"</i> — which invents
    /// a compass, a unit and a voice in one line, none of which the instruments would have agreed with.
    /// So the line is taken apart here and every piece is held against the thing that owns it: the floor's
    /// plate (<see cref="WalletChoice.FloorTag"/>, itself read off <c>UndergroundComplex.NameOf</c>), the
    /// SDR kit's four-word compass, and the fan's own way of saying how far.</para>
    ///
    /// <para>The range is checked <b>by substring against <see cref="MotionTracker.Readout"/> itself</b>
    /// rather than against a format string typed here. That is the difference between a guard and a
    /// restatement: swap the unit for metres, round it differently, or put the number after the word, and
    /// this goes red because the fan's own line no longer contains what the card is showing.</para>
    /// </summary>
    [Theory]
    [InlineData("luna", 0, 40.0, 0.0)]
    [InlineData("luna", 0, -40.0, 0.0)]
    [InlineData("titan", 0, 0.0, 30.0)]
    [InlineData("titan", 0, 0.0, -30.0)]
    [InlineData("the-hive-miranda", -2, 12.0, -55.0)]
    [InlineData("phobos", -1, -300.0, 7.0)]
    public void TheWhereLine_IsComposedOfTokensTheGameAlreadyOwns(
        string body, int level, double dx, double dy)
    {
        string line = GroundGrows.Where(body, level, dx, dy);

        string[] floorAndRest = line.Split(" · ");
        Assert.True(floorAndRest.Length == 2,
            $"#584 · the where-line is not a plate any more: \"{line}\". It is FLOOR · BEARING — RANGE, and " +
            "every one of those three is somebody else's word. A fourth clause is a sentence, and a " +
            "sentence is what this fix exists not to write.");

        // 1 · The floor is the building's own plate, read and not re-derived.
        Assert.Equal(WalletChoice.FloorTag(body, level), floorAndRest[0]);

        string[] bearingAndRange = floorAndRest[1].Split(" — ");
        Assert.True(bearingAndRange.Length == 2, $"#584 · no bearing/range pair in \"{line}\".");

        // 2 · The bearing is the SDR kit's compass — the exact word, from the kit's own list.
        Assert.Equal(SdrScanner.BearingFrom(dx, dy), bearingAndRange[0]);
        Assert.Contains(bearingAndRange[0], SdrScanner.Bearings);

        // 3 · The range is the fan's own. Not "looks like the fan's" — it is a SUBSTRING of the sentence
        //     MotionTracker itself writes about a contact at that range, so the unit and the rounding are
        //     the instrument's and can never become a second opinion.
        double range = System.Math.Sqrt((dx * dx) + (dy * dy));
        Assert.Contains(bearingAndRange[1], MotionTracker.Readout(range, closing: false),
            System.StringComparison.Ordinal);
    }

    /// <summary>#584 · <b>THE RESERVED WORD IS NOT IN IT.</b> docs/worldbuilding-notes.md §8: there is ONE
    /// monolith and the word is reserved. A location line is exactly where a future hand would reach for a
    /// landmark to pace from — <c>CacheMint</c> already does — and the moment it names one on a body that is
    /// not Phobos, the game has two.</summary>
    [Fact]
    public void TheWhereLine_NeverSpendsTheReservedWord()
    {
        foreach (string body in new[] { "luna", "phobos", "titan", "miranda", "the-hive-luna" })
        {
            foreach (int level in new[] { 0, -1, -4 })
            {
                foreach ((double dx, double dy) in new[] { (17.0, 3.0), (-5.0, -90.0), (0.0, 0.0) })
                {
                    Assert.DoesNotContain("MONOLITH",
                        GroundGrows.Where(body, level, dx, dy).ToUpperInvariant(),
                        System.StringComparison.Ordinal);
                }
            }
        }
    }

    /// <summary>
    /// #584 · <b>NO NEW STRING CONSTANT — a reflection sweep over the card's own copy.</b>
    ///
    /// <para>The fix's whole claim is that WHERE was answered without writing a new sentence. That claim is
    /// cheap to break by accident: the next hand that wants the line to read better adds
    /// <c>public const string WherePrefix = "New ground opens "</c> and the card grows a voice that no
    /// instrument shares. So the constants on this type are enumerated and held to the five the card had
    /// before #584 — a sixth has to be argued for in a diff to this list, which is the point.</para>
    /// </summary>
    [Fact]
    public void TheCopy_GrewNoNewSentenceForTheLocation()
    {
        string[] owned =
        [
            nameof(GroundGrows.Stamp), nameof(GroundGrows.Head), nameof(GroundGrows.ArtFile),
            nameof(GroundGrows.Dismiss), nameof(GroundGrows.Foot),
        ];

        string[] found =
        [
            .. typeof(GroundGrows)
                .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .Where(f => f.IsLiteral || f.FieldType == typeof(string))
                .Select(f => f.Name)
                .Order(System.StringComparer.Ordinal),
        ];

        Assert.True(found.Length > 0,
            "the reflection sweep found no string constants on GroundGrows at all — it is reading the wrong " +
            "type, and would then pass over any sentence anybody added.");
        Assert.Equal(owned.Order(System.StringComparer.Ordinal), found);
    }
}
