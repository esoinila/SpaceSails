using System;
using System.Collections.Generic;
using System.Globalization;
using SpaceSails.Core.Interior;

namespace SpaceSails.Core;

/// <summary>
/// #711 slice 2 · <b>WHAT THE PARCEL IS FOR.</b> The destination, the delivery, and the payment with no
/// sender — the three halves slice 1 deliberately left open (<i>"a captain who is never inspected has
/// carried a box for nothing"</i>).
///
/// <para>Owner, on #794, and it is the whole brief: <i>"If we do business with somebody who does not want
/// to expose their face to us and they want to leave us physical things… they would probably like to use a
/// dead drop."</i> Three properties define the trade and this file keeps all three:</para>
///
/// <list type="number">
/// <item><b>The counterparty never shows a face.</b> There is no contact row, no name, no
/// <see cref="ContactLedger"/> entry, no reputation. The job arrives across a desk that deals in
/// handovers and the money arrives across the same desk later, and nothing in this file can ever
/// introduce the two of you.</item>
/// <item><b>The goods are irreducibly physical.</b> The box goes in the ground. Not a message, not a
/// transfer — <see cref="TreasureCache.Deposit"/>, the same hole coin and cargo ride.</item>
/// <item><b>The drop is the delivery rail.</b> Burying at the named ground IS the delivery. There is no
/// hand-off beat, no second party on screen, and no confirmation that is not a lag and a payment.</item>
/// </list>
///
/// <h3>THE DELIVERY IS #319's OWN SHOVEL AND NOT A SECOND ONE</h3>
///
/// <para>A parcel is buried through <c>TreasureCache.Deposit</c> exactly as anything else light enough is:
/// the same walk, the same DIG HERE press, the same 2D6, the same ✗, the same
/// <see cref="CacheSafety"/> read, the same vault section, the same rebirth. This file adds no record of
/// its own and no second cache — it only ever ASKS the hoard a question: <i>is one of these holes the hole
/// somebody is coming for?</i> That is why a parcel buried on the wrong ground needs no special case at
/// all: it is a buried parcel, on the chest's own terms, and the captain can walk back and dig it up.</para>
///
/// <h3>AND THE PAYMENT IS THE HOLE ITSELF</h3>
///
/// <para>There is no pending-payment ledger anywhere in this feature, and there must not be. The chest in
/// the ground is the record: it says what was buried, where, and when
/// (<see cref="TreasureCache.BuriedSimTime"/>), and every one of those already rides the vault. The
/// payment is <b>due</b> when the clock has passed that moment by <see cref="WatchesBeforeItLands"/>, it is
/// <b>paid</b> the next time the captain opens a dark-web desk, and it is <b>spent</b> because the caller
/// lifts the chest out of the ledger at the same instant — somebody dug. A second visit to the desk finds
/// no chest, so it finds no payment, and "exactly once" is a property of the world rather than of a flag
/// somebody remembered to clear.</para>
///
/// <h3>The law this file is written under</h3>
///
/// <para>Slice 1's law, unchanged: <i>"No card names the pattern."</i> Four sentences are authored here,
/// they are enumerated by <see cref="AllProse"/>, and no word this file can put on a screen names the
/// shape it is part of.</para>
/// </summary>
public static class ParcelDrop
{
    // ── WHERE IT IS GOING ───────────────────────────────────────────────────────────────────────────────

    /// <summary>The seed tag the destination is drawn on. Its own stream, so where a box is going never
    /// moves with what it pays or with how long a desk stays shut after one is confiscated.</summary>
    public const string DestinationTag = "parcel:destination";

    /// <summary>
    /// Where the box is to go: a body, one of that body's own landing sites, and the site's name as the
    /// game already spells it.
    /// </summary>
    /// <param name="BodyId">The moon, out of the pool the caller handed in.</param>
    /// <param name="SiteIndex">Which of the body's 2–4 seeded grounds (<see cref="LandingSites"/>).</param>
    /// <param name="SiteName">That ground's display name, resolved rather than stored twice.</param>
    public readonly record struct Destination(string BodyId, int SiteIndex, string SiteName)
    {
        /// <summary>Is the captain standing on this ground? Body AND site, because since #650 a body is
        /// 2–4 different places and a delivery that answered by body alone would be delivered from
        /// anywhere on the moon — which is the bug #650 fixed for the ✗ itself.</summary>
        public bool IsTheGround(string? bodyId, int? siteIndex) =>
            siteIndex is { } site
            && site == SiteIndex
            && string.Equals(bodyId, BodyId, StringComparison.Ordinal);
    }

