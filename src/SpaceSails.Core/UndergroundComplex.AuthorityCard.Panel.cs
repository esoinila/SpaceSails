using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #251 · THE PANEL READS YOUR WALLET (#684) — the gate's automatic read, whether it wants a face, and the
/// read that is told as a scene.
///
/// <para>Split out of <c>UndergroundComplex.AuthorityCard.cs</c> under #251 as a pure move: one contiguous
/// run, no member renamed, re-scoped or re-ordered, and no field.</para>
/// </summary>
public static partial class UndergroundComplex
{
    // ── #684 · THE PANEL READS YOUR WALLET WITHOUT BEING ASKED, AND THAT READ IS A SCENE ───────────────
    //
    // Owner's ruling, on whether the shaft gate should become a TRY target like the doors: it should not.
    // "The panel's unprompted wallet-read IS its character" — a machine that goes through your pockets for
    // you is the whole administrative horror of this place in one gesture, and putting a verb in front of it
    // would turn the building polite.
    //
    // What was wrong was never the interaction. It was that the read happened in SILENCE and then answered
    // out of a SECOND set of sentences. `SatchelTry.Target.ShaftGate` carries the sharpest refusal matrix in
    // the game — #679/#683 taught it to tell "another shaft of THIS site" from "somebody else's building",
    // each named — and it had no client caller at all, while the panel said a flat "every one of them
    // countersigned, current, and for another shaft" out of `WrongCardLine`. Two answers to one question,
    // and the better one was the one nobody could read. That is this repo's third named bug class wearing a
    // costume: the sim knowing a thing the sentence does not say.
    //
    // So `WrongCardLine` is GONE and the matrix is the source. This composes the read into the house card
    // idiom (#528) so the answer is TOLD rather than muttered — art, a title, and the matrix's own line
    // verbatim — and per #736's law the line the player acts on lives ON the card that is up, never only in
    // a pulse behind its backdrop.

    /// <summary>#684 · The panel's read of the wallet, and the card it is told on.</summary>
    /// <param name="Worked">Whether the gate opened. False is a refusal, and <paramref name="Line"/> names
    /// its reason either way (#603's law).</param>
    /// <param name="Line">The matrix's own sentence, verbatim. Nothing here rewrites it.</param>
    /// <param name="Presented">The card the gate actually read, or null when there was nothing in the wallet
    /// to read. It is what decides the face on the card (#695) — the office that issued THIS one.</param>
    /// <param name="Label">The card's title.</param>
    /// <param name="ArtUrl">The presented card's own face, or the nameless fallback when none was presented
    /// — which can never impersonate one of the five offices.</param>
    public readonly record struct GateRead(
        bool Worked, string Line, AuthorityCard? Presented, string Label, string ArtUrl);

    /// <summary>#684 · What the story card is called, at a refusal and at a reading alike. It names the thing
    /// the owner declined to put a verb in front of: the machine goes through your wallet, and you watch.</summary>
    public const string GateReadLabel = "🎫 THE PANEL READS YOUR WALLET";

    /// <summary>#684 · The gate below this car's band, reading what the captain happens to be carrying.
    ///
    /// <para>The judgement is <see cref="SatchelTry.ReadTheWallet"/>'s and only its — this asks the building
    /// which shaft the gate serves and then does as it is told.</para></summary>
    /// <param name="bodyId">The site.</param>
    /// <param name="standingLevel">The floor the car is on; the gate is the one under this car's band.</param>
    /// <param name="carried">The satchel. Anything that is not an authority is not in the wallet.</param>
    /// <param name="heatAtThisOperator">#715 · What the outfit that runs this site remembers about this
    /// captain (<see cref="IllegalHeat.HeatAtSite"/>). Zero — a captain nobody has anything on — is the
    /// default and the old behaviour exactly, so every caller and every guard written before the meter
    /// existed still asks the question it always asked.</param>
    public static GateRead TheGateReads(
        string bodyId, int standingLevel, IReadOnlyList<Satchel.Item>? carried, int heatAtThisOperator = 0)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        // #677 · The next shaft that EXISTS. Under an unlisted band there is a band with nothing dug in it,
        // and a gate named for it would be a refusal about solid rock. Where the building has nothing below
        // at all there is no gate to read, and the band arithmetic is only a name for a card nobody holds.
        int band = NextShaftBelow(bodyId, standingLevel) ?? (BandOf(Math.Min(standingLevel, -1)) + 1);
        var gate = new AuthorityCard(bodyId, band);

