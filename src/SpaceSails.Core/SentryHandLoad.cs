using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #803 · THE PUT VERB. Owner, 2026-08-09: <i>"we might want to hand-load them into the bots for some
/// special purposes, like shooting a mechanical lock."</i>
///
/// <para><b>What was already true.</b> Every round a captain finds on the ground AUTO-ROUTES: the shelter
/// press, the outpost hut's locker and a ruin's half-shut drawer all walk their rounds straight into the
/// magazines and print a receipt (#580, #728, #797). That default is correct and this file does not touch
/// it — a captain who wants ammunition in their guns should not have to conduct it.</para>
///
/// <para><b>What was quietly missing.</b> Two of those three fill the drums and then <b>drop whatever will
/// not fit</b>. The receipt is honest about it — it names the rounds that went in, not the rounds that were
/// there — and the rest simply stopped existing, which is the one thing an object in this game is never
/// allowed to do (#587: <i>a find that is shown once is a find that is lost</i>). So the overflow goes in
/// the POCKET, where it is a thing you own and can spend on purpose. That is also where the found-rounds
/// item in #603's satchel finally comes from: the law was written, the door was hung, and nothing in the
/// game ever put a round in a captain's hand.</para>
///
/// <para><b>And the division of labour.</b> The world fills what you CARRY — the tube's belts, the
/// shelter's press, the hut's locker, all of them reaching into the sling. You fill what you SET DOWN. A
/// bot standing out on the line with eleven rounds left is not in the tube being handed a belt, and it is
/// not near the shelter either; it is out there, and the only thing that is going to walk rounds to it is
/// the captain. That asymmetry is the whole reason the put verb is worth having.</para>
/// </summary>
public static class SentryHandLoad
{
    /// <summary>What an offer of rounds did. <paramref name="Worked"/> false is a refusal, and
    /// <paramref name="Line"/> is never empty either way — a control that does nothing and says nothing is
    /// indistinguishable from a bug (the satchel's founding law).</summary>
    /// <param name="Accepted">Rounds that actually went into the drum.</param>
    /// <param name="LeftOver">Rounds still in the pocket afterwards. <c>Accepted + LeftOver</c> is always
    /// exactly what was offered — nothing this file touches may evaporate.</param>
    /// <param name="Magazine">What the counter reads afterwards. Never above
    /// <see cref="SentryBot.MaxMagazine"/>, because the two-digit readout the HUD and the bot both wear
    /// (#797) cannot say a bigger number and a magazine the instrument cannot report is the sim and the
    /// sentence disagreeing about one fact.</param>
    /// <param name="AmmoId">What the drum is loaded with afterwards.</param>
    public readonly record struct Load(
        bool Worked, int Accepted, int LeftOver, int Magazine, string AmmoId, string Line);

    /// <summary>
    /// Hand <paramref name="offered"/> rounds of <paramref name="offeredId"/> to a sentry.
    ///
    /// <para>Four refusals, each naming its reason:</para>
    /// <list type="bullet">
    /// <item><b>Nothing offered.</b></item>
    /// <item><b>It is on your back.</b> The put verb is for a bot you have set down; the sling is what the
    /// world's own fixtures fill.</item>
    /// <item><b>The drum is full.</b> Ninety-nine is ninety-nine.</item>
    /// <item><b>Two kinds in one drum.</b> A magazine holding issue ball and lab rounds at once could not
    /// answer what the next trigger pull does, and #603 hangs a real behaviour off the answer (one round per
    /// target, four deep, and lethal to the firer inside seven du). So a drum with something in it takes
    /// more of the SAME thing, and a drum at 00 takes anything.</item>
    /// </list>
    /// </summary>
    public static Load Offer(
        string unit, int rounds, string? loadedId, bool deployed, int offered, string? offeredId)
    {
        ArgumentNullException.ThrowIfNull(unit);
        int have = Math.Clamp(rounds, 0, SentryBot.MaxMagazine);
        string loaded = loadedId is { Length: > 0 } ? loadedId : Ammunition.Issue.Id;
        string giving = offeredId is { Length: > 0 } ? offeredId : Ammunition.Issue.Id;

        if (offered <= 0)
        {
            return new(false, 0, 0, have, loaded,
                "🔫 There is nothing in the pocket to load.");
        }

        if (!deployed)
        {
            return new(false, 0, offered, have, loaded,
                $"🔫 {unit} is on your back. Set it down first — the belts and the presses reach into a " +
                "sling, and your two hands do not.");
        }

        if (have >= SentryBot.MaxMagazine)
        {
            return new(false, 0, offered, have, loaded,
                $"🔫 {unit} reads {SentryBot.Readout(have)}/{SentryBot.MaxMagazine}. There is nowhere for " +
                "them to go.");
        }

        if (have > 0 && !string.Equals(loaded, giving, StringComparison.Ordinal))
        {
            return new(false, 0, offered, have, loaded,
                $"🔫 {unit} already has {Ammunition.ById(loaded).Name} in it, and these are not that. One " +
                "drum, one kind — run it dry first, or feed the other gun.");
        }

        int accepted = Math.Min(offered, SentryBot.MaxMagazine - have);
        int now = have + accepted;
        string kindNow = have > 0 ? loaded : giving;
        return new(true, accepted, offered - accepted, now, kindNow,
            LoadedLine(unit, accepted, have, now, Ammunition.ById(kindNow), offered - accepted));
    }

    /// <summary>What the captain reads when the rounds go in by hand. It names the number that moved and the
    /// number it moved TO (#740: a figure says what it is), because the whole point of the verb is that the
    /// captain chose where those rounds went.</summary>
    public static string LoadedLine(string unit, int accepted, int was, int now, Ammunition.Kind kind, int leftOver)
    {
        ArgumentNullException.ThrowIfNull(unit);
        string body =
            $"🔫 {accepted} round{(accepted == 1 ? "" : "s")} into {unit} by hand — " +
            $"{SentryBot.Readout(was)} → {SentryBot.Readout(now)}";
        body += kind.Id == Ammunition.Issue.Id ? "." : $", and it knows what they are: {kind.Name}.";
        return leftOver > 0
            ? $"{body} {leftOver} still in the pocket; the drum would not take them."
            : body;
    }

    /// <summary>
    /// #803 · THE ROUNDS THE GUNS COULD NOT HOLD. Every auto-route on the ground fills magazines in order
    /// and then has a remainder, and until now the remainder was simply not mentioned again.
    ///
    /// <para>Returns null when the guns took everything, so a caller adds nothing to the pocket and says
    /// nothing about it — the ordinary case must stay exactly as quiet as it is today.</para>
    /// </summary>
    public static Satchel.Item? IntoThePocket(int leftOver, string? ammoId) =>
        leftOver <= 0
            ? null
            : new Satchel.Item(
                Satchel.Kind.Rounds, ammoId is { Length: > 0 } ? ammoId : Ammunition.Issue.Id, leftOver);

    /// <summary>What is said about the overflow, and only when there is one. Deliberately a small sentence
    /// in the receipt's own register: it is a fact about a pocket, not an event.</summary>
    public static string PocketedLine(int leftOver) =>
        $"🔫 {leftOver} would not fit in any drum you have down here. They go in the pocket loose — you can " +
        "put them where you want them.";
}
