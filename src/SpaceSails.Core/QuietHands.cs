using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #1068 · <b>THROUGH PEOPLE WHO DO NOT KNOW WHY</b> — the third of the watchers' three manifestation
/// channels, and the third customer of <see cref="DisclosureClock"/>. #1063's burial delivered this
/// channel's third act (the works notice, the mason, the cheerful rag line); these are the two mundane
/// deliveries it left standing: <b>a berth reassigned</b> and <b>a moon repriced overnight</b>.
///
/// <para><b>THE SCULLY LAW (#672, binding, owner-blessed 2026-09-01).</b> <i>"We may show wonders, but a
/// Scully must always be able to plausibly say no. Any single watcher act must have a mundane reading that
/// a reasonable person can hold. The moment an act is only explicable by the watchers, it is spent — and we
/// spend exactly one, at the end."</i> Neither act here is that one, and neither is close to it. A harbour
/// moves a ship to a different slot every day of its working life, for a hundred reasons that are all about
/// somebody else's schedule; and a pump's price moving by the same credit the belt markup already moves it
/// by is not an event at all, it is Tuesday.</para>
///
/// <para><b>THE LAWS FROM #672, AND WHERE EACH IS KEPT:</b></para>
/// <list type="bullet">
/// <item><b>No stats.</b> Nothing here is a number a captain is shown as a reading. The berth is a SLOT —
/// the ship is tied up somewhere else round the same station — and the repricing is a line on a receipt he
/// was going to be handed anyway.</item>
/// <item><b>No sensor return.</b> Nothing is measured, nothing is reported, no instrument is involved. Both
/// acts are performed by people, in the ordinary course of their work, and neither of them knows why.</item>
/// <item><b>No art of THEM ever.</b> Nothing here is drawn. The berth is the berth the docking code has
/// drawn since #269; the price is the pump price the trade desk has printed since #157.</item>
/// <item><b>No farmable trigger.</b> Nothing in any signature here is effort: not a visit count, not a die
/// the player can re-roll, not a dock count. It is a fact about WHEN the captain went, read off the clock's
/// own register — law four of <see cref="DisclosureClock"/>, carried into its third customer. Docking ten
/// times changes nothing; the berth moves once and the price moves once.</item>
/// <item><b>No explaining dialog.</b> <b>This type publishes no prose at all</b> — no label, no line, not
/// one string — which is the strongest available form of that law and settles §8 for free, since a type
/// with no strings in it cannot contain the reserved word. Swept by reflection in
/// <c>ThePeopleWhoDoNotKnowWhyTests</c>, exactly as the clock's own guard sweeps the clock and
/// <c>TheWorldDeclinesPolitelyTests</c> sweeps <see cref="PoliteDecline"/>.</item>
/// </list>
///
/// <para><b>ONE REGISTER, TWO DELIVERIES, ON PURPOSE</b> — <see cref="PoliteDecline"/>'s own argument, and
/// it is stronger here: both acts are performed by the SAME harbour, on the same paperwork, on the same
/// morning. Two registers on one schedule would be the mirrored-constant bug this ground keeps a table of,
/// said about a fact instead of a number.</para>
///
/// <para><b>AND THE PORT IS ONE RULE, ASKED TWICE.</b> A ground in this game has no market of its own — an
/// ordinary moon shares its planet's depot (<see cref="TrafficSchedule.GenerateDepots"/>'s own words) and
/// has no berths to let. So both deliveries land at <see cref="PortFor"/>: the harbour that serves this
/// ground, which is the busiest berth in the ground's own neighbourhood. That is where the clerk who
/// reassigns the berth sits, and it is where the price the captain pays for having been out there is
/// printed. A second rule for the second delivery would be two harbours that could disagree about which
/// one serves a moon.</para>
/// </summary>
public static class QuietHands
{
    // ── THE THRESHOLD, AND ITS REASON WRITTEN BESIDE IT ──────────────────────────────────────────────────
    //
    // DisclosureClock's own docblock says what a customer owes it: "every beat that reads it chooses its own
    // threshold and writes that threshold's reason down beside its own words." These are those words.

