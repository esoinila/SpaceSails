using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

public static partial class UndergroundComplex
{
    /// <summary>The lift shaft's spot — the SAME (x, y) on every floor, so going down is legible and coming
    /// back up is never a search. Sits on the spine corridor at the field's heart.
    ///
    /// <para>#801 · This is THE CAGE now, and it is one of two. Everything that only ever wanted "the lift"
    /// still asks here and still gets the same answer it always did; the ones that mean <b>every way off
    /// this floor</b> ask <see cref="ShaftsOn"/>.</para></summary>
    public static (double X, double Y) ShaftAt(in SurfaceLayout.Field field) =>
        (field.AnchorX + 40, (field.BottomY + field.LandingBandY) / 2.0);

    /// <summary>Half-width of the lift car, and of the shaft cut through every floor.</summary>
    public const double ShaftHalf = 3.0;

    /// <summary>Corridor half-width. Wide enough for the captain and an Old One to pass and for the eye to
    /// read it as a built passage rather than a gap between two walls.</summary>
    public const double CorridorHalf = 3.5;

    // ── #801 · THE BUILDING HAS TWO CARS, AND THEY ARE NOT BESIDE EACH OTHER ──────────────────────────────
    //
    // Owner, 2026-08-09: "that elevator would be so busy it would be packed and never available… it is a
    // choke point, and the whole lab would be too easily guarded by just having the guard posted in front
    // of the one elevator. I want to remove that too-easy plot-to-catch-us plot hole."
    //
    // He is right three times over and the third one is the interesting one:
    //
    //   * TRAFFIC. A facility with a canteen for eighty, twelve growing beds and a goods hoist does not run
    //     on one personnel car. It never did; the building simply never drew the second one.
    //   * PACING. One car is a come-back-here point on every floor. Two at opposite ends of the spine turn
    //     a floor into a route with a decision in it, which is what #775 did for the park one storey up.
    //   * THE POSTED GUARD. This is the plot hole. A single car is a single square somebody stands on, and
    //     no amount of writing around that makes an escape feel earned. Two cars a hundred and seventy du
    //     apart cannot be watched by one person, and the fiction stops needing an excuse.
    //
    // What this is NOT. It is not #719's executive lift (that hangs off a principal apartment, is on no
    // panel, and costs your cover), and it is not #719's service stair. It is the ordinary, boring, second
    // car every real building of this size has, and it ships first because the other two are beats and this
    // is a topology.
    //
    // THE CARD LAW IS UNTOUCHED (§13.5). The service car runs its band and nothing else: no surface, no
    // gate, no way past the seam. A second car that could cross a band boundary would be a way to buy depth
    // without the paper, and depth past the first band is the one thing this game makes you earn.

    /// <summary>#801 · Which of the two cars this is.</summary>
    public enum ShaftKind
    {
        /// <summary>The cage: the one the surface head sits on top of, the one the plate is beside, and the
        /// only one that runs a gate.</summary>
        Cage,

        /// <summary>The goods car at the blind end of the corridor. Same four floors, no surface, no
        /// gate.</summary>
        Service,
    }

    /// <summary>#801 · A car, on the plan. Published from <see cref="ShaftsOn"/> so that a law about "every
    /// way off this floor" has a list to be written against — the same reason <see cref="Hall.Openings"/>
    /// and <see cref="Park.Ways"/> exist, said about the thing a captain leaves by.</summary>
    public readonly record struct Shaft(ShaftKind Kind, double X, double Y)
    {
        /// <summary>What is painted at the car mouth.</summary>
        public string Sign => Kind == ShaftKind.Cage ? CageSign : ServiceCarSign;

        /// <summary>Does this one climb all the way out? Only the cage does, because only the cage has a
        /// hut on the regolith over it (#606).</summary>
        public bool ReachesTheSurface => Kind == ShaftKind.Cage;

        /// <summary>Does this one run the gate to the band below? Only the cage. §13.5 is a law about the
        /// building, not about a car, and the second car may not be a way round it.</summary>
        public bool RunsTheGate => Kind == ShaftKind.Cage;

        /// <summary>Where a captain stands when the doors open — a pace out of the car, on the spine. The
        /// cage's alcove hangs off the spine's upper face and the service car's off the lower one, so the
        /// pace is outward in opposite directions and neither of them is a typed sign.</summary>
        public (double X, double Y) Landing =>
            (X, Kind == ShaftKind.Cage ? Y + 1.0 : Y - 1.0);
    }

