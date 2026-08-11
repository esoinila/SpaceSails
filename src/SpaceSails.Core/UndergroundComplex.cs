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
        in SurfaceLayout.Field field)
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

            // #801 · A DETOUR FROM EVERY CAR, not from the cage. This measured one shaft, and the day the
            // building grew a second one at the other end of the corridor it went on passing while the
            // sentence it exists to protect died: a third of the refuges in the game were four steps from
            // the goods car. The guard found it (332 of 1130 floors); the fix is that the carve asks the
            // same list the guard does.
            bool far = true;
            foreach (Shaft car in ShaftsOn(field))
            {
                double dx = rooms[i].X - car.X, dy = rooms[i].Y - car.Y;
                far &= (dx * dx) + (dy * dy) >= MinRefugeDetourDu * MinRefugeDetourDu;
            }
            if (far)
            {
                faraway.Add(i);
            }
        }

        // The detour is the design, so it is preferred — but it is NOT allowed to cost the guarantee. On a
        // floor whose rooms all happen to crowd the shaft, a near refuge beats no refuge, every time: the
        // owner's line is "at least one ... for pure safety", and a safety regulation that a seed can talk
        // out of is not one.
        // #801 · …and when NOTHING qualifies, the fallback takes the FURTHEST room rather than a rolled one.
        // With two cars at opposite ends of the spine there are floors whose every chamber is inside the
        // detour of one car or the other, and on those the old fallback rolled a room at random — which on
        // the sweep put a refuge twenty-nine du from a car on floors that had a sixty-du one going spare.
        // A safety regulation a seed can talk out of is not one, and neither is one it can shrug at.
        List<int> pool = faraway;
        if (pool.Count == 0 && anywhere.Count > 0)
        {
            int best = anywhere[0];
            double bestNear = -1;
            foreach (int i in anywhere)
            {
                double near = double.MaxValue;
                foreach (Shaft car in ShaftsOn(field))
                {
                    double dx = rooms[i].X - car.X, dy = rooms[i].Y - car.Y;
                    near = Math.Min(near, (dx * dx) + (dy * dy));
                }
                if (near > bestNear)
                {
                    (best, bestNear) = (i, near);
                }
            }
            pool = [best];
        }
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

    /// <summary>
    /// #791 · THE DESK'S SERVICE RUN — the whole front of the bar, as one segment, plus the spot behind it
    /// the service comes FROM.
    ///
    /// <para>Owner, live playtest 2026-08-08: <i>"The Bar desk is really long now, but there is only one
    /// spot to get service on it… we should probably have service on the whole length indicated somehow, so
    /// we would need an E-bus of the bar desk length instead of one bar keep cashier at a single spot. Now
    /// the counter is in front of the desk, where the bar keep physically would be behind the bar desk
    /// instead of being in front of it."</i></para>
    ///
    /// <h3>Why the fixture stopped being a point</h3>
    ///
    /// <para>The counter has been a POINT since #707 — one console, one interact radius — and #751 then grew
    /// the desk to eighty-odd deck units. The picture, the collidable wall and the row of stools all ran the
    /// length of it; the one thing that answered [E] covered about a fourteenth of it, in the middle, and
    /// nothing on the floor said which fourteenth. That is the drawn room and the pressed room disagreeing,
    /// which is this project's third named bug class, at the fixture the owner spends the most time at.</para>
    ///
    /// <para>So the fixture publishes a RUN and the client's press reaches the nearest point on it. One
    /// fixture, one card, many approach points — the owner's own sentence, and it costs a segment.</para>
    ///
    /// <h3>The three bands, in order, and they are an order</h3>
    ///
    /// <para><b>hall floor · the run · the desk · the keep's side.</b> <see cref="X0"/>..<see cref="X1"/> is
    /// laid on the HALL side of the counter's line, <see cref="HallServiceStandoffDu"/> out from it, which
    /// is where a customer stands. <see cref="KeepX"/>/<see cref="KeepY"/> is the middle of the sealed band
    /// BEHIND the desk — the strip #751 closed off because it is the one part of a bar the customer never
    /// stands in, which is exactly where the keep (#781) will stand. The counter's own collidable wall is
    /// between the two and stays the law: nothing here opens it, and a body may not cross it.</para>
    ///
    /// <para>It spans the SERVING desk only — from where the goods hoist's divider leaves off (#775) to the
    /// far end of the counter — because a bar you can order at across a freight shutter is not a bar. That
    /// is the same span the desk's own photograph is stretched over, so what is drawn and what serves are
    /// one length by construction rather than by two authors agreeing.</para>
    /// </summary>
    /// <param name="X0">One end of the run, on the hall side of the desk.</param>
    /// <param name="Y0">The same.</param>
    /// <param name="X1">The other end.</param>
    /// <param name="Y1">The same.</param>
    /// <param name="KeepX">Where the service comes from — behind the desk, in the sealed band.</param>
    /// <param name="KeepY">The same.</param>
    public readonly record struct ServiceRun(
        double X0, double Y0, double X1, double Y1, double KeepX, double KeepY)
    {
        /// <summary>The middle of the run — the fixture's own spot, where its one plate is read and where
        /// <c>?counter=1</c> sets a tester down.</summary>
        public double MidX => (X0 + X1) / 2.0;

        /// <summary>The same.</summary>
        public double MidY => (Y0 + Y1) / 2.0;

        /// <summary>Half the run, as a vector off <see cref="MidX"/>/<see cref="MidY"/>. This is the shape a
        /// client's interaction point wants: an anchor and a reach, so a fixture that is a point today is a
        /// run tomorrow without the press learning a new idea.</summary>
        public double HalfSpanX => (X1 - X0) / 2.0;

        /// <summary>The same.</summary>
        public double HalfSpanY => (Y1 - Y0) / 2.0;

        /// <summary>How long the desk serves for, in deck units. The number the guards measure "the whole
        /// length" against, so "a bus" can never quietly become "a slightly wider point".</summary>
        public double LengthDu => Math.Sqrt(((X1 - X0) * (X1 - X0)) + ((Y1 - Y0) * (Y1 - Y0)));

        /// <summary>How far this spot is from the desk's service line — <see cref="SurfaceCollision"/>'s own
        /// segment distance, the one every other reach in this game is measured with.</summary>
        public double DistanceTo(double x, double y) =>
            SurfaceCollision.DistanceToSegment(x, y, X0, Y0, X1, Y1);

        /// <summary>Is a captain standing here within <paramref name="reach"/> of being served? The whole of
        /// the E-bus, in one call, so the press and every guard ask the same question.</summary>
        public bool Serves(double x, double y, double reach) => DistanceTo(x, y) <= reach;
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
    /// <param name="ArtUrl">#756 · The picture this floor WEARS — drawn under the vector overlay, stretched
    /// across the hall's own box, the same seam the ship's rooms have used since the 3D renovation (the
    /// CANTINA wears <c>art/the-space-bar.jpg</c>). Owner: <i>"let's put todo to have gen-AI Bar image on
    /// the background like we have in space ports."</i> Published HERE, beside the box it is stretched over,
    /// because a renderer choosing which picture goes on which floor would be a second opinion about a room
    /// it does not own — the discipline <paramref name="BoardX"/> and <paramref name="PlateX"/> already
    /// answer to. Null leaves the floor bare, which is every hall nobody has painted yet.</param>
    /// <param name="Spots">#780 · The hall's FURNITURE, painted over its own boxes — the counter, and
    /// whatever fixture the next issue carves. Drawn over <paramref name="ArtUrl"/> and under every vector
    /// mark, at <see cref="SpotArtAlpha"/>. Empty on a hall with nothing painted in it, which is not a
    /// missing feature: a room's ambience and a room's furniture are two different claims and a hall may
    /// make either without making the other.</param>
    /// <param name="Stools">#792 · WHERE THE TALL SEATS ARE BOLTED DOWN, in the counter's own order —
    /// entry <c>s</c> is <c>Interior.TheStools</c>' stool <c>s</c>, so the seat a captain is told is free
    /// and the seat drawn free on the deck are the same piece of furniture. Empty where the counter does
    /// not serve. Published beside the box the counter's own three wall segments were built from, for the
    /// reason <paramref name="BoardX"/> already answers: a renderer measuring a bar it did not carve is how
    /// this project has twice set a captain down inside a wall.</param>
    /// <param name="Service">#791 · WHERE THIS DESK SERVES — the whole front of it as one segment, and the
    /// spot behind it the service comes from. Null on a hall whose counter takes no orders (the staff mess,
    /// the head office's sideboard), which is a true statement about those rooms: there is a counter in
    /// them, and nobody has ever been served over it. See <see cref="ServiceRun"/>.</param>
    public readonly record struct Hall(
        double X0, double Y0, double X1, double Y1, int SeatTarget, IReadOnlyList<Cabinet> Cabinets,
        double BoardX = 0, double BoardY = 0, double PlateX = 0, double PlateY = 0,
        string? ArtUrl = null, IReadOnlyList<SpotArt>? Spots = null,
        IReadOnlyList<(double X, double Y)>? Stools = null,
        IReadOnlyList<SurfaceLayout.Doorway>? Doors = null, FreightLift? Freight = null,
        ServiceRun? Service = null)
    {
        /// <summary>#780 · The furniture pictures, never null — a caller drawing a room must not have to
        /// ask whether "no spots" means an empty list or a missing one.</summary>
        public IReadOnlyList<SpotArt> Painted => Spots ?? [];

        /// <summary>#792 · The row of tall seats, never null — same reason as <see cref="Painted"/>. A hall
        /// whose counter does not serve has an empty row, which is a true statement about it and not a
        /// missing one.</summary>
        public IReadOnlyList<(double X, double Y)> StoolRow => Stools ?? [];

        /// <summary>#775 · EVERY WAY IN AND OUT OF THIS ROOM, published rather than inferred — the same
        /// discipline #587 applied to the ribs, for the same reason: a law about how many doors a hall has
        /// cannot be written against a list nobody keeps.
        ///
        /// <para>It holds BOTH kinds and does not distinguish them, because egress does not: the gaps in
        /// the rib's own face (which the hall never cut — see the module's header) and the front doors cut
        /// into the spine's face for #775. They come off one carve, out of the two functions that already
        /// own those two walls, so a door here is a gap there by construction.</para></summary>
        public IReadOnlyList<SurfaceLayout.Doorway> Openings => Doors ?? [];

        /// <summary>#775 · How many doors a room this size is REQUIRED to have — asked of its own published
        /// box, so a hall that grows a du grows its egress duty with it.</summary>
        public int EgressRequired => HallEgressDoors(FloorDu2);

        /// <summary>#775 · How much floor it has. The unit the egress law and the owner's "a canteen this
        /// size" are both stated in.</summary>
        public double FloorDu2 => (X1 - X0) * (Y1 - Y0);

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

    // ── #759 · THE PARK BEHIND THE BAR ───────────────────────────────────────────────────────────────────
    //
    // Owner, 2026-08-06 night: "Let's go Vault Tech fancy and have a view to an underground park behind the
    // bar, windows between… a recreation device made to squeeze more out of their workers." And then, from
    // a cruise ship two days later, the scale: "the reference is the ship's Central Park — the meeting place
    // the cafeterias and restaurants ring… Do not make the park a puny small closet. Make it too big."
    //
    // WHY IT IS A ROOM AND NOT A PICTURE. The bar's stool view and the hall's own establishing art are both
    // shot THROUGH the glass at green — so the moment those pictures shipped, the deck plan owed the player
    // a room on the other side of that wall. A backdrop with nothing behind it is the drawn world and the
    // simulated world disagreeing, which is the bug class this file keeps a table of.
    //
    // WHERE THE GROUND CAME FROM. Every rib in the building stops RibReachDu off the spine and the field
    // runs on for sixty du past that — a band the width of the base that no placer has ever put anything
    // in. The hall's rib now reaches HallRibExtraDu further into it (the owner's "at least double or triple
    // it"), and the park is the rest of it: as wide as the spine is long, which makes it several times the
    // floor area of the hall and the largest single room in the game.
    //
    // AND THE WALL BETWEEN IS GLASS, which is the one geometric fact the whole feature turns on: sight
    // crosses it, bodies do not, and the way in is a door at the end of a corridor somewhere else.

    /// <summary>#759 · One raised growing bed in the park — a solid box on the plan, stencilled with what is
    /// in it and where it goes.</summary>
    /// <param name="Number">1-based, as the plate reads.</param>
    /// <param name="X">Centre.</param>
    /// <param name="Y">Centre.</param>
    /// <param name="HalfW">Half-width of the box.</param>
    /// <param name="HalfH">Half-height.</param>
    /// <param name="Crop">What is growing in it, in the building's own shouting stencil voice.</param>
    public readonly record struct GrowingBed(
        int Number, double X, double Y, double HalfW, double HalfH, string Crop)
    {
        /// <summary>Is this spot inside the bed? The box the walls were laid on, and nothing else.</summary>
        public bool Contains(double x, double y) =>
            Math.Abs(x - X) <= HalfW && Math.Abs(y - Y) <= HalfH;

        /// <summary>What is stencilled on the end of it. The crop, and the room it is going to — which is
        /// the whole of the food connection: the counter's sign is CANTEEN 1 and so is this.</summary>
        public string Plate => $"🌱 BED {Number} · {Crop} · TO {ParkBedDestination}";
    }

    /// <summary>#801 · A room on the far side of the park — the back of house, entered off the gravel.
    ///
    /// <para>Its own box, its own door, its own plate, published for the same reason
    /// <see cref="Hall.Openings"/> is: "the far gates lead somewhere real" is a law about a list, and a law
    /// about a list nobody keeps is a law nobody can fail.</para></summary>
    /// <param name="Door">The gap cut in the park's far wall. It is the room's ONLY door — the band behind
    /// the park is the last of the field, and there is no corridor back there for a second one.</param>
    public readonly record struct BackRoom(
        double X0, double Y0, double X1, double Y1, SurfaceLayout.Doorway Door, string Plate)
    {
        /// <summary>The middle of it — where the search console stands and where the audit walks to.</summary>
        public double X => (X0 + X1) / 2.0;

        /// <summary>The same.</summary>
        public double Y => (Y0 + Y1) / 2.0;

        /// <summary>Is the captain in it?</summary>
        public bool Contains(double x, double y) => x >= X0 && x <= X1 && y >= Y0 && y <= Y1;
    }

    /// <summary>#759 · The park: the box, the walks through it, and what is standing in it.</summary>
    /// <param name="X0">Left edge, in the surface's own coordinates.</param>
    /// <param name="Y0">Bottom edge.</param>
    /// <param name="X1">Right edge.</param>
    /// <param name="Y1">Top edge.</param>
    /// <param name="Walk">The gravel walks, as the centre-line the ground was cleared along — the gate spur
    /// first, then the long curve. PUBLISHED because it is what makes the park a place you stroll rather
    /// than a lawn you look at: the beds are laid around it, and a guard walks every metre of it.</param>
    /// <param name="Beds">The raised beds, which are solid.</param>
    /// <param name="Benches">Steel benches beside the walk, one per bend.</param>
    /// <param name="Masts">Floodlight masts — the artificial day, as posts on the plan.</param>
    /// <param name="Gate">The doorway into it, as the segment across the opening.</param>
    /// <param name="Window">The glazed wall it shares with the hall, as the segment along it.</param>
    /// <param name="X">Where the park's own plate reads from — just inside the gate.</param>
    /// <param name="Y">The same.</param>
    /// <param name="FigureX">The lone figure on the far bench. Scenery: a plate at a coordinate, with
    /// nothing to press and nothing to say.</param>
    /// <param name="FigureY">The same.</param>
    /// <param name="FigurePlate">What the figure is, at plate size — one of <see cref="CanteenRegulars"/>'
    /// own strangers, seeded off the site so a park is the same park every time it is walked. Chosen here
    /// rather than in the renderer for the reason every string on these rooms is chosen here, and for one
    /// more: a client picking it out of the list with <c>string.GetHashCode</c> would pick a DIFFERENT one
    /// on every process start, and a guard run in the same process would never see it.</param>
    /// <param name="ArtUrl">The picture the floor of it WEARS — same seam as <see cref="Hall.ArtUrl"/>,
    /// laid in panels (<see cref="ParkArtPanels"/>) because one photograph stretched over a room six times
    /// wider than it is deep is a photograph nobody can read.</param>
    public readonly record struct Park(
        double X0, double Y0, double X1, double Y1,
        IReadOnlyList<(double X, double Y)> Walk,
        IReadOnlyList<GrowingBed> Beds,
        IReadOnlyList<(double X, double Y)> Benches,
        IReadOnlyList<(double X, double Y)> Masts,
        SurfaceLayout.Doorway Gate,
        SurfaceLayout.Wall Window,
        double X, double Y, double FigureX, double FigureY, string FigurePlate = "",
        string? ArtUrl = null,
        IReadOnlyList<SurfaceLayout.Doorway>? Gates = null,
        IReadOnlyList<BackRoom>? Back = null,
        IReadOnlyList<RingRoom>? Ring = null)
    {
        /// <summary>
        /// #813 · THE ROOMS WITH THE VIEW, all the way round — the near band's suites, the back of house,
        /// and the two end blocks, in that order.
        ///
        /// <para><b>The HALL is not one of them, and the omission is deliberate.</b> It is a ring room in
        /// every way that matters — its doors are gaps in the spine's face, its far wall is glass on the
        /// green — and it is already published twice: as this floor's <see cref="Amenity"/> and, for the
        /// wall itself, as <see cref="Window"/>. A third entry would be the same room in a third list, and
        /// a caller summing frontage or counting rooms off two of the three would get a different answer
        /// depending which two. So the ring is <i>the rooms the ring carved</i>, the hall is the hall, and
        /// anything measuring the park's whole perimeter unions the two out loud (see
        /// <c>TheParkIsTheCentreOfTheBlockTests.EveryDuOfTheFrontageIsARoomOrAGate</c>).</para>
        ///
        /// <para>Never null, so a caller asking "what faces the park" cannot mistake an empty ring for a
        /// missing one — and on the one floor that has a park, an empty ring is a bug rather than an
        /// answer.</para></summary>
        public IReadOnlyList<RingRoom> Frontage => Ring ?? [];

        /// <summary>
        /// #801 · WHAT IS ON THE OTHER SIDE, and the reason the far wall stopped being a horizon.
        ///
        /// <para>Owner, 2026-08-09: <i>"we could have rooms to explore below the park also (on the map).
        /// Walking through the park is fun, it should not be the edge."</i> He is describing a map problem
        /// and it was a real one: #775 made the green a thoroughfare between corridors, and a thoroughfare
        /// with a painted wall along one whole side is still a room you cross rather than a place you are
        /// IN. The back of house is what a park of this size actually has behind it — potting, soil, feed,
        /// a cold room the counter draws on — and it is the one row of doors in the building a captain
        /// reaches by walking across a garden.</para>
        ///
        /// <para>They are ordinary rooms: they appear in <see cref="FloorPlan.RoomCentres"/>, they hold what
        /// any room down here holds, and the A* audit that walks every room from the car walks these. What
        /// makes them the park's is only where their doors are.</para></summary>
        public IReadOnlyList<BackRoom> Rooms => Back ?? [];

        /// <summary>
        /// #801 · The doors OFF THE GRAVEL, in the ring's own order. Kept out of <see cref="Ways"/> on
        /// purpose: a Way is a way THROUGH the park, corridor to corridor, and these are ways OUT OF it into
        /// a room. The distinction is not decorative — the amenities' conservation sum counts a Way as a
        /// place and would count these twice.
        ///
        /// <para>#817 · IT IS READ OFF <see cref="Frontage"/> NOW, not off <see cref="Rooms"/>. The far
        /// band's back doors used to be the only openings in the park's boundary that led into a room, so
        /// "the back rooms' doors" and "the doors off the gravel" were the same list; the owner's ruling
        /// that every premium suite gets a door onto the green made them two different lists, and the one
        /// this property has always MEANT is this one. The sealing experiment in
        /// <c>TheParkIsWalkableTests</c> plugs whatever is in here and says out loud that anything adding a
        /// way onto the green belongs in it — reading the narrower list would have left that guard passing
        /// while proving nothing.</para></summary>
        public IReadOnlyList<SurfaceLayout.Doorway> BackDoors
        {
            get
            {
                var doors = new List<SurfaceLayout.Doorway>(Frontage.Count);
                foreach (RingRoom r in Frontage)
                {
                    if (r.Gate is { } onto)
                    {
                        doors.Add(onto);
                    }
                }
                return doors;
            }
        }

        /// <summary>
        /// #775 · EVERY WAY IN, and there is more than one now.
        ///
        /// <para>Owner, 2026-08-09: <i>"let's have multiple doors to the park… it is a kind of place people
        /// like to walk through on their way."</i> That is what a central park is FOR — the crossing, not
        /// the visit — and #790 shipped it with one gate at the end of one corridor, which makes it a
        /// destination and a cul-de-sac.</para>
        ///
        /// <para><see cref="Gate"/> is still the hall's own — the first of these, and the one the room's
        /// plate and its dev route are pinned to. This is all of them, published for the same reason
        /// <see cref="Hall.Openings"/> is: a law about how a room is entered cannot be written against a
        /// list nobody keeps.</para></summary>
        public IReadOnlyList<SurfaceLayout.Doorway> Ways => Gates ?? [Gate];

        /// <summary>Is the captain in the park? The box the walls were laid on — the hall's own law
        /// (<see cref="Hall.Contains"/>), for the same reason: a refuge-sized containment box in a room this
        /// size would say "you are not in the park" from almost everywhere in the park.</summary>
        public bool Contains(double x, double y) => x >= X0 && x <= X1 && y >= Y0 && y <= Y1;

        /// <summary>How much floor it has. The owner's "do not make it a puny small closet", in the one
        /// unit a guard can measure.</summary>
        public double FloorDu2 => (X1 - X0) * (Y1 - Y0);
    }

    /// <summary>#759 · Where the beds' produce goes, and it is the sign over the counter that serves it —
    /// <see cref="AmenitySigns"/>'s own CANTEEN 1, said by the thing that grows the food. That is the whole
    /// of the connection and it needs no sentence: the bed and the till name the same room.</summary>
    public const string ParkBedDestination = "CANTEEN 1";

    /// <summary>#759 · What the beds are growing, in the order they are laid. The card under the glass on
    /// the counter (#756) sells coffee, a fry-up and a stew whose ingredients are "sourced from as far down
    /// as we are willing to say" — every one of these is one of those things, standing in soil ten metres
    /// from the table it is served at, and nothing anywhere points that out.</summary>
    public static readonly IReadOnlyList<string> ParkCrops =
    [
        "TABLE GREENS",
        "STEW ROOT",
        "BREAKFAST TOMATO",
        "SOFT HERBS",
        "SALAD STOCK",
        "COFFEE · SIX TREES · TRIAL",
    ];

    /// <summary>#759 · What is stencilled at the gate. The owner's own two phrases, in the inspectorate
    /// voice the issue asks for: a company that builds a park underground is squeezing morale like any
    /// other ore, and it does not pretend otherwise on the sign.</summary>
    public const string ParkPlate =
        "🌳 THE PARK · RECREATION SCHEDULE POSTED · ATTENDANCE IS RECORDED";

    /// <summary>#759 · What the field book keeps of a walk in the park — filed once per excursion, the
    /// cabinet's own idiom (<see cref="CabinetNote"/>). Authored, verbatim.
    ///
    /// <para>The surveillance is a LINE and not a system, which is the whole restraint of the beat: the
    /// plate says attendance is recorded, the book records that it said so, and nothing anywhere counts
    /// anything. §13.8 holds — the park says what the KITCHEN is for and never once what the facility
    /// is.</para></summary>
    public const string ParkNote =
        "An indoor park behind the canteen's glass: gravel walks, raised beds under grow-lamps, and a "
        + "plate at the gate that says attendance is recorded. The beds are stencilled for the counter — "
        + "including the stew the card sources from as far down as they are willing to say, which is "
        + "growing ten metres from the table it is served at.";

    /// <summary>#759 · The glyph the park's filed line wears.</summary>
    public const string ParkGlyph = "🌳";

    /// <summary>#759 · What a bench is, on the plan. Steel, bolted, and a seat — the seat verb is #778's and
    /// arrives with it; this is the furniture it will arrive at.</summary>
    public const string ParkBenchPlate = "🪑 A STEEL BENCH";

    /// <summary>#759/#793 · Half the length of one, in deck units — the segment the carve bolts down and
    /// therefore the segment a body collides with. PUBLISHED because #793 makes the bench a SEAT WITH TWO
    /// ENDS (<see cref="ParkBenches"/>): where you sit down, where somebody else can sit, and how far apart
    /// the two are, are all this number. A caller measuring it off a screenshot would be doing geometry
    /// about furniture it did not bolt down (§13.15).</summary>
    public const double ParkBenchHalfDu = 1.8;

    /// <summary>#759 · The picture the park's floor wears. The owner's shotcrete ruling applies to it and it
    /// is the regenerated shot: <i>"the crude rock is not up to modern mining smooth spray concrete
    /// specs."</i></summary>
    public const string ParkArtUrl = "art/b1-park-walk.jpg";

    /// <summary>#759 · Which hall has a park behind it, asked exactly the way <see cref="HallArtFor"/> asks
    /// which halls get a picture — because it is the same question. The branch office's upper canteen is the
    /// room whose own art is shot through a window wall at the green; the head office's dining room is a
    /// different room in a different building with its own everything (#411), and the staff mess two
    /// hundred metres down has no view of anything.
    ///
    /// <para>ONE predicate, asked by the carve and by the paint, so a park can never be laid on a floor that
    /// then declines to paint it — or, worse, painted on a floor that has no room behind the glass.</para></summary>
    public static bool HasPark(string bodyId, Comfort use)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return use == Comfort.UpperCanteen && !IsHeadOffice(bodyId);
    }

    /// <summary>#813 · Does this floor get the block? The one floor that gets a park, asked exactly the way
    /// <see cref="HasPark"/> asks it, because it is the same question — a block with no park in the middle
    /// of it is a ring of offices around a hole.</summary>
    public static bool HasParkBlock(string bodyId, int level)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return IsHallFloor(bodyId, level) && HasPark(bodyId, HallUseOn(bodyId, level));
    }

    /// <summary>#759 · Which picture the park's floor wears, or null where there is no park.</summary>
    public static string? ParkArtFor(string bodyId, Comfort use) =>
        HasPark(bodyId, use) ? ParkArtUrl : null;

    /// <summary>#759 · The park's floor art, cut into panels across its own box.
    ///
    /// <para>The hall wears ONE picture because a hall is about as wide as a photograph is. The park is six
    /// times wider than it is deep — the owner's "make it too big" — and the same seam used once would
    /// stretch a 16:9 frame to 6:1 and turn a garden into a smear. So the law is published HERE, beside the
    /// box, for the reason every other number on these rooms is: a renderer working out how many copies of
    /// a picture go on a floor would be doing geometry about a room it does not own.</para></summary>
    public static IReadOnlyList<(double X0, double Y0, double X1, double Y1)> ParkArtPanels(in Park park)
    {
        double w = park.X1 - park.X0, h = park.Y1 - park.Y0;
        int panels = Math.Max(1, (int)Math.Round(w / (h * ParkArtAspect), MidpointRounding.AwayFromZero));
        var cut = new List<(double, double, double, double)>(panels);
        for (int i = 0; i < panels; i++)
        {
            cut.Add((
                park.X0 + (i * w / panels), park.Y0,
                park.X0 + ((i + 1) * w / panels), park.Y1));
        }
        return cut;
    }

    /// <summary>#759 · The shape of the frames this set is painted in — 1280 × 720, every one of them.</summary>
    public const double ParkArtAspect = 16.0 / 9.0;

    /// <summary>#759 · Half the width of a gravel walk. Comfortably wider than <see cref="DoorHalf"/>, for
    /// the reason DoorHalf itself was widened: a path narrower than a couple of the reachability flood's
    /// grid steps is a path that is open in the geometry and shut to anything that pathfinds.</summary>
    public const double ParkWalkHalfDu = 3.5;

    /// <summary>#759 · The promenade kept clear against the park's own walls, so the walk is never the only
    /// way across and a bed is never laid against the glass.</summary>
    public const double ParkEdgeClearDu = 5.0;

    /// <summary>#759 · Half a raised bed, across the park and along it.</summary>
    public const double ParkBedHalfWDu = 7.0;

    /// <summary>#759 · Half a raised bed, the short way.</summary>
    public const double ParkBedHalfHDu = 3.5;

    /// <summary>#759 · How many bends the long walk takes between the two ends. Curved is the owner's word
    /// — <i>"It must be WALKABLE, with curved paths … the curve that hides the far end"</i> — and a curve on
    /// a deck plan is a run of walkable ground whose beds were laid around it.</summary>
    public const int ParkWalkBends = 3;

    /// <summary>#759/#801 · How many floodlight masts stand against the far wall. It was a literal 5 inside
    /// the carve; it is named because the back of house is laid in the BAYS BETWEEN them (#801) and a second
    /// copy of the count would have put a door in front of a lamp post.</summary>
    public const int ParkMastCount = 5;

    /// <summary>#801 · Where the masts stand along the park, as a pure function of its box. One answer, asked
    /// by the carve that erects them and by the carve that lays rooms between them.</summary>
    public static IReadOnlyList<double> ParkMastXs(double x0, double x1)
    {
        double uLo = x0 + ParkEdgeClearDu, uHi = x1 - ParkEdgeClearDu;
        double span = uHi - uLo;
        var xs = new List<double>(ParkMastCount);
        for (int k = 0; k < ParkMastCount; k++)
        {
            xs.Add(uLo + (span * (k + 0.5) / ParkMastCount));
        }
        return xs;
    }

    // ── #801 · THE BACK OF HOUSE, ON THE FAR SIDE OF THE GREEN ────────────────────────────────────────────
    //
    // Owner, 2026-08-09: "we could have rooms to explore below the park also (on the map). Walking through
    // the park is fun, it should not be the edge."
    //
    // WHERE THE GROUND CAME FROM, written down because it is the whole engineering answer and it is not
    // obvious. The park is already the biggest room in the game and its size is a LAW — it must stand
    // ParkDepthDu deep and hold half again the floor of the hall behind it, and on the shipped field the
    // second of those binds at 38.3 du of the 42 it has. So the band beyond it could not be bought by making
    // the park shallower: there is 3.7 du of slack in the whole feature and a room needs twelve.
    //
    // It is bought instead from the LAST STRIP OF THE FIELD. The park's far wall clamps at
    // BottomY + EdgeMargin, and the edge margin is a SURFACE law — the half-lane the regolith generator
    // keeps clear so nothing is drawn into the #563 falloff. There is no falloff on a floor with a roof on
    // it: a Hive deck publishes no unseen wall at all, so nothing down here fades and nothing is clipped.
    // The band is 16.5 du of the field's own envelope that no floor has ever used, and a chamber module is
    // twelve. The one law that DOES bind is the envelope itself (`ItNEVERLeavesTheSurfacesOwnEnvelope`), and
    // the back wall is laid inside it with rock to spare.

    /// <summary>#801 · How deep a back-of-house room is. The facility's own chamber module
    /// (<see cref="RoomHeightDu"/>) and not a number of its own: these are ordinary rooms that happen to be
    /// entered off a garden.</summary>
    public static double ParkBackDepthDu => RoomHeightDu;

    /// <summary>#801 · How much rock is left between the back wall and the end of the field. Small on
    /// purpose — this is the last strip of the world and the building is meant to read as having used it —
    /// but never zero, because a wall standing exactly on the envelope is a wall one rounding error outside
    /// it.</summary>
    public const double ParkBackRockDu = 3.0;

    /// <summary>#801 · The pier of rock left between two back rooms.</summary>
    public const double ParkBackPierDu = 8.0;

    /// <summary>#801 · What is stencilled beside the doors in the far wall.
    ///
    /// <para>§13.8, and this row is a soft place to break it: a room behind a garden is one sentence away
    /// from being a room about what the garden was really for. Every one of these says what the KITCHEN and
    /// the GROUNDS are for — the same restraint the beds are stencilled with — and not one of them is about
    /// the facility. The cold room names CANTEEN 1 because the beds already do, which is the entire food
    /// connection and is still never pointed out.</para></summary>
    public static readonly IReadOnlyList<string> ParkBackPlates =
    [
        "🌱 POTTING · SOIL, TRAYS, GRIT",
        "🧰 GROUNDS PLANT · LAMPS, FEED, TIMERS",
        "❄ COLD ROOM · TO CANTEEN 1",
        "🧤 GROUNDS STORE · TOOLS SIGNED OUT AND BACK",
        "🚿 WASH-DOWN",
        "📋 GROUNDS OFFICE · ROTA POSTED",
    ];

    // ── #813 · THE MANHATTAN RULING — THE PARK IS THE MIDDLE OF THE BLOCK ────────────────────────────────
    //
    // Owner, 2026-08-09 evening: "The central park needs to be in the center of all the other rooms… not on
    // the side. Think of New York, is the park on one side or is it in the center?" And the clause that
    // decides every number below: "make sure the park prime real estate is not wasted and not unused, not on
    // any side. It is the best real estate."
    //
    // WHAT WAS WRONG WITH THE SHIPPED PARK. #759 bought its ground out of the strip beyond the ribs' far
    // ends — the one band no placer had ever used — so the green ran the whole width of the field with the
    // hall's glass on one long side and PAINTED ROCK on the other three. #801 put a row of doors in the far
    // wall, which was the owner noticing the same thing from inside: "walking through the park is fun, it
    // should not be the edge." A room with three dead sides is a room on the side of the map however big it
    // is, and three quarters of the best frontage in the building was frontage onto nothing.
    //
    // WHAT A BLOCK IS. The park is now the middle of a city block, and everything else here is the block:
    //
    //   ─────────────── THE SPINE ─────────────────   the block's own near street, with the cage on it
    //   │ suite │ G │ suite │   T H E   H A L L   │   NEAR RING · doors on the spine, glass on the park
    //   ├───────┴───┴───────┴─────────────────────┤
    //  W│                                         │E  WEST/EAST RING · doors on the street, glass on the
    //  e│            T H E   P A R K              │a  park; one gate through each
    //  s│                                         │s
    //  t├─────────────────────────────────────────┤t
    //   │ store │ G │ potting │ cold │ G │ office │   FAR RING · the back of house, now ring fabric:
    //   ─────────── THE BACK STREET ──────────────    a door on the street AND its old door on the gravel
    //
    // FOUR LAWS, and each of them is a guard in TheParkIsTheCentreOfTheBlockTests:
    //
    //   1. CENTRALITY. Every one of the park's four walls is faced by carved fabric. No side of it is the
    //      field's edge and none of it is rock.
    //   2. THE RING IS COMPLETE. Every du of the park's perimeter that is not a gate is a room's park-facing
    //      wall, and that wall is GLASS (published in FloorPlan.Windows, blocking like any other wall). The
    //      owner's "not unused, not on any side", in the one unit a guard can measure.
    //   3. A CORRIDOR ON EVERY SIDE. The near street is the spine itself; the far street is the back street;
    //      the west and east streets join them into one loop. Every ring room's door is on a street and
    //      never on the park, so nobody walks through an office to reach an office.
    //   4. THE CAR GOES WHERE THE BUILDING IS THINNEST. The ring's own frontage decides which end of the
    //      block the goods car stands at — the density decides the shaft's side and never the other way
    //      round (see ServiceShaftAt).
    //
    // WHICH SIDE OF THE SPINE. Always the lower one. The cage's alcove hangs off the spine's UPPER face, and
    // a block that took that side would have to be carved around the captain's own way home — a notch in the
    // best frontage in the building, on the one column nothing may ever be laid across (#585). The block
    // takes the side the cage does not, which is a reason and not a coin toss.

    /// <summary>#813 · How far the block's two service streets stand in from the spine's own end caps. What
    /// is left outside them is the rock the goods car's alcove is cut into, which is why this is a number
    /// and not the edge itself.</summary>
    public const double BlockStreetInsetDu = 14.0;

    /// <summary>#813 · How deep the ring is on the SPINE side — the premium band, and the one the hall
    /// stands in. It is the hall's own length (the old <c>RibReachDu + HallRibExtraDu</c> less the corridor
    /// half it was measured from), because the room that has to fit is the one with eighty seats in it: a
    /// band shallower than this would make the bar wider than the ground it stands on.</summary>
    public const double RingNearDepthDu = 51.5;

    /// <summary>#813 · How deep the ring is on the BACK STREET side. The facility's own chamber module
    /// exactly (<see cref="ParkBackDepthDu"/>) and not a number of its own — these are #801's back of house
    /// re-anchored, and #801's law is that they are ordinary rooms that happen to be entered off a garden.
    /// A ring band a du deeper than the module would have made that sentence false by one du, which is how
    /// a law quietly stops being one.</summary>
    public static double RingFarDepthDu => ParkBackDepthDu;

    /// <summary>#813 · How deep the ring is on the two ends of the block. Deeper than a chamber and far
    /// shallower than the near band: a corner office with the green out of one wall.</summary>
    public const double RingSideDepthDu = 20.0;

    /// <summary>#813 · How wide a ring room WANTS to be. A band is cut into
    /// <c>max(1, round(span / this))</c> rooms of equal width, so the pier between two suites is their
    /// shared wall and no frontage is ever left over — the owner's "not unused" said as arithmetic rather
    /// than as an intention.</summary>
    public const double RingRoomTargetDu = 40.0;

    /// <summary>#813 · The narrowest a ring room may be. Also the clearance a gate must leave at each end of
    /// a band: a corridor cut so close to the corner that the room beside it is a cupboard is a corridor
    /// that has eaten the frontage it was there to serve.</summary>
    public const double RingRoomMinDu = 16.0;

    /// <summary>
    /// #817 · HOW MUCH STREET FACE ONE DOOR SERVES.
    ///
    /// <para>Owner, live in a 40 du landscape office with one leaf in it: <i>"Oh just one door in a landscape
    /// office?"</i> and, the same evening, <i>"bigger spaces must have much more doors."</i> That overrode
    /// <see cref="RingRoom.Door"/>'s documented "exactly one", and this is the number the override is stated
    /// as: a room's street frontage divided by this, rounded, is how many leaves it gets.</para>
    ///
    /// <para>Eighteen du is the precedent already standing in the building rather than a figure somebody
    /// liked — the hall's own corridor face carries four to five doors over its run (#775/#812), which is
    /// this ratio to within a leaf. It is also comfortably more than twice
    /// <see cref="DoorHalf"/>, so two doors on the narrowest frontage the ring will ever cut
    /// (<see cref="RingRoomMinDu"/>) still leave a pier of wall between them.</para>
    /// </summary>
    public const double RingStreetFaceDuPerDoor = 18.0;

    /// <summary>
    /// #822 · THE FIRE CODE — no space may have only one way out, except the bedroom-small ones.
    ///
    /// <para>Owner's standing law, issued mid-build: <i>"no space may have only one door except bedroom-small
    /// rooms."</i> Every ring room takes at least this many exits, counting its street doors and its gate
    /// onto the green, however little frontage it has. The exemption is
    /// <see cref="FireCodeSmallRoomDu"/>.</para>
    /// </summary>
    public const int FireCodeMinExits = 2;

    /// <summary>
    /// #822 · How big a room may be and still be let off with one door. A space whose LONGEST side is no
    /// longer than this is bedroom-small: a WC cubicle, a privacy booth, a cell you can cross in two paces
    /// and whose one door you are always within reach of.
    ///
    /// <para>Named here rather than inside <see cref="RingOffice"/> because the sweep this law is really
    /// about is building-wide (#818 will walk every carved room in the facility), and a threshold that lived
    /// in the ring's own furniture file would be re-typed the moment a laboratory needed it.</para>
    /// </summary>
    public const double FireCodeSmallRoomDu = 8.0;

    /// <summary>#822 · How many doors a run of street frontage this long is served by, fire code included.
    /// The whole of the door law in one function, so the carve, the guards and any later sweep are reading
    /// one sentence.</summary>
    /// <param name="frontageDu">The room's street face.</param>
    public static int DoorsForFrontage(double frontageDu)
    {
        int wanted = (int)Math.Round(
            frontageDu / RingStreetFaceDuPerDoor, MidpointRounding.AwayFromZero);
        return frontageDu <= FireCodeSmallRoomDu
            ? Math.Max(1, wanted)
            : Math.Max(FireCodeMinExits, wanted);
    }

    /// <summary>
    /// #813 · WHAT IS STENCILLED ON A ROOM WITH THE VIEW — the six plates the block hangs on its park-facing
    /// frontage, and nowhere else in the building.
    ///
    /// <para>#775's amenity gradient says amenities follow rank. The Manhattan ruling is where that becomes
    /// a MAP: the rooms on the green are the expensive ones, so they get a vocabulary the corridors do not.
    /// Every other plate down here is drawn from <see cref="SignFor"/>'s own register of departments and
    /// refusals — <c>QUOTA OFFICE</c>, <c>DO NOT ADMIT UNESCORTED</c> — and those still go on the block's
    /// CORNER rooms, which stand past the end of the park's wall and have nothing to look at. The gradient
    /// is legible without a word being said about it: read along one wall and the rooms get better as the
    /// green comes into view.</para>
    ///
    /// <para>§13.8 holds, and this row is a soft place to break it exactly as the back of house is. Every
    /// one of these says what a ROOM is — a booking, a signature, an appointment — and not one of them says
    /// what the facility is for. The nearest any of them comes is #770's negotiation room, and all it names
    /// is where you book it: at the counter, which is the same sentence a cabinet's plate has carried since
    /// #751 (<see cref="CabinetPlate"/>). The building rents rooms with a view of a garden it built to
    /// squeeze morale out of a workforce, and it advertises the aspect.</para>
    /// </summary>
    public static readonly IReadOnlyList<string> ParkViewPlates =
    [
        "REGISTERED OFFICE · GARDEN ASPECT",
        "NEGOTIATION ROOM · BOOK AT THE COUNTER",
        "SIGNATORY SUITE · TWO KEYS",
        "SENIOR ROTA · GREEN SIDE",
        "PRIVILEGED RECORDS · READING ROOM",
        "RECEPTION · APPOINTMENTS HELD",
    ];

    /// <summary>#813 · Which side of the park a ring room faces it from. Near is the spine's side.</summary>
    public enum RingSide
    {
        /// <summary>The spine's side — the premium band, and the hall's.</summary>
        Near,

        /// <summary>The back street's side — the back of house.</summary>
        Far,

        /// <summary>The block's west end.</summary>
        West,

        /// <summary>The block's east end.</summary>
        East,
    }

    /// <summary>
    /// #813 · ONE ROOM ON THE RING — the room with the view, published with both of its walls.
    ///
    /// <para>Two walls are what make it one of these rather than an ordinary chamber, and they are on
    /// opposite sides of it: <see cref="View"/> is the park-facing wall, which is glass; <see cref="Door"/>
    /// is the way in, which is on a street. That is the owner's <i>"view side to the park, service side to
    /// the corridor"</i> as a record rather than as an arrangement two placers happen to agree on.</para>
    ///
    /// <para>Published for the reason <see cref="Hall.Openings"/> and <see cref="Park.Ways"/> are: "every
    /// park-facing wall is used" is a law about a list, and a law about a list nobody keeps is a law nobody
    /// can fail.</para>
    /// </summary>
    /// <param name="Number">1-based, in the order the ring was laid: near, far, west, east.</param>
    /// <param name="X0">Left edge, in the surface's own coordinates.</param>
    /// <param name="Y0">Bottom edge.</param>
    /// <param name="X1">Right edge.</param>
    /// <param name="Y1">Top edge.</param>
    /// <param name="Side">Which of the park's four walls this room is one of.</param>
    /// <param name="Door">The FIRST way in, cut in the street's face. #817 · It used to be the only one and
    /// this parameter used to say so; the owner overrode that from inside a landscape office with one leaf
    /// in it. Every caller that means "the way in" still means this one, and every caller that means "all of
    /// them" says <see cref="RingRoom.Doors"/>.</param>
    /// <param name="View">The park-facing wall, as the segment it was built as — null on a CORNER room,
    /// which stands past the end of the park's own wall and therefore has nothing to look at. A corner
    /// office with no view is the amenity gradient (#775) drawn on the plan.</param>
    /// <param name="Gate">The door in the park-facing wall, where this room has one. #817 · EVERY ROOM WITH
    /// A VIEW HAS ONE — owner's ruling, live: a premium office on a garden gets a door to the garden and not
    /// only a window at it. The far band's back of house has kept #801's own way in off the gravel since it
    /// was carved, and this is the same door, granted to the rooms that were paying for the aspect. Null on
    /// a corner room, which has no park in front of it — and null on the HALL, whose glass is still never a
    /// door: that rule was always about the bar's window wall and never about the ring's.</param>
    /// <param name="Plate">What is stencilled beside the FIRST street door.</param>
    /// <param name="Ways">#817 · Every street door, in the order they were cut along the frontage. Null on a
    /// room built before the count scaled, which is why <see cref="Doors"/> falls back to
    /// <paramref name="Door"/> rather than to an empty list — an empty list would make "every ring room
    /// opens onto a street" vacuously true, which is the one way a law about a list can fail silently.</param>
    /// <param name="Fittings">#817 · What is standing on the floor of it. See <see cref="RingOffice"/>.</param>
    /// <param name="Chairs">#817 · Every seat in it, in the room's own order. Chairs face the GLASS, because
    /// the view is what the room rents for and the furniture should agree.</param>
    public readonly record struct RingRoom(
        int Number, double X0, double Y0, double X1, double Y1, RingSide Side,
        SurfaceLayout.Doorway Door, SurfaceLayout.Wall? View, SurfaceLayout.Doorway? Gate,
        string Plate,
        IReadOnlyList<SurfaceLayout.Doorway>? Ways = null,
        IReadOnlyList<RingOffice.Fixture>? Fittings = null,
        IReadOnlyList<RingOffice.Chair>? Chairs = null)
    {
        /// <summary>#817 · EVERY STREET DOOR. Owner: <i>"bigger spaces must have much more doors."</i>
        /// Published for the reason <see cref="Hall.Openings"/> is: a law about how a room is entered cannot
        /// be written against a list nobody keeps.</summary>
        public IReadOnlyList<SurfaceLayout.Doorway> Doors => Ways ?? [Door];

        /// <summary>#817 · The furniture, never null.</summary>
        public IReadOnlyList<RingOffice.Fixture> Furniture => Fittings ?? [];

        /// <summary>#817 · The seats, never null.</summary>
        public IReadOnlyList<RingOffice.Chair> Seats => Chairs ?? [];

        /// <summary>#822 · How many ways out of it there are altogether — the street doors and the gate onto
        /// the green. The number the fire code is stated in, asked of the room's own published lists.</summary>
        public int Exits => Doors.Count + (Gate is null ? 0 : 1);

        /// <summary>The middle of it — where its plate is read from and where the audit walks to.</summary>
        public double X => (X0 + X1) / 2.0;

        /// <summary>The same.</summary>
        public double Y => (Y0 + Y1) / 2.0;

        /// <summary>Is the captain in it? The box the walls were laid on, and nothing else.</summary>
        public bool Contains(double x, double y) => x >= X0 && x <= X1 && y >= Y0 && y <= Y1;

        /// <summary>How much floor it has.</summary>
        public double FloorDu2 => (X1 - X0) * (Y1 - Y0);

        /// <summary>Does it look at the green? False on the four corner rooms, and that is a true statement
        /// about them rather than a missing one.</summary>
        public bool HasView => View is not null;
    }

    /// <summary>
    /// #813 · THE BLOCK, AS A PURE FUNCTION OF THE GROUND — every line the ring is laid on, decided once.
    ///
    /// <para>Field-pure on purpose, and it is the same discipline <see cref="RibColumnsOn"/> is written
    /// with: the goods car is placed against these numbers (<see cref="ServiceShaftAt"/>) and a car stands
    /// in the same place on every floor of a site, so anything the car is measured against has to hold for
    /// the whole building and not for the one floor that has a park on it.</para>
    /// </summary>
    /// <param name="WestStreetX">The centre line of the block's west street.</param>
    /// <param name="EastStreetX">The same, east.</param>
    /// <param name="X0">The park's own left edge.</param>
    /// <param name="X1">The park's own right edge.</param>
    /// <param name="Y0">The park's far wall — the back street's side.</param>
    /// <param name="Y1">The park's near wall — the spine's side, and the hall's glass.</param>
    /// <param name="SpineFaceY">The spine's lower face, which is the near ring's own front wall.</param>
    /// <param name="BackStreetY0">The back street's far face — the block's outer wall.</param>
    /// <param name="BackStreetY1">Its near face, which is the far ring's back wall.</param>
    /// <param name="SpurXs">Where a gate cuts through the near and far bands. These are rib columns, so a
    /// corridor and a cross corridor can never disagree about where the crossings are (#801's own
    /// reason).</param>
    public readonly record struct ParkBlock(
        double WestStreetX, double EastStreetX,
        double X0, double X1, double Y0, double Y1,
        double SpineFaceY, double BackStreetY0, double BackStreetY1,
        IReadOnlyList<double> SpurXs)
    {
        /// <summary>The inside face of the west street — the wall the west ring's doors are cut in.</summary>
        public double WestInnerX => WestStreetX + CorridorHalf;

        /// <summary>The same, east.</summary>
        public double EastInnerX => EastStreetX - CorridorHalf;

        /// <summary>The block's own outer wall, west.</summary>
        public double WestOuterX => WestStreetX - CorridorHalf;

        /// <summary>The same, east.</summary>
        public double EastOuterX => EastStreetX + CorridorHalf;

        /// <summary>How much floor the park itself has — what the owner's "do not make it a puny small
        /// closet" is measured in.</summary>
        public double ParkDu2 => (X1 - X0) * (Y1 - Y0);
    }

    /// <summary>#813 · The block's lines, off the ground alone. See <see cref="ParkBlock"/>.</summary>
    public static ParkBlock BlockOn(in SurfaceLayout.Field field)
    {
        double margin = SurfaceLayout.EdgeMargin + 6;
        double left = field.LeftX + margin, right = field.RightX - margin;
        (double _, double shaftY) = ShaftAt(field);

        double west = left + BlockStreetInsetDu, east = right - BlockStreetInsetDu;
        double spineFace = shaftY - CorridorHalf;
        double nearY = spineFace - RingNearDepthDu;
        double farY = nearY - ParkDepthDu;
        double streetY1 = farY - RingFarDepthDu;
        double streetY0 = streetY1 - (2 * CorridorHalf);

        double x0 = west + CorridorHalf + RingSideDepthDu;
        double x1 = east - CorridorHalf - RingSideDepthDu;

        // WHICH COLUMNS THE GATES GO DOWN. The rib columns, so a gate and a cross corridor are one line —
        // and only those that leave a whole room at each end of the band they cut. A gate against the corner
        // would buy a through-route by spending the frontage the through-route exists to show off.
        var spurs = new List<double>();
        foreach ((int _, double rx) in RibColumnsOn(field))
        {
            if (rx - CorridorHalf >= x0 + RingRoomMinDu && rx + CorridorHalf <= x1 - RingRoomMinDu)
            {
                spurs.Add(rx);
            }
        }

        return new ParkBlock(west, east, x0, x1, farY, nearY, spineFace, streetY0, streetY1, spurs);
    }

    /// <summary>
    /// #813 · HOW MUCH ROOM FRONTAGE THE BLOCK CARRIES ON EACH SIDE OF ITS OWN MIDDLE, in deck units.
    ///
    /// <para>The near and far bands run the length of the block and the gates cut them; what is left is
    /// room, and how much of it lies each side of the park's centre line is what "the less-built side"
    /// means. It is not symmetric, and the reason it is not is worth saying: the rib columns are laid at
    /// fifths of the spine and the one nearest the cage is DROPPED (<see cref="RibColumnsOn"/>), so the
    /// gates fall on one side of the middle and the long unbroken run of suites falls on the other.</para>
    ///
    /// <para>Field-pure, so the car it decides stands in the same place on every floor.</para>
    /// </summary>
    public static (double West, double East) RingFrontageOn(in SurfaceLayout.Field field)
    {
        ParkBlock block = BlockOn(field);
        double mid = (block.X0 + block.X1) / 2.0;
        double west = mid - block.X0, east = block.X1 - mid;
        foreach (double sx in block.SpurXs)
        {
            double lo = sx - CorridorHalf, hi = sx + CorridorHalf;
            west -= Math.Max(0, Math.Min(hi, mid) - Math.Max(lo, block.X0));
            east -= Math.Max(0, Math.Min(hi, block.X1) - Math.Max(lo, mid));
        }
        return (west, east);
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
        IReadOnlyList<EnSuite> EnSuites,
        // #759 · THE GLAZING, kept out of Walls on purpose. Every segment here is a wall a body may not
        // pass and an eye may — the renderer puts them back into the deck in the window idiom the ship's
        // own bridge glass already uses, so one segment carries both halves and nothing draws a second one.
        IReadOnlyList<SurfaceLayout.Wall>? Windows = null,
        // #759 · The park, on the one floor that has one.
        Park? Park = null,
        // #798 · Somewhere to put a document you never want read again. Appended and never inserted: every
        // caller of this record builds it positionally.
        IReadOnlyList<RipAndBin.Bin>? Bins = null)
    {
        /// <summary>#798 · Somewhere to put a paper on this floor, never null — a caller asking "is there a
        /// bin here" must not have to tell an empty list from a missing one.</summary>
        public IReadOnlyList<RipAndBin.Bin> TheBins => Bins ?? [];
    }

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
        // ── #813 · IS THIS THE BLOCK'S FLOOR? Decided first, because it decides which way every corridor on
        //    it runs. See the Manhattan header above ParkBlock.
        ParkBlock? blockOn = HasParkBlock(bodyId, level) ? BlockOn(field) : null;

        var ribXs = new System.Collections.Generic.List<(double X, bool Down)>();
        // #801 · The x's come from RibColumnsOn now — the same list the second car is placed against, so a
        // car and a corridor can never disagree about where the corridors are. Which WAY each one runs is
        // still this floor's own seeded business.
        //
        // #813 · …on every floor but ONE. On the block's floor the lower half of the field IS the block, all
        // of it, so a column runs DOWN if and only if the block uses it as a gate through the ring, and
        // every other column runs UP into the ordinary grid. That is not the seed being overruled for
        // convenience: a rib running down anywhere else would arrive in the middle of somebody's office,
        // and the seeded direction is a fact about a floor with two open halves.
        foreach ((int ordinal, double rx) in RibColumnsOn(field))
        {
            bool ribDown = Frac(bodyId, $"hive:{level}:rib-dir:{ordinal}") < 0.62;
            if (blockOn is { } gated)
            {
                ribDown = false;
                foreach (double sx in gated.SpurXs)
                {
                    ribDown |= Math.Abs(sx - rx) < 0.001;
                }
            }
            ribXs.Add((rx, ribDown));
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

        // #801 · …and the GOODS CAR's alcove, as a mouth in the LOWER face at the blind end of the corridor.
        // Two cars on one face would read as one machine room; on opposite faces, at opposite ends, they read
        // as two ways out, which is the whole of the feature. Appended for the same reason the cage is, and
        // the sweep sorts (§13.2) so being out of x order cannot re-seal anything.
        double? serviceX = ServiceShaftAt(field) is { } car ? car.X : null;
        if (serviceX is { } sx2)
        {
            ribXs.Add((sx2, true));
        }

        // #775 · THE DOORS THE HALL CUTS IN THE SPINE'S OWN FACE, filled in by the carve below and read by
        // the wall builder — one list, so the gap the corridor leaves and the door the hall publishes are
        // the same gap. #585's law, said about the one wall the hall did not previously own a hole in.
        //
        // Owner, walking the new B1: "the bar/canteen needs DOORS ON THE MAIN CORRIDOR — today you have to
        // really look for the way in; a venue's entrance should find YOU." He was right about the topology:
        // the hall's near wall IS the spine's face, and until this list existed that face ran unbroken past
        // it, so a hundred and ten du of canteen frontage on the building's only through-corridor had no
        // way in at all. You had to turn down the rib and find a gap in the side wall.
        var hallSpineCuts = new List<(double Lo, double Hi)>();
        double hallSpineFaceY = double.NaN;

        // #813 · …and the SAME wall, for the same reason, on behalf of every other room in the near band.
        // The block's premium suites front the spine exactly the way the hall does — their door is a gap in
        // this face and nothing else — so they hand their spans to the very list the wall is swept from and
        // never cut a wall of their own. Kept apart from the hall's cuts only because the plates differ:
        // the hall's first cut is the venue's entrance and the rest are its egress doors, and an office's
        // plate is the office's own.
        var ringSpineCuts = new List<(double Lo, double Hi, double PlateX, string Plate)>();

        // #775 · …and the mouths on that face that are corridors rather than doors: the walks down to the
        // park. Kept in their own list because the two are drawn differently and always were — a doorway
        // gets an imported leaf and a plate, a corridor mouth gets neither, exactly as the ribs' mouths get
        // neither.
        //
        // #813 · There were one of these and there are four: the block's two service streets and a gate
        // down each of the near band's crossings. The spine is the block's own near street now, and a street
        // that meets it has a mouth in it.
        var spineMouths = new List<(double Lo, double Hi)>();

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
            //
            // #775 · …and a mouth is a SPAN now rather than a centre, because the two things cut into this
            // wall are no longer the same width: a rib mouth is the corridor's own CorridorHalf and a hall's
            // front door is DoorHalf, the number every other door in the building is cut to. One sweep, one
            // sorted list of spans, and the cursor still only ever moves forward.
            var mouths = new List<(double Lo, double Hi)>();
            foreach ((double rx, bool down) in ribXs)
            {
                if (cutHere(rx, down))
                {
                    mouths.Add((rx - CorridorHalf, rx + CorridorHalf));
                }
            }
            if (Math.Abs(y - hallSpineFaceY) < 0.001)
            {
                mouths.AddRange(hallSpineCuts);
                mouths.AddRange(spineMouths);
                foreach ((double lo, double hi, double _, string _) in ringSpineCuts)
                {
                    mouths.Add((lo, hi));
                }
            }
            mouths.Sort((a, b) => a.Lo.CompareTo(b.Lo));

            double cursor = left;
            foreach ((double lo, double hi) in mouths)
            {
                double near = Math.Max(cursor, lo);
                if (near > cursor)
                {
                    walls.Add(new(cursor, y, near, y, true));
                }
                cursor = Math.Max(cursor, hi);
            }
            walls.Add(new(cursor, y, right, y, true));
        }

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

        // ── #801 · AND THE SECOND ONE, the same box mirrored onto the lower face. Same spot on every floor,
        //    for the cage's own reason: a car a captain has to look for twice is a car they will not use.
        //    Claimed as well as built — the ground it stands on is past the last rib's chambers, so nothing
        //    was ever going to be laid here, and the ledger says so rather than leaving it to arithmetic.
        if (serviceX is { } carX)
        {
            walls.Add(new(carX - ShaftHalf, shaftY - CorridorHalf, carX - ShaftHalf, shaftY - CorridorHalf - 5, true));
            walls.Add(new(carX + ShaftHalf, shaftY - CorridorHalf, carX + ShaftHalf, shaftY - CorridorHalf - 5, true));
            walls.Add(new(carX - ShaftHalf, shaftY - CorridorHalf - 5, carX + ShaftHalf, shaftY - CorridorHalf - 5, true));
            claimed.Add((
                carX - ShaftHalf - 1.5, shaftY - CorridorHalf - 6.5,
                carX + ShaftHalf + 1.5, shaftY - CorridorHalf));
        }
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
        // #813 · …and on the block's floor it is not carved off a rib at all. It is a RING ROOM: the widest
        // sub-segment of the near band, front doors on the spine, glass on the park — which is what it has
        // been in every way but its bookkeeping since #775 hung its doors on the main corridor. What the
        // Manhattan ruling changed is that the ground either side of it is now a room too.
        HallSite? hallSite = null;
        Park? park = null;
        (int Rib, int Side)? hallSlot = null;
        var ring = new List<RingRoom>();

        // #759 · The park's own glazing, kept apart from the poured walls all the way out of this method.
        // See CarveHall: one segment, in the list that says what it is MADE OF, and the client turns it back
        // into a wall the eye reads as glass and the boots read as wall.
        var glass = new List<SurfaceLayout.Wall>();

        if (blockOn is { } block)
        {
            Comfort use = HallUseOn(bodyId, level);
            double mouth = block.SpineFaceY, far = block.Y1;

            // ── WHICH SUB-SEGMENT OF THE BAND · #751's rule, unchanged, asked of the block's own list
            //    (RingNearSegments) rather than of the room slots down a rib: the best FLOOR wins and
            //    nearest-the-cage breaks the tie. The hall takes the whole of the segment it stands in
            //    unless what is left over would still make a room, and never leaves a strip too narrow to
            //    be one — the owner's "not unused" applied to the ground the biggest room in the building
            //    does not want.
            var wide = new List<Rib> { new(block.WestInnerX - CorridorHalf, true) };
            double wanted = HallGround(
                bodyId, use, wide, 0, +1, mouth, far, shaftX, serviceX, left, right + 400.0, roomScale)
                is { } asked ? asked.Wanted : 0.0;

            double bestLo = double.NaN, bestW = 0, bestD2 = double.MaxValue;
            foreach ((double lo, double hi) in RingNearSegments(block))
            {
                double span = hi - lo;
                double w = span <= wanted || span - wanted < RingRoomMinDu ? span : wanted;
                var pseudo = new List<Rib> { new(lo - CorridorHalf, true) };
                if (HallGround(
                        bodyId, use, pseudo, 0, +1, mouth, far, shaftX, serviceX,
                        left, lo + w + HallEdgePadDu, roomScale)
                    is null)
                {
                    continue;   // this segment will not take a hall, and that is a real answer
                }
                double d2 = ((lo + (w / 2.0)) - shaftX) * ((lo + (w / 2.0)) - shaftX);
                if (w > bestW + 0.5 || (w > bestW - 0.5 && d2 < bestD2))
                {
                    (bestLo, bestW, bestD2) = (lo, Math.Max(w, bestW), d2);
                }
            }

            if (!double.IsNaN(bestLo))
            {
                var stand = new List<Rib> { new(bestLo - CorridorHalf, true) };
                hallSite = CarveHall(
                    walls, glass, hallSpineCuts, bodyId, level, stand, 0, +1, mouth, far,
                    shaftX, serviceX, left, bestLo + bestW + HallEdgePadDu, roomScale, glazed: true);
            }

            if (hallSite is { } built)
            {
                // #775 · WHICH FACE OF THE SPINE THE HALL'S FRONT DOORS ARE CUT IN — the carve's own mouth,
                // and never a second opinion about it. It is the whole near band's face now.
                hallSpineFaceY = mouth;
                claimed.Add((
                    built.Hall.X0 - 1.5, built.Hall.Y0 - 1.5,
                    built.Hall.X1 + 1.5, built.Hall.Y1 + 1.5));

                // ── #813 · THE RING, AND THEN THE GREEN IN THE MIDDLE OF IT ────────────────────────────
                //
                // In this order because the ring owns the park's whole boundary: every wall the park has is
                // a room's glass or a gate's stub, so the green is what is left when the block has been
                // built rather than a box with openings cut in it afterwards.
                var parkGates = new List<SurfaceLayout.Doorway>();
                ring = CarveRing(
                    walls, glass, doorways, labels, claimed, ringSpineCuts, spineMouths, parkGates,
                    bodyId, level, block, built.Hall);

                park = CarvePark(
                    walls, bodyId, level, built.Hall, built.Glass!.Value, block, parkGates, ring);
            }
        }
        else
        {
            // ── EVERY OTHER FLOOR, UNCHANGED · the staff mess two hundred metres down stands on a rib's
            //    room column exactly as it has since #751, and there is no park behind it to glaze.
            hallSlot = HallSlotFor(
                bodyId, level, ribList, field, shaftX, serviceX, shaftY, left, right, roomScale);
            if (hallSlot is { } slot)
            {
                (double hmouth, double hfar) = RibReach(field, shaftY, ribList[slot.Rib].Down, hall: true);
                hallSite = CarveHall(
                    walls, glass, hallSpineCuts, bodyId, level, ribList, slot.Rib, slot.Side, hmouth, hfar,
                    shaftX, serviceX, left, right, roomScale, glazed: false);
                if (hallSite is { } built)
                {
                    hallSpineFaceY = hmouth;
                    claimed.Add((
                        built.Hall.X0 - 1.5, built.Hall.Y0 - 1.5,
                        built.Hall.X1 + 1.5, built.Hall.Y1 + 1.5));
                }
                else
                {
                    hallSlot = null;   // the ground would not take one. It keeps its ordinary canteen.
                }
            }
        }

        // ── #585/#775 · THE SPINE'S TWO LONG FACES, BUILT LAST OF THE THREE ──────────────────────────────
        //
        // The lift alcove hangs off the TOP face, so that face needs a mouth for it too — otherwise the car
        // opens into a sealed box and the captain cannot reach their own way out. The A* audit reported this
        // as "the lift cannot be reached from the lift", which is as clear as a guard gets.
        //
        // #775 · …and these two lines used to stand above the hall carve, which is why the hall could not
        // have a front door: the wall was already poured by the time anybody knew where the room was. They
        // are the SAME two calls, moved after the carve, reading the one list of cuts the carve filled in.
        // Nothing else between here and there touches this wall — walls are a set, not a sequence.
        //
        // #801 · …and the LOWER face now has an alcove of its own to leave a mouth for. Same clause, same
        // wall, the other way up: the goods car opens into a sealed box otherwise, which is #585's "the lift
        // cannot be reached from the lift" said about the car nobody had built yet.
        SpineFace(shaftY + CorridorHalf, (rx, down) => !down || Math.Abs(rx - shaftX) < 0.001);
        SpineFace(shaftY - CorridorHalf,
            (rx, down) => down || (serviceX is { } atX && Math.Abs(rx - atX) < 0.001));

        // #775 · THE FRONT DOORS, PUBLISHED FROM THE VERY LIST THE WALL WAS CUT FROM. #585's one-gap law
        // with no room left for a second opinion: the segments above stop at these spans and the leaves
        // below are drawn across them, off one list, in one method.
        //
        // …and nothing is drawn past the seam, exactly as AddRoomsAlong has it: a gallery has no door in it,
        // only a way through, so the gap is cut and no imported leaf is hung in it.
        if (!IsFound(bodyId, level))
        {
            // …and the plate goes on the CORRIDOR side of each of them, which is the whole point of the
            // feature: a walker on the spine is told what the wall beside them is before they have to
            // wonder. The first cut is the entrance (it was placed at the lift), the rest are the doors a
            // code put there and they say so.
            //
            // BESIDE the door and never over it, on the side the walker is coming from. A plate centred on
            // its own doorway is a plate with the captain standing on top of it the moment they arrive —
            // watched happen in the browser on the first boot of ?frontdoor=1, the dot sitting squarely on
            // the word CANTEEN — and a sign you have to step off to read is not signage.
            double plateY = hallSpineFaceY > shaftY ? hallSpineFaceY - 2.0 : hallSpineFaceY + 2.0;
            double aside = DoorHalf + 3.0;
            for (int d = 0; d < hallSpineCuts.Count; d++)
            {
                (double lo, double hi) = hallSpineCuts[d];
                doorways.Add(new(lo, hallSpineFaceY, hi, hallSpineFaceY));

                double cx = (lo + hi) / 2.0;
                double plateX = Math.Clamp(
                    cx + (cx > shaftX ? -aside : aside),
                    hallSite is { } signed ? signed.Hall.X0 : cx,
                    hallSite is { } bounded ? bounded.Hall.X1 : cx);
                labels.Add(new(plateX, plateY,
                    d == 0
                        ? HallEntrancePlate(bodyId, HallUseOn(bodyId, level))
                        : HallEgressPlate(d + 1)));
            }

            // #813 · …and the near band's other doors, out of the very same list the wall was swept from.
            // Their plates are the building's own vocabulary rather than the venue's, and they go on the
            // corridor side for the reason the hall's do: a walker on the spine is told what the wall beside
            // them is before they have to wonder.
            //
            // #817 · …and a room with three of them is stencilled ONCE. The carve hands the plate over with
            // the FIRST of a room's cuts and hands the rest over blank, so a landscape office that earned
            // more leaves does not also earn three copies of its own name across one wall.
            foreach ((double lo, double hi, double plateX, string plate) in ringSpineCuts)
            {
                doorways.Add(new(lo, hallSpineFaceY, hi, hallSpineFaceY));
                if (plate.Length > 0)
                {
                    labels.Add(new(plateX, plateY, plate));
                }
            }
        }

        // #775 · AND THE FREIGHT DOOR, HUNG. The shutter is a locked door like every other door down here
        // that will not open — drawn shut, walled behind, and its plate is what [E] reads — so the refusal
        // is TOLD in the grammar the building already speaks, and no new client idiom was invented for it.
        if (hallSite is { } withFreight && withFreight.Hall.Freight is { } hoist)
        {
            locked.Add(new(
                hoist.Shutter.X1, hoist.Shutter.Y1, hoist.Shutter.X2, hoist.Shutter.Y2, hoist.Plate));
            labels.Add(new(hoist.PlateX, hoist.PlateY, FreightSign));
        }

        // ── THE RIBS. Cross corridors off the spine, with rooms flanking them.
        for (int i = 0; i < ribXs.Count; i++)
        {
            (double x, bool down) = ribXs[i];
            if (Math.Abs(x - shaftX) < 0.001
                || (serviceX is { } carMouth && Math.Abs(x - carMouth) < 0.001))
            {
                continue;   // that entry is a car's alcove mouth, not a corridor
            }
            if (blockOn is not null && down)
            {
                // #813 · The block's own gates. Their walls, their claim and the doorway at the end of them
                // were laid by CarveRing, because the walls either side of a gate ARE the party walls of the
                // rooms it runs between — one wall, laid once, by whichever placer owns both of its faces.
                // A second pass over them here is exactly the two-authors-one-line bug this file opens with
                // a table of.
                continue;
            }
            // #759 · The hall's rib runs longer than the rest, and this is the same call the carve made —
            // never a second answer. `hallRib` is also the one rib in the building whose far end is a WAY IN
            // rather than an end: the park is behind it.
            bool hallRib = hallSlot is { } onThis && onThis.Rib == i;
            (double mouth, double far) = RibReach(field, shaftY, down, hall: hallRib);

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
            //
            // #759 · …EXCEPT THE ONE THAT IS THE PARK GATE. On the hall's rib, where a park was carved, the
            // corridor does not end: it opens, through a doorway cut to the same DoorHalf every other door
            // in the building is cut to, and that gap is the ONLY way a body gets into the park. (The wall
            // it shares with the hall is glass — an eye crosses it and nothing else does.) A sealed mouth
            // with a distance stencilled on it here would be a sign lying about a door you can see through.
            //
            // #775 · …AND IT IS NO LONGER THE ONLY ONE. Owner: "let's have multiple doors to the park — it
            // is a kind of place people like to walk through on their way." Every rib pointing the park's
            // way now runs the extra HallRibExtraDu that the hall's rib always ran and opens where it
            // arrives, so a route down one rib and up another crosses the green instead of going round it.
            // The extension is corridor, not room: the chambers were laid against `far` and nothing stands
            // in the sixteen du beyond it — that is the same unused band the park itself came out of.
            // #813 · Every rib that gets this far runs away from the block, so its far end is an end.
            // The one that used to be a way in is a gate through the ring now, and it never reaches here.
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
        // #813 · …and the ring's own rooms join the pool BEFORE either of them, which is #775's amenity
        // gradient cashed out: a washroom on the park side of the block is a washroom with a window, and
        // "amenities follow rank" means the best room in the building is a candidate for the best of them.
        // The back of house is NOT in here — it goes in below, after both have chosen, for #801's reason.
        foreach (RingRoom room in ring)
        {
            if (room.Side != RingSide.Far)
            {
                rooms.Add((room.X, room.Y, room.Plate));
            }
        }

        List<Amenity> amenities = CarveAmenities(bodyId, level, rooms, walls, shaftX, shaftY, hallSite);
        List<Refuge> refuges = CarveRefuges(bodyId, level, rooms, field);

        // #801 · …and the park's back of house LAST of all, appended after both of those have chosen. They
        // are rooms — they hold what any room down here holds and the A* audit walks to every one of them —
        // but they are the garden's, and an amenity or a refuge carved out of one would be the building
        // taking back the thing this feature exists to give: somewhere on the far side of the green.
        if (park is not null)
        {
            // #813 · …asked of the RING rather than of Park.Rooms, which is the #801 view of it and holds
            // only the ones with a door onto the gravel. The far band's two CORNER rooms stand past the end
            // of the park's own wall, so they have no gravel door and are not back rooms in #801's sense —
            // and reading the narrower list here left two rooms on every block floor with a door cut, a
            // plate hung and nothing behind them. Watched go red: "34 doors were cut and only 28 of them
            // lead anywhere."
            foreach (RingRoom room in ring)
            {
                if (room.Side == RingSide.Far)
                {
                    rooms.Add((room.X, room.Y, room.Plate));
                }
            }
        }

        var centres = new List<(double X, double Y)>(rooms.Count);
        foreach ((double rx, double ry, string _) in rooms)
        {
            centres.Add((rx, ry));
        }

        // #798 · LAST OF ALL, THE BINS — because a bin is fitted into a room that is already finished. It
        // is the only placer in this method that has to see EVERY wall the floor ended up with (its own
        // clearance is measured against them) and every piece of furniture that was laid in it, and a
        // placer that ran earlier would be measuring a room that did not exist yet. It appends walls of its
        // own, which is why nothing below it may read `walls` again.
        List<RipAndBin.Bin> bins = CarveBins(
            bodyId, level, walls, doorways, centres, amenities, refuges, ribList, park, shaftX, shaftY);

        return new FloorPlan(level, NameOf(bodyId, level), HoldsPressure(bodyId, level),
            walls, doorways, locked, labels, centres, ribList, refuges, amenities, ensuites,
            glass, park, bins);
    }

    /// <summary>#585/#751 · How far a rib reaches off the spine, and where its mouth is. ONE function,
    /// because the wall builder, the room builder and now the hall carver all have to be given the same two
    /// numbers — and this was three copies of the same two lines the moment the hall arrived.
    ///
    /// <para>#759 · <paramref name="hall"/> is the ONE rib that reaches further, and it reaches further for
    /// a reason that can be said in a sentence: the room on it is a hall. Owner, standing in the first
    /// one — <i>"it is like cramped… At least double or triple it"</i> — and a hall grown only sideways is a
    /// corridor with tables in it. The extra length comes out of the band beyond the rib ends that nothing
    /// has ever stood in, and the park takes what is left of that band.</para></summary>
    private static (double Mouth, double Far) RibReach(
        in SurfaceLayout.Field field, double shaftY, bool down, bool hall = false)
    {
        double margin = SurfaceLayout.EdgeMargin + 6;
        double reach = hall ? RibReachDu + HallRibExtraDu : RibReachDu;
        return down
            ? (shaftY - CorridorHalf, Math.Max(field.BottomY + margin, shaftY - reach))
            : (shaftY + CorridorHalf, Math.Min(field.LandingBandY - margin, shaftY + reach));
    }

    /// <summary>#585 · How far an ordinary rib runs off the spine. This was a literal <c>52</c> written
    /// twice inside <see cref="RibReach"/>; it is named here because #759 needed to say "further than an
    /// ordinary one" without retyping it.</summary>
    public const double RibReachDu = 52.0;

    /// <summary>#759 · How much further the HALL's own rib runs. The owner's <i>"at least double or triple
    /// it"</i> is a floor-AREA ask and floor area has two axes; this is the second one, and it is spent on
    /// ground the generator has never used — every rib in the building stops <see cref="RibReachDu"/> off
    /// the spine while the field runs on for another sixty du past that.</summary>
    public const double HallRibExtraDu = 16.0;

    /// <summary>#759/#813 · How deep the park is, measured from its own near wall (the hall's glass)
    /// outward. Stated as a number so the park's area law has something to be measured against rather than
    /// a coordinate.
    ///
    /// <para>#813 · It grew by two du when the Manhattan ruling turned the band into a block. That reads
    /// like a rounding error and it is not: the park lost most of its WIDTH — it no longer runs from one end
    /// cap of the spine to the other, because a room that runs to the end caps has ends nobody can build
    /// against — and every du of the depth budget freed by moving the hall's band and the back of house
    /// into one ring went back into the green. What is left of the field beyond the block is the rock the
    /// back street is cut in.</para></summary>
    public const double ParkDepthDu = 45.0;

    /// <summary>#751 · A carved hall, on its way to becoming an <see cref="Amenity"/>.</summary>
    /// <param name="Hall">The box and its cabinets, as published on the plan.</param>
    /// <param name="X">Where the fixture console stands — in front of the SERVING counter, on clear floor,
    /// at the middle of the run it answers over (#791).</param>
    /// <param name="Y">The same.</param>
    /// <param name="Tops">The round tops on the hall floor. Cabinet tops are NOT in here: a cabinet's chairs
    /// are extra, and the hall's own seat law is measured on this list.</param>
    /// <param name="Glass">#759 · The far wall, when it was built as glazing rather than as concrete —
    /// handed on so the park publishes the VERY segment the carve laid rather than a second one written from
    /// the same two corners. Two segments that are equal today and drawn from different arithmetic is the
    /// mirrored-constant bug with a one-line head start.</param>
    private readonly record struct HallSite(
        Hall Hall, double X, double Y, IReadOnlyList<(double X, double Y)> Tops,
        SurfaceLayout.Wall? Glass = null);

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
    /// spacing — and then spread by <see cref="HallSpreadFactor"/>, because a hall is not a canteen with
    /// more chairs in it. A hall on tight ground packs closer than this; it never spreads wider.</summary>
    public static double HallTopPitchDu =>
        Math.Sqrt(RoomWidthDu * RoomHeightDu * HallSpreadFactor / HallTopsPerModule);

    /// <summary>
    /// #759 · HOW MUCH MORE FLOOR A HALL GIVES A TABLE THAN A ROOM DOES — the owner's <i>"the hall is like
    /// cramped … At least double or triple it"</i>, stated as the one number it actually is.
    ///
    /// <para>The first hall was laid at the ordinary canteen's density (<see cref="HallTopsPerModule"/>
    /// tops to a room module) and simply repeated it eighty seats' worth, which is how a room ends up
    /// twenty tables wide and still feeling like a corridor: the crowding a player feels is the PITCH, not
    /// the table count. Three times the floor per top is the whole of the fix, and everything downstream —
    /// how deep the carve asks the ground to be, where the counter's line falls, how far the pillars stand
    /// apart — follows from it without a second number being typed.</para>
    /// </summary>
    public const double HallSpreadFactor = 3.0;

    /// <summary>#751 · The clear strip inside the hall's doors. Nothing is laid in it — a doorway a captain
    /// has to path around a table to use is #585's stranded room with better furniture.</summary>
    public const double HallDoorAisleDu = 4.0;

    // ── #775 · CIRCULATION: PEOPLE-DOORS, SAFETY-DOORS, AND WHERE THE GOODS COME IN ──────────────────────
    //
    // Owner, walking the new B1 the night #790 landed, three complaints in one breath:
    //
    //   (1) "The bar/canteen needs DOORS ON THE MAIN CORRIDOR — today you have to really look for the way
    //       in; a venue's entrance should find YOU."
    //   (2) "A canteen this size would have MORE THAN TWO DOORS just for safety reasons — egress is a code,
    //       and the base was built by people who file paperwork about codes."
    //   (3) "The facility needs FREIGHT ACCESS somewhere — a freight elevator or a long drive-in ramp for
    //       supplies; eighty seats of food and twelve beds of produce do not arrive through a personnel
    //       door."
    //
    // All three are one law said three times — THE LAYOUT MATCHES ITS FUNCTION — and all three are about
    // circulation rather than about rooms: people-doors where people come from, safety-doors because rules,
    // freight where freight goes.
    //
    // WHY THE ROOM HAD NO FRONT DOOR IN THE FIRST PLACE, because it is not an oversight anybody could have
    // seen from the plan. The hall's near wall IS the spine's own face (#751: "the front wall and the near
    // wall are not built at all — they are the rib's face and the spine's"), and the spine's faces were
    // poured BEFORE anybody knew where the hall was going to stand. So the one wall a walker on the main
    // corridor actually meets was the one wall the hall was structurally incapable of cutting. The carve
    // hands the wall builder a list of spans now, and the wall builder runs after it.

    /// <summary>#775 · HOW MUCH FLOOR ONE REQUIRED EGRESS DOOR COVERS. The one number the code-mandated
    /// door count is derived from, so nothing anywhere types how many doors a hall has.
    ///
    /// <para>Not a guess about du: the shipped halls run 2 100 – 7 300 du² and a du is about a metre of the
    /// deck the captain walks, which puts the biggest of them at the floor area of a real assembly hall.
    /// One way out per fifteen hundred of those is a conservative reading of every occupancy code anybody
    /// ever filed, and the people who built this place filed all of them.</para></summary>
    public const double HallDu2PerEgressDoor = 1500.0;

    /// <summary>#775 · THE FLOOR UNDER THE EGRESS COUNT — the owner's "more than two doors" as an integer.
    /// A room with two ways out has one way out the day one of them is where the fire is.</summary>
    public const int HallMinDoors = 3;

    /// <summary>#775 · HOW MANY DOORS A HALL OF THIS FLOOR AREA MUST HAVE. Published, and asked by BOTH the
    /// carve and the guard — a second opinion about a door count is the mirrored-constant bug with a whole
    /// building to be wrong in.</summary>
    public static int HallEgressDoors(double floorDu2) => Math.Max(
        HallMinDoors, (int)Math.Ceiling(floorDu2 / HallDu2PerEgressDoor));

    /// <summary>#775 · THE FRONT DOOR GOES WHERE THE WALKER IS, and this is how near it may be allowed to
    /// get to the corners of the room it is cut into: a door's own half-width, and a little.</summary>
    public const double HallSpineDoorEdgeDu = DoorHalf + 2.0;

    /// <summary>#775 · How wide the goods hoist's car is. Twelve du by the counter band's own five — the
    /// footprint of something a pallet goes into, parked in the one part of the room the customer never
    /// stands in.</summary>
    public const double FreightCarWidthDu = 12.0;

    /// <summary>
    /// #775 · THE GOODS HOIST — freight access, on the deck, drawn and collidable, and shut.
    ///
    /// <para>Owner: <i>"eighty seats of food and twelve beds of produce do not arrive through a personnel
    /// door."</i> It is parked in the counter's own service band — the band #751 closed off because it is
    /// the one part of a bar the customer never stands in — at the end of that band nearest the park's
    /// gate, which is the end the produce comes in by. Behind it, through the glass, are the beds it exists
    /// to carry; in front of it is the hall floor it feeds. One fixture, both jobs, no sentence needed.</para>
    ///
    /// <para><b>The captain cannot ride it, and is TOLD so.</b> The shutter is a
    /// <see cref="LockedDoor"/> — the building's own grammar for a door that will not open: drawn shut, a
    /// wall poured behind it, and its plate is exactly what [E] reads. Nothing here simulates freight and
    /// nothing pretends to; the fixture exists, it is labelled, a body stops at it, and the refusal is a
    /// sentence rather than an absence (#757's lesson about what a player cannot read).</para>
    /// </summary>
    /// <param name="X0">The car's box, min/max normalised where it is carved.</param>
    /// <param name="Shutter">The roller door in the counter's line, hall side.</param>
    /// <param name="PlateX">Where the plate is read from — on the HALL floor, in front of the shutter.</param>
    public readonly record struct FreightLift(
        double X0, double Y0, double X1, double Y1,
        SurfaceLayout.Doorway Shutter, double PlateX, double PlateY, string Plate)
    {
        /// <summary>Is this spot inside the car? The box the walls were laid on, and nothing else — the
        /// hall's own law (<see cref="Hall.Contains"/>).</summary>
        public bool Contains(double x, double y) => x >= X0 && x <= X1 && y >= Y0 && y <= Y1;
    }

    /// <summary>#775 · What is stencilled on the goods hoist's shutter, and therefore what the captain is
    /// told when they press it. The bureaucracy's own register: a number, a window, and whose side of the
    /// shutter you are on — no explanation, no apology, and not one word about the building.
    ///
    /// <para>It carries no glyph of its own because the sign console already hangs the 🔒 on it, which is
    /// the building's grammar for a door that will not open (#585). Two glyphs on one plate is a plate
    /// nobody reads.</para></summary>
    public const string FreightPlate =
        "GOODS HOIST 1 · DELIVERIES 04:00–06:00 · CREW SIDE ONLY";

    /// <summary>#775 · What is painted on the floor in front of it, so the fixture reads as a fixture from
    /// across the room rather than as one more shut door in a building full of them.</summary>
    public const string FreightSign = "🚛 GOODS HOIST 1";

    /// <summary>#775 · What is painted beside the hall's main front door, the one nearest the lift. The
    /// venue announcing ITSELF on the corridor a walker is already on — the owner's <i>"a venue's entrance
    /// should find YOU"</i>, which a plate reading only ENTRANCE would not do: a door labelled "the way in"
    /// says nothing about what it is the way in TO.
    ///
    /// <para>Composed from the room's OWN sign rather than spelled a second time, and cut at its first
    /// separator: <see cref="AmenitySigns"/> writes the venue, then who it is for, then the pass rule, and
    /// only the first of those three belongs on a corridor at walking pace. So the bar's front door says
    /// <c>🍸 CANTEEN 1 · ENTRANCE</c> and the staff mess's says <c>🍽 CANTEEN 2 · ENTRANCE</c>, and the day
    /// either room is renamed both plates follow without anybody remembering this one exists.</para></summary>
    public static string HallEntrancePlate(string bodyId, Comfort use)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        string plate = AmenitySigns(bodyId, use).Plate;
        int cut = plate.IndexOf(" · ", StringComparison.Ordinal);
        return $"{(cut < 0 ? plate : plate[..cut])} · ENTRANCE";
    }

    /// <summary>#775 · What is painted beside the hall's OTHER front doors. They exist because a code says
    /// a room this size has them, so they say what a code-mandated door says and nothing else — and the
    /// number is the one thing that makes a corridor of them read as a building rather than as a wall with
    /// holes in it.</summary>
    public static string HallEgressPlate(int number) =>
        $"⇥ EXIT {number} · KEEP CLEAR";

    /// <summary>#751 · How deep the cabinets run off the hall's outer wall.</summary>
    public const double HallCabinetDepthDu = 10.0;

    /// <summary>#751 · The band at the hall's far wall that THE COUNTER and its service side own — the one
    /// part of a bar the customer never stands in, closed off exactly the way it would be (#707).</summary>
    public const double HallCounterBandDu = 5.0;

    /// <summary>#751 · How much of a hall's own edge is left clear of furniture.</summary>
    public const double HallEdgePadDu = 2.0;

    /// <summary>
    /// #792 · How far out from the counter's line the row of stools stands — a stool's depth plus room for
    /// a pair of knees, inside <see cref="HallCounterBandDu"/>'s own band so the seats never land on a top.
    ///
    /// <para>Named, and not typed into the loop that lays them: the counter band, the tops' grid and this
    /// standoff are three numbers that have to keep out of each other's way, and a literal in the middle of
    /// the three is how a chair ends up inside a table.</para>
    /// </summary>
    public const double HallStoolStandoffDu = 1.6;

    /// <summary>
    /// #791 · How far out from the desk's line A SERVED CUSTOMER STANDS — the line the service run is laid
    /// along, and therefore the line the [E] bus answers from.
    ///
    /// <para>It is a hair further out than <see cref="HallStoolStandoffDu"/> on purpose: the stools are
    /// bolted between it and the desk, so the standing customer is behind the row rather than inside it,
    /// which is where a person stands at a bar with stools at it.</para>
    ///
    /// <para>Named rather than left as the <see cref="HallEdgePadDu"/> it used to borrow. The two happen to
    /// be the same number today and they are not the same QUANTITY — one is how much of a room's edge is
    /// kept clear of furniture, the other is how far a body stands off a counter — and a mirrored constant
    /// is the second-named bug class in this file's own table.</para>
    /// </summary>
    public const double HallServiceStandoffDu = 2.0;

    /// <summary>
    /// #751 · WHICH COLUMN THE HALL STANDS ON — the same criterion #707 already uses for the canteen, asked
    /// one step earlier.
    ///
    /// <para>"Nearest the car, every time": a building puts its catering by the lift, and a bar you have to
    /// go looking for is not a bar anybody drank in on a shift. The old carve chose the nearest ROOM out of
    /// the rooms the floor had built; a hall has to be chosen before any room exists, so this asks the same
    /// question of the room SLOTS — the very positions <see cref="RoomCentresAlong"/> is about to place. Same
    /// answer, computed from the same arithmetic, one pass earlier.</para>
    ///
    /// <para>#759 · …<b>of the slots that can actually hold one.</b> Nearest-the-car on its own put every
    /// hall in the game on the rib beside the shaft, and on the floors whose rib runs UP that is the one
    /// column in the building with the lift alcove standing in front of it — so the room the owner called
    /// cramped was cramped by a fixture thirty du away, and no amount of spreading its tables could have
    /// answered him. The ground is asked FIRST now (<see cref="HallGround"/>, the same arithmetic the carve
    /// itself uses, so the two can never disagree), the best floor wins, and nearest-the-car breaks the tie
    /// — which it does on every floor where two slots would both take the hall whole, i.e. the rule is
    /// unchanged everywhere it was ever doing any work.</para>
    /// </summary>
    private static (int Rib, int Side)? HallSlotFor(
        string bodyId, int level, List<Rib> ribs, in SurfaceLayout.Field field,
        double shaftX, double? serviceX, double shaftY, double leftEnd, double rightEnd, double roomScale)
    {
        if (!IsHallFloor(bodyId, level) || ribs.Count == 0)
        {
            return null;
        }

        Comfort use = HallUseOn(bodyId, level);
        double roomW = RoomWidthDu * roomScale;
        (int Rib, int Side)? best = null;
        double bestFloor = -1, bestD2 = double.MaxValue;

        for (int i = 0; i < ribs.Count; i++)
        {
            (double mouth, double far) = RibReach(field, shaftY, ribs[i].Down, hall: true);
            List<double> ys = RoomCentresAlong(mouth, far, ribs[i].Down, roomScale);
            if (ys.Count == 0)
            {
                continue;
            }
            for (int side = -1; side <= 1; side += 2)
            {
                if (HallGround(
                        bodyId, use, ribs, i, side, mouth, far, shaftX, serviceX, leftEnd, rightEnd,
                        roomScale)
                    is not { } ground)
                {
                    continue;   // the ground here would not take a hall at all
                }

                // The floor this slot would yield, and never more than the hall ASKED for — so two slots
                // that both take it whole are equal and the tie falls to the lift, which is the #751 rule.
                double floor = Math.Min(ground.Width, ground.Wanted) * ground.Length;

                double cx = ribs[i].X + (side * (CorridorHalf + (roomW / 2)));
                double d2 = double.MaxValue;
                foreach (double cy in ys)
                {
                    double dx = cx - shaftX, dy = cy - shaftY;
                    d2 = Math.Min(d2, (dx * dx) + (dy * dy));
                }

                if (floor > bestFloor + 0.5 || (floor > bestFloor - 0.5 && d2 < bestD2))
                {
                    (best, bestFloor, bestD2) = ((i, side), Math.Max(floor, bestFloor), d2);
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
    /// #759 · HOW MUCH GROUND A SLOT HAS FOR A HALL, AND HOW MUCH THE HALL WANTS — the arithmetic
    /// <see cref="CarveHall"/> used to do inline, lifted out whole so <see cref="HallSlotFor"/> can ask the
    /// same question one pass earlier.
    ///
    /// <para>It is lifted rather than copied for the reason this file opens with a table of: a chooser that
    /// worked out available width on its own would be a second opinion about a room the carve owns, and the
    /// two would drift apart the first time either grew a clause. Null means this ground will not take a
    /// hall at all.</para>
    /// </summary>
    private static (double Width, double Wanted, double Length, double VSpan)? HallGround(
        string bodyId, Comfort use, List<Rib> ribs, int ribIndex, int side,
        double mouth, double far, double shaftX, double? serviceX, double leftEnd, double rightEnd,
        double roomScale)
    {
        double ribX = ribs[ribIndex].X;
        double roomW = RoomWidthDu * roomScale;

        // ── HOW FAR OUT THE GROUND GOES. Clamped against the things that are already spoken for rather
        //    than against a guess: the next rib's chambers, the lift alcove where the hall shares a spine
        //    face with it, and the spine's own end cap.
        //
        // #759 · …and a rib pointing the OTHER WAY off the spine is not in the way of anything. This used
        // to reserve a full room column beside EVERY neighbouring rib, which on half the floors in the game
        // was ground held back for chambers standing on the far side of the spine — sixty du of rock the
        // hall was refused because of rooms it could not have reached with a drill. The clamp asks which
        // way the neighbour runs now, and against a neighbour that shares this band it stops at the
        // CORRIDOR rather than at the far side of that corridor's rooms: those room slots are ground, the
        // claim ledger drops what stands on the hall, and a passage is the one thing that may never be
        // covered.
        double limit = side > 0 ? rightEnd - HallEdgePadDu : leftEnd + HallEdgePadDu;
        foreach (Rib other in ribs)
        {
            if (other.Down != ribs[ribIndex].Down)
            {
                continue;
            }
            if (side > 0 && other.X > ribX)
            {
                limit = Math.Min(limit, other.X - CorridorHalf - 1.5);
            }
            else if (side < 0 && other.X < ribX)
            {
                limit = Math.Max(limit, other.X + CorridorHalf + 1.5);
            }
        }
        // The lift alcove hangs off the TOP face, so only a rib that runs UP can meet it. #585 was a wall
        // lying across a mouth; a hall laid over the alcove would be the same mistake with the captain's own
        // way home inside it.
        //
        // #759 · …and only where the alcove is actually in the way, which is the clause this shipped
        // without. A hall growing LEFT off a rib that is already left of the shaft was being clamped to a
        // limit on the shaft's far side — a lower bound raised above the wall it was bounding, so
        // `available` came out as the distance to a point behind the hall and the room was laid seventy du
        // outside the field. It never fired while every hall in the game stood on the rib beside the car;
        // the moment #759 let the chooser look at the other seven slots, it laid a bar through the edge of
        // the world. A clamp that does not ask which side its obstacle is on is not a clamp.
        if (!ribs[ribIndex].Down)
        {
            if (side > 0 && shaftX > ribX)
            {
                limit = Math.Min(limit, shaftX - ShaftHalf - 1.5);
            }
            else if (side < 0 && shaftX < ribX)
            {
                limit = Math.Max(limit, shaftX + ShaftHalf + 1.5);
            }
        }

        // #801 · …and the GOODS CAR's alcove, which hangs off the LOWER face, so it is the ribs running DOWN
        // that can meet it. The same two lines the other way up — stated as its own clause rather than as a
        // loop over a list of cars, because the clause a car needs is WHICH FACE it is on, and a list that
        // had lost that would be a clamp that does not ask which side its obstacle is on.
        if (ribs[ribIndex].Down && serviceX is { } carX)
        {
            if (side > 0 && carX > ribX)
            {
                limit = Math.Min(limit, carX - ShaftHalf - 1.5);
            }
            else if (side < 0 && carX < ribX)
            {
                limit = Math.Max(limit, carX + ShaftHalf + 1.5);
            }
        }

        double faceX = ribX + (side * CorridorHalf);
        double available = Math.Abs(limit - faceX);
        double length = Math.Abs(far - mouth);

        // ── WHAT THE HALL NEEDS. The tops come first: the seat target decides the bill, the bill decides
        //    how many tops, and the tops decide how deep the room has to be at the pitch a hall lays its
        //    tables out at (HallSpreadFactor — the owner's "at least double or triple it").
        // …and the cabinets are the cantina's alone. The mess is the room the shift stopped coming to; a
        // door for sensitive negotiations in it would be furnishing a joke nobody is in the room to make.
        double cabBand = use == Comfort.UpperCanteen ? HallCabinetDepthDu : 0.0;

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
        return width < minWidth || vSpan < 4 * DoorHalf ? null : (available, wanted, length, vSpan);
    }

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
        List<SurfaceLayout.Wall> walls, List<SurfaceLayout.Wall> glass,
        List<(double Lo, double Hi)> spineCuts,
        string bodyId, int level, List<Rib> ribs, int ribIndex, int side,
        double mouth, double far, double shaftX, double? serviceX, double leftEnd, double rightEnd,
        double roomScale, bool glazed)
    {
        Comfort use = HallUseOn(bodyId, level);

        if (HallGround(
                bodyId, use, ribs, ribIndex, side, mouth, far, shaftX, serviceX, leftEnd, rightEnd,
                roomScale)
            is not { } ground)
        {
            return null;
        }

        double ribX = ribs[ribIndex].X;
        bool cabs = use == Comfort.UpperCanteen;
        double cabBand = cabs ? HallCabinetDepthDu : 0.0;
        double pitch = HallTopPitchDu;
        int tops = HallSeatBill(bodyId, use, HallSeatsFor(bodyId, use)).Count;
        double faceX = ribX + (side * CorridorHalf);
        double length = ground.Length;
        double width = Math.Min(ground.Width, ground.Wanted);

        // ── FROM (u, v) TO THE FIELD'S OWN COORDINATES, in one place. ───────────────────────────────────
        bool down = ribs[ribIndex].Down;
        double X(double u) => faceX + (side * u);
        double Y(double v) => down ? mouth - v : mouth + v;

        double x0 = Math.Min(X(0), X(width)), x1 = Math.Max(X(0), X(width));
        double y0 = Math.Min(Y(0), Y(length)), y1 = Math.Max(Y(0), Y(length));

        // ── THE THREE WALLS THE HALL OWNS. (The fourth and fifth are the rib's face and the spine's.)
        walls.Add(new(X(width), Y(0), X(width), Y(length), true));        // the outer wall

        // ── #759 · THE FAR WALL, WHICH IS GLASS WHERE THERE IS A PARK BEHIND IT ─────────────────────────
        //
        // Owner requirement, pinned: the restaurant scene must have a) A VIEW TO THE PARK and b) A WINDOW
        // WALL BETWEEN. Both of the room's own pictures are shot through it — the stool view is counter,
        // glass, green; the hall's own establishing art is steel tables, riveted glass, green — so the deck
        // plan drawing an ordinary poured wall there would be the drawn room and the pictured room
        // disagreeing about the one surface both of them are about.
        //
        // ONE SEGMENT, PUBLISHED TWICE AND BUILT ONCE. It goes in the `glass` list rather than the wall
        // list, and the client puts it back into the deck as a wall that draws in the window idiom — so it
        // collides exactly like the poured wall it replaced (a body may not pass) and reads as glass (an eye
        // may). A second segment laid on the same line would be the drawn-versus-simulated split this house
        // has a name for.
        var farWall = new SurfaceLayout.Wall(X(0), Y(length), X(width), Y(length), true);
        (glazed ? glass : walls).Add(farWall);

        // ── THE COUNTER · a long bar wall along the far end, with the service side shut off behind it.
        double counterV = length - HallCounterBandDu;
        double counterU0 = HallDoorAisleDu;
        double counterU1 = width - cabBand - HallEdgePadDu;

        // ── #775 · AND THE GOODS HOIST, PARKED AT ONE END OF THAT BAND ─────────────────────────────────
        //
        // The band is already a sealed strip the customer never stands in, five du deep and the length of
        // the bar. The hoist is the first twelve du of it, divided off by one wall — so the car's four
        // sides are the band's end cap, that new divider, the room's own far wall behind it, and the
        // shutter in the counter's line in front. Four walls, one of them new: freight access is a fixture
        // in a room somebody already carved, which is the cheapest honest version of the owner's ask.
        //
        // WHICH END. The one nearest u = 0, which on the hall's rib is the end the park's gate is on — the
        // beds' produce comes in the gate at the far end of the corridor and goes straight into the hoist,
        // and the counter that serves it is on the other side of the same divider. Nothing says any of
        // that; the geometry is the sentence.
        //
        // …and it is only fitted where BOTH halves of the band survive it: a car narrower than a doorway is
        // not a freight lift, and a bar left with less counter than that is not a bar. Every hall the game
        // ships clears both by a wide margin; the clause is here so the day one does not, the floor says so
        // by having no hoist rather than by having a serving hatch.
        double hoistU1 = Math.Min(counterU0 + FreightCarWidthDu, counterU1);
        bool hoisted = hoistU1 - counterU0 >= 2 * DoorHalf && counterU1 - hoistU1 >= 2 * DoorHalf;
        double serveU0 = hoisted ? hoistU1 : counterU0;

        walls.Add(new(X(serveU0), Y(counterV), X(counterU1), Y(counterV), true));
        walls.Add(new(X(counterU0), Y(counterV), X(counterU0), Y(length), true));
        walls.Add(new(X(counterU1), Y(counterV), X(counterU1), Y(length), true));

        FreightLift? freight = null;
        if (hoisted)
        {
            walls.Add(new(X(hoistU1), Y(counterV), X(hoistU1), Y(length), true));   // the car's divider
            freight = new FreightLift(
                Math.Min(X(counterU0), X(hoistU1)), Math.Min(Y(counterV), Y(length)),
                Math.Max(X(counterU0), X(hoistU1)), Math.Max(Y(counterV), Y(length)),
                new SurfaceLayout.Doorway(X(counterU0), Y(counterV), X(hoistU1), Y(counterV)),
                X((counterU0 + hoistU1) / 2.0), Y(counterV - HallEdgePadDu),
                FreightPlate);
        }

        // #780 · …and THE PICTURE OF IT, over the same three walls' own box. Owner: "see how in the space
        // bars we have the image of bar desk at the spot where the bar desk is." Built HERE, out of the very
        // (u, v) the segments above were built from, so the frame and the furniture cannot drift apart —
        // the alternative was a renderer measuring a counter it did not carve, which is the mistake that has
        // set this project's captain down inside a wall twice. Only where a counter actually serves: the
        // washroom has no bar, and a picture of one over a room's back wall would be the game saying
        // something about that room that is not true.
        //
        // #775 · …and it starts where the SERVING counter starts, which since the goods hoist took the end
        // of the band is not where the band starts. A bar-desk photograph stretched over the hoist's own
        // twelve du would be a picture of a counter drawn across a freight car — the drawn room and the
        // carved room disagreeing about one wall, which is precisely the split #759 kept the glass out of.
        var spots = new List<SpotArt>(1);
        if (CounterArtFor(bodyId, use) is { } deskArt)
        {
            spots.Add(new SpotArt(
                deskArt,
                Math.Min(X(serveU0), X(counterU1)), Math.Min(Y(counterV), Y(length)),
                Math.Max(X(serveU0), X(counterU1)), Math.Max(Y(counterV), Y(length))));
        }

        // ── #792 · AND THE STOOLS ALONG THE FRONT OF IT ────────────────────────────────────────────────
        //
        // Owner, playtest 2026-08-08: "people looking to sit down look at those like hungry wild beasts
        // look at their prey… Now I have trouble finding a free table." The row has existed in Core since
        // #756 — eight seats, occupied or not, watch by watch — and has never been anywhere on the floor,
        // so a captain could be told the row was full only by walking up and pressing.
        //
        // WHERE, out of the very (u, v) the three counter segments above were built from, exactly as the
        // desk picture is. The order is the row's own: entry s is stool s, and it has to be, because that
        // ordinal is what Interior.TheStools.Taken answers about — a row published in some other order
        // would draw one seat's occupancy over another seat, which is this project's drawn-versus-simulated
        // class with somebody sitting in it.
        //
        // AND ONLY WHERE A COUNTER SERVES. Asked of Interior.CounterService, which is the same call the
        // stool verb itself is gated on, rather than restated here as "upper canteen and not the head
        // office" — two spellings of one condition is how a room grows eight seats nothing will ever sit on.
        //
        // #791 · …AND ALONG THE SERVING DESK, which is not the whole band. This laid the row from counterU0
        // — the start of the band — so on every hall in the game the first stool and part of the second
        // stood in front of the GOODS HOIST'S SHUTTER (#775 took the first twelve du of that band for a
        // freight car). Two tall seats at a roller door, in a room whose own photograph starts twelve du
        // further along: the drawn desk and the seated row disagreeing about where the bar is. They run the
        // service run's length now, which is the picture's length, which is the desk's length.
        var stools = new List<(double X, double Y)>(Interior.TheStools.Count);
        bool serves = Interior.CounterService.For(bodyId, use) is not null;
        if (serves)
        {
            double stoolV = counterV - HallStoolStandoffDu;
            double stoolSpan = counterU1 - serveU0;
            for (int s = 0; s < Interior.TheStools.Count; s++)
            {
                double u = serveU0 + (stoolSpan * ((s + 0.5) / Interior.TheStools.Count));
                stools.Add((X(u), Y(stoolV)));
            }
        }

        // ── #791 · AND THE RUN THE WHOLE DESK SERVES OVER ──────────────────────────────────────────────
        //
        // Owner, live: "there is only one spot to get service on it… we would need an E-bus of the bar desk
        // length instead of one bar keep cashier at a single spot."
        //
        // Same (u, v) as the wall segments, the photograph and the stools — a fourth author measuring this
        // desk is exactly the mistake the three above are written the way they are to avoid. serveU0 to
        // counterU1 is the SERVING desk; the standoff puts the line on the hall side of the counter, where a
        // customer stands; and the keep's own spot is the middle of the sealed band behind it, which is the
        // side of the desk a keep has stood on since bars were invented and the side #781 will arrive on.
        //
        // Only where the counter serves, off the same one call the stools ask. A run published for a desk
        // nobody serves at would be an [E] bus to a card that does not exist.
        double serveMidU = (serveU0 + counterU1) / 2.0;
        double serviceV = counterV - HallServiceStandoffDu;
        ServiceRun? service = serves
            ? new ServiceRun(
                X(serveU0), Y(serviceV), X(counterU1), Y(serviceV),
                X(serveMidU), Y(counterV + (HallCounterBandDu / 2.0)))
            : null;

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

        // ── #775 · THE DOORS · WHAT THE ROOM ALREADY HAD, AND WHAT A CODE SAYS IT MUST HAVE ─────────────
        //
        // WHAT IT ALREADY HAD, asked of the function that cut them rather than counted off the wall. The
        // hall never opened a door in the rib's face — RibFace leaves a gap at every room slot on that
        // column and those gaps ARE the hall's doors (#751). RoomCentresAlong is the one function that says
        // where a slot is, and it is called here for the same reason the wall builder and the room builder
        // both call it: a second answer about where a door is, is this file's oldest and most expensive bug.
        var doors = new List<SurfaceLayout.Doorway>();
        foreach (double cy in RoomCentresAlong(mouth, far, down, roomScale))
        {
            doors.Add(new SurfaceLayout.Doorway(faceX, cy - DoorHalf, faceX, cy + DoorHalf));
        }

        // WHAT THE CODE SAYS. The count comes off the room's own published floor (HallEgressDoors) and the
        // shortfall is cut into the spine's face — never fewer than one, because the owner's first
        // complaint is not about arithmetic: a venue with no door on the main corridor has no entrance at
        // all, whatever the total says.
        double floorDu2 = (x1 - x0) * (y1 - y0);
        int wantSpine = Math.Max(1, HallEgressDoors(floorDu2) - doors.Count);

        // WHERE THEY GO, in the hall's own u. Clear of both corners, and clear of the cabinet band — a
        // front door opening into the back of a negotiating cabinet is #585's stranded room told from the
        // corridor side.
        double duLo = HallSpineDoorEdgeDu;
        double duHi = width - cabBand - HallSpineDoorEdgeDu;

        // THE FIRST ONE IS THE ENTRANCE, AND IT GOES WHERE THE WALKER IS. #751 already puts the hall on the
        // column nearest the car; this is that same rule one step further in, and it is the whole of the
        // owner's "a venue's entrance should find YOU": the captain steps out of the lift onto the spine,
        // turns, and the door is the nearest thing on the wall.
        var atU = new List<double>();
        if (duHi > duLo)
        {
            double toShaft = side * (shaftX - faceX);   // the shaft, in this hall's own outward axis
            atU.Add(Math.Clamp(toShaft, duLo, duHi));

            // THE REST ARE SPREAD, each one put as far from every door already placed as the wall allows —
            // which is what a fire officer means by spread, and what a fixed pitch stops meaning the moment
            // the entrance is not in the middle. Placed only while they can still be a door's width and a
            // half apart: two exits sharing a jamb are one exit with a thick frame.
            const int samples = 240;
            while (atU.Count < wantSpine)
            {
                double best = duLo, bestGap = -1;
                for (int s = 0; s <= samples; s++)
                {
                    double u = duLo + ((duHi - duLo) * s / samples);
                    double gap = double.MaxValue;
                    foreach (double placed in atU)
                    {
                        gap = Math.Min(gap, Math.Abs(placed - u));
                    }
                    if (gap > bestGap)
                    {
                        (bestGap, best) = (gap, u);
                    }
                }
                if (bestGap < 3 * DoorHalf)
                {
                    break;   // the wall has run out of room. The guard says so out loud rather than here.
                }
                atU.Add(best);
            }
        }

        foreach (double u in atU)
        {
            double dx0 = Math.Min(X(u - DoorHalf), X(u + DoorHalf));
            double dx1 = Math.Max(X(u - DoorHalf), X(u + DoorHalf));
            spineCuts.Add((dx0, dx1));
            doors.Add(new SurfaceLayout.Doorway(dx0, Y(0), dx1, Y(0)));
        }

        // The board hangs half-way down the door wall and the plate reads a quarter of the way along it, so
        // neither crowds the other and both are things you meet on the way in rather than across the room.
        return new HallSite(
            new Hall(
                x0, y0, x1, y1, HallSeatsFor(bodyId, use), cabinets,
                X(HallDoorAisleDu / 2.0), Y(length / 2.0),
                X(HallDoorAisleDu / 2.0), Y(length * 0.25),
                HallArtFor(bodyId, use), spots, stools, doors, freight, service),

            // #791 · THE FIXTURE'S OWN SPOT is the MIDDLE OF THE DESK THAT SERVES, and not the middle of
            // the band. It used to be (uLo + uHi) / 2 — the mid-point of the counter's whole length
            // including the goods hoist's twelve du — so the one plate, the one console dot and the spot
            // ?counter=1 sets a tester down on all sat six du off centre, toward a freight shutter. The run
            // knows where its own middle is; nothing here works it out a second time.
            service?.MidX ?? X((uLo + uHi) / 2.0),
            service?.MidY ?? Y(counterV - HallServiceStandoffDu),
            laid,
            glazed ? farWall : null);
    }

    /// <summary>#813 · How a band of ring frontage is cut into rooms — one answer, asked by the near band,
    /// the far band, the two ends, and by the chooser that decides which sub-segment the hall stands in.
    ///
    /// <para>The gates cut the band into segments; the park's own two ends cut it again, because a room that
    /// straddled the corner would have glass along part of its front wall and rock along the rest, and a
    /// wall that is two materials is a wall two placers will one day disagree about.</para></summary>
    private static List<(double Lo, double Hi)> RingSegments(
        double lo, double hi, IReadOnlyList<double> gapCentres, double half, params double[] splits)
    {
        var cuts = new List<(double Lo, double Hi)>(gapCentres.Count);
        foreach (double c in gapCentres)
        {
            cuts.Add((c - half, c + half));
        }
        cuts.Sort((a, b) => a.Lo.CompareTo(b.Lo));

        var spans = new List<(double Lo, double Hi)>();
        double cursor = lo;
        foreach ((double glo, double ghi) in cuts)
        {
            if (glo > cursor)
            {
                spans.Add((cursor, glo));
            }
            cursor = Math.Max(cursor, ghi);
        }
        if (cursor < hi)
        {
            spans.Add((cursor, hi));
        }

        var cut = new List<(double Lo, double Hi)>(spans.Count + splits.Length);
        foreach ((double slo, double shi) in spans)
        {
            double at = slo;
            foreach (double s in splits)
            {
                if (s > at + 0.001 && s < shi - 0.001)
                {
                    cut.Add((at, s));
                    at = s;
                }
            }
            cut.Add((at, shi));
        }
        return cut;
    }

    /// <summary>#813 · The near band's sub-segments — the run of ground between the spine and the park's own
    /// near wall, once the gates and the park's two ends have been taken out of it. Field-pure, and lifted
    /// out whole so the chooser that puts the hall in one of them and the carve that fills the rest are
    /// reading the SAME list rather than two copies of the same arithmetic (§13.15).</summary>
    public static IReadOnlyList<(double Lo, double Hi)> RingNearSegments(in ParkBlock block) =>
        RingSegments(
            block.WestInnerX, block.EastInnerX, block.SpurXs, CorridorHalf, block.X0, block.X1);

    /// <summary>#813 · How many rooms a run of frontage this long is cut into, and each of them the same
    /// width. Never fewer than one and never so many that one of them is under
    /// <see cref="RingRoomMinDu"/>.</summary>
    private static int RingRoomsIn(double span)
    {
        int n = Math.Max(1, (int)Math.Round(span / RingRoomTargetDu, MidpointRounding.AwayFromZero));
        while (n > 1 && span / n < RingRoomMinDu)
        {
            n--;
        }
        return n;
    }

    /// <summary>
    /// #813 · THE RING, CARVED — every room that faces the park, the four streets that serve them, and the
    /// six gates through them.
    ///
    /// <para>It owns the park's whole boundary, which is the one thing that makes the Manhattan ruling
    /// provable rather than decorative: every du of the park's four walls is laid HERE, either as a room's
    /// glass or as a gate's stub, so "no side of the park is wasted" is true by construction and not by an
    /// arrangement two placers happen to agree on. The single exception is the hall's own glass, which
    /// <see cref="CarveHall"/> laid before this ran, and which this therefore steps over — #585's one-gap
    /// law, said about a wall with two authors.</para>
    ///
    /// <para>Laid in one order, near then far then the two ends, so a room's <see cref="RingRoom.Number"/>
    /// is a fact about where it is rather than about when the loop got to it.</para>
    /// </summary>
    private static List<RingRoom> CarveRing(
        List<SurfaceLayout.Wall> walls, List<SurfaceLayout.Wall> glass,
        List<SurfaceLayout.Doorway> doorways, List<SurfaceLayout.Landmark> labels,
        List<(double X0, double Y0, double X1, double Y1)> claimed,
        List<(double Lo, double Hi, double PlateX, string Plate)> spineDoors,
        List<(double Lo, double Hi)> spineMouths,
        List<SurfaceLayout.Doorway> gates,
        string bodyId, int level, in ParkBlock block, in Hall hall)
    {
        bool found = IsFound(bodyId, level);
        var ring = new List<RingRoom>();
        double sf = block.SpineFaceY;

        // ── THE SHELL · three walls, and the fourth side is the spine's own face.
        walls.Add(new(block.WestOuterX, sf, block.WestOuterX, block.BackStreetY0, true));
        walls.Add(new(block.EastOuterX, sf, block.EastOuterX, block.BackStreetY0, true));
        walls.Add(new(block.WestOuterX, block.BackStreetY0, block.EastOuterX, block.BackStreetY0, true));
        claimed.Add((block.WestOuterX - 1.5, block.BackStreetY0 - 1.5, block.EastOuterX + 1.5, sf + 1.5));

        // ── THE MOUTHS ON THE SPINE · the two streets and every gate down the near band. Corridor-width,
        //    and in the mouths list rather than the doors list, because a corridor mouth gets no leaf and no
        //    plate — exactly as a rib's mouth never has.
        spineMouths.Add((block.WestOuterX, block.WestInnerX));
        spineMouths.Add((block.EastInnerX, block.EastOuterX));
        foreach (double sx in block.SpurXs)
        {
            spineMouths.Add((sx - CorridorHalf, sx + CorridorHalf));
        }

        // ── (1) THE NEAR BAND · the premium suites, doors on the spine, glass on the green.
        foreach ((double lo, double hi) in RingNearSegments(block))
        {
            // The hall stands in one of these and it stands in the whole of it (see Build). Nothing else is
            // laid on that ground: the hall published its own glass and its own front doors already.
            if (hall.X1 > lo + 0.001 && hall.X0 < hi - 0.001)
            {
                continue;
            }
            int n = RingRoomsIn(hi - lo);
            for (int k = 0; k < n; k++)
            {
                double rx0 = lo + ((hi - lo) * k / n), rx1 = lo + ((hi - lo) * (k + 1) / n);

                // #817 · AND A DOOR ONTO THE GREEN. Owner's ruling, live in one of these: a premium office
                // that sold on the aspect gets a way OUT into the garden, not only a window at it. The far
                // band's back of house has had exactly this door since #801 and it is the same door.
                //
                // The hall's glass is untouched and always was: the segment the hall stands in is stepped
                // over above, and #751's "the glass between the bar and the green is never a door" was
                // always a rule about the BAR's window wall — a public room with eighty seats in it, whose
                // egress is its own front doors on the spine. RingBox cuts one only where there is park in
                // front of the room to cut it into, so the block's corner offices still have none.
                ring.Add(RingBox(
                    walls, glass, doorways, labels, claimed, spineDoors, bodyId, level, found,
                    ring.Count + 1, RingSide.Near, rx0, block.Y1, rx1, sf, block, gate: true));
            }
        }

        // ── (2) THE FAR BAND · #801's back of house, re-anchored as ring fabric. It keeps the door onto the
        //    gravel that made it worth walking across a garden for, and it GAINS the street door the
        //    Manhattan ruling requires — the owner's "nobody walks through an office to reach an office"
        //    said about the one row that used to have no other way in.
        foreach ((double lo, double hi) in RingSegments(
            block.WestInnerX, block.EastInnerX, block.SpurXs, CorridorHalf, block.X0, block.X1))
        {
            int n = RingRoomsIn(hi - lo);
            for (int k = 0; k < n; k++)
            {
                double rx0 = lo + ((hi - lo) * k / n), rx1 = lo + ((hi - lo) * (k + 1) / n);
                ring.Add(RingBox(
                    walls, glass, doorways, labels, claimed, spineDoors, bodyId, level, found,
                    ring.Count + 1, RingSide.Far, rx0, block.BackStreetY1, rx1, block.Y0, block,
                    gate: true));
            }
        }

        // ── (3) AND THE TWO ENDS · a gate through the middle of each, and a room above and below it.
        foreach (RingSide side in new[] { RingSide.West, RingSide.East })
        {
            bool west = side == RingSide.West;
            double inner = west ? block.WestInnerX : block.X1;
            double outer = west ? block.X0 : block.EastInnerX;
            double mid = (block.Y0 + block.Y1) / 2.0;
            foreach ((double lo, double hi) in RingSegments(
                block.Y0, block.Y1, [mid], CorridorHalf))
            {
                ring.Add(RingBox(
                    walls, glass, doorways, labels, claimed, spineDoors, bodyId, level, found,
                    ring.Count + 1, side, inner, lo, outer, hi, block, gate: false));
            }
        }

        // ── (4) THE GATES · one spur per crossing, and the park's wall opened where it arrives.
        //
        //    THE HALL OPENS ONTO ONE OF THEM. #751's law is that the hall never cuts a door: the gaps in
        //    the corridor beside it ARE its doors, and it publishes the very slots the corridor was cut at.
        //    That corridor used to be a rib and is a gate now — so the gate's side wall is swept with the
        //    hall's own published openings taken out of it, and the room keeps every door it had. Watched go
        //    red without this: "the door at (-1.5,-177.4) is one the hall knows about and the deck plan does
        //    not", 208 of them, on every floor with a block on it.
        var abutting = new List<SurfaceLayout.Doorway>();
        foreach (SurfaceLayout.Doorway o in hall.Openings)
        {
            if (Math.Abs(o.X1 - o.X2) < 0.001)
            {
                abutting.Add(o);

                // …and PUBLISHED, in the same list every other door down here is in, so the deck hangs its
                // imported leaf on it and an audit can find it without knowing anything about halls. This is
                // the job AddRoomsAlong used to do for the hall's own column (#751) and the column is a gate
                // now, so the gate does it. Watched go red without this: "the door at (-1.5,-177.4) is one
                // the hall knows about and the deck plan does not — nothing would be drawn there."
                if (!found)
                {
                    doorways.Add(o);
                }
            }
        }

        foreach (double sx in block.SpurXs)
        {
            RingGate(walls, doorways, claimed, gates, sx, sf, block.Y1, vertical: true, abutting);
            RingGate(walls, doorways, claimed, gates, sx, block.BackStreetY1, block.Y0, vertical: true);
        }
        double midY = (block.Y0 + block.Y1) / 2.0;
        RingGate(walls, doorways, claimed, gates, midY, block.WestInnerX, block.X0, vertical: false);
        RingGate(walls, doorways, claimed, gates, midY, block.EastInnerX, block.X1, vertical: false);

        return ring;
    }

    /// <summary>#813 · One ring room: four walls, a door on the street, and — where it has any park in front
    /// of it — the glass that makes it one of these rather than a chamber.
    ///
    /// <para><paramref name="fromX"/>/<paramref name="fromY"/> is the STREET corner and
    /// <paramref name="toX"/>/<paramref name="toY"/> the PARK corner, so this one function lays a room on
    /// any of the four sides without ever asking which side it is on — the same trick <see cref="CarveHall"/>
    /// plays with its u and v.</para></summary>
    private static RingRoom RingBox(
        List<SurfaceLayout.Wall> walls, List<SurfaceLayout.Wall> glass,
        List<SurfaceLayout.Doorway> doorways, List<SurfaceLayout.Landmark> labels,
        List<(double X0, double Y0, double X1, double Y1)> claimed,
        List<(double Lo, double Hi, double PlateX, string Plate)> spineDoors,
        string bodyId, int level, bool found, int number, RingSide side,
        double fromX, double fromY, double toX, double toY, in ParkBlock block, bool gate)
    {
        bool horizontal = side is RingSide.Near or RingSide.Far;
        double x0 = Math.Min(fromX, toX), x1 = Math.Max(fromX, toX);
        double y0 = Math.Min(fromY, toY), y1 = Math.Max(fromY, toY);

        // WHERE THE PARK IS, and how much of this room's front wall actually looks at it. A corner room
        // stands past the end of the park's own wall, so it has none — which is the amenity gradient (#775)
        // as geometry: the rooms with the view are the ones with the view.
        double faceLo = horizontal ? x0 : y0, faceHi = horizontal ? x1 : y1;
        double parkLo = horizontal ? block.X0 : block.Y0, parkHi = horizontal ? block.X1 : block.Y1;
        bool view = faceLo >= parkLo - 0.001 && faceHi <= parkHi + 0.001;
        double mid = (faceLo + faceHi) / 2.0;

        // ── THE FOUR WALLS. The park-facing one is laid last because it is the one that is sometimes glass,
        //    sometimes glass with a door in it, and on a corner room simply concrete.
        double parkLine = horizontal ? (side == RingSide.Near ? y0 : y1) : (side == RingSide.West ? x1 : x0);
        double streetLine = horizontal ? (side == RingSide.Near ? y1 : y0) : (side == RingSide.West ? x0 : x1);

        // the two side walls, which are also the pier between this room and its neighbour
        if (horizontal)
        {
            walls.Add(new(x0, y0, x0, y1, true));
            walls.Add(new(x1, y0, x1, y1, true));
        }
        else
        {
            walls.Add(new(x0, y0, x1, y0, true));
            walls.Add(new(x0, y1, x1, y1, true));
        }

        // ── THE STREET DOORS. On the near band the street is the SPINE, and its face is poured by the spine
        //    builder from one sorted list of spans — so the gaps are handed over rather than cut here, which
        //    is #585's one-gap law said about the one wall this room does not own.
        //
        //    #817 · THERE ARE SEVERAL OF THEM NOW. Owner, live, from inside one of these: "Oh just one door
        //    in a landscape office?" … "bigger spaces must have much more doors." The count is the room's own
        //    frontage divided by DoorsForFrontage's ratio, with #822's fire-code floor under it — never a
        //    number typed here — and they are spaced evenly down the face, so a room with ONE door still puts
        //    it exactly where it has always been (the frontage's midpoint) and nothing about the single-door
        //    case moved.
        int leaves = DoorsForFrontage(faceHi - faceLo);
        var openings = new List<SurfaceLayout.Doorway>(leaves);
        var cuts = new List<double>(leaves);
        for (int d = 0; d < leaves; d++)
        {
            double at = faceLo + ((faceHi - faceLo) * (d + 0.5) / leaves);
            cuts.Add(at);
            openings.Add(horizontal
                ? new SurfaceLayout.Doorway(at - DoorHalf, streetLine, at + DoorHalf, streetLine)
                : new SurfaceLayout.Doorway(streetLine, at - DoorHalf, streetLine, at + DoorHalf));
        }
        SurfaceLayout.Doorway door = openings[0];

        // ── THE PLATE. A room with the view gets the block's own register (ParkViewPlates); a CORNER room
        //    gets the building's ordinary one, which is the amenity gradient said in signage rather than in
        //    a sentence. The back of house keeps #801's plates, because those rooms did not become premium
        //    by acquiring a street door.
        string plate = found ? "" : SignFor(bodyId, level, $"hive:{level}:ring:{(int)side}:{number}");
        if (view && side != RingSide.Far)
        {
            plate = found
                ? ""
                : ParkViewPlates[
                    (number + (int)(Frac(bodyId, $"hive:{level}:ring-view") * ParkViewPlates.Count))
                        % ParkViewPlates.Count];
        }
        if (side == RingSide.Far)
        {
            plate = found
                ? ""
                : ParkBackPlates[
                    (number + (int)(Frac(bodyId, $"hive:{level}:park-back") * ParkBackPlates.Count))
                        % ParkBackPlates.Count];
        }

        // ── WHERE THE PLATE READS FROM · BESIDE the door and never over it.
        //
        // #775 learned this the expensive way on the hall's own front doors: "a plate centred on its own
        // doorway is a plate with the captain standing on top of it the moment they arrive — watched happen
        // in the browser on the first boot of ?frontdoor=1, the dot sitting squarely on the word CANTEEN."
        // The ring shipped the same mistake on fourteen rooms a floor and it was found the same way, in the
        // browser, on the first boot of ?parkwalk=1: PRIVILEGED RECORDS · READING ROOM with the avatar in
        // the middle of it. A sign you have to step off to read is not signage.
        //
        // Stepped along the room's own wall rather than out into the corridor, and clamped inside the
        // room's span so a narrow room's plate cannot wander onto its neighbour's frontage.
        //
        // #817 · …and it is beside the FIRST door only, however many the frontage earned. Two plates on one
        // room would be two answers to "which room is this", and the room's own signage is the only thing
        // telling fourteen identical poured boxes apart.
        double aside = DoorHalf + 3.0;
        double plateAt = Math.Clamp(cuts[0] + aside, faceLo + 1.5, faceHi - 1.5);

        if (side == RingSide.Near)
        {
            for (int d = 0; d < cuts.Count; d++)
            {
                spineDoors.Add((cuts[d] - DoorHalf, cuts[d] + DoorHalf, plateAt, d == 0 ? plate : ""));
            }
        }
        else
        {
            // #817 · ONE SORTED SWEEP WITH A CURSOR THAT MAY ONLY MOVE FORWARD (§13.2), because this wall
            // has several holes in it now. The cuts are generated in ascending order and the sweep is
            // written as if they were not, for #587's own reason: a wall built by a cursor over a list it
            // was handed in the wrong order is the bug this file keeps a monument to.
            double cursor = horizontal ? x0 : y0, end = horizontal ? x1 : y1;
            foreach (double at in cuts)
            {
                double near = Math.Max(cursor, at - DoorHalf);
                if (near > cursor)
                {
                    walls.Add(horizontal
                        ? new SurfaceLayout.Wall(cursor, streetLine, near, streetLine, true)
                        : new SurfaceLayout.Wall(streetLine, cursor, streetLine, near, true));
                }
                cursor = Math.Max(cursor, at + DoorHalf);
            }
            if (end > cursor)
            {
                walls.Add(horizontal
                    ? new SurfaceLayout.Wall(cursor, streetLine, end, streetLine, true)
                    : new SurfaceLayout.Wall(streetLine, cursor, streetLine, end, true));
            }

            if (!found)
            {
                foreach (SurfaceLayout.Doorway leaf in openings)
                {
                    doorways.Add(leaf);
                }
                labels.Add(new(
                    horizontal ? plateAt : streetLine + (side == RingSide.West ? -2.5 : 2.5),
                    horizontal ? streetLine + (side == RingSide.Far ? -2.5 : 2.5) : plateAt,
                    plate));
            }
        }

        // ── THE PARK-FACING WALL. Glass where there is a park behind it; on the far band a door is cut in
        //    it and the glass is the rest of it, which is what a potting shed's front actually looks like.
        SurfaceLayout.Wall? viewWall = null;
        SurfaceLayout.Doorway? parkDoor = null;
        if (view)
        {
            viewWall = horizontal
                ? new SurfaceLayout.Wall(x0, parkLine, x1, parkLine, true)
                : new SurfaceLayout.Wall(parkLine, y0, parkLine, y1, true);
            if (gate)
            {
                parkDoor = horizontal
                    ? new SurfaceLayout.Doorway(mid - DoorHalf, parkLine, mid + DoorHalf, parkLine)
                    : new SurfaceLayout.Doorway(parkLine, mid - DoorHalf, parkLine, mid + DoorHalf);
                glass.Add(horizontal
                    ? new SurfaceLayout.Wall(x0, parkLine, mid - DoorHalf, parkLine, true)
                    : new SurfaceLayout.Wall(parkLine, y0, parkLine, mid - DoorHalf, true));
                glass.Add(horizontal
                    ? new SurfaceLayout.Wall(mid + DoorHalf, parkLine, x1, parkLine, true)
                    : new SurfaceLayout.Wall(parkLine, mid + DoorHalf, parkLine, y1, true));
                if (!found)
                {
                    doorways.Add(parkDoor.Value);
                }
            }
            else
            {
                glass.Add(viewWall.Value);
            }
        }
        else
        {
            walls.Add(horizontal
                ? new SurfaceLayout.Wall(x0, parkLine, x1, parkLine, true)
                : new SurfaceLayout.Wall(parkLine, y0, parkLine, y1, true));
        }

        claimed.Add((x0 - 1.5, y0 - 1.5, x1 + 1.5, y1 + 1.5));

        // ── #817 · AND WHAT IS ON THE FLOOR OF IT.
        //
        // Owner, live, standing in one of these on a bare deck: "It really needs tables … the cubicles etc
        // chairs maybe tables etc. It is way too empty." The room is furnished LAST, because a placer that
        // ran before the doors were cut would be measuring its clearances against a wall with no holes in
        // it — and it is furnished by RingOffice, which is handed the finished room and answers what is
        // standing in it. Nothing here decides where a desk goes.
        //
        // The solids go into the SAME wall list every other piece of furniture down here goes into (the
        // park's raised beds, the en-suite's pan) so one segment is both the drawing and the collision, and
        // the cubicles' leaves go into the same doorway list every door in the building is in — #821's lock
        // has to be able to find them without knowing what a WC is.
        var furnished = new RingRoom(
            number, x0, y0, x1, y1, side, door, viewWall, parkDoor, plate, openings);
        RingOffice.Furnishing fit = RingOffice.Fit(in furnished);
        foreach (SurfaceLayout.Wall solid in fit.Solids)
        {
            walls.Add(solid);
        }
        if (!found)
        {
            foreach (SurfaceLayout.Doorway leaf in fit.Doors)
            {
                doorways.Add(leaf);
            }
        }

        return furnished with { Fittings = fit.Fixtures, Chairs = fit.Chairs };
    }

    /// <summary>#813 · One gate through the ring — a corridor's width of spur, and the park's own wall
    /// opened to a doorway where it arrives. The spur's two side walls ARE the party walls of the rooms
    /// either side of it, so a gate costs the ring a pier and never a room.</summary>
    private static void RingGate(
        List<SurfaceLayout.Wall> walls, List<SurfaceLayout.Doorway> doorways,
        List<(double X0, double Y0, double X1, double Y1)> claimed,
        List<SurfaceLayout.Doorway> gates, double at, double from, double to, bool vertical,
        IReadOnlyList<SurfaceLayout.Doorway>? abutting = null)
    {
        if (vertical)
        {
            // The two side walls, in the segments left between whatever opens off them. One sorted sweep
            // with a cursor that may only move forward (§13.2) — #587's own law, said about a corridor
            // whose neighbour is a room somebody else carved.
            foreach (int face in (int[])[-1, +1])
            {
                double wx = at + (face * CorridorHalf);
                var cuts = new List<(double Lo, double Hi)>();
                foreach (SurfaceLayout.Doorway o in abutting ?? [])
                {
                    if (Math.Abs(o.X1 - wx) < 0.001)
                    {
                        cuts.Add((Math.Min(o.Y1, o.Y2), Math.Max(o.Y1, o.Y2)));
                    }
                }
                cuts.Sort((a, b) => a.Lo.CompareTo(b.Lo));

                double lo = Math.Min(from, to), hi = Math.Max(from, to), cursor = lo;
                foreach ((double clo, double chi) in cuts)
                {
                    if (clo > cursor)
                    {
                        walls.Add(new(wx, cursor, wx, Math.Min(clo, hi), true));
                    }
                    cursor = Math.Max(cursor, chi);
                }
                if (cursor < hi)
                {
                    walls.Add(new(wx, cursor, wx, hi, true));
                }
            }
            walls.Add(new(at - CorridorHalf, to, at - DoorHalf, to, true));
            walls.Add(new(at + DoorHalf, to, at + CorridorHalf, to, true));
            var gate = new SurfaceLayout.Doorway(at - DoorHalf, to, at + DoorHalf, to);
            doorways.Add(gate);
            gates.Add(gate);
            claimed.Add((
                at - CorridorHalf - 1.5, Math.Min(from, to) - 1.5,
                at + CorridorHalf + 1.5, Math.Max(from, to) + 1.5));
        }
        else
        {
            walls.Add(new(from, at - CorridorHalf, to, at - CorridorHalf, true));
            walls.Add(new(from, at + CorridorHalf, to, at + CorridorHalf, true));
            walls.Add(new(to, at - CorridorHalf, to, at - DoorHalf, true));
            walls.Add(new(to, at + DoorHalf, to, at + CorridorHalf, true));
            var gate = new SurfaceLayout.Doorway(to, at - DoorHalf, to, at + DoorHalf);
            doorways.Add(gate);
            gates.Add(gate);
            claimed.Add((
                Math.Min(from, to) - 1.5, at - CorridorHalf - 1.5,
                Math.Max(from, to) + 1.5, at + CorridorHalf + 1.5));
        }
    }

    /// <summary>
    /// #759 · THE PARK, CARVED — the one room in the building that is not a box off a corridor.
    ///
    /// <para>Laid in the park's own two axes, the hall's discipline: <b>u</b> runs along the spine and
    /// <b>w</b> runs outward from the wall it shares with the hall. It owns three walls and half of a
    /// fourth: the far wall, the two ends, and the near wall in the segments left over once the corridor's
    /// gate and the hall's GLASS have been taken out of it. Neither of those two openings is cut here —
    /// the gate is the rib's own far end (the corridor stops being a dead end) and the glass is the hall's
    /// own far wall — which is #585's one-gap law said about a room that has two neighbours.</para>
    ///
    /// <para><b>The walk comes first and the planting is laid around it</b>, which is the whole reason a
    /// park drawn on a square grid can have a curve in it. The centre-line is a smooth line the length of
    /// the room; a bed is only laid where it clears that line by a walk's half-width and then some. Do it
    /// the other way — beds first, path threaded after — and the path is whatever the beds left, which is
    /// how a garden becomes a maze and how a guard stops being able to fail.</para>
    /// </summary>
    private static Park CarvePark(
        List<SurfaceLayout.Wall> walls, string bodyId, int level, in Hall hall, SurfaceLayout.Wall glass,
        in ParkBlock block, IReadOnlyList<SurfaceLayout.Doorway> gates, IReadOnlyList<RingRoom> ring)
    {
        // ── #813 · THE BOX IS THE BLOCK'S. Every one of the park's four walls belongs to a room or to a
        //    gate (CarveRing), so nothing here pours a single boundary segment: what is left for this method
        //    is the GROUND — the walk, the planting, the benches, the masts, and the one figure sitting at
        //    the far end of it.
        //
        //    That is a real narrowing and it is the point. The park used to own three of its own walls and
        //    half of a fourth, which meant the shape of the room and the shape of the fabric around it were
        //    two answers to one question; now the fabric IS the shape, and a wall the ring did not lay is a
        //    wall the park does not have.
        double x0 = block.X0, x1 = block.X1;
        double y0 = block.Y0, y1 = block.Y1;
        double depth = y1 - y0;
        double W(double w) => y1 - w;

        // WHICH GATE THE ROOM'S PLATE AND ITS DEV ROUTE ARE PINNED TO: the one on the hall's own side of
        // the green, nearest the bar, which is the gate a drinker walks out of. #775's rule, said about a
        // wall that now has six openings in it instead of two.
        SurfaceLayout.Doorway first = gates[0];
        double bestD = double.MaxValue;
        foreach (SurfaceLayout.Doorway g in gates)
        {
            if (Math.Abs(g.Y1 - y1) > 0.001 || Math.Abs(g.Y2 - y1) > 0.001)
            {
                continue;   // not on the near wall: a gate off the back street or one of the two ends
            }
            double d = Math.Abs(((g.X1 + g.X2) / 2.0) - ((hall.X0 + hall.X1) / 2.0));
            if (d < bestD)
            {
                (first, bestD) = (g, d);
            }
        }
        double gateX = (first.X1 + first.X2) / 2.0;

        // ── THE WALK · one long curve down the room, and a spur in from the gate.
        double uLo = x0 + ParkEdgeClearDu, uHi = x1 - ParkEdgeClearDu;
        double span = uHi - uLo;
        double mid = depth / 2.0;
        double amp = Math.Max(1.0, mid - ParkEdgeClearDu - ParkWalkHalfDu - 2.0);
        double Curve(double u) =>
            mid + (amp * Math.Sin(2 * Math.PI * ParkWalkBends * (u - uLo) / span));

        var walk = new List<(double X, double Y)>();
        double gateU = Math.Clamp(gateX, uLo, uHi);
        for (double w = 0; w < Curve(gateU); w += 1.5)
        {
            walk.Add((gateU, W(w)));      // in from the gate, until it meets the long walk
        }
        for (double u = uLo; u <= uHi + 0.001; u += 1.5)
        {
            walk.Add((u, W(Curve(u))));
        }

        // ── THE BEDS · a grid of raised boxes, minus every one that would stand on the walk.
        var beds = new List<GrowingBed>();
        double bu0 = uLo + ParkBedHalfWDu, bu1 = uHi - ParkBedHalfWDu;
        double bw0 = ParkEdgeClearDu + ParkBedHalfHDu, bw1 = depth - ParkEdgeClearDu - ParkBedHalfHDu;
        // #813 · The grid is a du tighter both ways than the band-shaped park's was. The green kept its area
        // when it became a block and it lost most of its LENGTH, and a planting pitch measured for a room
        // six times wider than it is deep leaves a garden with six beds in it — which is what the first
        // Manhattan carve produced, and the number a guard measures rather than a paragraph.
        int cols = Math.Max(1, (int)((bu1 - bu0) / ((2 * ParkBedHalfWDu) + 5)) + 1);
        int rows = Math.Max(1, (int)((bw1 - bw0) / ((2 * ParkBedHalfHDu) + 2)) + 1);
        double clearU = ParkBedHalfWDu + ParkWalkHalfDu + 1.0;
        double clearW = ParkBedHalfHDu + ParkWalkHalfDu + 1.0;

        // #813 · …and minus every one that would stand in front of a GATE. There are six of them now and
        // they arrive on all four sides, so "the ground in front of a door is clear" stopped being a thing
        // the old single gate got for free off the gate spur's own walk. A bed across a doorway is the
        // shape of bug this file keeps a table of: drawn correct, walked shut.
        var mouths = new List<(double X, double Y)>(gates.Count);
        foreach (SurfaceLayout.Doorway g in gates)
        {
            mouths.Add(((g.X1 + g.X2) / 2.0, (g.Y1 + g.Y2) / 2.0));
        }

        // …and the back of house's own doors onto the gravel (#801) are mouths in this wall exactly as the
        // gates are. They are not in `gates` — a Way is a way THROUGH the park and these are ways OUT of it
        // into a room, a distinction Park.BackDoors has kept since #801 — and leaving them out of THIS list
        // is what put a floodlight mast in front of two of them on every floor in the game. Watched go red.
        foreach (RingRoom room in ring)
        {
            if (room.Gate is { } gravel)
            {
                mouths.Add(((gravel.X1 + gravel.X2) / 2.0, (gravel.Y1 + gravel.Y2) / 2.0));
            }
        }

        for (int r = 0; r < rows; r++)
        {
            double bw = rows == 1 ? (bw0 + bw1) / 2.0 : bw0 + (r * (bw1 - bw0) / (rows - 1));
            for (int c = 0; c < cols; c++)
            {
                double bu = cols == 1 ? (bu0 + bu1) / 2.0 : bu0 + (c * (bu1 - bu0) / (cols - 1));

                // Would it stand on the gravel? Asked of the walk the room actually published, sample by
                // sample — never of the formula, which is the same discipline as measuring the tops rather
                // than reading the seat target.
                bool blocked = false;
                foreach ((double px, double py) in walk)
                {
                    double pw = Math.Abs(py - y1);
                    if (Math.Abs(px - bu) < clearU && Math.Abs(pw - bw) < clearW)
                    {
                        blocked = true;
                        break;
                    }
                }

                double by = W(bw);
                foreach ((double mx, double my) in mouths)
                {
                    blocked |= Math.Abs(mx - bu) < ParkBedHalfWDu + ParkWalkHalfDu + 1.0
                        && Math.Abs(my - by) < ParkBedHalfHDu + ParkWalkHalfDu + 1.0;
                }
                if (blocked)
                {
                    continue;
                }

                walls.Add(new(bu - ParkBedHalfWDu, by - ParkBedHalfHDu,
                    bu + ParkBedHalfWDu, by - ParkBedHalfHDu, true));
                walls.Add(new(bu - ParkBedHalfWDu, by + ParkBedHalfHDu,
                    bu + ParkBedHalfWDu, by + ParkBedHalfHDu, true));
                walls.Add(new(bu - ParkBedHalfWDu, by - ParkBedHalfHDu,
                    bu - ParkBedHalfWDu, by + ParkBedHalfHDu, true));
                walls.Add(new(bu + ParkBedHalfWDu, by - ParkBedHalfHDu,
                    bu + ParkBedHalfWDu, by + ParkBedHalfHDu, true));

                beds.Add(new GrowingBed(
                    beds.Count + 1, bu, by, ParkBedHalfWDu, ParkBedHalfHDu,
                    ParkCrops[beds.Count % ParkCrops.Count]));
            }
        }

        // ── THE BENCHES · one at every bend, on the outside of it, where a bench goes.
        var benches = new List<(double X, double Y)>();
        for (int k = 0; k < 2 * ParkWalkBends; k++)
        {
            double u = uLo + (span * (0.25 + (k * 0.5)) / ParkWalkBends);
            double w = Curve(u);
            double off = w > mid ? ParkWalkHalfDu + 1.6 : -(ParkWalkHalfDu + 1.6);
            double by = W(w + off);
            benches.Add((u, by));
            walls.Add(new(u - ParkBenchHalfDu, by, u + ParkBenchHalfDu, by, true));
        }

        // ── THE MASTS · the artificial day, standing along the far pavement. The far wall used to be the
        //    edge of the world and is a row of shop fronts now, so a mast is STEPPED ASIDE where one would
        //    otherwise stand in front of somebody's door — nudged, never dropped: the artificial day is what
        //    makes an underground garden legible and a park with two lamps in it is a car park.
        var masts = new List<(double X, double Y)>();
        double mastW = depth - (ParkEdgeClearDu / 2.0);
        foreach (double at in ParkMastXs(x0, x1))
        {
            double my = W(mastW), u = at;
            foreach ((double mx, double gy) in mouths)
            {
                if (Math.Abs(gy - my) >= DoorHalf + ParkEdgeClearDu || Math.Abs(mx - u) >= DoorHalf + 2.0)
                {
                    continue;
                }
                double step = DoorHalf + 2.0 + 0.1;
                u = mx + (mx <= (x0 + x1) / 2.0 ? step : -step);
            }
            u = Math.Clamp(u, x0 + 1.0, x1 - 1.0);
            masts.Add((u, my));
            walls.Add(new(u - 0.6, my - 0.6, u + 0.6, my - 0.6, true));
            walls.Add(new(u - 0.6, my + 0.6, u + 0.6, my + 0.6, true));
            walls.Add(new(u - 0.6, my - 0.6, u - 0.6, my + 0.6, true));
            walls.Add(new(u + 0.6, my - 0.6, u + 0.6, my + 0.6, true));
        }

        // ── #801/#813 · THE BACK OF HOUSE, which is now the ring's FAR band. It is published in both
        //    shapes on purpose and it is ONE set of rooms: Park.Rooms is the view #801's own consumers were
        //    written against (a box, a door off the gravel, a plate) and Park.Frontage is the whole ring.
        //    A second carve for the second shape is the mirrored-constant bug with a record's clothes on.
        var back = new List<BackRoom>();
        foreach (RingRoom room in ring)
        {
            if (room.Side == RingSide.Far && room.Gate is { } gravel)
            {
                back.Add(new BackRoom(room.X0, room.Y0, room.X1, room.Y1, gravel, room.Plate));
            }
        }

        // The lone figure, on the bench furthest from the gate. Scenery: the owner's own "benches, the lone
        // figure, the curve that hides the far end", and nothing to press — a park that started offering
        // things would be a park that had noticed you.
        (double figX, double figY) = benches[0];
        foreach ((double bx, double by) in benches)
        {
            if (Math.Abs(bx - gateX) > Math.Abs(figX - gateX))
            {
                (figX, figY) = (bx, by);
            }
        }

        // #775 · Every gate as a published doorway, the hall's own FIRST — it is the one the room's plate
        // and its dev route are pinned to, and the order is the only thing that says which is which.
        var ordered = new List<SurfaceLayout.Doorway>(gates.Count) { first };
        foreach (SurfaceLayout.Doorway g in gates)
        {
            if (g != first)
            {
                ordered.Add(g);
            }
        }

        return new Park(
            x0, y0, x1, y1, walk, beds, benches, masts,
            first,
            glass,
            gateX, W(4.0), figX, figY,
            CanteenRegulars.StrangerPlates[
                (int)(Frac(bodyId, $"hive:{level}:park-figure") * CanteenRegulars.StrangerPlates.Count)
                    % CanteenRegulars.StrangerPlates.Count],
            ParkArtFor(bodyId, HallUseOn(bodyId, level)),
            ordered,
            back,
            ring);
    }

    /// <summary>#775 · What is stencilled at the mouth of the walk down to the park — the arrow idiom this
    /// building already paints on a corridor whose end is somewhere else
    /// (<see cref="SealedMouthSign"/>). The difference is the whole point: that one names a place you will
    /// never reach, and this one is a way through.</summary>
    public const string ParkWaySign = "⟶ THE PARK";

    /// <summary>
    /// #775 · WHERE THE DEDICATED WALK DOWN TO THE PARK IS CUT, or null where this floor has no room for
    /// one.
    ///
    /// <para>Owner: <i>"multiple doors to the park"</i> — and the corridors that already reach it are the
    /// ribs pointing its way, which on a quarter of the shipped sites is exactly ONE (the hall's own). A
    /// park with one door is a cul-de-sac however many ribs happen to fall the right way, so the building
    /// gets a passage whose only job is that crossing: off the main corridor, straight down, into the
    /// green.</para>
    ///
    /// <para>It is placed at the point on the spine's park-side face FURTHEST from everything already
    /// standing on that side — the hall's box, the corridors that reach the park, and the lift alcove where
    /// the park is on the alcove's own face. Furthest rather than first-fit because the room columns either
    /// side of a rib are ground this passage would otherwise take: the claim ledger would drop them
    /// silently, and a floor quietly losing chambers is the shape of bug this file keeps a table of.</para>
    /// </summary>
    private static double? GardenWalkX(
        List<Rib> ribs, bool parkSide, in Hall hall, double shaftX, double? serviceX,
        double leftEnd, double rightEnd)
    {
        var keepOff = new List<(double Lo, double Hi)> { (hall.X0, hall.X1) };
        foreach (Rib r in ribs)
        {
            if (r.Down == parkSide)
            {
                keepOff.Add((r.X - CorridorHalf, r.X + CorridorHalf));
            }
        }
        if (!parkSide)
        {
            keepOff.Add((shaftX - ShaftHalf, shaftX + ShaftHalf));   // the alcove hangs off the top face
        }
        else if (serviceX is { } carX)
        {
            // #801 · …and on the other face the goods car stands, at the blind end — which is precisely
            // where this max-min search likes to land, because the emptiest x on a face is very often the
            // last one. Without this clause the walk down to the green and the second car would have been
            // cut into the same six du of wall.
            keepOff.Add((carX - ShaftHalf, carX + ShaftHalf));
        }

        double clear = CorridorHalf + 2.0;
        // …and it keeps a wall's worth of ground off the ends of the building, which the max-min search
        // would otherwise walk straight into: the emptiest x on this face is very often the last one.
        double lo = leftEnd + clear + CorridorHalf, hi = rightEnd - clear - CorridorHalf;
        double bestX = double.NaN, bestRoom = clear;

        for (double x = lo; x <= hi + 0.001; x += 1.0)
        {
            double room = double.MaxValue;
            foreach ((double a, double b) in keepOff)
            {
                room = Math.Min(room, x < a ? a - x : x > b ? x - b : 0.0);
            }
            if (room > bestRoom)
            {
                (bestRoom, bestX) = (room, x);
            }
        }
        return double.IsNaN(bestX) ? null : bestX;
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

    /// <summary>
    /// #798 · THE BINS — somewhere to put a document you never want read again.
    ///
    /// <para>Owner, live in play: <i>"those trash cans are needed so we get rid of the processed materials
    /// without connecting them to us too clearly, like leaving them to the table."</i> The phase-two loop
    /// (sit, dig, book) ends by producing a liability, and until this method ran, the only places a captain
    /// could put one were their own pocket and the table they had just been sitting at.</para>
    ///
    /// <h3>Where they go, and why those three places</h3>
    /// <list type="bullet">
    /// <item><b>The canteen hall</b> — a <see cref="RipAndBin.Tier.SlopBin"/> at the far end of the counter,
    /// where trays go back, and a <see cref="RipAndBin.Tier.Chute"/> at the service end of the same room,
    /// beside the end of the band the goods hoist parks in. Both rungs of the ladder in the one room a
    /// captain spends an evening in, which is what makes bin CHOICE a choice.</item>
    /// <item><b>The park</b> — a slop-grade grounds bin beside the bench nearest the gate. Clippings,
    /// cartons and whatever the walk leaves behind: a bin with wet in it, in the open, in another room.</item>
    /// <item><b>Every floor that breathes</b> — a <see cref="RipAndBin.Tier.PaperBin"/> by the lift, which is
    /// the copier-corner bin of #775's amenity rule and the reason the offices band has one at all. It is
    /// also the WORST rung, and it is the one that is everywhere: the convenient answer and the wrong one,
    /// which is the whole shape of this feature's joke.</item>
    /// </list>
    ///
    /// <h3>The one discipline</h3>
    /// <para><b>Nothing here is placed at a coordinate somebody liked.</b> Every anchor is derived from
    /// geometry the room already published, every candidate is then CHECKED against the floor's own finished
    /// walls and furniture, and a bin that cannot find clear floor is simply not fitted — the floor says so
    /// by having no bin rather than by having one inside a table. §13.15's rule, and the reason this method
    /// runs last: it is the only placer that needs to see the whole room.</para>
    ///
    /// <para>The solid box goes into <paramref name="walls"/>, which is what makes a bin drawn and collidable
    /// off ONE source — a second rectangle for the eye would be the drawn world and the simulated world
    /// disagreeing about a thing the captain walks into.</para>
    /// </summary>
    private static List<RipAndBin.Bin> CarveBins(
        string bodyId, int level,
        List<SurfaceLayout.Wall> walls,
        List<SurfaceLayout.Doorway> doorways,
        List<(double X, double Y)> roomCentres,
        List<Amenity> amenities,
        List<Refuge> refuges,
        List<Rib> ribs,
        Park? park,
        double shaftX, double shaftY)
    {
        var bins = new List<RipAndBin.Bin>();

        // #775's tidy-spaces rule is what makes a bin worth having AND what makes it a bet: this building is
        // kept by professionals, and professionals empty bins in rooms that have air in them. A floor that
        // does not hold pressure gets none — not a rule about litter, but about who works down there.
        if (!HoldsPressure(bodyId, level))
        {
            return bins;
        }

        var segs = new List<SurfaceCollision.Segment>(walls.Count);
        foreach (SurfaceLayout.Wall w in walls)
        {
            segs.Add(new SurfaceCollision.Segment(w.X1, w.Y1, w.X2, w.Y2));
        }

        // Everything a bin may not be shoved against. Walls are handled by the collision field above; this
        // is the list of things that are NOT walls and still cannot be blocked — a doorway, a rib's mouth, a
        // console the [E] key answers, a chair somebody has to be able to get out of.
        var furniture = new List<(double X, double Y)>();
        foreach (SurfaceLayout.Doorway d in doorways)
        {
            furniture.Add(((d.X1 + d.X2) / 2.0, (d.Y1 + d.Y2) / 2.0));
        }
        foreach (Rib rib in ribs)
        {
            furniture.Add((rib.X, shaftY - CorridorHalf));
            furniture.Add((rib.X, shaftY + CorridorHalf));
        }
        furniture.AddRange(roomCentres);
        foreach (Refuge r in refuges)
        {
            furniture.Add((r.X, r.Y));
        }
        foreach (Amenity a in amenities)
        {
            furniture.Add((a.X, a.Y));
            furniture.AddRange(a.Tables);
            if (a.Hall is { } hall)
            {
                furniture.AddRange(hall.StoolRow);
                foreach (Cabinet c in hall.Cabinets)
                {
                    furniture.Add(c.Table);
                }
                foreach (SurfaceLayout.Doorway d in hall.Openings)
                {
                    furniture.Add(((d.X1 + d.X2) / 2.0, (d.Y1 + d.Y2) / 2.0));
                }
                if (hall.Freight is { } hoist)
                {
                    furniture.Add((hoist.PlateX, hoist.PlateY));
                }
            }
        }
        if (park is { } green)
        {
            furniture.AddRange(green.Walk);
            furniture.AddRange(green.Benches);
            furniture.AddRange(green.Masts);
        }

        // ── THE HALL · the slop bin at the tray end of the counter, the chute at the service end ─────────
        foreach (Amenity a in amenities)
        {
            if (a.Hall is not { } h)
            {
                continue;
            }

            // THE HALL'S OWN TWO AXES, READ BACK OFF WHAT THE ROOM PUBLISHED. The carve laid this room in
            // (u, v) — u out from the rib's face, v in from the spine — and threw the frame away. It is
            // recoverable exactly, from two points the room hangs signs on: the PLATE is a quarter of the
            // way down the door wall at u = HallDoorAisleDu / 2, and the AMENITY's own spot is the middle
            // of the counter at v = counterV - HallEdgePadDu. Two signs, two signs' worth of arithmetic,
            // and not one number typed here that the room did not already say out loud.
            int su = Math.Sign(a.X - h.PlateX);
            int sv = Math.Sign(a.Y - h.PlateY);
            if (su == 0 || sv == 0)
            {
                continue;   // a hall that cannot say which way round it is gets no bins, and says so.
            }

            double faceX = su > 0 ? h.X0 : h.X1;
            double mouthY = sv > 0 ? h.Y0 : h.Y1;
            double U(double u) => faceX + (su * u);
            double V(double v) => mouthY + (sv * v);

            double counterV = ((a.Y - mouthY) * sv) + HallEdgePadDu;
            double cabBand = a.Use == Comfort.UpperCanteen ? HallCabinetDepthDu : 0.0;
            double counterU1 = (h.X1 - h.X0) - cabBand - HallEdgePadDu;
            double midX = (h.X0 + h.X1) / 2.0, midY = (h.Y0 + h.Y1) / 2.0;

            // The chute first, because it is the better bet and a room with only one of the two should keep
            // the one worth walking to. Down the aisle inside the door wall — the service side of the room,
            // where the goods band ends — trying further in until the floor has room for it.
            var chuteAt = new List<(double X, double Y, double TowardX, double TowardY)>();
            var slopAt = new List<(double X, double Y, double TowardX, double TowardY)>();
            for (int step = 1; step <= 6; step++)
            {
                chuteAt.Add((U(HallDoorAisleDu - 0.6), V(counterV - (3.0 * step)), midX, midY));
                slopAt.Add((U(counterU1 - HallEdgePadDu), V(counterV - (3.0 * step)), midX, midY));
            }

            TakeTheBin(RipAndBin.Tier.Chute, chuteAt);
            TakeTheBin(RipAndBin.Tier.SlopBin, slopAt);
        }

        // ── THE PARK · a grounds bin beside the bench nearest the gate ───────────────────────────────────
        if (park is { Benches.Count: > 0 } grounds)
        {
            (double bx, double by) = grounds.Benches[0];
            foreach ((double x, double y) in grounds.Benches)
            {
                double was = ((bx - grounds.X) * (bx - grounds.X)) + ((by - grounds.Y) * (by - grounds.Y));
                double now = ((x - grounds.X) * (x - grounds.X)) + ((y - grounds.Y) * (y - grounds.Y));
                if (now < was)
                {
                    (bx, by) = (x, y);
                }
            }

            double pmx = (grounds.X0 + grounds.X1) / 2.0, pmy = (grounds.Y0 + grounds.Y1) / 2.0;
            var parkAt = new List<(double X, double Y, double TowardX, double TowardY)>();
            foreach (double off in new[] { 5.5, 7.0, 8.5, 10.0 })
            {
                parkAt.Add((bx + off, by, pmx, pmy));
                parkAt.Add((bx - off, by, pmx, pmy));
            }
            TakeTheBin(RipAndBin.Tier.SlopBin, parkAt);
        }

        // ── THE LIFT · the paper bin every floor that breathes has, and the worst rung of the ladder ─────
        var liftAt = new List<(double X, double Y, double TowardX, double TowardY)>();
        foreach (double along in new[] { 3.5, 7.0, 10.5, -3.5, -7.0, -10.5 })
        {
            double bx = shaftX + (along > 0 ? ShaftHalf + along : -ShaftHalf + along);
            liftAt.Add((bx, shaftY - CorridorHalf + 2.0, bx, shaftY));
            liftAt.Add((bx, shaftY + CorridorHalf - 2.0, bx, shaftY));
        }
        TakeTheBin(RipAndBin.Tier.PaperBin, liftAt);

        return bins;

        // ── THE FITTING ITSELF ───────────────────────────────────────────────────────────────────────────
        //
        // One local function, so every bin in the building is stood up by the same three tests and none of
        // the placers above can quietly relax one of them: clear of the walls, clear of the furniture, and
        // with somewhere to STAND that is clear of both and inside arm's reach. A candidate that fails any
        // of the three is skipped; a bin whose whole list fails is not fitted at all.
        void TakeTheBin(
            RipAndBin.Tier tier, IReadOnlyList<(double X, double Y, double TowardX, double TowardY)> anchors)
        {
            foreach ((double bx, double by, double tx, double ty) in anchors)
            {
                if (SurfaceCollision.Blocked(bx, by, RipAndBin.ClearDu, segs) || Crowded(bx, by))
                {
                    continue;
                }

                double dx = tx - bx, dy = ty - by;
                double len = Math.Sqrt((dx * dx) + (dy * dy));
                if (len < 0.001)
                {
                    continue;
                }

                double sx = bx + (dx / len * RipAndBin.StandOffDu);
                double sy = by + (dy / len * RipAndBin.StandOffDu);
                if (SurfaceCollision.Blocked(sx, sy, RipAndBin.StandClearDu, segs))
                {
                    continue;
                }

                bins.Add(new RipAndBin.Bin(tier, bx, by, sx, sy, RipAndBin.PlateFor(tier)));

                // The box, in the ONE list that is both drawn and collided with. And into the collision
                // field this method is still reading, so the next bin cannot be stood inside this one.
                double h = RipAndBin.HalfDu;
                AddBox(bx, by, h);

                // …and the spot a captain stands on is furniture now, for the same reason a chair is: the
                // next bin may not be put where somebody has to stand to use this one.
                furniture.Add((sx, sy));
                return;
            }
        }

        void AddBox(double cx, double cy, double h)
        {
            (double, double, double, double)[] sides =
            [
                (cx - h, cy - h, cx + h, cy - h), (cx - h, cy + h, cx + h, cy + h),
                (cx - h, cy - h, cx - h, cy + h), (cx + h, cy - h, cx + h, cy + h),
            ];
            foreach ((double x1, double y1, double x2, double y2) in sides)
            {
                walls.Add(new SurfaceLayout.Wall(x1, y1, x2, y2, true));
                segs.Add(new SurfaceCollision.Segment(x1, y1, x2, y2));
            }
        }

        bool Crowded(double bx, double by)
        {
            foreach ((double fx, double fy) in furniture)
            {
                double dx = fx - bx, dy = fy - by;
                if ((dx * dx) + (dy * dy)
                    < RipAndBin.ClearOfFurnitureDu * RipAndBin.ClearOfFurnitureDu)
                {
                    return true;
                }
            }
            return false;
        }
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

    /// <summary>#801 · The nearest plate in this floor s own register that is NOT a room somebody sat in.
    ///
    /// <para>Walked from the same seed <see cref="SignFor(string, int, string)"/> used, one step on, so a
    /// room that had to give up its rank keeps the building s vocabulary rather than acquiring a sign
    /// invented for the occasion. Empty only if a kind ever ships a register with nothing but principal
    /// plates in it, which is a thing a guard would notice long before a player did.</para></summary>
    private static string NotPrincipal(string bodyId, int level, string tag)
    {
        string[] signs = SignsFor(KindOn(bodyId, level));
        ulong seed = DiceRule.Seed($"hive-sign:{bodyId}:{tag}");
        for (int i = 1; i <= signs.Length; i++)
        {
            string candidate = signs[(int)((seed + (ulong)i) % (ulong)signs.Length)];
            if (!IsPrincipalRoom(candidate))
            {
                return candidate;
            }
        }
        return string.Empty;
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

                // #801 · A ROOM THAT COULD NOT BE PLUMBED WAS NEVER A PRINCIPAL ROOM.
                //
                // #707 s law is that rank is readable in PLUMBING: a cell on a store room says nothing, and
                // a principal plate with no cell says the opposite of the thing it is there to say. The cell
                // is refused when the ground behind the room is already spoken for (the ledger is law,
                // #585), and until now that left the plate saying a rank the building could not back up.
                // It never fired on the shipped field — which is exactly why it was worth fixing the moment
                // a new passage moved by four du and it fired on four generated moons.
                //
                // The re-plate walks the SAME seeded list from the SAME seed, one step on, so it is the
                // floor s own vocabulary and not a special sign invented for the case.
                if (!cell && !found && IsPlumbed(bodyId, level) && IsPrincipalRoom(plate))
                {
                    plate = NotPrincipal(bodyId, level, tag);
                }
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

    // ── #684 · THE PANEL READS YOUR WALLET WITHOUT BEING ASKED, AND THAT READ IS A SCENE ───────────────
    //
    // Owner's ruling, on whether the shaft gate should become a TRY target like the doors: it should not.
    // "The panel's unprompted wallet-read IS its character" — a machine that goes through your pockets for
    // you is the whole administrative horror of this place in one gesture, and putting a verb in front of it
    // would turn the building polite.
    //
    // What was wrong was never the interaction. It was that the read happened in SILENCE and then answered
    // out of a SECOND set of sentences. `SatchelTry.Target.ShaftGate` carries the sharpest refusal matrix in
    // the game — #679/#683 taught it to tell "another shaft of THIS site" from "somebody else's building",
    // each named — and it had no client caller at all, while the panel said a flat "every one of them
    // countersigned, current, and for another shaft" out of `WrongCardLine`. Two answers to one question,
    // and the better one was the one nobody could read. That is this repo's third named bug class wearing a
    // costume: the sim knowing a thing the sentence does not say.
    //
    // So `WrongCardLine` is GONE and the matrix is the source. This composes the read into the house card
    // idiom (#528) so the answer is TOLD rather than muttered — art, a title, and the matrix's own line
    // verbatim — and per #736's law the line the player acts on lives ON the card that is up, never only in
    // a pulse behind its backdrop.

    /// <summary>#684 · The panel's read of the wallet, and the card it is told on.</summary>
    /// <param name="Worked">Whether the gate opened. False is a refusal, and <paramref name="Line"/> names
    /// its reason either way (#603's law).</param>
    /// <param name="Line">The matrix's own sentence, verbatim. Nothing here rewrites it.</param>
    /// <param name="Presented">The card the gate actually read, or null when there was nothing in the wallet
    /// to read. It is what decides the face on the card (#695) — the office that issued THIS one.</param>
    /// <param name="Label">The card's title.</param>
    /// <param name="ArtUrl">The presented card's own face, or the nameless fallback when none was presented
    /// — which can never impersonate one of the five offices.</param>
    public readonly record struct GateRead(
        bool Worked, string Line, AuthorityCard? Presented, string Label, string ArtUrl);

    /// <summary>#684 · What the story card is called, at a refusal and at a reading alike. It names the thing
    /// the owner declined to put a verb in front of: the machine goes through your wallet, and you watch.</summary>
    public const string GateReadLabel = "🎫 THE PANEL READS YOUR WALLET";

    /// <summary>#684 · The gate below this car's band, reading what the captain happens to be carrying.
    ///
    /// <para>The judgement is <see cref="SatchelTry.ReadTheWallet"/>'s and only its — this asks the building
    /// which shaft the gate serves and then does as it is told.</para></summary>
    /// <param name="bodyId">The site.</param>
    /// <param name="standingLevel">The floor the car is on; the gate is the one under this car's band.</param>
    /// <param name="carried">The satchel. Anything that is not an authority is not in the wallet.</param>
    public static GateRead TheGateReads(
        string bodyId, int standingLevel, IReadOnlyList<Satchel.Item>? carried)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        // #677 · The next shaft that EXISTS. Under an unlisted band there is a band with nothing dug in it,
        // and a gate named for it would be a refusal about solid rock. Where the building has nothing below
        // at all there is no gate to read, and the band arithmetic is only a name for a card nobody holds.
        int band = NextShaftBelow(bodyId, standingLevel) ?? (BandOf(Math.Min(standingLevel, -1)) + 1);
        var gate = new AuthorityCard(bodyId, band);

        SatchelTry.WalletRead read =
            SatchelTry.ReadTheWallet(carried, SatchelTry.Target.ShaftGate, gate.Id);

        AuthorityCard? presented =
            read.Read is { } item && AuthorityCard.TryParse(item.Id, out AuthorityCard c) ? c : null;

        return new(
            read.Outcome.Worked, read.Outcome.Line, presented, GateReadLabel,
            presented is { } shown ? AuthorityCardArtUrl(shown) : AuthorityCardFallbackArtUrl);
    }

    /// <summary>#684 · The same read, the other way it can end — told at the ARRIVAL rather than at the
    /// panel, because that is where the ride's beat has been said since #689 and a card raised on the frame
    /// the floor is rebuilt is a card raised at nobody.
    ///
    /// <para><see cref="CardAcceptedLine"/> stays the one sentence for this: it is about the car going deeper
    /// than the building admits to, which is a different question from the one the matrix answers, and it has
    /// been the panel's success beat since #592.</para></summary>
    public static GateRead TheGateAccepted(AuthorityCard card) => new(
        true, CardAcceptedLine(card), card, GateReadLabel, AuthorityCardArtUrl(card));

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

    /// <summary>#751/#759 · The B1 cantina hall, painted.
    ///
    /// <para>#759 · …and repainted, because the room grew a wall. Owner's pinned requirement: <i>"the
    /// restaurant scene must have a) A VIEW TO THE PARK and b) A WINDOW WALL BETWEEN"</i>, and the canonical
    /// mapping he filed with it makes <c>b1-restaurant-park-view.jpg</c> the hall's own establishing shot —
    /// the same room, the same steel tables, and the floor-to-ceiling riveted glass with the green behind
    /// it. It supersedes <c>b1-cantina-hall.jpg</c> everywhere the hall's art shows (the floor it wears and
    /// the card it raises are the same picture on purpose, #755), because a card that shows a room with no
    /// window in it, hung over a plan whose far wall is a window, is two pictures of one room disagreeing
    /// about the room.</para></summary>
    public const string CantinaHallArtUrl = "art/b1-restaurant-park-view.jpg";

    /// <summary>#756 · How opaque a hall's floor art is drawn. A shade under the ship's own 0.9f: the hall
    /// is thirty times the floor area of a cabin, so the same alpha that reads as texture behind a 12×7
    /// cantina reads as a photograph the deck grid is lost in. Legibility first — the walls, the plates,
    /// the tops and the captain all draw OVER this.</summary>
    public const float HallArtAlpha = 0.72f;

    /// <summary>#756 · Which picture a hall's floor wears, or null for a bare deck.
    ///
    /// <para>Owner, on walking into the biggest social room in the game and finding bare grid: <i>"let's
    /// put todo to have gen-AI Bar image on the background like we have in space ports."</i> One row per
    /// painted hall, asked of the building the same way its plates and its seats are — so the park, the
    /// mess and the head office's dining room each take a row here when their art lands, and nothing else
    /// anywhere has to learn a new idea to wear one.</para>
    ///
    /// <para>The branch office's UPPER canteen is the one painted so far, and the art is the very frame
    /// #755's card already shows: the same room, from the door, so walking in and reading the card are two
    /// looks at one place rather than two places.</para></summary>
    public static string? HallArtFor(string bodyId, Comfort use)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return use == Comfort.UpperCanteen && !IsHeadOffice(bodyId) ? CantinaHallArtUrl : null;
    }

    // ── #756/#780 · THE PICTURE OF THE THING, AT THE THING ────────────────────────────────────────────
    //
    // Owner, live playtest 2026-08-08: "see how in the space bars we have the image of bar desk at the spot
    // where the bar desk is."
    //
    // He is describing the ship, and he is right that the hall was only half doing it. #756 gave the floor a
    // HALL-WIDE backdrop — one picture stretched over the whole room, which is ambience — while the bar-desk
    // art it shipped in the same PR was only ever on the service card. So the room you walked was a wide
    // room with a wall drawn across the end of it, and the counter was a line.
    //
    // A SPOT IS THE SECOND KIND OF ROOM ART, and the difference is not size, it is what it claims. Hall art
    // says WHERE YOU ARE. Spot art says WHAT IS THERE — a piece of furniture, drawn over its own box, hard
    // enough at the edges to read as an object you could walk up to. The ship has said this since the 3D
    // renovation and never needed a word for it; the hall needs one because a hall has furniture in it that
    // Core carves and a renderer must not measure.
    //
    // PUBLISHED, NOT DERIVED. The box comes out of CarveHall at the moment the counter's walls are laid, off
    // the very same (u, v) the wall segments are built from — so a hall carved a du wider moves its picture
    // with its bar and no caller anywhere re-measures a room it does not own (§13.15's second cause, which
    // this project has paid for four times). #759's park windows and any fixture after them wear one by
    // adding a Spot where the fixture is carved, and nothing else at all.

    /// <summary>#780 · One piece of a hall's furniture, painted over its own carved box.</summary>
    /// <param name="Url">The picture. Degrades like every other art slot — no file, no frame.</param>
    /// <param name="X0">Left edge, in the surface's own coordinates.</param>
    /// <param name="Y0">Bottom edge.</param>
    /// <param name="X1">Right edge.</param>
    /// <param name="Y1">Top edge.</param>
    public readonly record struct SpotArt(string Url, double X0, double Y0, double X1, double Y1);

    /// <summary>#780 · The bar desk itself: long polished top, brass rail, the backlit shelves behind it and
    /// the stools bolted down along the front. The very picture the counter's own service card wears
    /// (<c>Interior.CounterService</c>), now also standing where the counter stands.</summary>
    public const string CounterArtUrl = "art/b1-bar-desk.jpg";

    /// <summary>#780 · How opaque a FIXTURE's picture is drawn — deliberately harder than
    /// <see cref="HallArtAlpha"/>, and a shade harder than the ship's own 0.9f room backdrops.
    ///
    /// <para>One constant, and the reasoning is the whole of the owner's note. The hall's 0.72 is right for
    /// a floor: it is ambience under a grid, and the grid has to win. A counter is not ambience. It is the
    /// object you walked across the room to stand at, it is five deck-units deep in an eighty-seat hall, and
    /// at 0.72 over hall art it would have read as a slightly different patch of wallpaper — a second
    /// picture where the eye wanted a piece of furniture. So the spot is nearly solid: it has EDGES, it
    /// occludes the floor art under it, and it stops at the exact line the counter's wall is drawn on. The
    /// vector overlay still goes over the top of it, which is the one thing this alpha may never cost — the
    /// walls, the plates, the console dot and the captain are all still legible on the counter.</para></summary>
    public const float SpotArtAlpha = 0.96f;

    /// <summary>#780 · Which picture stands where a hall's counter stands, or null where the room's bar is
    /// not a bar anybody is served at.
    ///
    /// <para>ONE QUESTION, ASKED THE SAME WAY <see cref="HallArtFor"/> is asked. It answers for exactly the
    /// halls whose counter <c>Interior.CounterService.For</c> serves at — the branch office's upper canteen
    /// — because a bar desk drawn on the floor of a room where nothing is poured would be the deck telling a
    /// story the card refuses to tell. The head office's dining room has a SIDEBOARD and not a bar, and its
    /// picture has not been shot; a mess has neither.</para></summary>
    public static string? CounterArtFor(string bodyId, Comfort use)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return use == Comfort.UpperCanteen && !IsHeadOffice(bodyId) ? CounterArtUrl : null;
    }

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

    // ── #693 · WHAT THE DOORS OPENING HAS TO SAY, AND IN WHAT ORDER OF IMPORTANCE ────────────────────────
    //
    // An arrival on a floor can have five things to say at once: the car dropped, the plan has no such floor,
    // the air is good or gone, a gate read a paper, the pour stopped. The HUD's pulse has ONE slot, and until
    // #693 the rule was "last write wins" — so which of the five a player actually read was decided by the
    // order three separate blocks in a razor file happened to be written in. Three of those blocks carried a
    // comment explaining that they were deliberately last. #592's climax — the first words on a floor that
    // does not exist, the biggest sentence in the feature — was not one of them, and had been eaten by the
    // routine pressurisation line since the day it shipped.
    //
    // So the arrival composes here, once, with a RANK on each saying, and PulseSlot's law picks the winner.
    // The list is still in narrative order, because the BOOK keeps every one of them and reads top to bottom;
    // but nothing depends on that order any more, which is the whole point and is guarded as such (the house
    // bug class: a list built by appending is not a list in order).
    //
    // What stays in the client: the cards, the nerve, the flags, the save. Those are effects on a world Core
    // does not have. This is the SAYING, and a saying is prose with a rank on it.

    /// <summary>#693 · Which of the arrival's sayings this is, so the client can hang the effects that belong
    /// to it — a card, a nerve shock, a flag — off the one list rather than re-deciding the conditions.</summary>
    public enum ArrivalBeat
    {
        /// <summary>The car left the surface and kept going. The one beat of scale before any plan is drawn.</summary>
        Descending,

        /// <summary>#592 · The doors part on a floor the lobby's plan does not have.</summary>
        Unlisted,

        /// <summary>#609 · The first dead floor of an excursion — the one that also stops the world with a
        /// card, because whether you can breathe here is not a toast.</summary>
        DeadAirFirst,

        /// <summary>#609 · Every later floor's air line, whichever way it goes.</summary>
        Air,

        /// <summary>#689 · A gate read the countersignature card and the car went deeper than this shaft was
        /// ever dug to.</summary>
        CardAccepted,

        /// <summary>#752 · A gate read the day-labour chit, which is a tired man with a list and not an
        /// office that stopped answering its post.</summary>
        ChitGate,

        /// <summary>#677 · The pour stopped at a line. Said in the shaft, on the way.</summary>
        Seam,

        /// <summary>#677 · The first gallery. Four sentences, three of them measurable and the fourth an
        /// absence.</summary>
        Found,
    }

    /// <summary>#693 · One thing this arrival has to say, what to file it under, and how much it matters.</summary>
    /// <param name="Beat">Which saying it is (the client hangs its effects off this).</param>
    /// <param name="Text">The prose, ready to say and to file.</param>
    /// <param name="Glyph">The book's own column mark for it.</param>
    /// <param name="Rank">Who wins the one pulse slot. See <see cref="PulseRank"/>.</param>
    /// <param name="Gate">The card the gate read, on <see cref="ArrivalBeat.CardAccepted"/> and nowhere
    /// else. Carried on the saying so the caller can mark the shaft as narrated without spelling out a
    /// second time which band a ride ended in — that arithmetic has already been done once, above.</param>
    public readonly record struct Saying(
        ArrivalBeat Beat, string Text, string Glyph, PulseRank Rank, AuthorityCard? Gate = null);

    /// <summary>#693 · What this excursion has already heard, so a beat that is said once is said once.
    ///
    /// <para>Every field is a fact the client already keeps on its <c>SurfaceExcursion</c>; passing them in
    /// rather than re-deriving them keeps the once-ness and the SAYING in one place, which is the thing that
    /// had drifted.</para></summary>
    /// <param name="WasUnderground">Whether the car started below the surface.</param>
    /// <param name="FirstSightOfThisFloor">Whether this excursion has stood on this floor before.</param>
    /// <param name="VacuumWarned">Whether the dead-air card has already been spent this excursion.</param>
    /// <param name="UnlistedSeen">Whether #592's climax has already been said this excursion.</param>
    /// <param name="ChitBeatSpent">Whether the chit's gate has already been narrated this excursion.</param>
    /// <param name="SeamCrossed">Whether the pour's edge has already been narrated this excursion.</param>
    /// <param name="FoundSeen">Whether the first gallery has already been narrated this excursion.</param>
    /// <param name="ShaftsNarrated">Which bands' gates have already told their card story this excursion.</param>
    public readonly record struct ArrivalMemory(
        bool WasUnderground = false,
        bool FirstSightOfThisFloor = true,
        bool VacuumWarned = false,
        bool UnlistedSeen = false,
        bool ChitBeatSpent = false,
        bool SeamCrossed = false,
        bool FoundSeen = false,
        IReadOnlyCollection<int>? ShaftsNarrated = null);

    /// <summary>
    /// #693 · EVERYTHING THIS ARRIVAL HAS TO SAY, ranked, in narrative order.
    ///
    /// <para>The caller says all of them — the book keeps every one — and lets <see cref="PulseSlot"/> decide
    /// which is on the screen. A caller that reorders this list must get the same line on screen; that is the
    /// law, and it is what makes the ordering comments that used to live in the client unnecessary.</para>
    ///
    /// <para>Empty on a ride to the surface: coming up is the shed, the moon and what you are carrying out of
    /// it, which is one line the client composes out of the satchel it owns.</para>
    /// </summary>
    /// <param name="via">The button that was pressed, when a button was pressed. Only a ride through the
    /// panel can cross a gate — the dev floor cheat rides the same car and has no gate to cross.</param>
    public static IReadOnlyList<Saying> ArrivalSayings(
        string bodyId, int fromLevel, int toLevel, ArrivalMemory memory, LiftStop? via,
        IReadOnlyCollection<string> heldCardIds, IReadOnlyList<Satchel.Item>? carried = null)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(heldCardIds);

        var said = new List<Saying>();
        if (toLevel >= 0)
        {
            return said;
        }

        if (!memory.WasUnderground)
        {
            said.Add(new(ArrivalBeat.Descending, DescendingLine, "\U0001F6C3", PulseRank.Beat));
        }

        // #592 · THE FLOOR THAT IS NOT ON THE PLAN, and the reason this function exists. It says the operation
        // upstairs was enormous, funded, staffed and inspected, and that this was under it, and that the
        // people upstairs did not know. It does not say what it was for. CLIMAX, and it has never once won
        // the slot it was written for.
        if (IsUnlisted(bodyId, toLevel) && !memory.UnlistedSeen)
        {
            said.Add(new(
                ArrivalBeat.Unlisted,
                UnlistedArrivalLine(-DepthOf(bodyId), KindFor(bodyId), KindOn(bodyId, toLevel)),
                "\U0001F573",
                PulseRank.Climax));
        }

        // #677 · NEITHER AIR LINE IS SAID PAST THE SEAM. The pressurised line describes plant and somebody's
        // account decades after the last invoice; said in a gallery it would explain, in one breath, the one
        // thing that must never be explained. The authored arrival line states it once and after that the
        // gauge and the plate answer, which is what instruments are for.
        if (memory.FirstSightOfThisFloor && !IsFound(bodyId, toLevel))
        {
            bool pressurised = HoldsPressure(bodyId, toLevel);
            if (!pressurised && !memory.VacuumWarned)
            {
                // #609 · The first one is a BEAT and it comes with a card: depth is priced in air, and the
                // owner suffocated on B2 finding that out from a toast that had already faded.
                said.Add(new(ArrivalBeat.DeadAirFirst, DeadAirLine, "🫁", PulseRank.Beat));
            }
            else
            {
                // …and every one after it is the weather. Status, which is what it has always been and what
                // it was never allowed to stand on top of.
                said.Add(new(
                    ArrivalBeat.Air, pressurised ? PressurisedLine : DeadAirLine, "🫁",
                    PulseRank.Status));
            }
        }

        // #689 · THE CARD'S FINEST HOUR. A fact about the RIDE rather than about the floor, said once per
        // shaft per excursion, and the beat the owner filed an issue about never seeing.
        if (via is not null
            && GateOpenedByRidingTo(bodyId, fromLevel, toLevel, heldCardIds, carried) is { } opened
            && !(memory.ShaftsNarrated?.Contains(opened.Band) ?? false))
        {
            said.Add(new(
                ArrivalBeat.CardAccepted, CardAcceptedLine(opened), "🎫", PulseRank.Beat, opened));
        }

        // #752 · …and the other paper's arrival, which is the job finishing. A gate that reads a timesheet
        // and waves you through is the least frightening thing this building has done.
        if (via is { OpenedByChit: true } && !memory.ChitBeatSpent)
        {
            said.Add(new(
                ArrivalBeat.ChitGate, CanteenTable.ChitGateLine, CanteenTable.ChitGlyph, PulseRank.Beat));
        }

        // #677 · The seam first, because it happens in the shaft, on the way; then the arrival, because it
        // happens when the doors open. Both authored verbatim, both CLIMAX. Two climaxes in one arrival is
        // allowed and settled by the ordinary tie-break — among equals the last written wins — which is
        // exactly the reading order the owner wrote them in.
        if (IsFound(bodyId, toLevel))
        {
            if (!memory.SeamCrossed)
            {
                said.Add(new(ArrivalBeat.Seam, SeamLine, "\U0001F573", PulseRank.Climax));
            }
            if (!memory.FoundSeen)
            {
                said.Add(new(ArrivalBeat.Found, FoundArrivalLine, "\U0001F573", PulseRank.Climax));
            }
        }

        return said;
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

    /// <summary>#803 · Is this sign the goods hoist's own plate? Asked of the sign for the same reason
    /// <see cref="IsSealedWay"/> is: the client meets these as strings on a console and must never be the
    /// place that decides what one of them IS.</summary>
    public static bool IsFreightShutter(string sign)
    {
        ArgumentNullException.ThrowIfNull(sign);
        return string.Equals(sign, FreightPlate, StringComparison.Ordinal);
    }

    /// <summary>
    /// #803 · Is this sign a ROOM DOOR's plate — one of the words this building paints on a door somebody
    /// shut? Membership in the door vocabularies themselves (<see cref="SignsFor"/>), across every kind of
    /// site, so the answer cannot drift from the list that produced the string.
    ///
    /// <para>Positive recognition on purpose. The three things <see cref="Build"/> hangs a
    /// <see cref="LockedDoor"/> on are a room's plate, a rib's sealed mouth and the hoist's shutter, and
    /// anything asking "what is holding this shut" (#803's designate mode) must be able to say <b>yes, this
    /// is a door with a department on it</b> rather than <b>no, it is not a sealed way</b> — a rule written
    /// as a negation selects every string in the game the day somebody adds a fourth kind.</para></summary>
    public static bool IsDoorSign(string sign)
    {
        ArgumentNullException.ThrowIfNull(sign);
        foreach (Kind kind in Enum.GetValues<Kind>())
        {
            foreach (string word in SignsFor(kind))
            {
                if (string.Equals(word, sign, StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }
        return false;
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
    /// <para>Every seam that opens a card has already run <c>AuthorityCard.TryParse</c> before it asks for a
    /// picture, and an id that fails there gets no card at all rather than a card wearing a stranger's
    /// photograph. This is the face for a seam that has an authority in front of it and cannot roll — the
    /// satchel row already keeps the matching text fallback (<i>"🎫 an authority card"</i>), and the art side
    /// should not have to invent one under pressure. It is deliberately NOT one of the five, so it can never
    /// impersonate an office.</para>
    ///
    /// <para>#684 gave it its first real caller, and for exactly that reason: the panel's read of an EMPTY
    /// wallet is a story card with no card in it. Painting one of the five offices onto that would be the
    /// game showing the captain a pass they do not have.</para></summary>
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
