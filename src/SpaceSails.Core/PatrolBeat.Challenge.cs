using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

// Subject: the badge, and the challenge a round stops you with (part of PatrolBeat).
public static partial class PatrolBeat
{
    // ── THE BADGE ─────────────────────────────────────────────────────────────────────────────────────
    //
    // Owner: "our own badge once we get a gig". #618 said why it is the load-bearing object: "A disguise is
    // worthless without somebody to fool."
    //
    // It is SITE-SCOPED, exactly like an authority card, and for the card's reason (#679): a pass that
    // worked everywhere would be a skeleton key, and a pass that is read out loud as somebody else's site
    // is the best sentence a refusal can say. One tier in this phase — GENERAL HANDS, off the board's own
    // HIRING notice — because the department ladder is #605's question and not this one's.

    /// <summary>The id a site's pass rides under in the wallet. Same shape as an authority card's: a fact
    /// the vault can store, with the words rebuilt at read time.</summary>
    public static string BadgeId(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return $"badge:{bodyId}";
    }

    /// <summary>Which site a pass is for, or null when nothing can read it.</summary>
    public static string? SiteOfBadge(string? id) =>
        id is { Length: > 6 } && id.StartsWith("badge:", StringComparison.Ordinal) ? id[6..] : null;

    /// <summary>The pass as a thing in the wallet.</summary>
    public static Satchel.Item Badge(string bodyId) => new(Satchel.Kind.Badge, BadgeId(bodyId));

    /// <summary>The glyph the satchel row wears. A card with a face on it, which is the whole difference
    /// between this and every other piece of paper down here.</summary>
    public const string BadgeGlyph = "🪪";

    /// <summary>The one tier this phase issues. It is the board's own <c>HIRING — GENERAL HANDS</c> notice
    /// arriving as an object, which is the cheapest possible way for a pass to mean something.</summary>
    public const string BadgeTier = "GENERAL HANDS";

    /// <summary>What is printed on it. Seeded off nothing — a pass says the site and the tier, and a pass
    /// that said more would be a department, which is a question nobody has ruled on.</summary>
    public static string BadgeTitle(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return $"SITE PASS · {BadgeTier} · {BodyNames.Designation(bodyId)} SITE";
    }

