namespace SpaceSails.Core;

// ─────────────────────────────────────────────────────────────────────────────────────────────────
// #973 L5a · THE OLD CREW — the people who knew the face before.
//
// `CaptainSuccession` gives every rebirth a new name and a new face. Until this lane that was a rule
// about paperwork. The old crew make it SOCIAL: somebody who served with the captain is the only kind
// of person who can say *you look different*, and the only kind who can hold up a picture proving the
// captain used to be somebody else's shipmate.
//
// BONDS BEFORE PLAY (owner ruling 2026-08-23 §10, the Fail Forward table adopted whole). Per game
// thread the history-between table is rolled FIRST — one bond to the captain and one to another seeded
// shipmate — and only THEN are the shipmates posted to their places. That order is not decoration and
// it is guarded: a posting is a function of the ROLE, so rolling the postings first would mean the
// place decided who the person was, which is the wrong way round for a game that wants the player to
// walk into a room already knowing what is in it.
//
// WHAT IS NOT IN THIS FILE, by law. The bible's account of the decent ship — what she carried, who
// opened a pod, what was inside — is WRITERS' BIBLE and appears in no game text anywhere. The word for
// what the clinic actually does never appears either (`NoGameTextNamesTheThingTests` holds both).
// ─────────────────────────────────────────────────────────────────────────────────────────────────

/// <summary>#973 L5a · The pool of old shipmates, the bonds-before-play table, the postings, and every
/// word any of them says. Pure and deterministic from the thread seed on the shared dice — the same
/// universe always seeds the same four people with the same history between them.</summary>
public static class OldCrew
{
    // ── §1 · THE POOL ────────────────────────────────────────────────────────────────────────────────

    /// <summary>What one shipmate was to the captain. The captain-bond, and the only one of the two bonds
    /// that is fixed rather than rolled: it is who they were on the ship, and the ship is over.</summary>
    public enum Role
    {
        /// <summary>The sparks that the service's fraternization rule forbade.</summary>
        TheFling,

        /// <summary>The one who did not sign either — or did. The seed decides; the reveal is the scene.</summary>
        TheBestFriend,

        /// <summary>The one who signed the manifest the captain would not. Always seeded.</summary>
        TheSigner,

        /// <summary>You covered for her, and she has not forgotten it.</summary>
        TheOneWhoOwesYou,

        /// <summary>He covered a debt of yours, and he has not forgotten it either.</summary>
        TheOneYouOwe,

        /// <summary>The one who wanted your berth and says so.</summary>
        TheRival,

        /// <summary>The one who covered for you. Dead, and filed.</summary>
        TheOneWhoCoveredForYou,
    }

    /// <summary>The kind of place a shipmate ended up working — the owner's <i>"they work where they know
    /// things"</i>. A kind, never a station: the concrete berth is rolled per thread from the live world.</summary>
    public enum PlaceKind
    {
        /// <summary>A Nebula Mutual claims desk. Great ports only — the desks are where the traffic is.</summary>
        NebulaClaimsDesk,

        /// <summary>A port registrar's office: who a hull is, and who it used to be.</summary>
        PortRegistrar,

        /// <summary>A customs post. Cargo, manifests, and who signed them.</summary>
        CustomsPost,

        /// <summary>A clinic clerk's counter — the second page of everything.</summary>
        ClinicClerk,

        /// <summary>Behind the taps at a working berth.</summary>
        WorkingBerthBar,

        /// <summary>Second officer on a registry cutter, which is a berth that moves.</summary>
        RegistryCutter,
    }

    /// <summary>One bond on the history-between table (the Fail Forward pre-game roll). The captain-bond is
    /// the shipmate's <see cref="Role"/>; the shipmate-to-shipmate bond is rolled from this list.</summary>
    public enum BondKind
    {
        /// <summary>There were sparks, and the rule said no.</summary>
        TheFling,

        /// <summary>The one you told things to.</summary>
        TheBestFriend,

        /// <summary>The one who wanted what you had.</summary>
        TheRival,

