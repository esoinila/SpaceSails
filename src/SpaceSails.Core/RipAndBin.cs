using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #798 · RIP IT AND BIN IT — destroying a paper is a verb, and in a base where professionals empty the
/// bins, binning is a BET.
///
/// <para>Owner, live in play (2026-08-09): <i>"Now the option is to photograph the papers and leave them
/// there… I would not like to leave them at table in canteen… we need option to destroy them by ripping and
/// binning etc?"</i> And, filing the priority off the phase-two loop: <i>"those trash cans are needed so we
/// get rid of the processed materials without connecting them to us too clearly, like leaving them to the
/// table."</i></para>
///
/// <h3>The gap this closes</h3>
/// <para>#788's register photographs a thing and LEAVES it, which is right in the room it was found in and
/// wrong at a canteen table — leaving the original where you sat is signing it. The satchel had TAKE and it
/// had LEAVE, and no third answer at all. The dig produces exactly the disposal problem this file solves:
/// the book has the gist, the original is now a liability, and your own table is the worst place in the
/// building for it.</para>
///
/// <h3>The one law this verb is written under</h3>
/// <para><b>THE BOOK NEVER UNLEARNS.</b> Destroying an original destroys the EVIDENCE and nothing else — not
/// a filed note, not a red line between two of them (#741), not the seated write-up's own set. What the
/// captain had already dug out of a sheet is in the book in their own hand, and a hand-written page does not
/// come apart because the paper it was copied from did. Nothing in this file can reach a book; that is the
/// enforcement, and a guard proves the entries survive.</para>
///
/// <h3>A bin is not oblivion. It is a HANDOVER.</h3>
/// <para>#775's tidy-spaces rule says professionals empty every bin in this building on schedule. Torn-up
/// paper in a canteen bin is destroyed only if nobody cares; if somebody cares, the bin is where they
/// collect it. So the tiers below are a LADDER OF BETS and never a promise, and no sentence in this file
/// tells a captain they got away with it. <b>You find out only if it comes back</b> (#649's discipline).</para>
///
/// <para>Owner's own ranking of the bets, and the joke it turns on: <i>"preferably some safe disposal paper
/// bin or some bin that has wet stuff"</i> — the clean paper-recycling bin is tidy, professional, and every
/// sheet in it stays legible to whoever empties it, which makes it the worst choice wearing the safest face;
/// the canteen slop bin is the competent one, because soup does what no amount of tearing can. Above both
/// sits the chute, which feeds the base's waste stream directly rather than leaving a bag standing in a room.
/// <b>In this first cut all three do exactly the same thing</b> — what differs is the word recorded on the
/// act's filed fact, which is what a later arc reads when it decides whether anything comes back.</para>
///
/// <para>Pure and deterministic, like everything else in Core: it holds the tiers, the reach, and the words.
/// It has never heard of a satchel, a book or a floor.</para>
/// </summary>
public static class RipAndBin
{
    /// <summary>
    /// THE DISPOSAL LADDER, worst bet first.
    ///
    /// <para>Ordinal order IS the ranking, so a caller that wants "is this a better bet than that" compares
    /// two enum values rather than consulting a table somebody has to remember to keep in step. Nothing is
    /// stored under these ordinals — bins are carved fresh from the floor every time it is built — so the
    /// order is free to be the design rather than an artefact of what shipped first.</para>
    /// </summary>
    public enum Tier
    {
        /// <summary>The clean paper-recycling bin in a corridor or a copier corner. Tidy, professional, and
        /// every sheet in it legible to whoever empties it: the worst choice wearing the safest face.</summary>
        PaperBin,

        /// <summary>The canteen slop bin. Owner: <i>"some bin that has wet stuff."</i> The poor man's
        /// shredder, and choosing the disgusting option is the competent one.</summary>
        SlopBin,

        /// <summary>A waste chute in a wall. It feeds the base's own stream directly — no bag standing in a
        /// room waiting for a professional. A better bet, and still not certainty: the stream goes
        /// SOMEWHERE.</summary>
        Chute,
    }

