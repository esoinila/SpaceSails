using System;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #242 · <b>"WHY AREN'T WE MOVING?"</b> Owner, post-undock: <i>"I was wondering why the ship don't move...
/// the origin was set to Mars... after setting to Sun we got going. We should have some tip about that in
/// burn planning."</i>
///
/// <para>The sim was right and the picture was lying by omission: in the Mars frame the map subtracts the
/// heliocentric motion the ship SHARES with Mars. So the fix is a sentence, and a sentence is exactly where
/// this repository's third named bug class lives — a line reporting one thing while the sim does another. The
/// guards below hold the two places that could go wrong: <b>when</b> it speaks (the owner's 15 %, read from
/// the one place it is written) and <b>what</b> it says (the body's own name, in the owner's own words).</para>
///
/// <para><b>The threshold is discovered, not typed.</b> <see cref="TheOwnersFifteenPercentIsWhereItSpeaks"/>
/// bisects the edge out of <see cref="FrameMotionTip.ShouldShow"/> and compares it against
/// <see cref="FrameMotionTip.QuietFraction"/> — so a razor that grew a threshold of its own, or a constant
/// that moved without the tip moving with it, is red here rather than merely wrong on the glass.</para>
/// </summary>
public class TheFrameIsEatingTheCruiseTests
{
    private const double Helio = 33_500;     // the owner's own cruise, m/s

    /// <summary>The frame speed at which the tip stops speaking, found by bisection rather than written down.</summary>
    private static double EdgeFraction()
    {
        double lo = 0, hi = Helio;           // silent at the top, speaking at the bottom
        for (int i = 0; i < 200; i++)
        {
            double mid = (lo + hi) / 2;
            if (mid <= lo || mid >= hi)
            {
                break;
            }
            if (FrameMotionTip.ShouldShow(planningABurn: true, underThrust: false, mid, Helio, "mars"))
            {
                lo = mid;
            }
            else
            {
                hi = mid;
            }
        }

        return lo / Helio;
    }

    /// <summary>The owner's "say &lt;15 %" is where the line appears, and the fraction it is judged by is the
    /// one <see cref="FrameMotionTip"/> holds — not a second copy that agrees with it today.</summary>
    [Fact]
    public void TheOwnersFifteenPercentIsWhereItSpeaks()
    {
        Assert.Equal(FrameMotionTip.QuietFraction, EdgeFraction(), 9);
        Assert.Equal(0.15, FrameMotionTip.QuietFraction, 9);   // …and that fraction is the number he gave

        Assert.True(FrameMotionTip.ShouldShow(true, false, Helio * 0.149, Helio, "mars"));
        Assert.False(FrameMotionTip.ShouldShow(true, false, Helio * 0.151, Helio, "mars"));
    }

    /// <summary>
    /// <b>It speaks only while the captain is doing something about motion.</b> Outside those two moments the
    /// arithmetic is true half the time — every ship parked at a berth reads zero in its host's frame — and a
    /// line that is on the glass half the time is furniture. Both halves of the pair are asserted, so this
    /// cannot pass on a tip that never appears.
    /// </summary>
    [Fact]
    public void PlanningOrThrustingIsTheMomentAndNothingElseIs()
    {
        double quiet = Helio * 0.05;

        Assert.True(FrameMotionTip.ShouldShow(planningABurn: true, underThrust: false, quiet, Helio, "mars"));
        Assert.True(FrameMotionTip.ShouldShow(planningABurn: false, underThrust: true, quiet, Helio, "mars"));
        Assert.False(FrameMotionTip.ShouldShow(planningABurn: false, underThrust: false, quiet, Helio, "mars"));
    }

    /// <summary>
    /// <b>The Sun frame never accuses itself.</b> A null frame body IS the Sun / inertial frame everywhere in
    /// this family, and motion "shared with the Sun" is the cruise itself — a tip offering to switch to the
    /// frame you are already reading would be the game talking nonsense at the captain.
    /// </summary>
    [Fact]
    public void TheSunFrameCannotBeHidingMotionYouShareWithTheSun()
    {
        Assert.False(FrameMotionTip.ShouldShow(true, true, 0, Helio, frameBodyId: null));
        Assert.True(FrameMotionTip.ShouldShow(true, true, 0, Helio, frameBodyId: "mars"));
    }

    /// <summary>
    /// <b>A ship that is genuinely stopped gets no tip.</b> Zero heliocentric speed means the frame is not
    /// hiding anything: it really is parked, and blaming the frame would be the game explaining a fact it
    /// invented.
    ///
    /// <para>This guard is why the rule is written with a STRICT comparison and not a
    /// <c>heliocentricSpeedMps &gt; 0</c> clause. The first cut had that clause, it read like the law, and
    /// it was dead code — both speeds are magnitudes, so at rest the test is already <c>0 &lt; 0</c> and the
    /// clause could never change an answer. Deleting it and re-proving this guard is what showed the
    /// difference: the rows below go red on <c>&lt;</c> becoming <c>&lt;=</c>, which is a mistake somebody
    /// could actually make, and went GREEN on the clause being removed, which is the mistake nobody could.</para>
    /// </summary>
    [Fact]
    public void AShipThatIsTrulyStoppedIsNotToldItIsTheFramesFault()
    {
        Assert.False(FrameMotionTip.FrameIsEatingTheMotion(0, 0));
        Assert.False(FrameMotionTip.ShouldShow(true, true, 0, 0, "mars"));

        // …and the very same ship, once it IS cruising, is told.
        Assert.True(FrameMotionTip.ShouldShow(true, true, 0, Helio, "mars"));
    }

    /// <summary>
    /// <b>The sentence is the owner's, with the body's name in it — twice.</b> Owner's wording, verbatim
    /// from the issue: <i>"Barely moving? That's the Mars frame — it hides motion you share with Mars. Sun
    /// frame shows the cruise."</i> Both substitutions are asserted, because a line that named the body once
    /// and left the second slot on a default would read perfectly and be wrong at Europa.
    /// </summary>
    [Fact]
    public void TheLineIsTheOwnersWordsWithTheBodyInBothSlots()
    {
        Assert.Equal(
            "Barely moving? That's the Mars frame — it hides motion you share with Mars. Sun frame shows the cruise.",
            FrameMotionTip.Line("Mars"));

        string europa = FrameMotionTip.Line("Europa");
        Assert.DoesNotContain("Mars", europa, StringComparison.Ordinal);
        Assert.Equal(2, europa.Split("Europa").Length - 1);
        Assert.EndsWith("Sun frame shows the cruise.", europa, StringComparison.Ordinal);
    }

    /// <summary>The standing explanation for the plotting card (#119's inventory idiom) is the owner's other
    /// sentence, verbatim, kept beside the contextual one so the two can never be reworded apart.</summary>
    [Fact]
    public void TheStandingExplanationIsTheOwnersOtherSentence()
    {
        Assert.Equal(
            "Frames hide shared motion — pick the frame of the thing you're steering RELATIVE to.",
            FrameMotionTip.StandingExplanation);
    }

    /// <summary>The two numbers the frame row already prints, said as the sum that makes the case — so the
    /// tip can show its working rather than ask to be believed.</summary>
    [Fact]
    public void TheReadoutShowsTheSumTheTipIsMadeOf()
    {
        Assert.Equal("1.7 of 33.5 km/s is showing", FrameMotionTip.SharedMotionReadout(1_700, Helio));
    }
}
