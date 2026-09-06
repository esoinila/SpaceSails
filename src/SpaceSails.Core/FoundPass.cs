using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #804 · <b>THE FALSE ID — SOMEBODY ELSE'S SITE PASS, FOUND.</b>
///
/// <para>Owner, in the issue's own point 4: <i>"THE BADGE, kept in the Fletch wallet: our own badge once we
/// get a gig, or FALSE IDs we discovered."</i> The first half shipped with the challenge (#804 phase 1,
/// <see cref="PatrolBeat.BadgeIssuedLine"/>). The second half did not, and the reconciliation audit of
/// 2026-09-06 said exactly how it did not:</para>
///
/// <para><i>"<c>WalletChoice.Outcome.WrongSite</c> exists and is judged properly, but nothing authored ever
/// puts a foreign site's pass in the wallet — only a dev cheat. Seed a found pass; the judgement is already
/// written."</i></para>
///
/// <h3>What this file is, and what it very deliberately is not</h3>
///
/// <para><b>It mints nothing new.</b> A false ID in this game is not a forgery and not a purchase — it is a
/// REAL pass, issued by a real site to a real person, that has ended up somewhere it was not meant to be.
/// So the object is <see cref="PatrolBeat.Badge"/> itself, unchanged, with another site's id in it, and
/// every seam it then touches was already written: the wallet fans it
/// (<see cref="WalletChoice.Fan"/>), the man on the rota reads it
/// (<see cref="PatrolBeat.TheGuardReads"/>), the ladder judges it
/// (<see cref="WalletChoice.WhatHappens"/> → <see cref="WalletChoice.Outcome.WrongSite"/> here,
/// <see cref="WalletChoice.Outcome.Worked"/> on its own ground), and the captain's own book files what
/// happened. <b>Nothing about the judgement is touched by this file, and nothing new is added to what a
/// refusal costs.</b></para>
///
/// <para><b>And it is never bought.</b> There is no vendor, no fence and no price. It is a find, dealt by
/// the idioms this ground already deals finds with, which is the whole of the owner's word for it —
/// <i>FALSE IDs we DISCOVERED</i>.</para>
///
/// <h3>Two questions, and they are kept apart on purpose</h3>
///
/// <list type="number">
/// <item><b>IS there one here?</b> — <see cref="RoomFor"/>. A fact about a building, seeded off its own id,
/// so a captain who walks out and comes back finds the same drawer, and a rumour about a site is worth
/// something.</item>
/// <item><b>WHOSE is it?</b> — <see cref="MintedElsewhere"/>. Which site's pass it turns out to be, chosen
/// from the sites this world actually has, because a pass for a building nobody can fly to is a pass that
/// can only ever be refused. The roster is passed IN rather than looked up, exactly as
/// <see cref="SecretLab.MoonWorthLookingAt"/> takes its candidates: Core does not own the list of places a
/// shuttle can reach.</item>
/// </list>
///
/// <para><b>ONE PRODUCER.</b> <see cref="MintedElsewhere"/> is the only thing in the game that puts a pass
/// the captain did not earn into a wallet — the room find and the dev cheat both go through it. Two mints
/// would be two answers to "what is a false ID", and the dev path would drift off the real one the first
/// afternoon somebody tuned either.</para>
/// </summary>
public static class FoundPass
{
    // ── WHERE ONE IS ─────────────────────────────────────────────────────────────────────────────────────
    //
    // The paper idiom, and it is the designation law every other authored find on this ground already keeps
    // (UndergroundComplex.KeyRoomFor and its four siblings): a one-in-N object placed by seeded dice is an
    // object that is silently absent on some worlds FOREVER, with nothing on screen ever saying so. So the
    // RARITY is a fact about the site and the ROOM is designated — a site either keeps one or it does not,
    // and where it does, it is always in the same drawer.

    /// <summary>One site in this many has somebody else's pass lying in it. FLAGGED for the owner's tuning,
    /// and the only rarity number in this file.
    ///
    /// <para>It is a gate on top of the mess floor existing and <see cref="Room"/> being empty rather than
    /// the whole rate, so what a captain actually meets is this number THROUGH the building's own shape and
    /// its own haul roll — measured by <c>TheFalseIdTests</c> rather than assumed, the discipline
    /// <see cref="BlackOpsKey.OneInEligibleHulls"/> keeps.</para></summary>
    public const int OneInSites = 3;

