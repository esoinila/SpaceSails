using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #588 · WHOSE KIT IS THIS? — the stranger who assembles out of the things they left behind.
///
/// <para>Owner, 2026-08-01: <i>"when we find somebody's kit maybe we get gen ai compilation of what we
/// discover about them... nice place for world building and dropping bread crumbs about our big plot ... like
/// some top notch scientist being found on far away moon .... what were they doing there... maybe they are in
/// photos posing with important politicians and officials :-D"</i></para>
///
/// <para>And, in the same breath, the other half — <i>"In leisure suit larry game there was a tip to go to a
/// door and say Ken sent me to get into exclusive club ... we could also find tips like that. Ins to people
/// and places."</i></para>
///
/// <para>Those are one feature. A dead stranger's effects are only interesting if they <b>give you
/// something</b>, and the best thing they can give you is a NAME TO DROP — a door somewhere else in the
/// game that opens because of what you found on a moon. That is what turns a ruin from a loot box into a
/// thread, and it is the single cheapest way to make the ground and the bar feel like the same world.</para>
///
/// <h3>The canon rule this file must never break</h3>
/// <para>The Old Ones are failed restores, procured at scale by a Minister of Labor for cheap obedient
/// labour; KAAMOS <i>is</i> the procurement. That is canon and it is <b>never stated in a card and never
/// confirmed by a sensor</b> (owner ruling, 2026-07-30). So a dossier may show you a continuity researcher
/// photographed shaking hands with a ministry delegation at the opening of a facility that appears on no
/// register — and it may never tell you what the facility was for. The player assembles the horror or does
/// not. Every line here is a FACT ABOUT A PERSON, never an explanation.</para>
/// </summary>
public static class FieldDossier
{
    /// <summary>How many pieces of somebody's kit must be found before the picture comes together. Three is
    /// deliberate: one is litter, two is a coincidence, three is a person.</summary>
    public const int FragmentsToAssemble = 3;

    /// <summary>The compiled effects, as the card shows them.</summary>
    public const string ArtUrl = "art/dossier-effects.jpg";

    /// <summary>The label on the card.</summary>
    public const string ConsoleLabel = "🗂 WHOSE KIT WAS THIS";

    // ── The person ──────────────────────────────────────────────────────────────────────────────────────

    private static readonly string[] Given =
    [
        "Ilse", "Marek", "Yevgenia", "Tobias", "Ama", "Rúna", "Piotr", "Sunniva",
        "Dmitri", "Halla", "Casimir", "Nkechi", "Wen", "Astrid", "Oleg", "Beatriz",
    ];

    private static readonly string[] Family =
    [
        "Vandermeer", "Okonkwo", "Halvorsen", "Brandt", "Sarkisyan", "Duclos", "Nyström",
        "Petrosyan", "Aaltonen", "Meireles", "Zeeman", "Kowalczyk", "Iversen", "Rasmussen",
    ];

    /// <summary>What they did. Every one of these is a real discipline with an ugly application, and none of
    /// them names the application — that is the whole register.</summary>
    private static readonly string[] Discipline =
    [
        "continuity engineering",
        "neural cartography",
        "cryogenic tissue recovery",
        "cognitive fidelity assurance",
        "long-storage biostasis",
        "pattern integrity审 review",
        "post-transfer rehabilitation",
        "occupational psychometrics",
    ];

    /// <summary>Who signed their movement orders. Institutional, bureaucratic, and never explained.</summary>
    private static readonly string[] Employer =
    [
        "a directorate that files under Labour",
        "a contractor billing through three shells",
        "an institute with one registered address and no windows",
        "a ministry sub-office that ceased to exist the year after",
        "a charitable foundation with an unusually large transport budget",
    ];

    /// <summary>The person these effects belonged to. Seeded on the exact ruin, so a given room on a given
    /// site always held the same stranger — and two captains comparing notes agree.</summary>
    public static Person Who(string bodyId, string siteSalt, int roomIndex)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(siteSalt);