    /// <summary>Every tier, worst first — for guards that must sweep the whole ladder rather than sample a
    /// rung, and for anything that wants to state the ranking without spelling it.</summary>
    public static IReadOnlyList<Tier> Ladder { get; } = (Tier[])Enum.GetValues(typeof(Tier));

    /// <summary>
    /// One bin, chute or slop bucket standing on a floor.
    /// </summary>
    /// <param name="Tier">Which rung of the ladder it is — the word that ends up on the filed fact.</param>
    /// <param name="X">Centre of the solid box, in the surface's own coordinates.</param>
    /// <param name="Y">Centre.</param>
    /// <param name="StandX">Where a captain stands to use it. Published rather than worked out by every
    /// caller, because it is the one coordinate that has to be BOTH clear of the room's own furniture and
    /// inside <see cref="ReachDu"/> — two facts only the carver knows, and this project has set its captain
    /// down inside a wall twice by letting somebody else guess at them.</param>
    /// <param name="StandY">…and the other half of it.</param>
    /// <param name="Plate">What is stencilled on it, at signage size.</param>
    public readonly record struct Bin(
        Tier Tier, double X, double Y, double StandX, double StandY, string Plate)
    {
        /// <summary>How far this bin is from a spot on the floor.</summary>
        public double DistanceTo(double x, double y)
        {
            double dx = x - X, dy = y - Y;
            return Math.Sqrt((dx * dx) + (dy * dy));
        }
    }

    /// <summary>Half the side of the solid box a bin puts on the plan. Small — it is a bin, not a skip — and
    /// it is the ONE number both the carved wall segments and every clearance test read, so the thing that is
    /// drawn and the thing a body collides with cannot become two different objects.</summary>
    public const double HalfDu = 0.9;

    /// <summary>How near a captain has to be for the verb to be live: arm's length and a step. Deliberately
    /// smaller than a walk (9 du/s) so standing at a bin is a thing you did on purpose, and comfortably
    /// larger than the box itself so the control does not flicker while you shuffle.</summary>
    public const double ReachDu = 4.0;

    /// <summary>How much clear floor a bin needs around its own centre before a carver will stand one there.
    /// The box plus most of a body — a bin stands AGAINST a wall, which is where bins go, so this is
    /// deliberately not "a corridor's worth": it is the margin that keeps the box out of the wall itself and
    /// leaves the rest of the passage to walk down.</summary>
    public const double ClearDu = 1.8;

    /// <summary>…and how far a bin keeps from FURNITURE — a table somebody is sitting at, a stool, a
    /// doorway, a console the [E] key answers. Larger than <see cref="ClearDu"/> on purpose: a wall is
    /// something you stand a bin against, and a chair is something you have to be able to get out of.</summary>
    public const double ClearOfFurnitureDu = 4.0;

    /// <summary>How far from the bin the published standing spot is placed. Comfortably inside
    /// <see cref="ReachDu"/>, and far enough that a captain standing there is beside the thing rather than
    /// inside its box.</summary>
    public const double StandOffDu = 2.2;

    /// <summary>How much clear floor the STANDING spot needs. A body's own width and no more: a captain
    /// stands at a bin, they do not park a vehicle at one.</summary>
    public const double StandClearDu = 0.9;

    /// <summary>
    /// How far somebody else's eyes reach for a rip to be a thing they SAW.
    ///
    /// <para>Not a reading distance — nobody makes out a pay sheet across a canteen — but the distance at
    /// which <i>the new face tore something up and binned it</i> is a sentence a stranger could later say.
    /// Deliberately wider than a hall's table pitch, so the top next to yours counts, and always asked with a
    /// LINE OF SIGHT: a cabinet's privacy is the walls it is made of and not a number typed here.</para>
    /// </summary>
    public const double OverlookedDu = 15.0;

    /// <summary>The verb's glyph, and the one the filed fact wears.</summary>
    public const string Glyph = "🗑";