        /// <summary>The one who owes.</summary>
        Owes,

        /// <summary>The one who is owed.</summary>
        Owed,

        /// <summary>The one who signed.</summary>
        Signed,

        /// <summary>The one who covered.</summary>
        Covered,
    }

    /// <summary>The seven names of the history-between table, in the order the bible lists them. These are
    /// the words the black book prints, so they are written once, here.</summary>
    public static string Name(BondKind bond) => bond switch
    {
        BondKind.TheFling => "the fling",
        BondKind.TheBestFriend => "the best friend",
        BondKind.TheRival => "the rival",
        BondKind.Owes => "the one who owes you",
        BondKind.Owed => "the one you owe",
        BondKind.Signed => "the one who signed",
        _ => "the one who covered for you",
    };

    /// <summary>The same seven words, asked of a role — a shipmate's bond TO THE CAPTAIN is their role, and
    /// the book prints it in exactly the same voice as the other one.</summary>
    public static string Name(Role role) => Name(AsBond(role));

    /// <summary>A role read as a bond, so the captain-bond and the shipmate-bond are one vocabulary and
    /// cannot drift into two.</summary>
    public static BondKind AsBond(Role role) => role switch
    {
        Role.TheFling => BondKind.TheFling,
        Role.TheBestFriend => BondKind.TheBestFriend,
        Role.TheSigner => BondKind.Signed,
        Role.TheOneWhoOwesYou => BondKind.Owes,
        Role.TheOneYouOwe => BondKind.Owed,
        Role.TheRival => BondKind.TheRival,
        _ => BondKind.Covered,
    };

    /// <summary>One name out of the pool of seven. <paramref name="Living"/> is false for the one who is
    /// dead and filed — he is a face on the photograph and a name in the rep's file, and never a contact.
    /// <paramref name="Warmth"/> is the goodwill a shipmate starts with, which is small and depends only on
    /// what they were to the captain.</summary>
    public readonly record struct Shipmate(
        string Id, string Name, string Short, Role Role, PlaceKind Posting, bool Living, int Warmth);

    /// <summary>The id a shipmate's row is filed under in the <see cref="ContactLedger"/>. Prefixed for the
    /// reason <see cref="IllegalHeat.LedgerPrefix"/> is: one book holding two unrelated kinds of
    /// relationship under one key would be a keyspace collision waiting to be a bug.</summary>
    public const string LedgerPrefix = "crew:";

    /// <summary>Where one shipmate's history with the captain is filed.</summary>
    public static string LedgerId(string shipmateId) => LedgerPrefix + shipmateId;

    /// <summary>Is this row of the contacts book an old shipmate rather than a fixer or an outfit?</summary>
    public static bool IsAnOldShipmate(string? contactId) =>
        contactId is not null && contactId.StartsWith(LedgerPrefix, StringComparison.Ordinal);

    /// <summary>The one who signed. Always seeded (owner ruling §8) — the arc has to have somebody in it who
    /// did the thing the captain would not do.</summary>
    public const string SignerId = "corwin";

    /// <summary>The one who is dead and filed. Never a contact; a face on the photograph.</summary>
    public const string DeadId = "hollis";

    /// <summary>The fling.</summary>
    public const string FlingId = "ilse";

    /// <summary>The best friend.</summary>
    public const string BestFriendId = "teo";

    /// <summary>The seven, in the bible's order. Order is part of the save-compatible identity of a
    /// seeding: changing it re-casts every universe.</summary>
    public static IReadOnlyList<Shipmate> Pool { get; } =
    [
        new(FlingId, "Ilse Marrow", "Ilse", Role.TheFling, PlaceKind.NebulaClaimsDesk, true, 3),
        new(BestFriendId, "Teodor \"Teo\" Brask", "Teo", Role.TheBestFriend, PlaceKind.PortRegistrar, true, 4),
        new(SignerId, "Corwin Sallis", "Corwin", Role.TheSigner, PlaceKind.CustomsPost, true, 1),
        new("maren", "Maren Okafor", "Maren", Role.TheOneWhoOwesYou, PlaceKind.ClinicClerk, true, 3),
        new("pell", "Pell Andrade", "Pell", Role.TheOneYouOwe, PlaceKind.WorkingBerthBar, true, 2),
        new("dagny", "Dagny Voss", "Dagny", Role.TheRival, PlaceKind.RegistryCutter, true, 0),
        new(DeadId, "Hollis Grey", "Hollis", Role.TheOneWhoCoveredForYou, PlaceKind.CustomsPost, false, 0),
    ];

