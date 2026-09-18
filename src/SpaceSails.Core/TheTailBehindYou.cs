using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #1062 slice 2 · <b>THE TAIL BEHIND YOU</b> — the mirror half, and the one the owner laughed about second:
/// <i>"… or trying to lose a tail our selves :-D"</i>.
///
/// <para>#1062 named what it would be made of and refused to invent anything for it: <i>"Detection is
/// gumshoe craft with mechanics we already have: sit facing the door (the seat system knows sightlines),
/// double back through a two-door room (the FIRE CODE's ≥2 doors becomes gameplay), watch who follows you
/// through the second one. Losing them: crowd churn, a lift timed at the close, the park."</i> This file is
/// that, with the seam <see cref="FootTail"/> left open for it in #793 finally filled.</para>
///
/// <h3>THERE IS NOT ONE DIE IN THIS FILE, and that is the whole difference from slice 1</h3>
///
/// <para><see cref="TheTail"/> — the captain tailing somebody — asks #436's eye, and #436's eye rolls, because
/// the question there is <i>did a man happen to glance over his shoulder</i>. <b>This half asks the opposite
/// question and it must not roll.</b> The captain is not glancing: he is sitting in a chair with his eye on a
/// door, on purpose, for as long as he chooses to sit there. Gumshoe craft that paid off on a die would be a
/// craft nobody could practise — you would learn nothing from having done it right — and #1062's own law says
/// so in one line: <i>deterministic notice (a pure function of distance/exposure ticks, no dice)</i>.</para>
///
/// <para>So every question below is arithmetic on a range, a clock and a count of doorways. The same inputs
/// give the same answer on every machine, on every reload, for ever. <see cref="AllProse"/> is the only place
/// this file can surprise anybody.</para>
///
/// <h3>ONE SIGHTLINE ORACLE, and it is the one slice 1 uses</h3>
///
/// <para><see cref="PatrolBeat.EyesOn"/> — range plus <see cref="SurfaceCollision.HasLineOfSight"/>, the
/// game's one look, reached here through <see cref="FootTail.InPlainSight"/> so that this file does not even
/// spell the call. Nothing below measures a wall, and there is no second arithmetic anywhere in the lane for
/// a source sweep to find.</para>
///
/// <h3>He is a person, and the numbers say which kind</h3>
///
/// <para><b>His standoff is the range this game already calls "registering somebody".</b>
/// <see cref="PatrolBeat.NoticeDu"/> is #832's own statement of <i>how close one person has to be before they
/// register that somebody is standing there</i> — a third of the eye's reach, and the whole of the timing
/// window the stealth game is played in. A man keeping station lives exactly on it: near enough to keep you,
/// far enough that you have had no reason to have registered him. So <see cref="StandsOffDu"/> quotes that
/// number rather than choosing one, and the far edge of his band is the range at which a body on a deck is
/// legible at all (<see cref="FootTail.LegibleDu"/>) — past which he would be the one losing you.</para>
///
/// <para><b>And he is mundane.</b> #1062 is explicit that this half <i>"takes NO position on #672"</i> — a
/// tail here is a human being until some other issue says otherwise. He never confronts, never speaks, never
/// blocks a door and is never put on a pathfinder that an Old One could be put on. What he does is stand
/// where he can see you and not order a drink.</para>
///
/// <para>Pure and deterministic, like everything else in Core.</para>
/// </summary>
public static class TheTailBehindYou
{
    // ── WHETHER ANYBODY IS BEHIND YOU AT ALL ────────────────────────────────────────────────────────────