        SatchelTry.WalletRead read =
            SatchelTry.ReadTheWallet(carried, SatchelTry.Target.ShaftGate, gate.Id);

        AuthorityCard? presented =
            read.Read is { } item && AuthorityCard.TryParse(item.Id, out AuthorityCard c) ? c : null;

        // ── #715 · AND THEN THE OTHER QUESTION, WHICH A COLD GATE NEVER ASKS ────────────────────────────
        //
        // Owner's ruling: the cost of heat is PRESSURE and never a lockout. This is the mildest shape that
        // has teeth — the card is still read, still correct, and still not enough on its own: an outfit that
        // remembers you wants the pass with your face on it as well, on the FIRST press, where a gate that
        // has never heard of you does not ask at any press.
        //
        // It is not a lock. The site's own pass opens it (#804 — the gig is having gone down), and so does
        // walking away for a few hours, because this meter cools in absence and in nothing else. And it is
        // owed to ONE outfit: the identical card at the identical band of a company that has heard nothing
        // opens on the first press, which is exactly the difference the issue asks the player to notice.
        if (read.Outcome.Worked && TheGateWantsAFaceHere(bodyId, carried, heatAtThisOperator))
        {
            return new(
                false, IllegalHeat.TheGateWantsAFaceLine, presented, GateReadLabel,
                presented is { } held ? AuthorityCardArtUrl(held) : AuthorityCardFallbackArtUrl);
        }

        return new(
            read.Outcome.Worked, read.Outcome.Line, presented, GateReadLabel,
            presented is { } shown ? AuthorityCardArtUrl(shown) : AuthorityCardFallbackArtUrl);
    }

    /// <summary>
    /// #715 · <b>DOES THIS SITE'S GATE WANT A FACE WITH THE PAPER?</b> The one predicate, read by the PANEL
    /// (<see cref="LiftPanel"/> — which button is drawn, and whether it opens) and by the READ
    /// (<see cref="TheGateReads"/> — what the card says when it does not). Two callers of one question, never
    /// two questions: a panel that refused while the card said the gate opened is this house's third named
    /// bug class, and it would land in the one system whose register is entirely procedure.
    ///
    /// <para><b>Never at the head office.</b> There is no gate there to ask anything, and that absence is the
    /// rank difference (#411) rather than an oversight — a parent undertaking that started checking passes
    /// would be a branch office with a bigger sign on it.</para>
    /// </summary>
    public static bool TheGateWantsAFaceHere(
        string bodyId, IReadOnlyList<Satchel.Item>? carried, int heatAtThisOperator)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return !IsHeadOffice(bodyId)
            && IllegalHeat.TheGateWantsAFace(heatAtThisOperator)
            && !PatrolBeat.BadgeHeld(bodyId, carried);
    }

    /// <summary>#684 · The same read, the other way it can end — told at the ARRIVAL rather than at the
    /// panel, because that is where the ride's beat has been said since #689 and a card raised on the frame
    /// the floor is rebuilt is a card raised at nobody.
    ///
    /// <para><see cref="CardAcceptedLine"/> stays the one sentence for this: it is about the car going deeper
    /// than the building admits to, which is a different question from the one the matrix answers, and it has
    /// been the panel's success beat since #592.</para></summary>
    public static GateRead TheGateAccepted(AuthorityCard card) => new(
        true, CardAcceptedLine(card), card, GateReadLabel, AuthorityCardArtUrl(card));
}
