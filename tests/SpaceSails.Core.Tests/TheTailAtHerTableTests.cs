using System;
using System.Linq;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #1202 slice 3 · <b>THE TAIL AT HER TABLE — the arithmetic.</b> SEEN is a flag on the spike and never a fourth
/// outcome; what Core decides alone is pinned here: when a take is seen (the tail's own band, nothing else), what
/// the window makes of a seen take, which receipt the desk shows, when the squared-stack line is told, and the one
/// line of contract state the flag rides. The page's half is driven on a live page in
/// <c>TheTakeCanBeSeenTests</c>. Every guard below was watched go red on the revert its summary names.
/// </summary>
public sealed class TheTailAtHerTableTests
{
    /// <summary>
    /// A SEEN TAKE CANNOT SPIKE: at the window it is LATE, whatever the pages are — taken, back with the client's
    /// page, or on her table. An unseen take is exactly slice 2's one of three, SPIKED and ALTERED both still
    /// possible. No fourth outcome exists.
    ///
    /// <para><b>RED</b> by <c>TheWindowFor</c> ignoring <c>seenTaking</c> (a seen take spiked), and by it answering
    /// LATE for an unseen take too (SPIKED unreachable).</para>
    /// </summary>
    [Fact]
    public void ASeenTakeIsLateWhateverElseWasDoneAndAnUnseenOneIsSliceTwos()
    {
        foreach (SpikeIt.Pages pages in Enum.GetValues<SpikeIt.Pages>())
        {
            Assert.Equal(SpikeIt.Outcome.Late, SpikeIt.TheWindowFor(pages, seenTaking: true));
            Assert.Equal(SpikeIt.AtTheWindow(pages), SpikeIt.TheWindowFor(pages, seenTaking: false));
        }

        Assert.Equal(SpikeIt.Outcome.Spiked, SpikeIt.TheWindowFor(SpikeIt.Pages.Taken, seenTaking: false));
        Assert.Equal(SpikeIt.Outcome.Altered, SpikeIt.TheWindowFor(SpikeIt.Pages.Swapped, seenTaking: false));
        Assert.Equal(4, Enum.GetValues<SpikeIt.Outcome>().Length);
        Assert.True(SpikeIt.Ran(SpikeIt.TheWindowFor(SpikeIt.Pages.Taken, seenTaking: true)));
    }

    /// <summary>
    /// SEEN IS THE TAIL'S OWN CONTACT: a man on the captain holding his band (<see cref="TheTailBehindYou.HoldsHisBand"/>,
    /// nine to thirty deck units) — and nobody on the floor, or a man beyond his band's far edge, sees nothing.
    ///
    /// <para><b>RED</b> by dropping <c>aManIsOnYou</c> from <c>TheTakeIsSeen</c> (a take seen by a man who is not
    /// there), and by dropping the band (every take seen from anywhere on the station).</para>
    /// </summary>
    [Fact]
    public void ATakeIsSeenOnlyByAManHoldingHisBand()
    {
        double inTheBand = (TheTailBehindYou.StandsOffDu + TheTailBehindYou.LosesYouBeyondDu) / 2;
        Assert.True(SpikeIt.TheTakeIsSeen(aManIsOnYou: true, inTheBand));
        Assert.True(SpikeIt.TheTakeIsSeen(aManIsOnYou: true, TheTailBehindYou.LosesYouBeyondDu));
        Assert.False(SpikeIt.TheTakeIsSeen(aManIsOnYou: false, inTheBand));
        Assert.False(SpikeIt.TheTakeIsSeen(aManIsOnYou: true, TheTailBehindYou.LosesYouBeyondDu + 0.01));
        Assert.False(SpikeIt.TheTakeIsSeen(aManIsOnYou: false, double.MaxValue));

        // The Selene Gate case the canon names: the man at the mouth of the tube and the captain at the far table
        // are inside the band — measured on the live page in TheTakeCanBeSeenTests, stated here as the premise.
        Assert.Equal(30.0, TheTailBehindYou.LosesYouBeyondDu);
    }

    /// <summary>
    /// THE RECEIPT SAYS WHY ONLY WHEN IT WAS SEEN: a seen LATE says <i>'Noted that you were seen.'</i>, an unseen
    /// LATE says slice 2's <i>'Noted that you tried.'</i>, and the two never swap.
    ///
    /// <para><b>RED</b> by <c>LateReceipt</c> returning <c>SeenLateLine</c> unconditionally (every LATE was "seen").</para>
    /// </summary>
    [Fact]
    public void TheSeenReceiptIsOnlyForASeenLate()
    {
        Assert.Equal(SpikeIt.SeenLateLine, SpikeIt.LateReceipt(seenTaking: true));
        Assert.Equal(SpikeIt.LateLine, SpikeIt.LateReceipt(seenTaking: false));
        Assert.NotEqual(SpikeIt.LateLine, SpikeIt.SeenLateLine);
    }

