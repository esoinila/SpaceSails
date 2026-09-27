using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #251 · THE WORDS (#798, #828) — every plate, hint, prompt and line a bin says, the worked-first
/// ordering, and what a disposal leaves to find.
///
/// <para>Split out of <c>RipAndBin.cs</c> under #251 as a pure move: one contiguous run, no member renamed,
/// re-scoped or re-ordered. Every field here is a <c>const</c>; <c>Ladder</c>, the class's one initialised
/// static, stays in the opening file (#1163).</para>
/// </summary>
public static partial class RipAndBin
{
    // ── THE WORDS ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>What the control is called, wherever it is drawn.</summary>
    public const string Label = "🗑 RIP IT AND BIN IT";

    /// <summary>What a tier is called in a sentence. The one place the three of them are named, so the
    /// hint, the said line and the filed fact cannot come to three different views of the same bucket.</summary>
    public static string TheBin(Tier tier) => tier switch
    {
        Tier.PaperBin => "the paper bin",
        Tier.SlopBin => "the slop bin",
        Tier.Chute => "the waste chute",
        _ => "the secure disposal",
    };

    /// <summary>What is stencilled on each kind, at signage size. The building's own flat voice — a bin does
    /// not explain the building it stands in, and the wet one does not admit to being useful.</summary>
    public static string PlateFor(Tier tier) => tier switch
    {
        Tier.PaperBin => "🗄 PAPER · FLATTEN AND STACK · EMPTIED DAILY",
        Tier.SlopBin => "🪣 SLOP · FOOD WASTE ONLY · NO TRAYS",
        Tier.Chute => "🕳 WASTE CHUTE · NO GLASS · NO PRESSURE VESSELS",
        // #828 · The building's flat voice again, and the one plate on the floor that admits what the
        // machine under it is FOR. It stands in a premium suite (RingOffice's furnishing tier), which is
        // the whole of what it says about who gets one.
        _ => "🗑 SECURE DISPOSAL · PAPER ONLY · WATCH IT GO",
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
        Tier.Chute => "A chute. Whatever goes down it is somewhere else within the minute — and somewhere is "
            + "still a place.",
        // #828 · The one rung whose sentence is allowed to promise, because it is the one the captain can
        // CHECK. Everything else on this ladder is a bet about hands you will never see; this is a machine
        // you stand over until there is nothing left, and the promise is worth exactly what you watched.
        _ => "The office's own disposal. You feed it and you stand there until it has finished, and what "
            + "comes out the other side is not paper any more.",
    };

    /// <summary>The hint on the control while it will work, naming the bucket and the price. It names what
    /// SURVIVES, because that is the fact the whole decision turns on: the book keeps its gist and the sheet
    /// stops existing.</summary>
    public static string Hint(Tier tier) =>
        (tier == Tier.SecureDisposal
            ? $"Feed it to {TheBin(tier)} and stand there while it goes. "
            : $"Tear it up and put it in {TheBin(tier)}. ")
        + "Whatever you had already dug out of it stays in the book — the sheet does not come back.";

    /// <summary>…and the refusal when there is nothing to put it in. It names WHAT WOULD FIX IT, because a
    /// refusal a player cannot act on is a wall with a sign on it (#603).</summary>
    public const string NoBinLine =
        "Nothing here to put it in. A file torn up and carried about in your coat is the same file in more "
        + "pockets — find a bin, a slop bucket or a chute, stand at it, and then tear it.";

    /// <summary>…and the refusal for a thing that is not evidence at all.</summary>
    public const string NotEvidenceLine =
        "That is not a thing anybody reads over your shoulder. Paper is, and a file on somebody is. Rounds, "
        + "cards and the paperwork for something too big to lift are not evidence — they are property.";

