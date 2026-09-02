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
public static partial class UndergroundComplex
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

    /// <summary>#725 · IS THIS THE LOBBY OF THE BAND NOBODY LISTED — the one floor in the game where the
    /// plate beside the shaft names a different kind of building from every floor above it.
    ///
    /// <para>Written as <see cref="ShowsFacilityPlate"/> MINUS the entrance lobby, which is the whole of it:
    /// the plate law already answers "does a building say its name here", and it says yes in exactly two
    /// places. Take B1 away and what is left is the corrected wall. Nothing here re-derives a band — a second
    /// copy of that arithmetic is the named bug class this file opens with a table of, and #694's own doc
    /// comment is explicit that this cannot be simplified to "is this floor a band top" either.</para></summary>
    public static bool IsUnlistedLobby(string bodyId, int level) =>
        ShowsFacilityPlate(bodyId, level) && level != BandTop(0);

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

    /// <summary>How far down a captain can ACTUALLY walk — the listed depth, plus the band nobody listed,
    /// plus (#677) the band nobody dug.
    ///
    /// <para>Every audit, every renderer and every lab wants this one: an unlisted floor is still a floor,
    /// and a topology nothing walks is a topology nobody has checked. Only the things that speak FOR the
    /// building — the lift panel, the directory — get to use <see cref="DepthOf"/>.</para></summary>
    public static int TrueDepthOf(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        if (HasFoundBand(bodyId))
        {
            return BandBottom(FoundBandOf(bodyId));
        }
        return HasUnlistedBand(bodyId) ? UnlistedBottomOf(bodyId) : DepthOf(bodyId);
    }

    /// <summary>#592/#677 · The deepest floor of the band nobody listed — the bottom of the BUILDING, which
    /// stopped being the same number as <see cref="TrueDepthOf"/> the day something under it turned out not
    /// to be a building at all.
    ///
    /// <para>Written down rather than inlined because two callers need exactly this and would otherwise each
    /// reach for <c>TrueDepthOf</c>: the thing on the pallet (<see cref="RelicRoomFor"/>, which belongs to
    /// the operation and not to the halls) and the guards. When #677 moved the true bottom two bands deeper,
    /// a <c>TrueDepthOf</c> here would have quietly relocated the one designated relic in the game into a
    /// gallery nobody built.</para></summary>
    public static int UnlistedBottomOf(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return BandBottom(UnlistedBandOf(bodyId));
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

    // ── #677 · THE BAND NOBODY DUG ───────────────────────────────────────────────────────────────────────
    //
    // Owner ruling 2026-08-04, recorded in worldbuilding-notes.md §10: this is humanity's FOURTH run, the
    // prior three were ENDED, and every end spared a remnant underground, in massive halls. Out on the moons
    // that is this: a dig that breaks into volume which was ALREADY THERE.
    //
    // It is a different CLASS of thing from the band nobody listed (#592, §13.7), and the whole feature turns
    // on the difference. The unlisted band is human all the way down — poured, surveyed, invoiced, and hidden
    // from the staff who paid for it. This one was never ours. So:
    //
    //   * it is one band BELOW the unlisted band, with a WHOLE BAND of nothing dug between them — §13.7's gap
    //     idiom one rung further along. The unlisted band's gap is the remainder of a band the listed building
    //     stopped inside; this gap is four floors of untouched rock that a shaft was driven straight through;
    //   * the way in is the #590 card idiom again, found in the band nobody listed — the paper telling the
    //     truth about a building that is not;
    //   * nothing down there is a facility. No plate, no department, no livery, no sealed SECTOR doors, no
    //     locked rooms, no plumbing, no fixtures of any kind. The renderer's ink and the room scale do the
    //     storytelling and not one sentence explains anything (§13.8, §13.20).
    //
    // CANON, harder here than anywhere in the game: nothing names a builder, an age or a purpose; the word
    // reserved by §8 never appears; and BOTH readings of §10 — the instruments simply got better / this was
    // always here and is being SHOWN to us — have to survive every line. If any string ever settles which,
    // the horror dies.

    /// <summary>#677 · How many of the sites that already hide a band have something under THAT, and it is
    /// deliberately rarer than <see cref="UnlistedOneInN"/>.
    ///
    /// <para>One in five of the sites that already keep a secret from their own staff. Measured rather than
    /// asserted (<c>TheFoundBandTests</c> sweeps the generator and reads the rate off the sweep), and the
    /// measured incidence is lower still, because only the shallower half of the hiding sites has room under
    /// it for another shaft inside the performance guard — see below.</para></summary>
    public const int FoundOneInN = 5;

    /// <summary>#677 · THE SITE THE <c>?found=1</c> CHEAT PARKS, and the reason a body id lives in Core.
    ///
    /// <para>A site's whole shape — its depth, its kinds, its unlisted band and its halls — is seeded off its
    /// BODY ID, so reaching this feature from a URL is a matter of parking a rock with the right name rather
    /// than of overriding a Core fact from the client. This name is a seven-floor laboratory with an unlisted
    /// clinic under it and galleries under a whole band of nothing: the full chain, in one rock. The suffix is
    /// the search that found it and not decoration — about one id in fifty has halls.</para>
    ///
    /// <para>It sits here rather than beside the cheat because <b>five places have to agree about it</b>: the
    /// cheat itself, and four sweeps that would otherwise be auditing a universe with no galleries in it and
    /// passing vacuously. Five copies of a magic string is this repo's most expensive habit, and the deep rock
    /// already costs two. Pinned by <c>TheFoundBandTests</c>: if the seeding ever stops giving this id halls,
    /// the cheat and every one of those sweeps go red together and say why.</para></summary>
    public const string FoundBandCheatSiteId = "secret-lab-site-halls-116";

    /// <summary>#677/#1063 · Does this site have a band nobody dug? Seeded off its own id, like everything
    /// else about a site's shape — <b>and no longer, once the neighbours have filled it in</b>.
    ///
    /// <para><b>#1063 · THE ONE GATE.</b> This is the single seam every question about the halls already goes
    /// through — <see cref="IsFound"/>, <see cref="FoundBandOf"/>'s customers, <see cref="TrueDepthOf"/>,
    /// <see cref="FloorsOf"/>, <see cref="NextShaftBelow"/> (through <c>SiteHasBand</c>),
    /// <see cref="FoundKeyRoomFor"/>, <see cref="DeclaresDarkness"/>, and
    /// <see cref="DisclosureClock.OpensOn"/>, which delegates to <see cref="IsFound"/> by design. So the
    /// burial is asked HERE and nowhere else: after a fill, the shaft ends at the listed bottom and every one
    /// of those predicates answers exactly as it would for a site that never had halls at all. The erasure
    /// procedure's clauses (1) <i>remove the element</i> and (2) <i>remove its marks</i> are both this one
    /// line, because the marks — the found-key card room, the hall record's find id, the darkness, the room
    /// scale, the plateless name — are every one of them downstream of it.</para>
    ///
    /// <para>The alternative was thirty callers each taught what a burial is, which is §13.15's second cause
    /// (a caller reasoning about the shape of a building it does not own) said thirty times.</para></summary>
    public static bool HasFoundBand(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        // #1063 · Filled in, floored over, resurfaced. Asked FIRST, and asked cheaply: on every world where
        // nobody has been past a seam long enough ago — which is almost every world — this is one length
        // check on an empty list and the site's shape is exactly what it always was.
        if (Burial.IsFilled(bodyId))
        {
            return false;
        }

        return FoundBandSeeded(bodyId);
    }

    /// <summary>#677/#1063 · THE SEEDED FACT, asked BEFORE any burial: does this site's own id deal it a band
    /// nobody dug?
    ///
    /// <para>Private on purpose and it must stay private. Exactly two things may ask it: the public predicate
    /// above, which is the whole game's answer, and the specimen (<see cref="SpecimenFloorOf"/>), which is the
    /// one souvenir the erasure keeps and therefore the one caller that has to know what was filled in. A
    /// third caller would be a way to see the halls past the burial, which is the feature undone.</para></summary>
    private static bool FoundBandSeeded(string bodyId)
    {
        // It hangs off the band nobody listed, so a site with nothing to hide has nothing under that either.
        // (The head office is already excluded by HasUnlistedBand, and for the reason recorded there: the
        // directory is complete and the rank difference IS the absence.)
        if (!HasUnlistedBand(bodyId))
        {
            return false;
        }

        // And only where the whole arrangement — a band of nothing, then a band of halls — still fits above
        // the generator's own floor. This is HasUnlistedBand's second guard one rung further along, and it is
        // a PERFORMANCE bound rather than a design one (#585): a band clamped to nothing is not a band.
        if (BandTop(FoundBandOf(bodyId)) <= DeepestPossibleFloor)
        {
            return false;
        }

        return DiceRule.Roll(DiceRule.Seed($"hive:found:{bodyId}"), FoundOneInN).Face == 1;
    }

    /// <summary>#677 · WHICH band the halls are, and why it is <b>two</b> bands under the listed bottom's own.
    ///
    /// <para>The band nobody listed fills its band, so the next one down would be flush against it — one
    /// shaft's floor and the next shaft's ceiling, which is how a BUILDING continues. What has to read here
    /// is that the digging stopped and something else began, so there is a whole band between them with
    /// nothing in it: the shaft was driven through four floors' worth of rock and broke into what was
    /// waiting. §13.7's gap, one rung further, and the only rung where the gap is the point rather than an
    /// artefact of where a depth happened to stop.</para></summary>
    public static int FoundBandOf(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return UnlistedBandOf(bodyId) + 2;
    }

    /// <summary>#677 · Is this floor one of the ones nobody built?</summary>
    public static bool IsFound(string bodyId, int level)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return HasFoundBand(bodyId) && level < 0 && BandOf(level) == FoundBandOf(bodyId);
    }

    /// <summary>#592/#677 · EVERY FLOOR THIS SITE ACTUALLY HAS, top to bottom — the listed ones, then the gap
    /// where nothing was dug, then the band nobody listed, then the band of nothing under THAT, then the
    /// galleries nobody dug at all.
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

        if (!HasFoundBand(bodyId))
        {
            yield break;
        }
        int found = FoundBandOf(bodyId);
        for (int level = BandTop(found); level >= BandBottom(found); level--)
        {
            yield return level;
        }
    }

    /// <summary>#677 · THE NEXT SHAFT THAT EXISTS below this floor, or null where there is none.
    ///
    /// <para>Everything that used to ask <c>BandOf(level) + 1</c> asks this instead, and the reason is the
    /// band of nothing: the band immediately under the one nobody listed has no floors in it at all, so a
    /// card minted for "the next band" would authorise a hole nobody dug and the panel would offer a button
    /// to rock. One walk over <see cref="SiteHasBand"/>, in one place, rather than two callers each teaching
    /// themselves the shape of a building that now has two gaps in it.</para></summary>
    public static int? NextShaftBelow(string bodyId, int level)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        int last = BandOf(DeepestPossibleFloor);
        for (int band = BandOf(Math.Min(level, -1)) + 1; band <= last; band++)
        {
            if (SiteHasBand(bodyId, band))
            {
                return band;
            }
        }
        return null;
    }

    /// <summary>#677 · The real floor a caller asking for <paramref name="wanted"/> should be put on: the
    /// floor of this site nearest to it, and never a floor in a gap where nothing was dug.
    ///
    /// <para>Written here because the dev floor cheat used to do this arithmetic itself — clamp to the true
    /// depth, then snap into the unlisted band's head — which is a caller reasoning about the shape of a
    /// building it does not own (§13.15's second cause). It was right about a building with one gap in it
    /// and would have set a captain down in solid rock the day there were two. Ties go DEEPER, because a
    /// cheat asking for a floor between two buildings is asking for the lower one.</para></summary>
    public static int NearestFloorTo(string bodyId, int wanted)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        int best = -1;
        int bestGap = int.MaxValue;
        foreach (int level in FloorsOf(bodyId))
        {
            int gap = Math.Abs(level - wanted);
            if (gap <= bestGap)
            {
                bestGap = gap;
                best = level;
            }
        }
        return best;
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

    /// <summary>Which floors still hold atmosphere. THE one pressure fact in this building (§13.13), and
    /// everything that shows it — the drain, the gauge, the plate by the car — asks this and nothing else.
    ///
    /// <para>Owner's biggest open question, answered with a beat in it: a floor with power lulls you and the
    /// rest costs you. Extended for unbounded depth by making it the TOP OF EVERY SHAFT BAND — that is where
    /// a facility puts its lobbies — so a captain who finds the next shaft gets one floor of relief before the
    /// dark again. It keeps a very deep site playable without ever making it safe.</para>
    ///
    /// <para><b>#677 · And it takes the BODY now, which is the whole cost of the halls.</b> Every floor of
    /// the band nobody dug holds pressure — all of it, all the way down, and nothing anywhere shows the plant
    /// that does it. Whether a floor breathes therefore stopped being arithmetic on a level and became a fact
    /// about the site, and there is deliberately NO level-only overload left: one would be a second answer to
    /// the one question §13.13 exists to keep single, silently right on every floor of every building except
    /// the four this feature is about. The compiler made every caller say which moon it is standing under,
    /// which is the strongest guard available.</para>
    ///
    /// <para><b>#802 · AND THE SURFACE IS A FLOOR IT ANSWERS FOR.</b> Owner: <i>"the surface should be
    /// vacuum / unbreathable ... being unbreathable makes the breathing so much more scary."</i> Level 0 and
    /// above is the regolith, and on every body this game lands on the regolith is airless — so the answer
    /// here is <c>false</c>, deliberately and not as a side effect of the <c>level &lt; 0</c> clause. It is
    /// stated because a caller had to ask: the lift panel typed <c>Pressurised: true</c> into its SURFACE row
    /// rather than asking, and for as long as it did, the one button every captain presses on the way out
    /// promised air on the one ground the whole tank mechanic is built on.</para></summary>
    public static bool HoldsPressure(string bodyId, int level)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        if (level >= 0)
        {
            return false;   // #802 · the regolith. Vacuum on every body, with no exception to write down.
        }
        return (-level - 1) % FloorsPerShaft == 0 || IsFound(bodyId, level);
    }

    /// <summary>#677 · IS ANYTHING PLUMBED ON THIS FLOOR — the question every amenity, cubicle and en-suite
    /// asks, and the one place the answer differs from <see cref="HoldsPressure"/>.
    ///
    /// <para>§13.17's law is that plumbing is for people out of their suits, and it is asked against the one
    /// pressure fact so that no room down here can ever breathe for a reason the plate by the lift does not
    /// know about. The halls breathe and are not plumbed, and those are not in tension: a canteen, a cubicle
    /// and a duct are all things somebody was made to PAY for, and there is no invoice down there. The air in
    /// a found gallery is not provided by anything the cone can find — that is the entire sensation (§13.20)
    /// — so a grille in one would be the building explaining the one thing it must never explain.</para></summary>
    public static bool IsPlumbed(string bodyId, int level)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return HoldsPressure(bodyId, level) && !IsFound(bodyId, level);
    }

    // ── #708 · DARKNESS IS A PROPERTY OF A FLOOR ─────────────────────────────────────────────────────────
    //
    // Owner's ruling 2026-08-05, filed with the headlights: darkness is NOT a filter somebody switches on
    // over the top of the game. It is a fact a floor states about itself, in exactly the way HoldsPressure
    // states whether the same floor can be breathed — and for exactly the same reason. The moment two things
    // in this building can each hold an opinion about whether the lights are on, the plate by the lift and
    // the picture on the screen are reading two different maps, and this ground has already paid for that
    // mistake once (§13.13, the pressure fact).
    //
    // So there is ONE ask, and everything that cares calls it: the renderer, the boot cheat, and whatever sim
    // eventually wants to know (nothing does today — a sentry's rules are its own, §13.18). The cheat is an
    // ARGUMENT to the ask, never a second answer OR-ed in beside it at a call site, because an `||` at a call
    // site is precisely how a second source of truth gets built one honest line at a time.

    /// <summary>
    /// #708 · Whether this floor is DARK: no fixtures, no failing facility light, nothing at all — the suit's
    /// headlights (<see cref="SuitLamp"/>) are the whole of the seeing there is.
    ///
    /// <para>The one ask. Nowhere else in this game gets to decide this.</para>
    ///
    /// <para><b>Above ground is never dark.</b> A surface has a sun, a sky and the #563 falloff into the
    /// unseen bound; darkness is a property of somewhere with a roof on it, and a cheat that blacked out the
    /// regolith would be testing a different feature.</para>
    /// </summary>
    /// <param name="lampsOut">The <c>?dark=1</c> boot cheat: kill the fixtures on every floor of this
    /// excursion. No shipped floor declares darkness yet (see <see cref="DeclaresDarkness"/>), so this is the
    /// only way to reach the feature today — and a scene nobody can reach on demand is a scene that ships
    /// broken.</param>
    public static bool IsDark(string bodyId, int level, bool lampsOut = false) =>
        level < 0 && (lampsOut || DeclaresDarkness(bodyId, level));

    /// <summary>
    /// #708/#677 · Whether a floor declares itself dark of its own accord.
    ///
    /// <para><b>No shipped floor does, and that is deliberate.</b> Every listed floor down here has failing
    /// facility light and the instrument-lit look it has always had; changing that would change every Hive
    /// anybody has ever played, to solve a problem those floors do not have. The customer is the FOUND BAND
    /// (#677) — galleries that pre-exist the shaft, with no fixtures, no wiring and no ventilation anybody
    /// can find — and it will answer here, in one line, when it is built.</para>
    ///
    /// <para>Dead-air floors are NOT dark and do not flicker. A flicker is a fixture reporting that it is
    /// dying; a floor that cannot be breathed is not a floor whose lamps have failed, and wiring the two
    /// together would have made the suit gauge and the ceiling say the same thing twice.</para>
    ///
    /// <para><b>#677 · The customer arrived.</b> The band nobody dug is the one thing in the game that
    /// declares itself dark, and it is one line, exactly as #708 promised. Owner's ruling: <i>"the
    /// pre-existing tunnels would be scary as dark ones and totally different style"</i> — no fixtures, no
    /// wiring, nothing that ever held a lamp. The facility's failing light stops at the poured concrete;
    /// past the seam the dark is ORIGINAL, and the cone is the whole of the seeing.</para>
    /// </summary>
    public static bool DeclaresDarkness(string bodyId, int level) => IsFound(bodyId, level);

    private static double Lerp(double a, double b, double t) => a + ((b - a) * t);

    private static double Frac(string bodyId, string tag) =>
        (DiceRule.Roll(DiceRule.Seed($"{bodyId}:{tag}"), 4096).Face - 1) / 4095.0;
}