    /// <summary>One of the seven by id, or null for a name this build has never heard of.</summary>
    public static Shipmate? ById(string? id)
    {
        foreach (Shipmate s in Pool)
        {
            if (string.Equals(s.Id, id, StringComparison.Ordinal))
            {
                return s;
            }
        }

        return null;
    }

    /// <summary>How many shipmates a thread seeds (owner ruling §8: four, one of them always the signer).</summary>
    public const int SeededPerThread = 4;

    // ── §2 · THE HISTORY-BETWEEN TABLE, ROLLED FIRST ─────────────────────────────────────────────────

    /// <summary>
    /// One seeded shipmate: who they are, the bond they hold to another seeded shipmate, and — filled in by
    /// <see cref="Post"/> afterwards, never before — the berth they are posted to.
    /// </summary>
    /// <param name="Id">Which of the <see cref="Pool"/>.</param>
    /// <param name="ToCaptain">Their bond to the captain. Fixed: it is their role.</param>
    /// <param name="BondToId">The other seeded shipmate this one has history with.</param>
    /// <param name="Bond">What that history is.</param>
    /// <param name="StationId">The berth they work at — empty until the postings are rolled.</param>
    public readonly record struct Seeded(
        string Id, BondKind ToCaptain, string BondToId, BondKind Bond, string StationId)
    {
        /// <summary>The one line the black book shows beside this name from the first meeting — the bond to
        /// the captain and the bond to the other one, in the table's own words. The player, like a Fail
        /// Forward player, reads it BEFORE the scene and plays into it.</summary>
        public string History() =>
            IsTheClassic
                ? $"{OldCrew.Name(ToCaptain)} · now with {ById(BondToId)?.Short ?? BondToId}"
                : $"{OldCrew.Name(ToCaptain)} · {OldCrew.Name(Bond)}: {ById(BondToId)?.Name ?? BondToId}";

        /// <summary>THE CLASSIC (owner, addendum 2): the best friend ended up with the fling. It is the one
        /// bond the table is constrained to produce often, and it is the one the book announces in plain
        /// words rather than in the table's vocabulary — because <i>now with Ilse</i> is what a person would
        /// actually say, and it is what the captain would actually hear.</summary>
        public bool IsTheClassic =>
            (Id == BestFriendId && BondToId == FlingId && Bond == BondKind.TheFling)
            || (Id == FlingId && BondToId == BestFriendId && Bond == BondKind.TheBestFriend);
    }

    /// <summary>How many seedings in eight are forced to be THE CLASSIC — the fling and the best friend cast
    /// together and bound to each other. The owner's constraint is "at least one seeding in three"; this is
    /// the knob that buys it, and it is a deliberate roll rather than a hope, because a uniform cast draw
    /// lands the pair together in exactly three seedings in ten and would sit on the floor of the band.
    /// FLAGGED for the owner's tuning.</summary>
    public const int TheClassicInEight = 3;