        string tag = $"dossier:{bodyId}:{siteSalt}:{roomIndex}";
        string name = $"{Pick(Given, tag + ":given")} {Pick(Family, tag + ":family")}";
        return new Person(
            name,
            Pick(Discipline, tag + ":discipline"),
            Pick(Employer, tag + ":employer"));
    }

    /// <summary>A stranger reconstructed from their kit.</summary>
    public readonly record struct Person(string Name, string Discipline, string Employer);

    /// <summary>The assembled card. Facts, in the order a captain would work them out — the kit first, the
    /// person second, the company they kept last, because that is the beat that lands.</summary>
    public static string Compiled(in Person who, string placeLabel)
    {
        ArgumentNullException.ThrowIfNull(placeLabel);

        return
            $"You lay the pieces out on a flat rock, the way you were taught to, and a person comes together.\n\n" +
            $"**{who.Name}.** A specialist in {who.Discipline}, carried here by {who.Employer}. The lanyard is " +
            "expired by a margin that would embarrass anybody, and it was still being worn.\n\n" +
            "The slate still lights. Most of it is gone, but a picture survives: a row of dark suits and one " +
            "lab coat, hands stacked over a ribbon, all of them smiling for somebody's opening ceremony. The " +
            "coat is theirs. The suits are not the sort of people who attend an opening on a moon nobody can " +
            "name, and the bulkhead behind them is not the sort of bulkhead you cut a ribbon in front of.\n\n" +
            "Under the slate, face down, a photograph of a child. Paper. Somebody had it printed, which costs " +
            $"money out here, and carried it to {placeLabel}, which cost rather more.\n\n" +
            "You put that one back the way you found it.";
    }

    // ── The in ──────────────────────────────────────────────────────────────────────────────────────────
    //
    // Owner: "In leisure suit larry game there was a tip to go to a door and say Ken sent me to get into
    // exclusive club ... we could also find tips like that. Ins to people and places."

    /// <summary>A name to drop, found among somebody's effects. The point is that it is USABLE somewhere the
    /// captain has not been yet — a ruin that hands you a phrase is a thread out of the ruin, which is a very
    /// different object from a ruin that hands you forty credits.</summary>
    public readonly record struct Introduction(string Phrase, string Whom, string Where, string Note);

    private static readonly (string Whom, string Where)[] Doors =
    [
        ("the barkeep at The Rusty Roadstead", "The Rusty Roadstead"),
        ("the night clerk at Selene Gate", "Selene Gate"),
        ("the yard foreman at Highport Satellite Works", "Highport Satellite Works"),
        ("the quiet man in the back at Ringside Exchange", "Ringside Exchange"),
        ("the harbourmaster's second at The Tilt", "The Tilt"),
        ("whoever is holding the desk at The Deep", "The Deep"),
    ];

    /// <summary>How the captain is told to use it. Deliberately in the owner's register: a phrase, a person,
    /// and no explanation of why it works.</summary>
    private static readonly string[] Framing =
    [
        "Say it exactly. Do not explain it, and do not answer the question they ask after.",
        "It is not a password, it is a name, and the difference matters to the person hearing it.",
        "Whoever wrote this underlined it twice, which is either emphasis or a warning.",
        "It was written on the inside of a cuff. People write on the inside of a cuff when they expect to be searched.",
    ];

    /// <summary>The introduction hidden in this stranger's kit — a name, and where it opens something.</summary>
    public static Introduction InTheKit(string bodyId, string siteSalt, int roomIndex)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(siteSalt);

        string tag = $"intro:{bodyId}:{siteSalt}:{roomIndex}";
        Person who = Who(bodyId, siteSalt, roomIndex);
        (string whom, string where) = Doors[Index(Doors.Length, tag + ":door")];

        // The phrase is the DEAD PERSON'S OWN NAME. That is the good version: the in you are carrying is a
        // name that used to belong to somebody, you know exactly how they ended up, and the person you say it
        // to does not. Nothing in the game ever remarks on this.
        return new Introduction(
            $"\"{who.Name} sent me.\"",
            whom,
            where,
            Pick(Framing, tag + ":framing"));
    }

    /// <summary>The line the captain reads when the in falls out of somebody's kit.</summary>
    public static string IntroductionLine(in Introduction intro) =>
        $"🎟 A name, and where to spend it: {intro.Phrase} — to {intro.Whom}, at {intro.Where}. {intro.Note}";

    // ── The people who are still waiting ────────────────────────────────────────────────────────────────
    //
    // Owner, immediately after the "Ken sent me" idea and much better than it: "Like if we know what happened
    // to someone we get contacts easily by contacting their loved ones, in some cases that might lead our
    // gum-shoe-efforts forward."
    //
    // This is the version worth building. Dropping a dead person's name at a door is a joke (a good one, kept
    // above). Being the one who can finally TELL SOMEBODY what happened is a relationship — and it is the only
    // currency a stranger's family would ever accept. It also turns the whole ground-search loop into a
    // detective engine: a ruin gives you a person, a person gives you somebody who has been waiting years for
    // news, and that somebody knows things nobody would tell a pirate.
    //
    // The horror stays under the floor: a family that never got a body, a transfer they were never told
    // about, an employer who stopped answering. Nobody in this file knows what a Reever is.

    /// <summary>Somebody who has been waiting for news, and what it costs you to bring it.</summary>
    public readonly record struct NextOfKin(string Name, string Relation, string Where, string Waiting);

    private static readonly (string Relation, string Waiting)[] Relations =
    [
        ("their sister", "has been filing the same request with the same office for nine years"),
        ("their husband", "still pays the storage on a container they were going to unpack together"),
        ("their daughter", "was small enough in the photograph that she will not remember the face"),
        ("their old supervisor", "signed the transfer and has not slept well since"),
        ("their mother", "was told there had been an accident, and was not told where"),
        ("their brother", "took the settlement, and has spent every year since wishing he had not"),
    ];

    /// <summary>Who is waiting for word about this stranger, and where you would find them. Seeded on the
    /// same room, so the person and the people who miss them are one fact.</summary>
    public static NextOfKin WhoIsWaiting(string bodyId, string siteSalt, int roomIndex)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(siteSalt);

        string tag = $"kin:{bodyId}:{siteSalt}:{roomIndex}";
        Person who = Who(bodyId, siteSalt, roomIndex);
        (string relation, string waiting) = Relations[Index(Relations.Length, tag + ":rel")];
        (_, string where) = Doors[Index(Doors.Length, tag + ":where")];

        // They share the family name — the detail that makes the connection obvious on sight and needs no
        // sentence explaining it.
        string family = who.Name[(who.Name.IndexOf(' ', StringComparison.Ordinal) + 1)..];
        return new NextOfKin($"{Pick(Given, tag + ":given")} {family}", relation, where, waiting);
    }

    /// <summary>What the captain is holding once the kit has assembled: not loot, an errand. It is left
    /// entirely to them whether they ever run it.</summary>
    public static string KinLine(in Person who, in NextOfKin kin) =>
        $"📇 {kin.Name} — {kin.Relation} — is at {kin.Where}, and {kin.Waiting}. You are the only person " +
        $"alive who can tell them where {who.Name} finished. That is worth more than anything else in this " +
        "room, to them and, if you are the sort who thinks a few moves ahead, to you.";

    /// <summary>Does bringing this family word open a thread worth pulling? Not always — most of the time it
    /// is simply a decent thing you can do, and a game where every kindness pays out is not offering you a
    /// choice at all.</summary>
    public static bool KinKnowsSomething(string bodyId, string siteSalt, int roomIndex)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(siteSalt);
        return DiceRule.Roll(DiceRule.Seed($"kin:lead:{bodyId}:{siteSalt}:{roomIndex}"), 3).Face == 1;
    }

    /// <summary>The hint that this one goes somewhere. Vague on purpose — it narrows a search, it does not
    /// end one, the same law the rumour blobs on the tracker follow.</summary>
    public static string LeadHint(string bodyId, string siteSalt, int roomIndex)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(siteSalt);

        string[] hints =
        [
            "They kept every letter, including the ones that were not supposed to leave the facility.",
            "They were sent a final effects inventory. Two items on it were never in the kit you are holding.",
            "They have a copy of the transfer order, and it is countersigned by an office that denies existing.",
            "They know the name of the ship that took them out, and it is a name you have seen on a manifest.",
        ];
        ulong seed = DiceRule.Seed($"kin:hint:{bodyId}:{siteSalt}:{roomIndex}");
        return hints[(int)(seed % (ulong)hints.Length)];
    }

    /// <summary>Does this room's kit carry an in at all? Most do not; an in you find every time is a menu
    /// option rather than a discovery, and the whole value is that it feels like luck.</summary>
    public static bool HasIntroduction(string bodyId, string siteSalt, int roomIndex)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(siteSalt);
        return DiceRule.Roll(DiceRule.Seed($"intro:has:{bodyId}:{siteSalt}:{roomIndex}"), 3).Face == 1;
    }

    private static string Pick(string[] pool, string tag) => pool[Index(pool.Length, tag)];

    private static int Index(int count, string tag) =>
        (int)(DiceRule.Roll(DiceRule.Seed(tag), count).Face - 1);
}