    /// <summary>Is the captain carrying this site's own pass? The possession IS the state — no flag, no
    /// parallel ledger, the discipline <see cref="CanteenTable.Cover"/> already keeps.</summary>
    public static bool BadgeHeld(string bodyId, IReadOnlyList<Satchel.Item>? carried)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return Satchel.CountOf(carried, Satchel.Kind.Badge, BadgeId(bodyId)) > 0;
    }

    /// <summary>#804 · WHERE IT COMES FROM: the shift you actually turned up for. The Hand's chit says
    /// <i>take this to the lift</i>; the cage's gate reads it and takes you down (#752); and at the bottom
    /// the site does the thing a site does with a body that has arrived on somebody's account — it puts you
    /// on its books. The gig is not the paper; the gig is having gone down.</summary>
    public const string BadgeIssuedLine =
        "🪪 At the bottom of the cage somebody photographs you against a wall, prints a pass while you wait " +
        "and hands it over without looking up. GENERAL HANDS. No department, no expiry, and your name " +
        "spelled the way the rota spells it.";

    /// <summary>What the field book keeps of that. Not "you got a badge" — what the badge turns out to be.</summary>
    public const string BadgeGist =
        "You are on this site's books. There is a pass in your wallet with your face on it and somebody " +
        "else's idea of your name.";

    // ── THE CHALLENGE ─────────────────────────────────────────────────────────────────────────────────
    //
    // #684's ruling, one building along: the panel's unprompted wallet-read IS its character, and the answer
    // is TOLD on a card rather than muttered into a pulse. A man doing a round is the same gesture with a
    // face on it — he does not ask you to press anything, he puts his hand out and reads what is in it.
    //
    // So this is a CARD with two arms and no buttons, and the read is automatic. #746 built the encounter
    // machine for the day a guard stop needs MOVES ("show ID, explain yourself"); this stop has exactly one
    // move and it is one the captain has already made by carrying the thing or not carrying it. Growing it
    // into an Encounter.Scene is the next phase's work and needs no new mechanics — which is that file's
    // whole claim, and this phase leaves it true rather than pre-empting it.

    /// <summary>What the story card is called. It names the moment rather than the man: a round has a
    /// direction and a rhythm, and the frightening thing is that it has stopped.</summary>
    public const string ChallengeLabel = "👮 THE ROUND STOPS AT YOU";

    /// <summary>#804 · The painting the card wears — a contract guard, palm up, clipboard under the arm, a
    /// laminated pass on his chest, in a shotcrete corridor of bolt plates with faint chalk scrawls on the
    /// walls (an accidental #794 nod the owner spotted in his own generation).
    ///
    /// <para>ONE picture for all four rungs, deliberately. The card's body is the same sentence whatever
    /// comes out of the wallet — <see cref="ChallengeCard"/> describes a man who has not read it yet — and a
    /// per-outcome plate would be the picture telling the captain how the read went before he has finished
    /// reading. The verdict lives in the amber row under the image and nowhere else (#736).</para>
    ///
    /// <para>A constant here rather than a literal in the client, for the reason every other plate is one: a
    /// razor that names a jpg is a second answer to "which picture is this moment", and the one that cannot
    /// be kept in step with the prose beside it. #804 shipped caption-only under the degradation law; this
    /// closes that gap without the degradation law changing at all.</para></summary>
    public const string ChallengeArtUrl = "art/the-round-stops-at-you.jpg";

    /// <summary>
    /// What is happening, before anything is read. Evidence, and then it stops (§13.9's discipline) — the
    /// card describes a man doing a task and says nothing at all about what happens next.
    /// </summary>
    public static string ChallengeCard(string plate) =>
        $"{plate} — and they see you before you hear them stop. No shout and no lamp in the face: this " +
        "floor has lights, and you are plainly not what the lights are for.\n\n" +
        "What you get is a hand out, palm up, for the thing everybody who works this corridor carries. " +
        "Nothing about it is urgent. That is the part worth being frightened of — he has done this a " +
        "hundred times, it has come to nothing a hundred times, and he will write down whichever way it " +
        "goes tonight either way.";

    /// <summary>#804 · The guard's read of the wallet, and the card it is told on. Deliberately the same
    /// shape as <see cref="UndergroundComplex.GateRead"/>: two arms, one sentence, one label — so the client
    /// raises the identical card for a pass that works and a pass that does not, and neither arm can grow a
    /// presentation of its own.</summary>
    /// <param name="Satisfied">Whether he walks on. False is a refusal, and <paramref name="Line"/> names
    /// its reason either way (#603's law).</param>
    /// <param name="Line">The read itself, verbatim. Nothing downstream rewrites it.</param>
    /// <param name="Label">The card's title.</param>
    /// <param name="Card">The card's body — who stopped you and what they are doing.</param>
    /// <param name="Consequence">What happens next, or null when nothing does. #736's law is why this is
    /// carried rather than pulsed: the sentence a captain ACTS ON has to live on the card that is up, and a
    /// card is exactly what is up at this moment. Composed with <see cref="Line"/> by <see cref="Told"/>, so
    /// no caller can put the read on the card and the consequence behind the backdrop.</param>
    public readonly record struct Read(
        bool Satisfied, string Line, string Label, string Card, string? Consequence = null)
    {
        /// <summary>The whole of what the card says under the picture: the read, and — when there is one —
        /// what it cost. One string, one region, one source.</summary>
        public string Told => Consequence is { Length: > 0 } cost ? $"{Line}\n\n{cost}" : Line;
    }

    /// <summary>
    /// #804 · WHAT HE FINDS IN YOUR WALLET. Four rungs, and each one teaches something different — #683's
    /// ladder, kept because the ladder is the storytelling: this site's pass, somebody else's site's pass,
    /// the wrong class of paper entirely, and nothing.
    ///
    /// <para>#836 · <b>AND IT READS WHAT WAS HANDED OVER.</b> Owner: <i>"I think I should be able to pick the
    /// badge I show the guard... like Fletch"</i>. This used to take the whole satchel and walk it — pass
    /// first, then anybody's pass, then the chit — which meant the answer was always the BEST paper in the
    /// wallet. A captain who chose the bad one was quietly rescued by the sim, and a wallet with four names
    /// in it would have had no game in it at all.</para>
    ///
    /// <para>So it takes ONE paper, chosen during #833's approach (<see cref="WalletChoice"/>), and the
    /// judgement itself is <see cref="WalletChoice.WhatHappens"/> — one ladder, read by this card and by the
    /// line the book files about it, so the sentence and the paper trail cannot disagree.</para>
    ///
    /// <para>The 2026-08-08 ruling is untouched: there is no TRY verb here, nothing is pressed, and the read
    /// is as automatic as it ever was. What moved is WHEN the captain decided, not whether the man asks.</para>
    /// </summary>
    /// <param name="bodyId">The site whose floor you are standing on.</param>
    /// <param name="plate">Who stopped you (<see cref="PlateOf"/>).</param>
    /// <param name="shown">The paper that went into his hand, or null when nothing did — an empty wallet, or
    /// a captain who had nothing a palm is for.</param>
    public static Read TheGuardReads(string bodyId, string plate, Satchel.Item? shown)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(plate);

        string card = ChallengeCard(plate);

        switch (WalletChoice.WhatHappens(bodyId, shown))
        {
            case WalletChoice.Outcome.Worked:
                return new(true, SatisfiedLine, ChallengeLabel, card);

            // Somebody else's building. Named, because a refusal that reads the site code out loud is worth
            // carrying (#679) — and because a captain who has worked two sites should learn that the second
            // pass is still worth keeping.
            case WalletChoice.Outcome.WrongSite when shown is { } wrong
                                                     && SiteOfBadge(wrong.Id) is { Length: > 0 } site:
                return new(false, WrongSiteLine(site), ChallengeLabel, card, EscortLine);

            // The cage chit. It is a real paper and it is real cover — for the cage. He says so the way you
            // would say a platform number, which is the most bureaucratic refusal available.
            case WalletChoice.Outcome.WrongPaper:
                return new(false, WrongPaperLine, ChallengeLabel, card, EscortLine);

            default:
                return new(false, NothingLine, ChallengeLabel, card, EscortLine);
        }
    }

    /// <summary>The pass works. He is not pleased and he is not suspicious; the paperwork balances and he
    /// has four more corridors to do.</summary>
    public const string SatisfiedLine =
        "👮 He reads it the way a man reads a pass at the end of a shift — the face, the site code, the " +
        "tier — and puts it back in your hand. \"Mind the wet floor round the corner.\" The round picks up " +
        "where it left off.";

    /// <summary>A pass for another site. He reads it correctly and is entirely unmoved by it.</summary>
    public static string WrongSiteLine(string site) =>
        $"🔒 He reads it, and he reads it properly: this one was issued for {BodyNames.Designation(site)} " +
        "SITE. \"That's not us.\" He does not ask how you came by it, and he does not hand it back quickly, " +
        "and neither of those is a threat. It is a man being thorough about somebody else's paperwork.";

    /// <summary>The day-labour chit, on a floor that is not the cage.</summary>
    public const string WrongPaperLine =
        "🔒 He turns the chit over once. \"That's for the cage. This isn't the cage.\" He is not wrong, and " +
        "he says it the way you would say a platform number.";

    /// <summary>Nothing at all. The waiting is the whole of it.</summary>
    public const string NothingLine =
        "🔒 Nothing comes out of your wallet that this floor has ever heard of. He waits the entire time you " +
        "are looking — longer than he needs to, and exactly as long as the form says.";

    /// <summary>
    /// #804 · THE MILDEST HONEST CONSEQUENCE, and the whole of it.
    ///
    /// <para>Owner's law: <i>"a rolling guard has no reason to run after anyone just on sight."</i> Nothing
    /// here escalates. He walks you back to the car, at your pace, and the cost is that somebody now knows
    /// you were on this floor — which is #715's per-entity memory arriving as one line in a book rather than
    /// as a meter, because a meter would be the announcement that issue's canon section rules out.</para>
    ///
    /// <para>#833 · <b>And every clause of it is now literally true.</b> It shipped over a placement — the
    /// captain was PUT at the lift and the guard stayed where he was, so the sentence claimed a walk the sim
    /// did not take, in the feature whose whole register is procedure. The walk is real now (the client's
    /// escort: he plans a route, the captain is walked at his shoulder, both are contacts on the fan the
    /// whole way, <see cref="PumpsLine"/> is said on it), so the only line here that had to change is the one
    /// that is kept for when the ground refuses a route at all — <see cref="EscortCutLine"/>, which admits
    /// the cut instead of narrating it.</para>
    ///
    /// <para>#835 · <b>And it is no longer the whole of it — it is the whole of it for the first
    /// <see cref="EscortsAWatchAllows"/> times.</b> Past that the same walk keeps going, into the car and up
    /// (<see cref="KickOutDueLine"/>). Nothing about this rung changed; a rung was added above it.</para>
    /// </summary>
    public const string EscortLine =
        "👮 He walks you back to the car himself, at your pace, talking about the pumps. He presses the " +
        "button for you and stands there while the doors shut. Nothing is taken, nobody is called, and " +
        "somewhere a line goes into a book with the time on it.";

    /// <summary>What the field book keeps of an escort. It records what happened and never the mechanic —
    /// and it is the first thing this game has ever filed about being KNOWN somewhere.</summary>
    public const string EscortNote =
        "Walked back to the lift by a man on the security rota. No voices, no confiscation, and a line in " +
        "a book with the time on it.";

    /// <summary>#804 · How long a guard leaves it before the round stops at you again. Long enough that an
    /// escort is one event rather than a loop at the lift doors, short enough that it is not a free pass for
    /// the rest of the excursion. FLAGGED for the owner's tuning.</summary>
    public const double AfterTheStopSeconds = 45.0;
}