    /// <summary>
    /// #1062 · <b>WHOSE MAN HE IS — the folder, and there is no new state anywhere in this lane for it.</b>
    ///
    /// <para>#715's illegal heat is owed to an OUTFIT, and an outfit's folder is the only thing in this game
    /// that already means <i>somebody, somewhere, is keeping a page with this captain on it</i>. It is
    /// persisted (it rides the contacts ledger into the vault), it is already banked ASHORE by two shipped
    /// acts — the walk-in's femme-fatale setup, which is a customs post that has been waiting for this ship,
    /// and selling the compromising chip at a dark-web desk — and it is readable at a berth through
    /// <see cref="IllegalHeat.HeatAtSite"/>, which is the one call every effect in the game asks this
    /// question with.</para>
    ///
    /// <para><b>The band is the game's own, quoted.</b> <see cref="IllegalHeat.TheGateWantsAFace"/> is the
    /// rung where an outfit stops treating a hull as paperwork and starts wanting to know what the captain
    /// looks like. A man walking twenty paces behind him across a concourse is that sentence with legs on: it
    /// is not a punishment bolted onto the meter, it is the meter's own existing meaning, carried out. A new
    /// threshold here would have been a second opinion about when a company gets curious.</para>
    ///
    /// <para><b>And the other door in is slice 1's own failure.</b> A captain who tailed a named regular and
    /// was CLOCKED doing it has told somebody, in the most direct way available, that he follows people. The
    /// evening answers. That costs no new field either — the page already latches it for the visit — and it
    /// is the two halves of #1062 pointed at each other, which is what the issue said the whole thing
    /// was.</para>
    ///
    /// <para><b>No roll and no watch term.</b> The folder cools by itself (<see cref="IllegalHeat.Cool"/> —
    /// an hour of sim time a point, and only off that outfit's ground), so the clock is already inside the
    /// answer: a captain who wants the man gone goes away, which is the owner's own ruling about this meter
    /// said back to him. Adding a per-watch die would have made the one thing the player can do about it —
    /// leave — indistinguishable from luck.</para>
    /// </summary>
    /// <param name="heatAtThisBerth"><see cref="IllegalHeat.HeatAtSite"/> for the berth the captain is
    /// standing in.</param>
    /// <param name="youWereClockedTailingSomebody">Slice 1's latch: the person of interest noticed the
    /// captain behind him at this berth, on this visit.</param>
    public static bool Follows(int heatAtThisBerth, bool youWereClockedTailingSomebody) =>
        IllegalHeat.TheGateWantsAFace(heatAtThisBerth) || youWereClockedTailingSomebody;

    // ── THE BAND HE KEEPS ───────────────────────────────────────────────────────────────────────────────

    /// <summary>#1062 · How close he ever comes — <see cref="PatrolBeat.NoticeDu"/>, this game's own
    /// statement of the range at which one person registers that another is standing there. Quoted and never
    /// chosen: see the type's own note.</summary>
    public static double StandsOffDu => PatrolBeat.NoticeDu;

    /// <summary>#1062 · …and the range past which he would lose YOU, which is the same range at which a body
    /// on a deck is legible at all (<see cref="FootTail.LegibleDu"/>, itself
    /// <see cref="PatrolBeat.MarkerSightDu"/>). Both ends of his band are the game's own numbers and neither
    /// was invented here.</summary>
    public static double LosesYouBeyondDu => FootTail.LegibleDu;

    /// <summary>#1062 · Is he where a tail stands? No nearer than the range a person is registered at, and no
    /// further than the range a body is legible at.</summary>
    public static bool HoldsHisBand(double rangeDu) =>
        rangeDu >= StandsOffDu && rangeDu <= LosesYouBeyondDu;

    /// <summary>
    /// #1062 · <b>THE TWO RANGES HE TRIES, in order.</b> As far back as the room will let him, and if it will
    /// not, as far back as his band allows at all.
    ///
    /// <para>Two and not one because a station bar is a ROOM: a single standoff would mean a man who can only
    /// tail you across a concourse and simply fails to exist indoors, which is the worst of both — a feature
    /// that is invisible exactly where the owner plays. And it is an ordered LIST rather than a search
    /// between them, for this file's usual reason: a list can be read, and a search has to be trusted.</para>
    /// </summary>
    public static IReadOnlyList<double> TheRangesHeTries { get; } =
        [(StandsOffDu + LosesYouBeyondDu) / 2.0, StandsOffDu];