    /// <summary>#801 · What is painted at the cage's mouth. The console has said this since #585.</summary>
    public const string CageSign = "\U0001F6D7 LIFT";

    /// <summary>#801 · …and at the other one. It says what it is for and it says what it does not do, in
    /// the inspectorate voice every plate down here is stencilled in — a car with no surface button is a
    /// car a captain has to be told about ONCE rather than discover by pressing.</summary>
    public const string ServiceCarSign = "\U0001F6D7 GOODS CAR 2 · THIS BAND ONLY";

    /// <summary>#801 · What the service car's panel says under its title, in place of the cage's own line.
    /// It names where the other car is, which is the whole of the anti-choke feature said in a sentence:
    /// a captain who finds one car has been told there is another and roughly where.</summary>
    public const string ServiceCarPanelLine =
        "The goods car. It runs these floors and it does not climb out: for the surface, and for anything "
        + "below this band, the cage is at the other end of the corridor.";

    /// <summary>#801 · How much clear corridor a car's alcove wants either side of itself before the ground
    /// counts as taking one.</summary>
    public const double ShaftClearDu = 1.5;

    /// <summary>#801 · THE WIDEST A CHAMBER EVER GETS in this building — the found band's deepest floor
    /// (<see cref="FoundGrowthPerFloor"/> compounded across a band), and the reason it is here rather than
    /// beside the growth constant: a car stands in the SAME place on every floor of a site, so the ground
    /// it needs has to be clear of the biggest room the site can produce and not merely of this floor's.
    /// A number worked out per floor would have put the second car in solid chamber four storeys down.</summary>
    public static double DeepestRoomScale => Math.Pow(FoundGrowthPerFloor, FloorsPerShaft - 1);

    /// <summary>#801 · How far apart the two cars must be before the building counts as having two. Stated
    /// as a share of the spine it is measured on rather than as a distance, because "far enough that one
    /// person cannot watch both" is a fact about the corridor's length and not about deck units. A third of
    /// the main corridor is a walk with two cross-corridors and the length of the hall in it.</summary>
    public static double MinShaftSeparationOn(in SurfaceLayout.Field field)
    {
        double margin = SurfaceLayout.EdgeMargin + 6;
        return ((field.RightX - margin) - (field.LeftX + margin)) / 3.0;
    }

    /// <summary>#801 · WHERE EVERY CROSS CORRIDOR IS, as a pure function of the ground.
    ///
    /// <para>This was five lines inside <see cref="Build"/> and it had to come out: the second car is
    /// placed where no rib and no rib's chambers can reach, and a placer that worked that out from its own
    /// copy of the rib arithmetic would be the mirrored constant this file keeps a table of. One list, and
    /// <see cref="Build"/> reads it too.</para>
    ///
    /// <para>The x's are the same on every floor of every site — only which WAY a rib runs is seeded — so a
    /// spot chosen against them is a spot that holds for the whole building.</para>
    ///
    /// <para><b>The ordinal is the SLOT the field offered, not the survivor's place in this list</b>, and it
    /// is carried out of here for exactly one reason: a rib's direction is seeded on it. The slot the lift
    /// stands in is dropped, so the ordinals a real building uses have a hole in them — and a caller that
    /// re-numbered from zero would silently re-roll which way every corridor in the game runs. That is the
    /// same class of mistake as #587's out-of-order sweep: the arithmetic is right and the INDEX is not.</para></summary>
    public static IReadOnlyList<(int Ordinal, double X)> RibColumnsOn(in SurfaceLayout.Field field)
    {
        double margin = SurfaceLayout.EdgeMargin + 6;
        double left = field.LeftX + margin, right = field.RightX - margin;
        (double shaftX, _) = ShaftAt(field);

        var xs = new List<(int, double)>();
        const int ribs = 5;
        for (int i = 0; i < ribs; i++)
        {
            double t = (i + 0.5) / ribs;
            double rx = Lerp(left + 16, right - 16, t);
            if (Math.Abs(rx - shaftX) < ShaftHalf + CorridorHalf + 4)
            {
                continue;   // never run a rib through the lift
            }
            xs.Add((i, rx));
        }
        return xs;
    }

