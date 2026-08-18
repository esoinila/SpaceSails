using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #760 · THE SHIP REMOTE LEARNS TO SEND. Owner, 2026-08-08:
///
/// <para><i>"Where that standing exists, the ship remote … can use it over the air: send messages that open
/// places and enable things — a door unbolted before you reach it, a lift enabled from the surface. The
/// wallet is who you claim to be in person; the remote is who your ship claims to be on the operator's
/// network."</i></para>
///
/// <h3>What it does, and the fence around it</h3>
/// <para><b>It opens exactly what the card would open in person, and not one thing more.</b> The gate it
/// asks about is the same gate the panel reads (<see cref="UndergroundComplex.TheGateReads"/>), judged by
/// the same predicate (<see cref="UndergroundComplex.Honours"/>), and what comes back is that gate's own id
/// — so a send and a walk leave the building in identical states. A remote that could open a second band
/// would be a way to buy depth without the paper, which is #590's law and not this issue's to renegotiate.
/// </para>
///
/// <h3>Three silences, and only one of them is a refusal</h3>
/// <list type="bullet">
/// <item><b>No network.</b> The head office's operator publishes none (#411's rank difference, said in radio
/// this time), and neither do the halls nobody dug (#677). Nothing goes out, nothing is owed, and nothing in
/// the sentence says anybody down there is listening — because nobody is, and the watchers emit nothing
/// (#649/#672).</item>
/// <item><b>Nothing to send.</b> A wallet with no authority in it has nothing to put on the air. The handset
/// says so and charges nothing: you cannot cross an outfit you never addressed.</item>
/// <item><b>Refused.</b> You addressed them, they read it, and the answer is no — in the matrix's own words
/// (#684: one source for what a refusal says). That one costs what a refused card costs at the gate, owed to
/// the operator and to nobody else (#715).</item>
/// </list>
/// </summary>
public static class RemoteSend
{
    /// <summary>What the switch on the handset says. The verb the owner named.</summary>
    public const string OpenLabel = "📡 SEND STANDING";

    /// <summary>What the switch is FOR, on its own face — the price and the promise before the press.</summary>
    public const string Blurb =
        "Put your standing on the operator's own net and ask them to open ahead of you what the card in your " +
        "wallet would open in person.";

    /// <summary>#760 · A silence that is not a refusal: there is no net here to answer with.
    ///
    /// <para>Canon, and the reason this string is written once and read twice: it says nothing about anybody
    /// down there hearing it. No carrier, no register, no acknowledgement — the handset is talking to a moon.
    /// </para></summary>
    public const string NoNetworkLine =
        "📡 You put it on the air and the air keeps it. No carrier, no register, nothing that answers to a " +
        "company — whatever runs this hole does not take messages, and there is no evidence it ever did.";

    /// <summary>#760 · A wallet with no authority in it. The handset has nothing to say and says that, rather
    /// than transmitting a claim to be nobody — which is the difference between this and a refusal, and the
    /// reason only one of the two is owed to anybody.</summary>
    public const string NothingToSendLine =
        "📡 The handset is ready and you have nothing to put in it. Standing is a thing an operator gave " +
        "somebody, and there is not a countersignature in your pocket to send in your name.";

    /// <summary>#760 · Nothing under this floor to unbolt. Said, because a control that does nothing and says
    /// nothing is indistinguishable from a bug (#603's founding law) — and it names no shaft, because there
    /// is not one.</summary>
    public const string NothingBelowLine =
        "📡 There is nothing under this floor for anybody to open. The net is there and the request has no " +
        "object: you are asking a company to unbolt a piece of rock.";

    /// <summary>#760 · What the preamble on a refusal is. The reason itself is the matrix's, word for word —
    /// this is only the fact that it came back over the air rather than out of a slot in a wall.</summary>
    public const string RefusedPreamble =
        "📡 It goes out in your ship's name and the answer is back inside a second. ";

    /// <summary>#760 · What the operator's net did about it.</summary>
    /// <param name="Worked">Whether something opened.</param>
    /// <param name="Line">Told on-screen, always (#684/#736). Never empty, in any of the four cases.</param>
    /// <param name="OpenedGateId">The gate that is now unbolted ahead of the captain — the card id the lift
    /// panel reads — or null. Exactly one gate, ever: the one the wallet would have opened in person.</param>
    /// <param name="Charge">What this cost, and who is owed it (#715). Zero for everything except a refusal.</param>
    public readonly record struct Sent(
        bool Worked, string Line, string? OpenedGateId, UndergroundComplex.HeatCharge Charge);