    /// <summary>What a filed line about being SEEN doing it wears. Different ink on purpose: the act and the
    /// witness are two facts, and a book that wrote them under one glyph would be filing an opinion.</summary>
    public const string SeenGlyph = "👁";

    /// <summary>Is this a thing that can be torn up at all? Paper and a file on somebody — the two kinds that
    /// are EVIDENCE, which is the same pair <see cref="LeftBehind.GistOf"/> has a gist for. Rounds, cards and
    /// relic paperwork are not: nobody reads a handful of rounds over your shoulder, and tearing up an
    /// authority is throwing away a door.</summary>
    public static bool IsEvidence(Satchel.Kind kind) =>
        kind is Satchel.Kind.Paper or Satchel.Kind.Dirt;

    /// <summary>Is the captain standing near enough to this bin to use it?</summary>
    public static bool WithinReach(double x, double y, in Bin bin) => bin.DistanceTo(x, y) <= ReachDu;

    /// <summary>
    /// THE NEAREST BIN THE CAPTAIN CAN ACTUALLY REACH, or null when there is none.
    ///
    /// <para>One function, so the control that is drawn, the hint that explains it and the act that fires all
    /// ask the same question of the same list. Ties are broken by the better bet, then by the tier's own
    /// order — two bins at the same distance is a thing a room can honestly produce, and a captain who
    /// reaches for "the bin" at a counter with a slop bucket and a chute beside each other should get the
    /// chute, because that is the one they would use.</para>
    /// </summary>
    public static Bin? NearestWithinReach(double x, double y, IReadOnlyList<Bin>? bins)
    {
        Bin? best = null;
        double bestAt = double.MaxValue;
        foreach (Bin bin in bins ?? [])
        {
            double at = bin.DistanceTo(x, y);
            if (at > ReachDu)
            {
                continue;
            }
            if (best is null || at < bestAt - 0.001
                || (Math.Abs(at - bestAt) <= 0.001 && bin.Tier > best.Value.Tier))
            {
                (best, bestAt) = (bin, at);
            }
        }
        return best;
    }

    // ── THE WORDS ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>What the control is called, wherever it is drawn.</summary>
    public const string Label = "🗑 RIP IT AND BIN IT";

    /// <summary>What a tier is called in a sentence. The one place the three of them are named, so the
    /// hint, the said line and the filed fact cannot come to three different views of the same bucket.</summary>
    public static string TheBin(Tier tier) => tier switch
    {
        Tier.PaperBin => "the paper bin",
        Tier.SlopBin => "the slop bin",
        _ => "the waste chute",
    };

    /// <summary>What is stencilled on each kind, at signage size. The building's own flat voice — a bin does
    /// not explain the building it stands in, and the wet one does not admit to being useful.</summary>
    public static string PlateFor(Tier tier) => tier switch
    {
        Tier.PaperBin => "🗄 PAPER · FLATTEN AND STACK · EMPTIED DAILY",
        Tier.SlopBin => "🪣 SLOP · FOOD WASTE ONLY · NO TRAYS",
        _ => "🕳 WASTE CHUTE · NO GLASS · NO PRESSURE VESSELS",
    };

    /// <summary>
    /// What each rung is worth, said once, at the bin the captain is standing at.
    ///
    /// <para>The wet-bin joke lives HERE and nowhere else in the game — one rung of one ladder, at the
    /// canteen — because a joke told at three bins is not a joke, it is a tone. Owner: <i>"preferably some
    /// safe disposal paper bin or some bin that has wet stuff :-D"</i></para>
    ///
    /// <para>None of these three sentences says the paper is destroyed. That is the whole discipline of the
    /// feature: a bin in this building is a handover, and the game never once tells you which tier was
    /// enough.</para>
    /// </summary>
    public static string TierBet(Tier tier) => tier switch
    {
        Tier.PaperBin => "A clean paper bin, emptied on a schedule by somebody whose job it is. Every piece "
            + "in it stays readable.",
        Tier.SlopBin => "The slop bin. Soup does what no amount of tearing can.",
        _ => "A chute. Whatever goes down it is somewhere else within the minute — and somewhere is still a "
            + "place.",
    };

