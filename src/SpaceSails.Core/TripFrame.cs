namespace SpaceSails.Core;

/// <summary>
/// #926 · THE FRAME A TRIP IS REALLY IN. Which body a plotted journey should be READ in — the common
/// parent of where the ghost is at the node and where the plan is going.
///
/// <para>Owner (2026-08-17, playing the vector planner from #916): <i>"the real thrust amounts are
/// dependent on the coordinate origin. I had to remember to switch to Sun to get the ship to really start
/// moving from Earth towards Mars."</i></para>
///
/// <para>OWNER RULING (2026-08-17), option A: the planner NAMES the frame it reads the plan in, and when
/// the plan has a destination it OFFERS the trip's frame in one press. It never switches by itself
/// (option B was rejected) — the captain presses, or does not.</para>
///
/// <h3>The law, in one line</h3>
/// <para>THE TRIP'S FRAME IS THE COMMON PARENT OF BOTH ENDS. Earth → Mars: both ends go round the Sun, so
/// the trip is a heliocentric one and a 4 km/s burn read in Earth's frame looks like almost nothing —
/// which is exactly the trap the owner fell into. Earth → Luna: both ends go round EARTH, so that trip is
/// read in Earth's frame and the Sun's 30 km/s would drown it. Europa → Ganymede: both ends go round
/// Jupiter. In every case the answer is the deepest body that is an ancestor-or-self of BOTH ends, and
/// when that body is the root of the hierarchy the answer is <c>null</c> — the client's Sun / inertial
/// frame, the null <c>_plotFrameBodyId</c>.</para>
///
/// <h3>Why "ancestor-or-SELF"</h3>
/// <para>Because one end is often the other end's parent. Sitting at Earth with Luna as the destination,
/// the strict common ancestor of {earth, luna} would be the Sun — and reading a lunar transfer in the
/// Sun's frame is the very mistake this issue exists to stop. Earth is an ancestor of Luna and it is
/// itself, so it is the deepest body both chains contain, and it is the right answer.</para>
///
/// <h3>Nothing here moves a burn</h3>
/// <para>This module decides what the captain READS the plan in. The quick selects keep aiming in the
/// NODE's frame (#916) — an escape burn IS Earth-prograde whichever origin the map is drawn about — so
/// pressing the offer changes the ribbon and the numbers and never the plan.</para>
/// </summary>
public static class TripFrame
{
    /// <summary>
    /// THE ONE FUNCTION THE PLANNER CALLS. The body whose frame this trip should be read in, at the
    /// scrubbed node: the common parent of the ghost's own primary at that instant and the destination.
    /// <c>null</c> is the root / Sun / inertial frame — the same null the client's plot frame uses.
    /// </summary>
    /// <param name="ghostPositionAtNode">Where the projected ship is at the node's epoch.</param>
    /// <param name="destinationBodyId">The body the plan is FOR. The caller gates on having one.</param>
    public static string? Of(
        Vector2d ghostPositionAtNode, string destinationBodyId, ICelestialEphemeris ephemeris, double simTime) =>
        CommonParent(PrimaryAt(ghostPositionAtNode, ephemeris, simTime), destinationBodyId, ephemeris);

    /// <summary>An offer worth making: the frame this trip should be read in. <c>TripFrameBodyId</c> null
    /// is the root / Sun / inertial frame — the same null the plot frame uses.</summary>
    public readonly record struct FrameOffer(string? TripFrameBodyId);

    /// <summary>
    /// WHETHER THE PANEL SAYS ANYTHING AT ALL, and which frame it names. Returns <c>null</c> — no offer —
    /// when there is no destination to have a trip to, and again when the plan is ALREADY being read in
    /// the frame the trip is in. Otherwise the frame to offer.
    ///
    /// <para>The whole decision lives here rather than in the panel so it can be flown: an offer that
    /// appears with no destination, or that keeps appearing after the captain has pressed it, are both
    /// bugs a source-shaped guard would never see.</para>
    /// </summary>
    /// <param name="readingFrameBodyId">The frame the plan is being read in now (null = Sun/inertial).</param>
    public static FrameOffer? At(
        Vector2d ghostPositionAtNode,
        string? destinationBodyId,
        string? readingFrameBodyId,
        ICelestialEphemeris ephemeris,
        double simTime)
    {
        if (destinationBodyId is null)
        {
            return null;   // no destination — there is no trip to have a frame
        }

        string? trip = Of(ghostPositionAtNode, destinationBodyId, ephemeris, simTime);
        return trip == readingFrameBodyId ? null : new FrameOffer(trip);
    }