    /// <summary>#1068 · How many WHOLE world-side windows must have passed since the ground was opened
    /// before the paperwork moves. <b>One</b> — the burial's number and the decline's number, and
    /// deliberately the same one: <b>the watchers act on the schedule the neighbours do.</b>
    ///
    /// <para>Never on the visit that opened the ground, and here the reason is the plainest of the three:
    /// a berth reassigned and a price rewritten are <i>overnight</i> acts in the owner's own phrasing. A
    /// roster that had already been retyped by the time the captain climbed back out of the seam he had
    /// just crossed would be a decision taken about him, inside the hour, by an office that was watching him
    /// take it — which is a sensor return with a desk in front of it.</para>
    ///
    /// <para>Read off <see cref="DisclosureClock.WindowsSince(DisclosureClock.Opening, long)"/> and never
    /// re-derived: the window is the monolith's own, asked through the clock.</para></summary>
    public const long WindowsBeforeTheHandMoves = 1;

    /// <summary>#1068 · Is this ground due — <b>at this moment, ignoring where the captain is standing</b>.
    /// One whole window, per <see cref="WindowsBeforeTheHandMoves"/>.</summary>
    public static bool IsDue(DisclosureClock.Opening opening, long window) =>
        DisclosureClock.WindowsSince(opening, window) >= WindowsBeforeTheHandMoves;

    // ── THE REGISTER ─────────────────────────────────────────────────────────────────────────────────────
    //
    // Two numbers beside the id, and both are load-bearing.
    //
    // THE WINDOW, because both deliveries are CHOSEN against it — which berth, and which way the price went
    // — and a choice needs something stable to be chosen against. A reload that forgot it would hand the
    // captain a different slot and a price that had moved the other way, and a price that walks about
    // between two visits is not weather, it is an event.
    //
    // THE SPENT FLAG, because the berth is handed over ONCE. The world's window is 4,000 sim-seconds
    // (Monolith.EpochSeconds) and a voyage between two planets is days, so a reassignment that expired with
    // its own window would expire somewhere out in the dark and never be handed to anybody. It is marked
    // when the captain actually ties up, which is also what makes it un-farmable in the only direction that
    // was ever open: the second clamp at that port is the ordinary berth again, and so is the tenth.
    //
    // The repricing carries no such flag and must not: a price that reverted the moment it had been paid
    // once would be a price that is watching the captain's wallet.

    /// <summary>#1068 · One ground the harbour has done its ordinary paperwork about. Nothing else is kept:
    /// not who, not which berth (that is derived), not what the price did (also derived).</summary>
    /// <param name="BodyId">The ground whose halls were opened.</param>
    /// <param name="Window">The world-side window the paperwork moved in
    /// (<see cref="DisclosureClock.WindowAt"/>).</param>
    /// <param name="BerthGiven">Whether the reassigned berth has already been handed over — see the register
    /// notes above for why this is kept and the repricing has no twin of it.</param>
    public readonly record struct Hand(string BodyId, long Window, bool BerthGiven);

    private static IReadOnlyList<Hand> _hands = [];

    /// <summary>#1068 · The grounds the harbour has moved paperwork about. Empty in every world where nobody
    /// has been past a seam long enough ago, which is almost every world.</summary>
    public static IReadOnlyList<Hand> Handled => _hands;

    /// <summary>#1068 · Install the register — the ONE writer, called by whoever owns the save, exactly as
    /// <see cref="Burial.Install"/> and <see cref="PoliteDecline.Install"/> are. Null and empty are the same
    /// answer: nobody's paperwork has moved anywhere.
    ///
    /// <para>Tests restore what they installed in a <c>finally</c>, and install grounds of their OWN — the
    /// register only ever changes the answer for the ids in it, and that is what makes an ambient safe here
    /// rather than merely convenient. <see cref="PoliteDecline.Install"/> records what it cost to learn
    /// that: a guard that installs a SHIPPED id here is moving a berth under somebody else's audit.</para>
    /// </summary>
    public static void Install(IReadOnlyList<Hand>? hands) => _hands = hands ?? [];

