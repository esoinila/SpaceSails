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

        // ── #664 · THE ELEVEN THE OTHER SYSTEM WAS RAISING ──────────────────────────────────────────────
        //
        // The fork answered #528 twice, on the same day, from opposite ends. This branch built the CARD and
        // raised it by hand — ShowRevealCard(title, art, caption), no cadence, no deferral, always modal —
        // and `main` built this file. The reunification merge (#633) kept both and said so out loud; #664 is
        // where a winner is picked, and the winner is the one that can say NO.
        //
        // Each of these was already a moment somebody had written words and painted a canvas for, in Core,
        // beside the rule that decides it happened (ArchiveNode.PurgedPlate, KaamosLore.PlateFor, and so on).
        // What they gain here is the half the client-only card could never have: a CADENCE, so the owner's
        // "or be too repetitive" is answerable, and a DEFERRAL, so no card of theirs is ever the reason
        // somebody died behind it. What they do NOT gain is new text — see PlateOf, which is the whole of
        // where their words come from.

        /// <summary>#664 · The purge handle goes over, and a column of somebody's pattern stops being warm.</summary>
        ArchivePurged,

        /// <summary>#664 · The one warm card in a dread-heavy set: a stranger stands you the cognac.</summary>
        StrangerStandsADrink,

        /// <summary>#664 · A KAAMOS shard that turns the arc — the subject is the fragment id, and the arc's
        /// own pool decides which painting and which words (<c>KaamosLore.PlateFor</c>).</summary>
        KaamosShardFound,

        /// <summary>#664 · RETURNED TO SENDER — the ice-moon berth takes a filing and bounces it.</summary>
        KaamosFilingBounced,

        /// <summary>#664 · A NEBULA shard that arrives at a bare bar table (<c>NebulaLore.PlateFor</c>).</summary>
        NebulaShardFound,

        /// <summary>#664 · Somebody's last effects, on the floor of a sealed hut on an airless moon.</summary>
        OutpostEffectsRead,

        /// <summary>#664 · The detector shrieks and holds: a sealed door, buried flush with the regolith.</summary>
        SecretLabDoorFound,

        /// <summary>#664 · The Hive's loudest moment — the cradles nearest the door are open.</summary>
        TheDormantThingWakes,

        /// <summary>#664 · A shelter is a pressure vessel, not a sanctuary, and they have settled in to wait.</summary>
        ShelterIsNotSanctuary,

        /// <summary>#664 · A boat you did not call sets down between you and the way home.</summary>
        CollectorsSetDown,

        /// <summary>#664 · The sealed hatch comes off its dogs — and it opens both ways.</summary>
        SealedDoorReleased,

        /// <summary>#973 · A page you don't remember writing gives something back. The subject is the MEMORY
        /// ID — the ledger entry the captain just read at — because a flashback is always about one page and
        /// never about flashbacks in general.</summary>
        Flashback,

        /// <summary>#973 L5b · SHE COMES IN THROUGH THE DOOR. A woman crosses a classy room to a captain
        /// sitting alone and asks for something found. The subject is HER (<see cref="WalkIn.Subject"/>),
        /// because the cadence is once per subject and the subject of this moment is a person — two women is
        /// two moments; the same woman twice is not one.</summary>
        WalkIn,
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

        // ── #664 · the eleven, and why each one is the cadence it is ────────────────────────────────────
        //
        // ONCE EVER, because the picture, the caption and the arithmetic are byte-identical every time it
        // fires, so a second showing adds a dismissal and nothing else. The purge handle is the one
        // irreversible act in the game and the card is its milestone; the bounced filing is the first thing
        // the KAAMOS arc ever says to most captains; and the shelter plate is a RULE OF THE WORLD — its own
        // caller files the line at PulseRank.Beat and calls it "a rule of the world learned once".
        Beat.ArchivePurged or Beat.KaamosFilingBounced or Beat.ShelterIsNotSanctuary => Cadence.OnceEver,

        // COOLED, because the moment is real every time and the CARD is not. A stranger standing you a drink
        // is worth a picture the first time each evening and wallpaper by the third; a captain throwing three
        // sealed hatches in one hull in one minute has already been told what is behind them.
        Beat.StrangerStandsADrink or Beat.SealedDoorReleased => Cadence.Cooled,

        // ONCE PER SUBJECT — #541's cadence, and every one of these is about a PLACE or a THING rather than
        // about the captain. A second KAAMOS shard is a different painting and different words; a second
        // moon's buried door is a different moon. OnceEver would show one and silently swallow the rest,
        // which is the exact failure the arrival tube was written to stop.
        Beat.KaamosShardFound or Beat.NebulaShardFound or Beat.OutpostEffectsRead
            or Beat.SecretLabDoorFound or Beat.TheDormantThingWakes => Cadence.OncePerSubject,

        // #973 L5b · …and the walk-in with them, for the same clause and one more. It is about a PERSON, and
        // a person who has already crossed a room to ask you for something does not do it again — the ask is
        // the whole of the scene, and the second time it would be a job board with a face on it.
        Beat.WalkIn => Cadence.OncePerSubject,

        // #973 · …and Flashback falls through to EveryTime with them, for the clause EveryTime is reserved
        // for: it is rare by its OWN nature and cannot be made repetitive by trying. A page may be read at
        // ONCE PER LIFE and never again (FilingLine.PageState.Refused is the latch), and there are only grey
        // pages to read at all after a captain has died. OncePerSubject was the near miss and it is wrong for
        // the one reason that matters here: the rebirth RE-GREYS the book, so the same page read by a later
        // captain is a different captain reaching for a different stranger's afternoon, and swallowing that
        // would silently un-illustrate every flashback after the first death.

        // …and CollectorsSetDown falls through to EveryTime with the grapples, deliberately. It is the only
        // warning the player gets — after it the only information in the world is a tracker fan — and it is
        // rare by its own nature (a heat threshold, and at most one landing per excursion), which is the
        // clause EveryTime is reserved for. A warning suppressed for being repetitive is not a warning.
        _ => Cadence.EveryTime,                    // a collector's grapples, a crew meeting, an arc breaking
    };

    /// <summary>How long a cooled beat holds its tongue. Tuned so a beat cannot punctuate the same fight twice
    /// and cannot become the wallpaper of a long run. FLAGGED for the owner's tuning.</summary>
    public static double CooldownSeconds(Beat beat) => beat switch
    {
        Beat.SailHoled => 6 * 60.0,
        Beat.ChargeLetGo => 10 * 60.0,

        // #664 · A stranger's cognac is a whole bar visit apart; a sealed hatch is long enough that the three
        // doors of one sweep give one card and not three, and short enough that the NEXT hull is a fresh
        // fright. FLAGGED for the owner's tuning, like the two above.
        Beat.StrangerStandsADrink => 15 * 60.0,
        Beat.SealedDoorReleased => 5 * 60.0,

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

        // #973 L5b · HOSTED, and the host is HER OWN CARD. She is standing at the table with her portrait on
        // the screen and her two lines under it by the time this beat is raised; a card here would be the
        // same face twice on one screen, which is exactly what #777 named. The seam still spends the cadence,
        // files the seen-set and writes the words into the ledger — which is the whole of what a hosted beat
        // is for.
        Beat.WalkIn => Presentation.Hosted,

        // #973 · A PLATE, and deliberately not a card. The captain is at the Captain's desk with the ledger
        // open, clicking grey rows; a full-screen modal over that is a dismissal between every click, which
        // is the "too repetitive" half of the owner's law arriving through the back door. The plate rides the
        // edge for its seven seconds while the book stays open and readable underneath — and the page the
        // captain just won back is right there to be read, which is the whole reason they clicked.
        Beat.Flashback => Presentation.Plate,

        // #664 · All eleven of the adopted moments fall through to CARD, and that is not an oversight: every
        // one of them was already a full-screen modal under the other system, and every one of them is a
        // moment where the world has just stopped for the captain anyway — a handle pulled, a shard laid on
        // a table, a hatch coming off its dogs. What changes is not whether they take the screen; it is
        // WHEN, and how often.
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

        // #973 L5b · her card, raised on the frame she reaches the table and taken down when she leaves it.
        Beat.WalkIn => "the WALK-IN card (Map.razor, Map.WalkIn.cs · _walkInCard)",

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
    /// <para>#664 · AND FOUR OF THE ADOPTED ELEVEN SAY NO FOR THE SAME TWO REASONS THE HAIL DOES. Three of
    /// them RAISE the danger one statement before they knock — the pack comes off its benches, the pack comes
    /// out of the hatch, the collectors are already walking — so <c>CaptainIsInDanger()</c> is true at the
    /// instant of the raise and a deferrable card there does not wait for a calmer moment: it waits for the
    /// fight to end and then explains a thing that is already over. And two of them ARE the warning: the
    /// shelter card is the only sentence in the game that says a pressure vessel will not save you, and the
    /// arrival card is, in its own caller's words, <i>"THE ONLY WARNING THE PLAYER GETS"</i>. A warning held
    /// back until it is safe to read is not a warning, it is a receipt.</para>
    /// <para>#865's sit-beat hold is a different rule and still covers all eleven: that arm asks nothing
    /// about deferrability, because it is a beat and a half of screen owed to a press the player just made.</para>
    public static bool DeferrableWhileInDanger(Beat beat) => beat switch
    {
        Beat.CollectorHail => false,   // this IS the danger; it cannot wait for a better time

        // #664 · the pack is standing off its benches / coming through the hatch as this is raised
        Beat.TheDormantThingWakes or Beat.SealedDoorReleased => false,

        // #664 · the warning, which is worth nothing after the thing it warns about
        Beat.ShelterIsNotSanctuary or Beat.CollectorsSetDown => false,

        _ => PresentationOf(beat) == Presentation.Card,
    };

    /// <summary>How long a plate stays up. Long enough to read the caption at a glance, short enough that it is
    /// gone before it becomes furniture.</summary>
    public const double PlateSeconds = 7.0;

    /// <summary>
    /// #664 · THE NOISE THE SURFACE MAKES, decided here for the same reason the picture and the cadence are.
    ///
    /// <para>The seam used to chime <c>"reveal"</c> for every card and plate it raised, which was right while
    /// every beat in the file was raised by the seam alone. The eleven moments adopted from the deleted
    /// reveal-card system are not: each of them is a press or an event that <b>already makes its own
    /// noise</b>, chosen by the moment — <c>"board"</c> for a find, <c>"alarm"</c> for a hatch coming off its
    /// dogs and a pack coming through it — and the old card was deliberately silent so as not to flatten
    /// three different moments into one chime. Layering a second cue over that is the stacked-card mistake in
    /// the one channel the player cannot close, which is the argument
    /// <see cref="Presentation.Hosted"/> already makes in this file.</para>
    ///
    /// <para>So: an empty string means <i>the act that raised this beat has already been heard</i>, and it is
    /// a statement about the moment rather than a client's opinion about the seam — which is why it lives
    /// here beside the cadence and not in an argument the caller passes.</para>
    /// </summary>
    public static string Cue(Beat beat) => beat switch
    {
        Beat.ArchivePurged or Beat.StrangerStandsADrink or Beat.KaamosShardFound or Beat.KaamosFilingBounced
            or Beat.NebulaShardFound or Beat.OutpostEffectsRead or Beat.SecretLabDoorFound
            or Beat.TheDormantThingWakes or Beat.ShelterIsNotSanctuary or Beat.CollectorsSetDown
            or Beat.SealedDoorReleased => "",
        _ => "reveal",
    };

    // ── What each one shows and says ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// #664 · THE ELEVEN ADOPTED BEATS DO NOT GET NEW WORDS, THEY GET A DOOR. Every one of them already had a
    /// <see cref="RevealPlate"/> in Core — a title, a painting and a caption — written beside the rule that
    /// decides the moment happened, because that was #634's law before this file existed. Copying those
    /// strings into the three switches below would have made a second source of truth for the same sentence,
    /// which is the drift this project keeps paying for; so the switches ASK, and this is the one place that
    /// knows which plate belongs to which beat.
    ///
    /// <para>Two of them are keyed by <paramref name="subject"/> rather than fixed, and that is the shape the
    /// old card had that a per-beat table could not: a KAAMOS or NEBULA shard's plate is chosen by the
    /// fragment the captain just assembled, so the picture cannot be shown for a shard nobody found. With no
    /// subject they resolve to nothing at all, which is honest — there is no such thing as "the KAAMOS card"
    /// in general. <see cref="Canvases"/> is how a sweep asks what the whole pool can name.</para>
    /// </summary>
    private static RevealPlate? PlateOf(Beat beat, string? subject) => beat switch
    {
        Beat.ArchivePurged => ArchiveNode.PurgedPlate,
        Beat.StrangerStandsADrink => StrangerBond.CognacPlate,
        Beat.KaamosShardFound => KaamosLore.PlateFor(subject ?? ""),
        Beat.KaamosFilingBounced => KaamosLore.BouncePlate,
        Beat.NebulaShardFound => NebulaLore.PlateFor(subject ?? ""),
        Beat.OutpostEffectsRead => SurfaceOutpost.EffectsPlate,
        Beat.SecretLabDoorFound => SecretLab.DoorPlate,
        Beat.TheDormantThingWakes => SecretLab.TheyStandPlate,
        Beat.ShelterIsNotSanctuary => CollectorLanding.SiegePlate,
        Beat.CollectorsSetDown => CollectorLanding.ArrivalPlate,
        Beat.SealedDoorReleased => NestPlates.Released,

        _ => null,
    };

    /// <summary>
    /// #664 · EVERY CANVAS THIS BEAT CAN NAME — one for a fixed beat, the whole authored pool for one whose
    /// picture is chosen by its subject.
    ///
    /// <para><c>StoryArtPresentTests</c> used to sweep <see cref="ArtFile(Beat)"/>, which was the whole truth
    /// while every beat had exactly one painting. It is not any more: a subjectless <c>KaamosShardFound</c>
    /// has no canvas, and a sweep that took the empty string for an answer would have quietly stopped
    /// guarding the two arcs the moment they arrived. This is what the sweep asks instead, so adding a
    /// twelfth plate to a pool still means painting it or being told.</para>
    /// </summary>
    public static IEnumerable<string> Canvases(Beat beat) => beat switch
    {
        Beat.KaamosShardFound => KaamosLore.AllPlates.Select(p => p.Value.ArtFile).Distinct(StringComparer.Ordinal),
        Beat.NebulaShardFound => NebulaLore.AllPlates.Select(p => p.Value.ArtFile).Distinct(StringComparer.Ordinal),
        // #973 L5b · both women's portraits, so the manifest sweep sees the one that is not on screen too.
        Beat.WalkIn => WalkIn.AllPortraits,

        _ => [ArtFile(beat)],
    };

    /// <summary>The painting for this beat. Named here so the manifest and the code cannot drift; a file that
    /// has not been painted yet simply does not render, and the words carry it.</summary>
    /// <param name="subject">#664 · Which one this instance is about, for the beats whose canvas is chosen by
    /// it. Ignored by every beat that has one painting.</param>
    public static string ArtFile(Beat beat, string? subject = null) => beat switch
    {
        Beat.FirstShotFired => "art/first-shot.jpg",
        Beat.SailHoled => "art/sail-holed.jpg",
        Beat.CollectorHail => "art/collector-hail.jpg",
        Beat.CrewDeputation => "art/crew-deputation.jpg",
        Beat.CrewMeeting => "art/crew-meeting.jpg",
        Beat.ArcNewsBreaks => "art/arc-news.jpg",
        Beat.ChargeLetGo => "art/charge-let-go.jpg",
        Beat.FireAboard => "art/fire-aboard.jpg",
        // #973 · One plate for every flashback, and one style: bleached to white, a single object left in
        // focus. Fixed rather than keyed by the memory id on purpose — a memory is not a place, and painting
        // one canvas per ledger row is a pool nobody could ever finish.
        Beat.Flashback => "art/flashback.jpg",
        // #973 L5b · her portrait, which is also what her card draws. One painting for both women would be
        // one woman, so the canvas is chosen by the subject the beat was raised with.
        Beat.WalkIn => WalkIn.PortraitArt(subject),
        // #541: the tube's own canvases live with the tube, so the tier rule and the picture cannot disagree.
        Beat.BerthGreatPort => ArrivalTube.ArtFile(ArrivalTube.Tier.GreatPort),
        Beat.BerthWorkingBerth => ArrivalTube.ArtFile(ArrivalTube.Tier.WorkingBerth),
        Beat.BerthOutpost => ArrivalTube.ArtFile(ArrivalTube.Tier.Outpost),

        _ => PlateOf(beat, subject)?.ArtFile ?? "",
    };

    /// <summary>The title: it names the place and the verb, never the outcome. "WHAT THE VACUUM LEFT", not
    /// "salvage complete".</summary>
    /// <param name="subject">#664 · The room, the shard, the place — for the beats whose stamp names it.</param>
    public static string Title(Beat beat, string? subject = null) => beat switch
    {
        Beat.FirstShotFired => "🔫 THE FIRST ROUND YOU EVER FIRED",
        Beat.SailHoled => "🎯 HER SAIL IS GONE",
        Beat.CollectorHail => "⛓ GRAPPLES",
        Beat.CrewDeputation => "🧑‍🔧 A DEPUTATION",
        Beat.CrewMeeting => "🕯 THE MEETING YOU WERE NOT ASKED TO",
        Beat.ArcNewsBreaks => "📰 THE STORY BREAKS",
        Beat.ChargeLetGo => "⚡ SHE LETS GO",
        Beat.FireAboard => "🔥 THERE IS FIRE IN HER",
        // #973 · The stamp is the mark and the label the ledger row already wears, said louder. The subject
        // is the memory id and is deliberately NOT in the stamp: an entry key is bookkeeping, and a card that
        // put one on the screen would be showing the player the filing system instead of the memory.
        Beat.Flashback => FilingLine.Mark + " " + "A PAGE YOU DON'T REMEMBER WRITING",
        // #973 L5b · the stamp names the door, because the door is what the room looked at.
        Beat.WalkIn => "🚪 THE ROOM LOOKS AT THE DOOR",
        Beat.BerthGreatPort => ArrivalTube.Title(ArrivalTube.Tier.GreatPort),
        Beat.BerthWorkingBerth => ArrivalTube.Title(ArrivalTube.Tier.WorkingBerth),
        Beat.BerthOutpost => ArrivalTube.Title(ArrivalTube.Tier.Outpost),

        // #664 · The one adopted beat whose stamp names its subject: "🕷 DEEP HOLD — IT OPENS BOTH WAYS". The
        // two halves are joined in NestPlates so they cannot drift apart in two files, exactly as the after-
        // card's are; with no compartment named it falls back to the bare stamp rather than inventing a room.
        Beat.SealedDoorReleased => string.IsNullOrWhiteSpace(subject)
            ? NestPlates.Released.Title
            : NestPlates.ReleasedTitle(subject!),

        _ => PlateOf(beat, subject)?.Title ?? "",
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

            // #973 · The caption for EVERY flashback plate — the signing one included (#973 L2). Fable's
            // line, verbatim; the FABLE marker L1 left here is answered and gone.
            // #973 L5b · …except the one whose subject is `since`, which is a page about a WOMAN and not
            // about a desk. Chosen by the subject, the way a shard's plate already is, and null for every
            // other memory — so the signing's sentence stays the sentence for all of them.
            Beat.Flashback when WalkIn.FlashbackCaption(subject) is { } sinceLine => sinceLine,

            Beat.Flashback =>
                "Bleached to the bone. A pen on a steel desk, every scratch in it sharp; behind it the room, " +
                "the chair, the one at the far side of the desk, all gone to white. Only the thing that was " +
                "in the hand survives the light.",

            // #973 L5b · Fable's own line for the moment, verbatim and whole: the room notices her before the
            // captain does, and nothing else about her is said. The subject is her id and is deliberately not
            // in the sentence — a caption that named her would be the seam introducing somebody the player is
            // about to be introduced to.
            Beat.WalkIn => WalkIn.TheRoomLooks,

            Beat.FireAboard =>
                "Forty years, and a pocket of her atmosphere was still shut in with something that would burn. " +
                "The light of it comes down the spine ahead of the heat, and every hatch anybody left open is a " +
                "road it already knows.",

            // #664 · The adopted eleven read their caption off the same Core plate their title and their
            // painting come from. Not one word of these was retyped here: `KaamosLore.PlateFor` and the nine
            // named constants beside it are still the only place they are written down.
            _ => PlateOf(beat, subject)?.Caption ?? "",
        };
    }
}