    /// <summary>
    /// #801 · WHERE THE SECOND CAR STANDS, or null where this ground will not take one.
    ///
    /// <para>At the blind end of the main corridor: the stretch past the outermost cross corridor, which is
    /// the one length of spine in the building that no chamber can ever reach — every room down here hangs
    /// off a rib, so the ground beyond the last rib's own column is ground nothing will ever be laid in.
    /// That is also exactly where a goods car goes in a building anybody has ever worked in.</para>
    ///
    /// <para><b>The end FURTHER from the cage</b>, and then only if what is left is still a third of the
    /// corridor away from it (<see cref="MinShaftSeparationOn"/>). Two cars a captain can see at once are
    /// one car drawn twice, and the whole point of the feature is that they cannot both be watched.</para>
    ///
    /// <para><b>Null is a real answer.</b> A field whose ribs run out to its own end caps has no blind end,
    /// and this returns null rather than putting a car through a chamber — which is what makes the choke
    /// law provable: it binds where the generator admits two and says nothing where it does not.</para>
    /// </summary>
    public static (double X, double Y)? ServiceShaftAt(in SurfaceLayout.Field field)
    {
        IReadOnlyList<(int Ordinal, double X)> ribs = RibColumnsOn(field);
        if (ribs.Count == 0)
        {
            return null;
        }

        (double cageX, double cageY) = ShaftAt(field);
        double margin = SurfaceLayout.EdgeMargin + 6;
        double left = field.LeftX + margin, right = field.RightX - margin;

        // How far a rib's own chambers reach along the spine, at the biggest a chamber ever gets. The 1.5
        // is the claim ledger's own inflation, so this is the room's keep-out and not the room.
        double reach = CorridorHalf + (RoomWidthDu * DeepestRoomScale) + 1.5;
        double clear = ShaftHalf + ShaftClearDu;

        (double Lo, double Hi)[] ends =
        [
            (left + clear, ribs[0].X - reach - clear),
            (ribs[^1].X + reach + clear, right - clear),
        ];

        // ── #813 · WHICH END, AND THE RING DECIDES IT ────────────────────────────────────────────────────
        //
        // Owner's Manhattan ruling, clause 4: "the extra lift goes on the LESS-BUILT side of the park — the
        // ring's density decides the shaft's side, not the other way around."
        //
        // So the two blind ends are no longer ranked by how far they are from the cage. They are ranked by
        // how much ROOM FRONTAGE the block carries at that end (RingFrontageOn), and the thinner end wins.
        // Distance from the cage is what breaks a tie, which is where the old rule went — it was never
        // wrong, it was only the only thing being asked.
        //
        // This is a real measurement and it really does choose: on the shipped field the west half of the
        // block carries 91 du of frontage and the east half 98, because the rib column nearest the cage is
        // dropped (RibColumnsOn) and the gates therefore fall to the west of the park's middle while the
        // long unbroken run of suites falls to the east. Mirror that asymmetry and the car moves; the guard
        // in TheParkIsTheCentreOfTheBlockTests does exactly that.
        (double westFrontage, double eastFrontage) = RingFrontageOn(field);
        ParkBlock block = BlockOn(field);

        // …and WHERE in that end. Hard against the block's own service street, which is where a goods
        // vehicle stops in any building anybody has ever worked in — clamped into the blind end, because
        // the blind end is a fact about the chambers and this is a preference about the streets.
        (double Lo, double Hi, double Frontage, double Want)[] candidates =
        [
            (left + clear, ribs[0].X - reach - clear, westFrontage,
                block.WestOuterX - ShaftHalf - ShaftClearDu),
            (ribs[^1].X + reach + clear, right - clear, eastFrontage,
                block.EastOuterX + ShaftHalf + ShaftClearDu),
        ];

        double bestX = double.NaN, bestFrontage = double.MaxValue, bestGap = -1;
        foreach ((double lo, double hi, double frontage, double want) in candidates)
        {
            if (hi <= lo)
            {
                continue;   // the ribs run all the way to the cap: no blind end on this side
            }
            double x = Math.Clamp(want, lo, hi);
            double gap = Math.Abs(x - cageX);
            if (frontage < bestFrontage - 0.001 || (frontage < bestFrontage + 0.001 && gap > bestGap))
            {
                (bestX, bestFrontage, bestGap) = (x, Math.Min(frontage, bestFrontage), gap);
            }
        }

        return double.IsNaN(bestX) || bestGap < MinShaftSeparationOn(field) ? null : (bestX, cageY);
    }

