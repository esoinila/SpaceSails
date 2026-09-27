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
// what the clinic actually does never appears either
// (`TheOldCrewTests.NoGameTextInThisLaneNamesTheThing` holds both).
// ─────────────────────────────────────────────────────────────────────────────────────────────────

/// <summary>#973 L5a · The pool of old shipmates, the bonds-before-play table, the postings, and every
/// word any of them says. Pure and deterministic from the thread seed on the shared dice — the same
/// universe always seeds the same four people with the same history between them.</summary>
public static partial class OldCrew
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

    /// <summary>#973 L5b · THE DECENT SHIP, by the name she had before they took her. Bible only (owner
    /// ruling §6): a survey tender on the KAAMOS supply chain, impounded and renamed after the crew opened a
    /// pod. She is named in the photograph's own line and, now, in the one job that goes looking for her —
    /// one constant, so the sheet in the book and the row in the ledger cannot spell her two ways. What was
    /// in the pods is still never said (the Reever law).</summary>
    public const string TheDecentShip = "HALCYON REACH";

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
