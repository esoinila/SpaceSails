using System;
using System.Collections.Generic;
using System.Globalization;
using SpaceSails.Core.Interior;

namespace SpaceSails.Core;

/// <summary>
/// #794 · <b>THE CHALK MARK — the faceless trade's return leg.</b> Slice 1 built it in a Hive's park; slice 2
/// moved it to the gallery at the end of Selene Gate's observation walk, by the owner's ruling of 2026-09-28.
///
/// <para>Owner, on #794: <i>"I saw an X marked with chalk and I knew there was a dead drop waiting with my
/// name on it…"</i> and, on the trade that makes it an economy: <i>"they would probably like to use a dead
/// drop with some scheduled times of possible use."</i> #711 shipped the first half — a parcel from a
/// dark-web desk, buried at the ground <see cref="ParcelDrop.Destination"/> names, paid a few watches later
/// (<see cref="ParcelDrop.ThePaymentThatIsThere"/>). Nothing ever came back. This is what comes back.</para>
///
/// <h3>Why the gallery and not the park (#794, 2026-09-28)</h3>
///
/// <para>Slice 1 left the return under a bench in the park of the complex under the ground that was dug. It
/// was complete and, in shipped play, silent: a park is a Hive block, a Hive is <see cref="SecretLab.Present"/>,
/// no sol moon keeps one, and <see cref="ParcelDrop.For(string, IReadOnlyList{string})"/> only ever names the
/// scenario's moons — so the delivery rail could never name a ground a drop stood on. The owner ruled the
/// drop moves to where the captain already goes. The gallery at the end of the observation walk
/// (<see cref="ObservationWalk.HavenId"/>) is a room nobody is in, with two steel tables and stone beside the
/// vending machines — exactly where a dead drop wants to be. The mechanic MOVED: the park's bench card is
/// back to its own two moves, and nothing is duplicated.</para>
///
/// <h3>The mechanic, as pure arithmetic</h3>
///
/// <list type="number">
/// <item><b>A paid delivery earns ONE return</b>, at any haven's dark-web desk, left under one of the two
/// gallery tables at Selene Gate. The haven always exists, so there is no gate on it beyond the room being
/// there: a berth with no gallery has no tables, and no tables is no drop and no sentence.</item>
/// <item><b>Where:</b> under one table, seeded off (parcel id, haven), named first or second by the room's
/// own table order.</item>
/// <item><b>When:</b> every <see cref="WatchesBetweenWindows"/>th watch from the watch the payment landed,
/// with a seeded offset. During the window watch the drop is LOADED and the mark is UP; at the next turnover
/// the crew that keeps the gallery clean wipes the stone, the goods lie <see cref="WatchesOfGrace"/> more
/// watch exposed, and then the counterparty takes them back until the next window. No dice after the seed:
/// the mark's clock and the goods' clock differ by exactly one watch, always.</item>
/// </list>
///
/// <h3>Where it is kept</h3>
///
/// <para>Nowhere new. Every fact rides the captain's durable register of turned-over ground — the same SET of
/// strings <see cref="ParcelDrop.NothingForThisHullOn"/> writes into — as tags: what is OWED, which marks the
/// captain SAW, which wipes were TOLD, and the collection itself (<see cref="CollectedTag"/>, the pattern
/// the owner named, written and unread). With no paid delivery on record not one tag exists, and nothing
/// anywhere in the game is different.</para>
///
/// <para>The lines are Fable-authored canon (#794, 2026-09-28 evening) and verbatim. The law is Kosh's:
/// nothing announces that a table has something under it. The move is ABSENT when there is nothing to feel
/// for, never greyed.</para>
/// </summary>
/// <param name="ParcelId">The delivered parcel whose payment earned this return.</param>
/// <param name="HavenId">The haven the gallery is in (<see cref="ObservationWalk.HavenId"/>).</param>
/// <param name="PaidWatch">The watch the payment landed on (<see cref="PatronRota.WatchIndex"/>).</param>
/// <param name="Table">The table's index in the gallery's own list — which is also how the instruction names
/// it: 0 is the first table, 1 the second.</param>
/// <param name="Offset">Watches from the payment to the first window, 0 to
/// <see cref="WatchesBetweenWindows"/> − 1.</param>
public readonly partial record struct ChalkMark(
    string ParcelId, string HavenId, long PaidWatch, int Table, int Offset)
{
    // ── THE CLOCK ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Every third watch — a day and a half between windows. A judgement call left for the
    /// owner.</summary>
    public const int WatchesBetweenWindows = 3;

    /// <summary>How long the goods outlive the mark: one watch, exposed. Also left for the owner.</summary>
    public const int WatchesOfGrace = 1;

    /// <summary>The first window: the payment's own watch plus the seeded offset.</summary>
    public long Window => PaidWatch + Offset;

    /// <summary>Is this watch a window — the counterparty's scheduled time?</summary>
    public bool IsWindow(long watch) =>
        watch >= Window && (watch - Window) % WatchesBetweenWindows == 0;

    /// <summary>The most recent window STRICTLY before <paramref name="watch"/>, or null when there has not
    /// been one yet.</summary>
    public long? LastWindowBefore(long watch)
    {
        if (watch <= Window)
        {
            return null;
        }
        long since = (watch - 1 - Window) % WatchesBetweenWindows;
        return watch - 1 - since;
    }

    /// <summary>Is the drop LOADED — the window watch, when the counterparty has been and gone?</summary>
    public bool IsLoadedAt(double simTime) => IsWindow(PatronRota.WatchIndex(simTime));

    /// <summary>Is the chalk on the stone? Exactly while the drop is loaded: it goes up with the goods and the
    /// crew takes it off at the next turnover. A collection does not take it down — nobody wiped the stone when
    /// the captain reached under the table (<see cref="OnTheStone"/>).</summary>
    public bool MarkIsUpAt(double simTime) => IsLoadedAt(simTime);

    /// <summary>Are the goods under the table? The window, and <see cref="WatchesOfGrace"/> watch after it,
    /// with the stone already clean.</summary>
    public bool GoodsAreThereAt(double simTime)
    {
        long w = PatronRota.WatchIndex(simTime);
        for (int back = 0; back <= WatchesOfGrace; back++)
        {
            if (IsWindow(w - back))
            {
                return true;
            }
        }
        return false;
    }

    // ── WHERE ───────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The haven every return is left at: the one with the observation walk and its gallery.</summary>
    public const string Haven = ObservationWalk.HavenId;

    /// <summary>The seed tag the table and the window are drawn on — its own stream.</summary>
    public const string SeedTag = "gallery-drop:seed";

    /// <summary>The table, counted the way the instruction says it: first or second.</summary>
    public int Ordinal => Table + 1;

    /// <summary>
    /// <b>THE RETURN, SEEDED OFF (PARCEL, HAVEN) AND THE TABLES THAT ARE REALLY THERE.</b> Null when the room
    /// has no table to leave it under — a berth with no gallery — or none the instruction can name in words.
    /// </summary>
    /// <param name="tables">How many tables the gallery at <paramref name="havenId"/> stands — the room's own
    /// published list, counted by the caller that can see the room.</param>
    public static ChalkMark? For(string parcelId, string havenId, long paidWatch, int tables)
    {
        ArgumentNullException.ThrowIfNull(parcelId);
        ArgumentNullException.ThrowIfNull(havenId);

        int nameable = Math.Min(tables, OrdinalWords.Count);
        if (nameable <= 0)
        {
            return null;
        }

        int table = DiceRule.Roll(DiceRule.Seed($"{SeedTag}:table:{parcelId}:{havenId}"), nameable).Face - 1;
        return new ChalkMark(parcelId, havenId, paidWatch, table, OffsetFor(parcelId, havenId));
    }

    /// <summary>The seeded offset of the first window — one stream, read by the return and the dev start.</summary>
    private static int OffsetFor(string parcelId, string havenId) =>
        DiceRule.Roll(DiceRule.Seed($"{SeedTag}:window:{parcelId}:{havenId}"), WatchesBetweenWindows).Face - 1;

    /// <summary>The ordinals the instruction can say — the gallery stands two tables.</summary>
    public static readonly IReadOnlyList<string> OrdinalWords = ["first", "second"];

    /// <summary>How far along the stone from the machine's edge the cross is chalked.</summary>
    public const double BesideTheMachineDu = 1.5;

    /// <summary>How far in from the wall's line the cross is drawn, so it sits on the room's face of it.</summary>
    public const double OnTheFaceDu = 0.6;

    /// <summary>
    /// <b>WHERE ON THE STONE.</b> The back wall of the gallery is the only stone in the room — the machines
    /// are bolted to it — and the cross goes on it beside the machine that stands behind the named table, on
    /// the side toward the throat, where anybody coming in out of the tube has the wall in front of them.
    /// Derived from the machine's own drawn box and the throat the walls were built round, never typed.
    /// </summary>
    /// <param name="machine">The machine bolted to the back wall behind the named table, as its drawn box.
    /// Its face stands off the wall into the room, so the wall is the box edge FARTHER from the room.</param>
    /// <param name="throatY">Where the tube opens into the gallery, across the wall's run.</param>
    /// <param name="roomIsWest">Whether the room lies toward smaller x of the wall.</param>
    public static (double X, double Y) WhereOnTheStone(
        (double X0, double Y0, double X1, double Y1) machine, double throatY, bool roomIsWest = true)
    {
        double wallX = roomIsWest ? machine.X1 : machine.X0;
        double x = wallX + (roomIsWest ? -OnTheFaceDu : OnTheFaceDu);
        double mid = (machine.Y0 + machine.Y1) / 2.0;
        double y = throatY > mid ? machine.Y1 + BesideTheMachineDu : machine.Y0 - BesideTheMachineDu;
        return (x, y);
    }

    // ── WHAT IS KEPT, AS TAGS IN THE REGISTER ───────────────────────────────────────────────────────────

    /// <summary>A return is owed: <c>gallery-owed:{haven}|{parcelId}@{paidWatch}</c>.</summary>
    public const string OwedTag = "gallery-owed";

    /// <summary>The collection, the pattern the owner named: <c>gallery-drop:{parcelId}@{watch}</c> — the
    /// exact shape of <see cref="ParcelDrop.NothingForThisHullOn"/>'s tags. Written, and read by nobody
    /// yet.</summary>
    public const string CollectedTag = "gallery-drop";

    /// <summary>The captain read the mark in this window.</summary>
    public const string SeenTag = "gallery-chalk-seen";

    /// <summary>The wipe of a seen mark has been told.</summary>
    public const string WipeToldTag = "gallery-chalk-wiped";

    /// <summary>Slice 1's names for the same four tags, in the same order, from when the drop was in a park.
    /// No shipped save carries them (the park drop was never reachable in sol play), and the parsers read
    /// them anyway.</summary>
    public static readonly IReadOnlyList<string> ParkTags =
        ["park-owed", "park-drop", "park-chalk-seen", "park-chalk-wiped"];

    private static string W(long watch) => watch.ToString(CultureInfo.InvariantCulture);

    /// <summary>The tag that records this return as owed.</summary>
    public string Owed => OwedFor(HavenId, ParcelId, PaidWatch);

    /// <summary>The owed tag for a payment, before the mark is built.</summary>
    public static string OwedFor(string havenId, string parcelId, long paidWatch) =>
        $"{OwedTag}:{havenId}|{parcelId}@{W(paidWatch)}";

    /// <summary>The collection tag, for the watch it happened on.</summary>
    public string CollectedOn(long watch) => $"{CollectedTag}:{ParcelId}@{W(watch)}";

    /// <summary>The mark of this window was seen.</summary>
    public string SeenOn(long window) => $"{SeenTag}:{ParcelId}@{W(window)}";

    /// <summary>The wipe of this window's mark was told.</summary>
    public string WipeToldOn(long window) => $"{WipeToldTag}:{ParcelId}@{W(window)}";

    /// <summary>The same tag under slice 1's name.</summary>
    private static string AsSliceOneSaidIt(string tag)
    {
        int colon = tag.IndexOf(':', StringComparison.Ordinal);
        if (colon < 0)
        {
            return tag;
        }
        string[] now = [OwedTag, CollectedTag, SeenTag, WipeToldTag];
        int i = Array.IndexOf(now, tag[..colon]);
        return i < 0 ? tag : ParkTags[i] + tag[colon..];
    }

    // ── WHAT THE STONE SAYS ─────────────────────────────────────────────────────────────────────────────

    /// <summary>What walking into the gallery tells, and the tag that keeps it told.</summary>
    public readonly record struct StoneBeat(string Line, string Tag);

    /// <summary>
    /// <b>THE CAPTAIN IS IN THE GALLERY: WHAT DOES THE STONE SAY?</b> The mark, once per window it is up. A
    /// wipe, once, and only of a mark the captain SAW go up — a wipe nobody saw is never told. Nothing,
    /// otherwise.
    /// </summary>
    public StoneBeat? InTheGallery(double simTime, IReadOnlyCollection<string>? register)
    {
        IReadOnlyCollection<string> seen = register ?? [];
        long w = PatronRota.WatchIndex(simTime);
        if (IsWindow(w))
        {
            string tag = SeenOn(w);
            return Contains(seen, tag) ? null : new StoneBeat(MarkIsUpLine, tag);
        }

        if (LastWindowBefore(w) is { } last && Contains(seen, SeenOn(last)))
        {
            string tag = WipeToldOn(last);
            return Contains(seen, tag) ? null : new StoneBeat(WipedLine, tag);
        }

        return null;
    }

    private static bool Contains(IReadOnlyCollection<string> set, string tag) =>
        Has(set, tag) || Has(set, AsSliceOneSaidIt(tag));

    private static bool Has(IReadOnlyCollection<string> set, string tag)
    {
        if (set is ICollection<string> c)
        {
            return c.Contains(tag);
        }
        foreach (string s in set)
        {
            if (string.Equals(s, tag, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    // ── THE TABLE ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The move's id on the seat's card.</summary>
    public const string FeelUnderTheLip = "gallery:feel-under-the-lip";

    /// <summary>The move's label. Canon.</summary>
    public const string FeelUnderTheLipLabel = "FEEL UNDER THE LIP";

    /// <summary>
    /// <b>THE TABLE'S CARD, WITH THE MOVE OR WITHOUT IT.</b> The card the captain sat down to
    /// (<see cref="SittingAlone.TheTable"/>, whichever register it is in), plus one move before its last —
    /// only when the goods are under THIS table and the captain has it to himself. Otherwise the card is the
    /// plain table's, move for move: absent, never greyed. Idempotent both ways, so a frame may ask it
    /// again.
    /// </summary>
    public static Encounter.Scene TheTable(Encounter.Scene table, bool goodsUnderThisLip)
    {
        IReadOnlyList<Encounter.Move> had = table.Moves ?? [];
        var moves = new List<Encounter.Move>(had.Count + 1);
        foreach (Encounter.Move m in had)
        {
            if (!string.Equals(m.Id, FeelUnderTheLip, StringComparison.Ordinal))
            {
                moves.Add(m);
            }
        }
        if (goodsUnderThisLip && moves.Count > 0)
        {
            moves.Insert(moves.Count - 1, new(FeelUnderTheLip, FeelUnderTheLipLabel, Says: FeltLine));
        }
        return table with { Moves = moves };
    }

    /// <summary>Does this card carry the move?</summary>
    public static bool Offers(Encounter.Scene scene)
    {
        foreach (Encounter.Move m in scene.Moves ?? [])
        {
            if (string.Equals(m.Id, FeelUnderTheLip, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>Is the move on offer to a captain at this table, at this moment?</summary>
    public bool IsOnOffer(int table, bool alone, double simTime) =>
        alone && table == Table && GoodsAreThereAt(simTime);

    /// <summary>How many other grounds the return is tried against before it settles for any.</summary>
    public const int ReturnAddressTries = 16;

    /// <summary>
    /// <b>WHAT IS UNDER THE LIP:</b> the counterparty's parcel, already addressed — an
    /// <see cref="UnlistedParcel"/> whose id is the return's own, so <see cref="ParcelDrop.For(string, IReadOnlyList{string})"/>
    /// names its ground the way it names every parcel's, and it rides the whole rail: bury it, get paid, earn
    /// the next return. Addressed ELSEWHERE than the ground that was dug for the delivery that earned it (the
    /// rail's own answer for that parcel, asked again rather than stored) — the first of a short seeded run of
    /// ids whose ground is another body (distance between moons changes with the clock, so "farther" is read
    /// as "not there"). Named for slice 1's slat and kept, by the brief.
    /// </summary>
    public Satchel.Item TheParcelUnderTheSlat(IReadOnlyList<string>? landableBodyIds)
    {
        string? dug = ParcelDrop.For(ParcelId, landableBodyIds)?.BodyId;
        string first = ReturnId(0);
        for (int k = 0; k < ReturnAddressTries; k++)
        {
            string id = ReturnId(k);
            if (ParcelDrop.For(id, landableBodyIds) is { } where
                && !string.Equals(where.BodyId, dug, StringComparison.Ordinal))
            {
                return new Satchel.Item(Satchel.Kind.Parcel, id);
            }
        }
        return new Satchel.Item(Satchel.Kind.Parcel, first);
    }

    private string ReturnId(int k) => $"{ParcelId}~{k.ToString(CultureInfo.InvariantCulture)}";

    // ── THE LINES (verbatim, #794 · Fable, 2026-09-28 evening) ──────────────────────────────────────────

    /// <summary>The glyph the mark's notes are filed under — one no other kind in the game files under.</summary>
    public const string Glyph = "🖍";

    /// <summary>Appended to the payment pulse at any desk. <c>{0}</c> is the table's ordinal, in words.</summary>
    public const string PaymentLine =
        "There is something for you at Selene Gate. The gallery at the end of the walk, the {0} table. "
        + "Watch the stone by the machines.";

    /// <summary>The payment line with the table in it.</summary>
    public string ThePaymentLine() =>
        string.Format(CultureInfo.InvariantCulture, PaymentLine, OrdinalWords[Ordinal - 1]);

    /// <summary>Entering the gallery, the mark up. Once per window.</summary>
    public const string MarkIsUpLine =
        "Somebody has chalked the stone beside the machines. A cross, waist-high, the width of a hand. "
        + "The crew that keeps this gallery clean will file it as damage by the next watch.";

    /// <summary>In the gallery, after a wipe the captain saw go up. Once.</summary>
    public const string WipedLine =
        "The stone is clean. Somebody wiped it, or somebody read it. The stone does not say.";

    /// <summary>Under the lip.</summary>
    public const string FeltLine =
        "Tape, cold. A packet the size of a hand, wrapped so it does not rattle. Nobody on the walk looks round.";

    /// <summary>What the field book keeps of the collection.</summary>
    public const string CollectedEntry =
        "Collected under the gallery's table. Whoever left it keeps the walk's hours better than the walk does.";

    /// <summary>Every sentence this slice can put on a screen.</summary>
    public static IEnumerable<string> AllProse()
    {
        yield return PaymentLine;
        yield return MarkIsUpLine;
        yield return WipedLine;
        yield return FeelUnderTheLipLabel;
        yield return FeltLine;
        yield return CollectedEntry;
    }
}
