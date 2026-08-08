namespace SpaceSails.Core;

/// <summary>
/// #528 · THE GAME'S BEST INSTRUMENT, MADE A RULE. Owner: <i>"Let's add some cool gen-ai to places where we tell
/// the story in the game. I think it makes a big difference to have that pop-up style we used in the reever
/// vented room scenario. We have a lot of events that don't have that level of service yet."</i> And then the law
/// itself: <i>"The pattern something of importance to story telling happens we get image pop up should happen
/// universally in the game as long as it does not block the playing too much or be too repetitive."</i>
///
/// <para>Which is a rule about a SYSTEM, not a list of seven cards — so this is the seam every moment registers
/// with, and the two constraints in his sentence are the two things it enforces.</para>
///
/// <para><b>"Not too repetitive."</b> Every beat declares a <see cref="Cadence"/>. The first round you ever fire
/// is a once-in-a-captain's-life card; a sail going is worth showing but not four times in one fight; a
/// collector's grapples are rare enough to be worth every time. A card that fires whenever its condition is true
/// is how a game teaches players to dismiss cards without reading them, and that ruins the instrument for the
/// moments that deserve it.</para>
///
/// <para><b>"Does not block the playing too much."</b> Every beat declares a <see cref="Presentation"/>. The
/// vented-room card is a MODAL and earns it — the captain walked into a room to look at something. A hit landing
/// mid-fight must never steal the keyboard, so it gets a PLATE: the same art and the same caption, at the edge,
/// for a few seconds, over a game that never stopped. And a modal that comes due while something is trying to
/// kill you WAITS (<see cref="DeferrableWhileInDanger"/>) — the wreck lane learned that the expensive way, when
/// a full-screen tutorial card let a pack of Reevers kill the captain behind it.</para>
///
/// <para>Art files are named here so <c>docs/art-manifest-moments.md</c> and the code cannot drift apart, and
/// every card degrades honestly: a beat whose JPG has not been painted yet still fires, with its title and its
/// caption, and no broken image.</para>
/// </summary>
public static class StoryBeats
{
    /// <summary>A moment worth showing the player a picture of.</summary>
    public enum Beat
    {
        /// <summary>The first round this captain ever fires. A smuggler becomes a pirate exactly once.</summary>
        FirstShotFired,

        /// <summary>A hit that takes a sail and leaves a ship adrift — the consequence, not the explosion.</summary>
        SailHoled,

        /// <summary>Grapples across the frame: a collector has you.</summary>
        CollectorHail,

        /// <summary>The crew send a deputation. <i>"This is the last cheap moment."</i></summary>
        CrewDeputation,

        /// <summary>The meeting you were not asked to, and the empty chair that is obviously yours.</summary>
        CrewMeeting,

        /// <summary>An arc beat breaks on the wire — the story arriving as news rather than as a quest line.</summary>
        ArcNewsBreaks,

        /// <summary>She sheds her charge: a discharge off the antennae, filaments raking outward.</summary>
        ChargeLetGo,

        /// <summary>There is fire in a hull you are standing in — and three ways to answer it (#524).</summary>
        FireAboard,

        /// <summary>#541 · The long walk in: a gangway at a place that processes people for a living.</summary>
        BerthGreatPort,

        /// <summary>#541 · One tube, no ceremony — somebody works here and nobody is selling you anything.</summary>
        BerthWorkingBerth,

        /// <summary>#541 · Collar to collar: no tube at all, and nobody who was expecting a ship.</summary>
        BerthOutpost,
    }

    /// <summary>How often a beat is allowed to speak.</summary>
    public enum Cadence
    {
        /// <summary>Once per captain, ever. The card IS the milestone.</summary>
        OnceEver,

        /// <summary>Once, then not again for <see cref="CooldownSeconds"/> — worth showing, not worth repeating.</summary>
        Cooled,

        /// <summary>Every time. Reserved for moments that are rare by their own nature.</summary>
        EveryTime,

