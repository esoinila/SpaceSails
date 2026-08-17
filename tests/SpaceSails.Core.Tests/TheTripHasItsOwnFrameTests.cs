using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #926 · THE TRIP'S FRAME. Owner (2026-08-17, playing #916's vector planner): <i>"the real thrust
/// amounts are dependent on the coordinate origin. I had to remember to switch to Sun to get the ship to
/// really start moving from Earth towards Mars."</i>
///
/// <para>Ruling A: the planner NAMES the frame it reads a plan in and OFFERS the trip's frame in one
/// press. The trip's frame is the common parent of both ends — and that law is what these guards fly,
/// over the REAL <c>scenarios/sol.json</c> rather than a fixture, because the whole claim is about the
/// body hierarchy the game actually ships.</para>
///
/// <h3>The green test this could have been</h3>
/// <para>"Every answer is a body in the scenario" passes for a function that returns the origin, the
/// destination, or the Sun every time. So the sweep asserts the one property that separates a common
/// parent from any of those: <b>the answer is an ancestor-or-self of BOTH ends</b> — and LAW TWO runs an
/// impostor (return the origin) through the same property and proves it goes red, in this file, so
/// nobody has to take the RED PROOF prose on faith.</para>
/// </summary>
public sealed class TheTripHasItsOwnFrameTests
{
    /// <summary>The shipped solar system — the one the owner was flying when he hit this.</summary>
    private static readonly CircularOrbitEphemeris Sol =
        CircularOrbitEphemeris.FromScenario(ScenarioLoader.LoadFile(
            Path.Combine(AppContext.BaseDirectory, "scenarios", "sol.json")));

    /// <summary>The root of the hierarchy — the body <c>TripFrame</c> reports as <c>null</c>, because a
    /// null plot frame IS the inertial frame the root sits still in.</summary>
    private static string RootId => Sol.Bodies.Single(b => b.ParentId is null).Id;

    /// <summary>An answer as a body id, with the null "Sun / inertial" answer named.</summary>
    private static string Named(string? frameId) => frameId ?? RootId;

    // ── GUARD (a) · THE COMMON PARENT, OVER EVERY PAIR THE SCENARIO CAN MAKE ────────────────────────

    /// <summary>
    /// LAW ONE — the three cases the owner named, in the shipped world.
    ///
    /// <para>Earth → Mars is the SUN's (the null frame): both ends go round the Sun, and it is the trip
    /// that looked like nothing in Earth's frame. Earth → Luna is EARTH's: the Sun's 30 km/s would drown
    /// it. Europa → Ganymede is JUPITER's — a moon to its planet's other moon.</para>
    ///
    /// <para>RED PROOF: return the origin from <c>CommonParent</c> and the first two fail (earth, not
    /// null; luna, not earth). Return the destination and all three fail. Return the root always and the
    /// last two fail.</para>
    /// </summary>
    [Fact]
    public void TheThreeTripsTheOwnerNamed_ReadInTheirCommonParentsFrame()
    {
        Assert.Null(TripFrame.CommonParent("earth", "mars", Sol));          // the Sun / inertial frame
        Assert.Equal("earth", TripFrame.CommonParent("earth", "luna", Sol));
        Assert.Equal("earth", TripFrame.CommonParent("luna", "earth", Sol));   // and it does not care which end
        Assert.Equal("jupiter", TripFrame.CommonParent("europa", "ganymede", Sol));
    }

    /// <summary>
    /// LAW TWO — and over EVERY ordered pair of bodies in the scenario, the answer is an ancestor-or-self
    /// of both ends. That is the property a common parent has and an impostor does not.
    ///
    /// <para>Anti-vacuous twice over: the sweep asserts it saw a real number of pairs, and that it got at
    /// least three DISTINCT answers — a function that returned the Sun for everything would satisfy the
    /// ancestor law perfectly and be useless, and this is exactly the failure that read the Saturn moon
    /// tour heliocentric.</para>
    ///
    /// <para>RED PROOF, run here: the same sweep with the answer replaced by the ORIGIN is asserted to
    /// break the ancestor law. If the impostor ever passes, this guard is not testing what it says.</para>
    /// </summary>
    [Fact]
    public void EveryPairInTheScenario_LandsOnAnAncestorOfBothEnds()
    {
        List<string> ids = Sol.Bodies.Select(b => b.Id).ToList();
        Assert.True(ids.Count >= 20, $"sol.json has only {ids.Count} bodies — this sweep is reading the wrong world.");

        var answers = new HashSet<string>(StringComparer.Ordinal);
        var wrong = new List<string>();
        int pairs = 0, impostorBreaks = 0;

        foreach (string origin in ids)
        {
            foreach (string destination in ids)
            {
                if (origin == destination)
                {
                    continue;   // a "trip" to where you already are has no two ends to share a parent
                }
                pairs++;

                string answer = Named(TripFrame.CommonParent(origin, destination, Sol));
                answers.Add(answer);
                if (!IsAncestorOrSelf(answer, origin) || !IsAncestorOrSelf(answer, destination))
                {
                    wrong.Add($"  {origin} → {destination} answered {answer}");
                }

                // The impostor: "the trip is in the origin's frame" — the very mistake the owner made by
                // hand. It must break the same law, or the law above is not doing any work.
                if (!IsAncestorOrSelf(origin, destination))
                {
                    impostorBreaks++;
                }
            }
        }

        Assert.True(pairs >= 700, $"the sweep only saw {pairs} pairs — it is not sweeping the scenario.");
        Assert.True(wrong.Count == 0,
            "TripFrame.CommonParent returned a body that is not an ancestor-or-self of both ends (#926):"
            + Environment.NewLine + string.Join(Environment.NewLine, wrong));
        Assert.True(answers.Count >= 3,
            $"the sweep saw only {answers.Count} distinct answer(s) ({string.Join(", ", answers)}) — a "
            + "frame law that answers the same body for every trip is the heliocentric-moon-tour bug "
            + "wearing a different hat.");
        Assert.True(impostorBreaks > 0,
            "the RED demonstration found no pair the origin-returning impostor gets wrong, so the "
            + "ancestor law above cannot be failing anything either.");
    }

