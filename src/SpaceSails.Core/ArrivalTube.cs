namespace SpaceSails.Core;

/// <summary>
/// #541 · THE DOCKING TUBE IS THE ESTABLISHING SHOT. Owner, photographing a long glazed gangway while boarding at
/// Tallinn — a queue stretching to a vanishing point, the same advertising banner repeating down its whole length,
/// a moving walkway: <i>"This kind of tunnel when docking the ship could be used in gen-ai to tell how big the
/// docked place is."</i>
///
/// <para>Every berth in this game currently arrives the same way: a state change and a card. But the tube is the
/// establishing shot — its length, its traffic and how much money has been spent on its walls say what kind of
/// place has just accepted you, before a single line of UI does. It is the arrival version of the breadcrumb
/// discipline (#533): the player <b>reads</b> the place's importance instead of being told it.</para>
///
/// <para><b>The tier is DERIVED, never authored.</b> The scenario already carries a weighted traffic model with a
/// <c>publishesTimetable</c> flag on every route, and that flag is a far better answer than any new field: a place
/// served only by discreet haulers has no scheduled foot traffic, so it has no gangway to walk through. So the
/// tube reads the economy that is already written down — and, pleasingly, it comes out saying exactly what the
/// owner's own worldbuilding note in that file says: <i>"the busiest lanes … are the scoop worlds feeding
/// Jupiter's energy-hungry habitat mega-works at Europa, not commutes from Earth. Earth is one node among
/// several."</i> Ringside and the Red Eye get the grand gangways. Selene Gate does not.</para>
///
/// <para><b>And the tube is a rule, not only a picture.</b> It is how far you walk with contraband before anybody
/// can inspect you: a great port gives an inspection team a long corridor, cameras and a queue you cannot leave;
/// an outpost has no tube, no queue and nobody to run it. Which makes <i>where you sell</i> a smuggling decision
/// (#537, #538) rather than a price lookup.</para>
/// </summary>
public static class ArrivalTube
{
    /// <summary>How big the place that just accepted you is — three tiers, readable at a glance.</summary>
    public enum Tier
    {
        /// <summary>A long glazed gangway, a moving walkway, branding repeated down its length, foot traffic both
        /// ways. You are nobody here, and this place processes people for a living.</summary>
        GreatPort,

        /// <summary>One short rigid tube, bare ribbed walls, a hand-painted berth number. Somebody works here;
        /// nobody is selling you anything.</summary>
        WorkingBerth,

        /// <summary>No tube at all — a soft collar clamped straight to your own lock. They were not expecting a
        /// ship, and there is no <i>they</i> to speak of.</summary>
        Outpost,
    }

    /// <summary>
    /// Scheduled tonnage at or above which a berth has earned a proper gangway. FLAGGED for the owner's tuning —
    /// and worth knowing what it currently decides: at 12 the great ports are <b>Ringside Exchange</b> (23) and
    /// <b>The Red Eye</b> (19), while Selene Gate (10) and the Rusty Roadstead (10) come out as working berths.
    /// Raising it past 19 leaves Saturn alone at the top; dropping it to 10 puts Earth and Mars back in the club
    /// and makes the set read Earth-centred again.
    /// </summary>
    public const double GreatPortTonnage = 12.0;

    /// <summary>
    /// The tonnage that shows up on a departures board, summed over the haven's whole neighbourhood: itself, its
    /// parent, and everything else under that parent. A station in Jovian space is busy because <i>Jovian space</i>
    /// is busy — the mega-works at Europa are the reason anyone is at Jupiter at all, and a rule that only counted
    /// routes naming the station itself would have called the Red Eye a backwater.
    /// </summary>
    public static double ScheduledTonnage(ICelestialEphemeris ephemeris, string havenId) =>
        Tonnage(ephemeris, havenId, scheduled: true);

    /// <summary>…and the tonnage that does not: the discreet haulers out of pirate country. It buys a berth its
    /// living and buys it no gangway, because nothing about it is on a board anybody queues for.</summary>
    public static double DiscreetTonnage(ICelestialEphemeris ephemeris, string havenId) =>
        Tonnage(ephemeris, havenId, scheduled: false);

    private static double Tonnage(ICelestialEphemeris ephemeris, string havenId, bool scheduled)
    {
        ArgumentNullException.ThrowIfNull(ephemeris);

        if (ephemeris.Traffic is not { Routes.Count: > 0 } traffic)
        {
            return 0;   // a scenario with no traffic section says nothing about anybody's importance
        }

        HashSet<string> neighbourhood = Neighbourhood(ephemeris, havenId);
        double total = 0;

        foreach (Contracts.RouteDefinition route in traffic.Routes)
        {
            if (route.PublishesTimetable != scheduled)
            {
                continue;
            }

            // Either end counts: a berth is busy because things arrive as well as leave.
            if (neighbourhood.Contains(route.From) || neighbourhood.Contains(route.To))
            {
                total += route.Weight;
            }
        }

        return total;
    }