    /// <summary>
    /// #1062 · <b>THE SIDES HE SOUNDS, in order</b> — the bearings, relative to the captain, that a standing
    /// place is looked for on.
    ///
    /// <para>Behind first, then the two quarters behind, then out to the sides, and the captain's own front
    /// last: a man keeping station on somebody stands where he is least looked at, and the order is the
    /// argument. It is a LIST AND NOT A ROLL for the same reason everything else here is — two captains in
    /// the same room with the same walls get the same man in the same corner — and it is
    /// the haven's own idiom for this (<c>HavenInterior.BesideATop</c>), where a body beside a table is
    /// placed by sounding published sides in a published order until the stone allows one.</para>
    /// </summary>
    public static IReadOnlyList<double> TheSidesHeSounds { get; } =
    [
        Math.PI, 3 * Math.PI / 4, -3 * Math.PI / 4, Math.PI / 2, -Math.PI / 2,
        Math.PI / 4, -Math.PI / 4, 0.0,
    ];

    // ── #1229 · …AND THE OTHER BEHAVIOUR, WHICH THE BRIEF HAD FROM THE START ─────────────────────────────
    //
    // #1062's brief said it in one sentence — "enters the room after the captain, KEEPS A DISTANCE BAND,
    // TAKES A SEAT/STANDING SPOT WITH A SIGHTLINE TO HIM, orders nothing" — and only the first half shipped.
    // A man who can only keep a band is a man a station bar cannot hold: #1231 measured it at Selene Gate,
    // where the reach he keeps is longer than the room has in every direction but its corners, so the stone
    // pushed him OUT through the one doorway and he lost a captain he had never been in the room with. The
    // spot-finder had exactly one place clause in it, and that clause was about a gangway.
    //
    // So there are two behaviours and the ROOM chooses between them, by measurement and never by name:
    //   · the room can hold the reach he keeps  → he KEEPS THE BAND, unchanged, which is a concourse;
    //   · it cannot                             → he takes a POST, which is a bar.
    // A post is a standing place against the room's own stone, inside the room, with a line to the captain,
    // nearest the doorway he came in by — and never the counter and never a chair, because he has not
    // ordered and a seat would make him a patron. The chair reading then reads as it was always meant to:
    // you sit facing the door, and he is the man by the door who has not ordered.

    /// <summary>#1229 · <b>THE REACH A BAND IS ACTUALLY KEPT AT</b> — the first of
    /// <see cref="TheRangesHeTries"/>, named so that the question <i>can this room hold his band?</i> and the
    /// sounding that places him are asking about one number rather than two.</summary>
    public static double TheReachHeKeeps => TheRangesHeTries[0];

    /// <summary>#1229 · How far off the stone a POST stands: one body ACROSS, the same step the haven uses to
    /// stand a body beside a table (<c>HavenInterior.BesideATop</c>) and the ashore boot uses to stand the
    /// captain wholly inside a room. A body-width and never a coordinate.</summary>
    public static double StandsOffTheWallBy(double bodyRadius) => 2 * bodyRadius;