    private static bool IsAncestorOrSelf(string candidate, string bodyId) =>
        TripFrame.Chain(bodyId, Sol).Contains(candidate, StringComparer.Ordinal);

    /// <summary>
    /// LAW THREE — and the same answer arrives from a POSITION, which is how the planner asks it: the
    /// ghost at the node is placed inside Earth's Hill sphere, and the trip's frame is the Sun's for Mars
    /// and Earth's for Luna. This is the half that exercises <see cref="TripFrame.PrimaryAt"/> — the
    /// innermost Hill sphere holding the ghost, the same reading #916's up/down uses.
    ///
    /// <para>RED PROOF: have <c>PrimaryAt</c> answer the root — "the ghost orbits the Sun" — instead of
    /// the innermost Hill sphere actually holding it, and the Luna case answers null: a lunar transfer
    /// read heliocentric, which is the Saturn-moon-tour bug from the other end.</para>
    /// </summary>
    [Fact]
    public void FromAGhostSittingAtEarth_TheTripsFrameIsTheSunsForMarsAndEarthsForLuna()
    {
        const double t = 12 * 3600;
        Vector2d earth = Sol.Position("earth", t);
        Vector2d luna = Sol.Position("luna", t);
        Vector2d ghost = earth + (luna - earth) * 0.25;   // a quarter of the way to the Moon: deep in Earth's Hill sphere

        Assert.Equal("earth", TripFrame.PrimaryAt(ghost, Sol, t));
        Assert.Null(TripFrame.Of(ghost, "mars", Sol, t));
        Assert.Equal("earth", TripFrame.Of(ghost, "luna", Sol, t));
    }

    // ── GUARD (b) · THE OFFER STANDS ONLY WHEN IT HAS SOMETHING TO SAY ──────────────────────────────

    /// <summary>
    /// LAW FOUR — the offer appears exactly when there is a destination AND the frame being read is not
    /// the trip's. Both halves are asserted, both ways round.
    ///
    /// <para>RED PROOF (shown with no destination): drop the destination gate and the first assertion
    /// fails — a ship with no orders would be told which frame its non-existent trip is in. RED PROOF
    /// (hidden when frames differ): return null whenever the reading frame is a real body and the third
    /// assertion fails — the owner would be back to remembering the switch by hand. RED PROOF (still
    /// shown after the press): compare the wrong pair and the last assertion fails, and the line would sit
    /// there forever accusing the captain of a frame he is already in.</para>
    /// </summary>
    [Fact]
    public void TheOffer_StandsOnlyWithADestinationAndOnlyWhileTheFramesDiffer()
    {
        const double t = 12 * 3600;
        Vector2d earth = Sol.Position("earth", t);
        Vector2d ghost = earth + (Sol.Position("luna", t) - earth) * 0.25;

        // No destination — nothing to offer, whatever frame is being read.
        Assert.Null(TripFrame.At(ghost, null, "earth", Sol, t));
        Assert.Null(TripFrame.At(ghost, null, null, Sol, t));

        // Reading Earth-centric with Mars as the destination: the trip is the Sun's, so it is offered.
        TripFrame.FrameOffer? offer = TripFrame.At(ghost, "mars", "earth", Sol, t);
        Assert.True(offer.HasValue, "no offer while reading a Mars trip in Earth's frame — the owner's own bug.");
        Assert.Null(offer!.Value.TripFrameBodyId);   // the Sun / inertial frame

        // Press it — now the plan IS read in the trip's frame, and the offer stands down.
        Assert.Null(TripFrame.At(ghost, "mars", null, Sol, t));

        // The mirror: heliocentric with Luna as the destination offers Earth's frame, and stands down once taken.
        TripFrame.FrameOffer? lunar = TripFrame.At(ghost, "luna", null, Sol, t);
        Assert.True(lunar.HasValue, "no offer while reading a lunar trip in the Sun's frame.");
        Assert.Equal("earth", lunar!.Value.TripFrameBodyId);
        Assert.Null(TripFrame.At(ghost, "luna", "earth", Sol, t));
    }