    /// <summary>The hint on the control while it will work, naming the bucket and the price. It names what
    /// SURVIVES, because that is the fact the whole decision turns on: the book keeps its gist and the sheet
    /// stops existing.</summary>
    public static string Hint(Tier tier) =>
        $"Tear it up and put it in {TheBin(tier)}. Whatever you had already dug out of it stays in the book "
        + "— the sheet does not come back.";

    /// <summary>…and the refusal when there is nothing to put it in. It names WHAT WOULD FIX IT, because a
    /// refusal a player cannot act on is a wall with a sign on it (#603).</summary>
    public const string NoBinLine =
        "Nothing here to put it in. A file torn up and carried about in your coat is the same file in more "
        + "pockets — find a bin, a slop bucket or a chute, stand at it, and then tear it.";

    /// <summary>…and the refusal for a thing that is not evidence at all.</summary>
    public const string NotEvidenceLine =
        "That is not a thing anybody reads over your shoulder. Paper is, and a file on somebody is. Rounds, "
        + "cards and the paperwork for something too big to lift are not evidence — they are property.";

    /// <summary>
    /// What the captain is told as it comes apart in their hands. One sentence per rung, because the hands
    /// are doing three different things and a captain told the wrong story about their own hands has been
    /// lied to whatever the sim did.
    ///
    /// <para><b>The document is named at the END of its clause and never inside one.</b> FOUND BY READING IT
    /// ON THE SCREEN, in the browser, on this build: <c>SatchelLabel</c> hands over a phrase that carries its
    /// own em-dash clause — <i>"maintenance log, two hands — a mention"</i> — so the first cut's <i>"You tear
    /// {what} up"</i> rendered as <i>"You tear maintenance log, two hands — a mention up and push…"</i>, with
    /// the particle stranded five words from its verb. A sentence that only reads correctly for short labels
    /// is a sentence nobody has read.</para>
    /// </summary>
    public static string RippedLine(string what, Tier tier)
    {
        ArgumentNullException.ThrowIfNull(what);
        const string kept =
            " Whatever you had already dug out of it is still in the book, in your own hand.";
        return tier switch
        {
            Tier.PaperBin =>
                $"{Glyph} You tear it into eighths and drop it in the paper bin — {what}, out of the sleeve "
                + $"and out of your hands.{kept}",
            Tier.SlopBin =>
                $"{Glyph} You tear it up and push the pieces down under the soup — {what}, out of the "
                + $"sleeve.{kept}",
            _ =>
                $"{Glyph} You tear it up, pull the hatch and let it go — {what}, out of the sleeve, and "
                + $"something takes it a long way down.{kept}",
        };
    }

    /// <summary>
    /// WHAT THE BOOK KEEPS OF THE ACT — and the TIER is in it, in words.
    ///
    /// <para>The tiers do the same thing today. What a later arc will want to know is not "was a document
    /// destroyed" but WHERE IT WENT, and the only durable place to keep that is the line the field book
    /// already files and the vault already carries. So the bucket is named here rather than counted in a
    /// meter nobody can read back.</para>
    ///
    /// <para>It records what the captain did and never what it bought them, because nothing in this game
    /// knows that yet.</para>
    /// </summary>
    public static string DisposalNote(string what, Tier tier)
    {
        ArgumentNullException.ThrowIfNull(what);
        // The BUCKET first and the document after it, for two reasons that agree: the notebook clips a
        // title to 88 characters (CaseThreads.TitleLength), and the one word a later arc has to be able to
        // read back is the one saying where the pieces went — and a label with its own em-dash clause in it
        // ("maintenance log, two hands — a mention") made "Tore up {what} and put it in…" unreadable on the
        // page. Both faults were on the screen at once, which is where they were found.
        return $"Torn up and put in {TheBin(tier)}: {what}. Nothing of it was left on the table.";
    }

