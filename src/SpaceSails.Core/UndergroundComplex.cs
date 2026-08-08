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

    /// <summary>#677 · Does this site have a band nobody dug? Seeded off its own id, like everything else
    /// about a site's shape, so it is a fact about the world and not about the visit.</summary>
    public static bool HasFoundBand(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

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
    /// which is the strongest guard available.</para></summary>
    public static bool HoldsPressure(string bodyId, int level)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return level < 0 && ((-level - 1) % FloorsPerShaft == 0 || IsFound(bodyId, level));
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
        string bodyId, int level, List<(double X, double Y, string Plate)> rooms,
        double shaftX, double shaftY)
    {
        var refuges = new List<Refuge>();
        if (HoldsPressure(bodyId, level) || rooms.Count == 0)
        {
            return refuges;   // a pressurised floor IS the refuge — and every gallery is one (#677)
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
        (double rx, double ry, string _) = rooms[pick];
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

        // #592/#677 · Two very different floors answer with the same four words, and that is the honest
        // answer for both: there is no plate. One has none because the building refuses to admit the floor
        // exists; the other has none because nothing down there was ever labelled by anybody. Inventing a
        // second phrase for the halls would be the game telling the captain which of those they are standing
        // on, which is the one thing #677 may never do.
        if (IsUnlisted(bodyId, level) || IsFound(bodyId, level))
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

        // #677 · …and null again on the band nobody dug, for a harder version of the same reason. A livery is
        // paint a department put on its own corridor. Nobody painted these, and there is no department.
        if (level >= 0 || IsUnlisted(bodyId, level) || IsFound(bodyId, level))
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

    // ── #707 · THE AMENITIES — SOMEBODY WORKED SHIFTS DOWN HERE ──────────────────────────────────────────
    //
    // Owner, the morning after walking a clinic: "all the secret labs dont have any cantina / bar nor any
    // toilets. We should add those like to the most top most pressurized floor. The toilets should have like
    // bathroom level equipments and the high level important rooms would have their built in bathrooms and
    // be pressurized."
    //
    // It is the cheapest storytelling left in this building and the most damning. Everything down here says
    // BUDGET — a lined shaft, poured walls, a lift on somebody's account decades after the last invoice —
    // and none of it says PEOPLE. A canteen and a wall of cubicles say people, in the only register this
    // ground is allowed to use: what somebody was made to pay for.
    //
    // THE TWO TIERS, which is the owner's ruling of 2026-08-05 and is a design and not a decoration:
    //
    //   1. THE UPPER CANTEEN, on the topmost floor that holds pressure. "publicly accessible and just
    //      happens to be in the secret base" — vendors drink here, normal credits work, and security is
    //      loose BY DESIGN. Classy, dangerous, and tight-lipped: there are strangers in the room and
    //      everybody knows it.
    //   2. THE STAFF CANTEEN, on the deepest floor the building ADMITS to that still holds pressure.
    //      Machines, no bottles, and a room where every face is known — so the talk is careless in exactly
    //      the room a stranger cannot stand in.
    //
    // And the upper one is the answer to a question the mechanics have been shipping since #590 without one.
    // Owner, closing the loop: "setting access to off the books secret lab to partners all trying to keep
    // things off records would be bureaucratic nightmare of office interorganization bureaucracy so the
    // underground bar just is there with access from surface. It kind of provides cover-story as well."
    // Band 0 has never wanted a card. Now it has a reason: credentialing every deniable partner across
    // organisations that all deny existing was never going to happen, so the first floor is simply OPEN, and
    // the bar is why anybody believes the shed on the surface is what it pretends to be. Access control
    // starts where the drinks stop. Nothing is built for that here — the plate carries it in four words and
    // the room prose carries the rest, and neither of them ever explains a thing.
    //
    // WHY THE AMENITIES ONLY EXIST ON FLOORS THAT BREATHE, which is the one rule the whole section turns on:
    // a canteen, a cubicle and an en-suite are all PLUMBING, and plumbing is for people out of their suits.
    // The owner already ruled the general form of it — "any room that would house like office work would be
    // pressurized by that constraint ... any kind of fine motor skill stuff" — and eating, washing and
    // signing things are the same constraint. So there is NO SECOND PRESSURE MAP here and there never will
    // be: HoldsPressure is asked, and where it says no, nothing is plumbed. A private washroom breathing on
    // a floor the plate by the lift is calling NO ATMOSPHERE would be two instruments disagreeing about air,
    // which is the one thing §13.13 says is worse than saying nothing.

    /// <summary>#707 · What an amenity room is FOR. Three, and the difference between the first and the
    /// third is the whole of the owner's inverted-economics ruling rather than a change of furniture.</summary>
    public enum Comfort
    {
        /// <summary>The bar on the top floor that holds pressure — the one room in the building outsiders
        /// are in, and the reason nobody at band 0 is ever asked for a card.</summary>
        UpperCanteen,
        /// <summary>Cubicles, a basin run and a mirror. Bathroom-grade, per the owner.</summary>
        Washroom,
        /// <summary>Machines and close tables, on the deepest floor the directory admits to that still
        /// breathes. Staff only, and the paperwork says it is somewhere else entirely.</summary>
        StaffCanteen,
    }

    /// <summary>One amenity room, taken out of the rooms the floor had already built — same discipline as
    /// <see cref="Refuge"/>, and for the same reason: a room is already audited walkable from the lift,
    /// already has a door, and already sits down a rib.</summary>
    /// <param name="Use">Which of the three it is.</param>
    /// <param name="X">Centre, in the surface's own coordinates.</param>
    /// <param name="Y">Centre.</param>
    /// <param name="Plate">What is stencilled beside the door.</param>
    /// <param name="Fixture">What the thing in the middle of the room is called, at console size.</param>
    /// <param name="Tables">Round tops on the floor, drawn in the game's existing table idiom. Empty in a
    /// washroom, which is the one amenity nobody sits down in.</param>
    /// <param name="Hall">#751 · The hall this amenity IS, when it is one — a room that left the standard
    /// grammar. Null for the ordinary three-top canteen and for every washroom.</param>
    public readonly record struct Amenity(
        Comfort Use, double X, double Y, string Plate, string Fixture,
        IReadOnlyList<(double X, double Y)> Tables,
        Hall? Hall = null)
    {
        /// <summary>#725 · Is the captain standing in this room? <see cref="RefugeHolds"/>, because an
        /// amenity is one of the floor's own rooms taken over — the same poured box, with the same square
        /// corners — and a second containment box written here would be a room whose walls the sim and the
        /// picture disagreed about. One law, asked in one place, exactly as the refuge does it.
        ///
        /// <para>#751 · …unless it is a HALL, in which case the box is the hall's own — carved, published,
        /// and the very same rectangle the walls were laid on. A hall is thirty times the floor area of the
        /// module, so a refuge-sized containment box would have said "you are not in the canteen" from
        /// almost everywhere inside the canteen.</para></summary>
        public bool Contains(double x, double y) => Hall is { } hall
            ? hall.Contains(x, y)
            : RefugeHolds(X, Y, x, y);
    }

    // ── #751 · THE HALL RULE — WHEN AN AMENITY STOPS BEING A ROOM ─────────────────────────────────────
    //
    // Owner, 2026-08-06: "The Canteen is way too small… It needs to house like 80 customers… I am thinking
    // like Mos Eisley Space port size bar." And, an hour later, the second customer: "The canteen for only
    // staff can also be a lot bigger ... usually people eat lunch at same time so the whole staff using it
    // should about fit in."
    //
    // TWO ROOMS, ONE CARVE. They are opposite rooms in every way that matters — one heaves with strangers on
    // a day watch, the other has been empty since before the captain was born — and they are the SAME
    // geometry problem: a seat count that the standard 15 x 12 module cannot hold. So there is exactly one
    // hall carver (CarveHall), it takes a SEAT TARGET, and the two customers differ only in what number they
    // hand it. A second copy of this for the mess is the shape of bug the table at the top of this file is
    // a list of.
    //
    // WHY IT IS A RIB'S ROOM COLUMN AND NOT A BOX DROPPED ON THE FLOOR PLAN. #585's law is that the doorway
    // a room cuts and the gap its corridor leaves are ONE gap, computed once. A hall drawn as its own
    // rectangle would have had to cut its own doors — a second answer to a question RibFace already owns,
    // and the disease that sealed every room in the building for a day. Instead the hall simply IS the
    // ground the rib's room column stood on: its front wall is the rib's own face, already built, already
    // cut with a doorway at every room slot the corridor has. The hall has two doors and the corridor has
    // two gaps and they are the same two openings, because nothing ever made a second set.
    //
    // WHAT IT COSTS THE FLOOR, STATED. A hall eats the two room slots of one column, and the claim ledger
    // is told about the box before any other placer runs, so nothing is ever laid on top of it and no room
    // is silently dropped. The floor loses two rooms and gains a hall; that is the trade, it is stated
    // here, and TheCantinaHallTests measures it rather than trusting this paragraph.

    /// <summary>#751 · One enclosed side room off a hall — <b>CABINET · BY ARRANGEMENT</b>.
    ///
    /// <para>Owner: <i>"Definitely want to make the B1 bar be fancy ... and have cabinet-spaces for
    /// sensitive negotiations."</i> Six chairs, one door, and no line of sight to the counter — and that
    /// last clause is the whole mechanic, because #746's file-on-the-table is LOUD precisely because
    /// <i>"the counter has eyes"</i>. A room the counter cannot see is a room where it does not.</para>
    ///
    /// <para>Empty of people in v1. They are geometry plus a rule; #731's walkers will put somebody in
    /// one.</para></summary>
    /// <param name="Number">1-based, as the plate reads.</param>
    /// <param name="X">Centre.</param>
    /// <param name="Y">Centre.</param>
    /// <param name="HalfW">Half-width of the enclosed box.</param>
    /// <param name="HalfH">Half-height.</param>
    /// <param name="Table">The one round top in it, at its own centre.</param>
    public readonly record struct Cabinet(
        int Number, double X, double Y, double HalfW, double HalfH, (double X, double Y) Table)
    {
        /// <summary>Is the captain inside this cabinet? The box the walls were laid on, and nothing
        /// else.</summary>
        public bool Contains(double x, double y) =>
            Math.Abs(x - X) <= HalfW && Math.Abs(y - Y) <= HalfH;

        /// <summary>What is stencilled beside its door.</summary>
        public string Plate => CabinetPlate(Number);
    }

    /// <summary>#751 · A hall: the box, what it seats, and the cabinets off it.</summary>
    /// <param name="X0">Left edge, in the surface's own coordinates.</param>
    /// <param name="Y0">Bottom edge.</param>
    /// <param name="X1">Right edge.</param>
    /// <param name="Y1">Top edge.</param>
    /// <param name="SeatTarget">How many the hall was asked to seat — the owner's eighty for the cantina,
    /// <see cref="ImpliedComplement"/> for the mess. The bill of tops is derived from it and the guards
    /// measure the tops rather than reading this.</param>
    /// <param name="Cabinets">The enclosed side rooms. Empty on a mess — nobody negotiates anything in a
    /// room the shift stopped coming to.</param>
    /// <param name="BoardX">Where THE BOARD hangs — by the door, which is where a rota goes. Carried on the
    /// hall rather than computed from a fixed offset, because a hall's door is on whichever face the rib is
    /// on and a renderer guessing at that would be doing geometry about a room it does not own.</param>
    /// <param name="BoardY">The same.</param>
    /// <param name="PlateX">Where the room's own stencilled plate reads from — down the door wall, a
    /// quarter of the way along, clear of the board. Same reason as <paramref name="BoardX"/>.</param>
    /// <param name="PlateY">The same.</param>
    public readonly record struct Hall(
        double X0, double Y0, double X1, double Y1, int SeatTarget, IReadOnlyList<Cabinet> Cabinets,
        double BoardX = 0, double BoardY = 0, double PlateX = 0, double PlateY = 0)
    {
        /// <summary>Is the captain inside the hall? Cabinets are inside it, by construction.</summary>
        public bool Contains(double x, double y) => x >= X0 && x <= X1 && y >= Y0 && y <= Y1;

        /// <summary>Which cabinet holds this spot, or null for the hall floor itself.</summary>
        public Cabinet? CabinetAt(double x, double y)
        {
            foreach (Cabinet c in Cabinets)
            {
                if (c.Contains(x, y))
                {
                    return c;
                }
            }
            return null;
        }
    }

    /// <summary>#751 · What is stencilled beside a cabinet's door. Numbered, and it says how you get one:
    /// not off a menu.</summary>
    public static string CabinetPlate(int number) =>
        $"CABINET {number} · BY ARRANGEMENT · ASK AT THE COUNTER";

    /// <summary>#751 · How many a cabinet seats. SIX, on every one of them, and it is not a taste: the
    /// cabinet's own card and the field book both count the chairs out loud (<i>"six chairs, one door"</i>),
    /// and a cabinet that seated four would make a card lie about a room the captain is standing in.</summary>
    public const int CabinetSeats = 6;

    /// <summary>#751 · How many cabinets a cantina hall has. Three is a row of doors along a back wall —
    /// enough that the row reads as a FACILITY for the thing rather than as one odd room.</summary>
    public const int CabinetsPerHall = 3;

    /// <summary>
    /// #751 · HOW MANY PEOPLE THIS BUILDING IS FOR — the establishment, derived from the building's own
    /// stock and never typed.
    ///
    /// <para>Owner: <i>"usually people eat lunch at same time so the whole staff using it should about fit
    /// in."</i> That makes the mess's size a question about STAFFING, and this is the only place the game
    /// answers it — because staffing questions keep arriving (#618's guards, #717's rosters) and two answers
    /// to one of them is the table at the top of this file.</para>
    ///
    /// <para><b>The arithmetic, so it can be argued with.</b> A floor of this building IS a department —
    /// <see cref="DepartmentOf"/> gives exactly one plate per floor, and the lift panel has been printing it
    /// since #605. A department is a desk, a store, the plant that serves them and the hands that work all
    /// three: <see cref="HeadsPerDepartment"/>. So the complement is the departments the building admits to,
    /// times that. A twenty-storey clinic runs eighty people; a five-floor annex runs twenty, and its mess is
    /// smaller for an honest reason rather than because somebody typed a smaller number.</para>
    ///
    /// <para><b>LISTED floors only</b>, which is the line worth reading twice. <see cref="DepthOf"/> is what
    /// the directory admits to; the band nobody listed has no department, no plate and no livery (#592's
    /// whole tell is that absence) — so it has nobody on the books either. Whoever is down there is not on
    /// this payroll, and the catering budget says so without one word of prose.</para>
    /// </summary>
    public static int ImpliedComplement(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return -DepthOf(bodyId) * HeadsPerDepartment;
    }

    /// <summary>#751 · What one department is, in people: a desk, a store, the plant that serves them, and
    /// the hands. FLAGGED for the owner's tuning — it is the one number <see cref="ImpliedComplement"/>
    /// cannot derive from the building, because the building never wrote a payroll down.</summary>
    public const int HeadsPerDepartment = 4;

    /// <summary>
    /// #751 · HOW MANY THE B1 CANTINA HALL SEATS. The owner's own figure — <i>"It needs to house like 80
    /// customers"</i> — and it is a statement about the COVER rather than about the staff: eighty carriers
    /// eating on the company's coin, none of whom ask what the cage carries, is #707's lie rendered as a
    /// crowd. FLAGGED for tuning.
    /// </summary>
    public const int CantinaHallSeats = 80;

    /// <summary>#751 · Is this amenity carved as a hall? Both canteens are; a washroom never is (nobody
    /// eats a shift's lunch in the cubicles).</summary>
    public static bool IsHallClass(Comfort use) =>
        use is Comfort.UpperCanteen or Comfort.StaffCanteen;

    /// <summary>#751 · What a hall of this kind is asked to seat. The two customers of one carve, and the
    /// only line in the file where they differ.</summary>
    public static int HallSeatsFor(string bodyId, Comfort use)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return use == Comfort.StaffCanteen ? ImpliedComplement(bodyId) : CantinaHallSeats;
    }

    /// <summary>#751 · The three sizes of round top a caterer buys, smallest first. The owner's own three
    /// (#746, <i>"tables should seat 2/4/more, not all pairs"</i>), stated as a list so a guard can pin them
    /// without knowing the arithmetic that fills a hall with them.</summary>
    public static readonly IReadOnlyList<int> HallTopSizes = [2, 4, 6];

    /// <summary>
    /// #751 · THE BILL OF FURNITURE — how many of each size a hall seating <paramref name="seatTarget"/>
    /// buys, and in what order they are laid out.
    ///
    /// <para><b>Designed, not rolled, and that is load-bearing.</b> A seeded 2/4/6 per top has a standard
    /// deviation of seven seats over twenty tables, so a hall asked for eighty would ship anywhere between
    /// sixty-five and ninety-five and the guard would be measuring a die. A caterer does not roll dice: they
    /// buy a stock — three tops in ten seat two, four seat four, three seat six — and the floor plan decides
    /// where each one goes. The stock's average is exactly four, so the total is exactly the target and the
    /// mix is exactly the owner's three.</para>
    ///
    /// <para>The ORDER is seeded off the site (never off the watch — #746's law: a canteen does not
    /// re-furnish itself every shift), so two halls of the same size are laid out differently and neither
    /// reads as three zones of identical furniture.</para>
    /// </summary>
    /// <param name="bodyId">The site, which decides only the arrangement.</param>
    /// <param name="use">Which hall, so the cantina and the mess of one site differ.</param>
    /// <param name="seatTarget">How many the hall must seat. Rounded up to a whole top.</param>
    public static IReadOnlyList<int> HallSeatBill(string bodyId, Comfort use, int seatTarget)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        // Four is the stock's average, so the table count falls straight out of the target. Rounded UP: a
        // mess that seats the shift less one is a mess that does not seat the shift.
        int tables = Math.Max(HallTopSizes.Count, (seatTarget + 3) / 4);

        // Three in ten either side of the middle. Equal counts of twos and sixes is what makes the total
        // land exactly on four per top — every 6 is paid for by a 2.
        int wings = Math.Max(1, (int)Math.Round(tables * 0.3, MidpointRounding.AwayFromZero));
        while ((2 * wings) + 1 > tables)
        {
            wings--;    // a tiny hall still gets one of each, and never more tops than it has
        }

        var bill = new List<int>(tables);
        for (int i = 0; i < wings; i++)
        {
            bill.Add(2);
        }
        for (int i = 0; i < wings; i++)
        {
            bill.Add(6);
        }
        while (bill.Count < tables)
        {
            bill.Add(4);
        }

        // …and shuffled into place with a seeded swap walk, so the twos are not all by the door. Same
        // skip-forward discipline the rest of this ground uses: a deterministic permutation, never a
        // re-roll loop.
        for (int i = bill.Count - 1; i > 0; i--)
        {
            int j = DiceRule.Roll(
                DiceRule.Seed($"hive:hall:bill:{bodyId}:{(int)use}:{i}"), i + 1).Face - 1;
            (bill[i], bill[j]) = (bill[j], bill[i]);
        }

        return bill;
    }

    /// <summary>#707 · A private washroom cell hung off the back of a room that mattered.
    ///
    /// <para>Owner: <i>"the high level important rooms would have their built in bathrooms"</i> — and the
    /// design under it is that RANK IS READABLE IN PLUMBING. A captain who has learned the grammar reads
    /// "somebody with a name worked in here" off a door to a private cell, the same way sealed SECTOR doors
    /// read as scale. No card ever says it, and the cell itself carries no plate — a private washroom does
    /// not need a sign, and that absence is the last word of the tell.</para></summary>
    /// <param name="X">Centre of the cell.</param>
    /// <param name="Y">Centre of the cell.</param>
    /// <param name="Of">The plate of the room it hangs off — the reason it is there.</param>
    /// <param name="Open">Whether its parent room's own door opens. False behind a locked plate, where the
    /// cell is a thing you can only read from the corridor, exactly like the room it belongs to.</param>
    public readonly record struct EnSuite(double X, double Y, string Of, bool Open);

    /// <summary>How deep the en-suite cell hangs off the back of its room, in deck units.</summary>
    public const double EnSuiteDepth = 5.0;

    /// <summary>Half the cell's height. Comfortably taller than <see cref="DoorHalf"/>, so the doorway cut
    /// in the parent's back wall always lands inside the cell rather than beside it.</summary>
    public const double EnSuiteHalfHeight = 4.0;

    /// <summary>#707 · THE TOPMOST FLOOR THAT HOLDS PRESSURE — where the bar is.
    ///
    /// <para>Derived rather than typed. It is B1 on every building in the game and writing <c>-1</c> here
    /// would be a second answer to a question <see cref="HoldsPressure"/> already owns, sitting quietly
    /// correct until somebody moves a band. Two sources, one of which never hears about a change, is the
    /// table at the top of this repo's spec.</para></summary>
    public static int? TopPressurisedFloor(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        foreach (int level in FloorsOf(bodyId))
        {
            // #677 · IsPlumbed, not HoldsPressure. The bar goes on the topmost floor that breathes AND has a
            // wet stack; the halls breathe and have no plant of any kind, and on a shallow site they would
            // otherwise be eligible for a counter and three round tops.
            if (IsPlumbed(bodyId, level))
            {
                return level;
            }
        }
        return null;
    }

    /// <summary>#707 · WHERE THE STAFF CANTEEN IS: the deepest floor the building ADMITS to that still
    /// holds pressure, and null on a site too shallow to have a second one.
    ///
    /// <para><b>Deepest, and listed.</b> Two calls, each worth one line:</para>
    /// <list type="bullet">
    /// <item><b>Deepest</b> because the owner's inversion needs distance. The bar is the floor strangers
    /// walk into off the surface; the mess has to be as far from that as the building goes, so that a face
    /// nobody knows is a fact about the room rather than a matter of taste.</item>
    /// <item><b>Listed</b> (<see cref="DepthOf"/>, not <see cref="TrueDepthOf"/>) because catering is a
    /// thing a directory knows about. The band nobody listed has no department, no livery and no plate —
    /// #592's whole tell is the ABSENCE down there — and a canteen sign under it would be the building
    /// admitting to a floor in the one place it must not.</item>
    /// </list>
    ///
    /// <para>Null on a shallow site, and that is the honest answer rather than a gap: a three-floor annex
    /// has one canteen, because one canteen is the entire catering budget of a three-floor annex.</para></summary>
    public static int? StaffCanteenFloor(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        int? top = TopPressurisedFloor(bodyId);
        int? deepest = null;
        foreach (int level in FloorsOf(bodyId))
        {
            // #677 · …and never in the halls, which the directory could not list if it wanted to. IsPlumbed
            // already refuses them; the IsUnlisted clause stays because a floor can be listed-and-unplumbed
            // for the OTHER reason (§13.7's whole tell is the absence of a plate, not of a drain).
            if (IsPlumbed(bodyId, level) && !IsUnlisted(bodyId, level))
            {
                deepest = level;
            }
        }
        return deepest == top ? null : deepest;
    }

    /// <summary>
    /// #707 · WHICH DOOR PLATES BELONG TO SOMEBODY RATHER THAN TO SOMETHING — the rooms that get an
    /// en-suite.
    ///
    /// <para>The criterion, so it can be argued with instead of guessed at: <b>a plate is principal when it
    /// names an OFFICE or an AUTHORITY — somewhere a decision gets signed — rather than a process, a store,
    /// or a room where work is done TO somebody.</b> COLD STORE 2 is a place things are kept; SUBJECT PREP
    /// is a place things are done; QUOTA OFFICE is a place a person sits and rules on other people, and
    /// that person had a door of their own and did not queue for the cubicles on B1.</para>
    ///
    /// <para>And the RATIO is the rank difference, emergent and never stated: one plate in eight at a
    /// branch office, five in twelve at the head office. A captain who has crawled a Hive and then walks a
    /// head-office corridor sees private washrooms on half the doors, and nothing anywhere tells them what
    /// that means.</para>
    ///
    /// <para>Written as a list of plates taken verbatim out of <see cref="SignsFor"/> rather than as a
    /// keyword match on the string. A match on "OFFICE" would silently collect MANIFEST OFFICE and QUOTA
    /// OFFICE and then, the day somebody writes a plate reading POST OFFICE, that too — a rule that selects
    /// by accident is this repo's fifth bug class wearing a clever hat. Every entry here is proved to exist
    /// in some kind's vocabulary by <c>EveryPrincipalPlateIsAPlateThisBuildingActuallyHangs</c>.</para></summary>
    public static bool IsPrincipalRoom(string plate)
    {
        ArgumentNullException.ThrowIfNull(plate);
        foreach (string p in PrincipalPlates)
        {
            if (string.Equals(p, plate, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>The plates a person sat behind. See <see cref="IsPrincipalRoom"/> for the criterion.</summary>
    public static readonly string[] PrincipalPlates =
    [
        "CONTINUITY — AUTHORISED ONLY",                       // Laboratory: the one plate that grants
        "OCCUPATIONAL REVIEW", "QUOTA OFFICE",                 // ProcessingDepot: a panel, and a desk
        "AUDIT — NO ADMITTANCE",                               // RecordsAnnex
        "CONSENT FILES",                                       // BlackClinic: somebody countersigned those
        "MANIFEST OFFICE",                                     // TransitStation
        // #411 · The head office is mostly people who sign things, and it shows in the plumbing.
        "OFFICE OF THE REGISTRAR", "ESTABLISHMENT BOARD", "COMMITTEE ROOM 2", "APPROPRIATIONS",
        "DEPUTATIONS",
    ];

    /// <summary>What is stencilled beside an amenity's door, and what the fixture in the middle of it is
    /// called. Both from one place, so the sign on the wall and the console under the captain's hand can
    /// never come to describe different rooms.
    ///
    /// <para>Institutional throughout, and explaining nothing — with one deliberate exception of TONE. The
    /// branch office's bar plate is the only WARM sign in the building, because it is the only sign in the
    /// building that is a lie: a rest-house plate on a corridor of DESTRUCTION QUEUE and MORTUARY. NO PASS
    /// REQUIRED is a fact about band 0 that the lift panel has been shipping since #590, said out loud on a
    /// wall for the first time and still not explained.</para>
    ///
    /// <para>The head office answers the same law in its own vocabulary (#411): not a canteen and a
    /// washroom but a DINING ROOM and a CLOAKROOM, and its staff hall is for the ESTABLISHMENT — which is
    /// the word on its own B2 plate. Same rule, same grammar, a rank nobody has to be told about.</para></summary>
    public static (string Plate, string Fixture) AmenitySigns(string bodyId, Comfort use)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        bool hq = IsHeadOffice(bodyId);
        return use switch
        {
            Comfort.UpperCanteen => hq
                ? ("🍸 THE DINING ROOM · GUESTS & DEPUTATIONS", "🍸 THE SIDEBOARD")
                : ("🍸 CANTEEN 1 · CARRIERS & CONTRACTORS · NO PASS REQUIRED", "🍸 THE COUNTER"),
            Comfort.StaffCanteen => hq
                ? ("🍽 THE STAFF DINING HALL · ESTABLISHMENT ONLY", "🍽 THE SERVERY")
                : ("🍽 CANTEEN 2 · STAFF ONLY · PASS TO BE SHOWN", "🍽 THE MACHINES"),
            _ => hq
                ? ("🚻 CLOAKS & WASHROOMS", "🚻 THE BASIN RUN")
                : ("🚻 WASHROOMS · STAFF & VISITORS", "🚻 THE BASIN RUN"),
        };
    }

    /// <summary>What one of these rooms says when the captain stands in it. Evidence, and then it stops —
    /// every one of them is about what somebody was made to pay for and none of them is about what any of
    /// it was for.</summary>
    public static string AmenityLine(string bodyId, Comfort use)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        bool hq = IsHeadOffice(bodyId);
        return (use, hq) switch
        {
            (Comfort.UpperCanteen, false) =>
                "🍸 A long counter, a mirror behind it with the bottles gone, and the stools bolted down in " +
                "a row. Somebody kept this room WARM: the paint is a colour that appears nowhere else in " +
                "the building and the tables have been wiped. Whoever came down that shaft with a delivery " +
                "was fed and watered before they went back up, and nothing on this floor ever asked them " +
                "for a pass to do it.",

            (Comfort.UpperCanteen, true) =>
                "🍸 A dining room, and it is LAID. Cloth on the tables, glasses upended on a tray, covers " +
                "still on the sideboard. Places for eleven, set at the same spacing all the way down, and " +
                "the chair at the head pulled out by a hand's width. Somebody set this for a date, and the " +
                "date is not on anything in the room.",

            (Comfort.StaffCanteen, false) =>
                "🍽 Four machines and not a bottle of anything in the racks: soup, tea, and a wall of the " +
                "same wrapped biscuit. The tables are close together and the chairs face each other, which " +
                "is what a room for people who already know each other looks like.\n\n" +
                "📋 Pinned by the machines, a delivery manifest renewed every quarter without a break — and " +
                "the address on it is a SCHOOL, on another world entirely, costed per head for a roll of " +
                "two hundred and forty. Same account number every quarter. Signed for by a name with no " +
                "initial.",

            (Comfort.StaffCanteen, true) =>
                "🍽 Long tables, a servery with the shutters down, and trays stacked to the ceiling with " +
                "nothing between them. Every tray is clean. The rota on the wall is ruled to the end of a " +
                "year nobody has written in yet.\n\n" +
                "📋 And the standing order over the servery is the OTHER HALF of a manifest you have read " +
                "somewhere else: same account number, same quarterly quantity, addressed to a school a very " +
                "long way from here. This is the copy the office kept.",

            (_, true) =>
                "🚻 Cloakroom and washrooms. Numbered hooks, none of them used. A basin run in stone rather " +
                "than steel, and the taps run clear from the first second — somebody flushed this system " +
                "through, and not decades ago.",

            _ =>
                "🚻 Cubicles, a basin run, and a mirror with a tally scratched into the corner and mostly " +
                "rubbed out again. The taps still turn. The water comes through brown for four seconds and " +
                "then runs clear, which means a pump somewhere under your boots has never once stopped.",
        };
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
        IReadOnlyList<Rib> Ribs,
        IReadOnlyList<Refuge> Refuges,
        IReadOnlyList<Amenity> Amenities,
        IReadOnlyList<EnSuite> EnSuites);

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

        // #707 · A ROOM CARRIES ITS OWN PLATE THROUGH THE BUILD. It used to be a bare centre, because the
        // only thing that ever asked a room what it was, was the locked door hung on it — and a room that
        // opens has never had a sign drawn on it. That is still true on screen and it stopped being true in
        // the generator the moment rank became readable in plumbing: which rooms get an en-suite, and which
        // rooms are the wrong ones to turn into a canteen, are both questions about the plate. Carried in the
        // same list rather than in a second one kept in lockstep beside it, for the obvious reason.
        var rooms = new List<(double X, double Y, string Plate)>();
        var ensuites = new List<EnSuite>();

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

        // ── #677 · HOW BIG THE CHAMBERS ARE ON THIS FLOOR, decided ONCE and handed to both builders.
        //
        // The wall builder and the room builder must be given the same number for the same reason they are
        // already given the same centres function (#585): the doorway a room cuts and the gap its corridor
        // leaves are one gap, and two copies of a scale would open a door onto a wall on every floor of every
        // hall in the game.
        //
        // CAPPED BY THE GROUND, not by a guess. Two ribs' facing room columns must not meet, so the widest a
        // chamber may grow is half the closest rib spacing this field actually produced, less the corridor it
        // opens off. Below that the claim ledger would simply drop rooms — correct, and silent, which is the
        // shape of bug this file's spec opens with a table of.
        double roomScale = RoomScaleOn(bodyId, level);
        if (roomScale > 1.0 && ribList.Count > 1)
        {
            double closest = double.MaxValue;
            for (int i = 1; i < ribList.Count; i++)
            {
                closest = Math.Min(closest, ribList[i].X - ribList[i - 1].X);
            }
            double widest = (closest / 2.0) - CorridorHalf;
            roomScale = Math.Min(roomScale, Math.Max(1.0, widest / RoomWidthDu));
        }

        // ── #751 · THE HALL, FIRST, BECAUSE IT IS THE ONLY PLACER THAT CANNOT BE REFUSED ────────────────
        //
        // Carved BEFORE the rib loop and claimed immediately, so everything after it — rooms, en-suites,
        // refuges — sees the box and steps around it. The alternative was to carve it last and delete the
        // walls of whatever it had swallowed, which is the same thing said in a way that can go wrong.
        //
        // The floor pays for it in exactly two room slots (the column the hall stands on), and nothing else
        // on the floor is dropped: CarveHall clamps its own outer edge short of the next rib's chambers and
        // short of the lift alcove rather than trusting the ledger to notice afterwards.
        (int Rib, int Side)? hallSlot = HallSlotFor(bodyId, level, ribList, field, shaftX, shaftY, roomScale);
        HallSite? hallSite = null;
        if (hallSlot is { } slot)
        {
            (double hmouth, double hfar) = RibReach(field, shaftY, ribList[slot.Rib].Down);
            hallSite = CarveHall(
                walls, bodyId, level, ribList, slot.Rib, slot.Side, hmouth, hfar,
                shaftX, left, right, roomScale);
            if (hallSite is { } built)
            {
                claimed.Add((
                    built.Hall.X0 - 1.5, built.Hall.Y0 - 1.5,
                    built.Hall.X1 + 1.5, built.Hall.Y1 + 1.5));
            }
            else
            {
                hallSlot = null;   // the ground would not take one. The floor keeps its ordinary canteen.
            }
        }

        // ── THE RIBS. Cross corridors off the spine, with rooms flanking them.
        for (int i = 0; i < ribXs.Count; i++)
        {
            (double x, bool down) = ribXs[i];
            if (Math.Abs(x - shaftX) < 0.001)
            {
                continue;   // that entry is the lift alcove's mouth, not a corridor
            }
            (double mouth, double far) = RibReach(field, shaftY, down);

            // #585 · THE RIB'S OWN WALLS ARE CUT WHERE ROOMS OPEN OFF THEM. Owner: "a door is missing here
            // towards down", and his A* suggestion found it everywhere at once — 94 floors, not one room
            // reachable.
            //
            // The rooms cut a doorway in their OWN corridor-facing face, at x ± CorridorHalf. The rib's side
            // wall runs down that exact line. So every door in the building opened onto a wall: the plan drew
            // a facility and the collision field was a set of sealed boxes beside a sealed tube. Two walls on
            // one line, each correct on its own, and neither aware of the other — the same shape as every
            // expensive bug on this ground.
            RibFace(walls, x - CorridorHalf, mouth, far, bodyId, level, i, -1, down, roomScale);
            RibFace(walls, x + CorridorHalf, mouth, far, bodyId, level, i, +1, down, roomScale);

            // The rib's far end. #585: it is ALWAYS closed — by a sealed door with a distance on it, or by a
            // plain wall. It was 40/60 before, and a corridor that simply stops in mid-air is the same
            // topology bug one level down ("a door is missing here towards down").
            //
            // #677 · NEVER a sealed mouth in the halls. `⟶ SECTOR 7 · 2.4 km` is a plate somebody stencilled,
            // and a stencil is a department, a survey and a decision about where somebody's authority stops.
            // Down here the passage simply ends in the same material as everything else, and the captain gets
            // no number to reason with — which is worse, and is the point.
            if (!IsFound(bodyId, level) && Frac(bodyId, $"hive:{level}:rib-far:{i}") < 0.55)
            {
                double km = 0.8 + (Frac(bodyId, $"hive:{level}:rib-km:{i}") * 3.4);
                locked.Add(new(x - CorridorHalf, far, x + CorridorHalf, far,
                    SealedMouthSign(bodyId, i, km)));
            }
            walls.Add(new(x - CorridorHalf, far, x + CorridorHalf, far, true));

            // #751 · …and on the column the hall is standing on, no rooms at all. The rib's own face is
            // still built above (RibFace), with its doorway at every slot — those gaps ARE the hall's doors,
            // and they are the same gaps the corridor has because nothing ever cut a second set.
            AddRoomsAlong(
                walls, doorways, locked, rooms, ensuites, claimed, bodyId, level, i, x, mouth, far, down,
                roomScale, hallSlot is { } taken && taken.Rib == i ? taken.Side : 0);
        }

        // #608 · LAST, because a refuge is taken out of the rooms this floor actually managed to build. Any
        // earlier and it would be a designated INDEX rather than a designated ROOM — and the claim ledger
        // above drops a room whenever one would sit on something already standing, so an index chosen before
        // the loop is an index that sometimes names nothing. That is exactly the shape of the bug KeyRoomFor
        // was written to avoid, and a safety regulation may not be the second thing in this file to trip
        // over it.
        // #707 · …and the amenities, out of the same pool and BEFORE the refuge, so the two can never take
        // the same room. They never compete in practice — an amenity is only ever plumbed on a floor that
        // holds pressure and a refuge is only ever carved on one that does not — but the order says so
        // rather than leaving it to be rediscovered.
        List<Amenity> amenities = CarveAmenities(bodyId, level, rooms, walls, shaftX, shaftY, hallSite);
        List<Refuge> refuges = CarveRefuges(bodyId, level, rooms, shaftX, shaftY);

        var centres = new List<(double X, double Y)>(rooms.Count);
        foreach ((double rx, double ry, string _) in rooms)
        {
            centres.Add((rx, ry));
        }

        return new FloorPlan(level, NameOf(bodyId, level), HoldsPressure(bodyId, level),
            walls, doorways, locked, labels, centres, ribList, refuges, amenities, ensuites);
    }

    /// <summary>#585/#751 · How far a rib reaches off the spine, and where its mouth is. ONE function,
    /// because the wall builder, the room builder and now the hall carver all have to be given the same two
    /// numbers — and this was three copies of the same two lines the moment the hall arrived.</summary>
    private static (double Mouth, double Far) RibReach(in SurfaceLayout.Field field, double shaftY, bool down)
    {
        double margin = SurfaceLayout.EdgeMargin + 6;
        return down
            ? (shaftY - CorridorHalf, Math.Max(field.BottomY + margin, shaftY - 52))
            : (shaftY + CorridorHalf, Math.Min(field.LandingBandY - margin, shaftY + 52));
    }

    /// <summary>#751 · A carved hall, on its way to becoming an <see cref="Amenity"/>.</summary>
    /// <param name="Hall">The box and its cabinets, as published on the plan.</param>
    /// <param name="X">Where the fixture console stands — in front of the counter, on clear floor.</param>
    /// <param name="Y">The same.</param>
    /// <param name="Tops">The round tops on the hall floor. Cabinet tops are NOT in here: a cabinet's chairs
    /// are extra, and the hall's own seat law is measured on this list.</param>
    private readonly record struct HallSite(
        Hall Hall, double X, double Y, IReadOnlyList<(double X, double Y)> Tops);

    // ── #751 · THE HALL'S OWN MODULE ─────────────────────────────────────────────────────────────────
    //
    // Nothing below is a size somebody liked the look of. Every number is either the facility's own module
    // (RoomWidthDu / RoomHeightDu), the doorway both this room and its corridor are cut to (DoorHalf), or a
    // clearance stated as what it is for.

    /// <summary>#751 · How many round tops the facility's own room module holds — three, which is what
    /// <see cref="Fitting"/> has put in a canteen since #707. It is the constant that turns a room's floor
    /// area into a table PITCH without anybody typing one.</summary>
    public const int HallTopsPerModule = 3;

    /// <summary>#751 · How far apart a hall's round tops stand, at the density the game's own canteens
    /// already use: one top per (module area ÷ <see cref="HallTopsPerModule"/>), squared back into a
    /// spacing. A hall on tight ground packs closer than this; it never spreads wider.</summary>
    public static double HallTopPitchDu =>
        Math.Sqrt(RoomWidthDu * RoomHeightDu / HallTopsPerModule);

    /// <summary>#751 · The clear strip inside the hall's doors. Nothing is laid in it — a doorway a captain
    /// has to path around a table to use is #585's stranded room with better furniture.</summary>
    public const double HallDoorAisleDu = 4.0;

    /// <summary>#751 · How deep the cabinets run off the hall's outer wall.</summary>
    public const double HallCabinetDepthDu = 10.0;

    /// <summary>#751 · The band at the hall's far wall that THE COUNTER and its service side own — the one
    /// part of a bar the customer never stands in, closed off exactly the way it would be (#707).</summary>
    public const double HallCounterBandDu = 5.0;

    /// <summary>#751 · How much of a hall's own edge is left clear of furniture.</summary>
    public const double HallEdgePadDu = 2.0;

    /// <summary>
    /// #751 · WHICH COLUMN THE HALL STANDS ON — the same criterion #707 already uses for the canteen, asked
    /// one step earlier.
    ///
    /// <para>"Nearest the car, every time": a building puts its catering by the lift, and a bar you have to
    /// go looking for is not a bar anybody drank in on a shift. The old carve chose the nearest ROOM out of
    /// the rooms the floor had built; a hall has to be chosen before any room exists, so this asks the same
    /// question of the room SLOTS — the very positions <see cref="RoomCentresAlong"/> is about to place. Same
    /// answer, computed from the same arithmetic, one pass earlier.</para>
    /// </summary>
    private static (int Rib, int Side)? HallSlotFor(
        string bodyId, int level, List<Rib> ribs, in SurfaceLayout.Field field,
        double shaftX, double shaftY, double roomScale)
    {
        if (!IsHallFloor(bodyId, level) || ribs.Count == 0)
        {
            return null;
        }

        double roomW = RoomWidthDu * roomScale;
        (int Rib, int Side)? best = null;
        double bestD2 = double.MaxValue;

        for (int i = 0; i < ribs.Count; i++)
        {
            (double mouth, double far) = RibReach(field, shaftY, ribs[i].Down);
            List<double> ys = RoomCentresAlong(mouth, far, ribs[i].Down, roomScale);
            for (int side = -1; side <= 1; side += 2)
            {
                double cx = ribs[i].X + (side * (CorridorHalf + (roomW / 2)));
                foreach (double cy in ys)
                {
                    double dx = cx - shaftX, dy = cy - shaftY;
                    double d2 = (dx * dx) + (dy * dy);
                    if (d2 < bestD2)
                    {
                        (best, bestD2) = ((i, side), d2);
                    }
                }
            }
        }

        return best;
    }

    /// <summary>#751 · Does this floor get a hall at all? The two customers of the carve, asked in one
    /// place: the floor the bar is on, and the floor the mess is on.</summary>
    public static bool IsHallFloor(string bodyId, int level)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return TopPressurisedFloor(bodyId) == level || StaffCanteenFloor(bodyId) == level;
    }

    /// <summary>#751 · Which hall this floor's is. The mess wins where a site is shallow enough for the two
    /// to land on the same floor — but <see cref="StaffCanteenFloor"/> returns null in exactly that case, so
    /// this is belt and braces rather than a rule.</summary>
    private static Comfort HallUseOn(string bodyId, int level) =>
        TopPressurisedFloor(bodyId) == level ? Comfort.UpperCanteen : Comfort.StaffCanteen;

    /// <summary>
    /// #751 · THE HALL, CARVED. Returns null when the ground will not take one, in which case the floor
    /// keeps the ordinary three-top canteen and a guard says so out loud.
    ///
    /// <para>Laid out in the hall's own two axes so nothing here has to think about which way the rib
    /// points: <b>u</b> runs outward from the rib's face, <b>v</b> runs down the rib from the spine. The
    /// front wall (u = 0) and the near wall (v = 0) are not built at all — they are the rib's face and the
    /// spine's face, already standing, already cut. That is the whole of the one-gap law here: the hall
    /// cannot open a door in the wrong place because it never opens one.</para>
    /// </summary>
    private static HallSite? CarveHall(
        List<SurfaceLayout.Wall> walls, string bodyId, int level, List<Rib> ribs, int ribIndex, int side,
        double mouth, double far, double shaftX, double leftEnd, double rightEnd, double roomScale)
    {
        Comfort use = HallUseOn(bodyId, level);
        double ribX = ribs[ribIndex].X;
        double roomW = RoomWidthDu * roomScale;

        // ── HOW FAR OUT THE GROUND GOES. Clamped against the things that are already spoken for rather
        //    than against a guess: the next rib's chambers, the lift alcove where the hall shares a spine
        //    face with it, and the spine's own end cap.
        double limit = side > 0 ? rightEnd - HallEdgePadDu : leftEnd + HallEdgePadDu;
        foreach (Rib other in ribs)
        {
            if (side > 0 && other.X > ribX)
            {
                limit = Math.Min(limit, other.X - CorridorHalf - roomW - 1.5);
            }
            else if (side < 0 && other.X < ribX)
            {
                limit = Math.Max(limit, other.X + CorridorHalf + roomW + 1.5);
            }
        }
        if (!ribs[ribIndex].Down)
        {
            // The lift alcove hangs off the TOP face, so only a rib that runs UP can meet it. #585 was a
            // wall lying across a mouth; a hall laid over the alcove would be the same mistake with the
            // captain's own way home inside it.
            limit = side > 0
                ? Math.Min(limit, shaftX - ShaftHalf - 1.5)
                : Math.Max(limit, shaftX + ShaftHalf + 1.5);
        }

        double faceX = ribX + (side * CorridorHalf);
        double available = Math.Abs(limit - faceX);
        double length = Math.Abs(far - mouth);

        // ── WHAT THE HALL NEEDS. The tops come first: the seat target decides the bill, the bill decides
        //    how many tops, and the tops decide how deep the room has to be at the pitch the game's own
        //    canteens already use.
        // …and the cabinets are the cantina's alone. The mess is the room the shift stopped coming to; a
        // door for sensitive negotiations in it would be furnishing a joke nobody is in the room to make.
        bool cabs = use == Comfort.UpperCanteen;
        double cabBand = cabs ? HallCabinetDepthDu : 0.0;

        int tops = HallSeatBill(bodyId, use, HallSeatsFor(bodyId, use)).Count;
        double pitch = HallTopPitchDu;
        double vSpan = length - (2 * HallEdgePadDu) - HallCounterBandDu - HallEdgePadDu;
        int rowsAtPitch = Math.Max(1, (int)(vSpan / pitch));
        int colsNeeded = (tops + rowsAtPitch - 1) / rowsAtPitch;
        double wanted = HallDoorAisleDu + (colsNeeded * pitch) + HallEdgePadDu + cabBand;

        double width = Math.Min(available, wanted);

        // A hall that cannot hold its own doorways, its aisle and a table strip is not a hall. Saying so
        // and standing down is the honest answer; a guard asserts it never actually happens.
        double minWidth = HallDoorAisleDu + cabBand + HallEdgePadDu + (2 * DoorHalf);
        if (width < minWidth || vSpan < 4 * DoorHalf)
        {
            return null;
        }

        // ── FROM (u, v) TO THE FIELD'S OWN COORDINATES, in one place. ───────────────────────────────────
        bool down = ribs[ribIndex].Down;
        double X(double u) => faceX + (side * u);
        double Y(double v) => down ? mouth - v : mouth + v;

        double x0 = Math.Min(X(0), X(width)), x1 = Math.Max(X(0), X(width));
        double y0 = Math.Min(Y(0), Y(length)), y1 = Math.Max(Y(0), Y(length));

        // ── THE THREE WALLS THE HALL OWNS. (The fourth and fifth are the rib's face and the spine's.)
        walls.Add(new(X(width), Y(0), X(width), Y(length), true));        // the outer wall
        walls.Add(new(X(0), Y(length), X(width), Y(length), true));       // the far wall

        // ── THE COUNTER · a long bar wall along the far end, with the service side shut off behind it.
        double counterV = length - HallCounterBandDu;
        double counterU0 = HallDoorAisleDu;
        double counterU1 = width - cabBand - HallEdgePadDu;
        walls.Add(new(X(counterU0), Y(counterV), X(counterU1), Y(counterV), true));
        walls.Add(new(X(counterU0), Y(counterV), X(counterU0), Y(length), true));
        walls.Add(new(X(counterU1), Y(counterV), X(counterU1), Y(length), true));

        // ── THE CABINETS · a row of doors down the hall's outer wall.
        var cabinets = new List<Cabinet>(CabinetsPerHall);
        if (cabs)
        {
            double band = (length - (2 * HallEdgePadDu)) / CabinetsPerHall;
            double cabU0 = width - HallCabinetDepthDu;
            for (int c = 0; c < CabinetsPerHall; c++)
            {
                double vLo = HallEdgePadDu + (c * band) + 1.0;
                double vHi = HallEdgePadDu + ((c + 1) * band) - 1.0;
                double vMid = (vLo + vHi) / 2.0;

                // Three sides and a face with one gap in it — the same shape every room down here has, and
                // the gap is the same DoorHalf the corridor and the en-suites are cut to.
                walls.Add(new(X(cabU0), Y(vLo), X(width), Y(vLo), true));
                walls.Add(new(X(cabU0), Y(vHi), X(width), Y(vHi), true));
                walls.Add(new(X(cabU0), Y(vLo), X(cabU0), Y(vMid - DoorHalf), true));
                walls.Add(new(X(cabU0), Y(vMid + DoorHalf), X(cabU0), Y(vHi), true));

                double ccU = (cabU0 + width) / 2.0;
                cabinets.Add(new Cabinet(
                    c + 1, (X(cabU0) + X(width)) / 2.0, Y(vMid),
                    HallCabinetDepthDu / 2.0, (vHi - vLo) / 2.0,
                    (X(ccU), Y(vMid))));
            }
        }

        // ── THE TOPS · a grid in what is left, at whatever pitch the ground allows up to the module's own.
        double uLo = HallDoorAisleDu;
        double uHi = width - cabBand - HallEdgePadDu;
        double tvLo = HallEdgePadDu;
        double tvHi = counterV - (2 * HallEdgePadDu);
        double uw = Math.Max(pitch, uHi - uLo), vh = Math.Max(pitch, tvHi - tvLo);

        int cols = Math.Clamp((int)Math.Round(Math.Sqrt(tops * uw / vh), MidpointRounding.AwayFromZero), 1, tops);
        int rows = (tops + cols - 1) / cols;

        var laid = new List<(double X, double Y)>(tops);
        for (int t = 0; t < tops; t++)
        {
            double u = uLo + ((((t % cols) + 0.5) / cols) * uw);
            double v = tvLo + ((((t / cols) + 0.5) / rows) * vh);
            laid.Add((X(u), Y(v)));
        }

        // ── THE PILLARS · poured, load-bearing, and honest: this rock is heavy. Placed on the grid's own
        //    seams so they break sightlines without ever standing on a chair.
        double ph = Math.Min(0.9, Math.Min(uw / cols, vh / rows) / 5.0);
        for (int p = 1; p < Math.Min(cols, 4); p++)
        {
            double u = uLo + ((p / (double)cols) * uw);
            double v = tvLo + (((p % 2 == 0 ? 1 : 2) / 3.0) * vh);
            walls.Add(new(X(u - ph), Y(v - ph), X(u + ph), Y(v - ph), true));
            walls.Add(new(X(u - ph), Y(v + ph), X(u + ph), Y(v + ph), true));
            walls.Add(new(X(u - ph), Y(v - ph), X(u - ph), Y(v + ph), true));
            walls.Add(new(X(u + ph), Y(v - ph), X(u + ph), Y(v + ph), true));
        }

        // The board hangs half-way down the door wall and the plate reads a quarter of the way along it, so
        // neither crowds the other and both are things you meet on the way in rather than across the room.
        return new HallSite(
            new Hall(
                x0, y0, x1, y1, HallSeatsFor(bodyId, use), cabinets,
                X(HallDoorAisleDu / 2.0), Y(length / 2.0),
                X(HallDoorAisleDu / 2.0), Y(length * 0.25)),
            X((uLo + uHi) / 2.0), Y(counterV - HallEdgePadDu),
            laid);
    }

    /// <summary>#707 · Hang a washroom cell off the back of a room, if the room is one that earned one and
    /// the ground behind it is free. Returns true when it built the cell AND the parent's back wall (with a
    /// doorway cut in it), so the caller knows not to build that wall itself.</summary>
    private static bool AddEnSuite(
        List<SurfaceLayout.Wall> walls, List<EnSuite> ensuites,
        List<(double X0, double Y0, double X1, double Y1)> claimed,
        string bodyId, int level, string plate, double backX, double cy, int side, bool open)
    {
        // The one pressure source, asked through the one plumbing question: a cell is for people out of their
        // suits AND for a building that had a wet stack to hang it off. The halls breathe and have neither
        // (#677) — a pan in a gallery would be the most explaining object in the game.
        if (!IsPlumbed(bodyId, level) || !IsPrincipalRoom(plate))
        {
            return false;
        }

        double outward = side < 0 ? -EnSuiteDepth : EnSuiteDepth;
        double farX = backX + outward;
        double cx0 = Math.Min(backX, farX), cx1 = Math.Max(backX, farX);
        double cy0 = cy - EnSuiteHalfHeight, cy1 = cy + EnSuiteHalfHeight;

        // #585 · Checked against the ledger BEFORE it is built, not only added to it afterwards. The room
        // columns either side of a rib are laid in x order and this cell reaches BACK toward a neighbour
        // that already exists, so a placer that only claims forward is a placer that can bury one.
        foreach ((double ax0, double ay0, double ax1, double ay1) in claimed)
        {
            if (cx0 < ax1 && cx1 > ax0 && cy0 < ay1 && cy1 > ay0)
            {
                return false;   // somebody is already standing on it. The room keeps its solid back wall.
            }
        }
        claimed.Add((cx0 - 1.5, cy0 - 1.5, cx1 + 1.5, cy1 + 1.5));

        // The parent's back wall, in two segments with the cell's doorway between them — the whole tell, in
        // one gap in one wall. The room is 12 du deep, so the segments run from its own corners.
        walls.Add(new(backX, cy - 6.0, backX, cy - DoorHalf, true));
        walls.Add(new(backX, cy + DoorHalf, backX, cy + 6.0, true));

        // …and the cell itself: two returns and an end wall.
        walls.Add(new(backX, cy0, farX, cy0, true));
        walls.Add(new(backX, cy1, farX, cy1, true));
        walls.Add(new(farX, cy0, farX, cy1, true));

        // The fixture. One pan against the end wall, which is all a private cell has room for and all it
        // needs to read as one on a plan.
        double basinX = backX + (outward * 0.76);
        walls.Add(new(basinX, cy + 1.0, basinX, cy + 3.2, true));

        ensuites.Add(new EnSuite(backX + (outward / 2.0), cy, plate, open));
        return true;
    }

    /// <summary>#707 · The amenity rooms, taken out of the rooms the floor had already built — the same
    /// discipline as <see cref="CarveRefuges"/>, and for the same three reasons: a room is already audited
    /// walkable from the lift, already has a door the captain can find, and already sits down a rib.
    ///
    /// <para><b>Nearest the car, which is the exact opposite of the refuge law and is right for the same
    /// reason.</b> A refuge earns its existence by being a detour (#608). A canteen earns its by being the
    /// first door off the lift: it is the room a haulier with a pallet and forty minutes actually used, and
    /// a bar you have to go looking for is not a bar anybody drank in on a shift. No dice — a building puts
    /// its catering by the car, every time, and a captain gets to learn that.</para>
    ///
    /// <para><b>And the washroom is beside the canteen</b>, for the reason a plumber would give: a building
    /// runs ONE wet stack and hangs everything that needs a drain off it. That is the same sentence as the
    /// en-suites only appearing on floors that breathe, which is why §13's amenity law is one rule and not
    /// three.</para></summary>
    private static List<Amenity> CarveAmenities(
        string bodyId, int level, List<(double X, double Y, string Plate)> rooms,
        List<SurfaceLayout.Wall> walls, double shaftX, double shaftY, HallSite? hall)
    {
        var built = new List<Amenity>();
        bool top = TopPressurisedFloor(bodyId) == level;
        bool mess = StaffCanteenFloor(bodyId) == level;
        if (!top && !mess)
        {
            return built;
        }

        // #751 · THE HALL IS THE CANTEEN, where one was carved. It is not taken out of the room pool at all
        // — it is the ground the pool's own column stood on, claimed before any room was laid — so the only
        // thing left for this method to do on a hall floor is to give it its plate and (on the top floor)
        // find the washroom a wet stack away from it.
        Comfort hallUse = top ? Comfort.UpperCanteen : Comfort.StaffCanteen;
        if (hall is { } site)
        {
            (string hallPlate, string hallFixture) = AmenitySigns(bodyId, hallUse);
            built.Add(new Amenity(
                hallUse, site.X, site.Y, hallPlate, hallFixture, site.Tops, site.Hall));

            if (!top || rooms.Count == 0)
            {
                return built;
            }

            var near = new List<int>();
            for (int i = 0; i < rooms.Count; i++)
            {
                if (!IsPrincipalRoom(rooms[i].Plate) && !ReservedRoom(bodyId, level, i))
                {
                    near.Add(i);
                }
            }
            if (near.Count == 0)
            {
                return built;
            }

            Nearest(near, rooms, site.X, site.Y);
            int washroom = near[0];
            (double wx, double wy, string _) = rooms[washroom];
            rooms.RemoveAt(washroom);
            (string wplate, string wfixture) = AmenitySigns(bodyId, Comfort.Washroom);
            built.Add(new Amenity(
                Comfort.Washroom, wx, wy, wplate, wfixture, Fitting(walls, Comfort.Washroom, wx, wy)));
            built.Sort((a, b) => a.Use.CompareTo(b.Use));
            return built;
        }

        if (rooms.Count == 0)
        {
            return built;
        }

        // #592/#614/#411 · The designated rooms, which may never be taken. The same reservation
        // CarveRefuges makes and for the same reason: a designated INDEX read off a list that a second
        // placer shortens is a feature silently dead on some worlds forever, with every test still green.
        // Candidates, nearest the car first. A principal room is never one: it already has its own
        // washroom, and a director's office is not where a building puts the vending machines.
        var pool = new List<int>();
        var anywhere = new List<int>();
        for (int i = 0; i < rooms.Count; i++)
        {
            if (ReservedRoom(bodyId, level, i))
            {
                continue;
            }
            anywhere.Add(i);
            if (!IsPrincipalRoom(rooms[i].Plate))
            {
                pool.Add(i);
            }
        }

        int need = top ? 2 : 1;
        List<int> from = pool.Count >= need ? pool : anywhere;
        if (from.Count < need)
        {
            return built;   // nothing left to give. The guards say this has never happened.
        }
        Nearest(from, rooms, shaftX, shaftY);

        // The canteen takes the nearest room to the car; the washroom takes the room nearest THE CANTEEN,
        // which is the wet stack rather than a second walk from the lift.
        int first = from[0];
        var taken = new List<(int Index, Comfort Use)>
        {
            (first, top ? Comfort.UpperCanteen : Comfort.StaffCanteen),
        };
        if (top)
        {
            from.RemoveAt(0);
            Nearest(from, rooms, rooms[first].X, rooms[first].Y);
            taken.Add((from[0], Comfort.Washroom));
        }

        // Highest index first, so removing one never renumbers another out from under us.
        taken.Sort((a, b) => b.Index.CompareTo(a.Index));
        foreach ((int index, Comfort use) in taken)
        {
            (double rx, double ry, string _) = rooms[index];
            rooms.RemoveAt(index);
            (string plate, string fixtureName) = AmenitySigns(bodyId, use);
            built.Add(new Amenity(use, rx, ry, plate, fixtureName, Fitting(walls, use, rx, ry)));
        }

        // Back into the order the plates read in, so a floor's amenity list is canteen-then-washroom rather
        // than an artefact of the order they happened to be removed in.
        built.Sort((a, b) => a.Use.CompareTo(b.Use));
        return built;
    }

    /// <summary>#592/#614/#411 · Is this room index DESIGNATED — reserved for a find that must exist? The
    /// same reservation <see cref="CarveRefuges"/> makes and for the same reason: a designated INDEX read off
    /// a list that a second placer shortens is a feature silently dead on some worlds forever, with every
    /// test still green. Lifted out of <see cref="CarveAmenities"/> when #751 gave it a second caller.</summary>
    private static bool ReservedRoom(string bodyId, int level, int index)
    {
        foreach ((int Level, int RoomIndex)? designated in
            new (int, int)?[]
            {
                KeyRoomFor(bodyId), RelicRoomFor(bodyId), StandingOrderRoomFor(bodyId),
                FoundKeyRoomFor(bodyId),   // #677 · the way down to the halls is a designation too
            })
        {
            if (designated is { } d && d.Level == level && d.RoomIndex == index)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>Sort room indices by how far they are from a point, ties broken by index — <c>List.Sort</c>
    /// is not stable, and a floor being the same floor every visit is law down here.</summary>
    private static void Nearest(
        List<int> which, List<(double X, double Y, string Plate)> rooms, double px, double py)
    {
        which.Sort((a, b) =>
        {
            double da = Dist2(rooms[a], px, py), db = Dist2(rooms[b], px, py);
            int by = da.CompareTo(db);
            return by != 0 ? by : a.CompareTo(b);
        });

        static double Dist2((double X, double Y, string Plate) room, double px, double py)
        {
            double dx = room.X - px, dy = room.Y - py;
            return (dx * dx) + (dy * dy);
        }
    }

    /// <summary>#707 · WHAT IS BOLTED DOWN IN ONE OF THESE ROOMS — a counter, a run of cubicles, a bank of
    /// machines — returning the round tops that go on the floor with it.
    ///
    /// <para>The fixtures are WALLS, in the same list as everything else, so they collide: a bar you can
    /// walk through is a bar drawn ON a floor rather than one IN a room, and this ground has paid for the
    /// sim doing one thing while the picture said another three times in one afternoon. Every fixture is
    /// laid against the room's own back half, so the doorway, the middle of the room and the console in it
    /// are all left clear — a fixture that seals a room is #585's stranded room with better furniture.</para>
    ///
    /// <para>The tables are NOT walls. Round tops are drawn and never collided with anywhere in this game
    /// (the ship's cantina, a haven bar), and a captain barking their shins on a table on a floor with a
    /// tank running would be a cruelty nobody asked for.</para></summary>
    private static IReadOnlyList<(double X, double Y)> Fitting(
        List<SurfaceLayout.Wall> walls, Comfort use, double cx, double cy)
    {
        switch (use)
        {
            case Comfort.UpperCanteen:
                // The counter, and the service side behind it — the one part of any bar the customer never
                // stands in, closed off exactly the way it would be.
                walls.Add(new(cx - 5.0, cy + 3.6, cx + 5.0, cy + 3.6, true));
                walls.Add(new(cx - 5.0, cy + 3.6, cx - 5.0, cy + 6.0, true));
                walls.Add(new(cx + 5.0, cy + 3.6, cx + 5.0, cy + 6.0, true));
                return [(cx - 4.5, cy - 2.5), (cx, cy - 4.2), (cx + 4.5, cy - 2.5)];

            case Comfort.StaffCanteen:
                // Four machines against the back wall and nothing to lean on. The owner's whole point about
                // this room is what is NOT in it.
                foreach (double m in new[] { cx - 5.4, cx - 1.8, cx + 1.8, cx + 5.4 })
                {
                    walls.Add(new(m - 1.4, cy + 4.4, m + 1.4, cy + 4.4, true));
                    walls.Add(new(m - 1.4, cy + 4.4, m - 1.4, cy + 6.0, true));
                    walls.Add(new(m + 1.4, cy + 4.4, m + 1.4, cy + 6.0, true));
                }
                // Tables close together and facing each other, which is the other half of that design.
                return [(cx - 3.6, cy - 2.4), (cx, cy - 2.4), (cx + 3.6, cy - 2.4)];

            default:
                // Bathroom-grade, per the owner: a basin run along the back and three cubicle dividers. The
                // stalls have no fronts on the plan — a deck plan draws partitions, and a captain made to
                // path around three cubicle doors to reach a mirror is being charged for a joke.
                walls.Add(new(cx - 5.5, cy + 4.2, cx + 5.5, cy + 4.2, true));
                foreach (double d in new[] { cx - 4.0, cx, cx + 4.0 })
                {
                    walls.Add(new(d, cy - 6.0, d, cy - 2.6, true));
                }
                return [];
        }
    }

    /// <summary>Rooms down both sides of a rib. About half are locked — the owner's illusion of scale — and a
    /// locked one still gets its sign, because a door that says what is behind it and will not open is doing
    /// far more work than a blank one.</summary>
    /// <summary>#585 · Where the rooms sit along a rib. ONE function, called by the wall builder and by the
    /// room builder, because the doorway a room cuts and the gap its corridor leaves must be the same gap.
    /// They were computed twice and agreed about nothing.</summary>
    private static List<double> RoomCentresAlong(double mouth, double far, bool down, double roomScale)
    {
        double roomH = RoomHeightDu * roomScale;
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
        string bodyId, int level, int rib, int side, bool down, double roomScale)
    {
        var doors = RoomCentresAlong(mouth, far, down, roomScale);
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

    /// <summary>#585/#677 · THE ROOM MODULE — how wide and how deep one room off a rib is, at the scale a
    /// facility builds at.
    ///
    /// <para>These were two <c>const</c>s inside <see cref="AddRoomsAlong"/> and a third inside
    /// <see cref="RoomCentresAlong"/>, which was exactly as safe as it sounds: the door a room cuts and the
    /// gap its corridor leaves are the SAME gap (#585's lesson), and the moment one floor in the game wanted
    /// bigger chambers there would have been two places to grow and one of them would have been missed. One
    /// module, published, and everything that scales it scales it once.</para></summary>
    public const double RoomWidthDu = 15.0;

    /// <summary>Room depth along its rib. See <see cref="RoomWidthDu"/>.</summary>
    public const double RoomHeightDu = 12.0;

    /// <summary>#677 · HOW MUCH BIGGER A GALLERY GETS PER FLOOR DOWN, and it is the one number the halls'
    /// geometry is allowed to state.
    ///
    /// <para>The whole game has taught the opposite: deeper is tighter, because a facility's cost per cubic
    /// metre goes up with every metre of overburden and the people paying for it knew that. Down here it
    /// inverts, and the renderer says so without one word of prose — <b>room scale increasing with depth</b>,
    /// which on a top-down plan is the only sentence a plan can speak. The four floors run 1.00, 1.10, 1.21,
    /// 1.33 of the module above, so the deepest gallery has getting on for twice the floor area of the first
    /// and about half as many chambers on it.</para>
    ///
    /// <para><b>Derived, never typed, and capped by the ground it is standing on.</b> Nothing here writes a
    /// room's dimensions: they are <see cref="RoomWidthDu"/>/<see cref="RoomHeightDu"/> — the facility's own
    /// module, the same one every floor above uses — taken to the power of how far into the band you are.
    /// And <see cref="Build"/> clamps the ratio against the actual rib spacing of the actual field, so the
    /// growth stops where two facing chambers would meet rather than at a number somebody guessed.</para></summary>
    public const double FoundGrowthPerFloor = 1.10;

    /// <summary>#677 · How much bigger than the module this floor's chambers are. 1.0 everywhere the building
    /// built itself; compounding with depth in the halls.</summary>
    public static double RoomScaleOn(string bodyId, int level)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        if (!IsFound(bodyId, level))
        {
            return 1.0;
        }
        return Math.Pow(FoundGrowthPerFloor, BandTop(FoundBandOf(bodyId)) - level);
    }

    private static void AddRoomsAlong(
        List<SurfaceLayout.Wall> walls, List<SurfaceLayout.Doorway> doorways, List<LockedDoor> locked,
        List<(double X, double Y, string Plate)> rooms, List<EnSuite> ensuites,
        List<(double X0, double Y0, double X1, double Y1)> claimed,
        string bodyId, int level, int rib, double x, double mouth, double far, bool down, double roomScale,
        int hallSide = 0)
    {
        double roomW = RoomWidthDu * roomScale, roomH = RoomHeightDu * roomScale;

        // #677 · Down here the rooms are the only thing that has to be different, and everything else about
        // them falls out of that: a gallery is not a room with a plate on it, so it has no plate, no lock and
        // no sign. A door that says CONSENT FILES on a floor nobody built would be the loudest lie in the
        // game — it would name a purpose, and a purpose implies somebody who had one.
        bool found = IsFound(bodyId, level);
        List<double> centres = RoomCentresAlong(mouth, far, down, roomScale);

        for (int i = 0; i < centres.Count; i++)
        {
            double cy = centres[i];

            for (int side = -1; side <= 1; side += 2)
            {
                // #751 · The hall is standing on this column. Nothing is built here — no chamber walls, no
                // plate, no lock — and the rib's face above keeps its doorway at this very slot, which is
                // how the hall comes to have a door without ever cutting one.
                if (side == hallSide)
                {
                    // …but the doorway is PUBLISHED, in the same list as every other door down here, so the
                    // hall's entrances are drawn as the imported leaves they are and an audit can find them
                    // without knowing anything about halls.
                    double hallFaceX = x + (side * CorridorHalf);
                    if (!found)
                    {
                        doorways.Add(new SurfaceLayout.Doorway(
                            hallFaceX, cy - DoorHalf, hallFaceX, cy + DoorHalf));
                    }
                    continue;
                }

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
                string plate = found ? "" : SignFor(bodyId, level, tag);
                bool shut = !found && Frac(bodyId, tag + ":locked") < 0.5;

                // Three walls and a corridor-facing face with a gap in it.
                walls.Add(new(x1, y1, x2, y1, true));
                walls.Add(new(x1, y2, x2, y2, true));

                // #707 · …and the back wall, which is the one that says whether anybody important sat here.
                //
                // ASKED BEFORE THIS ROOM CLAIMS ITS OWN GROUND, which is the whole of the ordering: the
                // claim boxes are inflated by 1.5 du on every side, so a cell hung on this room's own back
                // wall sits inside its PARENT'S keep-out and every single en-suite in the game refused
                // itself. (Watched happen: 202 floors, "1 principal room(s) and 0 en-suite(s)", with the
                // geometry perfectly correct.) The cell is checked against everything already standing and
                // the room is claimed immediately after, so nothing later can be laid on either of them.
                double backX = side < 0 ? x1 : x2;
                bool cell = AddEnSuite(
                    walls, ensuites, claimed, bodyId, level, plate, backX, cy, side, open: !shut);
                claimed.Add((x1 - 1.5, y1 - 1.5, x2 + 1.5, y2 + 1.5));
                if (!cell)
                {
                    walls.Add(new(backX, y1, backX, y2, true));
                }

                double faceX = side < 0 ? x2 : x1;
                walls.Add(new(faceX, y1, faceX, cy - DoorHalf, true));
                walls.Add(new(faceX, cy + DoorHalf, faceX, y2, true));

                if (shut)
                {
                    locked.Add(new(faceX, cy - DoorHalf, faceX, cy + DoorHalf, plate));
                }
                else
                {
                    // #677 · A GALLERY HAS NO DOOR IN IT, only a way through. Every doorway in this building
                    // is drawn as an IMPORTED leaf — the violet that means "this was flown here", which is
                    // the whole of #592's material language — so hanging one in a hall would say, in the one
                    // channel the game reserves for it, that somebody shipped it in and fitted it. The wall
                    // simply stops, and the gap is the gap the wall builder already left.
                    if (!found)
                    {
                        doorways.Add(new SurfaceLayout.Doorway(faceX, cy - DoorHalf, faceX, cy + DoorHalf));
                    }
                    rooms.Add((cx, cy, plate));
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
    /// <remarks>#677 · <see cref="UnlistedBottomOf"/> and not <c>TrueDepthOf</c>. The thing on the pallet
    /// belongs to the OPERATION — somebody crated it, somebody left the lights on over it — so it sits on the
    /// deepest floor the operation dug. The day something deeper turned out not to have been dug by anybody,
    /// a <c>TrueDepthOf</c> here would have moved the one designated relic in the game two bands down into a
    /// gallery with no lights and no pallets in it, and every test would still have passed.</remarks>
    public static (int Level, int RoomIndex)? RelicRoomFor(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return HasUnlistedBand(bodyId) ? (UnlistedBottomOf(bodyId), 0) : null;
    }

    /// <summary>#677 · WHERE THE WAY DOWN TO THE HALLS IS, designated for exactly the reason
    /// <see cref="KeyRoomFor"/> is: a Key is one face in nine, and a seeded band that happened to roll none
    /// would leave a site's halls unreachable not for that visit but forever, with nothing on screen ever
    /// saying so and every test still green.
    ///
    /// <para>It is room 0 of the band nobody listed's own SHAFT HEAD — the floor a captain steps out onto
    /// when the plate finally names a different building (#694). Not its bottom floor, which is already
    /// spoken for by <see cref="RelicRoomFor"/>, and not the listed bottom, which is already
    /// <see cref="KeyRoomFor"/>. Three designations, three floors, no collision.</para>
    ///
    /// <para>Every other Key in that band mints the same card anyway (<see cref="CardInRoom"/> asks
    /// <see cref="NextShaftBelow"/>, which steps over the band of nothing) — this one only guarantees that at
    /// least one exists. The paper telling the truth about a building that is not, one rung further.</para></summary>
    public static (int Level, int RoomIndex)? FoundKeyRoomFor(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return HasFoundBand(bodyId) ? (BandTop(UnlistedBandOf(bodyId)), 0) : null;
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

        // #677 · And the one room that holds the way down to the halls.
        if (FoundKeyRoomFor(bodyId) is { } hall && level == hall.Level && roomIndex == hall.RoomIndex)
        {
            return Haul.Key;
        }

        // ── #677 · AND THE HALLS, WHERE ALMOST NOTHING IS IN ALMOST EVERY ROOM ────────────────────────────
        //
        // The emptiness is load-bearing everywhere on this ground (§10.3) and down here it is the whole
        // sensation: a place kept ready, and nobody in it. So the roll is not a weighting of the facility's
        // roll — it is a different roll with almost nothing in it.
        //
        // What is deliberately ABSENT and why, because each absence is a canon law rather than a balance
        // call: no EQUIPMENT (nobody procured anything down here, and a crate would name a supplier); no
        // RECORDS and no DIRT (both are paperwork, and paperwork is an institution — a file on somebody in a
        // hall would say who kept it); no KEY (the entry card is the last card, and a second one would make
        // the halls a building with a directory). What is left is what a surveyor could actually carry out
        // of a place like this, which is a MEASUREMENT.
        if (IsFound(bodyId, level))
        {
            return DiceRule.Roll(DiceRule.Seed($"hive:hall-haul:{bodyId}:{level}:{roomIndex}"),
                    FoundRecordOneInN).Face == 1
                ? Haul.Relic
                : Haul.Nothing;
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
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        // ── #677 · THE HALLS HAVE THEIR OWN TWO ANSWERS AND NO OTHERS ────────────────────────────────────
        //
        // Taken before the switch below rather than as two more arms inside it, because the thing that must
        // never happen is a haul reaching the facility's DEFAULT arm down here: "stripped to the fittings…
        // whoever cleared this room did it carefully and did it in a hurry" is a sentence about STAFF, and
        // there was no staff. A default arm is how that sentence would arrive — silently, on the day
        // somebody adds a Haul value and does not think about a floor nobody built.
        if (IsFound(bodyId, level))
        {
            // A record find says nothing about its room. Everything there is to say is the pocket line and
            // the card, both authored; a sentence invented here to fill the gap would be the one thing this
            // feature forbids.
            return haul == Haul.Relic ? "" : FoundEmptyRoomLine;
        }

        return haul switch
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
    }

    /// <summary>#677/#603 · What the CASEBOOK keeps out of one room, or null where the pulse line is already
    /// the whole of the record.
    ///
    /// <para>Only the halls answer, and only for a record: looking is free and knowledge is one-shot, so the
    /// book keeps what the captain now KNOWS about a wall rather than the sentence about putting a rubbing in
    /// a pocket. Every other room in the game files its own line and always has.</para></summary>
    public static string? CasebookGistOf(Haul haul, string bodyId, int level)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return haul == Haul.Relic && IsFound(bodyId, level) ? FoundRecordGist : null;
    }

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
            || (HasUnlistedBand(bodyId) && band == UnlistedBandOf(bodyId))
            // #677 · …and the halls, which are not a band of this building at all. The band BETWEEN them is
            // deliberately not here: nothing was dug in it, so nothing may ever authorise it or offer it.
            || (HasFoundBand(bodyId) && band == FoundBandOf(bodyId)));

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

        // #677 · The next shaft that EXISTS, not the next band number. Under the band nobody listed there is
        // a band with nothing in it, and a card for a hole nobody dug is exactly the lie #613 was filed
        // about — a countersigned authority for a floor the building cannot open onto.
        return NextShaftBelow(bodyId, level) is { } next ? new AuthorityCard(bodyId, next) : null;
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
    public static string CardTitle(AuthorityCard card) =>
        $"🎫 SHAFT {card.Band + 1} · {OfficeOf(card).Letterhead} · " +
        $"{BodyNames.Designation(card.BodyId)} SITE";

    /// <summary>#695 · ONE OFFICE, ONE FACE. The office that issued a card is the letterhead printed across
    /// the top of it AND the photograph laminated into it, and those are the same office because they are
    /// the same record — not because two pieces of arithmetic were written to agree.
    ///
    /// <para>Owner, wallet in hand: <i>"I have 3 ID cards but they all have the same gen AI image."</i> The
    /// title had rolled one of five offices since #679; the picture was a single constant. Pairing them by
    /// re-deriving the roll at the art seam would have been the house's most expensive bug class — two
    /// sources for one fact — waiting for somebody to touch one seed string and not the other.</para></summary>
    /// <param name="Letterhead">What the office stamps across the top of the card.</param>
    /// <param name="ArtUrl">The face laminated into it (#695). Degrades cleanly like every other art slot.</param>
    public readonly record struct CardOffice(string Letterhead, string ArtUrl);

    /// <summary>The five offices a card can be issued by, in the order the roll indexes them. Order is part
    /// of the save-compatible identity of a card: changing it re-issues every card in every wallet.</summary>
    private static readonly CardOffice[] TheOffices =
    [
        new("OFFICE OF WORKS · SUB-REGISTRY", "art/the-authority-card-works.jpg"),
        new("MINISTRY LIAISON · UNNUMBERED", "art/the-authority-card-liaison.jpg"),
        new("ESTATES · SPECIAL PROJECTS", "art/the-authority-card-estates.jpg"),
        new("PROCUREMENT · SCHEDULE C", "art/the-authority-card-procurement.jpg"),
        new("INSPECTORATE · NO STANDING", "art/the-authority-card-inspectorate.jpg"),
    ];

    /// <summary>Every office, for an audit that has to walk them all. Nothing in the game iterates this —
    /// a card gets exactly one, from <see cref="OfficeOf"/>.</summary>
    public static IReadOnlyList<CardOffice> CardOffices => TheOffices;

    /// <summary>WHICH office issued this card. The single roll — everything printed on the card, in words or
    /// in pixels, reads its answer rather than rolling again.</summary>
    public static CardOffice OfficeOf(AuthorityCard card) =>
        TheOffices[(int)(DiceRule.Seed($"hive:card:{card.BodyId}:{card.Band}") % (ulong)TheOffices.Length)];

    /// <summary>#695 · The face of THIS card. A pure function of the card's identity — no stored state, so a
    /// wallet loaded off a save shows the same five faces it showed when the cards were minted.</summary>
    public static string AuthorityCardArtUrl(AuthorityCard card) => OfficeOf(card).ArtUrl;

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

            // #677 · A record out of the halls is the SAME law as the pallet — what goes in the pocket is the
            // record of a thing that stays — said in the owner's own words, and it carries no leading indent
            // because the room it came out of has nothing of its own to say first (HaulLine returns empty
            // there, deliberately). Told apart by the find's own id, minted once by FindId.
            Haul.Relic when IsHallRecord(findId) => FoundRecordFindLine,
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

    // ── #725 · THE TWO LOUDEST SILENT FINDS ──────────────────────────────────────────────────────────────
    //
    // Owner's audit question: "are we giving enough attention to plot-significant finds? They should have a
    // Gen-AI image and their own dialog by our standards." Walking the four handoff floors, two were met
    // (THE SHAFT, DEAD AIR) and the two the handoff doc actually sends playtesters to were SILENT — a wall
    // stencil and a room of furniture, both missable at deck-plan zoom by a player who has just walked past
    // the reveal of the arc.
    //
    // Both cards are the allowed shape and not the other one: they SHOW HARDER AND REFUSE TO CONCLUDE. The
    // plate card describes two coats of paint and a screwed-on sign and ends without naming what was under
    // the first coat; the mess card describes squared chairs and warm machines and ends on the machines. No
    // subtitle, no hint, no verb. TheHiveTests.NothingDownHereEXPLAINSAnything is one deck up, and neither of
    // these goes round it.
    //
    // NEITHER MAY NAME THE PLATE'S TEXT. It varies by site kind (TitleOf/KindOn) and a card that quoted it
    // would be prose transcribing a sign the renderer draws — the same fact in two places, one of which never
    // hears about a change.

    public const string UnlistedLobbyArtUrl = "art/the-plate.jpg";

    public const string UnlistedLobbyLabel = "▣ THE PLATE";

    /// <summary>The first arrival on the unlisted band's own lobby floor. #592's whole arithmetic delivered
    /// by one sign, and the sign is never quoted. Authored, verbatim.</summary>
    public const string UnlistedLobbyCard =
        "The car opens on a lobby with no department and no livery — bare pour, one lamp, somebody's chair. " +
        "Beside the shaft there is a plate, and the plate has been done twice: a wide patch of newer paint " +
        "first, laid over something larger, and then the small name screwed on over that. Good work, both " +
        "times — a crew that stencils for a living, sent down here to change an answer. It is not the name " +
        "of anything you rode down through. You read it again. It says what it said. Above you, twenty " +
        "floors file and grade and answer to one name; the wall down here has been corrected.";

    public const string StaffMessArtUrl = "art/the-staff-mess.jpg";

    public const string StaffMessLabel = "🍽 THE STAFF MESS";

    /// <summary>The first entry into the staff canteen's room — a ROOM beat and not a floor beat, because
    /// the floor it is on is an ordinary floor and the room is the find. Authored, verbatim.</summary>
    public const string StaffMessCard =
        "A mess for the staff: machines on the wall still holding their temperature, chairs squared to the " +
        "tables the way a crew squares them at the end of a shift it expects to repeat. The door wanted a " +
        "pass shown. Inside there is nobody to show it to, and nothing out of place — no tray abandoned, no " +
        "chair shoved back, no note. Whatever ended here was not sudden, or it was tidied. The machines hum " +
        "and keep their hours. The shift has not come, and the machines are not the kind that wonder.";

    // ── #751 · THE TWO STORY-GRADE ROOMS THE HALL RULE ADDS ──────────────────────────────────────────────
    //
    // Owner: these rooms are story-grade — they get first-entry CARDS with gen-AI art, the same one-shot
    // pattern as THE PLATE and THE STAFF MESS (#725/#743). Prose authored, wired VERBATIM, and neither of
    // them says what the building is for: the hall's card is about MONEY (a company that feeds contractors
    // like a hotel), and the cabinet's is about MEMORY (a room that has none). §13.8 holds.

    /// <summary>#751 · The B1 cantina hall, painted.</summary>
    public const string CantinaHallArtUrl = "art/b1-cantina-hall.jpg";

    /// <summary>#751 · What the card is called.</summary>
    public const string CantinaHallLabel = "🍸 THE HALL";

    /// <summary>#751 · First entry into the B1 cantina hall. Authored, verbatim.
    ///
    /// <para>The register is #601's funding trail said as a room: a suspiciously nice company canteen on a
    /// nowhere rock is money that does not mind being SEEN feeding contractors, only being asked. Nobody in
    /// the frame finds it strange, which is the whole horror technique of this set.</para></summary>
    public const string CantinaHallCard =
        "Carriers' canteen, the sign says, and the room says something else: linen on the tables, brass on " +
        "the pillars, light somebody chose. On a rock with no name on any chart, the company feeds its " +
        "contractors like a hotel feeds guests it wants to keep — and nobody at the tables finds that " +
        "strange, because the pay is on the nail, the coffee is real, and questions are the one thing on " +
        "the menu that costs. Along the back wall, a row of doors. Cabinets, by arrangement. The hall is " +
        "loud. The doors are why.";

    /// <summary>#751 · The cabinet, painted.</summary>
    public const string CabinetArtUrl = "art/b1-cabinet.jpg";

    /// <summary>#751 · What the card is called.</summary>
    public const string CabinetLabel = "🚪 THE CABINET";

    /// <summary>#751 · The glyph the cabinet's filed line wears — the door, because a door with nothing
    /// written on it is the whole of what one of these rooms is from the hall side.</summary>
    public const string CabinetGlyph = "🚪";

    /// <summary>#751 · First entry into ANY cabinet — once total, never once per door. Authored, verbatim.
    ///
    /// <para>The telephone with no dial is canon furniture of a cabinet from here on: it receives and never
    /// dials, it has no mechanics, and nothing anywhere explains it.</para></summary>
    public const string CabinetCard =
        "Six chairs, a table wiped past clean, and a door padded like a vault that dogs shut from inside. " +
        "The hall outside is loud the way a sea is loud — a noise you can hide a sentence in, but every " +
        "face out there sits in the counter's long memory. In here there is no memory: whatever crosses " +
        "this table crosses it once and leaves in the pockets it came in. There is a telephone on the wall. " +
        "It has no dial. Rooms like this are not on the menu — you arrange them, or you are brought.";

    /// <summary>#751 · What the field book keeps of a cabinet. The card is the moment; this is the book's
    /// compressed record of it, and it is the only place the MECHANIC is ever stated — by observation, never
    /// by tooltip.</summary>
    public const string CabinetNote =
        "A cabinet off the hall: six chairs, one door, and no line of sight to the counter. Rooms like " +
        "this are why the hall is loud.";

    /// <summary>#751 · Does THIS floor's hall earn the cantina card? The card's first four words name the
    /// sign on the door — <c>CANTEEN 1 · CARRIERS &amp; CONTRACTORS</c> — so it belongs to the branch
    /// office's bar and never to the head office's dining room, which has a plate, a register and an
    /// arrival card of its own (#411). Asked here, so no client ever decides it.</summary>
    public static bool ShowsCantinaHallCard(string bodyId, int level)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return !IsHeadOffice(bodyId) && TopPressurisedFloor(bodyId) == level;
    }

    // ── #677 · WHAT THE HALLS SAY, WHICH IS ALMOST NOTHING ───────────────────────────────────────────────
    //
    // EVERY STRING BELOW IS THE OWNER'S, LIFTED VERBATIM. Nothing in this file may reword one, and nothing
    // anywhere may add to them: the prose down here is the whole of the feature's voice, and a sentence
    // written to fill a gap is the sentence that explains something. Where the generator has nothing
    // authored to say — the room line of a gallery that holds a record, for instance — it says NOTHING, on
    // purpose, and the card does the describing.
    //
    // The three canon walls these are written under (§10, §13.20), checked by grep in TheFoundBandTests:
    //
    //   1. the word §8 reserves never appears down here, in any string, ever;
    //   2. nothing names a builder, an age, a purpose, or the Old Ones and the Reevers;
    //   3. BOTH readings survive every line — the mundane one (better instruments, a better resurvey team)
    //      and the other one (this was always here and is being SHOWN to us). The moment one sentence
    //      settles which, the horror dies, and that is the Reever law applied to archaeology.
    //
    // The register, in the owner's own four words: HORROR SERVED AS SMOOTH COMFY PILLOW. Nothing down there
    // threatens; everything accommodates. The dread is entirely in the implication — a pillow means you were
    // expected.

    /// <summary>#677 · Said once per excursion, on the ride that crosses out of the poured shaft. The one
    /// sentence in the game about the boundary between the two worlds, and it describes a MATERIAL and stops.
    ///
    /// <para>Authored, verbatim. It is deliberately not decorated with a glyph the way the pulse lines around
    /// it are: the book's own column carries one, and the sentence is the owner's.</para></summary>
    public const string SeamLine =
        "The pour stops. Not at a wall — at a line, clean as a tide mark, and past it the tunnel keeps " +
        "going in a material the light does not grip.";

    /// <summary>#677 · Said once per excursion, stepping out onto the first gallery. Four sentences, three
    /// of them facts a suit could measure and the fourth an absence.
    ///
    /// <para>Placed LAST of the arrival's sayings, which is #693's open problem worked around rather than
    /// solved: the pulse has one slot and the last write wins, so the climax goes last. Authored,
    /// verbatim.</para></summary>
    public const string FoundArrivalLine =
        "The car has no button for this floor. It stops anyway. The air is good. Nothing here says why.";

    /// <summary>#677 · What a gallery says when there is nothing in it, which is almost every gallery.
    /// It REPLACES the facility's stripped line, which must never be said down here — somebody clearing a
    /// room in a hurry is a sentence about staff, and there was no staff. Authored, verbatim.</summary>
    public const string FoundEmptyRoomLine =
        "Nothing. Not stripped — nothing was ever here. The room is clean the way a prepared room is clean.";

    /// <summary>#677 · How many galleries in this many hold a record worth carrying out. The rest are the
    /// line above, and the ratio is the point: the emptiness is load-bearing squared down here.</summary>
    public const int FoundRecordOneInN = 9;

    /// <summary>#677 · The pickup line for a record find — #614's law exactly: what goes in the pocket is the
    /// RECORD of a thing that stays where it is, because a satchel claiming to hold a wall would be the third
    /// named bug class one size up. Authored, verbatim, and it carries no leading indent because the room it
    /// belongs to has nothing of its own to say first.</summary>
    public const string FoundRecordFindLine =
        "🎒 Into your pocket: measurements, a photograph, a rubbing. The wall keeps the rest.";

    /// <summary>#677 · What the casebook keeps. #603's law — looking is free, knowledge is one-shot — so the
    /// BOOK gets this and the pulse gets the find line, and one wall never appears in the book twice in two
    /// registers (#701's rule, learned on the shelves). Authored, verbatim.</summary>
    public const string FoundRecordGist =
        "a wall with no seam, faintly warm — the tape measure fails to give it scale";

    /// <summary>#677 · The look-card's title: the authored gist inside the house frame, exactly the way
    /// <c>OddBooks.CardTitle</c> puts an authored shelf fragment inside its own. No new prose is written for
    /// a caption that would otherwise have to invent one.</summary>
    public static string FoundRecordCardLabel => $"⭕ {FoundRecordGist}";

    /// <summary>#677 · The card body. Caption-only, in the #528 idiom the odd book and the lifeboat muster
    /// already keep: there is no painted art for this and a wired-but-unpainted image is a card claiming a
    /// picture it does not have. Authored, verbatim — evidence, and then it stops.</summary>
    public const string FoundRecordCard =
        "A section of wall, recorded because it cannot be brought back: continuous, seamless, faintly warm. " +
        "The tape measure in the photograph is there to give it a scale, and fails.";

    /// <summary>#677 · THE DURABLE ID OF ONE FIND, minted in one place.
    ///
    /// <para>It carries which kind of place it came out of, in its prefix, and that is what lets the two
    /// relic-class objects in the game tell themselves apart wherever they are met — in the pocket line, in
    /// the satchel row, and on the look-card — without any of those three re-deriving a floor's band for
    /// itself. A carried thing is asked what it IS, once, and the answer travels with it.</para></summary>
    public static string FindId(string bodyId, int level, int roomIndex)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return $"{(IsFound(bodyId, level) ? HallFindPrefix : "hive")}:{bodyId}:{level}:{roomIndex}";
    }

    /// <summary>The prefix a find out of the halls wears. Not "hive": the whole point is that it is not one.</summary>
    public const string HallFindPrefix = "hall";

    /// <summary>#677 · Did this find come out of a gallery nobody dug? Asked of the id rather than of a body
    /// and a level, because the satchel keeps the id and nothing else — a row that had to re-derive a band
    /// from a parsed level would be the same fact computed in a second place, which is what this file's own
    /// spec opens with a table of.</summary>
    public static bool IsHallRecord(string? findId) =>
        findId is not null && findId.StartsWith(HallFindPrefix + ":", StringComparison.Ordinal);

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
    /// the captain work out their own margin is a warning delivered too late.
    ///
    /// <para>#740 · And it does the sum in the SUIT'S units, off <see cref="SuitAir.Clock"/>, because the
    /// card and the gauge are describing one tank and a captain compares them by eye.</para></summary>
    public static string VacuumCard(string bodyId, int level, double airSeconds)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        int band = BandOf(level);
        int refuge = BandTop(band);          // the top of this band always holds pressure
        int floorsUp = -level - -refuge;     // how many floors between here and breathable

        // #740 · THE CARD READS THE GAUGE, it does not do its own sum. This used to format the raw play
        // budget as minutes and seconds — "you have 21 min 01 s" — while the HUD two seconds later on the
        // same floor said AIR 8h09. Both sentences were about the tank and both were honest about the number
        // they held; they were simply holding it in different units, because the card was the one surface in
        // the game that had never gone through SuitAir. A captain cannot be expected to know which of two
        // instruments is quoting the designer's stopwatch, so there is now one clock and the card asks for it.
        //
        // …and the sentence OWNS the quantity: it names the instrument the figure came off, so that a captain
        // who glances at their wrist a second later reads the same characters back, and so that the next hand
        // to edit this copy cannot quietly re-derive the number from something else.
        string margin = airSeconds > 0
            ? $"Your gauge reads {SuitAir.Clock(airSeconds)}, and that is the figure it will go on counting " +
              "down the whole way up."
            : "Your gauge is already reading empty, which is its own instruction.";

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
            $"The nearest floor of air is {NameOf(bodyId, refuge)} — {upstairs}. {margin}\n\n" +
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
        IReadOnlyList<Satchel.Item>? carried = null)
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
            stops.Add(new(f, NameOf(bodyId, f), HoldsPressure(bodyId, f), f == level, null));
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

    /// <summary>The card face for a card nobody can name — the #528 original, kept when #695 gave every card
    /// the face of its own issuing office.
    ///
    /// <para>No caller reaches it today, and that is a fact rather than an oversight: every seam that opens a
    /// card has already run <c>AuthorityCard.TryParse</c> before it asks for a picture, and an id that fails
    /// there gets no card at all rather than a card wearing a stranger's photograph. This is the face for a
    /// seam that has an authority in front of it and cannot roll — the satchel row already keeps the matching
    /// text fallback (<i>"🎫 an authority card"</i>), and the art side should not have to invent one under
    /// pressure. It is deliberately NOT one of the five, so it can never impersonate an office.</para></summary>
    public const string AuthorityCardFallbackArtUrl = "art/the-authority-card.jpg";

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