        /// <summary>
        /// #541 · Once per SUBJECT, ever. The arrival tube taught this one: the first time a captain walks a great
        /// port's gangway is a moment, and so is the first time they walk a different one — but the second walk
        /// down the same tube is furniture. <c>OnceEver</c> would have shown one berth and silently swallowed
        /// every other place in the system; <c>EveryTime</c> would have made docking annoying.
        /// </summary>
        OncePerSubject,
    }

    /// <summary>How a beat reaches the player — the owner's "does not block the playing too much", as a type.</summary>
    public enum Presentation
    {
        /// <summary>A full card the player dismisses. Earned only when the moment is already a pause.</summary>
        Card,

        /// <summary>An art plate at the edge for a few seconds. Same picture, same words, no keyboard stolen and
        /// no world stopped.</summary>
        Plate,

        /// <summary>
        /// #777 · HOSTED — the beat's canvas is a card its own caller already raises, so the seam raises
        /// NOTHING and only keeps the books.
        ///
        /// <para>The hail is what this is for and it is not a special case, it is a shape. A collector's
        /// grapples arrive as the BUSTED demand panel, and that panel has rendered
        /// <see cref="ArtFile"/>(<see cref="Beat.CollectorHail"/>) at the top of itself since #528. Raising
        /// the beat the ordinary way would have put a second full-screen modal, showing the very same
        /// painting, on top of the first — <i>"stacking a card on a card is not service, it is noise"</i> —
        /// and this is the one beat that may not <see cref="DeferrableWhileInDanger">wait for a calmer
        /// moment</see>, because it IS the moment. So the beat had a picture and no cadence, no log line and
        /// no caller, and #663's scanner counted it as an orphan for exactly as long as the seam had only
        /// two answers.</para>
        ///
        /// <para>The third answer: the caller still knocks on the one door, the seam still applies the
        /// cadence, still files the seen-set and still writes the words into the log — and then stands
        /// aside, because the surface is already up. What the host owes in return is
        /// <see cref="HostCard">named here</see> and enforced by the client's own guards: the picture and
        /// the caption go in the host's subtree, where the player is already looking (#736, #761).</para>
        /// </summary>
        Hosted,
    }

    /// <summary>How often this beat may speak.</summary>
    public static Cadence CadenceOf(Beat beat) => beat switch
    {
        Beat.FirstShotFired => Cadence.OnceEver,
        Beat.CrewDeputation => Cadence.OnceEver,   // the FIRST deputation is the beat; later ones are the sheet's job
        Beat.SailHoled => Cadence.Cooled,
        Beat.ChargeLetGo => Cadence.Cooled,
        // #541: one gangway per berth. Each place gets its establishing shot exactly once.
        Beat.BerthGreatPort or Beat.BerthWorkingBerth or Beat.BerthOutpost => Cadence.OncePerSubject,
        _ => Cadence.EveryTime,                    // a collector's grapples, a crew meeting, an arc breaking
    };

    /// <summary>How long a cooled beat holds its tongue. Tuned so a beat cannot punctuate the same fight twice
    /// and cannot become the wallpaper of a long run. FLAGGED for the owner's tuning.</summary>
    public static double CooldownSeconds(Beat beat) => beat switch
    {
        Beat.SailHoled => 6 * 60.0,
        Beat.ChargeLetGo => 10 * 60.0,
        _ => 0.0,
    };

    /// <summary>Card, plate, or hosted. The rule of thumb: if the player was already standing still, they can
    /// have a modal; if something is moving toward them, they get a plate; and if the moment already HAS a
    /// card — the caller's own, showing this beat's painting — the beat is hosted and the seam shows
    /// nothing (#777).</summary>
    public static Presentation PresentationOf(Beat beat) => beat switch
    {
        Beat.FirstShotFired => Presentation.Plate,   // it happens mid-fight; it must not take the keyboard
        Beat.SailHoled => Presentation.Plate,
        Beat.ChargeLetGo => Presentation.Plate,
        // #541: scene-setting, never a decision — a docking must not wait for anybody to read anything.
        Beat.BerthGreatPort or Beat.BerthWorkingBerth or Beat.BerthOutpost => Presentation.Plate,
        // #777: the grapples arrive AS the BUSTED demand panel, which has been showing this beat's painting
        // since #528. A card here would be a second modal over the first, with the same picture on it.
        Beat.CollectorHail => Presentation.Hosted,
        _ => Presentation.Card,                      // the deputation, the meeting, the news
    };