    /// <summary>#801 · EVERY WAY OFF THIS FLOOR THAT IS A CAR. The cage first — it is the one the surface
    /// sits on and the one every older law means by "the lift" — then the goods car where the ground took
    /// one.
    ///
    /// <para>Published so that "no floor of a clandestine site has exactly one way off it" is a law that
    /// can be written down and can go red, instead of an arrangement two placers happen to agree on.</para></summary>
    public static IReadOnlyList<Shaft> ShaftsOn(in SurfaceLayout.Field field)
    {
        (double cageX, double cageY) = ShaftAt(field);
        var cars = new List<Shaft> { new(ShaftKind.Cage, cageX, cageY) };
        if (ServiceShaftAt(field) is { } service)
        {
            cars.Add(new Shaft(ShaftKind.Service, service.X, service.Y));
        }
        return cars;
    }

    // ── #600 · THE PANEL, BECAUSE THE CAR ONLY WENT DOWN ────────────────────────────────────────────────
    //
    // Owner, on B1: "looks like the elevator only takes me down... how do I get back to the surface with it
    // :-D Am I marooned in a secret lab underground now :-D ?" — then: "we should have elevator panel with
    // UI then".
    //
    // He was not marooned, but only by luck. `HiveLiftInteract` had ONE action and it always descended; the
    // car returned to the surface solely when pressed at the bottom of the band. Getting out of B2 on a
    // twenty-floor site therefore meant riding eighteen floors DEEPER first, on the tank, through dead air.
    // The file's own comment says a captain trapped on a dead floor is a death, and the lift was the thing
    // doing the trapping.
    //
    // It survived #590, #591 and #592 all editing that function because none of them asked what the UP case
    // did, and the A* audit cannot see a state machine — it proves you can REACH the lift, never that the
    // lift is a way HOME. That seam is where this hid.
    //
    // The fiction already had the answer written down: `EndOfTheLineLine` says "the panel has no button
    // below B{n}", which means there is a panel with buttons on it. So there is.

    /// <summary>One button on the lift panel.</summary>
    /// <param name="Level">The floor it goes to; 0 is the surface.</param>
    /// <param name="Name">What is written on the button.</param>
    /// <param name="Pressurised">Whether that floor still holds air — the panel says so, because it is the
    /// single fact that decides whether the trip is free. <b>#802 · Every row asks
    /// <see cref="HoldsPressure"/>, the SURFACE row included.</b> Nothing on this panel may type its own
    /// answer: a hand-written <c>true</c> is how the way out came to promise air on airless ground.</param>
    /// <param name="IsCurrent">The floor the car is on now: shown, and not a destination.</param>
    /// <param name="Refusal">Null when the button works. When set, the button is PRESENT and says why it
    /// will not — an absent button and a broken one look identical, and this ground has already shipped that
    /// mistake once.</param>
    /// <param name="OpenedBy">#689 · The title of the card in the captain's own wallet that this stop's gate
    /// will read — null on every ordinary button, and null at a gate no card opens. The positive twin of
    /// <paramref name="Refusal"/>: a sealed row says what is missing, and this one says what is HELD, before
    /// the ride rather than after it. Core decides it so the panel can never promise a reading the gate will
    /// not give (#600's rule: Core decides, the razor draws).</param>
    /// <param name="OpenedByChit">#752 · WHICH paper is doing it. Set only when the thing in the wallet that
    /// opens this gate is the day-labour chit rather than the countersignature card, because the two are read
    /// by the gate in completely different voices — one is an office still obeying an office nobody can find,
    /// the other is a tired man reading a timesheet. The row draws the same either way; the ARRIVAL does not,
    /// and the ride carries the stop with it, so the discrimination belongs on the stop.</param>
    public readonly record struct LiftStop(
        int Level, string Name, bool Pressurised, bool IsCurrent, string? Refusal, string? OpenedBy = null,
        bool OpenedByChit = false);

