using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

public static partial class UndergroundComplex
{
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
}