    /// <summary>
    /// THE CAST AND THE BONDS, in that order and in one roll. The signer is always in it (owner ruling §8);
    /// the other three are drawn from the five living names beside him; and then every one of them is given
    /// a bond to another one of them.
    ///
    /// <para>Nothing here knows anything about berths. That is the whole of the bonds-before-postings law:
    /// this function cannot be influenced by where anybody works, because it is not told.</para>
    /// </summary>
    public static IReadOnlyList<Seeded> Bonds(string threadId)
    {
        ArgumentNullException.ThrowIfNull(threadId);

        // THE CLASSIC first, because it decides the cast as well as the bond: you cannot bind two people
        // who were not both invited.
        bool classic = DiceRule.Roll(DiceRule.Seed($"oldcrew|classic|{threadId}"), 8).Face <= TheClassicInEight;

        var cast = new List<string> { SignerId };
        var others = new List<string>();
        foreach (Shipmate s in Pool)
        {
            if (s.Living && s.Id != SignerId)
            {
                others.Add(s.Id);
            }
        }

        if (classic)
        {
            cast.Add(FlingId);
            cast.Add(BestFriendId);
            others.Remove(FlingId);
            others.Remove(BestFriendId);
        }

        // Draw the rest without replacement off the shared dice, one face at a time — the house idiom, and
        // the reason two threads with adjacent ids do not cast the same four people.
        int draw = 0;
        while (cast.Count < SeededPerThread && others.Count > 0)
        {
            int face = DiceRule.Roll(DiceRule.Seed($"oldcrew|cast|{threadId}", draw++), others.Count).Face;
            cast.Add(others[face - 1]);
            others.RemoveAt(face - 1);
        }

        // Keep the cast in POOL ORDER so a seeding reads the same however the dice happened to draw it.
        var ordered = new List<string>();
        foreach (Shipmate s in Pool)
        {
            if (cast.Contains(s.Id))
            {
                ordered.Add(s.Id);
            }
        }

        var seeded = new List<Seeded>();
        foreach (string id in ordered)
        {
            Shipmate who = ById(id)!.Value;

            // The classic is written in rather than rolled — that is what forcing it means. Both ends carry
            // it, so the book says the same thing whichever door the captain knocks on first.
            if (classic && id == BestFriendId)
            {
                seeded.Add(new Seeded(id, AsBond(who.Role), FlingId, BondKind.TheFling, ""));
                continue;
            }

            if (classic && id == FlingId)
            {
                seeded.Add(new Seeded(id, AsBond(who.Role), BestFriendId, BondKind.TheBestFriend, ""));
                continue;
            }

            // Everyone else: one bond to another seeded shipmate, both the partner and the kind rolled.
            var candidates = new List<string>();
            foreach (string other in ordered)
            {
                if (other != id)
                {
                    candidates.Add(other);
                }
            }

            string partner = candidates[
                DiceRule.Roll(DiceRule.Seed($"oldcrew|bond-who|{threadId}|{id}"), candidates.Count).Face - 1];
            IReadOnlyList<BondKind> kinds = BondsAvailableAbout(partner);
            BondKind kind = kinds[
                DiceRule.Roll(DiceRule.Seed($"oldcrew|bond-what|{threadId}|{id}"), kinds.Count).Face - 1];
            seeded.Add(new Seeded(id, AsBond(who.Role), partner, kind, ""));
        }

        return Symmetrical(seeded);
    }

    /// <summary>
    /// #973 L3 · <b>THE ONE WHO SIGNED IS ONE MAN.</b> The shipmate-to-shipmate bond names what the OTHER
    /// person is to this one (<see cref="Seeded.History"/> reads <i>the fling · the best friend: Teo</i>), so
    /// the bond <see cref="BondKind.Signed"/> is a sentence about the person it POINTS AT — and there is
    /// exactly one man in this world it can truthfully point at. Corwin Sallis signed the manifest the captain
    /// would not; a second name wearing that bond would be the book telling the player, in its own vocabulary,
    /// that somebody else did the thing the whole arc turns on.
    ///
    /// <para>Carried by the DRAW rather than by a post-hoc fix-up: a rule that rolled the wrong answer and
    /// then corrected it would put a bias nobody wrote into whatever kind it corrected TO. So the kind is
    /// simply not in the bag unless the man it is about is on the other end of the bond — and, because the
    /// bag is only short by one when he is not, a thread whose bond does point at him rolls exactly the roll
    /// it always rolled (the L5a carry-over the crew flagged; every other bond kind is legal about anybody).</para>
    /// </summary>
    public static IReadOnlyList<BondKind> BondsAvailableAbout(string? partnerId)
    {
        var kinds = new List<BondKind>();
        foreach (BondKind k in Enum.GetValues<BondKind>())
        {
            if (k == BondKind.Signed && !string.Equals(partnerId, SignerId, StringComparison.Ordinal))
            {
                continue;
            }

            kinds.Add(k);
        }

        return kinds;
    }

