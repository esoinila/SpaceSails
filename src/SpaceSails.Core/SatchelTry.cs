using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #603 · OFFERING SOMETHING TO SOMETHING. The satchel's one verb, resolved in Core so the answer is the
/// same wherever it is asked and so every refusal can be pinned by a test.
///
/// <para>The law all of this obeys, and the reason it lives here rather than in a razor file: <b>a refusal
/// always names its reason.</b> A control that does nothing and says nothing is indistinguishable from a
/// bug, and this ground has shipped that mistake twice in a week — the lift that only went down, and the
/// return that put the captain in a wall.</para>
/// </summary>
public static class SatchelTry
{
    /// <summary>What the captain is holding it up to.</summary>
    public enum Target
    {
        /// <summary>The lift panel's gated button — the shaft below this car's band (#590).</summary>
        ShaftGate,

        /// <summary>A rib's far end: <c>⟶ SECTOR n · d.d km</c>. Has no reader and never will (#590 call 2).</summary>
        SealedWay,

        /// <summary>A room door that will not open, with a department painted on it.</summary>
        RoomDoor,

        /// <summary>A sentry that has run dry (#562's supply line).</summary>
        DrySentry,

        /// <summary>The motion tracker — offering a paper as a lead rather than as reading matter.</summary>
        Tracker,
    }

    /// <summary>What happened. <paramref name="Worked"/> false is a refusal, and <paramref name="Line"/> is
    /// never empty in either case.</summary>
    public readonly record struct Outcome(bool Worked, string Line);

    /// <summary>
    /// Offer <paramref name="item"/> to <paramref name="target"/>.
    ///
    /// <para><paramref name="context"/> is whatever the target needs to judge it — for a shaft gate, the id
    /// of the authority that opens it. Null where the target does not care.</para>
    /// </summary>
    public static Outcome Offer(Satchel.Item item, Target target, string? context = null) => target switch
    {
        Target.ShaftGate => AtShaftGate(item, context),
        Target.SealedWay => AtSealedWay(item),
        Target.RoomDoor => AtRoomDoor(item),
        Target.DrySentry => AtDrySentry(item),
        _ => AtTracker(item),
    };

    private static Outcome AtShaftGate(Satchel.Item item, string? wanted)
    {
        if (item.Kind != Satchel.Kind.Authority)
        {
            return new(false, item.Kind switch
            {
                Satchel.Kind.Rounds => "🔒 The gate is not a thing you can shoot open, and you would not " +
                    "want to be standing here if it were.",
                Satchel.Kind.Paper => "🔒 The gate wants an authority, not a document. Nothing you are " +
                    "holding has a countersignature on it.",
                _ => "🔒 The gate has no interest in what somebody else did. It reads authorities.",
            });
        }

        if (wanted is { Length: > 0 } id
            && string.Equals(item.Id, id, StringComparison.Ordinal))
        {
            return new(true, "🎫 The gate reads it without hesitating.");
        }

        // ── #679 · A REFUSAL THAT SORTS THE WALLET ──
        //
        // Owner: "We need story telling about whether cards etc work or not." The old sentence —
        // "countersigned, current, and for another shaft" — was true of every wrong card there is, which
        // means the second card a captain tried taught them nothing the first had not. TRY is a verb, and
        // an offer that leaves you knowing exactly what you knew before it is a slot machine.
        //
        // So the gate reads the card OUT LOUD, the way a gate would: it says which shaft this one runs, and
        // when the card belongs to another building it says which site issued it. Both facts are printed on
        // the thing in the captain's hand (#679's CardTitle) — the gate is not telling them anything they
        // could not read themselves, it is telling them what it CHECKED.
        //
        // The #590 law is untouched: the wallet is not a skeleton key, and nothing here hints that a
        // different card would open a sealed way.
        if (UndergroundComplex.AuthorityCard.TryParse(item.Id, out UndergroundComplex.AuthorityCard held))
        {
            if (UndergroundComplex.AuthorityCard.TryParse(wanted, out UndergroundComplex.AuthorityCard gate)
                && string.Equals(held.BodyId, gate.BodyId, StringComparison.Ordinal))
            {
                return new(false,
                    $"🔒 The gate reads it, and reads it correctly: this card runs shaft {held.Band + 1} of " +
                    "this site — deeper paper than this gate wants, or shallower, but either way not the " +
                    "authority for the hole in front of you. The countersignatures are fine. The shaft " +
                    "number is not.");
            }

            return new(false,
                $"🔒 The gate reads it, and it is somebody else's business: this one was issued for " +
                $"{BodyNames.Designation(held.BodyId)} SITE. The same office, the same two hands on the " +
                "countersignatures, a different hole under a different moon — and this gate has no opinion " +
                "about any of that. It is looking for its own site code. It does nothing at all.");
        }

        // A card the gate cannot even parse — an edited save, or a future build's authority. It still says
        // why, because a silent nothing is indistinguishable from a bug.
        return new(false,
            "🔒 Countersigned, current, and for another shaft. The gate reads it, decides it is somebody " +
            "else's business, and does nothing at all.");
    }

    private static Outcome AtSealedWay(Satchel.Item item)
    {
        // #590 call 2, held: these doors exist to be walls with a world behind them, and the moment one can
        // be opened every one of them becomes a puzzle. The refusal is honest and FINAL — it does not hint
        // that a different card would do it, because none would.
        string held = item.Kind == Satchel.Kind.Authority
            ? "You hold the card up to it anyway. "
            : "";
        return new(false,
            $"🔒 {held}There is no reader on this. No slot, no plate, no panel — the bolts go through the " +
            "frame and into the rock, and they were tightened from a side you are not on. Nothing you " +
            "could be carrying is the thing this is waiting for, because it is not waiting.");
    }

    private static Outcome AtRoomDoor(Satchel.Item item)
    {
        string held = item.Kind == Satchel.Kind.Authority
            ? "The card means nothing to it. "
            : "";
        return new(false,
            $"🔒 {held}The lock is mechanical and it was turned by somebody who then walked away with the " +
            "key. It is not refusing you; it has simply been shut for longer than you have been alive.");
    }

    private static Outcome AtDrySentry(Satchel.Item item)
    {
        if (item.Kind != Satchel.Kind.Rounds)
        {
            return new(false, "🔫 It is out of ammunition. That is the only thing it wants.");
        }
        return new(true, $"🔫 {item.Count} round{(item.Count == 1 ? "" : "s")} into the hopper. It is not a " +
            "magazine, but it is not nothing.");
    }

    private static Outcome AtTracker(Satchel.Item item)
    {
        if (item.Kind != Satchel.Kind.Paper)
        {
            return new(false,
                "📡 The tracker reads movement and places. There is nothing on this it could plot.");
        }

        // #603 · How much it is worth is a property of the DOCUMENT, and the instrument shows the
        // difference — a mention washes half the ground, a position earns a dot.
        return new(true, FieldClue.Line(FieldClue.CertaintyOf(item.Id)));
    }
}