    /// <summary>#760 · Is there anything to send, and anybody to send it to? Published so a caller can label
    /// the switch honestly — never so it can HIDE it. The switch is drawn either way and
    /// <see cref="Send"/> answers, because a control that is not there teaches nothing (#212).</summary>
    public static bool CanSend(string bodyId, int level, IReadOnlyList<Satchel.Item>? carried)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return TheNet(bodyId, level)
            && TheGate(bodyId, level) is { } gate
            && UndergroundComplex.StandingFor(carried, gate) is not null;
    }

    /// <summary>#760 · Send it. The whole verb, in one pure call.</summary>
    /// <param name="bodyId">The site the captain is standing on or in.</param>
    /// <param name="level">The floor they are on; the gate is the one under this car's band, exactly as the
    /// panel derives it (#677 — the next shaft that EXISTS, never <c>band + 1</c>).</param>
    /// <param name="carried">The satchel. Anything that is not an authority is not in the wallet.</param>
    /// <param name="heatAtThisOperator">#715 · What this site's outfit remembers about this captain
    /// (<see cref="IllegalHeat.HeatAtSite"/>). Zero is the default and the old behaviour exactly.</param>
    public static Sent Send(
        string bodyId, int level, IReadOnlyList<Satchel.Item>? carried, int heatAtThisOperator = 0)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        if (!TheNet(bodyId, level))
        {
            return new Sent(false, NoNetworkLine, null, UndergroundComplex.NothingOwed);
        }

        if (TheGate(bodyId, level) is not { } gate)
        {
            return new Sent(false, NothingBelowLine, null, UndergroundComplex.NothingOwed);
        }

        // Nothing in the wallet at all is not a refusal — see NothingToSendLine. Counted here rather than
        // asked of the matrix, because the matrix answers an empty wallet with what a GATE says to somebody
        // standing in front of it, and nobody is standing in front of anything.
        //
        // #715 · It is asked ABOVE the heat clause on purpose: a handset with nothing in it addressed nobody,
        // and an outfit cannot have stopped answering a message that was never sent.
        if (Satchel.OfKind(carried, Satchel.Kind.Authority).Count == 0)
        {
            return new Sent(false, NothingToSendLine, null, UndergroundComplex.NothingOwed);
        }

        // ── #715 · THEY HAVE STOPPED ANSWERING THAT SHIP ────────────────────────────────────────────────
        //
        // The one effect in this feature that takes a verb away, and it is above the standing read because
        // that is what has happened: nobody at the far end read the wallet. A company with this much of your
        // ship's name on a page does not process its requests and then decline them; it stops processing
        // them. The charge is the same charge any refused send books (#929), owed to the same outfit.
        if (IllegalHeat.TheNetStopsAnswering(heatAtThisOperator))
        {
            return new Sent(
                false, RefusedPreamble + IllegalHeat.TheNetWillNotAnswerLine, null,
                IllegalHeat.Charge(bodyId, IllegalHeat.Crossing.RefusedSend));
        }

        if (UndergroundComplex.StandingFor(carried, gate) is { } standing)
        {
            return new Sent(true, AcceptedLine(standing, gate), gate.Id, UndergroundComplex.NothingOwed);
        }

        // #684 · The reason is the MATRIX'S reason, verbatim. The whole wallet went out in one burst — the
        // handset is not going to send them one at a time — so this is the fan's own answer, and the ladder
        // decides which refusal is worth saying exactly as it does at the gate.
        SatchelTry.Outcome refused =
            SatchelTry.OfferWallet(carried, SatchelTry.Target.ShaftGate, gate.Id);

        return new Sent(
            false, RefusedPreamble + refused.Line, null, UndergroundComplex.RefusedAtTheGate(bodyId));
    }

    /// <summary>#760 · What the net says when it opens something. It names the card that did it and the gate
    /// it opened, because the captain is not standing there to watch it happen — the whole difference between
    /// this verb and walking up to the panel is that the thing happens somewhere you are not.</summary>
    public static string AcceptedLine(
        UndergroundComplex.AuthorityCard read, UndergroundComplex.AuthorityCard gate) =>
        $"📡 It goes out in your ship's name against {UndergroundComplex.CardTitle(read)}, and somewhere " +
        $"below you a gate that has not been asked for anything in decades takes the request and does as it " +
        $"is told. Shaft {gate.Band + 1} is unbolted before you get to it. Nothing acknowledges; the bolt " +
        "simply is not there any more.";

    /// <summary>#760 · Is there a network here at all?
    ///
    /// <para>Two silences, one answer. The operator either publishes a net or does not
    /// (<see cref="SiteOperator.Operator.PublishesNetwork"/> — the head office does not, #411). And the band
    /// nobody dug has none whatever the building above it runs, because nothing down there is a facility and
    /// no company ever wired it (#677). The second is asked of BOTH ends of the trip — the floor the captain
    /// is standing on and the band the gate opens onto — so a send can never reach into the halls.</para></summary>
    private static bool TheNet(string bodyId, int level)
    {
        if (!SiteOperator.Of(bodyId).PublishesNetwork || UndergroundComplex.IsFound(bodyId, level))
        {
            return false;
        }

        return UndergroundComplex.NextShaftBelow(bodyId, level) is not { } next
            || !UndergroundComplex.IsFound(bodyId, UndergroundComplex.BandTop(next));
    }

    /// <summary>#760 · WHICH gate a send is about: the one under this car's band, derived exactly as the
    /// panel derives it. One arithmetic, asked twice, never written twice.</summary>
    private static UndergroundComplex.AuthorityCard? TheGate(string bodyId, int level) =>
        UndergroundComplex.NextShaftBelow(bodyId, level) is { } next
            ? new UndergroundComplex.AuthorityCard(bodyId, next)
            : null;
}