    /// <summary>
    /// THE CLASSIC IS A FACT ABOUT TWO PEOPLE, so it is written on both of them. The forced seeding writes
    /// both ends already; a free roll can land one end of it by luck (his bond comes up <i>the fling ·
    /// Ilse</i> and hers is pointed elsewhere), and a book that said <i>now with Ilse</i> on his page and
    /// something else entirely on hers would be the sim doing one thing while a SENTENCE reported another —
    /// which is a named bug class in this house, not a rough edge.
    /// </summary>
    private static IReadOnlyList<Seeded> Symmetrical(List<Seeded> seeded)
    {
        bool classic = false;
        foreach (Seeded s in seeded)
        {
            classic |= s.IsTheClassic;
        }

        if (!classic)
        {
            return seeded;
        }

        for (int i = 0; i < seeded.Count; i++)
        {
            if (seeded[i].Id == BestFriendId)
            {
                seeded[i] = seeded[i] with { BondToId = FlingId, Bond = BondKind.TheFling };
            }
            else if (seeded[i].Id == FlingId)
            {
                seeded[i] = seeded[i] with { BondToId = BestFriendId, Bond = BondKind.TheBestFriend };
            }
        }

        return seeded;
    }

    /// <summary>True when this thread's table produced the classic — the fling and the best friend cast
    /// together and bound to each other. Read off the rolled bonds rather than re-rolled, so there is one
    /// answer to the question in the whole game.</summary>
    public static bool TheClassicHappened(IReadOnlyList<Seeded> seeded)
    {
        ArgumentNullException.ThrowIfNull(seeded);
        foreach (Seeded s in seeded)
        {
            if (s.IsTheClassic)
            {
                return true;
            }
        }

        return false;
    }

    // ── §3 · THE POSTINGS, ROLLED SECOND ─────────────────────────────────────────────────────────────

    /// <summary>One berth the world offers, as the postings see it: an id and how big the place is. The
    /// client builds this list off the live ephemeris (<see cref="BerthsOf"/>); a test builds it by hand,
    /// which is the whole reason the postings take a list rather than a world.</summary>
    public readonly record struct Berth(string Id, ArrivalTube.Tier Tier);

    /// <summary>Every dockable berth in the scenario with the tier it has earned — the list the postings are
    /// drawn from.</summary>
    public static IReadOnlyList<Berth> BerthsOf(ICelestialEphemeris ephemeris)
    {
        ArgumentNullException.ThrowIfNull(ephemeris);
        var berths = new List<Berth>();
        foreach (CelestialBody body in DockableHavens.All(ephemeris))
        {
            berths.Add(new Berth(body.Id, ArrivalTube.TierFor(ephemeris, body.Id)));
        }

        return berths;
    }

    /// <summary>Which berths a place of this kind can exist at. A Nebula claims desk is at a GREAT PORT and
    /// nowhere else (the desks are where the traffic is); a working berth's bar is at a working berth; every
    /// other office will take whatever the system has. A kind whose tier the scenario does not contain falls
    /// back to the whole list rather than leaving a shipmate unposted — a person with no job is not a
    /// contact, and a seeding that silently dropped one would be four people three of the time.</summary>
    public static IReadOnlyList<Berth> BerthsFor(PlaceKind kind, IReadOnlyList<Berth> berths)
    {
        ArgumentNullException.ThrowIfNull(berths);
        ArrivalTube.Tier? want = kind switch
        {
            PlaceKind.NebulaClaimsDesk => ArrivalTube.Tier.GreatPort,
            PlaceKind.WorkingBerthBar => ArrivalTube.Tier.WorkingBerth,
            _ => null,
        };

        if (want is not { } tier)
        {
            return berths;
        }

        var matching = new List<Berth>();
        foreach (Berth b in berths)
        {
            if (b.Tier == tier)
            {
                matching.Add(b);
            }
        }

        return matching.Count > 0 ? matching : berths;
    }