    /// <summary>
    /// Which room of the mess floor it is in.
    ///
    /// <para><b>Room 0, and it is the only index this may safely be</b> — the designation law
    /// <see cref="UndergroundComplex.RelicRoomFor"/> writes out in full: this file is pure of the field, so
    /// it cannot know how many rooms a floor actually has, and the count varies wildly (a generated ground's
    /// mess floor carries seventeen; Enceladus's carries five). Room 0 is the one index every floor has.
    /// A room further along looked safer and was measured: room 5 does not exist on a real scenario body,
    /// so the whole feature would have been silently absent from a shipped moon with every test green.</para>
    ///
    /// <para>It cannot collide with the four other room-0 designations, because none of them is ever on this
    /// floor: #1063's maintenance ledger is on the TOP pressurised floor, #592's Key on the listed bottom,
    /// #614's relic on the unlisted bottom and #677's found key at the unlisted shaft head. Where the mess
    /// floor IS the listed bottom, room 0 there holds that Key rather than nothing, and
    /// <see cref="RoomFor"/> walks past it — the emptiness clause doing the work rather than a second
    /// rule.</para>
    /// </summary>
    public const int Room = 0;

    /// <summary>How far along the mess floor the drawer may be — <see cref="Room"/>, then the next two.
    ///
    /// <para><b>Three, and not more</b>, for <see cref="Room"/>'s own reason: the four-room floor law is
    /// asserted for the scenario's own bodies and a generated site can produce a floor with three, so 0, 1
    /// and 2 are the indices this file may name without a field to count with. It walks them in order and
    /// takes the first the haul table calls empty — which is what makes the drawer FINDABLE on a floor whose
    /// first room happens to hold a crate, instead of the feature quietly not existing there.</para></summary>
    public const int RoomsWalked = 3;

    /// <summary>Does this building keep one at all? A fact about the ground, seeded off its id and nothing
    /// else, so it survives a reload, a re-entry and a rumour.</summary>
    public static bool ASiteKeepsOne(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return DiceRule.Roll(DiceRule.Seed($"false-id:kept:{bodyId}"), OneInSites).Face == 1;
    }

    /// <summary>
    /// <b>WHERE IT IS LYING</b>, or null on the sites that keep none.
    ///
    /// <para><b>THE MESS FLOOR</b> — <see cref="UndergroundComplex.StaffCanteenFloor"/>, the deepest floor
    /// the building admits to that still holds pressure, the one #707 put the staff canteen on. That is where
    /// a wallet gets left: it is the floor the people who work here eat on and keep their lockers on, it
    /// breathes by construction (#608's air law is met without having to be asked — nobody empties their
    /// pockets in a suit), and it is as far from the door strangers walk in through as the building goes.
    /// Null on a site too shallow to have a second canteen, which is honest: a three-floor annex has one
    /// room where people take their coats off and it is the one on the way in.</para>
    ///
    /// <para><b>And only where the room holds nothing else.</b> That is not tidiness, it is the one thing
    /// that keeps this feature from being a second answer to what a room contains: the pass is dealt beside
    /// <see cref="UndergroundComplex.InRoom"/> rather than inside it — the idiom <see cref="OddBooks"/>
    /// already uses on this ground, and for this exact reason — so it may only ever occupy a room the haul
    /// table has already said is empty. A designated room that overwrote a haul would delete a find and
    /// nothing on screen would ever say which one, which is this repo's quietest bug class.</para>
    ///
    /// <para><b>And that clause is stable</b>, which matters more than it looks. Every designation that can
    /// arrive or leave DURING a game — #1063's ledger, #1074's cost-centre papers — sits on the top
    /// pressurised floor, not this one, so no ground grows a paper into this drawer halfway through an
    /// evening and no pass is ever quietly deleted by an office filing a form.</para>
    /// </summary>
    public static (int Level, int RoomIndex)? RoomFor(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        if (!ASiteKeepsOne(bodyId) || UndergroundComplex.StaffCanteenFloor(bodyId) is not { } mess)
        {
            return null;
        }

        for (int room = Room; room < Room + RoomsWalked; room++)
        {
            if (UndergroundComplex.InRoom(bodyId, mess, room) == UndergroundComplex.Haul.Nothing)
            {
                return (mess, room);
            }
        }

        return null;
    }