    // ── #828 · THE OTHER DOOR: THE BIN TAKES [E] ─────────────────────────────────────────────────────────
    //
    // Owner, at his own table in the upper canteen (2026-08-11): "I think the trash could be an e-use …
    // where we select from inventory the processed items we rip and deposit into trash."
    //
    // #798 shipped this verb PAPER-FIRST — it lives on the row and the bin is a range check. This is the
    // mirror: stand at the bin, open the sleeve over it, feed it. Nothing below is a second shredder. It is
    // the WORDS a picker needs and the ORDER it lists in, and the act on the far side of a press is the one
    // in this file already, which is why the filed fact reads the same whichever grip you took.

    /// <summary>
    /// #828 · What the picker says when the sleeve holds nothing a bin has any business with — the
    /// bin-flavoured kin of <c>SeatedSpread.NothingToWorkLine</c>.
    ///
    /// <para>One sentence, and it names what WOULD go in, because a refusal a player cannot act on is a wall
    /// with a sign on it (#603) — the same discipline <see cref="NoBinLine"/> is written under, pointed the
    /// other way round: there the bin was missing, here it is the paper.</para>
    /// </summary>
    public const string NothingToBinLine =
        "Nothing in the sleeve this bin has any business with — paper is what it takes off your hands, and "
        + "rounds and relics are not evidence.";

    /// <summary>#828 · The picker's own title, naming the bucket the captain is actually standing at. The
    /// bin is never guessed at on this page: it is the one you walked to, and the title says which.</summary>
    public static string PickerTitle(Tier tier) => $"{Glyph} WHAT GOES IN {TheBin(tier).ToUpperInvariant()}";

    /// <summary>#828 · …and what the KEYBAR says while the captain stands at one. A verb nobody is told
    /// about is a verb nobody has (#212/#537, and the owner pressing T at a map that never mentioned it):
    /// the strip at the bottom of the screen reads "E — use" on every floor of this building, which is
    /// exactly nothing at the one spot where [E] does this. It names the RUNG, like every other sentence
    /// here, because which bucket you are about to feed is the whole decision.</summary>
    public static string KeyPrompt(Tier tier) => $"{Glyph} E — feed {TheBin(tier)}";

    /// <summary>
    /// #828 · THE QUIET FLAG on a sheet nothing has been dug out of yet.
    ///
    /// <para>Owner's two-tier reading law, same issue: <i>"the read from inventory and dig at it… If we dig
    /// at something then we have really read it with care"</i> — and his ruling on what the picker does about
    /// it: glanced-only papers <b>deserve a quiet flag rather than a refusal</b>. So this is a word on a row
    /// and never a gate. The captain may always bin their own paper; the game simply says what it costs.</para>
    /// </summary>
    public const string NotYetWorkedFlag = "not yet worked";

    /// <summary>#828 · …and the warning behind that flag, said BEFORE the press, because the price of a press
    /// is known before the press. It is the completeness law read backwards: after a dig the book holds
    /// everything the sheet could show, so binning costs nothing — and before one it costs the sheet.</summary>
    public const string NotYetWorkedWarning =
        "Nothing has been dug out of this one yet: tear it up and whatever it had to say goes with it, "
        + "because the book was never told.";

    /// <summary>#828 · …and what a row that IS done says instead, in the book's own register.</summary>
    public const string AlreadyInTheBookFlag = "in the book";

