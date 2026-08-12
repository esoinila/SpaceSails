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
///
/// <para><b>#837 · AND THE OWNER AMENDED THE REACH.</b> Evening playtest, 2026-08-11, satchel open on twelve
/// loose rounds: <i>"I would expect like load it into one or both guns."</i> The gun he meant was on his own
/// back. So the second refusal below is no longer <i>"it is on your back"</i> — it is <b>out of reach</b>,
/// and a bot riding the sling is never out of reach of the hands carrying it. Loading your own carried gun
/// is the LEAST demanding version of this verb, and the build was refusing exactly that one.
/// <see cref="WithinHands"/> is where the amended law lives, and both grips ask it.</para>
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
    /// <item><b>It is out of reach.</b> #837: not "it is on your back" — a gun on your back is the easiest
    /// thing in the world to hand rounds to. It is the one standing across the room, and the fix for it is
    /// three paces rather than a sentence about the division of labour. <see cref="WithinHands"/> decides
    /// it, so the [I] grip and the satchel row cannot come to two views of one arm's length.</item>
    /// <item><b>The drum is full.</b> Ninety-nine is ninety-nine.</item>
    /// <item><b>Two kinds in one drum.</b> A magazine holding issue ball and lab rounds at once could not
    /// answer what the next trigger pull does, and #603 hangs a real behaviour off the answer (one round per
    /// target, four deep, and lethal to the firer inside seven du). So a drum with something in it takes
    /// more of the SAME thing, and a drum at 00 takes anything.</item>
    /// </list>
    /// </summary>
    public static Load Offer(
        string unit, int rounds, string? loadedId, bool inReach, int offered, string? offeredId)
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

        if (!inReach)
        {
            return new(false, 0, offered, have, loaded,
                $"🔫 {unit} is standing too far off to hand anything to. Walk over to it — your arms are " +
                "the length they are, and rounds do not go in from across a room.");
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

    // ── #837 · THE SECOND GRIP: LOAD IT FROM THE ROW ─────────────────────────────────────────────────────
    //
    // Owner, evening playtest with the satchel open on '12 loose rounds' (2026-08-11): "I would expect like
    // load it into one or both guns." He was looking straight at the ammunition and the only verb on the row
    // said READ IT. The put verb existed — it was hiding behind a position, the way #798's shredder hid
    // behind a bin until #828 gave it the other door.
    //
    // Everything below is the house's own two-grips pattern: the row opens a chooser, the chooser picks a
    // TARGET and a COUNT, and not one line of it does arithmetic. `Offered` is a preview built by calling
    // `Offer` — the same pure function the press commits with — which is why "what the picker shows equals
    // what the MAGAZINES line will read afterwards" is structural rather than a promise somebody keeps.

    /// <summary>#837 · CAN YOUR HANDS GET TO IT? The amended reach law, in one place because two grips ask
    /// it.
    ///
    /// <para>A bot RIDING THE SLING is always yes: it is on the captain's own back, and the arms that are
    /// carrying it can reach it. A bot SET DOWN is a walk — <paramref name="radiusDu"/> is the same arm's
    /// length the positional grip has always used, handed in by the client because a du on a deck plan is a
    /// client fact.</para></summary>
    public static bool WithinHands(bool deployed, double distanceDu, double radiusDu) =>
        !deployed || distanceDu <= radiusDu;

    /// <summary>#837 · HOW MANY OF THE POCKET WOULD ACTUALLY GO IN — the count stepper's ceiling, and the
    /// default share.
    ///
    /// <para>It is <see cref="Offer"/>'s own answer and not a second sum. Drum space, the kind already in
    /// the magazine, the reach and the size of the pocket all bear on it, and every one of those rules is
    /// already written once, up there. A ceiling computed here as <c>min(pocket, cap - have)</c> would be
    /// right today and wrong the first time a rule moved — the fifth named bug class, arriving as a number
    /// that looks fine.</para>
    ///
    /// <para>Zero is a real answer: a full drum, a wrong kind, a bot out of reach. The row that gets it is
    /// still drawn and still pressable, because the refusal is the thing worth reading (#212/#603).</para></summary>
    public static int Ceiling(
        string unit, int magazine, string? loadedId, bool inReach, int pocket, string? offeredId) =>
        Offer(unit, magazine, loadedId, inReach, Math.Max(pocket, 0), offeredId).Accepted;

    /// <summary>#837 · One gun the chooser can offer the rounds to, as the world holds it.</summary>
    public readonly record struct Aim(
        string Unit, int Magazine, string? AmmoId, bool Deployed, bool InReach);

    /// <summary>#837 · One ROW of the chooser: who, where, what the drum reads now, what it will read after
    /// this press, and what stays in the pocket. Every figure on it comes out of <see cref="Offer"/> and
    /// <see cref="SentryBot.MagazineCell"/> — the picker owns no numbers of its own.</summary>
    /// <param name="Now">The drum as the MAGAZINES line prints it this instant.</param>
    /// <param name="After">…and as that same line will print it the moment this row is pressed. The price
    /// known before the press (#696), applied to a magazine.</param>
    /// <param name="Ceiling">The most this row can take right now; 0 on a refusal.</param>
    /// <param name="Share">What this row OFFERS when it is pressed, and what the stepper reads. On a row
    /// that works it is exactly what goes in (the share is clamped to the ceiling, so the drum takes all of
    /// it); on a refused row it is the whole pocket, which is what makes the press answer with the refusal a
    /// captain needs — a full drum, a wrong kind — instead of "there is nothing in the pocket to load".</param>
    /// <param name="Kept">What is still in the pocket afterwards. Includes rounds the captain deliberately
    /// held back, which is the whole point of the stepper, and nothing ever falls between the two.</param>
    public readonly record struct Pick(
        string Unit, string Where, string Now, string After,
        int Ceiling, int Share, int Pocket, int Kept, bool Worked, string Note);

    /// <summary>
    /// #837 · BUILD ONE ROW. <paramref name="wanted"/> is the stepper's position, or null for the default —
    /// and the default is ALL, the owner's own ruling: <i>"by default I guess it would be all into one in
    /// most cases, but we should give flexibility here."</i> One press, done; the common case pays no
    /// interface tax.
    ///
    /// <para>A refused row previews with the WHOLE pocket, so the sentence it carries is the refusal a
    /// captain needs — a full drum saying it is full, a wrong kind saying what is already in it — rather
    /// than "there is nothing in the pocket to load", which would be true of a zero share and true of
    /// nothing else.</para>
    /// </summary>
    public static Pick Offered(Aim aim, int pocket, string? offeredId, int? wanted)
    {
        int have = Math.Max(pocket, 0);
        int ceiling = Ceiling(aim.Unit, aim.Magazine, aim.AmmoId, aim.InReach, have, offeredId);
        int share = ceiling <= 0 ? have : Math.Clamp(wanted ?? ceiling, 1, ceiling);
        Load load = Offer(aim.Unit, aim.Magazine, aim.AmmoId, aim.InReach, share, offeredId);
        int kept = have - load.Accepted;

        return new(
            aim.Unit,
            SentryBot.WhereItIs(aim.Deployed),
            SentryBot.MagazineCell(aim.Magazine),
            SentryBot.MagazineCell(load.Magazine),
            ceiling,
            share,
            have,
            kept,
            load.Worked,
            load.Worked ? PriceLine(load.Accepted, kept) : load.Line);
    }

    /// <summary>#837 · What a row costs, said before it is pressed. Two figures and no fraction — the going
    /// and the staying, which between them are the whole pocket.</summary>
    public static string PriceLine(int going, int kept) =>
        kept <= 0
            ? $"All {going} of them into this one, and the pocket is empty after it."
            : $"{going} in by hand; {kept} stay{(kept == 1 ? "s" : "")} in the pocket.";

    /// <summary>#837 · …and afterwards, when the captain chose to hold some back. <see cref="LoadedLine"/>
    /// already accounts for what the drum REFUSED ("the drum would not take them"); this is the other
    /// remainder, and it must be said in different words because it is a different fact — nothing may
    /// vanish in the seam between a pocket and a drum, and neither may anything be blamed on a drum that
    /// was perfectly willing.</summary>
    public static string KeptBackLine(int kept) =>
        $"{kept} stay{(kept == 1 ? "s" : "")} in the pocket because you kept {(kept == 1 ? "it" : "them")} " +
        "there.";

    /// <summary>#837 · The word on the control that opens the chooser, beside the row's own read it →.</summary>
    public const string LoadLabel = "load →";

    /// <summary>#837 · …and what it says before it is pressed, when there is something to press it at.</summary>
    public const string LoadHintLine =
        "Put these into a gun by hand — pick which one, and how many.";

    /// <summary>#837 · The chooser's own subtitle: what is being spent, and the standing law that you need
    /// not spend all of it.</summary>
    public static string ChooserBlurb(int pocket, string kindName) =>
        $"🔫 {pocket} × {kindName} in the pocket. Pick a gun — all of them go in unless you say otherwise, " +
        "and whatever you keep back stays yours.";

    /// <summary>#837 · The chooser with nobody in it. Not a blank page: a captain who opened this and found
    /// nothing needs the sentence that says what would fix it.</summary>
    public const string NothingInReachLine =
        "🔫 Nothing here will take them. There is no sentry on your sling and none set down within arm's " +
        "reach — carry one, or walk to the one holding the line, and the rounds go in by hand.";

    /// <summary>#837 · Offered on a handful of rounds and on nothing else. A key, a paper and a collar do
    /// not go in a magazine, and on those rows this is not a refusal — it is a verb that does not apply, the
    /// same rule the shredder follows about evidence.</summary>
    public static bool IsLoadable(Satchel.Kind kind) => kind == Satchel.Kind.Rounds;

    /// <summary>#837 · The stepper's two hints and the way back, so no word on this page is typed in a
    /// markup file.</summary>
    public const string FewerHint = "One fewer into this gun — the rest stay in your pocket";

    /// <inheritdoc cref="FewerHint"/>
    public const string MoreHint = "One more into this gun";

    /// <inheritdoc cref="FewerHint"/>
    public const string BackLabel = "← back to the pocket";
}