    /// <summary>
    /// #600 · What this car's panel offers, standing on <paramref name="level"/>.
    ///
    /// <para><b>SURFACE is always on it.</b> That is the whole bug fix: from any floor, the way out must
    /// never require travelling further in.</para>
    ///
    /// <para>Then every floor of THIS car's band that the site actually has. A car serves a band and no
    /// further (#585) — the way deeper is a different shaft — so the band below appears only as the single
    /// gated button described next.</para>
    ///
    /// <para><b>#590 · the gate.</b> If a band exists below this one, the button for it is present and
    /// refuses by name unless the captain holds its authority card.</para>
    ///
    /// <para><b>#592 · the silence.</b> With one exception: if the band below is the one the building does
    /// not admit to, the button is not there at all unless the card is already held. A refusal that names a
    /// shaft would announce the secret in the one sentence it cannot survive — so on the last listed floor
    /// the panel looks exactly like the panel at the true bottom of an ordinary site.</para>
    ///
    /// <para><b>#752 · the chit.</b> The gate off the FIRST band — the cage, the one the day crew rides —
    /// also reads the day-labour chit, if the captain went and got hired for it. See the block inside.</para>
    /// </summary>
    /// <param name="carried">#752 · The satchel itself, so the panel can ask <see cref="CanteenTable.Cover"/>
    /// whether the captain has a reason to be in the cage. The COVER state is the chit's own PRESENCE (#746)
    /// and is read here rather than re-derived: a second spelling of "has cover" is the thing that drifts
    /// from what the player is carrying. Null is simply an empty satchel, so every older caller is unchanged.
    /// </param>
    public static IReadOnlyList<LiftStop> LiftPanel(
        string bodyId, int level, IReadOnlyCollection<string> heldCardIds,
        IReadOnlyList<Satchel.Item>? carried = null) =>
        LiftPanel(bodyId, level, ShaftKind.Cage, heldCardIds, carried);

    /// <summary>
    /// #801 · The same panel, asked of a CAR rather than of a building.
    ///
    /// <para>The cage's panel is the panel this game has always had, to the last string — every older caller
    /// reaches it through the overload above and nothing about it moved.</para>
    ///
    /// <para>The goods car's is the same four floors and <b>nothing else</b>: no SURFACE row, because the
    /// only hole with a hut on top of it is the cage's (#606), and no gate row, because §13.5 is a law about
    /// the BUILDING and a second car may not be a way to buy depth without the paper. That is also what makes
    /// the pair worth walking between rather than interchangeable: within a band either car will do, and the
    /// moment you want to leave the band you want the cage, which is at the other end of the corridor.</para>
    ///
    /// <para>Written as one method with one clause in it rather than as two panels, because two panels is two
    /// answers to "which floors does this site have on this band" and the second one goes stale the first
    /// time a band learns a new shape (§13.7 and §13.20 have each taught this file that once).</para>
    /// </summary>
    public static IReadOnlyList<LiftStop> LiftPanel(
        string bodyId, int level, ShaftKind car, IReadOnlyCollection<string> heldCardIds,
        IReadOnlyList<Satchel.Item>? carried = null)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(heldCardIds);

