namespace SpaceSails.Core;

// ─────────────────────────────────────────────────────────────────────────────────────────────────
// #973 L5a · "YOU LOOK DIFFERENT." — every word an old shipmate says, and the three the captain can
// say back.
//
// EVERY STRING IN THIS FILE IS FABLE'S, WIRED VERBATIM. L5a shipped with eleven replies and all six
// slips standing in behind a `FABLE: line needed` marker; #973 L3's comment authored every one of them
// and this file now carries them. There is no standing-in text left anywhere in it, and the two guards
// that used to COUNT the markers (`ReplyIsStandingIn`, `SlipIsPlaceholder`) are kept and inverted — they
// now assert zero, so the day somebody adds a slot the count moves in the code rather than in a
// paragraph. Nothing here is paraphrased and nothing is improved.
//
// AND THE LAW THAT GOVERNS THE FILE: the bible's account of the decent ship is WRITERS' BIBLE and is
// not here. What she carried is never named. The word for what the clinic does is never printed.
// ─────────────────────────────────────────────────────────────────────────────────────────────────

/// <summary>#973 L5a · The face-scene: what each shipmate says when they first see the new face, the
/// captain's three answers, and what each of them says back. Pure words and one crossing.</summary>
public static class OldCrewScene
{
    /// <summary>The captain's three answers. The choice is a CROSSING (owner ruling §9) — the first one in
    /// the game that is about the captain himself rather than about somebody else's compartment.</summary>
    public enum Answer
    {
        /// <summary>What happened, said plainly.</summary>
        TheTruth = 0,

        /// <summary>The salesman's sentence, in the captain's mouth.</summary>
        ThePolicyLine = 1,

        /// <summary>Burns, and a run he was never on.</summary>
        ALie = 2,
    }

    /// <summary>The three buttons, in order. Same for everyone — the captain has the same three things to
    /// say whoever is across the table, and which one he picks is the whole point.</summary>
    public static string Button(Answer answer) => answer switch
    {
        Answer.TheTruth => "The truth",
        Answer.ThePolicyLine => "The policy line",
        _ => "A lie",
    };

    /// <summary>What the captain actually says when the button is pressed.</summary>
    public static string Said(Answer answer) => answer switch
    {
        Answer.TheTruth => "I died. The policy brought me back.",
        Answer.ThePolicyLine => "New face. Same file.",
        _ => "Burns. A reactor seal on the Luna run.",
    };

    /// <summary>The opening line — per person, and every one of the six living names has one.</summary>
    public static string Opening(string shipmateId) => shipmateId switch
    {
        OldCrew.BestFriendId => "You look — different. What happened to you?",
        OldCrew.FlingId =>
            "I wasn't going to say it. — You look different. Everyone says that to you now, I suppose.",
        OldCrew.SignerId => "Different face, same walk. I'd know that walk in a riot.",
        "maren" => "Oh. Oh — it's you. Under that.",
        "pell" => "I don't pour for strangers. — No. Sit. I know the hands.",
        "dagny" => "They said you were dead. You look it.",
        _ => "",
    };

    /// <summary>
    /// What they say back. All eighteen slots — six living names by three answers — are authored, and every
    /// one of them is that person's own sentence.
    ///
    /// <para>The eleven that L5a left standing in are the reason this method is worth reading: a line written
    /// by the wiring would have been the one thing this arc cannot afford, because the whole value of the old
    /// crew is that they sound like people somebody knew. They stood empty until the words existed. The
    /// fallback arm below is now only ever reached by an id that is not one of the six — the man who is dead,
    /// or a name from a future pool — and it is the best friend's line rather than a blank for the same
    /// reason it always was: a scene that plays is better than a scene with a hole in it.</para>
    /// </summary>
    public static string Reply(string shipmateId, Answer answer) => (shipmateId, answer) switch
    {
        // ── "I died. The policy brought me back." ────────────────────────────────────────────────────
        (OldCrew.BestFriendId, Answer.TheTruth) => "…Then it's true, what they say about the premium.",
        (OldCrew.FlingId, Answer.TheTruth) => "I know. I process them.",
        (OldCrew.SignerId, Answer.TheTruth) => "Then we're square. Nobody owes a dead man.",
        ("maren", Answer.TheTruth) =>
            "They brought you back. — I do their paperwork. I never once thought about whose.",
        ("pell", Answer.TheTruth) => "Dead men drink free here. The first one, anyway.",
        ("dagny", Answer.TheTruth) => "You'd think they'd have fixed the walk.",

        // ── "New face. Same file." ───────────────────────────────────────────────────────────────────
        (OldCrew.BestFriendId, Answer.ThePolicyLine) => "That's Fess's line. You've been talking to Fess.",
        (OldCrew.FlingId, Answer.ThePolicyLine) => "Don't say it like him. Please.",
        (OldCrew.SignerId, Answer.ThePolicyLine) => "Files. Yes. I sign things too, these days. It pays.",
        ("maren", Answer.ThePolicyLine) =>
            "Same file. That's what the clinic says when the name doesn't match the scan. I say it four "
            + "times a shift.",
        ("pell", Answer.ThePolicyLine) => "A file's not a face. Sit down before somebody reads it.",
        ("dagny", Answer.ThePolicyLine) =>
            "The file can say what it likes. Sit where I can see your hands.",

        // ── "Burns. A reactor seal on the Luna run." ─────────────────────────────────────────────────
        (OldCrew.BestFriendId, Answer.ALie) => "Hm. You always did look away when you lied.",
        (OldCrew.FlingId, Answer.ALie) => "The Luna run. All right.",
        (OldCrew.SignerId, Answer.ALie) => "The Luna run. — I'll put that in my report, then.",
        ("maren", Answer.ALie) => "A seal doesn't change the eyes. But all right.",
        ("pell", Answer.ALie) => "Burns. Right. Same face for your tab, mind.",
        ("dagny", Answer.ALie) =>
            "Liar. You never held a seal in your life; you held the clipboard.",

        _ => Reply(OldCrew.BestFriendId, answer),
    };

