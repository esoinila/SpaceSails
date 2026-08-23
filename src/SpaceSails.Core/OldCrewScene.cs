namespace SpaceSails.Core;

// ─────────────────────────────────────────────────────────────────────────────────────────────────
// #973 L5a · "YOU LOOK DIFFERENT." — every word an old shipmate says, and the three the captain can
// say back.
//
// EVERY STRING IN THIS FILE IS FABLE'S, WIRED VERBATIM. Where the bible gives no line for a person and
// an answer, the slot carries a `FABLE: line needed` marker naming the person and the answer, and the
// placeholder beside it is the one line the bible DOES give for that answer — the best friend's — so
// the scene plays rather than showing a blank. Nothing here is paraphrased and nothing is improved.
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
    /// What they say back. The bible authors seven of the eighteen slots; the other eleven stand in with the
    /// best friend's line for that answer and are listed, one by one, in the lane's PR body.
    ///
    /// <para>Standing in rather than inventing is deliberate: a line written by the wiring would be the one
    /// thing this arc cannot afford, because the whole value of the old crew is that they sound like people
    /// somebody knew.</para>
    /// </summary>
    public static string Reply(string shipmateId, Answer answer) => (shipmateId, answer) switch
    {
        (OldCrew.BestFriendId, Answer.TheTruth) => "…Then it's true, what they say about the premium.",
        (OldCrew.FlingId, Answer.TheTruth) => "I know. I process them.",
        (OldCrew.SignerId, Answer.TheTruth) => "Then we're square. Nobody owes a dead man.",

        (OldCrew.BestFriendId, Answer.ThePolicyLine) => "That's Fess's line. You've been talking to Fess.",
        (OldCrew.FlingId, Answer.ThePolicyLine) => "Don't say it like him. Please.",

        (OldCrew.BestFriendId, Answer.ALie) => "Hm. You always did look away when you lied.",
        (OldCrew.FlingId, Answer.ALie) => "The Luna run. All right.",

        // FABLE: line needed — Maren Okafor's reply to THE TRUTH ("I died. The policy brought me back.").
        // She is the clinic clerk and the one who owes you; she of all of them has seen the second page.
        // FABLE: line needed — Pell Andrade's reply to THE TRUTH. He is the one you owe, behind the taps.
        // FABLE: line needed — Dagny Voss's reply to THE TRUTH. The rival, second officer on a cutter.
        // FABLE: line needed — Corwin Sallis's reply to THE POLICY LINE ("New face. Same file.").
        // FABLE: line needed — Maren Okafor's reply to THE POLICY LINE.
        // FABLE: line needed — Pell Andrade's reply to THE POLICY LINE.
        // FABLE: line needed — Dagny Voss's reply to THE POLICY LINE.
        // FABLE: line needed — Corwin Sallis's reply to A LIE ("Burns. A reactor seal on the Luna run.").
        // FABLE: line needed — Maren Okafor's reply to A LIE.
        // FABLE: line needed — Pell Andrade's reply to A LIE.
        // FABLE: line needed — Dagny Voss's reply to A LIE.
        _ => Reply(OldCrew.BestFriendId, answer),
    };

    /// <summary>True when the reply for this person and this answer is the best friend's line standing in
    /// rather than their own. Read by the guard that counts the unwritten slots, so the count in the PR body
    /// and the count in the code can never drift apart.</summary>
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

        return OldCrew.Find(seeded, "maren") is not null ? "maren" : "";
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
    /// What a shipmate slips you when the glass goes well: a sheet into the book, marked <i>his</i> and
    /// tagged by what they are to the captain.
    ///
    /// <para>All six texts are unwritten and every one of them is listed in the lane's PR body. The
    /// placeholder is deliberately the plainest sentence that can be true of any of them — it names the
    /// person and the fact that something was handed over, and stops.</para>
    /// </summary>
    public static string Slip(string shipmateId)
    {
        // FABLE: line needed — Ilse Marrow's slip. She reads fine print for a living, and the sheet she
        //   lets go of should be a piece of it. Tagged LOVE.
        // FABLE: line needed — Teodor "Teo" Brask's slip. The registrar; hulls and their former names.
        //   Tagged LOVE.
        // FABLE: line needed — Corwin Sallis's slip. The customs post, and he signed once already.
        //   Tagged MONEY.
        // FABLE: line needed — Maren Okafor's slip. The clinic clerk: the second page of everything.
        //   Tagged MONEY.
        // FABLE: line needed — Pell Andrade's slip. Behind the taps at a working berth. Tagged MONEY.
        // FABLE: line needed — Dagny Voss's slip. Second officer on a registry cutter. Tagged MONEY.
        string who = OldCrew.ById(shipmateId)?.Name ?? shipmateId;
        return $"A sheet {who} put on the table and did not pick up again.";
    }

    /// <summary>Every slip text is a placeholder today. The guard that counts them reads this rather than a
    /// number typed in a test, so the count cannot go stale the day one of them is written.</summary>
    public static bool SlipIsPlaceholder(string shipmateId) =>
        Slip(shipmateId).StartsWith("A sheet ", StringComparison.Ordinal);
}