    /// <summary>
    /// #1229 · <b>THE PLACES A PIECE OF THE ROOM'S OWN STONE OFFERS A MAN TO STAND.</b>
    ///
    /// <para>One wall, sounded: its length cut into body-wide slices and a standing place offered at the
    /// middle of each, one body clear of the stone, on the side the captain is standing on. Pure arithmetic
    /// on a segment and a point — no dice, no search, no pathfinder, and the same list in the same order on
    /// every machine for ever. Whether any of them is a place a body may actually BE is the caller's
    /// question, because only the caller has the room's whole stone and the one sightline oracle.</para>
    ///
    /// <para>The slicing is a body's width for the reason the offset is: it is the only unit a room like this
    /// has. A wall shorter than one body still offers its own middle, so no piece of a room is silently
    /// unavailable — a wall that offered nothing would be a corner a man could never stand in, and the tell
    /// this feature is built on is a man standing where you did not expect one.</para>
    /// </summary>
    /// <param name="x1">One end of the wall.</param>
    /// <param name="y1">One end of the wall.</param>
    /// <param name="x2">The other end.</param>
    /// <param name="y2">The other end.</param>
    /// <param name="bodyRadius">The width of the person being stood, in the deck's own units.</param>
    /// <param name="towardsX">The captain — which decides WHICH SIDE of the stone is the room.</param>
    /// <param name="towardsY">The captain.</param>
    public static IReadOnlyList<(double X, double Y)> PostsAlong(
        double x1, double y1, double x2, double y2, double bodyRadius, double towardsX, double towardsY)
    {
        double dx = x2 - x1, dy = y2 - y1;
        double length = Math.Sqrt((dx * dx) + (dy * dy));
        if (length <= 0 || bodyRadius <= 0 || double.IsNaN(length))
        {
            return [];   // a wall with no length is not a wall, and it offers nowhere to stand
        }

        double ux = dx / length, uy = dy / length;
        double step = StandsOffTheWallBy(bodyRadius);

        // The normal that points INTO the room, which is the side the captain is on. A wall has two faces and
        // only one of them is somewhere a man in this room could be standing.
        double nx = -uy, ny = ux;
        if (((towardsX - x1) * nx) + ((towardsY - y1) * ny) < 0)
        {
            (nx, ny) = (-nx, -ny);
        }

        int slices = Math.Max(1, (int)(length / step));
        var posts = new List<(double X, double Y)>(slices);
        for (int i = 0; i < slices; i++)
        {
            double along = (i + 0.5) * (length / slices);
            posts.Add((x1 + (ux * along) + (nx * step), y1 + (uy * along) + (ny * step)));
        }

        return posts;
    }

    // ── THE CLOCK BOTH HALVES ARE READ ON ───────────────────────────────────────────────────────────────

    /// <summary>#1062 · One exposure tick — <see cref="ReeverObservation.LookIntervalSeconds"/>, the cadence
    /// this game already measures looking in. Borrowed rather than re-stated so that a captain's patience and
    /// a Reever's glance are counted in the same unit, and so the day that cadence is tuned this feature is
    /// tuned with it instead of drifting away from it.</summary>
    public static double TickSeconds => ReeverObservation.LookIntervalSeconds;

    /// <summary>
    /// #1229 · <b>HOW LONG HE LETS YOU GET AHEAD BEFORE HE COMES AFTER YOU.</b> ONE look — the cadence
    /// above, not a number of its own.
    ///
    /// <para>It is the whole of what makes the two-door tell an honest sequence rather than an accident of
    /// where he happened to be planted. A man posted inside a room does not leave it the instant his subject
    /// does; he gives it a beat and then comes through the same doorway. <b>That beat is the tell</b> — the
    /// same coat through the doorway you just used, and then through the next one — and it is why the
    /// count is of DISTINCT doorways: what the captain reads is a sequence, not a position.</para>
    ///
    /// <para>One look and not two, argued at both ends: none at all and he is a shadow welded to the
    /// captain's heels, which is not a man; more than a look and a captain at his own pace is out of the
    /// room, across the concourse and through the next doorway before the man has moved, which is not a
    /// tail.</para>
    /// </summary>
    public static double SecondsBeforeHeFollowsYouOut => TickSeconds;

    /// <summary>
    /// #1062 · <b>HOW MANY TICKS OF HAVING HIM IN FRONT OF YOU MAKE IT A FACT.</b>
    ///
    /// <para>Twelve — nine seconds of the player's own clock. FLAGGED for the owner's tuning, and argued from
    /// both ends: fewer and every stranger who happens to be facing your way across a bar is a tail, which is
    /// the fifth bug class with a hat on (a reading that selects everybody selects nobody); many more and the
    /// craft costs a warp-forward rather than a decision, which is the exact complaint the owner made about
    /// the observation walk's first wait.</para>
    ///
    /// <para>It is stated as a COUNT OF LOOKS and not as a number of seconds, so that it goes on meaning
    /// "long enough for a man to have been there for several looks" if the look cadence ever moves.</para>
    /// </summary>
    public const int LooksToNotice = 12;