    /// <summary>
    /// #777 · WHOSE CARD IS THE CANVAS. A <see cref="Presentation.Hosted"/> beat is only honest if some
    /// surface really does show it, so the host is named here in the same file as the cadence and the art —
    /// one place, and a beat that claims a host it does not have is a beat nobody can find.
    ///
    /// <para>Prose rather than a type on purpose: the host is a card in the client and Core does not know
    /// what a card is. The client's guards read this the way the art manifest reads
    /// <see cref="ArtFile"/> — as the sentence a human checks the markup against.</para>
    /// </summary>
    /// <returns>The host's name, or an empty string for a beat the seam raises itself.</returns>
    public static string HostCard(Beat beat) => beat switch
    {
        Beat.CollectorHail => "the BUSTED demand panel (Map.razor, BustedEncounter.Stage.Demand)",
        _ => "",
    };

    /// <summary>
    /// Whether a card may WAIT for a safer moment rather than landing now.
    ///
    /// <para>The expensive lesson this exists for: a full-screen tutorial card once let a pack of Reevers kill
    /// the captain behind it, because the world kept running under the modal. A story card must never be the
    /// reason somebody died — so a deferrable one queues until the scene is calm, and the moments that ARE the
    /// danger (a collector already has you) do not defer, because deferring them would be absurd.</para>
    /// </summary>
    /// <para>#777 · A HOSTED beat never defers either, and for a second reason on top of the first: there is
    /// nothing to hold. Its surface is a card the caller is raising right now, so "later" would mean showing
    /// the words after the picture they belong to has gone.</para>
    public static bool DeferrableWhileInDanger(Beat beat) => beat switch
    {
        Beat.CollectorHail => false,   // this IS the danger; it cannot wait for a better time
        _ => PresentationOf(beat) == Presentation.Card,
    };

    /// <summary>How long a plate stays up. Long enough to read the caption at a glance, short enough that it is
    /// gone before it becomes furniture.</summary>
    public const double PlateSeconds = 7.0;

    // ── What each one shows and says ──────────────────────────────────────────────────────────────────

    /// <summary>The painting for this beat. Named here so the manifest and the code cannot drift; a file that
    /// has not been painted yet simply does not render, and the words carry it.</summary>
    public static string ArtFile(Beat beat) => beat switch
    {
        Beat.FirstShotFired => "art/first-shot.jpg",
        Beat.SailHoled => "art/sail-holed.jpg",
        Beat.CollectorHail => "art/collector-hail.jpg",
        Beat.CrewDeputation => "art/crew-deputation.jpg",
        Beat.CrewMeeting => "art/crew-meeting.jpg",
        Beat.ArcNewsBreaks => "art/arc-news.jpg",
        Beat.ChargeLetGo => "art/charge-let-go.jpg",
        Beat.FireAboard => "art/fire-aboard.jpg",
        // #541: the tube's own canvases live with the tube, so the tier rule and the picture cannot disagree.
        Beat.BerthGreatPort => ArrivalTube.ArtFile(ArrivalTube.Tier.GreatPort),
        Beat.BerthWorkingBerth => ArrivalTube.ArtFile(ArrivalTube.Tier.WorkingBerth),
        Beat.BerthOutpost => ArrivalTube.ArtFile(ArrivalTube.Tier.Outpost),
        _ => "",
    };