    /// <summary>
    /// THE SQUARED STACK IS TOLD ON ENTERING, ONCE: sitting on in the gallery after the take tells nothing; going
    /// out of it arms the line; coming back in tells it; nothing after that tells it again, in or out.
    ///
    /// <para><b>RED</b> by telling it from <see cref="SpikeIt.Stack.NotYet"/> while in the gallery (said at the
    /// table, the moment of the take — the Kosh law broken), and by <see cref="SpikeIt.Stack.Told"/> going back to
    /// <see cref="SpikeIt.Stack.YouWentOut"/> (told on every return).</para>
    /// </summary>
    [Fact]
    public void TheSquaredStackIsToldOnEnteringOnce()
    {
        Assert.Equal((SpikeIt.Stack.NotYet, false), SpikeIt.TheStackOnEntering(SpikeIt.Stack.NotYet, inTheGallery: true));
        Assert.Equal((SpikeIt.Stack.YouWentOut, false), SpikeIt.TheStackOnEntering(SpikeIt.Stack.NotYet, inTheGallery: false));
        Assert.Equal((SpikeIt.Stack.YouWentOut, false), SpikeIt.TheStackOnEntering(SpikeIt.Stack.YouWentOut, inTheGallery: false));
        Assert.Equal((SpikeIt.Stack.Told, true), SpikeIt.TheStackOnEntering(SpikeIt.Stack.YouWentOut, inTheGallery: true));
        Assert.Equal((SpikeIt.Stack.Told, false), SpikeIt.TheStackOnEntering(SpikeIt.Stack.Told, inTheGallery: true));
        Assert.Equal((SpikeIt.Stack.Told, false), SpikeIt.TheStackOnEntering(SpikeIt.Stack.Told, inTheGallery: false));
    }

    /// <summary>
    /// SEEN RIDES THE SPIKE'S ONE LINE, AND AN UNSEEN SPIKE'S LINE IS SLICE 2's TO THE BYTE: the two new keys are
    /// written only once a take was seen; they round-trip; and a line without them reads as unseen.
    ///
    /// <para><b>RED</b> by writing <c>watched</c>/<c>stack</c> unconditionally inside the spike's keys (every
    /// slice-2 contract's line moved), and by dropping the <c>"stack"</c> arm from <c>Read</c> (the squared line
    /// told again after a reload).</para>
    /// </summary>
    [Fact]
    public void SeenRidesTheSpikesLineAndAnUnseenLineIsSliceTwos()
    {
        var unseen = new CarryThePress.Passage(
            1, Landed: true, Walked: true, Tin: true, TurnedIn: 0.0, Spike: true, Pages: SpikeIt.Pages.Taken, Seen: true);
        Assert.Equal(
            "site=1;landed=1;walked=1;tin=1;in=0;printed=0;floor=0;spike=1;pages=1;seen=1;out=0;paid=0;gone=0",
            unseen.Write());
        Assert.DoesNotContain("watched", unseen.Write(), StringComparison.Ordinal);
        Assert.False(CarryThePress.Passage.Read(unseen.Write()).Watched);

        var seen = unseen with { Watched = true, Stack = SpikeIt.Stack.Told };
        Assert.EndsWith(";watched=1;stack=2", seen.Write(), StringComparison.Ordinal);
        Assert.Equal(seen, CarryThePress.Passage.Read(seen.Write()));
        Assert.Equal(seen with { Stack = SpikeIt.Stack.YouWentOut },
            CarryThePress.Passage.Read((seen with { Stack = SpikeIt.Stack.YouWentOut }).Write()));

        // A seen flag with no spike is nothing: the spike's keys (and so the seen keys) are not written at all.
        Assert.Equal(new CarryThePress.Passage(1).Write(), (new CarryThePress.Passage(1) with { Watched = true }).Write());
        Assert.Equal(CarryThePress.Passage.Read("site=1;spike=1;watched=1;stack=9"),
            new CarryThePress.Passage(1, Spike: true, Watched: true));
    }

    /// <summary>
    /// THE THREE LINES ARE IN THE SWEEP, AND NONE OF THEM NAMES ANYBODY: all three in <see cref="SpikeIt.AllProse"/>,
    /// distinct from every other line of the family, and not one names the captain, the coat's outfit or the
    /// client — the coat is a coat, the thumb is <i>somebody else's</i>.
    /// </summary>
    [Fact]
    public void TheThreeLinesAreInTheSweepAndNameNobody()
    {
        var prose = SpikeIt.AllProse().ToList();
        foreach (string line in new[] { SpikeIt.SeenTakeEntry, SpikeIt.SquaredLine, SpikeIt.SeenLateLine })
        {
            Assert.Single(prose, p => string.Equals(p, line, StringComparison.Ordinal));
            Assert.DoesNotContain("{", line, StringComparison.Ordinal);
            Assert.DoesNotContain("captain", line, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(TheTailBehindYou.Plate, line, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(CarryThePress.Plate, line, StringComparison.Ordinal);
        }

        Assert.DoesNotContain(SpikeIt.SeenTakeEntry, CarryThePress.AllProse());
    }
}
