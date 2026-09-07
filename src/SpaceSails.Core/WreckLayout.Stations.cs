namespace SpaceSails.Core;

/// <summary>
/// WHERE THINGS STAND — ONE definition per station, read by BOTH the client (which places the console)
/// and the audit (which walks to it), plus the archive node and the black-ops key when a hull is carrying
/// one, and the answer to which compartment a point is in.
///
/// <para>They were separate literals for one commit and immediately drifted — the log moved in Core and
/// stayed put in the client — which is the same duplication that let a doorway be cut where the airlock
/// was. <c>CompartmentAt</c> is here for the same reason: one place computes that answer, so the map, the
/// board and the rules can never disagree about what room something is in.</para>
///
/// <para>Split out of <c>WreckLayout.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public static partial class WreckLayout
{
    // ── Where things stand ────────────────────────────────────────────────────────────────────────────
    //
    // ONE definition per station, read by BOTH the client (which places the console) and the audit (which
    // walks to it). They were separate literals for one commit and immediately drifted — the log moved in
    // Core and stayed put in the client — which is the same duplication that let a doorway be cut where the
    // player was never shown one. A station the audit walks to must be the station the captain presses E on.

    /// <summary>The way home — up in the bow, deliberately CLEAR of the spawn.
    ///
    /// <para>It sat four units from the spawn, which put it inside the interact radius of the doorway the
    /// away team arrives in and must pass through to reach anything. Playtested: stepping bow-ward at all
    /// bounced the captain straight back to the ship. The exit should be somewhere you go ON PURPOSE, not
    /// something you fall through on your way past.</para></summary>
    public static DeckReachability.Point ShuttleStation => new(24f, 0f);

    /// <summary>The cargo, and the decision about her: amidships in the near hold, where the cargo is. You
    /// cannot decide what to do with her from the bridge — you have to go and look at what she carried.</summary>
    public static DeckReachability.Point CargoStation => new(-7f, 6f);

    /// <summary>The bridge log. Clear of the BRIDGE's aft bulkhead — it sat exactly on it for a commit.</summary>
    public static DeckReachability.Point LogStation => new(16f, -6f);

    /// <summary>The cargo manifest, in the deep hold.</summary>
    public static DeckReachability.Point ManifestStation => new(-7f, -6f);

    /// <summary>The scuttling panel, right aft with the reactor — standing at it means standing next to the
    /// thing you are about to overload. Its position lives HERE rather than in the client because that is
    /// the only way a test can see it: placed by eye in the renderer it landed exactly on top of the
    /// infested hull's nest station, and the game handed the captain the nest when they pressed E at the
    /// panel (owner: <i>"I don't see the scuttling panel here"</i>).</summary>
    public static DeckReachability.Point ScuttleStation => new(-31f, 6f);

    /// <summary>The valve board itself — the mimic panel, aft with the machinery, because her bridge panel
    /// has no bus behind it. In Core so the audit walks to it and the separation test can see it: it was a
    /// literal in the client, which is exactly how the nest ended up two compartments from its own name.</summary>
    /// <remarks>Not (−24, −6), where it lived as a client literal: that is two units from the reactor
    /// cascade's own evidence AND standing in the ENGINEERING doorway. The separation test found both the
    /// moment the station was written down somewhere a test could see it — which is the argument for
    /// putting geometry in Core, made twice in one weekend.</remarks>
    public static DeckReachability.Point ValveStation => new(-19f, -6f);

    /// <summary>The damage-control placard, on the corridor wall just inboard of the shuttle lock — the
    /// first thing on the ship, at the only point every boarding passes through.
    ///
    /// <para>Owner, thinking past his own tenth boarding: <i>"did we tell somewhere where to find the manual
    /// airlock controls? Just thinking about first time player of that ship, could they go directly to the
    /// right space."</i> They could not. The one signpost was the dead bridge panel, which only speaks if
    /// you walk to the BOW and press it — so a captain who turned aft, or who never touched the bridge, was
    /// told nothing at all. The deck said ATMOSPHERE VALVES on a label that means nothing until you already
    /// know you want it.</para>
    ///
    /// <para>Every real ship answers this with a placard at the lock, which is also the safety card the
    /// owner filed a design for. So she gets one, where you come in.</para>
    ///
    /// <para>NOT (16.5, 2), where it was first bolted: 1.62 du from the FORWARD LOCKER's hatch control, so
    /// the first thing the captain meets on a derelict was two labels drawn on top of each other. Nothing
    /// caught it, because <see cref="StandardFittings"/> is audited against ITSELF and the hatch controls are
    /// generated per compartment — the same blind spot her own ship had. The deck audit walks the built plan
    /// now, consoles and all (<c>ConsoleCrowdingTests</c>).</para>
    ///
    /// <para>Nor (20.5, 2), which was my first correction and traded one law for another: it is half a unit
    /// off the shuttle-lock wall, and <c>WreckLayoutTests</c> walks to every station at half again the
    /// captain's width — <i>"only a thinner captain could reach the damage-control placard"</i>. A plate you
    /// have to squeeze past is not a plate anybody reads.</para>
    ///
    /// <para>(15, 1.7) clears every wall by 1.3 du and every other console by 3, and it is still the first
    /// plate of the boarding: the captain comes through the lock at x 21 and walks straight past it on the
    /// only road there is.</para></summary>
    public static DeckReachability.Point PlacardStation => new(15f, 1.7f);

    /// <summary>Her dead bridge panel — the master that has no bus behind it, and therefore a signpost
    /// rather than a control.
    ///
    /// <para>THE LAST LITERAL. It was <c>(19f, -7.5f)</c> in the client, which put it 3.4 du from the bridge
    /// log and, because it was not in <see cref="Stations"/>, outside everything CI walks: not reachability,
    /// not separation. Every wreck literal moved into Core this weekend turned out to be already wrong the
    /// moment a test could see it — the nest two compartments from its own name, the valves standing in the
    /// ENGINEERING doorway. Two for two is not a coincidence, it is the argument.</para></summary>
    /// <remarks>And 20.5 was my second wrong answer: the BRIDGE ends at <c>BowX − 6</c> = 20, so that put the
    /// panel in the hull taper where nobody can stand. The walkability audit said so immediately, which is
    /// the entire point of moving it somewhere the audit can see.</remarks>
    public static DeckReachability.Point BridgePanelStation => new(19f, -4.2f);

    /// <summary>
    /// WHAT EVERY SHIP HAS, WHATEVER KILLED HER. The fittings a hull is built with rather than the ones her
    /// ending gave her: the way home, the cargo, her two sets of paperwork, the scuttling panel, the placard
    /// at her lock and her atmosphere valves.
    ///
    /// <para>ABSENCE OF A TOOL IS INFORMATION, AND THAT IS THE WHOLE REASON THIS LIST EXISTS. Owner:
    /// <i>"even without reevers we should have those tech we used here available, to not give a clue that
    /// they might not be needed."</i> He is naming a leak the evidence system cannot survive. If a valve
    /// board only appears on infested hulls, then FINDING a valve board tells the captain what killed her
    /// before they have read one line of her log — and the careful business of <see cref="Derelict.WreckCause"/>
    /// and its <c>MisreadsAs</c> misdirection is undone by a console being present.</para>
    ///
    /// <para>So the fittings are the same on every ship in the fleet, and a wreck is distinguished only by
    /// her EVIDENCE. A pressurised hull with nothing living in her still has valves, and pulling them still
    /// works; it just does not help. That is exactly the shape a red herring should have — a real tool,
    /// available, that answers a question nobody is asking on this particular ship.</para>
    /// </summary>
    public static IReadOnlyList<(string Name, DeckReachability.Point At)> StandardFittings =>
    [
        ("the way back to the shuttle", ShuttleStation),
        ("the cargo (the decision)", CargoStation),
        ("the bridge log", LogStation),
        ("the cargo manifest", ManifestStation),
        ("the scuttling panel", ScuttleStation),
        ("the damage-control placard", PlacardStation),
        ("the atmosphere valves", ValveStation),
        ("the dead bridge panel", BridgePanelStation),
    ];

    /// <summary>Every place the captain must be able to REACH: her standard fittings plus the one station
    /// her ending put aboard. This is the list CI walks — and, since <c>WreckLayoutTests</c> also checks they
    /// do not stand on top of each other, the list that keeps two consoles from sharing a doorstep.</summary>
    public static IReadOnlyList<(string Name, DeckReachability.Point At)> Stations(Derelict.WreckCause cause) =>
        [.. StandardFittings, (CauseStationName(cause), CauseStation(cause))];

    // ── The archive node, when a hull is carrying one ────────────────────────────────────────────────
    //
    // NOT a standard fitting: it is CARGO nobody invoiced, and it is aboard about one eligible hull in three
    // (ArchiveNode.IsAboard). It is written down HERE rather than in the renderer for the reason every other
    // literal on this ship moved into Core — placed by eye, the scuttling panel landed on the nest and the
    // valve board stood in a doorway, and no test could see either.

    /// <summary>The column itself, in the deep hold — <see cref="ArchiveNode.HoldCompartment"/>, because that
    /// is where freight nobody invoiced ends up, and because it puts the field between the away team and the
    /// far end of the ship.
    ///
    /// <para>#633 · IT WAS AGAINST THE AFT BULKHEAD AT <c>(-13.8, -7.8)</c> AND HAD TO COME FORWARD. Two of
    /// #537's laws, built on `main` while this node was being built here, closed that corner between them.
    /// The interior bulkhead runs turn the DEEP HOLD's aft wall at <c>x = -15</c> from a line into a
    /// <see cref="BulkheadDepth"/>-deep closed box spanning <c>-15.6 … -14.4</c>, which left the column
    /// 0.6 du of clearance where the walk audit wants 1.05 — unreachable on all ten causes. And the room's
    /// aft end is already spoken for on one of them: the nest sits at <c>(-11, -6)</c>, so everything the
    /// bulkhead run allows is inside the 3 du no-two-stations-share-a-doorstep rule.
    ///
    /// <para>So it moves to the hold's FORWARD end, which turns out to be the better staging anyway: the away
    /// team comes aft down the spine, turns in at the hold's door, and the column is the first thing in the
    /// room rather than the last. The structural law does not move — it governs every bulkhead on every hull,
    /// and this governs one crate.</para></para></summary>
    public static DeckReachability.Point ArchiveStation => new(-3.5f, -7.7f);

    /// <summary>The handle plate at the inboard end of the same housing — <see cref="ArchiveNode.SwitchLegend"/>
    /// stencilled on it.
    ///
    /// <para>It is a SEPARATE doorstep on purpose, and the separation is the mechanic rather than tidiness:
    /// the design's law is that a captain <i>may pull the handle without paying, and never find out what they
    /// did</i>. Put the handle inside the confrontation's card and pulling it would first cost a throw, which
    /// is the one thing the whole Ren &amp; Stimpy joke cannot survive. So the column and the handle are 3.5 du
    /// apart — far enough that <c>NearestConsoleSpot</c> can tell them apart, close enough to be one object.</para>
    ///
    /// <para>#633 · Moved forward with the column, keeping the 3.5 du between them EXACTLY, because that
    /// distance is the mechanic and not a layout preference.</para></summary>
    public static DeckReachability.Point ArchiveSwitchStation => new(-3.5f, -4.2f);

    /// <summary>The reachability/separation list for a hull that IS carrying a node. Kept apart from
    /// <see cref="Stations"/> so the "every ship has identical fittings" law stays literally true — but
    /// audited on every cause anyway, because geometry that is only checked where it is currently used is
    /// geometry that breaks the day somebody widens the eligibility rule.</summary>
    public static IReadOnlyList<(string Name, DeckReachability.Point At)> StationsWithArchive(
        Derelict.WreckCause cause) =>
        [.. Stations(cause),
         ("the archive node", ArchiveStation),
         ("the purge handle", ArchiveSwitchStation)];

    // ── The black-ops key, when a hull that fought is carrying one ───────────────────────────────────
    //
    // NOT a standard fitting, for the archive node's reason one section up: it is not something a ship is
    // built with, it is something that was ABOARD her. Written down here rather than in the renderer because
    // every wreck literal placed by eye in the client turned out to be wrong the moment a test could see it
    // — the scuttling panel on the nest, the valve board standing in a doorway, three for three.

    /// <summary>
    /// #535 · WHERE THE KEY IS LYING: in the CREW SPACES, in somebody's kit, at the outboard end of the room.
    ///
    /// <para>The placement is the cheap half of the object's canon. A code nobody was ever meant to read is
    /// not in the safe with the manifest and it is not invoiced into the hold — it is in the personal effects
    /// of whoever was carrying it when she stopped being a ship, which is exactly the drawer this game
    /// already puts loose rounds and somebody's wallet in (#563's outpost effects).</para>
    ///
    /// <para>Not the DEEP HOLD, which is the tempting room: that is freight nobody invoiced — a different
    /// sentence about a different kind of secret — and it is already spoken for by the column
    /// (<see cref="ArchiveStation"/>) on the very cause this key is dealt on.</para>
    ///
    /// <para>(11, −7.5) clears the CREW SPACES bulkhead at x 13 and the hull at y −9 by more than the walk
    /// audit's margin, and stands clear of every other station on every cause by more than the separation
    /// rule's three units — the nearest is the life-support/mutiny evidence at (7, −6), four and a quarter
    /// away.</para></summary>
    public static DeckReachability.Point KeyStation => new(11f, -7.5f);

    /// <summary>The reachability/separation list for a hull that IS carrying a key. Kept apart from
    /// <see cref="Stations"/> so the "every ship has identical fittings" law stays literally true — and
    /// audited on every cause anyway, for <see cref="StationsWithArchive"/>'s reason: geometry checked only
    /// where it is currently used is geometry that breaks the day somebody widens the eligibility rule.</summary>
    public static IReadOnlyList<(string Name, DeckReachability.Point At)> StationsWithKey(
        Derelict.WreckCause cause) =>
        [.. Stations(cause), ("the black-ops key", KeyStation)];

    /// <summary>Where the cause's own evidence stands.</summary>
    public static DeckReachability.Point CauseStation(Derelict.WreckCause cause) => cause switch
    {
        Derelict.WreckCause.ReactorCascade => new(-26f, -6f),
        Derelict.WreckCause.DriveFailure => new(-26f, 6f),
        // Not x=0: that is the bulkhead NEAR HOLD and LIFEBOAT CRADLES share, and a station standing ON a
        // wall cannot be walked to. The audit caught this on its very first run.
        Derelict.WreckCause.HullBreach => new(-3.5f, 5.5f),
        Derelict.WreckCause.LifeSupportFailure => new(7f, -6f),
        Derelict.WreckCause.NavigationalError => new(18.6f, -7.7f),
        Derelict.WreckCause.Mutiny => new(7f, -6f),
        // Deeper into the near hold than the cargo console: the two sat on the SAME POINT for a long time,
        // which the separation test found the day it was written. Both belong in this room — the stripped
        // frames and the decision about what is left — they just cannot be in the same square metre.
        Derelict.WreckCause.Piracy => new(-12f, 6f),
        // THE NEST IN THE DEEP HOLD, and now actually in the deep hold. It sat at (-24, 6) — the reactor
        // spaces — while every line of text about it, its own station name included, said deep hold. Owner,
        // finding them coming out of half the ship: "I thought there was only one nest?" There is exactly
        // one, and this is where it is.
        Derelict.WreckCause.Infested => new(-11f, -6f),
        Derelict.WreckCause.InsuranceJob => new(7f, 6f),
        // AMIDSHIPS IN THE SPINE, AND THE ONLY CAUSE STATION THAT IS NOT IN A ROOM — because her evidence is
        // not in a room. "Every door was thrown from the SPINE side": you stand in the corridor, look fore
        // and aft, and every hatch on the ship has its dogs on YOUR side of it. The tenth cause was the only
        // one with no arm in this switch, so it fell through to the fallback below — which happens to be this
        // same point, reached by accident and named "the wreck". Declared now, so the station is a decision.
        Derelict.WreckCause.VentedByOneOfTheirOwn => new(0f, 0f),
        _ => new(0f, 0f),
    };

    /// <summary>Which compartment a point stands in, or null out in the spine. The one place that answer is
    /// computed, so the map, the board and the rules can never disagree about what room something is in.</summary>
    public static string? CompartmentAt(double x, double y)
    {
        foreach ((string name, float x0, float x1, bool top) in Compartments)
        {
            if (x >= x0 && x <= x1 && (top ? y < -SpineHalfHeight : y > SpineHalfHeight))
            {
                return name;
            }
        }

        return null;
    }

    /// <summary>THE room the infestation comes out of — there is one, and it is wherever her station is.
    /// Derived rather than declared, so moving the station moves the nest and nothing drifts apart.</summary>
    public static string NestCompartment =>
        CompartmentAt(CauseStation(Derelict.WreckCause.Infested).X,
                      CauseStation(Derelict.WreckCause.Infested).Y)
        ?? "DEEP HOLD";

    /// <summary>The cause station's name, for a readable failure.</summary>
    public static string CauseStationName(Derelict.WreckCause cause) => cause switch
    {
        Derelict.WreckCause.ReactorCascade => "the reactor spaces",
        Derelict.WreckCause.DriveFailure => "the drive bells",
        Derelict.WreckCause.HullBreach => "the hole through her",
        Derelict.WreckCause.LifeSupportFailure => "the scrubber stacks",
        Derelict.WreckCause.NavigationalError => "the nav post",
        Derelict.WreckCause.Mutiny => "the arms locker",
        Derelict.WreckCause.Piracy => "the stripped hold",
        Derelict.WreckCause.Infested => "the nest in the deep hold",
        Derelict.WreckCause.InsuranceJob => "the lifeboat cradles",
        Derelict.WreckCause.VentedByOneOfTheirOwn => "the hatch dogs — spine side",
        _ => "the wreck",
    };
}