    /// <summary>#1068 · <b>THE EVENT.</b> Which of the grounds this captain has opened the harbour has done
    /// its paperwork about by now — folded into the register, and the register handed back <b>by
    /// reference</b> when there is nothing to add, so a caller can compare and only then ask for a save.
    ///
    /// <para><b>Two conditions, the burial's own two, for the burial's own reasons:</b> a whole window has
    /// passed since the opening (<see cref="WindowsBeforeTheHandMoves"/>), and <b>the captain is not on that
    /// body</b>. The second matters differently here than it does for a door: nobody is claiming the clerk
    /// waits for the captain to leave. It is that the acts are things he comes back to FIND — a slot that is
    /// not the one he had, a price that is not the one he paid — and an act with a witness is a thing he
    /// could describe.</para>
    ///
    /// <para>Nothing here is effort, a die, or a visit count.</para></summary>
    /// <param name="register">The disclosure clock's register of opened grounds.</param>
    /// <param name="hands">What the harbour has already moved.</param>
    /// <param name="standingOn">The body the captain is on right now, or null when he is on none.</param>
    /// <param name="simTime">Sim seconds — no clock is read in Core.</param>
    public static IReadOnlyList<Hand> Note(
        IReadOnlyList<DisclosureClock.Opening>? register,
        IReadOnlyList<Hand>? hands,
        string? standingOn,
        double simTime)
    {
        IReadOnlyList<Hand> had = hands ?? [];
        if (register is not { Count: > 0 })
        {
            return had;
        }

        long window = DisclosureClock.WindowAt(simTime);
        List<Hand>? next = null;
        foreach (DisclosureClock.Opening opening in register)
        {
            if (!IsDue(opening, window))
            {
                continue;
            }
            if (string.Equals(opening.BodyId, standingOn, StringComparison.Ordinal))
            {
                continue;   // not while he is standing on it
            }
            if (HandOn(had, opening.BodyId) is not null
                || (next is not null && HandOn(next, opening.BodyId) is not null))
            {
                continue;   // already done, and a harbour files a ground once
            }
            next ??= [.. had];
            next.Add(new Hand(opening.BodyId, window, BerthGiven: false));
        }
        return next ?? had;
    }

    /// <summary>#1068 · This ground's row, or null where the harbour has filed nothing about it — asked of a
    /// register the caller holds.</summary>
    public static Hand? HandOn(IReadOnlyList<Hand>? hands, string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        foreach (Hand h in hands ?? [])
        {
            if (string.Equals(h.BodyId, bodyId, StringComparison.Ordinal))
            {
                return h;
            }
        }
        return null;
    }

    /// <summary>#1068 · The same, asked of the register that is installed — the form the docking code and
    /// the trade desk use, because neither of them has any business being handed a save.</summary>
    public static Hand? HandOn(string bodyId) => HandOn(_hands, bodyId);

    /// <summary>#1068 · <b>Has the harbour filed anything about this ground?</b> False everywhere in a world
    /// where nobody has been past a seam long enough ago, which is almost every world.</summary>
    public static bool On(string bodyId) => HandOn(bodyId) is not null;

    // ── THE PORT THAT SERVES A GROUND ────────────────────────────────────────────────────────────────────

