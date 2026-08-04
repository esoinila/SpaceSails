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

        /// <summary>#411/#635 · THE HEAD OFFICE. Owner ruling, 2026-08-03: <i>"The KAAMOS destination is the
        /// HEAD of the organization. Not another outpost, not a bigger wintering camp: the place everything
        /// else answers to … The Hive facilities are branch offices. HQ outclasses them, and it should
        /// outclass them IN THE SAME VOCABULARY, so a player who has crawled a Hive recognises the rank
        /// difference without being told it."</i>
        ///
        /// <para>It is <b>never rolled</b>. <see cref="KindFor"/> assigns it to exactly one body in the
        /// system and the die that picks the other five has never heard of it — a head office that a seed
        /// could produce twice would not be a head office.</para></summary>
        HeadOffice,
    }

    /// <summary>
    /// #411 · Is the building under this body THE head office? There is exactly one, it is under the ice
    /// moon, and the id comes from the arc that owns it (<see cref="KaamosLore.IceMoonBodyId"/>) rather than
    /// from a literal typed here — one source of truth, and the reason nothing in this file has to know what
    /// PROJEKTI KAAMOS is.
    ///
    /// <para>Everything below asks this ONE question and then answers in the branch-office vocabulary the
    /// player already reads. That is the whole of the ruling: not a new grammar, the same grammar at a rank
    /// nobody has to be told about.</para></summary>
    public static bool IsHeadOffice(string bodyId) =>
        string.Equals(bodyId, KaamosLore.IceMoonBodyId, StringComparison.Ordinal);

    /// <summary>
    /// #411 · IS THE HEAD OFFICE THERE? The only site in the game whose existence is a fact about the
    /// CAPTAIN rather than about the moon.
    ///
    /// <para>Every other clandestine site is a one-in-forty roll on a body id. This one is there when the
    /// berth-code has resolved and the hull is on the board (<see cref="KaamosLore.CanReachEnceladus"/>),
    /// and it is <b>not there</b> otherwise — not sealed, not refused, not hinted at. Featureless ice and a
    /// good view.</para>
    ///
    /// <para>That refusal-by-ABSENCE is the whole reason the arrival lands, and it is the honest reading of
    /// an arc every one of whose shards is about a filing, a window, a berth or a manifest and not one of
    /// which is about fuel: nobody is stopping a captain going to the ice moon. What nobody can do is be
    /// EXPECTED there.</para>
    ///
    /// <para>Pure and world-blind, so the client asks rather than deciding — a rule this load-bearing living
    /// as an <c>if</c> in a partial class is a rule no test can reach.</para></summary>
    public static bool HeadOfficePresent(string bodyId, bool onTheBoard) =>
        IsHeadOffice(bodyId) && onTheBoard;

    /// <summary>Which kind hides under this body. Seeded, so a moon has the site it has — except the one
    /// that is not a matter of chance.</summary>
    public static Kind KindFor(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        if (IsHeadOffice(bodyId))
        {
            return Kind.HeadOffice;
        }

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
        Kind.HeadOffice => "▣ THE HEAD OFFICE",
        _ => "▣ THE TRANSIT STATION",
    };

    /// <summary>#694 · DOES THIS FLOOR GET THE FACILITY PLATE — the sign beside the shaft that says
    /// <see cref="TitleOf"/> of <see cref="KindOn"/>?
    ///
    /// <para>Owner, standing on B11 of a deep site: <i>"every floor has the text 'The Clinic' on it. Some
    /// kind of artifact?"</i> It was not an artifact and it was not a leak — the plate simply drew on every
    /// floor, and a name repeated identically twenty floors deep stops being a name and becomes wallpaper.
    /// His reaction IS the spec.</para>
    ///
    /// <para><b>A building says its name where you ENTER it.</b> That is two floors and only two:</para>
    /// <list type="bullet">
    /// <item><b>B1</b> — the lobby. You came down from the surface and the plate tells you what you have
    /// walked into.</item>
    /// <item><b>The unlisted band's top floor</b>, where the site has one (#592) — its own lobby, reached by
    /// a card and a shaft nobody listed, and the one place in the game where the plate names a
    /// <i>different</i> Kind from everything above it. <c>▣ THE CLINIC</c> first seen under twelve floors of
    /// <c>RETENTION 40 YR</c> is that whole feature's arithmetic delivered by one sign.</item>
    /// </list>
    ///
    /// <para><b>Not every band head.</b> B5 and B9 are shaft heads too, and they get nothing: a captain
    /// stepping out there has not entered anything, they have gone deeper into the same place. What earns
    /// the plate is a Kind you have not been told yet — which is exactly B1 and the unlisted lobby, and the
    /// reason this cannot be simplified to "is this floor a band top".</para>
    ///
    /// <para>The head office needs no exception and gets none: it has twenty-four listed floors and, by
    /// <see cref="HasUnlistedBand"/>, nothing under them, so <c>▣ THE HEAD OFFICE</c> falls on B1 alone. HQ
    /// naming itself once, in its own lobby, is more in character than HQ naming itself twenty-four times —
    /// the head office does not have to keep telling you where you are.</para>
    ///
    /// <para>Every other floor is answered by the plate over the car (<c>B11 · LONG STORAGE</c>) and the
    /// department signage, which is floor identity and always was. Pure, so the law is testable without a
    /// renderer and the renderer only has to ask.</para></summary>
    public static bool ShowsFacilityPlate(string bodyId, int level)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        // Outdoors is not a floor of the facility.
        if (level >= 0)
        {
            return false;
        }

        // B1 — BandTop(0), and written as the head of the first shaft rather than as a typed −1, because
        // that is what makes it the LOBBY: the floor the surface's own car opens onto.
        if (level == BandTop(0))
        {
            return true;
        }

        return HasUnlistedBand(bodyId) && level == BandTop(UnlistedBandOf(bodyId));
    }

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

        // #411 · The head office takes the whole allowance, and LISTS all of it. A branch office's depth is
        // a roll; HQ's is the building. Note this is the DEPTH IT ADMITS TO, and for once that is also the
        // bottom of the hole — see HasUnlistedBand.
        if (IsHeadOffice(bodyId))
        {
            return DeepestPossibleFloor;
        }

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

        // #411 · THE HEAD OFFICE HAS NOTHING TO HIDE FROM ITSELF, and that absence is the rank difference.
        // A branch office lies by omission to its own staff — a shaft not in the directory, a floor the
        // panel does not list. Here the directory is complete, every button is on the panel, and the whole
        // building is on the plan in the lobby. It does not need to keep a secret from the people who work
        // here, because the people who work here are the ones keeping it.
        //
        // BELT AND BRACES, said out loud rather than left to be discovered: this is currently REDUNDANT.
        // The head office already takes the whole allowance (DepthOf == DeepestPossibleFloor), so the second
        // guard below — "a band whose own shaft head would be clamped to nothing is not a band" — already
        // says no. It stays because the redundancy is the point: the day somebody raises the performance
        // bound, HQ would silently grow a band it does not admit to and the whole rank difference this file
        // is built on turns inside out. Its guard was proven RED by forcing this TRUE rather than by
        // deleting it, which is the honest way to say "dead today, load-bearing tomorrow".
        if (IsHeadOffice(bodyId))
        {
            return false;
        }

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

    // ── #608 · THE REFUGES — A DEAD FLOOR IS A FLOOR OF SUIT-WORK, AND SUITS RUN OUT ─────────────────────
    //
    // Owner, in the order he said it, after suffocating on B2: "I thought there is air in the base?" ...
    // "there should be a warning or something :-D ... the rooms should have airlocks etc ... some havens
    // :-D" ... "like the basement is more dangerous than the surface now :-D" ... "on surface there are
    // emergency shelters :-D" ... "Still for safety there would need to be a couple of places with air lock
    // and air refilling, because otherwise the elevator being busy could kill employees, and those honest
    // criminal scientists are hard to recruit :-D" ... and finally, deciding it:
    //
    //     "there should be like at least one air replenish station in each of the airless labs
    //      underground... for pure safety"
    //
    // AT LEAST ONE, ON EVERY AIRLESS FLOOR. Not "most floors", not "a rare one" — a regulation, in-world and
    // in code, and RefugesAreOnEveryAirlessFloor walks every floor of every band on every body to say so.
    //
    // THE REASON IT IS RIGHT, which is the owner's and is better than the mechanic it costs. He also ruled
    // on why any floor down here is pressurised at all: "the thought about the dead floors is that it is
    // very difficult to work in the suit. So all work would happen out of it. So any room that would house
    // like office work would be pressurized by that constraint" — "like writing with a pen ... reading
    // documents etc.... that kind of thing would not happen at all in vacuum as a working environment" —
    // "or any kind of fine motor skill stuff".
    //
    // So an airless floor is not an ABANDONED floor. It is a floor of SUIT-WORK: storage, hauling, plant,
    // hard-vacuum process. It had people in it, in suits, all day, every day — and a building that staffs a
    // vacuum floor and gives its staff nowhere to go when a tank runs short is a building that is one busy
    // lift away from killing somebody. Whoever inspected this place made them pay for the refuge. That the
    // pressure vessels are still holding decades after the last invoice is the same sentence the surface
    // shelter tells (#573): somebody built this for a stranger and it outlasted them.
    //
    // WHAT IT DOES NOT DO IS CANCEL #585. Depth is still paid for in air, because a refuge is not a floor:
    //
    //   * it is NEVER beside the lift (MinRefugeDetourDu) — reaching one is a decision to detour, which is
    //     the verb #608 asked for: not "how long dare I stay" but "can I get from the car to the refuge to
    //     the room I want and back";
    //   * its rack is the SURFACE rack, law for law — SurfaceShelter.Produce/Transfer and the two-thirds
    //     ceiling somebody set on purpose for the next person through the door. More refuges buy RANGE,
    //     never independence, exactly as more shelters do;
    //   * it holds pressure and nothing else. There is no locker down here, no reload, no bunk.
    //
    // Canon holds: the plate says what the room is FOR and never what the building was for. A safety sign is
    // the one thing on this ground that is allowed to be plain — a captain who cannot find air is not being
    // teased (#573) — and it is still an inspectorate's sign, not an explanation.

    /// <summary>Half the breathable width of a refuge, in deck units — the room's own box, inset by the
    /// poured wall. <see cref="RefugeHolds"/> is the one place that reads it.</summary>
    public const double RefugeHalfWidth = 6.3;

    /// <summary>Half the breathable height of a refuge.</summary>
    public const double RefugeHalfHeight = 4.8;

    /// <summary>How far a refuge must stand from the lift before it counts as one worth having.
    ///
    /// <para>#608: <i>"Never on the way. If it sits beside the lift it is decoration; it earns its existence
    /// by being somewhere you have to decide to detour to."</i> Measured from the shaft, so this is the
    /// smallest walk a captain can ever be asked for — and it is a floor plan, so the real one is longer.</para>
    ///
    /// <para><b>Why 70 and not 34.</b> This shipped for an hour as 34, which was chosen by eye and was
    /// WORTHLESS: the nearest room to the shaft that this generator can produce, measured over 808 dead
    /// floors, is 34.2 du out — so every room on every floor qualified, the constraint selected nothing, and
    /// the guard that was supposed to enforce it passed happily on a build deliberately rigged to put the
    /// refuge in the closest room there is. That is the house rule this repo names out loud (revert the fix
    /// and watch the guard go RED), and it caught a threshold that meant nothing.</para>
    ///
    /// <para>At 70 it is twice the nearest possible room and still satisfiable on every floor the generator
    /// makes, so the detour is real AND the fallback below never has to fire.</para></summary>
    public const double MinRefugeDetourDu = 70.0;

    /// <summary>Is (<paramref name="x"/>, <paramref name="y"/>) inside the air of the refuge centred at
    /// (<paramref name="cx"/>, <paramref name="cy"/>)?
    ///
    /// <para><b>The one containment law</b>, so Core, the audit and the live suit cannot disagree about
    /// whether the captain is breathing. Rectangular rather than the shelter's inscribed ellipse
    /// (<c>SurfaceShelter.Contains</c>) for the one reason that matters: a shelter is a regolith drum and
    /// its corners are metres of piled dirt, while this is a POURED ROOM with square corners — an ellipse
    /// here would leave a captain standing plainly inside a sealed room watching their tank tick down, which
    /// is precisely the kind of instrument-disagrees-with-the-world lie this ground keeps paying for.</para></summary>
    public static bool RefugeHolds(double cx, double cy, double x, double y) =>
        Math.Abs(x - cx) <= RefugeHalfWidth && Math.Abs(y - cy) <= RefugeHalfHeight;

    /// <summary>One pressure refuge: a room somebody kept the seals on, with an air cracker in it.</summary>
    public readonly record struct Refuge(double X, double Y, string Sign)
    {
        /// <summary>Is the captain in its air? <see cref="RefugeHolds"/>, so there is only ever one answer.</summary>
        public bool Contains(double x, double y) => RefugeHolds(X, Y, x, y);
    }

    /// <summary>What is stencilled beside a refuge door. An inspectorate's plate: a number, an occupancy and
    /// a date somebody stopped renewing — which is the whole story of this building told by a form.</summary>
    public static string RefugeSign(string bodyId, int level, int index)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ulong seed = DiceRule.Seed($"hive:refuge-sign:{bodyId}:{level}:{index}");
        int number = (int)(seed % 40) + 1;
        int occupancy = 4 + (int)((seed / 11) % 9);
        return $"🫁 PRESSURE REFUGE {number} · OCCUPANCY {occupancy} · KEEP CLEAR";
    }

    /// <summary>The refuges on a floor, taken out of the rooms it had already built.
    ///
    /// <para>A refuge IS one of the floor's rooms — three poured walls and a doorway cut in its corridor
    /// face — and that is deliberate rather than lazy. A room is already audited walkable from the lift
    /// (13.1), already has a door the captain can find, and already sits down a rib rather than on the
    /// spine. Inventing a second kind of chamber would be a second thing to keep reachable, and a refuge you
    /// cannot walk to is a refuge that does not exist.</para>
    ///
    /// <para>It stops being a haul room when it becomes one: a pressure vessel somebody maintained is not a
    /// drawer to turn over, and the air is what it pays.</para></summary>
    private static List<Refuge> CarveRefuges(
        string bodyId, int level, List<(double X, double Y)> rooms, double shaftX, double shaftY)
    {
        var refuges = new List<Refuge>();
        if (HoldsPressure(level) || rooms.Count == 0)
        {
            return refuges;   // a pressurised floor IS the refuge
        }

        // #592 · The one room that may never be taken. On a site with a band nobody listed, room 0 of the
        // last listed floor is the card that reaches it (KeyRoomFor) — designated exactly because a rolled
        // index would sometimes miss and strand the whole feature forever. Turning it into a refuge would
        // do the same thing by a different route.
        int reserved = KeyRoomFor(bodyId) is { } key && key.Level == level ? key.RoomIndex : -1;

        var faraway = new List<int>();
        var anywhere = new List<int>();
        for (int i = 0; i < rooms.Count; i++)
        {
            if (i == reserved)
            {
                continue;
            }
            anywhere.Add(i);
            double dx = rooms[i].X - shaftX, dy = rooms[i].Y - shaftY;
            if ((dx * dx) + (dy * dy) >= MinRefugeDetourDu * MinRefugeDetourDu)
            {
                faraway.Add(i);
            }
        }

        // The detour is the design, so it is preferred — but it is NOT allowed to cost the guarantee. On a
        // floor whose rooms all happen to crowd the shaft, a near refuge beats no refuge, every time: the
        // owner's line is "at least one ... for pure safety", and a safety regulation that a seed can talk
        // out of is not one.
        List<int> pool = faraway.Count > 0 ? faraway : anywhere;
        if (pool.Count == 0)
        {
            return refuges;
        }

        int pick = pool[DiceRule.Roll(DiceRule.Seed($"hive:refuge:{bodyId}:{level}"), pool.Count).Face - 1];
        (double rx, double ry) = rooms[pick];
        rooms.RemoveAt(pick);
        refuges.Add(new Refuge(rx, ry, RefugeSign(bodyId, level, 0)));
        return refuges;
    }

    /// <summary>Said once, stepping into a refuge's air on a dead floor. The relief, and the reason it is
    /// there — which is a form somebody filed, not a kindness.</summary>
    public const string RefugeBreathingLine =
        "🫁 The inner door cycles behind you and the readout stops falling. Pressure — in a room somebody " +
        "was made to build, on a floor nobody was ever meant to be caught out on. The seals held.";

    /// <summary>What the console inside is called.</summary>
    public const string RefugeTankLabel = "🫁 REFUGE RACK";

    /// <summary>What the plate over the door says at signage size — short enough to read at a run, because
    /// that is how it will be read.
    ///
    /// <para>It names the ROOM, not the floor, and that word is load-bearing (#612). The plate by the lift
    /// is simultaneously shouting NO ATMOSPHERE about the level; a sign forty du away reading only AIR
    /// would be a second instrument appearing to contradict the first, which is the one thing #612 says is
    /// worse than saying nothing. <c>REFUGE ·</c> makes the scope of the claim part of the claim.</para></summary>
    public const string RefugeGlyph = "🫁 REFUGE · AIR";

    /// <summary>What the level is called on the lift panel and the plan header. Named by depth band rather
    /// than from a hand-written list, because there is no longer a fixed bottom to write down.
    ///
    /// <para><b>There is no site-blind overload of this, on purpose.</b> There used to be, and it was fine
    /// for exactly as long as every building in the game used one list of department names. The moment a
    /// second building had its own plates (#411's head office), a body-blind <c>NameOf(level)</c> became a
    /// second answer to "what is this floor called" — which is this repo's most expensive shape, and the
    /// comment explaining it would have been a TODO with no owner. Everything asks with the body.</para></summary>
    public static string NameOf(string bodyId, int level)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        if (level >= 0)
        {
            return "SURFACE";
        }

        if (IsUnlisted(bodyId, level))
        {
            return $"B{-level} · NO PLATE";
        }

        return $"B{-level} · {DepartmentOf(bodyId, level)}";
    }

    /// <summary>#605 · THE DEPARTMENTS, in one place. They were a `string[]` local inside <see cref="NameOf"/>,
    /// which was fine while the name was the only thing that used them — the moment a floor's COLOUR also
    /// depends on which department it is, two copies of this list would be two answers to one question, and
    /// this ground has a table at the top of its spec full of exactly that.</summary>
    public static readonly string[] Departments =
    [
        "ADMINISTRATION", "LABORATORIES", "LONG STORAGE", "PLANT",
        "ARCHIVE", "ISOLATION", "DEEP STORAGE", "UNMARKED",
    ];

    /// <summary>
    /// #411 · THE HEAD OFFICE'S OWN PLATES — twenty-four of them, one per floor, <b>none repeated</b>.
    ///
    /// <para>That is the rank difference, in the branch-office vocabulary and costing one list of words. A
    /// branch reuses its plate stock (eight names on a cycle, so B1 and B9 are both ADMINISTRATION and are
    /// meant to feel alike); the head office had a plate made for every floor it has. A captain who has
    /// crawled a Hive reads the fourth un-repeated plate and knows what kind of building they are in.</para>
    ///
    /// <para>And the list is the story. Nobody narrates any of it: it starts as an office and turns, somewhere
    /// around the middle, into a vocabulary about people, and never turns back. BRANCH LIAISON is the one that
    /// does the most work — the department plates by a Hive's lift car are a branch-office idiom, and this is
    /// the office that ISSUED them. What none of these words ever do is explain anything.</para>
    /// </summary>
    public static readonly string[] HeadOfficeDepartments =
    [
        "RECEPTION", "ESTABLISHMENT", "THE REGISTRY", "SCHEDULING & WINDOWS",
        "PROCUREMENT", "CONTRACTS", "BRANCH LIAISON", "AUDIT",
        "PAYROLL — CLOSED ACCOUNTS", "LONG CONTRACTS", "CONTINUITY", "THE STANDING ORDER",
        "SITE ESTABLISHMENT", "DISPATCH", "THE COLD ROOMS", "OCCUPANCY",
        "WELFARE", "THE WINTER OFFICE", "RESIDENCY", "THE QUIET ROOMS",
        "DEEP RESIDENCY", "THE WATER GALLERY", "THE WINTERING HALL", "THE BERTH OFFICE",
    ];

    /// <summary>
    /// #411 · WHICH FLOOR A HEAD-OFFICE PLATE IS ON — read out of the plate list rather than typed twice.
    ///
    /// <para>Three floors of this building have a beat on them (the standing order, the wintering hall, the
    /// berth office) and every one of them is identified by its PLATE. Writing "−23" beside "THE WINTERING
    /// HALL" in a second place would be the same fact in two files, and this repo has a table of what that
    /// costs: re-order the departments once and the beat fires on ATTENDANCE instead, with every test still
    /// green because both numbers agree with themselves.</para>
    ///
    /// <para>Throws on a plate that is not in the list, deliberately: a beat pointed at a floor that does not
    /// exist should fail loudly at the first call, not go quietly missing on some worlds forever.</para></summary>
    public static int HeadOfficeLevelOf(string plate)
    {
        ArgumentNullException.ThrowIfNull(plate);
        int index = Array.IndexOf(HeadOfficeDepartments, plate);
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(plate), plate, "no floor of the head office carries that plate");
        }

        return -(index + 1);
    }

    /// <summary>B12 — the room the takeable evidence is in.</summary>
    public static int StandingOrderLevel => HeadOfficeLevelOf("THE STANDING ORDER");

    /// <summary>B23 — the room this arc was written for.</summary>
    public static int WinteringHallLevel => HeadOfficeLevelOf("THE WINTERING HALL");

    /// <summary>B24 — the office that never stopped filing.</summary>
    public static int BerthOfficeLevel => HeadOfficeLevelOf("THE BERTH OFFICE");

    /// <summary>The plate stock this building draws on.</summary>
    public static string[] DepartmentsFor(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return IsHeadOffice(bodyId) ? HeadOfficeDepartments : Departments;
    }

    /// <summary>Which department a level belongs to. A branch office cycles, so a deep site repeats — and
    /// that repetition is the point. The head office does not, because it never had to.</summary>
    public static string DepartmentOf(string bodyId, int level)
    {
        string[] stock = DepartmentsFor(bodyId);
        return stock[(-level - 1) % stock.Length];
    }

    /// <summary>
    /// #605 · WHAT COLOUR THIS FLOOR IS PAINTED. Owner, riding between floors cut from the same bones:
    /// <i>"Let's like change the wall colors on different floors... now they look too same"</i> — and then,
    /// naming the reference: <i>"We could use something like star trek og or Babylon 5 colors for different
    /// purposes ... command, medical, so fourth"</i>.
    ///
    /// <para>The important call: the livery belongs to the DEPARTMENT, not to the floor number. A colour per
    /// floor would be noise — pretty, and telling you nothing. A colour per department is a LANGUAGE (the
    /// spec's §11, "colour is a language"): two ADMINISTRATION floors nine levels apart look alike because
    /// they ARE alike, and a captain learns the building instead of learning a gradient.</para>
    ///
    /// <para>Muted on purpose. These are painted bands on poured concrete in a facility that stopped being
    /// maintained decades ago, not a bridge set — they read as livery at a glance and never compete with the
    /// consoles, which are the only things down here that mean "you may touch this".</para>
    ///
    /// <para><b>Null on a floor nobody listed (#592).</b> A livery is something a department paints on its own
    /// corridor, and those floors have no department and no plate. So the band nobody admits to is the one
    /// place the concrete is left bare — the ABSENCE is the tell, and it costs not one word of narration.</para>
    /// </summary>
    public static BodyPalette.Ink? LiveryFor(string bodyId, int level)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        if (level >= 0 || IsUnlisted(bodyId, level))
        {
            return null;
        }

        // #411 · THE HEAD OFFICE PAINTS BY WING, AND STEPS DOWN THE WING.
        //
        // Two rules at once, because the first one alone broke the second and a shipped guard said so:
        //
        //   · a HUE per shaft band, so six wings read as six wings and a captain who has ridden one knows
        //     at a glance which one they are in;
        //   · a VALUE per floor within the wing, because the owner's original complaint about this whole
        //     feature was "now they look too same", and ConsecutiveFloorsNeverLookAlike is that complaint
        //     turned into a law. Painting a wing one flat colour handed four identical floors straight back.
        //
        // So the language has two words instead of one, which is MORE legible than the branch office's, not
        // less — the hue says where in the building, the value says how far down the wing. That is what a
        // place with a signage budget does, and it costs one multiplier.
        //
        // Kept CLEAN, which is the whole tell: a branch office's colour is painted concrete somebody stopped
        // maintaining decades ago, and this is the same six-hue language still being kept up, by nobody, on
        // a schedule.
        if (IsHeadOffice(bodyId))
        {
            BodyPalette.Ink wing = BandOf(level) switch
            {
                0 => new BodyPalette.Ink(214, 186, 112),   // reception gold, and it still looks new
                1 => new BodyPalette.Ink(126, 176, 226),
                2 => new BodyPalette.Ink(136, 186, 146),
                3 => new BodyPalette.Ink(178, 156, 214),
                4 => new BodyPalette.Ink(190, 220, 218),
                _ => new BodyPalette.Ink(124, 178, 190),   // the deep wings, colder and no less kept
            };

            // Which step down the wing this floor is: the top of the band is the full value, and each floor
            // under it is darker. Never zero-length, because a band always has a top.
            int step = -level - 1 - (BandOf(level) * FloorsPerShaft);
            double k = 1.0 - (0.13 * Math.Clamp(step, 0, FloorsPerShaft - 1));
            return new BodyPalette.Ink(
                (byte)Math.Round(wing.R * k), (byte)Math.Round(wing.G * k), (byte)Math.Round(wing.B * k));
        }

        return DepartmentOf(bodyId, level) switch
        {
            "ADMINISTRATION" => new BodyPalette.Ink(198, 170, 98),   // command gold
            "LABORATORIES" => new BodyPalette.Ink(108, 156, 206),    // sciences blue
            "LONG STORAGE" => new BodyPalette.Ink(120, 166, 130),    // stores green
            "PLANT" => new BodyPalette.Ink(198, 112, 90),            // engineering rust
            "ARCHIVE" => new BodyPalette.Ink(154, 136, 194),         // records violet
            "ISOLATION" => new BodyPalette.Ink(172, 208, 206),       // medical pale
            "DEEP STORAGE" => new BodyPalette.Ink(92, 142, 152),     // deep teal
            _ => new BodyPalette.Ink(152, 158, 168),                 // UNMARKED — a grey that is not a colour
        };
    }

    // #592 · The floor a directory never listed has no department, because a department is a thing you
    // write on a plan — and the whole point of those floors is that nobody wrote them anywhere. That case now
    // lives inside NameOf above, with the rest of the naming, rather than in a wrapper around a body-blind
    // twin of it.

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
        IReadOnlyList<Rib> Ribs,
        IReadOnlyList<Refuge> Refuges);

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
        // #605 · The floor's name used to be pinned 26 du off down the spine, which is most of a screen
        // from the only thing that tells you which floor you are on. It is painted at the LIFT now
        // (HiveInterior), stacked under the depth, so the plate and the number are read together.

        // ── THE SHAFT. Same spot on every floor.
        walls.Add(new(shaftX - ShaftHalf, shaftY + CorridorHalf, shaftX - ShaftHalf, shaftY + CorridorHalf + 5, true));
        walls.Add(new(shaftX + ShaftHalf, shaftY + CorridorHalf, shaftX + ShaftHalf, shaftY + CorridorHalf + 5, true));
        walls.Add(new(shaftX - ShaftHalf, shaftY + CorridorHalf + 5, shaftX + ShaftHalf, shaftY + CorridorHalf + 5, true));
        // #605 · The "LIFT" plate is gone from here. The console at the car mouth is already labelled LIFT,
        // and the signage stack above it (HiveInterior) now answers the bigger question in the same wall
        // space. Three plates on one wall is a wall nobody reads.

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
                    SealedMouthSign(bodyId, i, km)));
            }
            walls.Add(new(x - CorridorHalf, far, x + CorridorHalf, far, true));

            AddRoomsAlong(walls, doorways, locked, rooms, claimed, bodyId, level, i, x, mouth, far, down);
        }

        // #608 · LAST, because a refuge is taken out of the rooms this floor actually managed to build. Any
        // earlier and it would be a designated INDEX rather than a designated ROOM — and the claim ledger
        // above drops a room whenever one would sit on something already standing, so an index chosen before
        // the loop is an index that sometimes names nothing. That is exactly the shape of the bug KeyRoomFor
        // was written to avoid, and a safety regulation may not be the second thing in this file to trip
        // over it.
        List<Refuge> refuges = CarveRefuges(bodyId, level, rooms, shaftX, shaftY);

        return new FloorPlan(level, NameOf(bodyId, level), HoldsPressure(level),
            walls, doorways, locked, labels, rooms, ribList, refuges);
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

    /// <summary>
    /// #411 · What is painted on a corridor mouth that will not open — the cheapest illusion of scale there
    /// is, and the one plate where the rank difference is a single number.
    ///
    /// <para>A branch office says <c>SECTOR 7 · 2.4 km</c>: a serious operation, and a distance a captain can
    /// imagine walking. The head office says <c>WING 3 · 24.6 km</c> — the same plate, the same typeface, one
    /// order of magnitude, and a word that says the thing beyond it is not a sector of this building but a
    /// whole PART of it. Nobody is told which is bigger.</para></summary>
    public static string SealedMouthSign(string bodyId, int index, double km)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return IsHeadOffice(bodyId)
            ? $"\u27F6 WING {index + 1} \u00b7 {km * 10.0:F1} km"
            : $"\u27F6 SECTOR {7 + index} \u00b7 {km:F1} km";
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
        Kind.HeadOffice =>
        [
            // #411 · The head office's own register: nothing clandestine, nothing furtive, nothing that
            // sounds like it is hiding. It sounds like a HEAD OFFICE — which, once a captain has read four
            // floors of it under a kilometre of ice with nobody in the corridors, is far worse.
            "OFFICE OF THE REGISTRAR", "STANDING ORDERS — CURRENT", "BRANCH RETURNS", "ESTABLISHMENT BOARD",
            "COMMITTEE ROOM 2", "SIGNATURES", "APPROPRIATIONS", "THE LONG LEDGER",
            "DEPUTATIONS", "MINUTES — SEALED", "ATTENDANCE", "THE STANDING LIST",
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

    /// <summary>#592 · Which room is GUARANTEED to hold the way down, on a site that has something to hide.
    ///
    /// <para>Null on an ordinary site: there is nothing under it, so nothing has to be findable and every
    /// Key stays a roll. On a site with an unlisted band it is a room on the last floor the building admits
    /// to — the floor a captain is standing on when the panel goes quiet, which is exactly where somebody
    /// would have been carrying one.</para>
    ///
    /// <para><b>Room 0, not a seeded index.</b> This function is pure of the field, so it cannot know how
    /// many rooms that floor actually has — and the count varies: the four-room floor law is asserted for
    /// the scenario's own bodies, and a generated site can produce a floor with three. A seeded 0..3 index
    /// therefore misses sometimes, which puts the guarantee back exactly where it started. Room 0 always
    /// exists on any floor worth riding to.</para>
    ///
    /// <para>Nobody can see the index, so nothing is lost by it being fixed — a player finds a room, not a
    /// number — and the alternative costs a floor plan on every haul lookup.</para></summary>
    public static (int Level, int RoomIndex)? KeyRoomFor(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return HasUnlistedBand(bodyId) ? (DepthOf(bodyId), 0) : null;
    }

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

        /// <summary>#614 · The thing on the pallet. Exactly one room in a whole facility, and only in the
        /// band nobody listed.</summary>
        Relic,
    }

    /// <summary>#614 · WHERE THE THING ON THE PALLET IS, and why it is not a roll.
    ///
    /// <para>Same reasoning as <see cref="KeyRoomFor"/>, for the same reason: a one-in-N object placed by
    /// seeded dice is an object that is silently absent on some worlds FOREVER, and nothing on screen ever
    /// says so. Every test still passes and the best thing in the game is simply missing from a third of the
    /// universe.</para>
    ///
    /// <para>So it is designated: the deepest floor of the band nobody listed. Sites without an unlisted band
    /// have no relic at all, which is correct — it is the payoff for getting somewhere you were not supposed
    /// to be able to reach, and a facility that admits to its own depth has nowhere to put it.</para>
    ///
    /// <para><b>Room 0.</b> A floor's room count depends on the site's field, so the only index a
    /// field-free designation may safely name is the one every floor has. Room 0 cannot collide with
    /// <see cref="KeyRoomFor"/> either: that one sits on the LISTED bottom, and a site only has a relic when
    /// its true depth runs deeper than the depth it admits to.</para></summary>
    public static (int Level, int RoomIndex)? RelicRoomFor(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return HasUnlistedBand(bodyId) ? (TrueDepthOf(bodyId), 0) : null;
    }

    /// <summary>
    /// #411 · THE ONE PIECE OF PAPER WORTH CARRYING OUT OF THE HEAD OFFICE, designated for exactly the
    /// reason <see cref="KeyRoomFor"/> and <see cref="RelicRoomFor"/> are: a seeded roll would leave it
    /// silently absent on some threads forever, and nothing on screen would ever say so.
    ///
    /// <para>It is a <see cref="Haul.Records"/> room and not a <see cref="Haul.Relic"/> one, and that is the
    /// honest call rather than the convenient one — the relic's own prose describes a band of alloy on a
    /// pallet, and dressing a sheet of paper in it would be the sim doing one thing while the sentence said
    /// another. Records already goes into the satchel as paper, which is all this needs.</para></summary>
    public static (int Level, int RoomIndex)? StandingOrderRoomFor(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return IsHeadOffice(bodyId) ? (StandingOrderLevel, 0) : null;
    }

    /// <summary>What is in this room. Weighted so the place feels stripped but worth walking: about a third
    /// empty, and DIRT is the rarest thing in the building because it is the most valuable.</summary>
    public static Haul InRoom(string bodyId, int level, int roomIndex)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        // #592 · THE ONE ROOM THAT IS NOT A ROLL.
        //
        // The way into the band nobody listed is a card, and a card comes out of a Key room on the last
        // floor the building admits to. Key is one face in nine, and a last band holds thirty-odd rooms, so
        // about one site in thirty would roll no Key at all — and because the rolls are seeded, that site's
        // hidden band would be unreachable NOT for that visit but FOREVER.
        //
        // Nothing on screen would ever say so, which is the only reason this is not the "map lies" bug; it
        // is the quieter one where a feature is silently dead on some worlds and every test still passes.
        // So one room on the last listed floor is designated, deterministically, and holds the way down.
        if (KeyRoomFor(bodyId) is { } wayDown && level == wayDown.Level && roomIndex == wayDown.RoomIndex)
        {
            return Haul.Key;
        }

        // #614 · And the one room that holds the thing nobody signed for. Designated for the same reason as
        // the Key room above — see RelicRoomFor.
        if (RelicRoomFor(bodyId) is { } pallet && level == pallet.Level && roomIndex == pallet.RoomIndex)
        {
            return Haul.Relic;
        }

        // #411 · And the head office's one designated room — the sheet that says the runs were to continue
        // until countermanded, in a folder with nothing else in it.
        if (StandingOrderRoomFor(bodyId) is { } order && level == order.Level && roomIndex == order.RoomIndex)
        {
            return Haul.Records;
        }

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

    /// <summary>The line for the rest of the hauls.
    ///
    /// <para>#678 · <paramref name="minted"/> is the card the caller ACTUALLY handed over for a
    /// <see cref="Haul.Key"/> — null when none was, which happens on the bottom band whenever the client's
    /// far-site fallback comes up empty. It is a required parameter rather than an optional one for the
    /// reason <see cref="NameOf"/> has no site-blind overload: a defaulted "no card" would be a second answer
    /// to "what does this room say", silently wrong at exactly the one call site that matters.</para></summary>
    public static string HaulLine(Haul haul, string bodyId, int level, int roomIndex, AuthorityCard? minted)
        => haul switch
    {
        Haul.Equipment =>
            "🧪 Bench hardware, crated and never unpacked — the good stuff, bought with somebody's grant and " +
            "abandoned with the lights on. It will fetch a great deal from people who will not ask.",
        // #411 · The head office's designated sheet reads as itself; everywhere else, operational paper.
        Haul.Records when StandingOrderRoomFor(bodyId) is { } o && level == o.Level && roomIndex == o.RoomIndex
            => StandingOrderLine,
        Haul.Records =>
            "📋 Operational paper: rosters, routes, a shipping schedule with a column nobody has labelled. It " +
            "does not say what was moved. It says exactly how often, and to where.",
        Haul.Key => KeyLine(bodyId, level, minted),
        Haul.Dirt => DirtOn(bodyId, level, roomIndex),

        // #614 · The room is described. The thing is NOT explained, here or anywhere: the pulse says what is
        // in front of you and the card (CarriedObject.CollarStory) says what it measures, and between them
        // they never once say what it was for. Canon holds hardest exactly here.
        Haul.Relic =>
            "⭕ The room is a bay, and there is one thing in it: a band of dark alloy on a pallet, taller " +
            "than you are and machined inside and out. Nobody stripped this room. They left it, and they " +
            "left the lights on over it.",
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
    /// of an office that will not admit to being one.
    ///
    /// <para>#679 · AND IT SAYS WHICH SITE. Owner, holding three of them: <i>"a captain holding three cards
    /// from three moons sees three identical shapes and cannot plan a wallet."</i> He is right, and the fix
    /// is the least invented thing available: a pass has ALWAYS had the holder's place of work printed on
    /// it. So the site designation goes on the face, in the office's own register — caps, like everything
    /// else that office stamps — as the last field of the title.</para>
    ///
    /// <para>This is a deliberate softening of §13.10's <i>"never which moon"</i>, made by the owner in #679
    /// and recorded there: the line that must not be crossed is a NAV FIX. A site code sorts a wallet; a
    /// bearing and a distance would hand the captain the search the whole Hive is arranged around. It still
    /// never says what the building was for (§13.8), which is the canon that actually matters.</para></summary>
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
        return $"🎫 SHAFT {card.Band + 1} · {offices[(int)(seed % (ulong)offices.Length)]} · " +
            $"{BodyNames.Designation(card.BodyId)} SITE";
    }

    /// <summary>The Key haul, said out loud. It names the shaft it runs, because a card whose purpose is a
    /// mystery is a keypad by another route.
    ///
    /// <para>#678 · IT DESCRIBES THE CARD THE CALLER ACTUALLY MINTED, and there are three of those: the one
    /// for the shaft under this building, #613's card for ANOTHER site, and — the case that broke it — no
    /// card at all. It used to ask <see cref="CardInRoom"/> itself and narrate a countersigned authority in
    /// the captain's hand whenever the answer was null, which was a sentence about an object the sim had not
    /// handed over. That is the third named bug class, in the residual path of the fix made for it.</para></summary>
    /// <param name="minted">The card that went into the pocket, or null if none did.</param>
    public static string KeyLine(string bodyId, int level, AuthorityCard? minted)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        if (minted is not { } card)
        {
            // Nothing was minted, so nothing is described. The room still pays what an ordinary room pays —
            // a look at what somebody did on their way out — and it never once claims you are holding a card.
            string[] empty =
            [
                "🪪 A lanyard on the floor and the holder still clipped to it, and the window in the holder " +
                "is empty. Whoever ran the shafts off this floor left with the one thing in this room worth " +
                "taking, and the counterfoil book agrees with them: signed out, never signed back in.",

                "🪪 A drawer of counterfoils, and every stub in it is torn along the same crooked line. The " +
                "cards themselves went out of this building in somebody's breast pocket. What is left is the " +
                "half the office kept, which opens nothing and was never meant to.",

                "🪪 A punch, an inking pad gone hard, and a rack of blanks that were never made out to " +
                "anybody. This is where the authorities were issued. It is not where they ended up.",
            ];
            ulong seed = DiceRule.Seed($"hive:nokey:{bodyId}:{level}");
            return empty[(int)(seed % (ulong)empty.Length)];
        }

        if (!string.Equals(card.BodyId, bodyId, StringComparison.Ordinal))
        {
            // #613's wallet, and #679's site code on the face of it: a card that crossed a world in somebody
            // else's pocket and is still good at gates you have not found yet.
            return $"🎫 An authority card, countersigned twice and still active: {CardTitle(card)} — and " +
                "that is a building which is not this one. Whoever carried it worked somewhere else, and " +
                "came here, and did not leave.";
        }

        return $"🎫 An authority card, countersigned twice and still active: {CardTitle(card)}. This " +
            "building never got the news that its owners stopped paying, and neither did its gates. The " +
            "second shaft is somewhere on these floors, and this runs it.";
    }

    // ── #678 · THE POCKET NEVER LIES ────────────────────────────────────────────────────────────────────
    //
    // Owner, after a live playtest: "we should have CI test that makes sure all picked items that sound
    // useful are put into the inventory ... If refused the item should stay where it was investigated last —
    // not disappear like they do now, or seem to."
    //
    // Two silent drops, one law. The pickup sentence and the pickup were composed in the client in the wrong
    // order — the line was printed, the room was marked emptied, and only then did Satchel.Add get a chance
    // to refuse — so a full pocket ate a find while announcing it, and a Key room whose card could not be
    // minted narrated a countersigned card into a hand that was empty. Both are this repo's third named bug
    // class: the sim doing one thing while the sentence reports another.
    //
    // The composition lives here now, pure, where a test can walk every haul against every pocket. The rule
    // it enforces, in one line:
    //
    //     A PICKUP LINE MAY ONLY BE PRINTED FOR SOMETHING THAT ACTUALLY WENT IN.
    //
    // And its other half: what the pocket cannot take is NOT consumed. The room keeps it, and searching
    // again offers it again — which is the enforcement side of #615 (leave must not destroy).

    /// <summary>#678 · What turning over one room actually yields: the thing that goes in the pocket (null if
    /// nothing does), the sentence that says so, and whether the room has been emptied at all.</summary>
    /// <param name="Take">The item to add, or null — nothing to add is not a failure, it is most rooms.</param>
    /// <param name="Line">The pocket line appended to the haul line. Empty where there is nothing to say.</param>
    /// <param name="RoomEmptied">False ONLY when the pocket refused the find. The caller must not mark the
    /// room searched — the find is still lying there.</param>
    public readonly record struct Pickup(Satchel.Item? Take, string Line, bool RoomEmptied);

    /// <summary>#678 · What goes in the pocket, said in the same breath as the decision to put it there.</summary>
    /// <param name="haul">What the room holds.</param>
    /// <param name="hereBodyId">The site being searched — used only to tell a card for THIS building from a
    /// card for another one, which is the one thing worth saying about an authority as it goes in.</param>
    /// <param name="minted">For a <see cref="Haul.Key"/>, the card the caller actually minted. Null means no
    /// card exists to hand over, and then the room says so rather than describing one.</param>
    /// <param name="findId">The durable id of this find — the seed tag the prose is rebuilt from.</param>
    /// <param name="carried">What is already in the pocket.</param>
    public static Pickup WhatGoesInThePocket(
        Haul haul, string hereBodyId, AuthorityCard? minted, string findId,
        IReadOnlyList<Satchel.Item>? carried)
    {
        ArgumentNullException.ThrowIfNull(hereBodyId);
        ArgumentNullException.ThrowIfNull(findId);

        Satchel.Item? take = haul switch
        {
            Haul.Records => new Satchel.Item(Satchel.Kind.Paper, findId),
            Haul.Dirt => new Satchel.Item(Satchel.Kind.Dirt, findId),

            // #614 · What goes in the pocket is the RECORD of the thing on the pallet. You cannot lift it,
            // and a satchel claiming to hold a three-metre alloy band would be the same lie one size up.
            Haul.Relic => new Satchel.Item(Satchel.Kind.Relic, findId),
            Haul.Key when minted is { } card => new Satchel.Item(Satchel.Kind.Authority, card.Id),
            _ => null,
        };

        if (take is { } wanted && !Satchel.CanTake(carried, wanted))
        {
            return new Pickup(null, PocketFullLine, RoomEmptied: false);
        }

        string line = haul switch
        {
            Haul.Records => "  🎒 Into your pocket: operational paper.",
            Haul.Dirt => "  🎒 Into your pocket: a file on somebody.",
            Haul.Relic => "  🎒 Into your pocket: measurements, a photograph, a scraping. The thing itself " +
                "stays where it is.",
            Haul.Key when minted is { } c && !string.Equals(c.BodyId, hereBodyId, StringComparison.Ordinal)
                => "  🎒 Into your pocket: an authority card — and it is not for this building.",
            Haul.Key when minted is not null => "  🎒 Into your pocket: an authority card.",
            Haul.Equipment => "  💳 Crated and carried out — it sells, it does not fit a pocket.",

            // A stripped room, and a Key room that had no card left to give. Neither has anything to say
            // about a pocket, and saying nothing is the honest answer for both.
            _ => "",
        };

        return new Pickup(take, line, RoomEmptied: true);
    }

    /// <summary>#678 · What a full pocket says. It is the only refusal in the game that leaves the world
    /// unchanged, and it has to be unmistakable about that: the find is still there.</summary>
    public const string PocketFullLine =
        "  🎒 Your hands and pockets are full, so you put it back exactly where it was lying. It will still " +
        "be here when you have read, spent or left something behind.";

    /// <summary>What the gate says when the card works. Said once, at the moment the car goes deeper than
    /// this shaft was ever dug to.
    ///
    /// <para>#592: worded so it is true of BOTH shafts it can open. It used to say "where the plan said a
    /// shaft would be" — right about the listed building, and a lie about the band the plan denies having.
    /// A card that announces the secret is a card that has given it away.</para></summary>
    public static string CardAcceptedLine(AuthorityCard card) =>
        $"🎫 You find the other shaft. It is not marked and it is not beside the first one, and its gate " +
        $"reads the card without hesitating — {CardTitle(card)}, countersigned by an office that stopped " +
        "answering its own post decades ago and never once revoked a thing. The car below is colder than " +
        "the one above.";

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

    // ── #411 · AND THE OTHER ONE. The first descent at the head office is not the same beat as the first
    //    descent at a branch office, and giving it the same card would be the loudest missed opportunity in
    //    the arc: the whole ruling is that a captain who has crawled a Hive should recognise the rank on
    //    sight. So the establishing shot is its own, and it is built out of the same four things the Hive's
    //    is — a shaft, a directory, a lobby, a floor — with every one of them answered differently.
    //
    //    Discipline, harder here than anywhere: EVIDENCE, then stop. The card may say the lamps come up
    //    ahead of the car. It may not say who turned them on.

    public const string HeadOfficeArrivalArtUrl = "art/kaamos-head-office.jpg";

    public const string HeadOfficeArrivalLabel = "🧊 THE HEAD OFFICE";

    /// <summary>The first descent at the head office, said once. Four paragraphs, and not one of them tells
    /// the captain what any of it means.</summary>
    public const string HeadOfficeArrivalCard =
        "The car does not go down a shaft so much as down a BUILDING.\n\n" +
        "Service lamps go past in the wall at first, the way they do everywhere. Then the shaft opens out " +
        "and the lamps stop being service lamps: they are lobby lighting, warm and even, and they come up " +
        "ahead of the car and go down behind it.\n\n" +
        "The doors part on a floor built to receive people. Stone facing around the lift surround. A bench. " +
        "A rack for coats with nothing on it. And a directory beside the doors that lists TWENTY-FOUR floors " +
        "— all of them, none of them abbreviated, none of them missing.\n\n" +
        "There is no dust on the floor. Not undisturbed dust. None.";

    /// <summary>Which establishing card this building's first descent earns — asked in one place so the two
    /// can never be shown for the wrong building.</summary>
    public static (string Label, string ArtUrl, string Card) FirstDescentCard(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return IsHeadOffice(bodyId)
            ? (HeadOfficeArrivalLabel, HeadOfficeArrivalArtUrl, HeadOfficeArrivalCard)
            : (DescentCardLabel, DescentArtUrl, DescentCard);
    }

    // ── #411 · THE THREE FLOORS WITH A BEAT ON THEM ──────────────────────────────────────────────────
    //
    // Every one of these is EVIDENCE and stops. Between them they say that somebody set an enormous thing
    // going, that nobody ever stopped it, and that it is still going. Not one of them says what it is, who
    // is doing it, or what any of it means — canon holds hardest exactly here, because this is the deepest
    // and most tempting room in the game.

    /// <summary>B12 · the sheet in the folder. Per #614's law a card may say WHAT and never WHERE: this
    /// names an instruction and an unused countersignature block, and no place at all.</summary>
    public const string StandingOrderLine =
        "📋 One countersigned sheet in a folder with nothing else in it: the runs are to continue UNTIL " +
        "COUNTERMANDED. Underneath, a countersignature block — ruled, printed, and never used. The folder has " +
        "been opened often enough to wear the crease through, and closed again every time.";

    public const string WinteringHallArtUrl = "art/kaamos-wintering-hall.jpg";

    public const string WinteringHallLabel = "❄❄ FORTY-ONE";

    /// <summary>B23. The room this arc was written for, and the only card in the game that is allowed to do
    /// arithmetic — because counting is a thing a captain does with their own eyes, and the count is the
    /// whole beat. It still never says whose the last one is.</summary>
    public const string WinteringHallCard =
        "The floor is one room, and the room does not end where the lamps do.\n\n" +
        "Four rows of ten, and every one of them is MADE. Not stripped, not stacked, not sheeted over for a " +
        "shutdown: made. The blanket turned back at the same angle on all forty. The pillow squared. Along " +
        "one side the wall is glass a hand thick and behind it there is black water going down further than " +
        "the lamps reach.\n\n" +
        "At the end of the fourth row, apart from the others by the width of a walkway, there is one more. " +
        "Turned back at the same angle. Squared.\n\n" +
        "Forty-one.";

    /// <summary>Said on the pulse line as well, because the card is dismissed and the log is not.</summary>
    public const string WinteringHallLine =
        "❄ You count them twice, from the far end the second time, because the first answer was not the one " +
        "you expected.";

    /// <summary>Why the nerve goes. Deliberately does not state the arithmetic — the captain has just done it.</summary>
    public const string WinteringHallShockReason =
        "a room that has been kept made up for a very long time, and the count in it";

    public const string BerthOfficeArtUrl = "art/kaamos-berth-office.jpg";

    public const string BerthOfficeLabel = "❄ ONE LINE STILL LIT";

    /// <summary>B24. The last floor, and the smallest room on it. It is the only untidy room in the building
    /// — and it is untidy with its own output, which is the tidiest possible reason.</summary>
    public const string BerthOfficeCard =
        "One console, one board, one line lit.\n\n" +
        "It is the only room in this building that is not immaculate, and it is knee-deep. The log has been " +
        "printing continuously and folding itself onto the floor, and nobody has emptied it, because " +
        "emptying it is not a thing anybody ever wrote down.\n\n" +
        "The entries are a requisition against a berth at Ringside Exchange, filed on every cycler window, " +
        "on the tick. The acknowledgement column beside them is blank for so far back that the form has " +
        "changed twice inside the drift.\n\n" +
        "The newest sheet is still warm. Under it, queued and dated, is the next one.";

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

    // ── #609 · THE ONE THING YOU MUST NOT MISS ──────────────────────────────────────────────────────────
    //
    // Owner, after suffocating on B2: "I thought there is air in the base?" ... "there should be a warning
    // or something :-D" ... "maybe pop-up about you have air or you are in vacuum type ... it is vital info"
    // ... "like the basement is more dangerous than the surface now :-D" ... "on surface there are emergency
    // shelters :-D"
    //
    // He is right on every count, and the last two are the argument. The surface gives a captain a visible
    // building to run to; a dead floor gives them a number they have to have been told. The rule itself is
    // good and stays exactly as it is — the top of each shaft band holds pressure and the rest costs air —
    // but it was being announced in a pulse that fades in eight seconds, between one about bench hardware
    // and one about dust.
    //
    // So the first dead floor of an excursion stops the world and says it properly, WITH THE ARITHMETIC:
    // which floors have air, how far the nearest one is, and how long the tank has. After that the pulse
    // line is enough, because by then it is knowledge rather than news.

    public const string VacuumArtUrl = "art/the-dead-air.jpg";

    public const string VacuumCardLabel = "🫁 DEAD AIR";

    /// <summary>What the first dead floor says. It states the rule and does the sum — a warning that makes
    /// the captain work out their own margin is a warning delivered too late.</summary>
    public static string VacuumCard(string bodyId, int level, double airSeconds)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        int band = BandOf(level);
        int refuge = BandTop(band);          // the top of this band always holds pressure
        int floorsUp = -level - -refuge;     // how many floors between here and breathable
        string tank = airSeconds > 0
            ? $"{(int)(airSeconds / 60)} min {(int)(airSeconds % 60):00} s"
            : "whatever is left";

        string upstairs = floorsUp == 0
            ? "this floor"
            : $"{floorsUp} floor{(floorsUp == 1 ? "" : "s")} up";

        return
            "The doors part on nothing.\n\n" +
            "No pressure, no lights but yours, and the dust has not been disturbed since it settled. You " +
            $"are {MetresDown(level):F0} m under the regolith and your tank is now the clock.\n\n" +
            "THE RULE, because it is the only one down here that can kill you: the TOP FLOOR OF EVERY SHAFT " +
            "BAND holds pressure. Nothing else does. That is where the lobbies were, and the fans on those " +
            "floors are still turning on somebody's account.\n\n" +
            $"The nearest floor of air is {NameOf(bodyId, refuge)} — {upstairs}. You have {tank}.\n\n" +
            // #608 · AND THE OTHER HALF, now that it is true. This card used to end "there are no shelters
            // down here", which was honest when it was written and is now the most dangerous sentence in the
            // game: a captain who believes it will ration a tank they did not have to ration. Owner: "there
            // should be like at least one air replenish station in each of the airless labs underground...
            // for pure safety". So the card says where the exception is, and says the two things about it
            // that decide whether it is any use — it is not beside the lift, and the instrument finds it.
            "There is a PRESSURE REFUGE on this floor. Every vacuum floor in this building has one: staff " +
            "worked these levels in suits all day, and somebody with a clipboard made the owners pay for " +
            "somewhere to go when a tank ran short. It is not beside the lift — it never is — and your " +
            "tracker paints it as a ring like any shelter on the surface.";
    }

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

    /// <summary>#600 · How far under the regolith the shed's floor a given level sits, in metres.
    ///
    /// <para>Owner: <i>"we can use seriously large numbers there :-D ... or depths (in meters)"</i>. He is
    /// right that the depth is the better number — <c>B4</c> is an index and <c>−76 m</c> is a fact about
    /// where you are standing, and it is the one that makes the walk back up mean something.</para>
    ///
    /// <para>The first floor is far down because the facility is BURIED — the shed on the surface is a lid
    /// over a shaft, and the descent card earns that ("service lamps go past in the wall at first, then a
    /// rhythm, and you find you have been counting them and have lost count"). After that a floor is a
    /// floor plus its slab, its services and the rock somebody left between levels.</para>
    ///
    /// <para>Owner, reading the paint on B1: <i>"also we could make it deeper like 150 meters :-D"</i> — and
    /// he is right, 40 m was a car park. The overburden is the number that has to sell the lid, because it
    /// is the whole ride down before the first door opens, and the descent card has always described a shaft
    /// long enough to lose count in. At 150 m it does.</para></summary>
    public const double OverburdenMetres = 150.0;

    /// <summary>Floor to floor, including the slab and the rock between.</summary>
    public const double MetresPerFloor = 12.0;

    /// <summary>Metres below the surface for a level. 0 on the surface, positive going down.</summary>
    public static double MetresDown(int level) =>
        level >= 0 ? 0 : OverburdenMetres + ((-level - 1) * MetresPerFloor);

    /// <summary>What is painted on the wall beside the lift, big enough to read on the way past.</summary>
    public static string DepthPaint(int level) =>
        level >= 0 ? "SURFACE" : $"−{MetresDown(level):F0} m";

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
    /// single fact that decides whether the trip is free.</param>
    /// <param name="IsCurrent">The floor the car is on now: shown, and not a destination.</param>
    /// <param name="Refusal">Null when the button works. When set, the button is PRESENT and says why it
    /// will not — an absent button and a broken one look identical, and this ground has already shipped that
    /// mistake once.</param>
    /// <param name="OpenedBy">#689 · The title of the card in the captain's own wallet that this stop's gate
    /// will read — null on every ordinary button, and null at a gate no card opens. The positive twin of
    /// <paramref name="Refusal"/>: a sealed row says what is missing, and this one says what is HELD, before
    /// the ride rather than after it. Core decides it so the panel can never promise a reading the gate will
    /// not give (#600's rule: Core decides, the razor draws).</param>
    public readonly record struct LiftStop(
        int Level, string Name, bool Pressurised, bool IsCurrent, string? Refusal, string? OpenedBy = null);

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
    /// </summary>
    public static IReadOnlyList<LiftStop> LiftPanel(
        string bodyId, int level, IReadOnlyCollection<string> heldCardIds)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(heldCardIds);

        var stops = new List<LiftStop>
        {
            new(0, "SURFACE", Pressurised: true, IsCurrent: level >= 0, Refusal: null),
        };

        int band = BandOf(Math.Min(level, -1));
        int deepest = BandFloor(bodyId, band);
        for (int f = BandTop(band); f >= deepest; f--)
        {
            stops.Add(new(f, NameOf(bodyId, f), HoldsPressure(f), f == level, null));
        }

        int next = band + 1;
        if (!SiteHasBand(bodyId, next))
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
        bool unlisted = IsUnlisted(bodyId, BandTop(next));
        if (unlisted && !holdsIt)
        {
            return stops;   // #592: the building does not admit this exists, and neither does its panel
        }

        stops.Add(new(
            BandTop(next),
            holdsIt ? "↓ THE OTHER SHAFT" : "↓ THE OTHER SHAFT — SEALED",
            HoldsPressure(BandTop(next)),
            IsCurrent: false,
            holdsIt ? null : "This car does not go lower. The shaft that does is on this floor, and its " +
                "gate wants an authority this building has not issued in a long time.",
            // #689 · …and when the wallet has the answer in it, the row says so BEFORE the ride. Owner, after
            // playing the whole loop: "It was locked until I got it ... there was no story point about it
            // being needed or used." Never at the head office: there is no gate there to read anything, and
            // that absence is the rank difference (#411) rather than an oversight worth papering over.
            carded && !IsHeadOffice(bodyId) ? CardTitle(gateCard) : null));
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
    public static AuthorityCard? GateOpenedByRidingTo(
        string bodyId, int fromLevel, int toLevel, IReadOnlyCollection<string> heldCardIds)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(heldCardIds);

        foreach (LiftStop stop in LiftPanel(bodyId, fromLevel, heldCardIds))
        {
            if (stop.Level == toLevel && stop.OpenedBy is not null)
            {
                return new AuthorityCard(bodyId, BandOf(toLevel));
            }
        }
        return null;
    }

    // ── #528 · TWO CARDS FOR THE TWO HALVES OF A DOOR ───────────────────────────────────────────────────
    //
    // Owner, standing at a rib's far end: "I see there is a nice lock here at the end of the corridor....
    // maybe we could have a gen-AI image for it and a pop-up to tell the story?" — and then, a minute later:
    // "the authority card could also have a gen ai image to really tell the story here :-D"
    //
    // He picked the right pair without saying so. The Hive has exactly two objects that are ABOUT the idea of
    // passage: a door that will never open, and a piece of paper that opens one. Giving both the reveal-card
    // treatment (#528) makes them answer each other.
    //
    // #528's recipe, which is a recipe and not a decoration:
    //   1. a title that names the place and the verb;
    //   2. one painted image of a CONSEQUENCE rather than an action;
    //   3. a caption that describes evidence and STOPS — it never says what it means;
    //   4. it fires at the moment it explains the most.
    //
    // The hard constraint on both, and the reason they are written here rather than in the client: neither
    // may TEASE. The sealed sector doors exist to be walls with a world behind them (#590 call 2), so the
    // card about one may never suggest that anything opens it — not a key, not a code, and above all not the
    // authority card, which is a real object a captain may be carrying while they read this. A player who
    // reads "no authority on the plate" and goes off to try their card has been lied to by a card.

    /// <summary>Is this sign the far end of a rib — the sealed way on — rather than a room's door?
    ///
    /// <para>Asked of the sign itself so the client never has to recognise one by parsing a distance out of
    /// it. The prose and the plate are then the same string by construction, which is the standing rule on
    /// this ground.</para></summary>
    public static bool IsSealedWay(string sign)
    {
        ArgumentNullException.ThrowIfNull(sign);
        return sign.StartsWith('⟶');   // ⟶ SECTOR n · d.d km
    }

    public const string SealedWayArtUrl = "art/the-sealed-way.jpg";

    public const string SealedWayCardLabel = "🔒 THE WAY ON, CLOSED";

    /// <summary>#528 · The card the first sealed rib mouth earns. The plate's own text is quoted VERBATIM
    /// rather than rebuilt, so the words on the wall and the words on the card can never drift.</summary>
    public static string SealedWayCard(string sign)
    {
        ArgumentNullException.ThrowIfNull(sign);
        return
            "The corridor does not end here. It is closed here.\n\n" +
            $"{sign} — stencilled, not printed. Somebody stood where you are standing with a plate and a " +
            "brush and recorded how far the passage runs before it stops being their department. The " +
            "distance is the only thing on it. No department, no date, no name.\n\n" +
            "The seal went in after the cut: the paint on the frame is a different age from the paint on " +
            "the walls either side of it. Nobody closes a passage they have not first spent a year digging, " +
            "and nobody digs that far through a moon to reach somewhere they mean to give up.\n\n" +
            "There is no handle on this side. The bolt pattern says there is none on the other side either. " +
            "It was not shut to keep anybody out of there. It was shut to keep it shut.";
    }

    public const string AuthorityCardArtUrl = "art/the-authority-card.jpg";

    public const string AuthorityCardLabel = "🎫 THE COUNTERSIGNATURE";

    /// <summary>#528 · The card the first authority card earns — the object, described and not explained.
    ///
    /// <para>It says what the thing IS and stops. It does not say what it opens: that is what the pulse line
    /// and the gate itself are for, and a card that spelled out the mechanic would turn a find into a
    /// tutorial. What it does instead is make a laminated staff pass frightening, which is the whole tone of
    /// this facility — the horror here is administrative and it has a filing system.</para></summary>
    public static string AuthorityCardStory(AuthorityCard card) =>
        "It is heavier than it looks. A laminate over a metal core, the sort of thing made to survive a " +
        "fire in a records room.\n\n" +
        $"{CardTitle(card)}. Two countersignatures, both in the same careful hand, four years apart by the " +
        "dates and identical in pressure. A grade. A photograph of somebody who has been told not to smile " +
        "and has obeyed exactly.\n\n" +
        "The issuing office is stencilled across the top and appears in no register you have ever read. " +
        "The countersigning office is a sub-registry of the issuing one. Between them they employed the " +
        "person in the photograph, paid them, graded them, and put them on the other side of a door that " +
        "the people upstairs did not know was there.\n\n" +
        "There is no expiry field. Not an expired one — none. Somebody designed this for a building they " +
        "expected to outlive them, and they were right about the building.";

    private static double Lerp(double a, double b, double t) => a + ((b - a) * t);

    private static double Frac(string bodyId, string tag) =>
        (DiceRule.Roll(DiceRule.Seed($"{bodyId}:{tag}"), 4096).Face - 1) / 4095.0;
}
