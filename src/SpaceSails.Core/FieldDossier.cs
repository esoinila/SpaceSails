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
    /// <param name="forced">#774's dev seat — see <see cref="HasIntroduction"/>.</param>
    public static bool KinKnowsSomething(string bodyId, string siteSalt, int roomIndex, bool forced = false)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(siteSalt);
        return forced || DiceRule.Roll(DiceRule.Seed($"kin:lead:{bodyId}:{siteSalt}:{roomIndex}"), 3).Face == 1;
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
    /// <param name="forced">#774's dev seat: yes, whatever the roll says. The assembly is the rarest beat on
    /// the regolith (three papers rooms in one excursion, at one room in eight) and its FULL form needs two
    /// more one-in-three rolls on top of that — so the four-sentence version, the one the whole issue is
    /// about, is unreachable on demand without this.</param>
    public static bool HasIntroduction(string bodyId, string siteSalt, int roomIndex, bool forced = false)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(siteSalt);
        return forced || DiceRule.Roll(DiceRule.Seed($"intro:has:{bodyId}:{siteSalt}:{roomIndex}"), 3).Face == 1;
    }

    // ── #774 · THE ASSEMBLY SAYS TWO TO FOUR THINGS, AND THEY ARE READ ON THE CARD ──────────────────────
    //
    // The bug this replaces: the client raised the dossier card and then fired the sentences below it as
    // ordinary pulses — two to four of them, in one breath, all at the same rank — so every one of them
    // played under the card's own blurred backdrop and the captain closed the card onto nothing. #768's hold
    // is the wrong remedy for it and was refused as such: a hold releases ONE winner, and picking a winner
    // out of a same-rank sequence by which line was written last is exactly the contract #693 killed.
    //
    // The right remedy is #736's law — compose the sentences ONTO the card the act raised, the one layer a
    // backdrop cannot blur. A card has a REGION rather than a slot, so all four fit and there is no winner
    // to pick, and the ordering question stops being about survival and becomes a question about READING.
    //
    // Which is answered here, in Core, beside the predicates that decide whether each sentence exists at all
    // (#634's law: a sentence composed in the client is a sentence that can drift away from the sim).

    /// <summary>What a sentence out of somebody's kit IS. <b>The order these members are declared in is the
    /// order the card reads them in</b>, and it is an order of MEANING, not of composition:
    ///
    /// <para>You work out WHO before you can carry news of them; you learn who is WAITING before you can
    /// learn what that person knows; and the in that fell out of the kit comes last because it is the only
    /// one of the four that is not about the dead — it is a favour owed somewhere else entirely.</para>
    ///
    /// <para>Nothing may read this order off the order the sayings were appended. That is the whole point:
    /// <see cref="Debrief"/> deliberately composes them backwards and <see cref="InTheOrderTheyAreRead"/>
    /// puts them right, so append order cannot decide anything even by accident.</para></summary>
    public enum Beat
    {
        /// <summary>The name, the discipline, and who carried them out here.</summary>
        WhoTheyWere,

        /// <summary>Somebody is still waiting for word. The errand, and it is entirely the captain's choice.</summary>
        WhoIsWaiting,

        /// <summary>…and that somebody knows something nobody would ever tell a pirate.</summary>
        WhatTheFamilyKnows,

        /// <summary>A name to drop, and the door it opens.</summary>
        TheIn,
    }

    /// <summary>One sentence the assembly produces: what it is, the glyph the field book files it under, and
    /// the words. <paramref name="Text"/> is <b>exactly</b> what goes into the book — the card may dress it,
    /// the record never does.</summary>
    public readonly record struct Saying(Beat Beat, string Glyph, string Text);

    /// <summary>The first thing said over an assembled kit: a name, out of the pieces.</summary>
    public static string WhoTheyWereLine(in Person who) =>
        $"🗂 {who.Name} — a specialist in {who.Discipline}, carried out here by " +
        $"{who.Employer}. The kit is theirs, all of it, and now you know their name.";

    /// <summary>Everything this room's kit has to say, in the order it is read.</summary>
    /// <param name="everySaying">#774's dev seat: take both one-in-three gates off, so the four-sentence
    /// assembly — the one the card has to be able to carry — can be looked at on demand.</param>
    public static IReadOnlyList<Saying> Debrief(
        string bodyId, string siteSalt, int roomIndex, bool everySaying = false)
    {
        Person who = Who(bodyId, siteSalt, roomIndex);

        // COMPOSED BACKWARDS, ON PURPOSE. A list that happens to be built in the right order proves nothing
        // about the rule that orders it, and this repo has already shipped one bug (#693, the killed
        // contract) that lived entirely in the gap between those two things.
        var said = new List<Saying>();
        if (HasIntroduction(bodyId, siteSalt, roomIndex, everySaying))
        {
            said.Add(new Saying(Beat.TheIn, "🎟", IntroductionLine(InTheKit(bodyId, siteSalt, roomIndex))));
        }
        if (KinKnowsSomething(bodyId, siteSalt, roomIndex, everySaying))
        {
            said.Add(new Saying(Beat.WhatTheFamilyKnows, "🔎", LeadHint(bodyId, siteSalt, roomIndex)));
        }
        said.Add(new Saying(Beat.WhoIsWaiting, "📇", KinLine(who, WhoIsWaiting(bodyId, siteSalt, roomIndex))));
        said.Add(new Saying(Beat.WhoTheyWere, "🗂", WhoTheyWereLine(who)));

        return InTheOrderTheyAreRead(said);
    }

    /// <summary>Put a handful of sayings in the order a captain reads them — by walking <see cref="Beat"/>'s
    /// declared members, never by taking the collection as it came. Public because the guard that pins the
    /// law has to be able to hand it the same sayings in every possible order and get one answer back.</summary>
    public static IReadOnlyList<Saying> InTheOrderTheyAreRead(IEnumerable<Saying> said)
    {
        ArgumentNullException.ThrowIfNull(said);

        var pool = new List<Saying>(said);
        var ordered = new List<Saying>(pool.Count);
        foreach (Beat beat in Enum.GetValues<Beat>())
        {
            foreach (Saying one in pool)
            {
                if (one.Beat == beat)
                {
                    ordered.Add(one);
                }
            }
        }
        return ordered;
    }

    /// <summary>The debrief as the CARD carries it — every sentence, in reading order, one paragraph each.
    ///
    /// <para>The only thing this adds to a sentence is its own glyph, and only when the sentence does not
    /// already open with one: three of the four are written with theirs (the file, the card index, the
    /// ticket) and the family's lead is bare prose that the book files under 🔎. No words are invented
    /// here — a card that narrated its own contents would be the book reading itself out loud.</para></summary>
    public static string DebriefBlock(IEnumerable<Saying> said)
    {
        ArgumentNullException.ThrowIfNull(said);

        var block = new System.Text.StringBuilder();
        foreach (Saying one in InTheOrderTheyAreRead(said))
        {
            if (block.Length > 0)
            {
                block.Append("\n\n");
            }
            block.Append(one.Text.StartsWith(one.Glyph, StringComparison.Ordinal)
                ? one.Text
                : $"{one.Glyph} {one.Text}");
        }
        return block.ToString();
    }

    private static string Pick(string[] pool, string tag) => pool[Index(pool.Length, tag)];

    private static int Index(int count, string tag) =>
        (int)(DiceRule.Roll(DiceRule.Seed(tag), count).Face - 1);
}