    /// <summary>#1068 · <b>Which harbour does this ground's paperwork?</b> The busiest dockable berth in the
    /// ground's own neighbourhood — itself, its parent, and everything else under that parent — or null
    /// where the ground has no harbour in its system at all, in which case nothing whatever happens and that
    /// is the correct answer: there is nobody out there to reassign anything.
    ///
    /// <para><b>Three borrowed rules and no new ones.</b> The neighbourhood is
    /// <see cref="ArrivalTube.Neighbourhood"/>'s — <i>"the system this berth is in"</i>, the same set that
    /// decides a tube's tier, so "which port serves that moon" means the same thing to the docking picture
    /// and to this. Dockability is <see cref="DockableHavens.IsDockable"/>'s, so nothing here has an opinion
    /// about what a berth is. Busiest is <see cref="ArrivalTube.ScheduledTonnage"/>'s, which is the scenario's
    /// own traffic model and never an authored ranking.</para>
    ///
    /// <para><b>No sim time anywhere in it.</b> The neighbourhood is structural and the tonnage is a
    /// timetable, so the answer is the same on the outbound leg and the return — which it must be, or the
    /// berth reassigned in one window would be owed at a different port in the next. Ties fall to the
    /// smaller id, ordinally, so a two-haven system answers the same on every machine.</para>
    ///
    /// <para><b>The berth itself comes back, not its id.</b> Which port serves a moon is the one thing
    /// either of this channel's types could have been tempted to publish as a string, and the no-prose law
    /// is worth more without a carve-out in it than with one — see <c>ThePeopleWhoDoNotKnowWhyTests</c>'s
    /// sweep. Every caller wanted the body anyway.</para></summary>
    public static CelestialBody? PortFor(ICelestialEphemeris ephemeris, string groundId)
    {
        ArgumentNullException.ThrowIfNull(ephemeris);
        ArgumentNullException.ThrowIfNull(groundId);

        IReadOnlySet<string> neighbourhood = ArrivalTube.Neighbourhood(ephemeris, groundId);
        CelestialBody? best = null;
        double bestTonnage = double.NegativeInfinity;

        foreach (CelestialBody body in ephemeris.Bodies)
        {
            if (!DockableHavens.IsDockable(body) || !neighbourhood.Contains(body.Id))
            {
                continue;
            }

            double tonnage = ArrivalTube.ScheduledTonnage(ephemeris, body.Id);
            if (best is null || tonnage > bestTonnage
                || (tonnage == bestTonnage && string.CompareOrdinal(body.Id, best.Id) < 0))
            {
                best = body;
                bestTonnage = tonnage;
            }
        }

        return best;
    }

    // ── DELIVERY 1 · A BERTH REASSIGNED ──────────────────────────────────────────────────────────────────
    //
    // The harbour hands the captain a different slot than the one it has always given him. No fault, no
    // fee, no line, no note on the plate: the berth kind is untouched (a great port is still a run ashore,
    // a working stop is still a working stop — #1066/#1078 read ArrivalTube.TierFor and this never goes
    // near it), the tube is the same tube, the walk ashore is the same walk. He is simply tied up somewhere
    // else round the same station, which is the single most ordinary thing that happens in a harbour.
    //
    // WHICH slot is DockRoster's business, not this file's, for the reason PoliteDecline leaves the door to
    // the building: a roster is a fact about a port, and a port is a thing this file cannot see.

    /// <summary>#1068 · <b>Is a reassignment owed at this berth?</b> The window it was filed in, or null —
    /// which is the answer at every berth in almost every world.
    ///
    /// <para>The window is what comes back rather than a bool, because the window is what the roster picks
    /// the new slot against (<see cref="DockRoster.BerthGiven"/>): handing the caller a flag would make it
    /// go and look the number up again somewhere else.</para>
    ///
    /// <para><b>Deterministic when two of a port's grounds are both owed</b> — the smaller ground id wins,
    /// ordinally, never the register's order. A list built by appending is not a list in order, and this
    /// ground keeps a named bug class about believing otherwise.</para></summary>
    public static long? BerthOwedAt(ICelestialEphemeris ephemeris, string havenId)
    {
        ArgumentNullException.ThrowIfNull(ephemeris);
        ArgumentNullException.ThrowIfNull(havenId);

        if (OwedGroundAt(_hands, ephemeris, havenId) is not { } owed)
        {
            return null;
        }
        return owed.Window;
    }

    /// <summary>#1068 · <b>Hand it over.</b> The register with this port's owed reassignment marked spent,
    /// or the register itself <b>by reference</b> when nothing was owed — so a caller can compare and only
    /// then ask for a save, exactly as <see cref="Note"/> does.
    ///
    /// <para>Called at the clamp and nowhere else. One arrival, one slot; the next clamp at that port is the
    /// ordinary berth and so is every clamp after it, which is the whole of "never twice in a row".</para>
    /// </summary>
    public static IReadOnlyList<Hand> GiveTheBerth(
        IReadOnlyList<Hand>? hands, ICelestialEphemeris ephemeris, string havenId)
    {
        ArgumentNullException.ThrowIfNull(ephemeris);
        ArgumentNullException.ThrowIfNull(havenId);

        IReadOnlyList<Hand> had = hands ?? [];
        if (OwedGroundAt(had, ephemeris, havenId) is not { } owed)
        {
            return had;
        }

        var next = new List<Hand>(had.Count);
        foreach (Hand h in had)
        {
            next.Add(string.Equals(h.BodyId, owed.BodyId, StringComparison.Ordinal)
                ? h with { BerthGiven = true }
                : h);
        }
        return next;
    }