    /// <summary>
    /// #711 · <b>WHERE THIS PARCEL IS GOING — SEEDED OFF THE PARCEL AND NOTHING ELSE.</b>
    ///
    /// <para>Deterministic per parcel, which is the whole of what makes the desk row honest: the row the
    /// captain reads after taking it is the same ground the shovel will be judged against a week later,
    /// across a reload, on a machine that has never seen the row. Nothing about the captain, the watch or
    /// the meter is in this roll.</para>
    ///
    /// <para><b>THE POOL IS HANDED IN, AND IT IS THE REAL ONE.</b> Core does not know what sky it is in;
    /// the client reads the scenario's own bodies and filters them with
    /// <see cref="ShuttleExcursion.IsLandableSurface"/> — the same predicate the shuttle's destination
    /// board uses, so a place this generator can name is a place the shuttle can be flown to. An invented
    /// moon is not merely wrong here, it is unreachable, and the job would be a lie the captain could not
    /// even walk to. The pool is sorted ordinally before the roll so the answer does not depend on the
    /// order a scenario happens to list its bodies in.</para>
    ///
    /// <para>The SITE is drawn on its own stream against the body's own board
    /// (<see cref="LandingSites.Count"/>), so it is in range by construction — there is no site index this
    /// can produce that <see cref="LandingSites.At"/> has to clamp.</para>
    ///
    /// <para>Null only when the sky has no landable ground in it at all, which no shipped scenario
    /// produces and a guard is entitled to hand in anyway.</para>
    /// </summary>
    public static Destination? For(string? parcelId, IReadOnlyList<string>? landableBodyIds)
    {
        if (parcelId is null || landableBodyIds is not { Count: > 0 })
        {
            return null;
        }

        var pool = new List<string>(landableBodyIds);
        pool.Sort(StringComparer.Ordinal);

        string bodyId = pool[DiceRule.Roll(DiceRule.Seed($"{DestinationTag}:{parcelId}"), pool.Count).Face - 1];
        int siteIndex =
            DiceRule.Roll(DiceRule.Seed($"{DestinationTag}:{parcelId}:site"), LandingSites.Count(bodyId)).Face - 1;

        return new Destination(bodyId, siteIndex, LandingSites.At(bodyId, siteIndex).Name);
    }

    /// <inheritdoc cref="For(string, IReadOnlyList{string})"/>
    public static Destination? For(Satchel.Item parcel, IReadOnlyList<string>? landableBodyIds) =>
        UnlistedParcel.IsAParcel(parcel) ? For(parcel.Id, landableBodyIds) : null;

    /// <summary>The parcel in a captain's pocket, or null when there is none. Asked of the KIND, the way
    /// <see cref="UnlistedParcel.Held"/> asks it, so the answer does not depend on which haven it came
    /// from.</summary>
    public static Satchel.Item? TheParcelIn(IReadOnlyList<Satchel.Item>? carried)
    {
        foreach (Satchel.Item item in carried ?? [])
        {
            if (UnlistedParcel.IsAParcel(item))
            {
                return item;
            }
        }
        return null;
    }

    /// <summary>The parcel in a HOLE, or null when that chest has none in it. Same question as
    /// <see cref="TheParcelIn(IReadOnlyList{Satchel.Item})"/>, asked of what went into the ground, because
    /// a hole's deposit is a satchel's rows and nothing else.</summary>
    public static Satchel.Item? TheParcelIn(TreasureCache cache) => TheParcelIn(cache.Deposit);

    // ── THE DELIVERY ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #711 · <b>IS THIS HOLE THE DELIVERY?</b> One question, asked of a chest already in the ground, and
    /// it is the only definition of <i>delivered</i> in this game.
    ///
    /// <para>True when the chest holds a parcel, the parcel's own destination names this chest's body and
    /// site, and the chest is the captain's own. Everything else — a parcel buried one site over, a chest
    /// somebody else's map led to, a hole with coin in it — is false, and false means the chest is simply a
    /// chest: it keeps its ✗, its odds and its return dig, exactly as #319 shipped it.</para>
    /// </summary>
    public static bool IsTheDelivery(TreasureCache cache, IReadOnlyList<string>? landableBodyIds) =>
        cache.PlayerOwned
        && TheParcelIn(cache) is { } parcel
        && For(parcel, landableBodyIds) is { } where
        && where.IsTheGround(cache.BodyId, cache.SiteIndex);

