using System;
using System.Collections.Generic;
using System.Globalization;
using SpaceSails.Core.Interior;

namespace SpaceSails.Core;

/// <summary>
/// #794 slice 1 · <b>THE CHALK MARK — the faceless trade's return leg.</b>
///
/// <para>Owner, on #794: <i>"I saw an X marked with chalk and I knew there was a dead drop waiting with my
/// name on it…"</i> and, on the trade that makes it an economy: <i>"they would probably like to use a dead
/// drop with some scheduled times of possible use."</i> #711 shipped the first half — a parcel from a
/// dark-web desk, buried at the ground <see cref="ParcelDrop.Destination"/> names, paid a few watches later
/// (<see cref="ParcelDrop.ThePaymentThatIsThere"/>). Nothing ever came back. This is what comes back.</para>
///
/// <h3>The mechanic, as pure arithmetic</h3>
///
/// <list type="number">
/// <item><b>A paid delivery earns ONE return</b>, left at the ground that was dug — if the complex under that
/// ground keeps a park (<see cref="TheGroundKeepsAPark"/>). No park, no drop, no sentence.</item>
/// <item><b>Where:</b> under one bench, seeded off (parcel id, body), never the bench the park's lone figure
/// sits on — a drop under a plank somebody is always sitting on is a drop nobody can reach alone.</item>
/// <item><b>When:</b> every <see cref="WatchesBetweenWindows"/>th watch from the watch the payment landed,
/// with a seeded offset. During the window watch the drop is LOADED and the mark is UP; at the next turnover
/// the grounds crew wipes the wall, the goods lie <see cref="WatchesOfGrace"/> more watch exposed, and then
/// the counterparty takes them back until the next window. No dice after the seed: the mark's clock and the
/// goods' clock differ by exactly one watch, always.</item>
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
/// <para>The lines are Fable-authored canon (#794, 2026-09-27) and verbatim. The law is Kosh's: nothing
/// announces that a bench has something under it. The move is ABSENT when there is nothing to feel for,
/// never greyed.</para>
/// </summary>
/// <param name="ParcelId">The delivered parcel whose payment earned this return.</param>
/// <param name="BodyId">The ground that was dug — the body whose complex keeps the park.</param>
/// <param name="PaidWatch">The watch the payment landed on (<see cref="PatronRota.WatchIndex"/>).</param>
/// <param name="Bench">The bench's index in <c>Park.Benches</c> — the room's own ordinal.</param>
/// <param name="Ordinal">The same bench, counted from the gate along the walk (1-based), which is how the
/// instruction names it.</param>
/// <param name="Offset">Watches from the payment to the first window, 0 to
/// <see cref="WatchesBetweenWindows"/> − 1.</param>
public readonly record struct ChalkMark(
    string ParcelId, string BodyId, long PaidWatch, int Bench, int Ordinal, int Offset)
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

    /// <summary>Is the chalk on the wall? Exactly while the drop is loaded: it goes up with the goods and the
    /// grounds crew takes it off at the next turnover.</summary>
    public bool MarkIsUpAt(double simTime) => IsLoadedAt(simTime);

    /// <summary>Are the goods under the slat? The window, and <see cref="WatchesOfGrace"/> watch after it,
    /// with the wall already clean.</summary>
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

    /// <summary>The seed tag the bench and the window are drawn on — its own stream.</summary>
    public const string SeedTag = "park-drop:seed";

    /// <summary>
    /// <b>THE RETURN, SEEDED OFF (PARCEL, BODY) AND THE PARK THAT IS REALLY THERE.</b> Null when the park
    /// has no bench to leave it under, or none the instruction can name in words.
    /// </summary>
    public static ChalkMark? For(string parcelId, string bodyId, long paidWatch, in UndergroundComplex.Park park)
    {
        ArgumentNullException.ThrowIfNull(parcelId);
        ArgumentNullException.ThrowIfNull(bodyId);

        var free = new List<ParkBenches.Bench>();
        foreach (ParkBenches.Bench b in ParkBenches.On(in park))
        {
            if (!b.Taken && OrdinalFromTheGate(in park, b.Index) is { } n && n <= OrdinalWords.Count)
            {
                free.Add(b);
            }
        }
        if (free.Count == 0)
        {
            return null;
        }

        ParkBenches.Bench bench =
            free[DiceRule.Roll(DiceRule.Seed($"{SeedTag}:bench:{parcelId}:{bodyId}"), free.Count).Face - 1];
        int offset =
            DiceRule.Roll(DiceRule.Seed($"{SeedTag}:window:{parcelId}:{bodyId}"), WatchesBetweenWindows).Face - 1;

        return new ChalkMark(
            parcelId, bodyId, paidWatch, bench.Index, OrdinalFromTheGate(in park, bench.Index)!.Value, offset);
    }

    /// <summary>
    /// <b>THE BENCH, COUNTED FROM THE GATE ALONG THE WALK.</b> The gate is the one with the notice on it
    /// (<c>Park.X</c> is where that plate reads from). The walk leaves the gate spur in two directions, so a
    /// bench is counted along ITS OWN side: the nearer benches on the same side of the gate, plus one. The
    /// shipped park is symmetric about its gate, so every ordinal names two benches, one each way — and the
    /// third one either way is the lone figure's, which never holds a drop. The captain walks the other.
    /// </summary>
    public static int? OrdinalFromTheGate(in UndergroundComplex.Park park, int benchIndex)
    {
        IReadOnlyList<(double X, double Y)> planks = park.Benches ?? [];
        if (benchIndex < 0 || benchIndex >= planks.Count)
        {
            return null;
        }

        double gate = park.X;
        double mine = planks[benchIndex].X - gate;
        int nearer = 0;
        foreach ((double bx, double _) in planks)
        {
            double d = bx - gate;
            if (Math.Sign(d) == Math.Sign(mine) && Math.Abs(d) < Math.Abs(mine))
            {
                nearer++;
            }
        }
        return nearer + 1;
    }

    /// <summary>The ordinals the instruction can say, 1 to 6 — a park lays one bench per bend, six in all.</summary>
    public static readonly IReadOnlyList<string> OrdinalWords =
        ["first", "second", "third", "fourth", "fifth", "sixth"];

    /// <summary>
    /// <b>DOES THE COMPLEX UNDER THIS GROUND KEEP A PARK?</b> A building must actually be down there — the
    /// same seeded fact the landing resolves (<see cref="SecretLab.Present"/>) — and its top pressurised floor
    /// must be the floor with the green (<see cref="UndergroundComplex.HasParkBlock"/>). The head office has
    /// none. An instruction naming a park on a moon with no building under it would be a lie the captain
    /// could fly to.
    /// </summary>
    public static bool TheGroundKeepsAPark(string? bodyId, bool forcePresent = false) =>
        bodyId is not null
        && (forcePresent || SecretLab.Present(bodyId))
        && UndergroundComplex.TopPressurisedFloor(bodyId) is { } level
        && UndergroundComplex.HasParkBlock(bodyId, level);

    /// <summary>The park under this ground, or null — built on the real field by the real generator.</summary>
    public static UndergroundComplex.Park? TheParkUnder(
        string? bodyId, in SurfaceLayout.Field field, bool forcePresent = false) =>
        TheGroundKeepsAPark(bodyId, forcePresent)
        && UndergroundComplex.TopPressurisedFloor(bodyId!) is { } level
            ? UndergroundComplex.Build(bodyId!, level, field).Park
            : null;

    /// <summary>How close to the notice a captain must be to read the wall beside it.</summary>
    public const double NoticeReachDu = 8.0;

    /// <summary>Is the captain standing at the gate notice?</summary>
    public static bool AtTheNotice(in UndergroundComplex.Park park, double x, double y) =>
        ((x - park.X) * (x - park.X)) + ((y - park.Y) * (y - park.Y)) <= NoticeReachDu * NoticeReachDu;

    /// <summary>How far past the gate's edge the cross is chalked.</summary>
    public const double BesideTheGateDu = 1.5;

    /// <summary>How far in from the wall's line the cross is drawn, so it sits on the park's face of it.</summary>
    public const double OnTheFaceDu = 0.6;

    /// <summary>
    /// <b>WHERE ON THE WALL.</b> Beside the gate with the notice, on the park's face of the near wall — the
    /// shotcrete the grounds crew keeps clean. Derived from the gate the carve cut, never typed.
    /// </summary>
    public static (double X, double Y) WhereOnTheWall(in UndergroundComplex.Park park)
    {
        SurfaceLayout.Doorway gate = park.Gate;
        double edge = Math.Max(gate.X1, gate.X2);
        double wallY = (gate.Y1 + gate.Y2) / 2.0;
        double inward = park.Y < wallY ? -OnTheFaceDu : OnTheFaceDu;
        return (edge + BesideTheGateDu, wallY + inward);
    }

    // ── WHAT IS KEPT, AS TAGS IN THE REGISTER ───────────────────────────────────────────────────────────

    /// <summary>A return is owed: <c>park-owed:{body}|{parcelId}@{paidWatch}</c>.</summary>
    public const string OwedTag = "park-owed";

    /// <summary>The collection, the pattern the owner named: <c>park-drop:{parcelId}@{watch}</c> — the exact
    /// shape of <see cref="ParcelDrop.NothingForThisHullOn"/>'s tags. Written, and read by nobody yet.</summary>
    public const string CollectedTag = "park-drop";

    /// <summary>The captain read the mark in this window.</summary>
    public const string SeenTag = "park-chalk-seen";

    /// <summary>The wipe of a seen mark has been told.</summary>
    public const string WipeToldTag = "park-chalk-wiped";

    private static string W(long watch) => watch.ToString(CultureInfo.InvariantCulture);

    /// <summary>The tag that records this return as owed.</summary>
    public string Owed => $"{OwedTag}:{BodyId}|{ParcelId}@{W(PaidWatch)}";

    /// <summary>The owed tag for a payment, before the mark is built.</summary>
    public static string OwedFor(string bodyId, string parcelId, long paidWatch) =>
        $"{OwedTag}:{bodyId}|{parcelId}@{W(paidWatch)}";

    /// <summary>The collection tag, for the watch it happened on.</summary>
    public string CollectedOn(long watch) => $"{CollectedTag}:{ParcelId}@{W(watch)}";

    /// <summary>The mark of this window was seen.</summary>
    public string SeenOn(long window) => $"{SeenTag}:{ParcelId}@{W(window)}";

    /// <summary>The wipe of this window's mark was told.</summary>
    public string WipeToldOn(long window) => $"{WipeToldTag}:{ParcelId}@{W(window)}";

    /// <summary>Has this return been collected, on any watch?</summary>
    public bool WasCollected(IEnumerable<string>? register)
    {
        string prefix = $"{CollectedTag}:{ParcelId}@";
        foreach (string tag in register ?? [])
        {
            if (tag.StartsWith(prefix, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// <b>EVERY RETURN OWED ON THIS BODY AND NOT YET COLLECTED</b>, in the register's ordinal order (the set
    /// has none of its own). Empty — without building anything — when no delivery was ever paid.
    /// </summary>
    public static IReadOnlyList<ChalkMark> OwedOn(
        IEnumerable<string>? register, string? bodyId, in UndergroundComplex.Park park)
    {
        if (register is null || bodyId is null)
        {
            return [];
        }

        string prefix = $"{OwedTag}:{bodyId}|";
        List<string>? owed = null;
        foreach (string tag in register)
        {
            if (tag.StartsWith(prefix, StringComparison.Ordinal))
            {
                (owed ??= []).Add(tag);
            }
        }
        if (owed is null)
        {
            return [];
        }
        owed.Sort(StringComparer.Ordinal);

        var marks = new List<ChalkMark>(owed.Count);
        foreach (string tag in owed)
        {
            string rest = tag[prefix.Length..];
            int at = rest.LastIndexOf('@');
            if (at <= 0
                || !long.TryParse(rest[(at + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out long paid))
            {
                continue;
            }
            if (For(rest[..at], bodyId, paid, in park) is { } mark && !mark.WasCollected(register))
            {
                marks.Add(mark);
            }
        }
        return marks;
    }

    // ── WHAT THE WALL SAYS ──────────────────────────────────────────────────────────────────────────────

    /// <summary>What walking to the notice tells, and the tag that keeps it told.</summary>
    public readonly record struct GateBeat(string Line, string Tag);

    /// <summary>
    /// <b>THE CAPTAIN IS AT THE NOTICE: WHAT DOES THE WALL SAY?</b> The mark, once per window it is up. A wipe,
    /// once, and only of a mark the captain SAW go up — a wipe nobody saw is never told. Nothing, otherwise.
    /// </summary>
    public GateBeat? AtTheGate(double simTime, IReadOnlyCollection<string>? register)
    {
        IReadOnlyCollection<string> seen = register ?? [];
        long w = PatronRota.WatchIndex(simTime);
        if (IsWindow(w))
        {
            string tag = SeenOn(w);
            return Contains(seen, tag) ? null : new GateBeat(MarkIsUpLine, tag);
        }

        if (LastWindowBefore(w) is { } last && Contains(seen, SeenOn(last)))
        {
            string tag = WipeToldOn(last);
            return Contains(seen, tag) ? null : new GateBeat(WipedLine, tag);
        }

        return null;
    }

    private static bool Contains(IReadOnlyCollection<string> set, string tag)
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

    // ── THE BENCH ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The move's id on the seat's card.</summary>
    public const string FeelUnderTheSlat = "park:feel-under-the-slat";

    /// <summary>The move's label. Canon.</summary>
    public const string FeelUnderTheSlatLabel = "FEEL UNDER THE SLAT";

    /// <summary>
    /// <b>THE BENCH'S CARD, WITH THE MOVE OR WITHOUT IT.</b> <see cref="ParkBenches.TheBench"/> exactly, plus
    /// one move — only when the captain has the whole plank AND the goods are under THIS one. Anything else
    /// and the card is the plain bench's, move for move: absent, never greyed.
    /// </summary>
    public static Encounter.Scene TheBench(bool shared, bool goodsUnderThisSlat)
    {
        Encounter.Scene bench = ParkBenches.TheBench(shared);
        if (shared || !goodsUnderThisSlat)
        {
            return bench;
        }

        var moves = new List<Encounter.Move>(bench.Moves.Count + 1);
        for (int i = 0; i < bench.Moves.Count; i++)
        {
            if (i == bench.Moves.Count - 1)
            {
                moves.Add(new(FeelUnderTheSlat, FeelUnderTheSlatLabel, Says: FeltLine));
            }
            moves.Add(bench.Moves[i]);
        }
        return bench with { Moves = moves };
    }

    /// <summary>Does this card carry the move?</summary>
    public static bool Offers(Encounter.Scene scene)
    {
        foreach (Encounter.Move m in scene.Moves ?? [])
        {
            if (string.Equals(m.Id, FeelUnderTheSlat, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>Is the move on offer to a captain on this bench, at this moment?</summary>
    public bool IsOnOffer(int benchIndex, bool shared, double simTime) =>
        !shared && benchIndex == Bench && GoodsAreThereAt(simTime);

    /// <summary>How many other grounds the return is tried against before it settles for any.</summary>
    public const int ReturnAddressTries = 16;

    /// <summary>
    /// <b>WHAT IS UNDER THE SLAT:</b> the counterparty's parcel, already addressed — an
    /// <see cref="UnlistedParcel"/> whose id is the return's own, so <see cref="ParcelDrop.For(string, IReadOnlyList{string})"/>
    /// names its ground the way it names every parcel's, and it rides the whole rail: bury it, get paid, earn
    /// the next return. Addressed ELSEWHERE than the ground that was dug — the first of a short seeded run of
    /// ids whose ground is another body (distance between moons changes with the clock, so "farther" is read
    /// as "not here").
    /// </summary>
    public Satchel.Item TheParcelUnderTheSlat(IReadOnlyList<string>? landableBodyIds)
    {
        string first = ReturnId(0);
        for (int k = 0; k < ReturnAddressTries; k++)
        {
            string id = ReturnId(k);
            if (ParcelDrop.For(id, landableBodyIds) is { } where
                && !string.Equals(where.BodyId, BodyId, StringComparison.Ordinal))
            {
                return new Satchel.Item(Satchel.Kind.Parcel, id);
            }
        }
        return new Satchel.Item(Satchel.Kind.Parcel, first);
    }

    private string ReturnId(int k) => $"{ParcelId}~{k.ToString(CultureInfo.InvariantCulture)}";

    // ── THE DEV START ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>Which scene <c>?chalk=</c> asked for.</summary>
    public enum Cheat
    {
        /// <summary>No chalk asked for.</summary>
        None,
        /// <summary><c>?park=1&amp;chalk=1</c> — a paid delivery on record, the clock at a window.</summary>
        Up,
        /// <summary><c>?park=1&amp;chalk=wiped</c> — one watch later, the mark seen and wiped.</summary>
        Wiped,
    }

    /// <summary>Read <c>chalk=</c> off an address. Read off the address rather than stored, because a latch
    /// on the page would move the frame ledger for a flag that is false in every other scene.</summary>
    public static Cheat CheatIn(string? uri)
    {
        if (uri is null)
        {
            return Cheat.None;
        }
        int q = uri.IndexOf('?', StringComparison.Ordinal);
        if (q < 0)
        {
            return Cheat.None;
        }
        foreach (string pair in uri[(q + 1)..].Split('&', '#'))
        {
            if (!pair.StartsWith("chalk=", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            string v = Uri.UnescapeDataString(pair["chalk=".Length..]).ToLowerInvariant();
            return v switch
            {
                "1" or "true" or "yes" or "up" => Cheat.Up,
                "wiped" => Cheat.Wiped,
                _ => Cheat.None,
            };
        }
        return Cheat.None;
    }

    /// <summary>The paid watch that puts <paramref name="now"/> on a window (or one watch past one, for
    /// <see cref="Cheat.Wiped"/>) for this parcel on this ground — the dev start's arithmetic, done by the
    /// clock's own owner.</summary>
    public static long PaidWatchFor(Cheat cheat, string parcelId, string bodyId, long now)
    {
        int offset =
            DiceRule.Roll(DiceRule.Seed($"{SeedTag}:window:{parcelId}:{bodyId}"), WatchesBetweenWindows).Face - 1;
        long window = cheat == Cheat.Wiped ? now - 1 : now;
        return window - offset;
    }

    // ── THE LINES (verbatim, #794 · Fable, 2026-09-27) ──────────────────────────────────────────────────

    /// <summary>The glyph the mark's notes are filed under — one no other kind in the game files under.</summary>
    public const string Glyph = "🖍";

    /// <summary>Appended to the payment pulse, only when the dug ground keeps a park. <c>{0}</c> is the
    /// bench's ordinal, in words.</summary>
    public const string PaymentLine =
        "There is something for you where you dug. The park, the {0} bench from the gate. "
        + "Watch the wall by the notice.";

    /// <summary>The payment line with the bench in it.</summary>
    public string ThePaymentLine() =>
        string.Format(CultureInfo.InvariantCulture, PaymentLine, OrdinalWords[Ordinal - 1]);

    /// <summary>At the gate, the mark up.</summary>
    public const string MarkIsUpLine =
        "Somebody has chalked the wall beside the notice. A cross, waist-high, the width of a hand. "
        + "The crew that keeps this shotcrete clean will file it as damage by the next watch.";

    /// <summary>At the gate, after a wipe the captain saw go up. Once.</summary>
    public const string WipedLine =
        "The wall is clean. Somebody wiped it, or somebody read it. The shotcrete does not say.";

    /// <summary>Under the slat.</summary>
    public const string FeltLine =
        "Tape, cold. A packet the size of a hand, wrapped so it does not rattle. Nobody on the walk looks up.";

    /// <summary>What the field book keeps of the collection.</summary>
    public const string CollectedEntry =
        "Collected under the ground's bench. Whoever left it keeps the park's hours better than the park does.";

    /// <summary>Every sentence this slice can put on a screen.</summary>
    public static IEnumerable<string> AllProse()
    {
        yield return PaymentLine;
        yield return MarkIsUpLine;
        yield return WipedLine;
        yield return FeelUnderTheSlatLabel;
        yield return FeltLine;
        yield return CollectedEntry;
    }
}