    /// <summary>#1062 · …in seconds.</summary>
    public static double NoticeSeconds => LooksToNotice * TickSeconds;

    /// <summary>#1062 · <b>AND IT COSTS EXACTLY THE SAME TO LOSE HIM.</b> One number, read from both ends:
    /// the instrument that says a man has been behind you for twelve looks is the instrument that says he has
    /// not been behind you for twelve looks, and a second constant here would be two opinions about how long
    /// a person stays interesting. It also makes the exchange honest — what the captain spends to find out is
    /// what he has to spend again to get rid of it.</summary>
    public static int LooksToLoseHim => LooksToNotice;

    /// <summary>#1062 · …in seconds.</summary>
    public static double LostSeconds => LooksToLoseHim * TickSeconds;

    // ── (i) THE CHAIR THAT FACES THE DOOR ───────────────────────────────────────────────────────────────

    /// <summary>
    /// #1062 · <b>CAN THIS SEAT SEE THAT DOOR?</b>
    ///
    /// <para><b>The issue said "the seat system knows sightlines" and the audit says it does not.</b> A
    /// sitting in this game writes the captain's X and Y and nothing else — no facing, no fan, no view —
    /// and a chair is asked whether it is quiet, whether it is aboard and how many it seats, never what it
    /// looks at. Rather than growing the seat family a thirty-third member for one feature, the question is
    /// asked the way every other question about what can be seen from a spot on a deck is asked: the ONE
    /// look, over the room's own stone. A seat that can see the door is a seat the oracle says has a line to
    /// it, and there is nothing else to know.</para>
    ///
    /// <para>That is also the honest reading of the beat. It is not the chair that is clever; it is choosing
    /// to sit in that one.</para>
    /// </summary>
    public static bool ThisChairSeesTheDoor(
        double chairX, double chairY, double doorX, double doorY,
        IReadOnlyList<SurfaceCollision.Segment>? walls) =>
        PatrolBeat.EyesOn(chairX, chairY, doorX, doorY, LosesYouBeyondDu, walls);

    /// <summary>#1062 · Has the sit paid off? A pure comparison on a count of ticks, and the whole of the
    /// notice law from the chair.</summary>
    public static bool NoticedFromTheChair(double exposureSeconds) =>
        exposureSeconds >= NoticeSeconds;

    // ── (ii) THE SAME COAT THROUGH TWO DOORS ────────────────────────────────────────────────────────────

    /// <summary>
    /// #1062 · <b>HOW MANY DOORWAYS MAKE IT A TELL — and it is the FIRE CODE's own number.</b>
    ///
    /// <para>#822's standing law is that no space may have only one way out
    /// (<see cref="UndergroundComplex.FireCodeMinExits"/>), and #1062 asked for that law to stop being
    /// scenery: <i>"double back through a two-door room (the FIRE CODE's ≥2 doors becomes gameplay)"</i>. So
    /// the count is quoted from the law rather than typed here. Two doorways is not a difficulty knob — it is
    /// the smallest number of ways out of a room that the building is required to have, which is exactly why
    /// doubling back is a move a captain can make anywhere rather than a trick that works in one bar.</para>
    /// </summary>
    public static int DoorsThatMakeTheTell => UndergroundComplex.FireCodeMinExits;

    /// <summary>#1062 · <b>Nobody's errand takes them through both.</b> The count is of DISTINCT doorways he
    /// has been seen coming through, so a man loitering in one doorway for a whole watch never adds up to
    /// anything, however long he stands there — which is the point: the tell is not that he was at a door, it
    /// is that he was at the door you chose next.</summary>
    public static bool TwoDoorsRunning(int distinctDoorwaysHeCameThrough) =>
        distinctDoorwaysHeCameThrough >= DoorsThatMakeTheTell;

