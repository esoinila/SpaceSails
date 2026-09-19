using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #1199 · <b>THE OBSERVATION WALK</b> — the room that is a dead end on purpose, and the one beat it is
/// built to carry.
///
/// <para>Owner, 2026-09-13, verbatim: <i>"Suppose we shadow somebody into a dead end and once we get there
/// there is nothing there — even better if we preclude the cliché hidden door by having that place be like
/// an observation tube (a Grand Canyon walk on top of the cliff with a transparent floor) in some space
/// station, with only one entry / exit, and somebody we tail vanishes there. So we know they went in, and
/// after a wait we wonder and go see, and nothing there. Those moments are narratively great."</i></para>
///
/// <h3>The place comes first, and it is ordinary</h3>
/// <para>A straight tube out from the concourse over the drop: glass underfoot, lit the whole way, a rail at
/// the blind end, <b>one way in and no other</b>. It is a real room before the beat and a real room after
/// it — captains walk it for the view, the map layer names it (<see cref="Plate"/>), and nothing about it is
/// special until it is. That is the whole of the precaution the owner asked for: the cliché this beat has to
/// survive is a hidden door, and the answer is a room whose own plate says it has none.</para>
///
/// <h3>Why the fire code has to let it off BY NAME</h3>
/// <para>#822's standing law — <i>"no space may have only one door except bedroom-small rooms"</i> — has
/// exactly one exemption, and it is dimensional: a room you can cross in two paces. This room is the
/// opposite of that (<see cref="LengthDu"/> is three times the length that exemption is stated in) and it
/// still has one doorway, because <b>a dead end by design is the whole point of the room</b>. So the law
/// grows a NAMED exemption rather than a softer number — see
/// <see cref="UndergroundComplex.FireCodeExemption.ObservationWalk"/>, where the exemption list and its
/// reasons live.</para>
///
/// <h3>The beat, and why it is spent once</h3>
/// <para>A person of interest walks a route through the station and the first route ends here. The captain
/// follows; the person goes in; the captain waits at the mouth; after <see cref="TheWaitSeconds"/> nobody
/// has come out; the captain walks in and <b>the walk is empty</b>. One card at the blind end
/// (<see cref="CardTitle"/>, <see cref="CardBody"/>), one note in the book (<see cref="NoteLine"/>) filed
/// under the person's own name, and later — once, somewhere else — <see cref="CounterLine"/>.</para>
///
/// <para><b>Once per universe</b>, on <see cref="EmptySeal"/>'s shape and for a harder version of its
/// reason. The owner's own argument is that these moments are <i>"used sparingly, because otherwise they
/// lose their dramatic potency"</i>: a second vanishing is not a second beat, it is the first beat becoming
/// a mechanic, and a mechanic can be tested. Afterwards the walk is a walk, the person keeps walking routes,
/// and nobody is ever tailed to a vanishing again.</para>
///
/// <h3>Scully protection, by construction (#672 / #1063)</h3>
/// <para>The captain could have looked away for a second. Nothing here says otherwise. THE BOOK NEVER LIES
/// (#1063) and the book says only what was seen — it was followed in, it was waited for, it was not there.
/// No string below names the pattern, the observables or §8's reserved word, and nothing explains anything:
/// the card IS the absence.</para>
///
/// <para>Pure and deterministic, like everything else in Core.</para>
/// </summary>
public static class ObservationWalk
{
    // ── THE PLACE ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1199 · <b>WHICH HAVEN HAS IT.</b> Selene Gate — the oldest port in the system, in orbit off Luna —
    /// and the choice is the view.
    ///
    /// <para>The issue asks for <i>"a view worth a walk"</i>, and this is the only berth in the game whose
    /// window looks at somewhere the captain is FROM: its bar is THE EARTHRISE BAR and its selfie spot is
    /// EARTHRISE, both named for the same limb. A pilgrimage port (The Red Eye) already spends its view in
    /// the room you drink in — the Spot is the bar's back wall — so a gallery there would be a second window
    /// onto the thing the first window is for. Here the walk is the errand: you go out over the drop to look
    /// at home, which is a reason to be alone at the end of a tube that needs no explaining and gets
    /// none.</para>
    ///
    /// <para>One haven, as the issue specifies. A room in every station would be a feature; a room in one
    /// station is a place.</para>
    /// </summary>
    public const string HavenId = "selene-gate";