    private static Hand? OwedGroundAt(IReadOnlyList<Hand> hands, ICelestialEphemeris ephemeris, string havenId)
    {
        Hand? owed = null;
        foreach (Hand h in hands)
        {
            if (h.BerthGiven || !ServedBy(ephemeris, h.BodyId, havenId))
            {
                continue;
            }
            if (owed is null || string.CompareOrdinal(h.BodyId, owed.Value.BodyId) < 0)
            {
                owed = h;
            }
        }
        return owed;
    }

    // ── DELIVERY 2 · A MOON REPRICED OVERNIGHT ───────────────────────────────────────────────────────────
    //
    // The pump at the ground's own port charges a credit more, or a credit less, than it did the last time
    // the captain filled her up there — and goes on charging it. One direction, chosen once, never
    // reverting: a price that walked back after it had been paid would be a price with an opinion about the
    // captain's wallet, and that is a fact about him rather than about the market.
    //
    // BOUNDED BY THE MARKET'S OWN VOLATILITY, DELEGATED RATHER THAN COPIED. The one price move this market
    // has ever published anywhere is the belt markup — a pump past the belt charges one credit more than a
    // pump inside it (FuelMarket.OuterPricePerPulse − FuelMarket.InnerPricePerPulse). So that is the whole
    // size of this, asked of FuelMarket rather than typed here: the day somebody re-tunes the markup, this
    // moves with it, and it can never grow into a number a Scully would have to explain. Two copies of a
    // spread is the mirrored constant this ground has paid for owning twice before.

    /// <summary>#1068 · How far a pump's price may move overnight, in credits per pulse: <b>the market's own
    /// published spread</b>, which is the belt markup and nothing else. Never typed here — see the section
    /// note above.</summary>
    public static int PulsePriceBandCr => FuelMarket.OuterPricePerPulse - FuelMarket.InnerPricePerPulse;

    /// <summary>#1068 · <b>What this pump's price has done overnight</b> — zero at every pump in almost
    /// every world, and otherwise one whole band, up or down.
    ///
    /// <para>Seeded on <b>(ground, the window the harbour filed it in)</b> and on nothing else, so it is the
    /// same move on the second visit and the tenth, and it survives a reload because the window does.</para>
    ///
    /// <para><b>Summed, then clamped to the band.</b> Two opened grounds under one planet do not add up to a
    /// price move a Scully could not read as noise: the clamp is the volatility law itself, and summing
    /// before it keeps the answer free of the register's ordering.</para></summary>
    public static int PulsePriceMoveAt(ICelestialEphemeris ephemeris, string pumpBodyId)
    {
        ArgumentNullException.ThrowIfNull(ephemeris);
        ArgumentNullException.ThrowIfNull(pumpBodyId);

        IReadOnlyList<Hand> hands = _hands;   // read the reference once (Burial.IsFilled's lesson)
        if (hands.Count == 0)
        {
            return 0;
        }

        int band = PulsePriceBandCr;
        int move = 0;
        foreach (Hand h in hands)
        {
            if (ServedBy(ephemeris, h.BodyId, pumpBodyId))
            {
                move += Direction(h) * band;
            }
        }
        return Math.Clamp(move, -band, band);
    }

    /// <summary>Is this berth the one that serves that ground? One question, asked by both deliveries, so
    /// neither of them owns a second opinion about it.</summary>
    private static bool ServedBy(ICelestialEphemeris ephemeris, string groundId, string havenId) =>
        PortFor(ephemeris, groundId) is { } port && string.Equals(port.Id, havenId, StringComparison.Ordinal);

    /// <summary>Which way this ground's port moved: up or down, once, for good.</summary>
    private static int Direction(Hand hand) =>
        DiceRule.Roll(DiceRule.Seed($"reprice:{hand.BodyId}", hand.Window), 2).Face == 1 ? 1 : -1;
}