        // #802 · THE SURFACE ROW ASKS, LIKE EVERY OTHER ROW. It used to say `Pressurised: true` — a literal,
        // typed once, on the one button every captain presses on the way out — and the panel therefore drew
        // `🫁 air` over SURFACE and titled it "holds pressure" while the sim spent tank on that exact ground
        // from the first step. The drain never believed it (SuitAir.SourceOf hands back Tanks at level 0),
        // the plate by the car never said it, and the card in the wallet says "there is no air on this moon
        // to help you" — so this was the house bug class in its purest form: a SENTENCE reporting one world
        // while the sim runs another, kept alive because the row nobody doubted was the row nobody asked.
        //
        // #801 · …and only the CAGE offers it at all. There is one hut on the regolith (#606) and it stands
        // over one hole; the goods car does not climb out, so the row is not drawn rather than drawn and
        // refused. #802's law is untouched — the row that exists still ASKS.
        var stops = new List<LiftStop>();
        if (car == ShaftKind.Cage)
        {
            stops.Add(new(0, "SURFACE", HoldsPressure(bodyId, 0), IsCurrent: level >= 0, Refusal: null));
        }

        int band = BandOf(Math.Min(level, -1));
        int deepest = BandFloor(bodyId, band);
        for (int f = BandTop(band); f >= deepest; f--)
        {
            stops.Add(new(f, NameOf(bodyId, f), HoldsPressure(bodyId, f), f == level, null));
        }

        if (car != ShaftKind.Cage)
        {
            // The goods car's panel ends here, and it ends in SILENCE rather than in a button that refuses.
            // A refusing row is right where a gate exists and the paper is missing (#590); there is no gate
            // in this shaft at all, and a row that said so would be an affordance a captain re-presses every
            // floor for the rest of the excursion. What the car is and is not is said once, at the top of
            // the panel, by ServiceCarPanelLine.
            return stops;
        }

        // #677 · The next shaft that EXISTS. Under the band nobody listed there is a whole band with nothing
        // dug in it, so `band + 1` would have the panel refusing — by name, in a sentence — to take the
        // captain to solid rock, and a card minted for it would authorise a hole.
        if (NextShaftBelow(bodyId, level) is not { } next)
        {
            return stops;   // nothing under this shaft at all; the panel simply ends
        }

        // #411 · THE CAR ANSWERS. A branch office's card opens exactly one band, and the way down is a piece
        // of paper somebody left in a room. The head office asks the captain for nothing at all, on any
        // floor — not because it is careless, but because a hull that is on the board is expected and the
        // building has never had any other kind of visitor. The gate is simply ABSENT, and the absence is
        // the rank difference: the same panel, and only one of them negotiates.
        var gateCard = new AuthorityCard(bodyId, next);
        bool carded = heldCardIds.Contains(gateCard.Id);
        bool holdsIt = IsHeadOffice(bodyId) || carded;

        // #592/#677 · Two different silences, one rule. The building does not admit the unlisted band exists,
        // so its panel does not either; and NOTHING admits the halls exist, least of all a lift directory.
        // A refusal that named either shaft would give the secret away in the one sentence it cannot survive,
        // so on both of those floors the panel looks exactly like the panel at the bottom of an ordinary site.
        bool undeclared = IsUnlisted(bodyId, BandTop(next)) || IsFound(bodyId, BandTop(next));
        if (undeclared && !holdsIt)
        {
            return stops;
        }

        // ── #752 · AND THE OTHER PAPER, WHICH IS NOT A CLEARANCE AT ALL ─────────────────────────────────
        //
        // Owner, playing #748 to its promised end: the Hand hands over the chit with "take this to the lift
        // and don't be clever near the counter", and the lift had never heard of it. The sentence the job was
        // hired to finish stopped one door short of the door it was about.
        //
        // Two papers, two doors, one gate. The countersignature card is a CLEARANCE — an office that stopped
        // existing still vouching for whoever holds it — and it keeps every band it ever opened, untouched
        // below. The chit is COVER: a name on the cage crew's list, worth exactly the trip the cage makes.
        // So it opens the gate off the FIRST band and nothing else. That is not caution about scope, it is
        // what the paper says: a day-labour chit is a reason to be in the cage, never clearance to the rest
        // of a building whose gates answer to an office nobody can find.
        //
        // And it never breaks the two silences above, because it cannot reach them: this runs after the
        // undeclared band has already returned empty-handed. A chit is a job somebody wrote you down for,
        // and nobody writes day labour onto a floor the building denies having.
        bool chitOpens = !holdsIt
            && BandOf(Math.Min(level, -1)) == 0
            && CanteenTable.Cover.Held(carried);
        bool opens = holdsIt || chitOpens;