    /// <summary>
    /// #828 · THE ORDER THE PICKER LISTS IN: worked sheets lead.
    ///
    /// <para>The natural feed, and the two-tier law's own consequence — <i>you can only safely destroy what
    /// you have DUG, because only the dig guarantees the book kept it</i> — so the finished ones are the ones
    /// under the captain's thumb. Nothing is hidden: the rest follow, flagged, one press away.</para>
    ///
    /// <para>STABLE within each half, so the sleeve's own order survives inside the two groups: a list that
    /// re-shuffled the unworked papers every time somebody dug one would be a list nobody can learn. It lives
    /// in Core, over a predicate, because "which of these is done" is the client's fact about a running
    /// world and "which end of the list they go" is a law.</para>
    /// </summary>
    public static List<T> WorkedLeading<T>(IReadOnlyList<T> rows, Func<T, bool> worked)
    {
        ArgumentNullException.ThrowIfNull(worked);
        var order = new List<T>((rows ?? []).Count);
        foreach (T row in rows ?? [])
        {
            if (worked(row))
            {
                order.Add(row);
            }
        }
        foreach (T row in rows ?? [])
        {
            if (!worked(row))
            {
                order.Add(row);
            }
        }
        return order;
    }

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
            Tier.Chute =>
                $"{Glyph} You tear it up, pull the hatch and let it go — {what}, out of the sleeve, and "
                + $"something takes it a long way down.{kept}",
            // #828 · THE ONE SENTENCE IN THIS FILE THAT IS ALLOWED TO SAY THE PAPER IS GONE. It says it
            // because the captain SAW it, which is the whole of what the top rung buys — and it is written
            // in the past tense of a thing watched rather than of a thing done, because standing there is
            // the act.
            _ =>
                $"{Glyph} You feed it in and watch it taken — {what}, drawn out of your fingers a "
                + $"centimetre at a time, and what falls into the drawer underneath is chaff.{kept}",
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
    /// <param name="what">The document, in the captain's own words.</param>
    /// <param name="tier">Which bucket took it.</param>
    /// <param name="boring">#798 item 2 · Whether what went in was the BULK of a split file — the folder
    /// with the one damning sheet already out of it. The shape does not change (bucket first, document
    /// after, one flat clause about what was left behind); the last clause does, because what was left
    /// behind is a different thing. See <see cref="LeftInTheBin"/> for the ladder, and
    /// <see cref="PageGranularity.BulkDisposalClause"/> for the clause itself.</param>
    public static string DisposalNote(string what, Tier tier, bool boring = false)
    {
        ArgumentNullException.ThrowIfNull(what);
        // The BUCKET first and the document after it, for two reasons that agree: the notebook clips a
        // title to 88 characters (CaseThreads.TitleLength), and the one word a later arc has to be able to
        // read back is the one saying where the pieces went — and a label with its own em-dash clause in it
        // ("maintenance log, two hands — a mention") made "Tore up {what} and put it in…" unreadable on the
        // page. Both faults were on the screen at once, which is where they were found.
        // #828 · The SHAPE is the same for every rung — bucket first, document after, one flat clause about
        // what was left behind — because a later arc reads these back and a note that changed shape at the
        // top of the ladder would be a second format to parse. What differs is the last clause, and it has
        // to: three of these rungs left a thing in a room, and the fourth left nothing anywhere.
        // #798 item 2 · …and the fifth answer to that same last clause: the bulk of a split file. It is asked
        // through LeavesSomethingToFind rather than against the enum, so the top rung cannot end up claiming
        // a boring folder was left in a bin it destroyed — there is nothing in that drawer to read as
        // anything, and the note that said otherwise would be the third named bug class in a book entry.
        if (boring && LeavesSomethingToFind(tier))
        {
            return $"Torn up and put in {TheBin(tier)}: {what}. {PageGranularity.BulkDisposalClause}";
        }

        return tier == Tier.SecureDisposal
            ? $"Fed to {TheBin(tier)}: {what}. Watched it go; there is nothing left of it to find."
            : $"Torn up and put in {TheBin(tier)}: {what}. Nothing of it was left on the table.";
    }

    /// <summary>
    /// #828 · DOES THIS RUNG LEAVE ANYTHING FOR ANYBODY TO FIND? The ladder's whole point, as a predicate.
    ///
    /// <para>Three of the four are HANDOVERS (#775: professionals empty every bin in this building) and the
    /// game never says which was enough — that is the bet, and the captain finds out only if it comes back.
    /// The secure rung is the one that answers no, and it answers no for a reason the player watched happen
    /// rather than for a reason a sentence claimed.</para>
    ///
    /// <para>It lives here, over the enum, so the act, the words and any later arc that decides whether
    /// something comes back all read ONE answer. A caller that decided this for itself would be the fifth
    /// bug class with a bin in its hand.</para>
    /// </summary>
    public static bool LeavesSomethingToFind(Tier tier) => tier != Tier.SecureDisposal;
}