    /// <summary>#1199 · What the map layer stencils on it. Authored (Fable, canon pass), verbatim — a NAME
    /// and nothing else. A plate that said what the room was for, or that it had one way in, would be the
    /// house explaining a room, and a room this beat needs the player to have walked casually is a room the
    /// house must have nothing to say about.</summary>
    public const string Plate = "OBSERVATION WALK";

    /// <summary>
    /// #1199 · How far the walk reaches out from the concourse, in deck units.
    ///
    /// <para>Three times <see cref="UndergroundComplex.FireCodeSmallRoomDu"/>, and that is the whole
    /// derivation: the fire code lets a room off with one door when its longest side is no longer than that
    /// number, and this room is emphatically, measurably not one of those. Writing the length as a multiple
    /// of the exemption it does NOT qualify for is the argument for its own exemption, stated in the
    /// dimension rather than in a comment — and a walk you cannot cross in two paces is the one the owner
    /// described (<i>"a Grand Canyon walk on top of the cliff"</i>), where going in is a decision and
    /// coming back out takes long enough to be noticed.</para>
    /// </summary>
    public const double LengthDu = 3 * UndergroundComplex.FireCodeSmallRoomDu;

    /// <summary>#1199 · How many ways in and out it has. One, and it is the room's defining fact rather than
    /// an accident of a carve, so it is stated here and asserted against the built deck.
    ///
    /// <para>#1199 (2026-09-18) · <b>Still one, now that the walk is a T.</b> The gallery at the far end is
    /// not a second room with a second door — the tube opens into it across its whole width, with no leaf,
    /// no jambs and no wall between, exactly as a corridor opens into the hall it belongs to. The whole T is
    /// one space with one doorway onto the concourse, which is the only reading under which the fire code's
    /// named exemption stays honest and the only reading under which the beat still works.</para></summary>
    public const int Doorways = 1;

    /// <summary>#1199 · Which exemption the fire code lets it off under — its own, by name. Stated here so
    /// the room and the law are reading one sentence, and so a guard can walk from the room to the exemption
    /// and back.</summary>
    public const UndergroundComplex.FireCodeExemption Exemption =
        UndergroundComplex.FireCodeExemption.ObservationWalk;

    /// <summary>#1199 · The plate the art hangs on — the same canvas the card carries, laid under the glass
    /// as the floor of the walk. One image, because the drop you walk over and the drop in the picture are
    /// the same drop, and two canvases of one view would be the room and the card disagreeing about where
    /// the captain is standing.</summary>
    public const string ArtUrl = "art/observation-walk.jpg";

    // ── #1199 (2026-09-18) · THE GALLERY — THE CROSSBAR OF THE T ───────────────────────────────────────
    //
    // Owner, live on the walk: "Let's have a wider place at the far end of the observation place… like
    // somewhere where 10 people can stand side to side and watch out — that is how these usually are built:
    // a tube, then an area to view." And, the same afternoon: "maybe even a small vending machine cafeteria
    // with a couple of tables there… the station likes to get the tourist money… by making the T-character
    // wider hatted."
    //
    // So the room keeps the tube it already had as the STEM and grows a viewing room across the far end of
    // it: the rail along the outer glass, the glass floor running on underneath, the tube its only way in.
    // What the shape buys mechanically is the reason it is worth building at all — a straight tube keeps the
    // person being followed in the captain's line down its whole length, so a captain who walks in AHEAD of
    // them holds them in sight for ever and the beat stalls at the mouth. A crossbar gives the captain
    // somewhere to stand out of that line, which is the only way the vanishing can happen at all.

