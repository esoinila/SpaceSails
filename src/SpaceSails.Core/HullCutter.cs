using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #537 · THE SECOND TOOL, AND THE ONE THAT COSTS. Owner, in the sentence that filed this whole lane:
/// <i>"Maybe a combi of detect tool that scans on timer like 5 seconds at a spot snd a tool to get at it.
/// That way we can gamify the seach."</i>
///
/// <para><b>Half of that combi shipped free.</b> The sounder is a switch on the captain's remote — no object,
/// no price, no wear — which is right for it: knocking on a wall is a thing a person does with a glove. But
/// FORCING the plate shipped free as well, and that is not a thing a person does with a glove. Until now
/// <c>OpenTheFalsePlate</c> was one bool and a paragraph: find it and it is yours, with nothing in between
/// worth calling a decision.</para>
///
/// <h3>The cost is the CELL, and the cell is the item</h3>
/// <para>There is no per-item wear model in this codebase and inventing one would rewrite what every saved
/// satchel means (<see cref="Satchel.Item.Stored"/> is <c>kind:count:id</c>, and the kind ordinal is load
/// bearing). It does not need one: <see cref="Satchel.Item.Count"/> already IS the wear. A cutting rig is
/// carried as one row with <see cref="CutsPerCell"/> cuts in it, <see cref="Satchel.Remove"/> takes one off
/// per plate, and at the last cut the row goes — the stub is left in the hole, which is what happens to
/// cutting gear. No new predicate, no new save format, and the arithmetic is the arithmetic six loose rounds
/// already use.</para>
///
/// <h3>What makes the force a decision rather than a formality</h3>
/// <list type="number">
/// <item><b>You have to be carrying it</b>, and it is <see cref="Satchel.Kind.Tool"/> — bulky, so it rides in
/// the pockets proper and the honest price of bringing it is not bringing something else.</item>
/// <item><b>Cuts are finite and bought.</b> Three to a cell, over a counter that does not write anything
/// down.</item>
/// <item><b>The cut is loud, slow and permanent</b> — and, from #537 slice 3 on, it is also the thing that
/// gives a stowaway away while it is still warm (<see cref="HullStowage.CutStaysWarmSeconds"/>). Cutting is
/// no longer only what gets you IN; it is the evidence you are in there.</item>
/// </list>
///
/// <para>Pure and deterministic, like everything else in Core: the same satchel offered to the same plate
/// gives the same answer, always.</para>
/// </summary>
public static class HullCutter
{
    // ── THE OBJECT ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The durable id the satchel and the vault hold. Short, and not a seed tag: there is one kind
    /// of these and a captain either has one or does not.</summary>
    public const string ItemId = "hull-cutter";

    /// <summary>What the satchel row calls it.</summary>
    public const string ItemName = "🔥 HULL CUTTER";

    /// <summary>
    /// HOW MANY PLATES ONE CELL OPENS. Three, and the number is doing real work in both directions: at one,
    /// a captain who finds a void on the wrong hull has nothing left for the right one and the whole search
    /// becomes a thing you do not dare start; at ten, the cell never runs out and the tool is furniture with
    /// a purchase price. Three means a void is worth roughly a third of a cutter, and about one hull in five
    /// hides one (<see cref="HullSounding.VoidOnARollOf"/>) — so a captain who buys a rig is buying about
    /// fifteen boardings of confidence. FLAGGED for the owner's tuning.
    /// </summary>
    public const int CutsPerCell = 3;

    /// <summary>A fresh rig, cell full.</summary>
    public static Satchel.Item FreshRig => new(Satchel.Kind.Tool, ItemId, CutsPerCell);

    /// <summary>Is this the rig? Asked of an item rather than of an id, so nothing else in the pocket can
    /// answer to it by accident.</summary>
    public static bool IsTheCutter(Satchel.Item item) =>
        item.Kind == Satchel.Kind.Tool && string.Equals(item.Id, ItemId, System.StringComparison.Ordinal);