    /// <summary>
    /// POST THEM. Called with the bonds ALREADY ROLLED, and that is the law this lane guards: a berth is
    /// chosen from the shipmate's <see cref="PlaceKind"/>, which comes from their <see cref="Role"/>, which
    /// the table decided one step earlier. Nothing about a berth ever reaches back into who somebody was.
    ///
    /// <para>The one arrangement that is not a roll: when the table produced THE CLASSIC, the fling is
    /// posted where the best friend is. She moved to be with him, which is the fact the whole beat is about
    /// — and it is what makes knocking on his door a thing the captain can actually do.</para>
    /// </summary>
    public static IReadOnlyList<Seeded> Post(
        string threadId, IReadOnlyList<Seeded> seeded, IReadOnlyList<Berth> berths)
    {
        ArgumentNullException.ThrowIfNull(threadId);
        ArgumentNullException.ThrowIfNull(seeded);
        ArgumentNullException.ThrowIfNull(berths);
        if (berths.Count == 0)
        {
            return seeded;
        }

        var posted = new List<Seeded>();
        string herBerth = "";
        foreach (Seeded s in seeded)
        {
            Shipmate who = ById(s.Id)!.Value;
            IReadOnlyList<Berth> choices = BerthsFor(who.Posting, berths);
            string at = choices[
                (int)(DiceRule.Seed($"oldcrew|post|{threadId}|{s.Id}") % (ulong)choices.Count)].Id;
            if (s.Id == FlingId)
            {
                herBerth = at;
            }

            posted.Add(s with { StationId = at });
        }

        // …and HE is the one who moved. A registrar's office exists at a great port as readily as anywhere
        // else, while a Nebula claims desk does not exist at a working berth at all — so moving her to him
        // would have broken the one posting rule the world actually asserts, and moving him to her breaks
        // nothing. The fiction is the better way round too: she took the desk, and he followed.
        if (herBerth.Length > 0)
        {
            for (int i = 0; i < posted.Count; i++)
            {
                if (posted[i].Id == BestFriendId && posted[i].IsTheClassic)
                {
                    posted[i] = posted[i] with { StationId = herBerth };
                }
            }
        }

        return posted;
    }

    /// <summary>The whole seeding in the one order it is allowed to happen in: bonds, then postings.</summary>
    public static IReadOnlyList<Seeded> Seed(string threadId, IReadOnlyList<Berth> berths) =>
        Post(threadId, Bonds(threadId), berths);

    // ── §4 · THE SIGNER REPORTS ──────────────────────────────────────────────────────────────────────

    /// <summary>What one meeting with the man who signed costs, in heat owed to the port authority of the
    /// station he works at. One unit: he does not denounce you, he files. FLAGGED for the owner's tuning.</summary>
    public const int SignerReportUnits = 1;

    /// <summary>
    /// #715's charge for a meeting the signer was in the room for. It is owed to the authority of THAT
    /// station and to nobody else, which is the whole of the #715 law: a customs man who reports you has
    /// told his own port, and a port that told the others would be admitting what it keeps in its basement.
    ///
    /// <para>Built through <see cref="SiteOperator.Of"/> and handed to <see cref="IllegalHeat.Bank"/> like
    /// every other crossing — no second banking path, no second key.</para>
    /// </summary>
    public static UndergroundComplex.HeatCharge SignerReport(string stationId)
    {
        ArgumentNullException.ThrowIfNull(stationId);
        return new UndergroundComplex.HeatCharge(SiteOperator.Of(stationId).Id, SignerReportUnits);
    }