    /// <summary>
    /// #1199 · <b>HOW MANY PEOPLE STAND AT THE RAIL SIDE BY SIDE.</b> Ten, and it is the owner's own number
    /// — <i>"somewhere where 10 people can stand side to side and watch out"</i> — kept as a COUNT of bodies
    /// rather than converted here into a width.
    ///
    /// <para>The conversion belongs where a body's width is known, which is the client's deck
    /// (<c>DeckPlan.AvatarRadius</c>, and Core may not reference it — see <see cref="SurfaceScale"/> for the
    /// same seam). So Core states the requirement and the client's own law proves the built plan meets it,
    /// by measurement off the generated walls rather than by arithmetic either side agrees with in
    /// private.</para>
    /// </summary>
    public const int AbreastAtTheRail = 10;

    /// <summary>
    /// #1199 · <b>HOW WIDE THE CROSSBAR IS</b> — as wide as the stem is long.
    ///
    /// <para>The ten abreast are a FLOOR and not the dimension: ten bodies at one body-width each want 14 du
    /// of glass, and a room built to exactly that is a corridor turned sideways with ten people wedged in
    /// it. The owner asked for the hat to be wider than the minimum in so many words — <i>"by making the
    /// T-character wider hatted"</i> — and the honest width for a T is the one the T itself states: the
    /// crossbar is the length of the stem. That is a proportion anybody can read off the drawn room, it
    /// leaves the ten abreast half the glass to themselves, and the slack is exactly the floor the vending
    /// cafeteria stands on.</para>
    ///
    /// <para>Written as <see cref="LengthDu"/> and not as 24, so a stem that is ever re-argued takes its
    /// crossbar with it and the room cannot become two opinions about one shape.</para>
    /// </summary>
    public const double GalleryWidthDu = LengthDu;

    /// <summary>
    /// #1199 · <b>HOW DEEP THE CROSSBAR IS</b> — bedroom-small, and that is the argument.
    ///
    /// <para><see cref="UndergroundComplex.FireCodeSmallRoomDu"/> is the game's one statement of <i>a space
    /// you can cross in two paces</i>. A viewing room is a place you stand in and look OUT of, so its depth
    /// is the one dimension that must not become a hall: two paces from the glass to the back wall is a
    /// gallery, and anything more is a lounge that happens to have a window. The fire code's own number is
    /// therefore the right one to borrow — and borrowing it is not the room claiming the exemption by size,
    /// because the exemption it is let off under is named (<see cref="Exemption"/>) and the room's WIDTH is
    /// three times this.</para>
    /// </summary>
    public const double GalleryDepthDu = UndergroundComplex.FireCodeSmallRoomDu;

    /// <summary>
    /// #1199 · <b>THE CAFETERIA BAND — the inner half, and nothing stands outside it but the binoculars.</b>
    ///
    /// <para>The owner's cafeteria goes on the INNER (east) side, away from the glass: <i>"a small vending
    /// machine cafeteria with a couple of tables there"</i>. Half the depth, so the split is a thing the
    /// player reads rather than a threshold: machines and tables against the back wall, and the whole outer
    /// half left as standing room at the rail, which is what the room is for. The one fixture allowed past
    /// this line is the coin binoculars, because a pair of binoculars anywhere but at the glass is a
    /// telescope pointed at a wall.</para>
    /// </summary>
    public const double CafeteriaBandDu = GalleryDepthDu / 2.0;

