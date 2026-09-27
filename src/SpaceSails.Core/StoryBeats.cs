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
public static partial class StoryBeats
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

        /// <summary>#1149 · THE ONE REFUGE THAT DID NOT HOLD. A pressure refuge, on the plan, marked on the
        /// fan, and dead — and the room says why. The subject is the SITE (<c>ex.Stop.Body.Id</c>): a
        /// building has at most one of these and it is that building's story, so a second moon's dead refuge
        /// is a second moment and not a repeat.</summary>
        RefugeFailed,

        /// <summary>#1151 · THE DESK COMES BACK. A claim is lodged at a kiosk and, from the second one
        /// onward, the captain is somewhere else for a moment: an office, a pen, a page he did not read. No
        /// subject — the memory is not about WHICH hull he lost, it is about the process he keeps agreeing
        /// to, and a subject here would be the seam filing the very thing the card is uneasy about.</summary>
        TheClaim,

        /// <summary>#1199 · THE WALK IS EMPTY. Somebody was followed out onto a glass-floored tube with one
        /// way in, the captain waited at the mouth, and then walked in and there is nobody there. <b>No
        /// subject</b>, and the cadence is <see cref="Cadence.OnceEver"/>: the beat is already spent once
        /// per universe against its own vault flag, and a card keyed on a person would quietly offer a
        /// second one the day a second person walked a route. There is one of these, ever.</summary>
        TheObservationWalk,

        /// <summary>#1199 (2026-09-18) · <b>A LOOK THROUGH THE WALK'S COIN BINOCULARS.</b> Two coins in a
        /// slot at the rail, and the optics show one of two things — out over the regolith, or straight down
        /// through the glass floor. The SUBJECT is which look it was (<see cref="GalleryFixtures.Look"/>, by
        /// name), because one beat with two paintings is exactly what that channel is for; two beats would be
        /// two cadences to keep in step for one machine. The cadence is <see cref="Cadence.EveryTime"/>: a
        /// captain who has paid again has bought the picture again, and a press that took the coins and
        /// showed nothing is the worst refusal in this codebase — an absence.</summary>
        TheWalksBinoculars,

        /// <summary>#1199 (2026-09-18) · <b>THE GALLERY'S VENDING MACHINE.</b> A coin, a wrapper in a
        /// language nobody aboard reads, and the one fact the room otherwise never states: somebody stocks
        /// it. <see cref="Cadence.EveryTime"/> for the binoculars' own reason — the coin is taken every time,
        /// so the card is owed every time.</summary>
        TheGalleryVendor,
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
}