    // ── LOSING HIM ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>#1062 · <b>IS HE LOST?</b> A run of ticks with nothing for him to see, and no other clause.
    ///
    /// <para><b>The audit changed what this is spent ON.</b> #1062's sketch offers "a door closed between
    /// you" and a lift timed at the close, and ashore this game has neither: a haven's collision field is
    /// built from WALLS ONLY, no door on a station deck carries an interlock, an auto-door never blocks a
    /// passage, and there is no lift at a berth at all. What a haven does have is walls — the bar's own south
    /// wall with one doorway in it, the ring's nine sealed edges, the gangway tube and, at Selene Gate, an
    /// observation walk with a blind end. So losing him is a geometry problem and not a button, which is the
    /// better version of the move anyway.</para></summary>
    public static bool HeIsLost(double outOfHisSightSeconds) =>
        outOfHisSightSeconds >= LostSeconds;

    // ── THE CANON (Fable, verbatim; nothing else authored) ──────────────────────────────────────────────

    /// <summary>#1062 · The glyph all three of this half's sayings wear — the EYE the game already hangs on
    /// every watched-from-somewhere beat (<see cref="ReeverObservation.FixedOnYouGlyph"/>), borrowed and
    /// never re-typed, exactly as slice 1 borrows it.</summary>
    public const string Glyph = ReeverObservation.FixedOnYouGlyph;

    /// <summary>#1062 · <b>THE CHAIR PAYS OFF.</b> Authored (Fable), verbatim. Two facts and one omission:
    /// what the seat is for, and what is in it — and not one word about who he is, because the captain does
    /// not know and the game is not going to tell him.</summary>
    public const string FromThisChairLine =
        "From this chair you can see the door. So can the man who came in after you, and he has not ordered.";

    /// <summary>#1062 · <b>THE DOUBLE-BACK PAYS OFF.</b> Authored (Fable), verbatim. It names a coat and a
    /// count, which is everything a doubling-back actually gives you, and then draws the only conclusion
    /// available from it.</summary>
    public const string TwoDoorsLine =
        "The same grey coat, two doors running. Nobody's errand takes them through both.";

    /// <summary>#1062 · <b>AND HE IS GONE.</b> Authored (Fable), verbatim — a sentence about a corridor being
    /// nothing, which is what losing a tail feels like and is the opposite of a fanfare.</summary>
    public const string LostLine =
        "The corridor behind you is only a corridor. Whoever it was is asking the wrong floor about you.";

    /// <summary>#1062 · What the book writes when he is lost. Authored (Fable), verbatim, with the place the
    /// game has printed substituted and no other word composed.</summary>
    public static string NoteLine(string place)
    {
        ArgumentNullException.ThrowIfNull(place);
        return $"a tail, lost at {place} — a grey coat, never a face";
    }

    /// <summary>#1062 · What that note is filed UNDER — the PLACE, declared here beside the sentence that
    /// prints it, which is #741's law and the reason no client file may mint a subject.
    ///
    /// <para>A place and never a person, and the prose is the argument: <i>never a face</i>. A thread heading
    /// with a name on it would be the book claiming an identification the captain never made, which is the
    /// one thing #741 exists to refuse.</para></summary>
    public static string Subjects(string place)
    {
        ArgumentNullException.ThrowIfNull(place);
        return CaseSubjects.Line(CaseSubjects.Place(place));
    }

    /// <summary>#1062 · The plate the walker carries. It is a QUOTATION of the authored note above and never
    /// a name, and the figure that wears it is drawn on the smear rung — where this game draws no plate at
    /// all — so it is never on screen. Stated here rather than typed into a client file so that the sweep
    /// that reads this type's prose sees it too.</summary>
    public const string Plate = "GREY COAT";