    /// <summary>True when the reply for this person and this answer is the best friend's line standing in
    /// rather than their own. Kept after every slot was written, and inverted: the guard now asserts that it
    /// is false for every living name, so a slot added to the pool without a voice is caught by the same
    /// mechanism that used to count the ones that had none.</summary>
    public static bool ReplyIsStandingIn(string shipmateId, Answer answer) =>
        shipmateId != OldCrew.BestFriendId
        && string.Equals(Reply(shipmateId, answer), Reply(OldCrew.BestFriendId, answer), StringComparison.Ordinal);

    // ── THE PHOTOGRAPH ───────────────────────────────────────────────────────────────────────────────

    /// <summary>The held-memory sheet a person hands you — the strongest kind, because a second witness is
    /// holding it. Four faces; four threads.</summary>
    public const string Photograph =
        "Four of you on the HALCYON REACH's boat deck, fleet-day bunting strung from the davit. The one on " +
        "the left is laughing at something said off the frame. You are the one not looking at the camera.";

    /// <summary>The subject the flashback beat is raised with when the photograph is handed over. It is a
    /// word rather than an entry id because the photograph is not a page of the captain's book — it is
    /// somebody else's memory of him, which is the whole reason it is worth having.</summary>
    public const string PhotographSubject = "photograph";

    /// <summary>Who hands it over: the best friend if this thread cast him, else the one who owes you. The
    /// bible's order, and the fallback is not arbitrary — she is the other one who would have kept it.</summary>
    public static string PhotographHeldBy(IReadOnlyList<OldCrew.Seeded> seeded)
    {
        ArgumentNullException.ThrowIfNull(seeded);
        if (OldCrew.Find(seeded, OldCrew.BestFriendId) is not null)
        {
            return OldCrew.BestFriendId;
        }

        if (OldCrew.Find(seeded, "maren") is not null)
        {
            return "maren";
        }

        // Neither of the two the bible names was cast. Somebody still kept it — a photograph that nobody in
        // the world is holding is a beat that silently never fires, and a beat that fires in three seedings
        // out of four is worse than one that fires in all of them. The signer is the last resort rather than
        // the first: the man who signed handing you a picture of the four of you is a different scene, and it
        // should not be the ordinary one.
        foreach (OldCrew.Seeded s in seeded)
        {
            if (s.Id != OldCrew.SignerId)
            {
                return s.Id;
            }
        }

        return seeded.Count > 0 ? seeded[0].Id : "";
    }

    /// <summary>How often the dead man is the fourth face instead of one of the living four — one seeding in
    /// two, out of eight. FLAGGED for the owner's tuning.</summary>
    public const int HollisIsAFaceInEight = 4;

    /// <summary>
    /// THE FOUR FACES. The seeded shipmates, in pool order — except that in one seeding in two the last of
    /// them is the man who is dead and filed, standing in a photograph on a boat deck.
    ///
    /// <para>The signer is never the one replaced: he is the person the whole seeding is arranged around,
    /// and a photograph that could quietly leave him out would be a clue that sometimes is not there.</para>
    /// </summary>
    public static IReadOnlyList<string> PhotographFaces(string threadId, IReadOnlyList<OldCrew.Seeded> seeded)
    {
        ArgumentNullException.ThrowIfNull(threadId);
        ArgumentNullException.ThrowIfNull(seeded);

        var faces = new List<string>();
        foreach (OldCrew.Seeded s in seeded)
        {
            faces.Add(OldCrew.ById(s.Id)?.Name ?? s.Id);
        }

        bool hollis = DiceRule.Roll(DiceRule.Seed($"oldcrew|hollis|{threadId}"), 8).Face <= HollisIsAFaceInEight;
        if (!hollis)
        {
            return faces;
        }

        for (int i = faces.Count - 1; i >= 0; i--)
        {
            if (seeded[i].Id != OldCrew.SignerId)
            {
                faces[i] = OldCrew.ById(OldCrew.DeadId)!.Value.Name;
                break;
            }
        }

        return faces;
    }