    /// <summary>Who was looking. The client decides which of these is true — what "watched" means at a
    /// counter, at a table and on a corridor are three questions about rooms, and this file does not have
    /// one.</summary>
    public enum Watcher
    {
        /// <summary>The bar desk. #781: the keep is security, and everything on that counter is read by him,
        /// by whoever is waiting to be served and by the man on the next stool.</summary>
        TheKeep,

        /// <summary>Somebody in the chair opposite — #784's own drama beat, one verb over.</summary>
        TheChairOpposite,

        /// <summary>An occupied top close enough to read a hand.</summary>
        TheNextTable,

        /// <summary>#804 · A round, with its eyes on you.</summary>
        TheRota,
    }

    /// <summary>
    /// WHO SAW IT — one ladder, asked top-down, stopping at the first true rung.
    ///
    /// <para>The four flags are the client's own facts about a running world; the ORDER is this file's, and
    /// it lives here for the reason every other law in this project lives in Core: a ladder spelled out at a
    /// call site is a ladder nobody can test both directions of, and this one has to be provable to answer
    /// <b>null</b> as often as it answers a name.</para>
    ///
    /// <para>The rota is first because a man whose job is looking at people is a different fact from a
    /// stranger who happened to glance up. It files ONE line and not a census: this cut records that it was
    /// seen, and nothing reacts.</para>
    /// </summary>
    /// <param name="rotaEyesOn">#804 · A guard on a round has registered the captain.</param>
    /// <param name="atTheCounter">The captain is on a bar stool — where the keep is security (#781) and
    /// everyone waiting to be served is behind you.</param>
    /// <param name="companyAtTheTable">Somebody is in the chair opposite.</param>
    /// <param name="overlooked">Somebody at a nearby seat has a clear line to the captain's hands.</param>
    public static Watcher? WhoSaw(
        bool rotaEyesOn, bool atTheCounter, bool companyAtTheTable, bool overlooked)
    {
        if (rotaEyesOn)
        {
            return Watcher.TheRota;
        }
        if (atTheCounter)
        {
            return Watcher.TheKeep;
        }
        if (companyAtTheTable)
        {
            return Watcher.TheChairOpposite;
        }
        return overlooked ? Watcher.TheNextTable : null;
    }

    /// <summary>Who it was, in a clause a sentence can be built round.</summary>
    public static string WatcherClause(Watcher who) => who switch
    {
        Watcher.TheKeep => "the keep behind the counter",
        Watcher.TheChairOpposite => "a face in the chair opposite",
        Watcher.TheNextTable => "people at the next table",
        _ => "a man on the security rota",
    };

    /// <summary>What is SAID when it was done in front of somebody. It never says what follows, because
    /// nothing follows yet — this cut files the fact and stops, which is #715's per-entity memory arriving
    /// as a line in a book rather than as a meter.</summary>
    public static string SeenLine(Watcher who) =>
        $"{SeenGlyph} You do it with {WatcherClause(who)} in plain view. Nobody says anything, and that is "
        + "not the same as nobody noticing.";

    /// <summary>…and what the book keeps of having been seen doing it. The captain's own note, in the
    /// captain's own flat register.</summary>
    public static string SeenNote(Watcher who) =>
        $"Tore a document up and binned it with {WatcherClause(who)} watching. Nothing was said about it.";

    /// <summary>Every authored sentence this feature owns, for the canon sweep. It walks the ladder and the
    /// witnesses rather than listing lines somebody has to remember to add — #709's discipline, and the
    /// reason a catalog is a method rather than a list that goes stale.</summary>
    public static IEnumerable<string> AllProse()
    {
        yield return Label;
        yield return NoBinLine;
        yield return NotEvidenceLine;
        foreach (Tier tier in Ladder)
        {
            yield return TheBin(tier);
            yield return PlateFor(tier);
            yield return TierBet(tier);
            yield return Hint(tier);
            yield return RippedLine("the manifest", tier);
            yield return DisposalNote("the manifest", tier);
        }
        foreach (Watcher who in (Watcher[])Enum.GetValues(typeof(Watcher)))
        {
            yield return WatcherClause(who);
            yield return SeenLine(who);
            yield return SeenNote(who);
        }
    }
}