    /// <summary>The title: it names the place and the verb, never the outcome. "WHAT THE VACUUM LEFT", not
    /// "salvage complete".</summary>
    public static string Title(Beat beat) => beat switch
    {
        Beat.FirstShotFired => "🔫 THE FIRST ROUND YOU EVER FIRED",
        Beat.SailHoled => "🎯 HER SAIL IS GONE",
        Beat.CollectorHail => "⛓ GRAPPLES",
        Beat.CrewDeputation => "🧑‍🔧 A DEPUTATION",
        Beat.CrewMeeting => "🕯 THE MEETING YOU WERE NOT ASKED TO",
        Beat.ArcNewsBreaks => "📰 THE STORY BREAKS",
        Beat.ChargeLetGo => "⚡ SHE LETS GO",
        Beat.FireAboard => "🔥 THERE IS FIRE IN HER",
        Beat.BerthGreatPort => ArrivalTube.Title(ArrivalTube.Tier.GreatPort),
        Beat.BerthWorkingBerth => ArrivalTube.Title(ArrivalTube.Tier.WorkingBerth),
        Beat.BerthOutpost => ArrivalTube.Title(ArrivalTube.Tier.Outpost),
        _ => "",
    };

    /// <summary>
    /// The caption: it describes what is there and STOPS. This is the hardest discipline in the whole idiom and
    /// the reason the vented-room card works — the gouges cross the deck toward a sealed hatch and stop there,
    /// and nobody tells you what that means.
    /// </summary>
    /// <param name="subject">A ship's name, a haven, a headline — whatever this instance is about. Optional; the
    /// lines are written to read whole without it.</param>
    public static string Caption(Beat beat, string? subject = null)
    {
        string it = string.IsNullOrWhiteSpace(subject) ? "her" : subject!;

        return beat switch
        {
            Beat.FirstShotFired =>
                "The breech is still warm and nobody on the gun deck is looking at the target — they are looking " +
                "at each other. Whatever you were before this watch, the log now says otherwise.",

            Beat.SailHoled =>
                $"{it} is intact everywhere that does not matter. The sail is blown out mid-span, silvered film " +
                "peeling away in the vacuum, and her windows are still lit from the inside.",

            Beat.CollectorHail =>
                $"Grapples come across the frame from somewhere you were not watching, and {it} fills the window " +
                "with running lights the colour of a docking clamp. Nobody aboard her is in a hurry.",

            Beat.CrewDeputation =>
                "Three of them in the corridor outside your door, hats in hands, one with his knuckles up and " +
                "not knocking yet. They have clearly agreed who is going to say it.",

            Beat.CrewMeeting =>
                "The cantina at an odd watch, lamps down, five of them round one table and a chair pulled out " +
                "that nobody is sitting in. Not one of them looks at the door — which is how you know they heard " +
                "you coming.",

            Beat.ArcNewsBreaks =>
                $"The concourse screen is mid-broadcast and the room has turned up to watch it. One figure walks " +
                $"away from the screen instead of toward it, because {it} is not news to them.",

            // Lab 43 corrected this line's last clause. A discharge is 85,514× DIMMER than her own reflected
            // sunlight — nobody watches it through a telescope. She is not brighter; she is LOUDER, and every
            // receiver in the volume gets that for free without pointing anything at her.
            Beat.ChargeLetGo =>
                "A blue-white core sits on the mast for a moment with filaments raking off it into the dark, and " +
                "then there is nothing on the hull at all. Nobody saw that. Everything with a receiver heard it.",

            // #541: the tube's words live with the tube. The subject is the berth's name, and every line reads
            // whole without it — the tier is what the plate is about.
            Beat.BerthGreatPort => ArrivalTube.Caption(ArrivalTube.Tier.GreatPort) + " " +
                                   ArrivalTube.WalkLine(ArrivalTube.Tier.GreatPort),
            Beat.BerthWorkingBerth => ArrivalTube.Caption(ArrivalTube.Tier.WorkingBerth) + " " +
                                      ArrivalTube.WalkLine(ArrivalTube.Tier.WorkingBerth),
            Beat.BerthOutpost => ArrivalTube.Caption(ArrivalTube.Tier.Outpost) + " " +
                                 ArrivalTube.WalkLine(ArrivalTube.Tier.Outpost),

            Beat.FireAboard =>
                "Forty years, and a pocket of her atmosphere was still shut in with something that would burn. " +
                "The light of it comes down the spine ahead of the heat, and every hatch anybody left open is a " +
                "road it already knows.",

            _ => "",
        };
    }
}