    /// <summary>How many cuts are left in the cell. Zero when there is no rig at all — the two are the same
    /// thing to a captain standing at a plate, and one number keeps every caller from having to ask which.</summary>
    public static int CutsLeft(IReadOnlyList<Satchel.Item>? carried) =>
        Satchel.CountOf(carried, Satchel.Kind.Tool, ItemId);

    /// <summary>Whether there is a rig with anything in it.</summary>
    public static bool InThePocket(IReadOnlyList<Satchel.Item>? carried) => CutsLeft(carried) > 0;

    // ── WHAT ONE CUT COSTS ──────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// How long it takes to go through a false plate, standing still. Longer than the loud sounding (5 s) and
    /// shorter than the quiet one (12 s), and it is the SAME clock idiom the sounding, the dig and the door
    /// force all use: the seconds buy the answer and walking away buys nothing. FLAGGED for tuning.
    /// </summary>
    public const double CutSeconds = 9.0;

    /// <summary>What the counter asks for a rig. Under the scanner's 320 on purpose — the kit that finds a
    /// thing should cost more than the kit that opens it, or nobody ever buys the one that does the
    /// thinking.</summary>
    public const int PriceCr = 240;

    /// <summary>What the switch on the bar card says.</summary>
    public static string BuyLabel => $"🔥 HULL CUTTER — {PriceCr} cr";

    // ── THE CUT ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>What the rig did about a plate.</summary>
    /// <param name="Cut">Whether the plate came off.</param>
    /// <param name="CutsLeft">What is left in the cell afterwards.</param>
    /// <param name="Carried">The satchel afterwards — the caller assigns it rather than mutating anything.</param>
    /// <param name="Line">Told on-screen, always. Never empty, in any case, refusals included.</param>
    public readonly record struct Order(
        bool Cut, int CutsLeft, IReadOnlyList<Satchel.Item> Carried, string Line);

    /// <summary>
    /// SPEND ONE CUT. The purse-in/purse-out shape <c>ShootTheLock.Fire</c> already uses, because it is the
    /// same kind of act: something finite is consumed, and Core says what it cost rather than leaving the
    /// caller to guess.
    /// </summary>
    public static Order Force(IReadOnlyList<Satchel.Item>? carried)
    {
        int left = CutsLeft(carried);
        if (left <= 0)
        {
            return new Order(false, 0, carried ?? [], NoCutterLine);
        }

        IReadOnlyList<Satchel.Item> after = Satchel.Remove(carried, Satchel.Kind.Tool, ItemId, 1);
        int now = CutsLeft(after);
        return new Order(true, now, after, now > 0 ? CutLine(now) : LastCutLine);
    }

    /// <summary>Bought over a counter that writes nothing down.</summary>
    /// <param name="Taken">Whether a rig is now in the pocket.</param>
    /// <param name="Cost">What it cost. Zero on every refusal.</param>
    /// <param name="RemainingCredits">The purse afterwards.</param>
    /// <param name="Line">Told on-screen, always. Never empty, in any of the four cases.</param>
    public readonly record struct Bought(bool Taken, int Cost, int RemainingCredits, string Line);

    /// <summary>BUY ONE — the scanner's own four cases, in the scanner's own order, because it is the same
    /// counter and a captain should not be able to tell which of the two things they just bought went
    /// through different code.
    ///
    /// <para>A part-spent rig refuses a second: the cell is the item, and two rigs in one pocket would be
    /// two rows the satchel cannot merge (<see cref="Satchel.Stacks"/> is rounds only). Spend it to the stub
    /// and the row goes, and then the counter will sell you another.</para></summary>
    public static Bought Buy(int credits, IReadOnlyList<Satchel.Item>? carried)
    {
        if (InThePocket(carried))
        {
            return new Bought(false, 0, credits, AlreadyCarryingLine);
        }
        if (credits < PriceCr)
        {
            return new Bought(false, 0, credits, ShortLine);
        }
        if (!Satchel.CanTake(carried, FreshRig))
        {
            return new Bought(false, 0, credits, NoRoomLine);
        }
        return new Bought(true, PriceCr, credits - PriceCr, SoldLine);
    }

