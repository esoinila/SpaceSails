using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #585 · THE HIVE — the secret lab as a facility, not a two-door apartment.
///
/// <para>Owner, 2026-08-01: <i>"I expect a large space to be discoverable with long tunnels. Maybe we could go
/// underground so that we don't need to go out of the border on normal level. Instead ... show what is on our
/// current 'floor' level. On surface we would only need a camouflaged elevator. I think there are a lot of
/// movie references to masked elevators to underground sites (The Hive in Resident Evil for example). I just
/// don't want the secret lab to be puny 2 door apartment, but look like it could facilitate a large operation
/// with serious funding. We can again use the locked doors to give the illusion of much larger space. And say
/// corridors that lead to somewhere far away where we dare not venture too far into."</i></para>
///
/// <para>What it replaces: <c>SecretLab</c> appended ONE room, 16 x 14 du, for a find the code itself bills as
/// "the veterans' once-a-career" payoff worth five thousand credits. His phrase for it was exact.</para>
///
/// <h3>Why down is the right answer, and not only thematically</h3>
/// <para><b>Each floor reuses the surface's own coordinate envelope.</b> That makes the "don't walk past the
/// border" problem disappear rather than be fought: a complex the size of the whole field costs no new space,
/// because it is not beside the field, it is under it. The renderer shows one floor; the deck-plan swap that
/// does it is the same machinery the ship ↔ haven ↔ surface switch already uses.</para>
///
/// <h3>The three calls</h3>
/// <para>The owner said "go forward" without answering the three open questions, so they are decided here and
/// written down loudly enough to be overruled in one line each:</para>
/// <list type="number">
/// <item><b>You find it by LOOKING.</b> The lift head is a real structure on the surface — a squat blockhouse
/// that reads like any other ruin until you are close enough to see its door, which is
/// <see cref="BodyPalette.Imported"/> violet. On a moon where every hatch is local stone, that is the one door
/// that was flown here, and it is the best possible use of the #592 language. The metal-detector probe still
/// works and still pings; it is no longer the only way, because one square in a 310 x 260 field is a needle in
/// a haystack.</item>
/// <item><b>Three floors down, and the bottom is not a bottom.</b> −1, −2, −3, and the deepest ends at a
/// sealed corridor mouth with a distance painted on it. The world continues past where you are allowed to
/// walk; that is the whole feeling he asked for.</item>
/// <item><b>−1 still holds pressure. Below that it does not.</b> This is the one that decides how the place
/// FEELS, so it gets the answer with a beat in it: the first floor is a refuge — the tank stops, the nerve
/// steadies, you relax — and everything below is dead, so depth is paid for in air and every stair down is a
/// decision about getting back up. A complex that is uniformly safe is a museum; one that is uniformly hostile
/// is a corridor shooter. The lie is what makes it frightening.</item>
/// </list>
///
/// <para>Canon holds absolutely: nothing down here explains what the Old Ones are (owner ruling 2026-07-30).
/// A facility may be enormous, expensive and obviously state-backed, and may never say what it was for.</para>
/// </summary>
public static class UndergroundComplex
{
    /// <summary>#585 · WHAT KIND OF PLACE THIS IS. Owner, extending the brief: <i>"feel free to upgrade the
    /// expanded section into proper literally underground lab space. We can have a lot of those in the sites,
    /// different clandestine sites in the spirit of world building."</i>
    ///
    /// <para>So this is not one rare lab any more — it is a CATEGORY. Clandestine sites are a thing that
    /// happens under moons, plural, and each kind is a different arm of the same unspeakable business. None
    /// of them ever explains the business; they only show you what it costs to run.</para></summary>
    public enum Kind
    {
        /// <summary>Dr Vantar's own — the original #409 find, now with a building around it.</summary>
        Laboratory,
        /// <summary>Where people were counted, graded and moved. The most bureaucratic and the worst.</summary>
        ProcessingDepot,
        /// <summary>Paper. Rooms and rooms of it, and somebody once thought that was the safe option.</summary>
        RecordsAnnex,
        /// <summary>A clinic with no name on the door and no register it appears in.</summary>
        BlackClinic,
        /// <summary>A transfer station: things came in, things went out, and the manifests do not match.</summary>
        TransitStation,
    }

    /// <summary>Which kind hides under this body. Seeded, so a moon has the site it has.</summary>
    public static Kind KindFor(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return (Kind)(DiceRule.Roll(DiceRule.Seed($"hive:kind:{bodyId}"), 5).Face - 1);
    }

    /// <summary>#592 · What kind of place THIS FLOOR is. The same as the site's own kind everywhere the
    /// building admits to, and something else entirely on the band nobody listed.
    ///
    /// <para>This is where the feature does its storytelling and it costs nothing but a different word list.
    /// A records annex whose bottom floor is a clinic tells you what the records were <i>of</i> without one
    /// line of narration — and, crucially, without ever saying it. The doors read MORTUARY and CONSENT FILES
    /// under twelve floors of RETENTION 40 YR and DESTRUCTION QUEUE, and the captain does the arithmetic
    /// themselves, or does not.</para>
    ///
    /// <para>Guaranteed DIFFERENT from the floors above: a hidden clinic under a clinic is a bigger clinic,
    /// which is the one outcome that makes the whole thing pointless.</para></summary>
    public static Kind KindOn(string bodyId, int level)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        Kind above = KindFor(bodyId);
        if (!IsUnlisted(bodyId, level))
        {
            return above;
        }