    // ── The words ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// LAW FIVE — the owner's own sentence, to the character. These two strings were written by hand and
    /// blessed by hand; a "tidy-up" that re-words them is a change to blessed copy, and this is what says
    /// so. Note the article: a planet is EARTH's, the star is <i>the</i> SUN's.
    ///
    /// <para>RED PROOF: drop the article rule (make Possessive unconditional) and the line reads "…is in
    /// SUN's." — this fails on the exact character. Uppercase the button's body name and it fails there.</para>
    /// </summary>
    [Fact]
    public void ThePanelSaysExactlyWhatTheOwnerBlessed()
    {
        Assert.Equal(
            "You are reading this plan in EARTH's frame — the trip to MARS is in the SUN's.",
            TripFrame.OfferLine("Earth", "Mars", "Sun"));
        Assert.Equal("Read it in the Sun's frame", TripFrame.OfferButton("Sun"));
        Assert.Equal("Read it in Earth's frame", TripFrame.OfferButton("Earth"));
        Assert.Equal("reading in EARTH's frame", TripFrame.ReadingNote("Earth"));
        Assert.Equal("reading in the SUN's frame", TripFrame.ReadingNote("Sun"));

        // A station whose name already carries its article does not get a second one.
        Assert.Equal("THE DEEP's", TripFrame.Possessive("The Deep"));
    }

    // ── The five-degree nudges (owner addition, 2026-08-17) ────────────────────────────────────────

    /// <summary>
    /// LAW SIX — two nudges up and one down leave the aim exactly one nudge from where it started, and the
    /// arithmetic wraps. Owner: <i>"Let's add the plus and minus buttons to the burn scrub angle … the
    /// vector rotation is good for flying with mouse alone, without inputting … like ±5 degrees."</i>
    ///
    /// <para>The starts are chosen to straddle the seam — 357°, 2°, 0°, 358.5° — because that is where a
    /// missing wrap hides. RED PROOF: drop <c>Wrap360</c> from <c>NodeFrame.Nudge</c> and nudging down
    /// from 2° lands on −3°; the range assertion fails immediately, and the round-trip assertion fails on
    /// the 357° start where two ups cross 360.</para>
    /// </summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(2.0)]
    [InlineData(178.0)]
    [InlineData(357.0)]
    [InlineData(358.5)]
    public void TwoNudgesUpAndOneDown_LeaveTheAimExactlyFiveDegreesOn(double start)
    {
        double after = NodeFrame.Nudge(NodeFrame.Nudge(NodeFrame.Nudge(start, +1), +1), -1);

        // Every intermediate value stays a legal heading — the wrap is not optional.
        Assert.InRange(NodeFrame.Nudge(start, -1), 0.0, 360.0 - 1e-9);
        Assert.InRange(NodeFrame.Nudge(start, +1), 0.0, 360.0 - 1e-9);
        Assert.InRange(after, 0.0, 360.0 - 1e-9);

        double expected = (start + NodeFrame.NudgeDegrees) % 360;
        Assert.Equal(expected, after, 9);

        // And a nudge down undoes a nudge up exactly, across the seam included.
        Assert.Equal(start % 360, NodeFrame.Nudge(NodeFrame.Nudge(start, +1), -1), 9);
    }

    /// <summary>
    /// LAW SEVEN — the two faces carry a DEGREE SIGN and no bare plus-or-minus glyph. #916 sent the
    /// reflex-flying ± idiom out of the planner and guards that it stays out; this control is an angle,
    /// not a factor, and its labels have to say so or that guard and this feature are in a fight.
    ///
    /// <para>RED PROOF: label the buttons "±5" or "+"/"−" alone and this fails — and #916's own
    /// <c>ThePlanner_OffersNoPlusMinusControl</c> would fail with it.</para>
    /// </summary>
    [Fact]
    public void TheNudgeFacesAreAnglesNotAPlusMinusToggle()
    {
        Assert.Equal("+5°", NodeFrame.NudgeLabel(+1));
        Assert.Equal("−5°", NodeFrame.NudgeLabel(-1));
        Assert.DoesNotContain("±", NodeFrame.NudgeLabel(+1) + NodeFrame.NudgeLabel(-1));
        Assert.Equal(5.0, NodeFrame.NudgeDegrees);
    }
}
