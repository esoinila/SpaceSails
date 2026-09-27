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
public static partial class RipAndBin
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

        /// <summary>
        /// #828 · THE OFFICE SECURE DISPOSAL — the top of the ladder, and the only rung that is not a bet.
        ///
        /// <para>Owner: <i>"One thing the good offices would also have is a safe paper disposal trashes…
        /// that visually destroy the notes as we watch… a more secure disposal than restaurant trash."</i>
        /// You feed it a sheet and you WATCH it go, and what you watched go cannot be dived for. Every rung
        /// below this one is a wager about somebody else's hands; this one closes the wager instead of
        /// placing it, which is why it stands in a premium suite and nowhere else.</para>
        ///
        /// <para>Appended, never inserted: the ordinal IS the ranking (see the enum's own note), and the
        /// best rung belongs at the end of a list that runs worst-first.</para>
        /// </summary>
        SecureDisposal,
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
    /// <param name="HalfX">#828 · Half the box's width. Three of the four rungs are a bucket
    /// (<see cref="HalfDu"/> on a side, which is why that is the default and why no existing caller passes
    /// this); the secure disposal is a MACHINE the size of the fitting it is, six deck units across the
    /// service strip. A ladder whose top rung is a different SIZE has to publish the size, or every guard
    /// and every reach measures a rectangle it made up.</param>
    /// <param name="HalfY">…and half its depth.</param>
    public readonly record struct Bin(
        Tier Tier, double X, double Y, double StandX, double StandY, string Plate,
        double HalfX = HalfDu, double HalfY = HalfDu)
    {
        /// <summary>
        /// How far this bin is from a spot on the floor — measured to the BOX, not to its middle.
        ///
        /// <para>#828 · It used to be centre-to-point, which is the same answer for a bucket 1.8 du on a
        /// side and a wrong one for a machine six du across: a captain standing at arm's length of the
        /// secure disposal's face is 5.2 du from its centre, and a reach of 4 would have refused a captain
        /// standing exactly where the carve told them to stand. You reach for the THING, not for the
        /// arithmetic mean of it.</para>
        /// </summary>
        public double DistanceTo(double x, double y)
        {
            double dx = Math.Max(Math.Abs(x - X) - HalfX, 0.0);
            double dy = Math.Max(Math.Abs(y - Y) - HalfY, 0.0);
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
}