    /// <summary>Is the man who signed at this berth? The question the drink's modifier and the report both
    /// ask, asked once so they cannot disagree about the room they are in.</summary>
    public static bool SignerIsAt(IReadOnlyList<Seeded> seeded, string? stationId) =>
        IsAt(seeded, SignerId, stationId);

    /// <summary>Is she at this berth?</summary>
    public static bool FlingIsAt(IReadOnlyList<Seeded> seeded, string? stationId) =>
        IsAt(seeded, FlingId, stationId);

    /// <summary>Is this shipmate posted at this berth right now?</summary>
    public static bool IsAt(IReadOnlyList<Seeded> seeded, string shipmateId, string? stationId)
    {
        ArgumentNullException.ThrowIfNull(seeded);
        if (string.IsNullOrEmpty(stationId))
        {
            return false;
        }

        return Find(seeded, shipmateId) is { } s && string.Equals(s.StationId, stationId, StringComparison.Ordinal);
    }

    /// <summary>Every shipmate posted at this berth, in pool order — who the captain can walk in on.</summary>
    public static IReadOnlyList<Seeded> At(IReadOnlyList<Seeded> seeded, string? stationId)
    {
        ArgumentNullException.ThrowIfNull(seeded);
        if (string.IsNullOrEmpty(stationId))
        {
            return [];
        }

        var here = new List<Seeded>();
        foreach (Seeded s in seeded)
        {
            if (string.Equals(s.StationId, stationId, StringComparison.Ordinal))
            {
                here.Add(s);
            }
        }

        return here;
    }

    /// <summary>#973 L5a · <b>THE DOOR YOU STAND OUTSIDE OF.</b> True when the black book already says the
    /// best friend is with the fling AND both of them are at this berth — the moment the owner's addendum 2
    /// is about, and the only moment the knock costs a nerve pip.</summary>
    public static bool KnockingCostsNerve(IReadOnlyList<Seeded> seeded, string? stationId) =>
        Find(seeded, BestFriendId) is { IsTheClassic: true }
        && IsAt(seeded, BestFriendId, stationId)
        && IsAt(seeded, FlingId, stationId);

    /// <summary>The seeded row for one shipmate id, or null when this thread did not cast them.</summary>
    public static Seeded? Find(IReadOnlyList<Seeded> seeded, string? id)
    {
        ArgumentNullException.ThrowIfNull(seeded);
        foreach (Seeded s in seeded)
        {
            if (string.Equals(s.Id, id, StringComparison.Ordinal))
            {
                return s;
            }
        }

        return null;
    }

    // ── §5 · WHAT THE VAULT CARRIES ──────────────────────────────────────────────────────────────────

    /// <summary>One seeded row, as the file stores it: the FACT (who, bound how to whom, posted where) and
    /// never a word of the prose the book prints about it.</summary>
    public static string Stored(Seeded s) =>
        $"{s.Id}|{(int)s.ToCaptain}|{s.BondToId}|{(int)s.Bond}|{s.StationId}";

    /// <summary>Read one back. A row this build cannot parse — an id retired from the pool, an enum from a
    /// future build — is dropped rather than thrown over, and a seeding that comes back short is re-rolled
    /// from the thread id, which costs nothing because the roll is deterministic.</summary>
    public static bool TryParse(string? stored, out Seeded seeded)
    {
        seeded = default;
        if (string.IsNullOrEmpty(stored))
        {
            return false;
        }

        string[] p = stored.Split('|', 5);
        if (p.Length != 5
            || ById(p[0]) is null
            || !int.TryParse(p[1], out int toCaptain) || !Enum.IsDefined((BondKind)toCaptain)
            || !int.TryParse(p[3], out int bond) || !Enum.IsDefined((BondKind)bond))
        {
            return false;
        }

        seeded = new Seeded(p[0], (BondKind)toCaptain, p[2], (BondKind)bond, p[4]);
        return true;
    }
}