        int kinds = Enum.GetValues<Kind>().Length;
        int step = DiceRule.Roll(DiceRule.Seed($"hive:unlisted-kind:{bodyId}"), kinds - 1).Face;
        return (Kind)(((int)above + step) % kinds);   // step is 1..kinds-1, so never `above`
    }

    /// <summary>What the place calls itself, if it calls itself anything.</summary>
    public static string TitleOf(Kind kind) => kind switch
    {
        Kind.Laboratory => "▣ THE LABORATORY",
        Kind.ProcessingDepot => "▣ THE PROCESSING DEPOT",
        Kind.RecordsAnnex => "▣ THE RECORDS ANNEX",
        Kind.BlackClinic => "▣ THE CLINIC",
        _ => "▣ THE TRANSIT STATION",
    };

    /// <summary>#585 · DEPTH IS FREE. Owner, working out the architecture himself:
    ///
    /// <para><i>"since every secret lab can have a depth of it's own we do not need to worry about running out
    /// of space down there, since down there is unlimited amount of floors as far as we are concerned. So
    /// let's architect it to keep this in mind from the start. Well ok the lift shafts are the limiting
    /// factor, but besides those we have space."</i></para>
    ///
    /// <para>He is right, and it is the whole reason "down" was the correct answer. A floor costs no
    /// coordinate space because every floor reuses the surface's own envelope, so the only real budget is how
    /// far a captain will walk. Depth is therefore <b>a property of the site</b>, never a constant: a records
    /// annex might be three floors and a processing depot twenty, and the difference costs nothing.</para>
    ///
    /// <para>The bound below is a PERFORMANCE guard, not a design one — it exists so a seed cannot ask for a
    /// thousand floors. Nothing should ever read it as "how deep the game goes".</para></summary>
    public const int DeepestPossibleFloor = -24;

    /// <summary>How far down this site ADMITS to going. Seeded per body, weighted so most are modest and a
    /// rare one is a hole in the world worth telling people about.
    ///
    /// <para>#592: read this as the building's own account of itself — the bottom of the lift directory, the
    /// last floor on the plan in the lobby. On a rare site it is not the bottom of the hole. Anything asking
    /// "how far down can a captain actually walk" wants <see cref="TrueDepthOf"/>; anything asking "what does
    /// this place say about itself" wants this one, and the gap between them is the feature.</para></summary>
    public static int DepthOf(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        int roll = DiceRule.Roll(DiceRule.Seed($"hive:depth:{bodyId}"), 12).Face;
        int floors = roll switch
        {
            <= 6 => 2 + roll,      // 3–8, the common case
            <= 10 => 6 + roll,     // 13–16, a serious operation
            _ => 8 + roll,         // 19–20, the one you tell people about
        };
        return -Math.Min(floors, -DeepestPossibleFloor);
    }

    // ── #592 · A SECRET LAB'S OWN SECRET LAB ────────────────────────────────────────────────────────────
    //
    // Owner: "we could even have a secret lab lab :-D"
    //
    // The joke is good and the mechanic under it is better. A facility whose BOTTOM BAND IS NOT ON ITS OWN
    // PLAN: a shaft not in the directory, a floor the panel does not list. Everything above it is a real,
    // expensive, thoroughly documented clandestine operation — and underneath THAT is the thing the
    // clandestine operation was hiding from its own staff.
    //
    // It costs almost nothing to build because three things were already right:
    //
    //   * depth is free — a floor reuses the surface's own envelope, so a hidden band takes no space;
    //   * bands already gate descent, and a hidden band is that mechanism with the next shaft simply not
    //     advertised;
    //   * Kind already varies the building, so the deepest band can be a DIFFERENT KIND from the floors
    //     above it — a records annex whose bottom is a clinic tells a story nobody has to narrate.
    //
    // THE BUILDING LIES BY OMISSION, which is exactly in register with everything else down here. The panel
    // on the floor above says what it has always said: there is no button below this one. It does not hedge,
    // it does not hint, and it is not lying about a door — the button really is not there. The way down is a
    // card somebody left in a room (#590), which is a piece of paper telling the truth about a building that
    // is not.
    //
    // Canon holds hardest here, because this is the single most tempting place in the game to explain the
    // Old Ones. It does not. The deepest floor of the deepest facility may be full of evidence of an
    // enormous, well-funded, decades-long operation, and may never once name what the operation produced.

    /// <summary>How many sites in this many have something under the floor they admit to. Rare on purpose:
    /// the moment it is common it stops being a secret and becomes a level.</summary>
    public const int UnlistedOneInN = 4;

    /// <summary>Does this site have a band nobody listed? Seeded off its own id, so it is a fact about the
    /// world and not about the visit.</summary>
    public static bool HasUnlistedBand(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        // Only somewhere that already had room to hide something. A three-floor annex with a secret basement
        // is a bungalow with a dungeon; the lie needs a building big enough to keep a secret from its staff.
        int listed = DepthOf(bodyId);
        if (listed > -FloorsPerShaft)
        {
            return false;
        }

        // And only where the hidden band's own shaft head still fits inside the performance guard. That
        // bound is a guard and not a design bottom (#585), but a band that would be clamped to nothing is
        // not a band.
        if (BandTop(BandOf(listed) + 1) <= DeepestPossibleFloor)
        {
            return false;
        }

        return DiceRule.Roll(DiceRule.Seed($"hive:unlisted:{bodyId}"), UnlistedOneInN).Face == 1;
    }

    /// <summary>How far down a captain can ACTUALLY walk — the listed depth, plus the band nobody listed.
    ///
    /// <para>Every audit, every renderer and every lab wants this one: an unlisted floor is still a floor,
    /// and a topology nothing walks is a topology nobody has checked. Only the things that speak FOR the
    /// building — the lift panel, the directory — get to use <see cref="DepthOf"/>.</para></summary>
    public static int TrueDepthOf(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return HasUnlistedBand(bodyId) ? BandBottom(UnlistedBandOf(bodyId)) : DepthOf(bodyId);
    }

    /// <summary>#592 · WHICH band is the one nobody listed.
    ///
    /// <para>It is the next WHOLE band under the one the listed bottom falls in — not "four floors below the
    /// listed bottom", which sounds the same and is not. Bands are fixed slices of four counted from the
    /// surface, because that is what a shaft is; a hidden band that started at an arbitrary depth would
    /// share a car with the floors above it and the secret would be reachable by pressing DOWN. There is a
    /// GAP between the two, and nothing is generated in it: the listed building stops where it stops, and
    /// the unlisted one starts at its own shaft head.</para></summary>
    public static int UnlistedBandOf(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return BandOf(DepthOf(bodyId)) + 1;
    }

    /// <summary>The top floor a shaft band serves — where its car opens.</summary>
    public static int BandTop(int band) => -(band * FloorsPerShaft) - 1;

    /// <summary>The deepest floor a shaft band could serve if nothing stopped it.</summary>
    private static int BandBottom(int band) =>
        Math.Max(DeepestPossibleFloor, -((band + 1) * FloorsPerShaft));

    /// <summary>Is this floor one of the ones the building does not admit to?</summary>
    public static bool IsUnlisted(string bodyId, int level)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return HasUnlistedBand(bodyId) && level < 0 && BandOf(level) == UnlistedBandOf(bodyId);
    }

    /// <summary>#592 · EVERY FLOOR THIS SITE ACTUALLY HAS, top to bottom — the listed ones, then the gap
    /// where nothing was dug, then the band nobody listed.
    ///
    /// <para>The one place that knows the shape. Audits, the renderer and the labs all walk this rather
    /// than counting from a depth, because with a gap in the middle "−1 down to the bottom" is no longer
    /// the floor list — and a phantom floor generated by an audit is a topology nobody ships being checked
    /// instead of the one they do.</para></summary>
    public static IEnumerable<int> FloorsOf(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        for (int level = -1; level >= DepthOf(bodyId); level--)
        {
            yield return level;
        }

        if (!HasUnlistedBand(bodyId))
        {
            yield break;
        }
        int band = UnlistedBandOf(bodyId);
        for (int level = BandTop(band); level >= BandBottom(band); level--)
        {
            yield return level;
        }
    }

    /// <summary>#585 · THE SHAFTS ARE THE LIMIT — the owner's own observation, turned into the mechanic.
    ///
    /// <para>A single lift never serves a whole facility: it serves a BAND. Reach the bottom of a band and the
    /// car goes no further; the way down is a different shaft, somewhere on that floor, which you have to
    /// find. That is what keeps unlimited depth from being an unlimited corridor — the descent is gated by
    /// exploring rather than by a number, and it is how a building this size would really be dug.</para></summary>
    public const int FloorsPerShaft = 4;

    /// <summary>Which shaft band a floor belongs to. Band 0 is the one the surface lift head serves.</summary>
    public static int BandOf(int level) => (-level - 1) / FloorsPerShaft;

    /// <summary>The deepest floor a shaft band reaches, never past the bottom of the building that band
    /// belongs to.
    ///
    /// <para>#592 · Two buildings, so two bottoms. Every band the site admits to stops at the LISTED depth —
    /// that is what makes the last listed floor feel like the bottom, because for that shaft it is. The
    /// band nobody listed is a whole band of its own, below a GAP where nothing was dug, and it stops at
    /// its own.</para></summary>
    public static int BandFloor(string bodyId, int band)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        int listed = DepthOf(bodyId);
        return band > BandOf(listed)
            ? BandBottom(band)                          // the unlisted band, on its own shaft
            : Math.Max(listed, BandBottom(band));       // everything the directory knows about
    }

    /// <summary>Is this the floor where the car stops and you go looking for the next shaft?</summary>
    public static bool IsBandBottom(string bodyId, int level) =>
        level == BandFloor(bodyId, BandOf(level)) && level > TrueDepthOf(bodyId);

    /// <summary>Which floors still hold atmosphere.
    ///
    /// <para>Owner's biggest open question, answered with a beat in it: a floor with power lulls you and the
    /// rest costs you. Extended for unbounded depth by making it the TOP OF EVERY SHAFT BAND — that is where
    /// a facility puts its lobbies — so a captain who finds the next shaft gets one floor of relief before the
    /// dark again. It keeps a very deep site playable without ever making it safe.</para></summary>
    public static bool HoldsPressure(int level) =>
        level < 0 && (-level - 1) % FloorsPerShaft == 0;

    /// <summary>What the level is called on the lift panel and the plan header. Named by depth band rather
    /// than from a hand-written list, because there is no longer a fixed bottom to write down.</summary>
    public static string NameOf(int level)
    {
        if (level >= 0)
        {
            return "SURFACE";
        }

        string[] departments =
        [
            "ADMINISTRATION", "LABORATORIES", "LONG STORAGE", "PLANT",
            "ARCHIVE", "ISOLATION", "DEEP STORAGE", "UNMARKED",
        ];
        return $"B{-level} · {departments[(-level - 1) % departments.Length]}";
    }

    /// <summary>#592 · What the level is called, given which building it is in. A floor the directory never
    /// listed has no department, because a department is a thing you write on a plan — and the whole point
    /// of these floors is that nobody wrote them anywhere.
    ///
    /// <para>It is not a hint: you can only read this once you are standing on the floor, and by then the
    /// building has stopped keeping the secret from you and is only keeping it from everybody else.</para></summary>
    public static string NameOf(string bodyId, int level)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return IsUnlisted(bodyId, level) ? $"B{-level} · NO PLATE" : NameOf(level);
    }

    /// <summary>The lift shaft's spot — the SAME (x, y) on every floor, so going down is legible and coming
    /// back up is never a search. Sits on the spine corridor at the field's heart.</summary>
    public static (double X, double Y) ShaftAt(in SurfaceLayout.Field field) =>
        (field.AnchorX + 40, (field.BottomY + field.LandingBandY) / 2.0);

    /// <summary>Half-width of the lift car, and of the shaft cut through every floor.</summary>
    public const double ShaftHalf = 3.0;

    /// <summary>Corridor half-width. Wide enough for the captain and an Old One to pass and for the eye to
    /// read it as a built passage rather than a gap between two walls.</summary>
    public const double CorridorHalf = 3.5;

    /// <summary>One floor, laid out. Walls and doorways in the same shapes <see cref="SurfaceLayout"/> speaks,
    /// so the client lays a floor exactly the way it lays a ground.</summary>
    public readonly record struct FloorPlan(
        int Level,
        string Name,
        bool Pressurised,
        IReadOnlyList<SurfaceLayout.Wall> Walls,
        IReadOnlyList<SurfaceLayout.Doorway> Doorways,
        IReadOnlyList<LockedDoor> Locked,
        IReadOnlyList<SurfaceLayout.Landmark> Labels,
        IReadOnlyList<(double X, double Y)> RoomCentres,
        IReadOnlyList<Rib> Ribs);

    /// <summary>#587 · A CROSS CORRIDOR, PUBLISHED RATHER THAN INFERRED.
    ///
    /// <para>The ribs used to be a local of <see cref="Build"/>, so the only thing outside this file that
    /// could say where one was, was arithmetic that copied the placement — which is the mirrored-constant
    /// bug this ground keeps paying for. #587 was a mouth that had been cut and then walled over again, and
    /// no guard could state that in Core because no guard could name the mouth. Now it can.</para>
    ///
    /// <para><b>Down</b> means the rib runs toward the deep field, away from the landing band, and therefore
    /// opens off the spine's LOWER face; an up rib opens off the upper one. That flag is the whole reason
    /// #587 only ever struck some floors.</para></summary>
    public readonly record struct Rib(double X, bool Down);

    /// <summary>A door that never opens. The cheapest illusion of scale there is, and the owner asked for it
    /// by name — <i>"we can again use the locked doors to give the illusion of much larger space"</i>. Each
    /// carries the sign that was on it, which is what does the work: a corridor of shut doors with departments
    /// painted on them is a facility, and the same corridor with blank doors is a wall.</summary>
    public readonly record struct LockedDoor(double X1, double Y1, double X2, double Y2, string Sign);

    /// <summary>Build one floor. Pure and deterministic per (body, level): the same complex every visit, so a
    /// captain can learn it and come back for the door they could not open.</summary>
    public static FloorPlan Build(string bodyId, int level, in SurfaceLayout.Field field)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        var walls = new List<SurfaceLayout.Wall>();
        var doorways = new List<SurfaceLayout.Doorway>();
        var locked = new List<LockedDoor>();
        var labels = new List<SurfaceLayout.Landmark>();
        var rooms = new List<(double X, double Y)>();

        // #585 · A CLAIM LEDGER, DOWN HERE TOO. The A* audit found rooms that were drawn and could not be
        // entered, and the cause is the one this project keeps paying for: two rooms (or a room and the
        // spine) laid on the same ground, each sealing the other's doorway with its own wall. Every placer
        // that writes into one space needs to see what is already in it.
        var claimed = new List<(double X0, double Y0, double X1, double Y1)>();

        double margin = SurfaceLayout.EdgeMargin + 6;
        double left = field.LeftX + margin, right = field.RightX - margin;
        (double shaftX, double shaftY) = ShaftAt(field);
        claimed.Add((left - 1, shaftY - CorridorHalf - 1, right + 1, shaftY + CorridorHalf + 1));

        // ── #585 · THE SPINE, CLOSED AT BOTH ENDS AND OPEN WHERE IT SHOULD BE.
        //
        // Owner, walking it: "see this empty tube end here... it is like I walk into the ground here" and
        // then, exactly: "this open end is a bug of topology."
        //
        // It was, and it was two bugs wearing one coat. The spine was capped on the LEFT and not on the
        // right, so walking east you left the building through the end of the corridor into open coordinate
        // space — which, drawn in the old dim ink, looked precisely like walking out into regolith. And the
        // spine's long walls ran unbroken from end to end ACROSS every rib mouth, so the cross corridors did
        // not actually open off it: the plan showed a facility and the collision said one sealed tube.
        //
        // A corridor is defined by where it does NOT have walls. Both faces are now built in segments with a
        // deliberate gap at each rib, and both ends are shut.
        var ribXs = new System.Collections.Generic.List<(double X, bool Down)>();
        int ribs = 5;
        for (int i = 0; i < ribs; i++)
        {
            double t = (i + 0.5) / ribs;
            double rx = Lerp(left + 16, right - 16, t);
            if (Math.Abs(rx - shaftX) < ShaftHalf + CorridorHalf + 4)
            {
                continue;   // never run a rib through the lift
            }
            ribXs.Add((rx, Frac(bodyId, $"hive:{level}:rib-dir:{i}") < 0.62));
        }

        // #587 · The ribs, exactly as built, published on the plan. Taken HERE — before the lift alcove is
        // appended — because the alcove is a mouth in a wall, not a corridor anybody walks down.
        var ribList = new List<Rib>(ribXs.Count);
        foreach ((double rx, bool rdown) in ribXs)
        {
            ribList.Add(new Rib(rx, rdown));
        }

        // The lift alcove, as a mouth in the top face at the shaft. It is APPENDED, so it is the one entry in
        // this list that is not in x order — which is the whole of #587. See SpineFace.
        ribXs.Add((shaftX, false));

        // One face of the spine, built as segments that stop either side of every mouth cut into it.
        void SpineFace(double y, Func<double, bool, bool> cutHere)
        {
            // #587 · A CURSOR THAT WALKS A LINE MUST BE GIVEN THE LINE IN ORDER.
            //
            // This is the third bug on this wall and the first one that was invisible from the plan: the
            // geometry was right, the mouths were right, and the WALLS BETWEEN THEM were built by a cursor
            // sweeping left to right over a list that was not sorted left to right. `ribXs` holds the ribs in
            // ascending x (they are Lerped in order) and then the lift alcove APPENDED at the end, at the
            // shaft's own x — which on this field sits left of the right-most rib.
            //
            // So the sweep ran out to the far rib, advanced the cursor past it, then met the alcove behind it
            // and emitted a segment from cursor BACK to the alcove's near edge: one long wall lying across
            // everything between the two, re-sealing both mouths it had just been asked to open. The A*
            // audit reported it as the two room columns beside the right-most rib plus the lift itself —
            // and it only ever happened when that rib pointed UP, because the alcove is only cut into the
            // top face, which is exactly the pattern #587 recorded and could not explain.
            //
            // RibFace already sorts its cuts for precisely this reason. Both faces sort now, and the cursor
            // can only ever move forward — so an overlapping pair of mouths degrades to one wide mouth
            // rather than to a wall.
            var mouths = new List<double>();
            foreach ((double rx, bool down) in ribXs)
            {
                if (cutHere(rx, down))
                {
                    mouths.Add(rx);
                }
            }
            mouths.Sort();

            double cursor = left;
            foreach (double rx in mouths)
            {
                double near = Math.Max(cursor, rx - CorridorHalf);
                if (near > cursor)
                {
                    walls.Add(new(cursor, y, near, y, true));
                }
                cursor = Math.Max(cursor, rx + CorridorHalf);
            }
            walls.Add(new(cursor, y, right, y, true));
        }

        // #585 · The lift alcove hangs off the TOP face, so that face needs a mouth for it too — otherwise
        // the car opens into a sealed box and the captain cannot reach their own way out. The A* audit
        // reported this as "the lift cannot be reached from the lift", which is as clear as a guard gets.
        SpineFace(shaftY + CorridorHalf, (rx, down) => !down || Math.Abs(rx - shaftX) < 0.001);
        SpineFace(shaftY - CorridorHalf, (_, down) => down);

        // BOTH ends shut. The missing right-hand cap is the "open end" itself.
        walls.Add(new(left, shaftY - CorridorHalf, left, shaftY + CorridorHalf, true));
        walls.Add(new(right, shaftY - CorridorHalf, right, shaftY + CorridorHalf, true));
        labels.Add(new(shaftX - 26, shaftY + 1.4, NameOf(bodyId, level)));

        // ── THE SHAFT. Same spot on every floor.
        walls.Add(new(shaftX - ShaftHalf, shaftY + CorridorHalf, shaftX - ShaftHalf, shaftY + CorridorHalf + 5, true));
        walls.Add(new(shaftX + ShaftHalf, shaftY + CorridorHalf, shaftX + ShaftHalf, shaftY + CorridorHalf + 5, true));
        walls.Add(new(shaftX - ShaftHalf, shaftY + CorridorHalf + 5, shaftX + ShaftHalf, shaftY + CorridorHalf + 5, true));
        labels.Add(new(shaftX, shaftY + CorridorHalf + 6.5, "\U0001F6D7 LIFT"));

        // ── THE RIBS. Cross corridors off the spine, with rooms flanking them.
        for (int i = 0; i < ribXs.Count; i++)
        {
            (double x, bool down) = ribXs[i];
            if (Math.Abs(x - shaftX) < 0.001)
            {
                continue;   // that entry is the lift alcove's mouth, not a corridor
            }
            double far = down
                ? Math.Max(field.BottomY + margin, shaftY - 52)
                : Math.Min(field.LandingBandY - margin, shaftY + 52);

            double mouth = down ? shaftY - CorridorHalf : shaftY + CorridorHalf;

            // #585 · THE RIB'S OWN WALLS ARE CUT WHERE ROOMS OPEN OFF THEM. Owner: "a door is missing here
            // towards down", and his A* suggestion found it everywhere at once — 94 floors, not one room
            // reachable.
            //
            // The rooms cut a doorway in their OWN corridor-facing face, at x ± CorridorHalf. The rib's side
            // wall runs down that exact line. So every door in the building opened onto a wall: the plan drew
            // a facility and the collision field was a set of sealed boxes beside a sealed tube. Two walls on
            // one line, each correct on its own, and neither aware of the other — the same shape as every
            // expensive bug on this ground.
            RibFace(walls, x - CorridorHalf, mouth, far, bodyId, level, i, -1, down);
            RibFace(walls, x + CorridorHalf, mouth, far, bodyId, level, i, +1, down);

            // The rib's far end. #585: it is ALWAYS closed — by a sealed door with a distance on it, or by a
            // plain wall. It was 40/60 before, and a corridor that simply stops in mid-air is the same
            // topology bug one level down ("a door is missing here towards down").
            if (Frac(bodyId, $"hive:{level}:rib-far:{i}") < 0.55)
            {
                double km = 0.8 + (Frac(bodyId, $"hive:{level}:rib-km:{i}") * 3.4);
                locked.Add(new(x - CorridorHalf, far, x + CorridorHalf, far,
                    $"\u27F6 SECTOR {7 + i} \u00b7 {km:F1} km"));
            }
            walls.Add(new(x - CorridorHalf, far, x + CorridorHalf, far, true));

            AddRoomsAlong(walls, doorways, locked, rooms, claimed, bodyId, level, i, x, mouth, far, down);
        }

        return new FloorPlan(level, NameOf(bodyId, level), HoldsPressure(level),
            walls, doorways, locked, labels, rooms, ribList);
    }

    /// <summary>Rooms down both sides of a rib. About half are locked — the owner's illusion of scale — and a
    /// locked one still gets its sign, because a door that says what is behind it and will not open is doing
    /// far more work than a blank one.</summary>
    /// <summary>#585 · Where the rooms sit along a rib. ONE function, called by the wall builder and by the
    /// room builder, because the doorway a room cuts and the gap its corridor leaves must be the same gap.
    /// They were computed twice and agreed about nothing.</summary>
    private static List<double> RoomCentresAlong(double mouth, double far, bool down)
    {
        const double roomH = 12.0;
        double span = Math.Abs(far - mouth);
        int count = Math.Max(1, (int)(span / (roomH + 3)) - 1);

        var ys = new List<double>(count);
        for (int i = 0; i < count; i++)
        {
            double along = (i + 1) * (span / (count + 1));
            ys.Add(down ? mouth - along : mouth + along);
        }
        return ys;
    }

    /// <summary>One side of a rib corridor, built as segments with a gap at every room door.</summary>
    private static void RibFace(
        List<SurfaceLayout.Wall> walls, double x, double mouth, double far,
        string bodyId, int level, int rib, int side, bool down)
    {
        var doors = RoomCentresAlong(mouth, far, down);
        double lo = Math.Min(mouth, far), hi = Math.Max(mouth, far);

        var cuts = new List<(double Lo, double Hi)>();
        foreach (double cy in doors)
        {
            cuts.Add((cy - DoorHalf, cy + DoorHalf));
        }
        cuts.Sort((a, b) => a.Lo.CompareTo(b.Lo));

        double cursor = lo;
        foreach ((double clo, double chi) in cuts)
        {
            if (chi <= lo || clo >= hi)
            {
                continue;
            }
            walls.Add(new(x, cursor, x, Math.Max(cursor, clo), true));
            cursor = Math.Min(hi, chi);
        }
        walls.Add(new(x, cursor, x, hi, true));
    }

    /// <summary>Half a doorway. Comfortably wider than the captain, and the ONE number both the room's own
    /// face and its corridor's wall are cut to.
    ///
    /// <para>#585: widened from 2.0. A 4 du gap is four captain-diameters and looked ample on paper, but the
    /// reachability flood walks a GRID — a gap narrower than a couple of grid steps can fail to be sampled at
    /// all, so a door that is open in the geometry is shut to anything that pathfinds. A facility corridor
    /// would have wide doors anyway; this is one of the happy cases where the honest fiction and the robust
    /// number are the same number.</para></summary>
    public const double DoorHalf = 3.2;

    private static void AddRoomsAlong(
        List<SurfaceLayout.Wall> walls, List<SurfaceLayout.Doorway> doorways, List<LockedDoor> locked,
        List<(double X, double Y)> rooms, List<(double X0, double Y0, double X1, double Y1)> claimed,
        string bodyId, int level, int rib, double x, double mouth, double far, bool down)
    {
        const double roomW = 15.0, roomH = 12.0;
        List<double> centres = RoomCentresAlong(mouth, far, down);

        for (int i = 0; i < centres.Count; i++)
        {
            double cy = centres[i];

            for (int side = -1; side <= 1; side += 2)
            {
                string tag = $"hive:{level}:{rib}:{i}:{side}";
                double cx = x + (side * (CorridorHalf + (roomW / 2)));

                double x1 = cx - (roomW / 2), x2 = cx + (roomW / 2);
                double y1 = cy - (roomH / 2), y2 = cy + (roomH / 2);

                // #585: if this room would sit on something already standing, it is not built at all. An
                // empty patch of corridor is a facility with a gap in it; a room you can see and cannot enter
                // is a lie, and the audit reports it as one.
                bool clash = false;
                foreach ((double ax0, double ay0, double ax1, double ay1) in claimed)
                {
                    clash |= x1 < ax1 && x2 > ax0 && y1 < ay1 && y2 > ay0;
                }
                if (clash)
                {
                    continue;
                }
                claimed.Add((x1 - 1.5, y1 - 1.5, x2 + 1.5, y2 + 1.5));

                // Three walls and a corridor-facing face with a gap in it.
                walls.Add(new(x1, y1, x2, y1, true));
                walls.Add(new(x1, y2, x2, y2, true));
                walls.Add(new(side < 0 ? x1 : x2, y1, side < 0 ? x1 : x2, y2, true));

                double faceX = side < 0 ? x2 : x1;
                walls.Add(new(faceX, y1, faceX, cy - DoorHalf, true));
                walls.Add(new(faceX, cy + DoorHalf, faceX, y2, true));

                if (Frac(bodyId, tag + ":locked") < 0.5)
                {
                    locked.Add(new(faceX, cy - DoorHalf, faceX, cy + DoorHalf, SignFor(bodyId, level, tag)));
                }
                else
                {
                    doorways.Add(new SurfaceLayout.Doorway(faceX, cy - DoorHalf, faceX, cy + DoorHalf));
                    rooms.Add((cx, cy));
                }
            }
        }
    }

    /// <summary>What is painted on a door. Institutional, expensive, and never explanatory — the register of
    /// a place with serious funding and nothing to say for itself.</summary>
    public static string SignFor(string bodyId, string tag) => SignFor(bodyId, 0, tag);

    /// <summary>#592 · The same, on a named floor — so the band nobody listed gets ITS OWN vocabulary. This
    /// overload is the one <see cref="Build"/> calls; the level-less form is kept for callers that only want
    /// the site's own register.</summary>
    public static string SignFor(string bodyId, int level, string tag)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        string[] signs = SignsFor(KindOn(bodyId, level));
        ulong seed = DiceRule.Seed($"hive-sign:{bodyId}:{tag}");
        return signs[(int)(seed % (ulong)signs.Length)];
    }

    /// <summary>Each kind's own door vocabulary. This is most of what makes one clandestine site feel unlike
    /// another, and it costs nothing but words: a corridor of doors reading INTAKE / GRADING / DISPATCH is a
    /// different building from one reading COLD STORE / ASSAY / PATTERN INTEGRITY, even laid on the same
    /// bones. Every list is institutional and none of it explains anything.</summary>
    public static string[] SignsFor(Kind kind) => kind switch
    {
        Kind.Laboratory =>
        [
            "CONTINUITY — AUTHORISED ONLY", "PATTERN INTEGRITY", "COLD STORE 2", "ASSAY",
            "SUBJECT PREP", "CALIBRATION", "POWER — LOCKED OUT", "LONG STORAGE — DO NOT OPEN",
        ],
        Kind.ProcessingDepot =>
        [
            "INTAKE", "GRADING", "DISPATCH", "HOLDING 3", "OCCUPATIONAL REVIEW",
            "SCHEDULING", "PAYROLL", "QUOTA OFFICE", "DO NOT ADMIT UNESCORTED",
        ],
        Kind.RecordsAnnex =>
        [
            "RECORDS — SEALED", "INDEX", "DUPLICATES", "RETENTION 40 YR", "MICROFORM",
            "DESTRUCTION QUEUE", "CLERKS", "AUDIT — NO ADMITTANCE",
        ],
        Kind.BlackClinic =>
        [
            "MEDICAL", "REHABILITATION", "RECOVERY 2", "THEATRE", "PHARMACY — LOCKED",
            "CONSENT FILES", "AFTERCARE", "MORTUARY",
        ],
        _ =>
        [
            "MANIFEST OFFICE", "BONDED HOLD", "CUSTOMS — SEALED", "CREW MUSTER",
            "LOADING 4", "TRANSIT REGISTER", "QUARANTINE", "OUTBOUND — AUTHORISED ONLY",
        ],
    };

    // ── WHAT YOU CARRY OUT ──────────────────────────────────────────────────────────────────────────────
    //
    // Owner: "those sites should have good loot of stuff and information also... like dirt on potential
    // contacts ... the works."
    //
    // The second half is the interesting one and it is the reason these places belong in this game rather
    // than in a shooter. A crate of credits is a number going up. A FILE ON SOMEBODY is a thing you can spend
    // on a person — and this game already has the people: the bar contacts, the barkeeps, the harbourmasters'
    // seconds, the families in #588's kits. A records annex under a moon is where you learn that the man who
    // sets the docking fees at The Tilt has a name in a payroll he should not be in.
    //
    // It is left entirely open whether the captain USES it. That is the whole point of leverage.

    /// <summary>What a room in one of these places holds.</summary>
    public enum Haul
    {
        /// <summary>Stripped. Load-bearing, as everywhere else on this ground.</summary>
        Nothing,
        /// <summary>Hardware worth money — the "good loot of stuff".</summary>
        Equipment,
        /// <summary>Somebody's file. Leverage on a person the captain can actually go and meet.</summary>
        Dirt,
        /// <summary>Operational paper: a manifest, a route, a schedule. Points somewhere else.</summary>
        Records,
        /// <summary>A way through a door somewhere — a code, a card, a countersigned authority.</summary>
        Key,
    }

    /// <summary>What is in this room. Weighted so the place feels stripped but worth walking: about a third
    /// empty, and DIRT is the rarest thing in the building because it is the most valuable.</summary>
    public static Haul InRoom(string bodyId, int level, int roomIndex)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        int face = DiceRule.Roll(DiceRule.Seed($"hive:haul:{bodyId}:{level}:{roomIndex}"), 9).Face;

        // #592 · THE PAYOFF FOR REACHING THE FLOOR NOBODY LISTED IS INFORMATION, NOT A BIGGER NUMBER.
        //
        // The issue is explicit about this and it is the right call: a crate of credits is a number going
        // up, and this game already has the better currency. Down here the rooms are heavy with paper —
        // FILES ON PEOPLE, and the operational record of what was moved and how often — because that is the
        // shape of a secret worth digging a shaft nobody wrote down for.
        //
        // Deliberately NOT more Equipment. If the hidden floor paid in hardware it would be a loot room with
        // a story painted on it, and every captain would end up describing it as "the good level".
        if (IsUnlisted(bodyId, level))
        {
            return face switch
            {
                1 or 2 => Haul.Nothing,       // still stripped. Somebody cleared this too, and in a hurry.
                3 => Haul.Equipment,
                4 or 5 => Haul.Records,
                6 => Haul.Key,
                _ => Haul.Dirt,               // a third of the floor is a file on somebody
            };
        }

        return face switch
        {
            1 or 2 or 3 => Haul.Nothing,
            4 or 5 => Haul.Equipment,
            6 or 7 => Haul.Records,
            8 => Haul.Key,
            _ => Haul.Dirt,
        };
    }

    /// <summary>Whose file it is, and what is in it. The subject is one of the standing roles a captain
    /// actually deals with, so the leverage has somewhere to be spent.</summary>
    public static string DirtOn(string bodyId, int level, int roomIndex)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        string[] subjects =
        [
            "the harbourmaster's second at The Tilt",
            "the man who sets the docking fees at Selene Gate",
            "the yard foreman at Highport Satellite Works",
            "the quiet one who drinks alone at The Rusty Roadstead",
            "the clerk who signs the bonded holds at Ringside Exchange",
            "the duty officer at The Deep",
        ];
        string[] findings =
        [
            "is in a payroll here they have no business being in, at a grade they were never qualified for",
            "signed for eleven consignments that the manifest office says never arrived",
            "was paid a settlement by an office that denies existing, and cashed it",
            "appears in the visitor book four times, always after midnight, always alone",
            "countersigned a transfer order for a person whose file is three rooms from here",
            "is listed as next of kin for somebody they have never once mentioned",
        ];

        ulong seed = DiceRule.Seed($"hive:dirt:{bodyId}:{level}:{roomIndex}");
        string who = subjects[(int)(seed % (ulong)subjects.Length)];
        string what = findings[(int)((seed / 7) % (ulong)findings.Length)];
        return $"🗃 A file, and it is not the file you were expecting: {who} {what}. " +
            "Nobody buried this here by accident. You can hold on to it, or you can never mention it. " +
            "Both of those are decisions.";
    }

    /// <summary>The line for the rest of the hauls.</summary>
    public static string HaulLine(Haul haul, string bodyId, int level, int roomIndex) => haul switch
    {
        Haul.Equipment =>
            "🧪 Bench hardware, crated and never unpacked — the good stuff, bought with somebody's grant and " +
            "abandoned with the lights on. It will fetch a great deal from people who will not ask.",
        Haul.Records =>
            "📋 Operational paper: rosters, routes, a shipping schedule with a column nobody has labelled. It " +
            "does not say what was moved. It says exactly how often, and to where.",
        Haul.Key => KeyLine(bodyId, level),
        Haul.Dirt => DirtOn(bodyId, level, roomIndex),
        _ =>
            "🚪 Stripped to the fittings. Whoever cleared this room did it carefully and did it in a hurry, " +
            "which are two different things and both of them are here.",
    };

    /// <summary>What the panel says when this car has gone as deep as it goes. It does not hint, it does not
    /// unlock, and there is no button that was hiding: the building simply continues past what this shaft was
    /// dug to reach, which is the honest reason a facility has more than one lift.</summary>
    public static string EndOfTheLineLine(int floorsDown) =>
        $"🛗 The panel has no button below B{floorsDown}. This car was dug to serve the top of the building " +
        "and nothing else — whatever is under you was reached another way, by somebody with their own shaft " +
        "and their own reasons. It is down here somewhere.";

    // ── #590 · THE AUTHORITY CARD, WHICH NOW OPENS SOMETHING ────────────────────────────────────────────
    //
    // Owner: "could there be like a keycode etc that allows us access to the lab" — and, earlier the same
    // session, "Coordinates / instructions about places and sights, pin codes to doors etc."
    //
    // Haul.Key already existed and already said "Something down here will open for this." It opened nothing,
    // which is worse than not offering it at all (the #212 law: an affordance you can see and cannot use is
    // worse than none). This is that promise kept.
    //
    // THREE CALLS, each overrulable in one line:
    //
    // 1. IT AUTHORISES THE NEXT SHAFT BAND, and nothing else. #590 offered three candidate shapes and this
    //    is the load-bearing one: the car already serves a BAND and stops, and the way down is already "a
    //    different shaft, somewhere on this floor, which you have to find". A card turns that from a wall
    //    into a thing you EARN by working the band you are on. Depth stops being a number and becomes a
    //    reward.
    //
    // 2. THE SEALED SECTOR DOORS STAY SEALED. #590's option (2) is explicitly declined. Those doors exist to
    //    be walls with a world behind them, and LockedLine deliberately never teases; the moment one of them
    //    can open, every one of them becomes a puzzle and the illusion of scale turns into a lock hunt.
    //    A card never opens a SECTOR door, and TheAuthorityCardTests pins that.
    //
    // 3. NEVER A CODE THE PLAYER TYPES. You have the card or you do not. A keypad minigame would be out of
    //    register with everything around it, and the owner's own phrasing — "allows us access" — is about
    //    possession, not about a puzzle.
    //
    // Canon holds: a card may be countersigned by an office that denies existing. It never says what the
    // building was for.

    /// <summary>Which shaft band this card runs. The identity is the fact — a card is for one band of one
    /// facility, decided by the world rather than by the moment it is used.</summary>
    public readonly record struct AuthorityCard(string BodyId, int Band)
    {
        /// <summary>The stable string a save file and a carried-cards set hold.</summary>
        public string Id => $"{BodyId}#{Band}";

        /// <summary>Read one back off a save. Returns false on anything that is not a card we wrote.</summary>
        public static bool TryParse(string? id, out AuthorityCard card)
        {
            card = default;
            if (id is null)
            {
                return false;
            }
            int cut = id.LastIndexOf('#');
            if (cut <= 0 || !int.TryParse(id.AsSpan(cut + 1), System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture, out int band) || band < 0)
            {
                return false;
            }
            card = new AuthorityCard(id[..cut], band);
            return true;
        }
    }

    /// <summary>Does this site have a shaft band that deep at all? Band 0 is the one the surface lift head
    /// serves; a band exists when its top floor is still inside the site's own depth.
    ///
    /// <para>#592: measured against <see cref="TrueDepthOf"/>, not the listed depth — so a Key found on the
    /// last floor the building admits to issues the card for the band it does not. That composition IS the
    /// way in: the panel never mentions the shaft, and a piece of paper somebody left in a room does.</para></summary>
    public static bool SiteHasBand(string bodyId, int band) =>
        band >= 0
        && (BandTop(band) >= DepthOf(bodyId)
            || (HasUnlistedBand(bodyId) && band == UnlistedBandOf(bodyId)));

    /// <summary>#590 · WHICH card a Key room holds: the one for the shaft band immediately below the floor
    /// you found it on. Not a roll — a fact about the building, and the most legible possible rule, because
    /// it means the card you need for the next shaft is always somewhere in the band you are standing in.
    ///
    /// <para>Returns null at the bottom band, where there is no shaft below to authorise. That Key is not
    /// wasted: the client turns it into a lead naming another moon, which is the same payoff Records and
    /// Dirt already give and keeps the deepest floor from handing out a card for a hole nobody dug.</para></summary>
    public static AuthorityCard? CardInRoom(string bodyId, int level)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        int next = BandOf(level) + 1;
        return SiteHasBand(bodyId, next) ? new AuthorityCard(bodyId, next) : null;
    }

    /// <summary>What is printed on the card. Institutional, expensive, and explains nothing — the register
    /// of an office that will not admit to being one.</summary>
    public static string CardTitle(AuthorityCard card)
    {
        string[] offices =
        [
            "OFFICE OF WORKS · SUB-REGISTRY",
            "MINISTRY LIAISON · UNNUMBERED",
            "ESTATES · SPECIAL PROJECTS",
            "PROCUREMENT · SCHEDULE C",
            "INSPECTORATE · NO STANDING",
        ];
        ulong seed = DiceRule.Seed($"hive:card:{card.BodyId}:{card.Band}");
        return $"🎫 SHAFT {card.Band + 1} · {offices[(int)(seed % (ulong)offices.Length)]}";
    }

    /// <summary>The Key haul, said out loud. It now names the shaft it runs, because a card whose purpose is
    /// a mystery is a keypad by another route.</summary>
    public static string KeyLine(string bodyId, int level)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        if (CardInRoom(bodyId, level) is not { } card)
        {
            return "🎫 An authority card, countersigned twice and still active — and issued for a " +
                "shaft in a building that is not this one. Whoever carried it worked somewhere else, and came " +
                "here, and did not leave.";
        }
        return $"🎫 An authority card, countersigned twice and still active: {CardTitle(card)}. This " +
            "building never got the news that its owners stopped paying, and neither did its gates. The " +
            "second shaft is somewhere on these floors, and this runs it.";
    }

    /// <summary>What the gate says when the card works. Said once, at the moment the car goes deeper than
    /// this shaft was ever dug to.</summary>
    public static string CardAcceptedLine(AuthorityCard card) =>
        $"🎫 You find the other shaft where the plan said a shaft would be, and its gate reads the " +
        $"card without hesitating — {CardTitle(card)}, countersigned by an office that stopped answering " +
        "its own post decades ago and never once revoked a thing. The car below is colder than the one above.";

    /// <summary>What the gate says when you are carrying authorities and none of them is this one. The
    /// failure has to name what is wrong with it — silence here would read as a bug.</summary>
    public static string WrongCardLine(int floorsDown, IEnumerable<AuthorityCard> held)
    {
        ArgumentNullException.ThrowIfNull(held);
        var names = new List<string>();
        foreach (AuthorityCard c in held)
        {
            names.Add(CardTitle(c));
        }
        if (names.Count == 0)
        {
            return $"🔒 The second shaft is here, below B{floorsDown}, and its gate wants an " +
                "authority this building has not issued in a long time. Somebody who worked these floors was " +
                "carrying one. They did not take it with them.";
        }
        return $"🔒 The second shaft's gate reads what you are carrying, and declines it. " +
            $"{string.Join("; ", names)} — every one of them countersigned, current, and for another " +
            "shaft. The card that runs THIS one is on these floors somewhere.";
    }

    /// <summary>#585 · The card the first descent earns. Owner: "I think we need to gen AI pop-up about
    /// finding the elevator" — and he is right that it is the beat of the whole feature: the moment a moon
    /// stops being a field with things on it and becomes a lid.</summary>
    public const string DescentArtUrl = "art/the-descent.jpg";

    public const string DescentCardLabel = "🛗 THE SHAFT";

    /// <summary>What the card says beside the picture. Scale, and the cost of digging it — never a word about
    /// what it was for.</summary>
    public const string DescentCard =
        "The gate rattles down and the car starts, and it does not stop starting.\n\n" +
        "Service lamps go past in the wall at first, then a rhythm, and you find you have been counting " +
        "them and have lost count. The shaft is LINED. Somebody cut this out of a moon and then finished " +
        "it: poured walls, bolted rails, lamps on a circuit that is somehow still live.\n\n" +
        "Nobody does this quietly. A hole this deep is surveyed, funded, staffed and inspected; it has " +
        "invoices, and a schedule, and a name on a form somewhere. And yet the only thing above it is a " +
        "shed with a maintenance plate, on a moon with no register entry, on nobody's chart.\n\n" +
        "The car keeps going down. You have time to think about that, and you would rather not.";

    /// <summary>What the lift says as it starts down. The one beat of scale before any of the plan is drawn.</summary>
    public const string DescendingLine =
        "🛗 The car takes a moment to decide you are allowed, and then it drops. It keeps dropping. Whatever " +
        "this was, nobody dug it in an afternoon and nobody paid for it out of pocket.";

    /// <summary>#592 · Said ONCE, on stepping out onto the first floor the building never admitted to.
    ///
    /// <para>The whole beat of the feature, and the hardest place in the game to hold the canon line. It may
    /// say that the operation upstairs was enormous, funded, staffed and inspected, and that this was under
    /// it, and that the people who worked upstairs did not know. It may not say what it was for. The captain
    /// gets the arithmetic and never the answer — and if they want one, the files are in the rooms and the
    /// files are about PEOPLE.</para></summary>
    public static string UnlistedArrivalLine(int floorsAbove, Kind above, Kind here) =>
        $"🕳 The doors part on a floor that is not on the plan in the lobby.\n\n" +
        $"{floorsAbove} storeys of {TitleOf(above).TrimStart('▣', ' ').ToLowerInvariant()} over your head — " +
        "surveyed, funded, staffed, inspected, invoiced. Every one of those floors had a number and a " +
        "department and a plate beside the lift. This one has a lift and no plate.\n\n" +
        $"And the doors down here do not read like the doors up there. They read like " +
        $"{TitleOf(here).TrimStart('▣', ' ').ToLowerInvariant()}.\n\n" +
        "Somebody dug a second shaft, off the directory, to serve four floors that the people working " +
        "upstairs went home every night without knowing were under them. That is not secrecy from an enemy. " +
        "That is secrecy from your own staff, and it costs more.";

    /// <summary>What a floor with no plate calls itself when the captain looks for a name.</summary>
    public const string UnlistedFloorLine =
        "🕳 No plate by the lift, no department, no number painted anywhere. The building has floors it " +
        "does not count, and you are standing on one.";

    /// <summary>Said on stepping out on the top floor — the lie that makes the rest work.</summary>
    public const string PressurisedLine =
        "🫁 The doors part on warm air and standing lights. Your suit stops drawing and the readout holds. " +
        "Somewhere a fan is still turning, on somebody's account, decades after the last invoice.";

    /// <summary>And on every floor below it.</summary>
    public const string DeadAirLine =
        "🫁 The doors part on nothing. No pressure, no lights but yours, and the dust on the floor has not " +
        "been disturbed since it settled. Your tank starts counting again. From here down, depth costs air.";

    /// <summary>What a locked door says when the captain tries it. It never opens, and the game never pretends
    /// it might — a door that teases is a puzzle, and this is meant to be a WALL with a world behind it.</summary>
    public static string LockedLine(string sign) =>
        $"🔒 {sign}. The lock is not a lock you can argue with — it is a decision somebody made, and it is " +
        "still being enforced by a building whose owners stopped answering a long time ago.";

    private static double Lerp(double a, double b, double t) => a + ((b - a) * t);

    private static double Frac(string bodyId, string tag) =>
        (DiceRule.Roll(DiceRule.Seed($"{bodyId}:{tag}"), 4096).Face - 1) / 4095.0;
}