    // ── THE BURN ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1062 · <b>WHAT A PLACE-REVEALING ACT COSTS WHEN SOMEBODY IS STANDING BEHIND YOU DOING IT.</b>
    ///
    /// <para>#1062: <i>"Failing: Fail Forward, never game-over — the place you were going is burned, and
    /// leading a tail to the dead-drop is how a dead-drop dies."</i> The dead drop is on a moon and this
    /// figure is not, so the honest ashore equivalent is what a captain actually goes to a berth to do
    /// quietly: take a favour across a contact's table, or buy a code off a fence at a dark-web desk. Both
    /// already run through ONE strike-off (<see cref="BlackOpsKey.ThePortHasDealtOne"/>) in the captain's own
    /// durable register of ground already gone through — which is why this burn needs no state of its own. It
    /// is a second tag in the same set, and it rides the vault for free.</para>
    ///
    /// <para><b>Silent at the moment.</b> Nothing whatever is said on the frame the act is performed. The
    /// place simply has nothing for the captain the next time he walks into it, and THAT is when the telling
    /// happens — #761's own rule, the same one slice 1 obeys about a burned lead.</para>
    ///
    /// <para><b>And it is the smallest burn the shipped state supports.</b> One port, one visit's worth of
    /// what that port deals. Not a goodwill band — that would be a contact having decided something about the
    /// captain, and in this fiction nobody has told anybody anything. Not a lost item, not a fine, not a
    /// heat point. Somebody got there first, and that is the whole of it.</para>
    /// </summary>
    public static string BurnTag(string portId)
    {
        ArgumentNullException.ThrowIfNull(portId);
        return $"tail-burn:{portId}";
    }

    /// <summary>#1062 · <b>WHAT THE PLACE IS LIKE WHEN YOU COME BACK.</b> Authored (Fable), verbatim — and it
    /// is a sentence about HOUSEKEEPING. It names nobody, accuses nobody and explains nothing; what it
    /// reports is an absence of mess, which is the only evidence a place that has been gone through carefully
    /// ever offers.</summary>
    public const string TheBurnLine = "Tidy, in the way a place is after somebody has been through it first.";

    /// <summary>#1062 · …and what the book writes about it. Authored (Fable), verbatim, with the place the
    /// game has printed substituted and no other word composed. Filed under the PLACE for the reason the
    /// losing note is: there is still no face, and there never will be one.</summary>
    public static string BurnNote(string place)
    {
        ArgumentNullException.ThrowIfNull(place);
        return $"{place} — walked before you got there, by somebody who knew where to walk";
    }

    /// <summary>#1062 · The burn's subject, minted in Core beside the sentence that prints it (#741). The
    /// same subject as the losing note, deliberately: both entries are about one place, so THREADS stacks
    /// them under one heading and the captain reads the evening in order.</summary>
    public static string BurnSubjects(string place) => Subjects(place);

    /// <summary>#1062 · Every player-facing string this half publishes, and there are no others. The
    /// <c>AllProse</c> discipline every prose-bearing type in Core keeps, and the list the reserved-word and
    /// pattern-word sweeps walk.</summary>
    public static IEnumerable<string> AllProse(string place)
    {
        ArgumentNullException.ThrowIfNull(place);
        yield return FromThisChairLine;
        yield return TwoDoorsLine;
        yield return LostLine;
        yield return NoteLine(place);
        yield return TheBurnLine;
        yield return BurnNote(place);
    }

    // ── THE DECLARATION #793 LEFT A SEAM FOR ────────────────────────────────────────────────────────────

    /// <summary>
    /// #1062 · <b>HE IS THE FIRST THING IN THIS GAME THAT DECLARES ITSELF A TAIL.</b>
    ///
    /// <para><see cref="FootTail"/> was written in #793 as a seam and said so out loud: <i>"Nothing in the
    /// game tails the captain yet … This file is the SEAM: the question exists, the bench asks it every time
    /// it is sat on, the hold law is written, and the day a watcher is built it has one place to declare
    /// itself rather than a bench growing a second opinion about who is behind you."</i> This is that day, and
    /// this is that one place: the mover is minted here, with <see cref="FootTail.Mover.Tailing"/> set by the
    /// thing that steers him, and never inferred from two positions and a heading by anybody.</para>
    ///
    /// <para>He is emphatically NOT <see cref="FootTail.OnARound"/>. His route is drawn against where the
    /// captain is standing this second, which is the disqualifying clause read the other way: a man whose
    /// destination is you is the definition the bench was given.</para>
    /// </summary>
    public static FootTail.Mover AsAMover(double x, double y) =>
        new(Plate, x, y, OnAPublishedRound: false, Tailing: true);
}