    // ── THE WAIT ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1199 · <b>HOW LONG "THEY MUST HAVE FINISHED THE VIEW" IS, AS A FRACTION OF THE ROOM'S OWN WATCH.</b>
    ///
    /// <para><b>It was a quarter — <see cref="Interior.Escort.PatienceFraction"/>, the escort's own — and the
    /// owner played it.</b> The two waits looked like one fact read from both sides, and as a piece of
    /// fiction they still are: the escort stands in a doorway expecting you, and here you stand at a mouth
    /// expecting somebody else. What the sharing missed is that the two are not waited THROUGH the same way.
    /// The escort's hour is the ceiling on a beat the captain ends by walking over — it is how long she will
    /// put up with being ignored, and the ordinary play of it is over in seconds. This one has nothing for
    /// the captain to do: he has already followed them in, they are already not there, and the wait is the
    /// whole of what happens next. <b>Waited out at warp 1, a quarter-watch is an hour of the player's own
    /// evening spent looking at an empty tube</b> — owner, 2026-09-17: <i>"YES, shorten the wait."</i></para>
    ///
    /// <para><b>So it is a fraction of its own, and the escort's has not moved.</b> One eightieth of a watch —
    /// <b>three minutes of the player's clock at warp 1</b>, which is the number this fraction is actually
    /// chosen against: long enough that the captain walks the tube, reaches the rail and has a moment of
    /// standing there before the game admits nobody is coming, and short enough to be played rather than
    /// warped past. It stays a fraction of <see cref="Interior.PatronRota.WatchSeconds"/> rather than becoming
    /// a literal 180, for the reason the escort's own docs give: a wait written as a number stops meaning
    /// anything about a watch the day a watch changes length.</para>
    ///
    /// <para><b>And it now fits where it is spent.</b> The walk is only dealt after last call — past
    /// <see cref="Interior.Egress.LastCallFraction"/> of the shift — which leaves a quarter of a watch of room
    /// on the far side of it. A wait that was itself a quarter-watch filled the whole of that remainder
    /// exactly; a wait of one eightieth sits inside it twenty times over.</para>
    /// </summary>
    public const double WaitFraction = 1.0 / 80.0;

    /// <summary>#1199 · …in seconds, off the rota's own shift — the same derivation
    /// <see cref="Interior.Escort.PatienceSeconds"/> makes from <see cref="WaitFraction"/>'s sibling, and
    /// never a second number.</summary>
    public static double TheWaitSeconds => Interior.PatronRota.WatchSeconds * WaitFraction;

    // ── THE CANON (Fable, verbatim; nothing else authored) ────────────────────────────────────────────

    /// <summary>#1199 · The glyph the card, the note and the later sighting all wear — the EYE the game
    /// already hangs on every watched-from-somewhere beat (<see cref="ReeverObservation.FixedOnYouGlyph"/>),
    /// borrowed and never re-typed. A mark of its own would be the book telling the captain this entry was
    /// the significant one, which is the single thing #1063 forbids it to do.</summary>
    public const string Glyph = ReeverObservation.FixedOnYouGlyph;

    /// <summary>#1199 · <b>WHAT THE BOOK WRITES.</b> Authored (Fable, the issue's own canon block), verbatim,
    /// with the person's printed name substituted and no other word composed.
    ///
    /// <para>Four short sentences and every one of them is something that happened: followed, waited, went
    /// in, nobody. It draws no conclusion, offers no reading and asks no question — the captain is the one
    /// who has to decide what it means, and the book's refusal to help is the whole of THE BOOK NEVER LIES
    /// (#1063) said in the one place the player will come back and re-read.</para></summary>
    public static string NoteLine(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return $"Followed {name} out onto the observation walk. One way in, glass underfoot, the drop below. "
            + "Waited at the mouth. Went in. Nobody there.";
    }

    /// <summary>#1199 · The story card's title, authored, verbatim. It is the room's own name and not an
    /// event: a title that announced what had happened would be the show telling the joke before the
    /// picture.</summary>
    public const string CardTitle = "THE OBSERVATION WALK";

    /// <summary>
    /// #1199 · <b>THE CARD AT THE BLIND END</b>, authored (Fable), verbatim, and it is the ABSENCE.
    ///
    /// <para>It describes the room and then runs out of room to describe. <i>"There is nobody here, and
    /// there is nowhere here to be"</i> is the only claim in it, and it is a claim about GEOMETRY — the one
    /// thing the captain can check by standing where he is standing. Nothing is explained, nothing is
    /// named, and there is no second sentence underneath it, because there is genuinely nothing there.</para>
    /// </summary>
    public const string CardBody =
        "The walk is lit the whole way out. The floor is glass and the drop is under it, and the far end is "
        + "a wall with a rail. There is nobody here, and there is nowhere here to be.";

    /// <summary>#1199 · <b>THE LATER SIGHTING</b>, authored, verbatim — said once, at a counter somewhere
    /// else in the system where this person's rota would have put them anyway.
    ///
    /// <para>Six words, and the last one is the whole beat: <i>unhurried</i>. It does not say they are back,
    /// or that they were ever gone, or that anything is owed an explanation. It reports a person standing at
    /// a counter, which is a thing that happens in every bar in this game every watch.</para></summary>
    public static string CounterLine(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return $"{name}, at the counter. Unhurried.";
    }