    /// <summary>Is THIS room the one? Asked by the search, so the client never re-states the designation.</summary>
    public static bool IsHere(string bodyId, int level, int roomIndex) =>
        RoomFor(bodyId) is { } at && at.Level == level && at.RoomIndex == roomIndex;

    // ── WHOSE IT IS ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE ONE PRODUCER.</b> A pass the captain did not earn, minted for a site that is NOT the one they
    /// are standing on — or null when this world has no other site to have issued one.
    ///
    /// <para>Null rather than a pass for here, and that is the load-bearing refusal: a "false ID" that turned
    /// out to be this building's own pass would hand a captain the thing the whole gig is for, and it would
    /// do it silently. Every caller must be able to deal nothing.</para>
    ///
    /// <para><b>THE ROSTER IS SORTED BEFORE IT IS DRAWN FROM.</b> The caller builds its list by appending —
    /// shuttle stops in whatever order the sky is in this afternoon — and a list built by appending is not a
    /// list in order (this repo's fourth named bug class, paid for once already). Unsorted, the same drawer
    /// on the same moon would hold a different site's pass depending on where the ship happened to be
    /// parked.</para>
    /// </summary>
    /// <param name="hereBodyId">The site the pass is being found on. Never the site it is minted for.</param>
    /// <param name="sites">Every site this world has that a captain could stand on. Order does not matter;
    /// duplicates and <paramref name="hereBodyId"/> itself are dropped.</param>
    /// <param name="seed">The find's own seed, so one drawer's answer never depends on another's.</param>
    public static Satchel.Item? MintedElsewhere(string hereBodyId, IReadOnlyList<string>? sites, ulong seed)
    {
        ArgumentNullException.ThrowIfNull(hereBodyId);

        var elsewhere = new List<string>();
        foreach (string body in sites ?? [])
        {
            if (string.IsNullOrEmpty(body)
                || string.Equals(body, hereBodyId, StringComparison.Ordinal)
                || elsewhere.Contains(body))
            {
                continue;
            }
            elsewhere.Add(body);
        }

        if (elsewhere.Count == 0)
        {
            return null;
        }

        elsewhere.Sort(StringComparer.Ordinal);
        return PatrolBeat.Badge(elsewhere[(int)(seed % (ulong)elsewhere.Count)]);
    }

    /// <summary>Is this row a pass for somewhere that is not here? What the wallet already answers at a
    /// challenge (<see cref="WalletChoice.WhatHappens"/>), asked of a row rather than of a read — for the
    /// find itself, which has to know whether it is dealing a false ID or this site's own paper.</summary>
    public static bool IsForElsewhere(Satchel.Item paper, string hereBodyId)
    {
        ArgumentNullException.ThrowIfNull(hereBodyId);
        return paper.Kind == Satchel.Kind.Badge
               && PatrolBeat.SiteOfBadge(paper.Id) is { Length: > 0 } site
               && !string.Equals(site, hereBodyId, StringComparison.Ordinal);
    }

    // ── WHAT IS SAID ABOUT IT ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE PLATE, AND IT IS THE MINTING SITE'S OWN.</b> The glyph a pass wears in a wallet and the face
    /// that is printed on it — <see cref="PatrolBeat.BadgeTitle"/>, #590's grammar — and not one word
    /// besides.
    ///
    /// <para>Nothing is authored here, deliberately. A sentence composed at a find would be a second voice
    /// describing a pass, and the pass already says what it is in the one register that matters: the site
    /// code and the tier a man on a rota is about to read out loud. The captain does the reading, exactly as
    /// they do in the chooser (<see cref="WalletChoice.Claims"/>), which is composed off the same call.</para>
    ///
    /// <para>// FABLE: line needed — the room a working man's old site pass is lying in, and the moment it
    /// goes into the wallet. Until there is one, the plate IS the telling (#528's caption-only idiom), which
    /// is honest and says nothing this feature has not been ruled on.</para>
    /// </summary>
    public static string Plate(Satchel.Item pass) =>
        PatrolBeat.SiteOfBadge(pass.Id) is { Length: > 0 } site
            ? $"{PatrolBeat.BadgeGlyph} {PatrolBeat.BadgeTitle(site)}"
            : $"{PatrolBeat.BadgeGlyph} {WalletChoice.UnreadableFaceLine}";
}