    /// <summary>#711 · The same question asked of ground the captain is STANDING on, with a parcel still in
    /// the hand — what the shovel needs to know at the moment it goes in, before there is a chest to
    /// ask.</summary>
    public static bool IsTheRightGround(
        Satchel.Item parcel, IReadOnlyList<string>? landableBodyIds, string? bodyId, int? siteIndex) =>
        For(parcel, landableBodyIds) is { } where && where.IsTheGround(bodyId, siteIndex);

    // ── THE PAYMENT ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The seed tag the money is drawn on — its own stream, so the amount never moves with the
    /// ground.</summary>
    public const string PaymentTag = "parcel:payment";

    /// <summary>
    /// #711 · <b>THE BAND A FACELESS PAYER QUOTES AT, AND WHY IT IS THE COLD ONE.</b>
    ///
    /// <para>Every other number on this desk is quoted at the captain's own meter, because every other
    /// counterparty can read it: the fence prices a clean record at three of the captain's own bribes
    /// (<see cref="BlackOpsKey.FencePrice"/>), the collector prices a seizure off the ship's heat, and the
    /// man with the form prices a fine off his own outfit's file (<see cref="UnlistedParcel.TheFine"/>).
    /// <b>A party who has never seen the captain's face has no file to read</b> — he does not know which
    /// hull turned up, which is the entire point of hiring one this way — so he quotes the band a captain
    /// nobody has filed anything on stands at, and he quotes it before the box ever leaves the desk.</para>
    /// </summary>
    public const int TheColdBand = 0;

    /// <summary>
    /// #711 · <b>WHAT THE JOB PAYS — ONE FINE'S WORTH, THROUGH THE FINE'S OWN FUNCTION.</b>
    ///
    /// <para>The audit that picked it, since this desk's other three rows are each derived from something
    /// and none of them fits a box:</para>
    ///
    /// <list type="bullet">
    /// <item><see cref="CompromisingChip.FencePrice"/> — a CONTRACT's pay plus the fence's markup. There is
    /// no contract here; nobody signed anything and that is the product.</item>
    /// <item><see cref="Inspectorate.FencePrice"/> — what a DOCUMENT is worth across this desk
    /// (<see cref="IntelMarket.BasePrice"/>) times how deep the pass reaches. A parcel is not a document
    /// and reaches nowhere.</item>
    /// <item><see cref="BlackOpsKey.FencePrice"/> — three bribes, at your own meter, because it buys you
    /// out of three catches.</item>
    /// </list>
    ///
    /// <para>So: the one statement this game makes about what a box aboard is WORTH is what a man with a
    /// form takes off you for it, and slice 1 already spelled that once —
    /// <see cref="UnlistedParcel.TheFine"/>, which is <see cref="BustedRule.BribeDemand"/> called. The job
    /// pays exactly one of those, at <see cref="TheColdBand"/>. It is the fence's own arithmetic with the
    /// multiplier that is honest for a single errand, and it means the errand is worth taking and never
    /// worth taking for the money: one bad afternoon at a hot haven costs more than the run pays, which is
    /// the correct shape for the sacrificial truth slice 1 built.</para>
    ///
    /// <para>Seeded off the PARCEL, so the sum is fixed from the moment the box is handed over and cannot
    /// be re-rolled by opening the desk again.</para>
    /// </summary>
    public static int ThePayment(string? parcelId) =>
        UnlistedParcel.TheFine(TheColdBand, DiceRule.Seed($"{PaymentTag}:{parcelId ?? string.Empty}"));

    /// <summary>The fewest watches between the shovel and the money — two, because one would be same-day
    /// and same-day is a receipt. A watch is <see cref="PatronRota.WatchSeconds"/>, the four-sim-hour shift
    /// this whole game's rosters, patiences and fences already turn on; no new unit of time is minted
    /// here.</summary>
    public const int FewestWatchesBeforeItLands = 2;

    /// <summary>…and the most. Four watches is most of a sim day: long enough that the captain has flown
    /// somewhere else and is not standing at the desk waiting, short enough to land inside one sitting.</summary>
    public const int MostWatchesBeforeItLands = 4;