    /// <summary>The haven, its parent, and its parent's other children — "the system this berth is in".</summary>
    private static HashSet<string> Neighbourhood(ICelestialEphemeris ephemeris, string havenId)
    {
        HashSet<string> at = new(StringComparer.Ordinal) { havenId };

        CelestialBody? haven = ephemeris.Bodies.FirstOrDefault(b => b.Id == havenId);
        if (haven?.ParentId is not { } parent)
        {
            return at;
        }

        at.Add(parent);
        foreach (CelestialBody sibling in ephemeris.Bodies.Where(b => b.ParentId == parent))
        {
            at.Add(sibling.Id);
        }

        return at;
    }

    /// <summary>Which tube a berth has earned. Scheduled traffic decides it, because a gangway is built for people
    /// who arrive on a timetable — the discreet tonnage buys a berth its living, not its architecture.</summary>
    public static Tier TierFor(ICelestialEphemeris ephemeris, string havenId)
    {
        double scheduled = ScheduledTonnage(ephemeris, havenId);
        return scheduled >= GreatPortTonnage ? Tier.GreatPort
             : scheduled > 0 ? Tier.WorkingBerth
             : Tier.Outpost;
    }

    // ── What the arrival shows and says ───────────────────────────────────────────────────────────────

    /// <summary>The canvas for each tier. Specced in <c>docs/art-manifest-moments.md</c>; a tier whose JPG is not
    /// painted yet still arrives — the plate's title and caption carry it and the image hides itself.</summary>
    public static string ArtFile(Tier tier) => tier switch
    {
        Tier.GreatPort => "art/tube-great-port.jpg",
        Tier.WorkingBerth => "art/tube-working-berth.jpg",
        _ => "art/tube-outpost.jpg",
    };

    /// <summary>The plate's heading. Never the station's name — the name is already on every other panel; what
    /// this is for is the SIZE of the place.</summary>
    public static string Title(Tier tier) => tier switch
    {
        Tier.GreatPort => "🛬 THE LONG WALK IN",
        Tier.WorkingBerth => "🛬 ONE TUBE, NO CEREMONY",
        _ => "🛬 COLLAR TO COLLAR",
    };

    /// <summary>
    /// And what it says, once, on the way in. Each one is a fact about the place rather than a compliment or a
    /// warning: how far you walk, who is walking with you, and who is watching you do it.
    /// </summary>
    public static string Caption(Tier tier) => tier switch
    {
        Tier.GreatPort =>
            "The gangway runs out ahead of you further than you can see the end of it, and the same advertisement " +
            "hangs every twenty metres of it — somebody costed the footfall through here and decided it was worth " +
            "buying. Two streams of people, a walkway to stand on if you are in a hurry, and nobody who has any " +
            "idea who you are.",
        Tier.WorkingBerth =>
            "One rigid tube, ribbed and bare, with the berth number brushed on the frame by hand and painted over " +
            "the last one. Cargo netting stacked at the far end and two dock workers waiting for you to get out of " +
            "the way. Nobody here is selling you anything, which is its own kind of welcome.",
        _ =>
            "No tube. Their collar comes across and mates to your own lock, frost blooming on the seal, and through " +
            "the gap you can see the hull you are joined to. One of them is standing there with a hand lamp. They " +
            "were not expecting a ship, and there is nobody else here to expect one either.",
    };

    /// <summary>
    /// THE PART THAT IS A RULE. How far you carry something you should not be carrying before anybody can ask you
    /// about it — the seam the smuggling and inspection lane (#537, #538) hangs off. Said on the plate so the
    /// consequence is legible on arrival rather than discovered at a checkpoint.
    /// </summary>
    public static string WalkLine(Tier tier) => tier switch
    {
        Tier.GreatPort =>
            "Anything in your pockets is in them for the whole of that walk, in a queue you cannot step out of.",
        Tier.WorkingBerth =>
            "Six metres of tube and nobody standing in it. What you carry is your own business for exactly that long.",
        _ => "You step from your lock into theirs. There is no walk, and there is no one to run it.",
    };

    /// <summary>How long the plate sits. Scene-setting, never a decision, and it must never hold up a docking —
    /// so it is a PLATE at the same length the story-beat seam uses.</summary>
    public static double PlateSeconds => StoryBeats.PlateSeconds;

    /// <summary>
    /// Which story beat an arrival raises. The tube goes through the ONE door every other moment goes through
    /// (#528) rather than inventing a second pop-up mechanism — and it is what taught that seam
    /// <see cref="StoryBeats.Cadence.OncePerSubject"/>: every berth's establishing shot fires once, for that
    /// berth, and never again.
    /// </summary>
    public static StoryBeats.Beat BeatFor(Tier tier) => tier switch
    {
        Tier.GreatPort => StoryBeats.Beat.BerthGreatPort,
        Tier.WorkingBerth => StoryBeats.Beat.BerthWorkingBerth,
        _ => StoryBeats.Beat.BerthOutpost,
    };
}