    // ── THE SUMMER-PARTY PAGE ────────────────────────────────────────────────────────────────────────

    /// <summary>The one page the filing line cannot grey, because the service filed it — against you (owner
    /// ruling §13, "the joke stays"). Seeded into the captain's ledger at thread start, marked <i>mine</i>,
    /// tagged <b>love</b>, and dated before anything else the book holds.</summary>
    public const string SummerPartyPage =
        "Fleet-day, the boat deck after the speeches. Bunting. A glass set down on the rail so it would not " +
        "spill in the roll. Someone beside you who did not move away when the rail went cold. That is all " +
        "the page says. There is a stamp on it that was not yours.";

    /// <summary>The id the summer-party page rides under, in the ledger and in the book. Fixed rather than
    /// derived: it is the same page in every universe, and the law that it never greys is written against
    /// this key.</summary>
    public const string SummerPartyId = "summer-party";

    /// <summary>The row's own heading in the Captain's ledger.</summary>
    public const string SummerPartyTitle = "🎞 Fleet-day";

    /// <summary>What the row's provenance line says. It is the stamp that makes the joke, and it says only
    /// that there is one — never who wrote it, never what it was for.</summary>
    public const string SummerPartyProvenance = "filed · a stamp that was not yours";

    // ── KNOCKING ON THE DOOR ─────────────────────────────────────────────────────────────────────────

    /// <summary>The line at the registrar's door when the book already says who is inside with him (owner
    /// ruling §14). One nerve pip, once per visit — the captain's reluctance is real and it is priced.</summary>
    public const string AtTheRegistrarsDoor = "You stand outside the registrar's longer than you need to.";

    /// <summary>The nerve a knock costs, in whole pips.</summary>
    public const int KnockNervePips = 1;

    /// <summary>The house-voice label that pip is filed under, so "what broke you?" can say so afterwards.</summary>
    public const string KnockNerveLabel = "the door you stood outside of";

    // ── THE SLIP (a good roll over a drink) ──────────────────────────────────────────────────────────

    /// <summary>Which of the detective's two theories a sheet serves (owner ruling §12). One enum on the
    /// sheet, one filter on THREADS, and no new machinery — L3 builds the filter.</summary>
    public static HeldMemory.Theory SlipTag(string shipmateId) => shipmateId switch
    {
        OldCrew.FlingId or OldCrew.BestFriendId => HeldMemory.Theory.Love,
        _ => HeldMemory.Theory.Money,
    };

    /// <summary>
    /// What a shipmate slips you when the glass goes well: a sheet into the book, marked <i>his</i> (or
    /// <i>hers</i>) and tagged by what they are to the captain.
    ///
    /// <para>All six are Fable's, verbatim (#973 L3). Every one is a piece of PAPER from the job that person
    /// ended up in — a claims form, a berth listing, a receipt, a docket, a tab, a patrol schedule — because
    /// the bible's whole conceit is that they work where they know things, and the thing they know is filed
    /// where they work. Nothing in any of them says what the pods held; that is writers' bible and it stays
    /// there. What they say is that somebody put it down and did not pick it up again.</para>
    /// </summary>
    public static string Slip(string shipmateId) => shipmateId switch
    {
        OldCrew.FlingId =>
            "A claims form, folded twice, your old name in the subject line and hers in the box marked "
            + "WITNESS. She did not say why she had kept it.",

        OldCrew.BestFriendId =>
            "A berth listing for the REACH under the name she was given when they took her. He had circled "
            + "the date. 'She's still there,' he said, and did not look at you.",

        OldCrew.SignerId =>
            "A customs receipt for sealed reefer pods, medical, and a counter-signature you would know "
            + "anywhere. It is his. He let you see it, which is not the same as giving it to you.",

        "maren" =>
            "A clinic docket: an intake for a patient whose name is crossed out and written again, twice, "
            + "in the same hand, a week apart.",

        "pell" =>
            "A bar tab in your old name, never paid, with a line at the bottom: 'covered — H.G.' The "
            + "bartender does not forgive debts. Somebody paid it.",

        "dagny" =>
            "A cutter's patrol schedule, one sector blanked with a thumb. 'Don't be there,' she said, which "
            + "is a kind of gift.",

        // Not one of the six. The plainest sentence that can be true of anybody, so a future pool member
        // without a slip is a visible hole rather than somebody else's paper in their hand.
        _ => $"A sheet {OldCrew.ById(shipmateId)?.Name ?? shipmateId} put on the table and did not pick up again.",
    };

    /// <summary>True while a slip is the plain standing-in sentence rather than that person's own paper.
    /// False for all six now that they are written; the guard reads this rather than a number typed in a
    /// test, so a name added to the pool without a slip is caught the day it is added.</summary>
    public static bool SlipIsPlaceholder(string shipmateId) =>
        Slip(shipmateId).StartsWith("A sheet ", StringComparison.Ordinal);
}