    /// <summary>
    /// The innermost body whose Hill sphere holds a point — the body that end of the trip "goes round".
    /// Stations and parentless bodies are never primaries (a station has no mass and so no Hill sphere at
    /// all); <c>null</c> means nothing holds the point, which reads as the root.
    ///
    /// <para>This is the fallback law #916's planner already flew, lifted here so the trip frame and the
    /// radial "up/down" are decided by ONE reading of where the ghost actually is.</para>
    /// </summary>
    public static string? PrimaryAt(Vector2d position, ICelestialEphemeris ephemeris, double simTime)
    {
        string? best = null;
        double bestHill = double.MaxValue;
        foreach (CelestialBody body in ephemeris.Bodies)
        {
            if (body.ParentId is null || body.Kind == BodyKind.Station)
            {
                continue;
            }
            CelestialBody? parent = ephemeris.Bodies.FirstOrDefault(b => b.Id == body.ParentId);
            if (parent is null)
            {
                continue;
            }
            double hill = OrbitRule.HillRadius(body, parent.Mu);
            if (hill <= 0 || hill >= bestHill)
            {
                continue;
            }
            if ((position - ephemeris.Position(body.Id, simTime)).Length < hill)
            {
                (bestHill, best) = (hill, body.Id);
            }
        }
        return best;
    }

    /// <summary>
    /// THE COMMON PARENT ITSELF, over body ids — the deepest body that appears in BOTH chains
    /// (<see cref="Chain"/>, which starts at the body itself). <c>null</c> when that body is the root of
    /// the hierarchy, when either end is unknown, or when the two ends share no ancestor at all.
    /// </summary>
    public static string? CommonParent(string? originBodyId, string? destinationBodyId, ICelestialEphemeris ephemeris)
    {
        if (originBodyId is null || destinationBodyId is null)
        {
            return null;
        }

        IReadOnlyList<string> origin = Chain(originBodyId, ephemeris);
        var destination = new HashSet<string>(Chain(destinationBodyId, ephemeris), StringComparer.Ordinal);

        // The origin chain runs deepest-first, so the FIRST hit is the deepest shared body.
        foreach (string id in origin)
        {
            if (!destination.Contains(id))
            {
                continue;
            }
            CelestialBody? body = ephemeris.Bodies.FirstOrDefault(b => b.Id == id);
            return body?.ParentId is null ? null : id;   // the root reads as the inertial frame
        }
        return null;
    }

    /// <summary>A body and every parent above it, deepest first, ending at the root. An unknown id yields
    /// an empty chain. Cycle-safe: a body already seen ends the walk.</summary>
    public static IReadOnlyList<string> Chain(string? bodyId, ICelestialEphemeris ephemeris)
    {
        var chain = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        string? at = bodyId;
        while (at is not null && seen.Add(at))
        {
            CelestialBody? body = ephemeris.Bodies.FirstOrDefault(b => b.Id == at);
            if (body is null)
            {
                break;
            }
            chain.Add(body.Id);
            at = body.ParentId;
        }
        return chain;
    }

    // ---- What the panel says ---------------------------------------------------------------------

    /// <summary>The offer, in the owner-blessed words: the frame the plan is being READ in, and the frame
    /// the trip is actually in. <c>"You are reading this plan in EARTH's frame — the trip to MARS is in
    /// the SUN's."</c></summary>
    public static string OfferLine(string readingFrameName, string destinationName, string tripFrameName) =>
        $"You are reading this plan in {Possessive(readingFrameName)} frame — "
        + $"the trip to {Shout(destinationName)} is in {Possessive(tripFrameName)}.";

    /// <summary>The one press: <c>"Read it in the Sun's frame"</c>.</summary>
    public static string OfferButton(string tripFrameName) => $"Read it in {WithArticle(tripFrameName)}'s frame";

    /// <summary>What the button promises, and — just as important — what it does NOT touch.</summary>
    public static string OfferButtonHint(string tripFrameName) =>
        $"Draw the map, the ribbon and the numbers about {WithArticle(tripFrameName)} — the burn's aim does not move";

    /// <summary>The line that is ALWAYS on the panel, offer or no offer: whose frame the plan is being
    /// read in. <c>"reading in EARTH's frame"</c>.</summary>
    public static string ReadingNote(string readingFrameName) => $"reading in {Possessive(readingFrameName)} frame";

    /// <summary>The Sun takes its article; a planet or a moon does not, and a name that already carries
    /// one ("The Deep") is left alone.</summary>
    public static string WithArticle(string bodyName) =>
        bodyName.StartsWith("the ", StringComparison.OrdinalIgnoreCase) ? bodyName
        : bodyName.Equals("Sun", StringComparison.OrdinalIgnoreCase) ? "the " + bodyName
        : bodyName;

    /// <summary>A body's name the way the offer shouts it — caps, with the article (if it needs one) left
    /// in lower case, and a possessive 's: <c>"EARTH's"</c>, <c>"the SUN's"</c>.</summary>
    public static string Possessive(string bodyName)
    {
        string shouted = Shout(bodyName);
        return WithArticle(bodyName) == bodyName ? $"{shouted}'s" : $"the {shouted}'s";
    }

    private static string Shout(string bodyName) => bodyName.ToUpperInvariant();
}