    /// <summary>#711 · How long the money takes, in watches, seeded off the parcel. Deterministic for the
    /// reason the destination is: a lag the captain cannot predict but the world does not re-roll.</summary>
    public static int WatchesBeforeItLands(string? parcelId)
    {
        int span = MostWatchesBeforeItLands - FewestWatchesBeforeItLands + 1;
        int roll = DiceRule.Roll(DiceRule.Seed($"{PaymentTag}:{parcelId ?? string.Empty}:when"), span).Face - 1;
        return FewestWatchesBeforeItLands + roll;
    }

    /// <summary>#711 · The sim-time moment a delivery becomes payable — measured off the chest's own burial
    /// stamp, which is the only clock in this feature and is already in the vault.</summary>
    public static double PayableAt(double buriedSimTime, string? parcelId) =>
        buriedSimTime + (WatchesBeforeItLands(parcelId) * PatronRota.WatchSeconds);

    /// <summary>A payment waiting across a desk: which hole it is for, what it is worth, where the hole is,
    /// and when it came due.</summary>
    /// <param name="CacheId">The chest the caller must now lift out of the ground — somebody dug.</param>
    /// <param name="ParcelId">The box that was in it.</param>
    /// <param name="Amount">Credits, from <see cref="ThePayment"/>.</param>
    /// <param name="Where">The ground it was left on.</param>
    /// <param name="DueAtSimTime">When it came due, for a caller that wants to date the hole.</param>
    public readonly record struct Payment(
        string CacheId, string ParcelId, int Amount, Destination Where, double DueAtSimTime);

    /// <summary>
    /// #711 · <b>IS THERE MONEY ON THIS DESK?</b> The one read, over the hoard the captain already has.
    ///
    /// <para>A chest qualifies when it is a delivery (<see cref="IsTheDelivery"/>) and the clock has passed
    /// <see cref="PayableAt"/>. The FIRST such chest in the ledger's own order is handed back and no other:
    /// one desk visit settles one drop, so two deliveries that came due while the captain was away are two
    /// payments on two visits, each with its own sentence. A loop that paid them both would put one line on
    /// the screen for two events.</para>
    ///
    /// <para><b>Nothing here writes.</b> The caller moves the coin and lifts the chest; this function is a
    /// question, and a question asked twice about an unchanged world gives the same answer twice — which is
    /// exactly what makes "paid exactly once" the caller's easy promise rather than a flag.</para>
    /// </summary>
    public static Payment? ThePaymentThatIsThere(
        IEnumerable<TreasureCache>? caches, IReadOnlyList<string>? landableBodyIds, double nowSimTime)
    {
        foreach (TreasureCache cache in caches ?? [])
        {
            if (!IsTheDelivery(cache, landableBodyIds) || TheParcelIn(cache) is not { } parcel)
            {
                continue;
            }

            double due = PayableAt(cache.BuriedSimTime, parcel.Id);
            if (nowSimTime < due)
            {
                continue;
            }

            Destination where = For(parcel, landableBodyIds)!.Value;
            return new Payment(cache.Id, parcel.Id, ThePayment(parcel.Id), where, due);
        }

        return null;
    }

    // ── AND WHEN A MAN WITH A FORM TOOK IT ──────────────────────────────────────────────────────────────

    /// <summary>How a shut desk is struck off in the captain's durable register of ground already gone
    /// through — the same set, and the same one-tag-per-window shape, the fence's own
    /// <see cref="BlackOpsKey.ThePortHasDealtOne"/> uses.</summary>
    public const string QuietTag = "parcel:none";

    /// <summary>The fewest watches a desk has nothing for a hull after a confiscation.</summary>
    public const int FewestQuietWatches = 2;

    /// <summary>…and the most.</summary>
    public const int MostQuietWatches = 4;

    /// <summary>
    /// #711 · <b>HOW LONG NOBODY HAS ANYTHING FOR THIS HULL.</b> Seeded off the parcel that was taken off
    /// the captain, so the length is a fact about the box that was lost and not a clock somebody started.
    ///
    /// <para>What happens is an ABSENCE and never a refusal: the row is simply not on the desk, the way it
    /// is not on the desk when the captain is already carrying one. Nothing is greyed out, nothing declines
    /// anything, and no sentence anywhere says why — a captain who was fined and then found the work had
    /// dried up is a captain who may draw his own conclusion, and the house idiom (#212) is that a thing
    /// that cannot be done is not drawn.</para>
    /// </summary>
    public static int QuietWatches(string? parcelId)
    {
        int span = MostQuietWatches - FewestQuietWatches + 1;
        int roll = DiceRule.Roll(DiceRule.Seed($"{QuietTag}:{parcelId ?? string.Empty}"), span).Face - 1;
        return FewestQuietWatches + roll;
    }