    // ── WHAT IS SAID ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The prompt, naming both costs before either is spent — the seconds and the cell — because a
    /// tool that only mentions its clock is hiding the half you cannot get back.</summary>
    public static string OfferLine(int cutsLeft) =>
        $"🔥 Cut the plate out — {CutSeconds:0} s of standing still, and one of the {cutsLeft} cuts in the cell.";

    /// <summary>Said while the clock runs. It is loud and it is not subtle, and the line says so.</summary>
    public const string WorkingLine =
        "🔥 The rig bites and the plate goes orange along the line of it. Nobody aboard is going to mistake " +
        "this for the ship settling.";

    /// <summary>…and abandoned. The cell is NOT refunded halfway through — the same law the sounding runs
    /// on, and for the same reason: a clock you can nibble at is not a cost.</summary>
    public const string AbandonedLine =
        "🔥 You come off the plate and the cut goes dark half-finished. That much of the cell is gone, and " +
        "the ship heard every second of it.";

    /// <summary>No rig, or nothing left in the cell. The refusal names the reason, like every refusal in the
    /// satchel: a silent nothing is indistinguishable from a bug.</summary>
    public const string NoCutterLine =
        "🔥 It is welded on three sides and shimmed on the fourth, and your hands are not a tool. Somebody " +
        "sells the rig for this over a back counter; nobody sells the trick of doing it without one.";

    /// <summary>One cut spent, and the cell says how many are left. A number, never a warning.</summary>
    public static string CutLine(int cutsLeft) =>
        $"🔥 The plate comes out in one piece and the cut edge glows a while. {cutsLeft} left in the cell.";

    /// <summary>…and the last one. The rig is left in the hole, because that is what happens to a flat cell
    /// and a worn blade a long way from anywhere that sells either.</summary>
    public const string LastCutLine =
        "🔥 The plate comes out, the cell goes flat in the same breath, and the rig is suddenly a shaped " +
        "weight. You leave it standing in the hole it made.";

    /// <summary>The receipt. Nobody writes anything down, which is the product.</summary>
    public const string SoldLine =
        "🔥 A cutting rig in a canvas roll, cell charged, blade unworn. “Salvage work,” he says, without " +
        "looking up. “Everybody does salvage work.”";

    /// <summary>Not enough in the purse.</summary>
    public static string ShortLine =>
        $"“It is {PriceCr} cr and the cell is most of that. No, you cannot have it without one.”";

    /// <summary>One is enough while it has anything in it.</summary>
    public const string AlreadyCarryingLine =
        "“You are carrying one with cuts in it. Come back when it is a paperweight.”";

    /// <summary>The pockets will not take it — it is a bulky thing and the satchel's own arithmetic says so
    /// (#688). Said rather than swallowed.</summary>
    public const string NoRoomLine =
        "“It is a roll of steel and a battery. Empty something first.”";

    /// <summary>What the satchel row shows, cuts and all — the count is the whole of the object's state, so
    /// it belongs on the face of it.</summary>
    public static string RowLabel(int cutsLeft) =>
        cutsLeft == 1 ? $"{ItemName} · one cut left" : $"{ItemName} · {cutsLeft} cuts";

    /// <summary>The card, when the captain looks at what they are carrying. It says what the object IS and
    /// never what to do with it — <c>CarriedObject</c>'s own law.</summary>
    public const string CardLabel = "🔥 HULL CUTTER";

    /// <summary>…and its story. No lock is named in it.</summary>
    public const string CardStory =
        "A shoulder roll of hose, a striker and a cell heavy enough to be worth stealing on its own. The " +
        "blade housing is scorched to the colour of old brass and somebody has scratched a tally into it " +
        "that stops at fourteen. It cuts steel slowly, loudly, and only once per line — there is no undoing " +
        "a cut and no hiding that one was made.";
}