        stops.Add(new(
            BandTop(next),
            opens ? "↓ THE OTHER SHAFT" : "↓ THE OTHER SHAFT — SEALED",
            HoldsPressure(bodyId, BandTop(next)),
            IsCurrent: false,
            opens ? null : "This car does not go lower. The shaft that does is on this floor, and its " +
                "gate wants an authority this building has not issued in a long time.",
            // #689 · …and when the wallet has the answer in it, the row says so BEFORE the ride. Owner, after
            // playing the whole loop: "It was locked until I got it ... there was no story point about it
            // being needed or used." Never at the head office: there is no gate there to read anything, and
            // that absence is the rank difference (#411) rather than an oversight worth papering over.
            //
            // #752 · …or the chit, in its own printed words, wearing the glyph the satchel row wears. The
            // card wins where both are carried: it is the deeper permission, it opens this gate and every
            // other one, and a captain who found it should be told about THAT paper. One row per floor —
            // the panel is a set of buttons and a button that appeared twice would be a building with two
            // of the same door in it.
            carded && !IsHeadOffice(bodyId) ? CardTitle(gateCard)
                : chitOpens ? $"{CanteenTable.ChitGlyph} {CanteenTable.ChitTitle}" : null,
            OpenedByChit: chitOpens));
        return stops;
    }

    /// <summary>#689 · WHICH GATE A RIDE GOES THROUGH — the card it reads, or null for an ordinary trip.
    ///
    /// <para>Owner, having played the whole loop on a deep site: <i>"It was locked until I got it ... there
    /// was no story point about it being needed or used."</i> Half of that is a beat said at the wrong
    /// moment (the client's job); this is the other half, and it is arithmetic, so it belongs where a test
    /// can reach it.</para>
    ///
    /// <para>The client used to derive it as <c>BandOf(min(Floor, -1)) + 1</c> — the band under the floor
    /// the press came FROM — which answers a question nobody asked. Whether a ride crosses a gate is a fact
    /// about the STOP, so this asks the panel: is the button being pressed one that is only on it because
    /// the captain is carrying the paper for it? That single question also settles two cases the old
    /// arithmetic got wrong, because it never looked at a card at all:</para>
    /// <list type="bullet">
    /// <item>the head office, whose gate is deliberately ABSENT (#411) — it used to narrate a
    /// countersignature being read by a door that is not there;</item>
    /// <item>any caller that is not the refusing panel — the old rule was right only because <i>its one
    /// caller</i> returned early on a refusal, and a rule that is right because of where it is called from
    /// is a rule waiting for its second caller.</item>
    /// </list></summary>
    /// <param name="carried">#752 · The satchel, so the panel asked here is the panel the captain pressed —
    /// a chit row exists only on a panel that was shown the wallet, and a rule that reads a DIFFERENT panel
    /// than the one that was pressed is the seam this function was written to close.</param>
    public static AuthorityCard? GateOpenedByRidingTo(
        string bodyId, int fromLevel, int toLevel, IReadOnlyCollection<string> heldCardIds,
        IReadOnlyList<Satchel.Item>? carried = null)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(heldCardIds);

        foreach (LiftStop stop in LiftPanel(bodyId, fromLevel, heldCardIds, carried))
        {
            // #752 · …and it is a CARD that is being read, not the day-labour chit. Both papers put a title
            // in OpenedBy, and only one of them is a countersignature; a ride the chit opened must not
            // narrate an office vouching for the captain, because no office did.
            if (stop.Level == toLevel && stop.OpenedBy is not null && !stop.OpenedByChit)
            {
                return new AuthorityCard(bodyId, BandOf(toLevel));
            }
        }
        return null;
    }
}