    /// <summary>The strike-off for one watch. A tag per watch rather than a deadline, because the register
    /// it lives in is a SET of strings that rides the vault — the same reason the fence writes one tag per
    /// window instead of storing a time.</summary>
    public static string NothingForThisHullOn(long watch) =>
        $"{QuietTag}@{watch.ToString(CultureInfo.InvariantCulture)}";

    /// <summary>#711 · Every watch a confiscation at <paramref name="atSimTime"/> silences, starting with
    /// the one it happened on. Bounded by <see cref="MostQuietWatches"/>, so this can never write an
    /// unbounded number of rows into a save.</summary>
    public static IEnumerable<string> TheWatchesWithNothingOnThem(string? parcelId, double atSimTime)
    {
        long from = PatronRota.WatchIndex(atSimTime);
        int watches = QuietWatches(parcelId);
        for (int i = 0; i < watches; i++)
        {
            yield return NothingForThisHullOn(from + i);
        }
    }

    // ── WHAT IT SAYS ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #711 · <b>THE INSTRUCTION, ON THE ROW, ONCE THE BOX IS IN THE POCKET.</b> Fable-authored canon,
    /// verbatim.
    ///
    /// <para>Read what is NOT in it. There is no recipient, no window, no price, no thank-you, and no verb
    /// but the two the captain will actually perform. A job that named somebody would be a job with a face
    /// in it, and the whole product here is that there is not one.</para>
    /// </summary>
    public const string TheInstruction = "No name. A moon, a bearing, a depth. Put it in the ground and leave.";

    /// <summary>
    /// #711 · <b>THE DELIVERY, AT THE SHOVEL.</b> Canon, verbatim, said on the one press where the ground
    /// under the captain is the ground on the row.
    ///
    /// <para>It is the only confirmation the captain ever gets that the job went right, and it confirms
    /// nothing: it is a sentence about a stranger's competence, told by a man standing alone on a moon. No
    /// number, no receipt, no "delivered".</para>
    /// </summary>
    public const string DeliveredLine =
        "In the ground, where somebody who has never seen your face will know to dig.";

    /// <summary>
    /// #711 · <b>THE MONEY, AT THE DESK.</b> Canon, verbatim.
    ///
    /// <para>Four words of fact and three of inference, and the inference is the captain's. Nothing on this
    /// screen ever says who, and nothing ever will.</para>
    /// </summary>
    public const string PaymentLine = "A payment with no sender. Somebody dug.";

    /// <summary>
    /// #711 · <b>WHAT THE FIELD BOOK KEEPS OF A DELIVERY.</b> Canon, verbatim, with the ground's own name
    /// dropped in — the <see cref="PatrolBeat.EscortNote"/> register: a fact, in the captain's own hand,
    /// never a mechanic.
    ///
    /// <para>Lower case and no full stop, because it is an entry and not an announcement. Its SUBJECT is
    /// declared by the author that writes it (#741's law: <i>a subject comes from the author, never from
    /// the prose</i>) — the place, which is the one thing in this sentence the game has printed.</para>
    /// </summary>
    public const string FieldBookEntry = "a parcel, put in the ground at {0} for nobody you have met";

    /// <summary>…and the entry with the ground in it.</summary>
    public static string TheFieldBookEntry(string siteName) =>
        string.Format(CultureInfo.InvariantCulture, FieldBookEntry, siteName ?? string.Empty);

    /// <summary>#711 · The destination as the desk PRINTS it — a functional line in the row format the
    /// desk's other off-the-books rows already use, and deliberately not a sentence. It is composed out of
    /// the caption the treasure map has always used for a sited chest (BODY · SITE), so the row a captain
    /// reads at the desk and the caption he reads over the ✗ are one spelling of one place.</summary>
    public static string DestinationRow(string bodyDisplayName, string siteName) =>
        $"📍 {(bodyDisplayName ?? string.Empty).ToUpperInvariant()} · {(siteName ?? string.Empty).ToUpperInvariant()}";

    /// <summary>Every sentence this slice can put on a screen, for the audit that reads them all. Four, and
    /// the fourth carries its placeholder.</summary>
    public static IEnumerable<string> AllProse()
    {
        yield return TheInstruction;
        yield return DeliveredLine;
        yield return PaymentLine;
        yield return FieldBookEntry;
    }
}