    /// <summary>#1199 · What the book files the note UNDER — the person, minted here beside the sentence
    /// that prints their name, which is #741's law and the reason no client file may mint one. So THREADS
    /// stacks the walk with everything else the captain has ever written down about them.</summary>
    public static string Subjects(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return CaseSubjects.Line(CaseSubjects.Person(name));
    }

    /// <summary>#1199 · Every player-facing string this beat publishes — the plate, the card, the note and
    /// the sighting, and there are no others. The <c>AllProse</c> discipline every prose-bearing type in
    /// Core keeps, and the list the reserved-word and pattern-word sweeps walk.</summary>
    public static IEnumerable<string> AllProse(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        yield return Plate;
        yield return CardTitle;
        yield return CardBody;
        yield return NoteLine(name);
        yield return CounterLine(name);
    }

    // ── SPENT ONCE PER UNIVERSE (EmptySeal's shape) ───────────────────────────────────────────────────

    /// <summary>#1199 · How the one spend is written down: the haven and the person, joined by a character
    /// neither of them can contain — <see cref="EmptySeal.Key"/>'s own composition, because a save that
    /// carries one field and answers "was it this one" with a comparison rather than a parse is a save that
    /// cannot drift.</summary>
    public static string Key(string bodyId, string name)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(name);
        return $"{bodyId}|{name}";
    }

    /// <summary>#1199 · <b>IS THIS THE ONE?</b> — asked on the frame the captain reaches the blind end of an
    /// empty walk, and the only place the beat can be spent.
    ///
    /// <para>Two refusals: it has not been spent already (<paramref name="spentOn"/> is null while the
    /// captain still has it to spend, which is most of every voyage), and this is the haven that has the
    /// room. There is deliberately no third condition — no die, no rate, no "one time in six". A rate would
    /// be a second pattern to learn, and a pattern the player can learn is a pattern the player can
    /// test.</para></summary>
    public static bool WouldSpend(string bodyId, string? spentOn)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return spentOn is null && string.Equals(bodyId, HavenId, StringComparison.Ordinal);
    }

    /// <summary>#1199 · Has it been spent at all? The question the tail asks before it ever sets off — after
    /// the beat the person keeps walking routes and is never tailed to a vanishing again, so this is what
    /// turns the whole mechanic back into a man crossing a room.</summary>
    public static bool IsSpent(string? spentOn) => spentOn is not null;

    /// <summary>#1199 · Is this the spend that was made? Asked by the later sighting, so the pulse fires for
    /// the person the captain actually lost and never for somebody else standing at a counter.</summary>
    public static bool IsSpentOn(string? spentOn, string bodyId, string name) =>
        spentOn is not null && string.Equals(spentOn, Key(bodyId, name), StringComparison.Ordinal);

    /// <summary>
    /// #1199 · <b>IS THE LATER SIGHTING STILL OWED?</b> — the second half of "once", and a separate written
    /// fact from the spend rather than a suffix on it.
    ///
    /// <para>Two fields and not one parsed field, deliberately. <see cref="EmptySeal"/>'s own doc argues the
    /// case: a key is worth having because the answer to "was it this one" is a COMPARISON rather than a
    /// parse, and a spend that grew a <c>|seen</c> tail would have thrown that away to save a line in a save
    /// file. So the spend says who, and this says whether the world has already handed them back.</para>
    ///
    /// <para>The sighting is owed exactly once: after it is paid, the walk is a walk, the person keeps
    /// walking routes, and nothing about any of it is ever mentioned again.</para>
    /// </summary>
    /// <param name="spentOn">The key of the one person this captain followed and did not find, or null.</param>
    /// <param name="sightingAt">Where the sighting was already paid, or null while it is still owed.</param>
    public static bool SightingIsOwed(string? spentOn, string? sightingAt) =>
        spentOn is not null && sightingAt is null;
}
